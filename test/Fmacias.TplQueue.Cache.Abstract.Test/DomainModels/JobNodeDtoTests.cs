using System;
using Fmacias.TplQueue.Cache.Abstract.Models;
using Fmacias.TplQueue.Contracts;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Cache.Abstract.Test.DomainModels
{
    [TestFixture]
    public sealed class JobNodeDtoTests
    {
        [Test]
        public void Create_WhenRetryPolicyFactoryIsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            var serializer = new Mock<IUniversalDataSerializer>(MockBehavior.Loose);
            var carrier = BuildCarrierJob();
            carrier.Setup(c => c.GetRetryPolicyFactory()).Returns((Func<IRetryPolicy>)null!);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                JobNodeDto.Create(serializer.Object, carrier.Object, isFifo: false, parentJob: null));
        }

        [Test]
        public void Create_WhenRetryPolicyFactoryReturnsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            var serializer = new Mock<IUniversalDataSerializer>(MockBehavior.Loose);
            var carrier = BuildCarrierJob();
            carrier.Setup(c => c.GetRetryPolicyFactory()).Returns(new Func<IRetryPolicy>(() => null!));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                JobNodeDto.Create(serializer.Object, carrier.Object, isFifo: false, parentJob: null));
        }

        [Test]
        public void UpdatePayloadJson_WhenValueIsWhitespace_ThrowsArgumentException()
        {
            // Arrange
            var dto = CreateNodeDto();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => dto.UpdatePayloadJson(" "));
        }

        [Test]
        public void Deserialize_WhenSerializerIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            var dto = CreateNodeDto();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => dto.Deserialize(null!));
            Assert.Throws<ArgumentNullException>(() => dto.Deserialize<object>(null!));
        }

        private static JobNodeDto CreateNodeDto()
        {
            var serializer = new Mock<IUniversalDataSerializer>(MockBehavior.Loose);
            var carrier = BuildCarrierJob();
            var policy = new Mock<IRetryPolicy>(MockBehavior.Loose);
            policy.Setup(p => p.ToDescriptor())
                .Returns(Mock.Of<IRetryPolicyOptions>());

            carrier.Setup(c => c.GetRetryPolicyFactory())
                .Returns(() => policy.Object);

            return JobNodeDto.Create(serializer.Object, carrier.Object, isFifo: false, parentJob: null);
        }

        private static Mock<IDataJob> BuildCarrierJob()
        {
            var carrier = new Mock<IDataJob>(MockBehavior.Loose);
            carrier.SetupGet(c => c.Id).Returns(Guid.NewGuid());
            carrier.SetupGet(c => c.Name).Returns("job");
            carrier.Setup(c => c.GetPayload()).Returns(new DummyPayload());
            carrier.Setup(c => c.GetDependentDataJobs()).Returns(Array.Empty<IDataJob>());
            carrier.As<ISerializable>()
                .Setup(s => s.Serialize(It.IsAny<IUniversalDataSerializer>()))
                .Returns("{}");
            return carrier;
        }

        private sealed class DummyPayload : IPayload
        {
            public Guid HandlerId { get; } = Guid.NewGuid();
            public string PayloadId { get; } = "dummy";
            public DateTime CollectionTime { get; } = DateTime.UtcNow;
        }
    }
}
