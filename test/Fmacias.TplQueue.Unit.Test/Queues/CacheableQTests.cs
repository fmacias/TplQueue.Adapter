using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Queues
{
    [TestFixture]
    public class CacheableQTests
    {
        [Test]
        public void TryLeaseWorkOnce_WhenNoSlots_DoesNotLease()
        {
            //Arrange
            var leaseCache = new Mock<IDataJobCache>(MockBehavior.Strict);
            var dispatcherMock = CreateDispatcherMock(slots: 0);
            using var queue = CacheQ.Create(Mock.Of<ILogger<ICacheQ>>(), leaseCache.Object, dispatcherMock.Object);

            //Act
            var leased = ((CacheQ)queue).TryLeaseWorkOnce();
            
            //Assert
            Assert.That(leased, Is.False);
            leaseCache.Verify(c => c.TryHydrateNextJob(out It.Ref<IDataJobRoot>.IsAny!, 
                out It.Ref<ICacheEntry>.IsAny!), Times.Never);
        }

        [Test]
        public void TryLeaseWorkOnce_WhenLeaseAvailable()
        {
            // Arrange
            var leaseCache = new Mock<IDataJobCache>();
            var lease = new TestLeaseEntry { IsFifo = true, CancellationToken = new CancellationTokenSource().Token };
            var runnerRoot = new TestPayloadCarrierRoot();

            // IMPORTANT: provide actual out vars
            IDataJobRoot outRoot = runnerRoot;
            ICacheEntry outLease = lease;

            leaseCache
                .Setup(x => x.TryHydrateNextJob(out outRoot, out outLease))
                .Returns(true);

            IJobRoot? addedRoot = null;
            bool? addedFifo = null;
            CancellationToken? addedToken = null;

            var jobQueueMock = CreateDispatcherMock(slots: 1);

            jobQueueMock
                .Setup(d => d.Enqueue(It.IsAny<IJobRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<IJobRoot, bool, CancellationToken>((r, f, t) =>
                {
                    addedRoot = r;
                    addedFifo = f;
                    addedToken = t;
                })
                .Returns(jobQueueMock.Object);

            var CacheQ = global::CacheQ.Create(
                Mock.Of<ILogger<ICacheQ>>(),
                leaseCache.Object,
                jobQueueMock.Object);

            // Act
            var leased = ((CacheQ)CacheQ).TryLeaseWorkOnce();

            // Assert
            Assert.That(leased, Is.True);
            Assert.That(addedRoot, Is.SameAs(runnerRoot));
            Assert.That(addedFifo, Is.True);

            CacheQ.Dispose();
        }

        [Test]
        public async Task OnJobEventChanged_CallBack_AcknowledgesLifecycle()
        {
            var leaseCache = new Mock<IDataJobCache>();
            var jobQueueMock = CreateDispatcherMock(slots: 1);
            var jobInfo = new Mock<IDataJobInfo>();
            var jobId = Guid.NewGuid();

            jobInfo.SetupGet(r => r.Id).Returns(jobId);

            var CacheQ = global::CacheQ.Create(Mock.Of<ILogger<ICacheQ>>(), leaseCache.Object, jobQueueMock.Object);
            var callback = CacheQ.OnJobEventChanged;

            await callback(CreateEvent(JobEventStatus.Successed, jobInfo.Object));
            await callback(CreateEvent(JobEventStatus.Failed, jobInfo.Object));
            await callback(CreateEvent(JobEventStatus.Canceled, jobInfo.Object));
            await callback(CreateEvent(JobEventStatus.RootSuccessed, jobInfo.Object));
            
            leaseCache.Verify(c => c.AckNode(jobId, jobInfo.Object), Times.Once);
            leaseCache.Verify(c => c.FailNode(jobId, It.IsAny<string?>()), Times.Once);
            leaseCache.Verify(c => c.CancelNode(jobId), Times.Once);
            leaseCache.Verify(c => c.SuccessRootNode(jobId), Times.Once);
            leaseCache.Verify(c => c.DeleteRootNode(jobId), Times.Exactly(4));

            CacheQ.Dispose();
        }
        [Test]
        public void LeasingPulseMs_NonPositiveValue_ResetsToDefault()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ICacheQ>>();
            var cache = Mock.Of<IDataJobCache>();
            var innerDispatcher = Mock.Of<IQ>();

            var dispatcher = CacheQ.Create(
                logger,
                cache,
                innerDispatcher);

            var defaultValue = dispatcher.LeasingPulseMs;

            // Act
            dispatcher.LeasingPulseMs = 0;
            var afterZero = dispatcher.LeasingPulseMs;

            dispatcher.LeasingPulseMs = -10;
            var afterNegative = dispatcher.LeasingPulseMs;

            // Assert
            Assert.That(afterZero, Is.EqualTo(defaultValue));
            Assert.That(afterNegative, Is.EqualTo(defaultValue));
        }

        private static IJobEvent CreateEvent(JobEventStatus status, IJobInfo info)
        {
            
            var evt = new Mock<IJobEvent>();
            evt.SetupGet(e => e.Status).Returns(status);
            evt.SetupGet(e => e.JobInfo).Returns(info);
            evt.SetupGet(e => e.Timestamp).Returns(DateTime.UtcNow);
            return evt.Object;
        }

        private static Mock<IQ> CreateDispatcherMock(int slots)
        {
            var jobQMock = new Mock<IQ>();
            jobQMock.SetupProperty(d => d.OnJobEventChanged);
            jobQMock.SetupGet(d => d.Semaphore).Returns(new SemaphoreSlim(slots));
            jobQMock.Setup(d => d.Dispose());
            jobQMock.Setup(d => d.Subscribe(It.IsAny<IObserver<IJobEvent>>())).Returns(Mock.Of<IDisposable>());
            jobQMock.SetupGet(d => d.Name).Returns("dispatcher");
            jobQMock.SetupGet(d => d.MaxParallelism).Returns(1);
            jobQMock.SetupGet(d => d.RetryPolicyFactory).Returns(() => Mock.Of<IRetryPolicy>());
            jobQMock.SetupGet(d => d.IsDisposed).Returns(false);
            jobQMock.Setup(d => d.Start());
            jobQMock.Setup(d => d.Pause());
            jobQMock.Setup(d => d.Enqueue(It.IsAny<IJobRoot>(), It.IsAny<CancellationToken>())).Returns(jobQMock.Object);
            jobQMock.Setup(d => d.EnqueueFifo(It.IsAny<IJobRoot>(), It.IsAny<CancellationToken>())).Returns(jobQMock.Object);

            return jobQMock;
        }

        private class TestPayloadCarrierRoot : IDataJobRoot
        {
            public Guid Id { get; } = Guid.NewGuid();
            public string Name { get; } = "payload-root";
            public bool IsCompleted => false;
            public DateTime ExecutionStart => DateTime.UtcNow;
            public TimeSpan ExecutionTime => TimeSpan.Zero;
            public DateTime ExecutionEnd => DateTime.UtcNow;
            public TaskStatus Status => TaskStatus.Created;
            public IReadOnlyCollection<IJobInfo> Dependencies { get; } = Array.Empty<IJobInfo>();
            public ISerializable PayloadSerializedData { get; } = Mock.Of<ISerializable>();
            public object GetPayload() => new object();
            public Type PayloadType => typeof(object);
            public IReadOnlyList<IDataJob> GetDependentDataJobs() => Array.Empty<IDataJob>();
            public IJob After(params IJob[] previousTasks) => this;
            public IJob[] GetJobsBatch() => Array.Empty<IJob>();
            public IJobInfo[] GetJobInfoDependencies() => Array.Empty<IJobInfo>();
            public void SetRoot(IJobRoot jobRootId) { }
            public IJobInfo CopyInfo() => this;
            public Func<IRetryPolicy> GetRetryPolicyFactory() => () => Mock.Of<IRetryPolicy>();
            public Task WaitUntilFinishedAsync() => Task.CompletedTask;
            public IQ Enqueue(IQ queue, CancellationToken ct) => queue;

            public string Serialize(IUniversalDataSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        private class TestLeaseEntry : ICacheEntry
        {
            public Guid LeaseId { get; } = Guid.NewGuid();
            public Guid JobRootId { get; } = Guid.NewGuid();
            public Guid JobId { get; } = Guid.NewGuid();
            public Guid ParentJobId { get; } = Guid.NewGuid();
            public IJobNodeDto JobNodeDto => Mock.Of<IJobNodeDto>();
            public DateTime CacheUtc { get; } = DateTime.UtcNow;
            public bool IsFifo { get; set; }
            public CancellationToken CancellationToken { get; set; }
            public EntryStatus Status { get; } = EntryStatus.Pending;
            public IRetryPolicyDescriptor RetryDescriptor { get; set; } = Mock.Of<IRetryPolicyDescriptor>();
            public bool IsRoot => true;

            public bool Deleted => throw new NotImplementedException();

            public bool RootSuccessed => throw new NotImplementedException();

            public void MarkLeased() { }
            public void MarkAck(ISerializable payloadData, IUniversalDataSerializer jsonUniversalPayloadSerializer) { }
            public void MarkFailed() { }
            public void MarkCanceled() { }
            public void MarkAsDeleted() { }

            public bool IsFinalized()
            {
                throw new NotImplementedException();
            }

            public void MarkAsRootSuccessed()
            {
                throw new NotImplementedException();
            }
        }
    }
}
