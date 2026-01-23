using Fmacias.TplQueue.Contracts;
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
            var leaseCache = new Mock<IPayloadLeaseCache>(MockBehavior.Strict);
            var dispatcherMock = CreateDispatcherMock(slots: 0);
            var dispatcher = CacheableQ.Create(Mock.Of<ILogger<ICacheablePayloadQ>>(), leaseCache.Object, dispatcherMock.Object);

            var leased = ((CacheableQ)dispatcher).TryLeaseWorkOnce();

            Assert.That(leased, Is.False);
            leaseCache.Verify(c => c.TryLeaseNextRoot(out It.Ref<IPayloadJobRoot>.IsAny!, out It.Ref<ICacheLeaseEntry>.IsAny!), Times.Never);
            dispatcher.Dispose();
        }

        [Test]
        public void TryLeaseWorkOnce_WhenLeaseAvailable_AddsToQueue()
        {
            // Arrange
            var leaseCache = new Mock<IPayloadLeaseCache>();

            var lease = new TestLeaseEntry { IsFifo = true, CancellationToken = new CancellationTokenSource().Token };
            var runnerRoot = new TestPayloadCarrierRoot();

            // IMPORTANT: provide actual out vars
            IPayloadJobRoot outRoot = runnerRoot;
            ICacheLeaseEntry outLease = lease;

            leaseCache
                .Setup(x => x.TryLeaseNextRoot(out outRoot, out outLease))
                .Returns(true);

            IJobRoot? addedRoot = null;
            bool? addedFifo = null;
            CancellationToken? addedToken = null;

            var dispatcherMock = CreateDispatcherMock(slots: 1);

            dispatcherMock
                .Setup(d => d.Enqueue(It.IsAny<IJobRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<IJobRoot, bool, CancellationToken>((r, f, t) =>
                {
                    addedRoot = r;
                    addedFifo = f;
                    addedToken = t;
                })
                .Returns(dispatcherMock.Object);

            var dispatcher = CacheableQ.Create(
                Mock.Of<ILogger<ICacheablePayloadQ>>(),
                leaseCache.Object,
                dispatcherMock.Object);

            // Act
            var leased = ((CacheableQ)dispatcher).TryLeaseWorkOnce();

            // Assert
            Assert.That(leased, Is.True);
            Assert.That(addedRoot, Is.SameAs(runnerRoot));
            Assert.That(addedFifo, Is.True);

            dispatcher.Dispose();
        }

        [Test]
        public async Task JobEventCallback_AcknowledgesLifecycle()
        {
            var leaseCache = new Mock<IPayloadLeaseCache>();
            var dispatcherMock = CreateDispatcherMock(slots: 1);
            var payloadData = Mock.Of<ISerializedPayload>();
            var runnerInfo = new Mock<IJobInfo>();
            var runnerId = Guid.NewGuid();

            runnerInfo.SetupGet(r => r.Id).Returns(runnerId);
            runnerInfo.SetupGet(r => r.PayloadSerializedData).Returns(payloadData);

            var dispatcher = CacheableQ.Create(Mock.Of<ILogger<ICacheablePayloadQ>>(), leaseCache.Object, dispatcherMock.Object);
            var callback = dispatcher.OnEventChange;

            await callback(CreateEvent(JobEventStatus.Successed, runnerInfo.Object));
            await callback(CreateEvent(JobEventStatus.Failed, runnerInfo.Object));
            await callback(CreateEvent(JobEventStatus.Canceled, runnerInfo.Object));
            await callback(CreateEvent(JobEventStatus.RootSuccessed, runnerInfo.Object));
            
            leaseCache.Verify(c => c.AckNode(runnerId, payloadData), Times.Once);
            leaseCache.Verify(c => c.FailNode(runnerId, It.IsAny<string?>()), Times.Once);
            leaseCache.Verify(c => c.CancelNode(runnerId), Times.Once);
            leaseCache.Verify(c => c.SuccessRootNode(runnerId), Times.Once);
            leaseCache.Verify(c => c.DeleteRootNode(runnerId), Times.Exactly(4));

            dispatcher.Dispose();
        }
        [Test]
        public void LeasingPulseMs_NonPositiveValue_ResetsToDefault()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ICacheablePayloadQ>>();
            var cache = Mock.Of<IPayloadLeaseCache>();
            var innerDispatcher = Mock.Of<IJobQ>();

            var dispatcher = CacheableQ.Create(
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
            evt.SetupGet(e => e.JobDTO).Returns(info);
            evt.SetupGet(e => e.Timestamp).Returns(DateTime.UtcNow);
            return evt.Object;
        }

        private static Mock<IJobQ> CreateDispatcherMock(int slots)
        {
            var jobQMock = new Mock<IJobQ>();
            jobQMock.SetupProperty(d => d.OnEventChange);
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

        private class TestPayloadCarrierRoot : IPayloadJobRoot
        {
            public Guid Id { get; } = Guid.NewGuid();
            public string Name { get; } = "payload-root";
            public bool IsCompleted => false;
            public DateTime ExecutionStart => DateTime.UtcNow;
            public TimeSpan ExecutionTime => TimeSpan.Zero;
            public DateTime ExecutionEnd => DateTime.UtcNow;
            public TaskStatus Status => TaskStatus.Created;
            public IReadOnlyCollection<IJobInfo> Dependencies { get; } = Array.Empty<IJobInfo>();
            public ISerializedPayload PayloadSerializedData { get; } = Mock.Of<ISerializedPayload>();
            public object GetPayload() => new object();
            public Type PayloadType => typeof(object);
            public IReadOnlyList<IPayloadCarrierJob> GetPayloadDependencies() => Array.Empty<IPayloadCarrierJob>();
            public IJob After(params IJob[] previousTasks) => this;
            public IJob[] GetJobsBatch() => Array.Empty<IJob>();
            public IJobInfo[] GetJobInfoDependencies() => Array.Empty<IJobInfo>();
            public void SetRoot(IJobRoot jobRootId) { }
            public IJobInfo CopyInfo() => this;
            public Func<IRetryPolicy> GetRetryPolicyFactory() => () => Mock.Of<IRetryPolicy>();
            public Task WaitUntilFinishedAsync() => Task.CompletedTask;
            public IJobQ Enqueue(IJobQ queue, CancellationToken ct) => queue;
        }

        private class TestLeaseEntry : ICacheLeaseEntry
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
            public void MarkAck(ISerializedPayload payloadData) { }
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
