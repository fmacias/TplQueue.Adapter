using Fmacias.TplQueue.Cache;
using Fmacias.TplQueue.Cache.Factories;
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
            var payloadSerializer = MockFactory.CreatePayloadSerializerMock(MockBehavior.Strict);

            var cache = MemCache.Create(
                payloadSerializer.Object,
                Mock.Of<INodeTypeResolver>(),
                Mock.Of<IPayloadJobFactory>(),
                CacheEntryFactory.Create());

            var leased = cache.TryHydrateNextJob(out var payloadCarrierRoot, out var lease);

            Assert.That(leased, Is.False);
            Assert.That(payloadCarrierRoot, Is.Null);
            Assert.That(lease, Is.Null);
        }

        [Test]
        public void TryLeaseNextRoot_WhenEntriesExist_LeasesRootAndDependencies()
        {
            var payloadSerializer = MockFactory.CreatePayloadSerializerMock(MockBehavior.Strict);
            payloadSerializer.Setup(s => s.Deserialize(It.IsAny<string>(), It.IsAny<Type>()))
                .Returns(new DummyPayload());

            var payloadJobFactory = new Mock<IPayloadJobFactory>(MockBehavior.Strict);

            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();

            var payloadJobRoot = new Mock<IPayloadJobRoot<IPayload>>(MockBehavior.Strict);
            var capturedDependencies = new List<IJob>();

            payloadJobRoot.SetupGet(r => r.Id).Returns(rootId);
            payloadJobRoot
                .Setup(r => r.After(It.IsAny<IJob[]>()))
                .Callback<IJob[]>(deps => capturedDependencies.AddRange(deps ?? Array.Empty<IJob>()))
                .Returns(payloadJobRoot.Object);

            var payloadJobChild = new Mock<IPayloadJob<IPayload>>(MockBehavior.Strict);
            payloadJobChild.SetupGet(c => c.Id).Returns(childId);
            payloadJobChild
                .Setup(c => c.After(It.IsAny<IJob[]>()))
                .Returns(payloadJobChild.Object);
            payloadJobFactory
                .Setup(f => f.CreateJobRoot<IPayload>(
                    It.IsAny<IPayload>(),
                    It.IsAny<string>()))
                .Returns(payloadJobRoot.Object);
            payloadJobFactory
                .Setup(f => f.CreateJobRoot<IPayload>(
                    It.IsAny<Guid>(),
                    It.IsAny<IPayload>(),
                    It.IsAny<string>()))
                .Returns(payloadJobRoot.Object);
            payloadJobFactory
                .Setup(f => f.CreateJob<IPayload>(
                    It.IsAny<IPayload>(),
                    It.IsAny<string>()))
                .Returns(payloadJobChild.Object);
            payloadJobFactory
                .Setup(f => f.CreateJob<IPayload>(
                    It.IsAny<Guid>(),
                    It.IsAny<IPayload>(),
                    It.IsAny<string>()))
                .Returns(payloadJobChild.Object);
            payloadJobFactory
                .Setup(f => f.CreateJob(
                    It.IsAny<IJobNodeDto>(),
                    It.IsAny<IPayload>()))
                .Returns(payloadJobChild.Object);
            payloadJobFactory
                .Setup(f => f.CreateJobRoot(
                    It.IsAny<IJobNodeDto>(),
                    It.IsAny<IPayload>()))
                .Returns(payloadJobRoot.Object);

            var (rootRunner, _) = CreateTaskGraph(rootId, childId);

            var cache = MemCache.Create(
                payloadSerializer.Object,
                Mock.Of<INodeTypeResolver>(),
                payloadJobFactory.Object,
                CacheEntryFactory.Create());

            cache.Dehydrate(rootRunner.Object, isFifo: true);

            var leased = cache.TryHydrateNextJob(out var payloadCarrierRoot, out var lease);

            var rootLease = cache.GetByJobId(rootId);
            var childLease = cache.GetByJobId(childId);

            Assert.Multiple(() =>
            {
                Assert.That(leased, Is.True);
                Assert.That(payloadCarrierRoot, Is.SameAs(payloadJobRoot.Object));
                Assert.That(lease, Is.SameAs(rootLease));
                Assert.That(rootLease.Status, Is.EqualTo(EntryStatus.Pending));
                Assert.That(childLease.Status, Is.EqualTo(EntryStatus.Pending));
                Assert.That(rootLease.JobNodeDto.PayloadTypeName, Does.Contain(nameof(DummyPayload)));
                Assert.That(capturedDependencies.Single(), Is.SameAs(payloadJobChild.Object));
            });
        }

        [Test]
        public void Clean_RemovesEntriesMarkedAsRemoved_AndMarksFailedEntries()
        {
            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var (payloadJobRoot, _) = CreateTaskGraph(rootId, childId);

            var cache = MemCache.Create(
                Mock.Of<IUniversalPayloadSerializer>(),
                Mock.Of<INodeTypeResolver>(),
                Mock.Of<IPayloadJobFactory>(),
                CacheEntryFactory.Create());

            cache.Dehydrate(payloadJobRoot.Object, isFifo: false);

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

        private static (Mock<IPayloadJobRoot<IPayload>> root, Mock<IPayloadCarrierJob> child) CreateTaskGraph(
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
            child.As<ISerializable>().Setup(r => r.Serialize(It.IsAny<IUniversalPayloadSerializer>()))
                .Returns("{}");

            var rootPayload = new DummyPayload();
            var root = new Mock<IPayloadJobRoot<IPayload>>(MockBehavior.Strict);
            root.SetupGet(r => r.Id).Returns(rootId);
            root.SetupGet(r => r.Name).Returns("root");
            root.As<IPayloadCarrierJob>().SetupGet(c => c.PayloadType).Returns(typeof(DummyPayload));
            root.As<IPayloadCarrierJob>().Setup(c => c.GetPayload()).Returns(rootPayload);
            root.As<IPayloadCarrierJob>().Setup(c => c.GetPayloadDependencies()).Returns(new[] { child.Object });
            root.As<IJob>().Setup(r => r.GetRetryPolicyFactory()).Returns(retryPolicy);
            root.As<ISerializable>().Setup(r => r.Serialize(It.IsAny<IUniversalPayloadSerializer>()))
                .Returns("{}");
            return (root, child);
        }

        private sealed class DummyPayload : IPayload
        {
            public string PayloadId => nameof(DummyPayload);

            public DateTime CollectionTime => DateTime.UtcNow;

            public Guid HandlerId => Guid.NewGuid();
        }

        private static class MockFactory
        {
            public static Mock<IUniversalPayloadSerializer> CreatePayloadSerializerMock(MockBehavior behavior)
            {
                var serializer = new Mock<IUniversalPayloadSerializer>(behavior);
                serializer.Setup(s => s.Serialize(It.IsAny<object?>(), It.IsAny<Type>())).Returns("{}");
                return serializer;
            }
        }
    }
}
