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
            var runnerFactory = new Mock<IPayloadJobFactory>(MockBehavior.Strict);

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

            var runnerFactory = new Mock<IPayloadJobFactory>(MockBehavior.Strict);

            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();

            var rootCarrier = new Mock<IPayloadJobRoot>(MockBehavior.Strict);
            var capturedDependencies = new List<IJob>();

            rootCarrier.SetupGet(r => r.Id).Returns(rootId);
            rootCarrier
                .Setup(r => r.After(It.IsAny<IJob[]>()))
                .Callback<IJob[]>(deps => capturedDependencies.AddRange(deps ?? Array.Empty<IJob>()))
                .Returns(rootCarrier.Object);

            var childCarrier = new Mock<IPayloadCarrierJob>(MockBehavior.Strict);
            childCarrier.SetupGet(c => c.Id).Returns(childId);
            childCarrier
                .Setup(c => c.After(It.IsAny<IJob[]>()))
                .Returns(childCarrier.Object);

            runnerFactory
                .Setup(f => f.LoadRoot(It.Is<ICacheLeaseEntry>(e => e.JobId == rootId), payloadSerializer.Object))
                .Returns(rootCarrier.Object);
            runnerFactory
                .Setup(f => f.Load(It.Is<ICacheLeaseEntry>(e => e.JobId == childId), payloadSerializer.Object))
                .Returns(childCarrier.Object);

            var (rootRunner, _) = CreateTaskGraph(rootId, childId);

            var cache = MemCache.Create(runnerFactory.Object, payloadSerializer.Object);
            cache.Append(rootRunner.Object, isFifo: true);

            var leased = cache.TryLeaseNextRoot(out var payloadCarrierRoot, out var lease);

            var rootLease = cache.GetByJobId(rootId);
            var childLease = cache.GetByJobId(childId);

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

            var runnerFactory = new Mock<IPayloadJobFactory>(MockBehavior.Strict);

            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var (rootRunner, _) = CreateTaskGraph(rootId, childId);

            var cache = MemCache.Create(runnerFactory.Object, payloadSerializer.Object);
            cache.Append(rootRunner.Object, isFifo: false);

            var rootLease = cache.GetByJobId(rootId);
            var childLease = cache.GetByJobId(childId);

            rootLease.MarkAsDeleted();
            childLease.MarkFailed();

            cache.CleanDeleted();

            Assert.Multiple(() =>
            {
                Assert.That(cache.GetByJobId(rootId), Is.Null);
                Assert.That(childLease.Status, Is.EqualTo(EntryStatus.Failed));
            });
        }

        private static (Mock<IPayloadJobRoot<IPayloadCommand>> root, Mock<IPayloadCarrierJob> child) CreateTaskGraph(
            Guid rootId,
            Guid childId)
        {
            var retryPolicy = new Func<IRetryPolicy>(() => Mock.Of<IRetryPolicy>());
            var childPayload = new DummyPayload();
            var child = new Mock<IPayloadCarrierJob>(MockBehavior.Strict);
            child.SetupGet(c => c.Id).Returns(childId);
            child.SetupGet(c => c.Name).Returns("child");
            child.SetupGet(c => c.PayloadType).Returns(typeof(DummyPayload));
            child.Setup(c => c.GetPayload()).Returns(childPayload);
            child.Setup(c => c.GetPayloadDependencies()).Returns(Array.Empty<IPayloadCarrierJob>());
            child.As<IJob>().Setup(r => r.GetRetryPolicyFactory()).Returns(retryPolicy);

            var rootPayload = new DummyPayload();
            var root = new Mock<IPayloadJobRoot<IPayloadCommand>>(MockBehavior.Strict);
            root.SetupGet(r => r.Id).Returns(rootId);
            root.SetupGet(r => r.Name).Returns("root");
            root.As<IPayloadCarrierJob>().SetupGet(c => c.PayloadType).Returns(typeof(DummyPayload));
            root.As<IPayloadCarrierJob>().Setup(c => c.GetPayload()).Returns(rootPayload);
            root.As<IPayloadCarrierJob>().Setup(c => c.GetPayloadDependencies()).Returns(new[] { child.Object });
            root.As<IJob>().Setup(r => r.GetRetryPolicyFactory()).Returns(retryPolicy);

            return (root, child);
        }

        private sealed class DummyPayload : IPayloadCommand
        {
            public string HandlerId => nameof(DummyPayload);

            public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
        }
    }
}
