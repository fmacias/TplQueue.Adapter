using System;
using System.Linq;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Cache.Abstract.Test
{
    [TestFixture]
    public sealed class CacheAbstractTests
    {
        [Test]
        public void Dehydrate_NullRoot_ThrowsArgumentNullException()
        {
            // Arrange
            var cache = CreateCache();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                cache.Dehydrate<IPayload>(null!, isFifo: false));
        }

        [Test]
        public void Dehydrate_ValidRoot_ExtractsAtLeastTheRootNode()
        {
            // Arrange
            var cache = CreateCache();
            var root = CreateRoot();

            // Act
            var nodes = cache.Dehydrate(root.Object, isFifo: false);

            // Assert
            Assert.That(nodes, Is.Not.Null);
            Assert.That(nodes.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(cache.AppendedNodes.Count, Is.EqualTo(nodes.Count));
        }

        [Test]
        public void AckNode_EmptyJobId_ThrowsArgumentException()
        {
            // Arrange
            var cache = CreateCache();
            var payload = Mock.Of<ISerializable>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => cache.AckNode(Guid.Empty, payload));
        }

        [Test]
        public void AckNode_NullPayload_ThrowsArgumentNullException()
        {
            // Arrange
            var cache = CreateCache();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache.AckNode(Guid.NewGuid(), null!));
        }

        [Test]
        public void FailNode_EmptyJobId_ThrowsArgumentException()
        {
            // Arrange
            var cache = CreateCache();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => cache.FailNode(Guid.Empty, "boom"));
        }

        [Test]
        public void CancelNode_EmptyJobId_ThrowsArgumentException()
        {
            // Arrange
            var cache = CreateCache();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => cache.CancelNode(Guid.Empty));
        }

        [Test]
        public void SuccessRootNode_EmptyRootId_ThrowsArgumentException()
        {
            // Arrange
            var cache = CreateCache();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => cache.SuccessRootNode(Guid.Empty));
        }

        [Test]
        public void DeleteRootNode_EmptyRootId_ThrowsArgumentException()
        {
            // Arrange
            var cache = CreateCache();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => cache.DeleteRootNode(Guid.Empty));
        }

        [Test]
        public void GetByJobId_EmptyJobId_ThrowsArgumentException()
        {
            // Arrange
            var cache = CreateCache();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => cache.GetByJobId(Guid.Empty));
        }

        [Test]
        public void TryHydrateNextJob_UsesPersistedPayloadHandlerKey()
        {
            var rootJobId = Guid.NewGuid();
            var payload = new DummyPayload("payload/fallback");
            const string persistedPayloadHandlerKey = "payload/persisted";
            var expectedHandler = Mock.Of<IHandler>();
            var hydratedRoot = Mock.Of<IDataJobRoot<IPayload>>();
            var serializer = new Mock<IUniversalDataSerializer>(MockBehavior.Strict);
            serializer
                .Setup(s => s.Deserialize("{}", typeof(DummyPayload)))
                .Returns(payload);

            var typeResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            typeResolver
                .Setup(r => r.Resolve(typeof(DummyPayload).AssemblyQualifiedName!))
                .Returns(typeof(DummyPayload));

            var retryPolicyFactory = new Mock<IRetryPolicyAbstractFactory>(MockBehavior.Strict);
            retryPolicyFactory
                .Setup(f => f.PolicyByOptions(It.IsAny<IRetryPolicyOptions>()))
                .Returns(Mock.Of<IRetryPolicy>());

            var payloadHandlers = new Mock<IPayloadHandlers>(MockBehavior.Strict);
            payloadHandlers
                .Setup(h => h.Handler(persistedPayloadHandlerKey))
                .Returns(expectedHandler);

            var payloadJobFactory = new Mock<IDataJobFactory>(MockBehavior.Strict);
            payloadJobFactory
                .Setup(f => f.DataJobRoot<IPayload>(
                    rootJobId,
                    It.Is<IPayload>(candidate => ReferenceEquals(candidate, payload)),
                    expectedHandler,
                    "root",
                    It.IsAny<Func<IRetryPolicy>>()))
                .Returns(hydratedRoot);

            var cacheRepository = new Mock<ICacheRepository>(MockBehavior.Strict);
            var rootLease = CreateLeaseEntry(CreateJobNodeRecord(rootJobId, persistedPayloadHandlerKey).Object, rootJobId);
            cacheRepository
                .Setup(r => r.SelectOldestPendingRoot())
                .Returns(rootLease.Object);
            cacheRepository
                .Setup(r => r.SelectPendingChildren(rootJobId))
                .Returns(Array.Empty<ICacheEntry>().OrderBy(entry => entry.CacheUtc));

            var cache = new FakeCache(
                serializer.Object,
                cacheRepository.Object,
                typeResolver.Object,
                payloadJobFactory.Object,
                Mock.Of<ICacheEntryFactory>(),
                payloadHandlers.Object,
                retryPolicyFactory.Object);

            var leased = cache.TryHydrateNextJob(out var payloadJobRoot, out var lease);

            Assert.Multiple(() =>
            {
                Assert.That(leased, Is.True);
                Assert.That(payloadJobRoot, Is.SameAs(hydratedRoot));
                Assert.That(lease, Is.SameAs(rootLease.Object));
            });
            payloadHandlers.Verify(h => h.Handler(persistedPayloadHandlerKey), Times.Once);
            payloadHandlers.Verify(h => h.Handler(payload.PayloadId), Times.Never);
        }

        [Test]
        public void TryHydrateNextJob_FallsBackToPayloadIdWhenPersistedKeyIsMissing()
        {
            var rootJobId = Guid.NewGuid();
            var payload = new DummyPayload("payload/legacy");
            var expectedHandler = Mock.Of<IHandler>();
            var hydratedRoot = Mock.Of<IDataJobRoot<IPayload>>();
            var serializer = new Mock<IUniversalDataSerializer>(MockBehavior.Strict);
            serializer
                .Setup(s => s.Deserialize("{}", typeof(DummyPayload)))
                .Returns(payload);

            var typeResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            typeResolver
                .Setup(r => r.Resolve(typeof(DummyPayload).AssemblyQualifiedName!))
                .Returns(typeof(DummyPayload));

            var retryPolicyFactory = new Mock<IRetryPolicyAbstractFactory>(MockBehavior.Strict);
            retryPolicyFactory
                .Setup(f => f.PolicyByOptions(It.IsAny<IRetryPolicyOptions>()))
                .Returns(Mock.Of<IRetryPolicy>());

            var payloadHandlers = new Mock<IPayloadHandlers>(MockBehavior.Strict);
            payloadHandlers
                .Setup(h => h.Handler(payload.PayloadId))
                .Returns(expectedHandler);

            var payloadJobFactory = new Mock<IDataJobFactory>(MockBehavior.Strict);
            payloadJobFactory
                .Setup(f => f.DataJobRoot<IPayload>(
                    rootJobId,
                    It.Is<IPayload>(candidate => ReferenceEquals(candidate, payload)),
                    expectedHandler,
                    "root",
                    It.IsAny<Func<IRetryPolicy>>()))
                .Returns(hydratedRoot);

            var cacheRepository = new Mock<ICacheRepository>(MockBehavior.Strict);
            var rootLease = CreateLeaseEntry(CreateJobNodeRecord(rootJobId, string.Empty).Object, rootJobId);
            cacheRepository
                .Setup(r => r.SelectOldestPendingRoot())
                .Returns(rootLease.Object);
            cacheRepository
                .Setup(r => r.SelectPendingChildren(rootJobId))
                .Returns(Array.Empty<ICacheEntry>().OrderBy(entry => entry.CacheUtc));

            var cache = new FakeCache(
                serializer.Object,
                cacheRepository.Object,
                typeResolver.Object,
                payloadJobFactory.Object,
                Mock.Of<ICacheEntryFactory>(),
                payloadHandlers.Object,
                retryPolicyFactory.Object);

            var leased = cache.TryHydrateNextJob(out var payloadJobRoot, out var lease);

            Assert.Multiple(() =>
            {
                Assert.That(leased, Is.True);
                Assert.That(payloadJobRoot, Is.SameAs(hydratedRoot));
                Assert.That(lease, Is.SameAs(rootLease.Object));
            });
            payloadHandlers.Verify(h => h.Handler(payload.PayloadId), Times.Once);
        }

        private static FakeCache CreateCache()
        {
            return new FakeCache(
                Mock.Of<IUniversalDataSerializer>(),
                Mock.Of<ICacheRepository>(),
                Mock.Of<ITypeResolver>(),
                Mock.Of<IDataJobFactory>(),
                Mock.Of<ICacheEntryFactory>(),
                Mock.Of<IPayloadHandlers>(),
                Mock.Of<IRetryPolicyAbstractFactory>());
        }

        private static Mock<IDataJobRoot<IPayload>> CreateRoot()
        {
            var payload = new DummyPayload();
            var root = new Mock<IDataJobRoot<IPayload>>(MockBehavior.Loose);
            root.SetupGet(r => r.Id).Returns(Guid.NewGuid());
            root.SetupGet(r => r.Name).Returns("root");
            root.As<IDataJobInfo>().SetupGet(c => c.PayloadHandlerKey).Returns(payload.PayloadId);
            root.As<IDataJobNode>().Setup(c => c.GetDependentDataJobs()).Returns(Array.Empty<IDataJob>());
            root.As<IDataJobNode>().Setup(c => c.GetPayload()).Returns(payload);
            root.As<ISerializable>()
                .Setup(s => s.Serialize(It.IsAny<IUniversalDataSerializer>()))
                .Returns("{}");
            root.Setup(c => c.GetRetryPolicyFactory()).Returns(() => NoRetryPolicy.Create());
            return root;
        }

        private static Mock<IJobNodeRecord> CreateJobNodeRecord(Guid jobId, string payloadHandlerKey)
        {
            var jobNodeRecord = new Mock<IJobNodeRecord>(MockBehavior.Strict);
            jobNodeRecord.SetupGet(r => r.JobId).Returns(jobId);
            jobNodeRecord.SetupGet(r => r.ParentJobId).Returns(Guid.Empty);
            jobNodeRecord.SetupGet(r => r.PayloadHandlerKey).Returns(payloadHandlerKey);
            jobNodeRecord.SetupGet(r => r.Name).Returns("root");
            jobNodeRecord.SetupGet(r => r.NodeCreationUtc).Returns(DateTime.UtcNow);
            jobNodeRecord.SetupGet(r => r.IsRoot).Returns(true);
            jobNodeRecord.SetupGet(r => r.IsFifo).Returns(false);
            jobNodeRecord.SetupGet(r => r.PayloadTypeName).Returns(typeof(DummyPayload).AssemblyQualifiedName!);
            jobNodeRecord.SetupGet(r => r.PayloadJson).Returns("{}");
            jobNodeRecord.SetupGet(r => r.RetryPolicyOptions).Returns(Mock.Of<IRetryPolicyOptions>());

            return jobNodeRecord;
        }

        private static Mock<ICacheEntry> CreateLeaseEntry(IJobNodeRecord jobNodeRecord, Guid rootJobId)
        {
            var leaseEntry = new Mock<ICacheEntry>(MockBehavior.Strict);
            leaseEntry.SetupGet(l => l.LeaseId).Returns(Guid.NewGuid());
            leaseEntry.SetupGet(l => l.JobRootId).Returns(rootJobId);
            leaseEntry.SetupGet(l => l.JobId).Returns(jobNodeRecord.JobId);
            leaseEntry.SetupGet(l => l.ParentJobId).Returns(jobNodeRecord.ParentJobId);
            leaseEntry.SetupGet(l => l.JobNodeRecordDto).Returns(jobNodeRecord);
            leaseEntry.SetupGet(l => l.CacheUtc).Returns(DateTime.UtcNow);

            return leaseEntry;
        }

        private sealed class DummyPayload : IPayload
        {
            public DummyPayload(string payloadId = "cache-abstract/root")
            {
                PayloadId = payloadId;
            }

            public string PayloadId { get; } = "cache-abstract/root";
            public DateTime CollectionTime { get; } = DateTime.UtcNow;
        }
    }
}
