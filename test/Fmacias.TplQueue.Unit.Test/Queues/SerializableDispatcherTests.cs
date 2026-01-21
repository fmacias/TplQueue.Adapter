using System;
using System.Threading;
using System.Threading.Tasks;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Queues;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Queues
{
    [TestFixture]
    public class SerializableDispatcherTests
    {
        [Test]
        public void TryLeaseWorkOnce_WhenNoSlots_DoesNotLease()
        {
            var leaseCache = new Mock<IPayloadLeaseCache>(MockBehavior.Strict);
            var dispatcherMock = CreateDispatcherMock(slots: 0);
            var dispatcher = SerializableDispatcher.Create(Mock.Of<ILogger<ISerializablePayloadDispatcher>>(), leaseCache.Object, dispatcherMock.Object);

            var leased = ((SerializableDispatcher)dispatcher).TryLeaseWorkOnce();

            Assert.That(leased, Is.False);
            leaseCache.Verify(c => c.TryLeaseNextRoot(out It.Ref<IPayloadCarrierRoot>.IsAny!, out It.Ref<ICacheLeaseEntry>.IsAny!), Times.Never);
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
            IPayloadCarrierRoot outRoot = runnerRoot;
            ICacheLeaseEntry outLease = lease;

            leaseCache
                .Setup(x => x.TryLeaseNextRoot(out outRoot, out outLease))
                .Returns(true);

            ITaskRunnerRoot? addedRoot = null;
            bool? addedFifo = null;
            CancellationToken? addedToken = null;

            var dispatcherMock = CreateDispatcherMock(slots: 1);

            dispatcherMock
                .Setup(d => d.AddToQueue(It.IsAny<ITaskRunnerRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<ITaskRunnerRoot, bool, CancellationToken>((r, f, t) =>
                {
                    addedRoot = r;
                    addedFifo = f;
                    addedToken = t;
                })
                .Returns(dispatcherMock.Object);

            var dispatcher = SerializableDispatcher.Create(
                Mock.Of<ILogger<ISerializablePayloadDispatcher>>(),
                leaseCache.Object,
                dispatcherMock.Object);

            // Act
            var leased = ((SerializableDispatcher)dispatcher).TryLeaseWorkOnce();

            // Assert
            Assert.That(leased, Is.True);
            Assert.That(addedRoot, Is.SameAs(runnerRoot));
            Assert.That(addedFifo, Is.True);

            dispatcher.Dispose();
        }

        [Test]
        public async Task TaskRunnerEventCallback_AcknowledgesLifecycle()
        {
            var leaseCache = new Mock<IPayloadLeaseCache>();
            var dispatcherMock = CreateDispatcherMock(slots: 1);
            var payloadData = Mock.Of<ISerializedPayload>();
            var runnerInfo = new Mock<ITaskRunnerInfo>();
            var runnerId = Guid.NewGuid();

            runnerInfo.SetupGet(r => r.Id).Returns(runnerId);
            runnerInfo.SetupGet(r => r.PayloadSerializedData).Returns(payloadData);

            var dispatcher = SerializableDispatcher.Create(Mock.Of<ILogger<ISerializablePayloadDispatcher>>(), leaseCache.Object, dispatcherMock.Object);
            var callback = dispatcher.InternalEventDelegator;

            await callback(CreateEvent(TaskRunnerEventStatus.Successed, runnerInfo.Object));
            await callback(CreateEvent(TaskRunnerEventStatus.Failed, runnerInfo.Object));
            await callback(CreateEvent(TaskRunnerEventStatus.Canceled, runnerInfo.Object));
            await callback(CreateEvent(TaskRunnerEventStatus.RootSuccessed, runnerInfo.Object));
            
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
            var logger = Mock.Of<ILogger<ISerializablePayloadDispatcher>>();
            var cache = Mock.Of<IPayloadLeaseCache>();
            var innerDispatcher = Mock.Of<ITaskDispatcher>();

            var dispatcher = SerializableDispatcher.Create(
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

        private static ITaskRunnerEvent CreateEvent(TaskRunnerEventStatus status, ITaskRunnerInfo info)
        {
            var evt = new Mock<ITaskRunnerEvent>();
            evt.SetupGet(e => e.Status).Returns(status);
            evt.SetupGet(e => e.RunnerDTO).Returns(info);
            evt.SetupGet(e => e.Timestamp).Returns(DateTime.UtcNow);
            return evt.Object;
        }

        private static Mock<ITaskDispatcher> CreateDispatcherMock(int slots)
        {
            var dispatcherMock = new Mock<ITaskDispatcher>();
            dispatcherMock.SetupProperty(d => d.InternalEventDelegator);
            dispatcherMock.SetupGet(d => d.Semaphore).Returns(new SemaphoreSlim(slots));
            dispatcherMock.SetupGet(d => d.PulseMs).Returns(10_000);
            dispatcherMock.Setup(d => d.Dispose());
            dispatcherMock.Setup(d => d.Subscribe(It.IsAny<IObserver<ITaskRunnerEvent>>())).Returns(Mock.Of<IDisposable>());
            dispatcherMock.SetupGet(d => d.Name).Returns("dispatcher");
            dispatcherMock.SetupGet(d => d.MaxParallelism).Returns(1);
            dispatcherMock.SetupGet(d => d.RetryPolicyFactory).Returns(() => Mock.Of<IRetryPolicy>());
            dispatcherMock.SetupGet(d => d.IsDisposed).Returns(false);
            dispatcherMock.Setup(d => d.StartPolling());
            dispatcherMock.Setup(d => d.StopPolling());
            dispatcherMock.Setup(d => d.Enqueue(It.IsAny<ITaskRunnerRoot>(), It.IsAny<CancellationToken>())).Returns(dispatcherMock.Object);
            dispatcherMock.Setup(d => d.EnqueueFifo(It.IsAny<ITaskRunnerRoot>(), It.IsAny<CancellationToken>())).Returns(dispatcherMock.Object);

            return dispatcherMock;
        }

        private class TestPayloadCarrierRoot : IPayloadCarrierRoot
        {
            public Guid Id { get; } = Guid.NewGuid();
            public string Name { get; } = "payload-root";
            public bool IsCompleted => false;
            public DateTime ExecutionStart => DateTime.UtcNow;
            public TimeSpan ExecutionTime => TimeSpan.Zero;
            public DateTime ExecutionEnd => DateTime.UtcNow;
            public TaskStatus Status => TaskStatus.Created;
            public IReadOnlyCollection<ITaskRunnerInfo> Dependencies { get; } = Array.Empty<ITaskRunnerInfo>();
            public ISerializedPayload PayloadSerializedData { get; } = Mock.Of<ISerializedPayload>();
            public object GetPayload() => new object();
            public Type PayloadType => typeof(object);
            public IReadOnlyList<IPayloadCarrier> GetPayloadDependencies() => Array.Empty<IPayloadCarrier>();
            public ITaskRunner After(params ITaskRunner[] previousTasks) => this;
            public ITaskRunner[] GetBatch() => Array.Empty<ITaskRunner>();
            public ITaskRunnerInfo[] GetInfoDependencies() => Array.Empty<ITaskRunnerInfo>();
            public void SetRoot(ITaskRunnerRoot taskRunnerRoot) { }
            public ITaskRunnerInfo CopyInfo() => this;
            public Func<IRetryPolicy> GetRetryPolicyFactory() => () => Mock.Of<IRetryPolicy>();
            public Task WaitUntilFinishedAsync() => Task.CompletedTask;
            public ITaskDispatcher Enqueue(ITaskDispatcher queue, CancellationToken ct) => queue;
        }

        private class TestLeaseEntry : ICacheLeaseEntry
        {
            public Guid LeaseId { get; } = Guid.NewGuid();
            public Guid TaskRunnerRootId { get; } = Guid.NewGuid();
            public Guid TaskRunnerId { get; } = Guid.NewGuid();
            public Guid ParentTaskRunnerId { get; } = Guid.NewGuid();
            public ITaskRunnerNodeDto TaskRunnerNodeDto => Mock.Of<ITaskRunnerNodeDto>();
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
