using System;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Cache.Abstract.Test;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Cache.Abstract.Test
{
    [TestFixture]
    public sealed class CacheAbstractTests
    {
        [Test]
        public void Append_NullRoot_ThrowsArgumentNullException()
        {
            // Arrange
            var retrySerializer = new Mock<IRetryPolicySerializable>(MockBehavior.Loose);
            var payloadSerializer = new Mock<IUniversalPayloadSerializer>(MockBehavior.Loose);

            var cache = new FakeCache(
                retrySerializer.Object,
                payloadSerializer.Object,
                Guid.Empty,
                knownEntry: null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                cache.Append<IPayloadCommand>(null!, isFifo: false));
        }

        [Test]
        public void Append_WhenEntryNotFound_DoesNotPersist()
        {
            // Arrange
            var retrySerializer = new Mock<IRetryPolicySerializable>();
            var payloadSerializer = new Mock<IUniversalPayloadSerializer>();

            payloadSerializer
                .Setup(s => s.Serialize(It.IsAny<object>(), It.IsAny<Type>()))
                .Returns("{}");

            var cache = new FakeCache(
                retrySerializer.Object,
                payloadSerializer.Object,
                knownRootId: Guid.NewGuid(),
                knownEntry: null);

            var root = new Mock<IPayloadJobRoot<IPayloadCommand>>(MockBehavior.Loose);
            root.SetupGet(r => r.Id).Returns(Guid.NewGuid());
            root.SetupGet(r => r.Name).Returns("root");

            var rootAsCarrier = root.As<IPayloadCarrierJob>();
            rootAsCarrier.Setup(c => c.GetPayloadDependencies()).Returns(Array.Empty<IPayloadCarrierJob>());
            rootAsCarrier.SetupGet(c => c.PayloadType).Returns(typeof(string));
            rootAsCarrier.Setup(c => c.GetPayload()).Returns("payload-root");
            Func<IRetryPolicy> retryPolicyFactory = () => Mock.Of<IRetryPolicy>();
            rootAsCarrier.Setup(c => c.GetRetryPolicyFactory()).Returns(retryPolicyFactory);

            // Act
            cache.Append(root.Object, isFifo: false);

            // Assert
            Assert.That(cache.PersistedEntries, Is.Empty);
        }

        [Test]
        public void Append_WhenEntryDoesNotExist_DoesNotThrow()
        {
            // Arrange
            var retrySerializer = new Mock<IRetryPolicySerializable>(MockBehavior.Loose);
            var payloadSerializer = new Mock<IUniversalPayloadSerializer>(MockBehavior.Loose);

            payloadSerializer
                .Setup(s => s.Serialize(It.IsAny<object>(), It.IsAny<Type>()))
                .Returns("{}");

            var rootId = Guid.NewGuid();

            var cache = new FakeCache(
                retrySerializer.Object,
                payloadSerializer.Object,
                knownRootId: Guid.NewGuid(), // different, so no entry found
                knownEntry: null);

            var root = new Mock<IPayloadJobRoot<IPayloadCommand>>(MockBehavior.Loose);
            root.SetupGet(r => r.Id).Returns(rootId);
            root.SetupGet(r => r.Name).Returns("root");

            var rootAsCarrier = root.As<IPayloadCarrierJob>();
            rootAsCarrier.Setup(c => c.GetPayloadDependencies()).Returns(Array.Empty<IPayloadCarrierJob>());
            rootAsCarrier.SetupGet(c => c.PayloadType).Returns(typeof(string));
            rootAsCarrier.Setup(c => c.GetPayload()).Returns("payload-root");
            Func<IRetryPolicy> retryPolicyFactory = () => Mock.Of<IRetryPolicy>();
            rootAsCarrier.Setup(c => c.GetRetryPolicyFactory()).Returns(retryPolicyFactory);
            // Act & Assert
            Assert.DoesNotThrow(() =>
                cache.Append(root.Object, isFifo: false));
        }
    }
}
