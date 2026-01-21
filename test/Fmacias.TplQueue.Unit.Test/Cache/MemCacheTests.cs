using Fmacias.TplQueue.Cache;
using Fmacias.TplQueue.Contracts;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Cache
{
    [TestFixture]
    public sealed class MemCacheTests
    {
        [Test]
        public void TryLeaseNextRoot_WhenCacheIsEmpty_ReturnsFalse()
        {
            var payloadSerializer = new Mock<IUniversalPayloadSerializer>(MockBehavior.Strict);
            var retrySerializer = new Mock<IRetryPolicySerializable>(MockBehavior.Strict);
            var runnerFactory = new Mock<IPayloadRunnerFactory>(MockBehavior.Strict);

            var cache = MemCache.Create(runnerFactory.Object, payloadSerializer.Object);

            var leased = cache.TryLeaseNextRoot(out var payloadCarrierRoot, out var lease);

            Assert.That(leased, Is.False);
            Assert.That(payloadCarrierRoot, Is.Null);
            Assert.That(lease, Is.Null);
        }

        [Test]
        public void TryLeaseNextRoot_WhenEntriesExist_LeasesRootAndDependencies()
        {
            var payloadSerializer = new Mock<IUniversalPayloadSerializer>(MockBehavior.Strict);
            payloadSerializer
                .Setup(s => s.Serialize(It.IsAny<object?>(), It.IsAny<Type>()))
                .Returns("{}");

            var retrySerializer = new Mock<IRetryPolicySerializable>(MockBehavior.Strict);
            var retryDescriptor = Mock.Of<IRetryPolicyDescriptor>();
            retrySerializer
                .Setup(s => s.ToDescriptor())
                .Returns(retryDescriptor);

            var runnerFactory = new Mock<IPayloadRunnerFactory>(MockBehavior.Strict);

            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();

            var rootCarrier = new Mock<IPayloadCarrierRoot>(MockBehavior.Strict);
            var capturedDependencies = new List<ITaskRunner>();

            rootCarrier.SetupGet(r => r.Id).Returns(rootId);
            rootCarrier
                .Setup(r => r.After(It.IsAny<ITaskRunner[]>()))
                .Callback<ITaskRunner[]>(deps => capturedDependencies.AddRange(deps ?? Array.Empty<ITaskRunner>()))
                .Returns(rootCarrier.Object);

            var childCarrier = new Mock<IPayloadCarrier>(MockBehavior.Strict);
            childCarrier.SetupGet(c => c.Id).Returns(childId);
            childCarrier
                .Setup(c => c.After(It.IsAny<ITaskRunner[]>()))
                .Returns(childCarrier.Object);

            runnerFactory
                .Setup(f => f.LoadRoot(It.Is<ICacheLeaseEntry>(e => e.TaskRunnerId == rootId), payloadSerializer.Object))
                .Returns(rootCarrier.Object);
            runnerFactory
                .Setup(f => f.Load(It.Is<ICacheLeaseEntry>(e => e.TaskRunnerId == childId), payloadSerializer.Object))
                .Returns(childCarrier.Object);

            var (rootRunner, _) = CreateTaskGraph(rootId, childId);

            var cache = MemCache.Create(runnerFactory.Object, payloadSerializer.Object);
            cache.Append(rootRunner.Object, isFifo: true);

            var leased = cache.TryLeaseNextRoot(out var payloadCarrierRoot, out var lease);

            var rootLease = cache.GetByTaskRunnerId(rootId);
            var childLease = cache.GetByTaskRunnerId(childId);

            Assert.Multiple(() =>
            {
                Assert.That(leased, Is.True);
                Assert.That(payloadCarrierRoot, Is.SameAs(rootCarrier.Object));
                Assert.That(lease, Is.SameAs(rootLease));
                Assert.That(rootLease.Status, Is.EqualTo(EntryStatus.Pending));
                Assert.That(childLease.Status, Is.EqualTo(EntryStatus.Pending));
                Assert.That(capturedDependencies.Single(), Is.SameAs(childCarrier.Object));
            });
        }

        [Test]
        public void Clean_RemovesEntriesMarkedAsRemoved_AndMarksFailedEntries()
        {
            var payloadSerializer = new Mock<IUniversalPayloadSerializer>(MockBehavior.Strict);
            payloadSerializer
                .Setup(s => s.Serialize(It.IsAny<object?>(), It.IsAny<Type>()))
                .Returns("{}");

            var retrySerializer = new Mock<IRetryPolicySerializable>(MockBehavior.Strict);
            retrySerializer
                .Setup(s => s.ToDescriptor())
                .Returns(Mock.Of<IRetryPolicyDescriptor>());

            var runnerFactory = new Mock<IPayloadRunnerFactory>(MockBehavior.Strict);

            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var (rootRunner, _) = CreateTaskGraph(rootId, childId);

            var cache = MemCache.Create(runnerFactory.Object, payloadSerializer.Object);
            cache.Append(rootRunner.Object, isFifo: false);

            var rootLease = cache.GetByTaskRunnerId(rootId);
            var childLease = cache.GetByTaskRunnerId(childId);

            rootLease.MarkAsDeleted();
            childLease.MarkFailed();

            cache.CleanDeleted();

            Assert.Multiple(() =>
            {
                Assert.That(cache.GetByTaskRunnerId(rootId), Is.Null);
                Assert.That(childLease.Status, Is.EqualTo(EntryStatus.Failed));
            });
        }

        private static (Mock<IPayloadTaskRunnerRoot<IPayloadCommand>> root, Mock<IPayloadCarrier> child) CreateTaskGraph(
            Guid rootId,
            Guid childId)
        {
            var retryPolicy = new Func<IRetryPolicy>(() => Mock.Of<IRetryPolicy>());
            var childPayload = new DummyPayload();
            var child = new Mock<IPayloadCarrier>(MockBehavior.Strict);
            child.SetupGet(c => c.Id).Returns(childId);
            child.SetupGet(c => c.Name).Returns("child");
            child.SetupGet(c => c.PayloadType).Returns(typeof(DummyPayload));
            child.Setup(c => c.GetPayload()).Returns(childPayload);
            child.Setup(c => c.GetPayloadDependencies()).Returns(Array.Empty<IPayloadCarrier>());
            child.As<ITaskRunner>().Setup(r => r.GetRetryPolicyFactory()).Returns(retryPolicy);

            var rootPayload = new DummyPayload();
            var root = new Mock<IPayloadTaskRunnerRoot<IPayloadCommand>>(MockBehavior.Strict);
            root.SetupGet(r => r.Id).Returns(rootId);
            root.SetupGet(r => r.Name).Returns("root");
            root.As<IPayloadCarrier>().SetupGet(c => c.PayloadType).Returns(typeof(DummyPayload));
            root.As<IPayloadCarrier>().Setup(c => c.GetPayload()).Returns(rootPayload);
            root.As<IPayloadCarrier>().Setup(c => c.GetPayloadDependencies()).Returns(new[] { child.Object });
            root.As<ITaskRunner>().Setup(r => r.GetRetryPolicyFactory()).Returns(retryPolicy);

            return (root, child);
        }

        private sealed class DummyPayload : IPayloadCommand
        {
            public string HandlerId => nameof(DummyPayload);

            public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
        }
    }
}
