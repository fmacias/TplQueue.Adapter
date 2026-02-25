using System;
using Fmacias.TplQueue.Cache.Contracts;
using Fmacias.TplQueue.Contracts;
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

        private static FakeCache CreateCache()
        {
            return new FakeCache(
                Mock.Of<IUniversalDataSerializer>(),
                Mock.Of<ICacheRepository>(),
                Mock.Of<INodeTypeResolver>(),
                Mock.Of<IDataJobFactory>(),
                Mock.Of<ICacheEntryFactory>());
        }

        private static Mock<IDataJobRoot<IPayload>> CreateRoot()
        {
            var root = new Mock<IDataJobRoot<IPayload>>(MockBehavior.Loose);
            root.SetupGet(r => r.Id).Returns(Guid.NewGuid());
            root.SetupGet(r => r.Name).Returns("root");
            root.Setup(c => c.GetDependentDataJobs()).Returns(Array.Empty<IDataJob>());
            root.Setup(c => c.GetPayload()).Returns("payload-root");
            root.As<ISerializable>()
                .Setup(s => s.Serialize(It.IsAny<IUniversalDataSerializer>()))
                .Returns("{}");
            root.Setup(c => c.GetRetryPolicyFactory()).Returns(() => Mock.Of<IRetryPolicy>());
            return root;
        }
    }
}
