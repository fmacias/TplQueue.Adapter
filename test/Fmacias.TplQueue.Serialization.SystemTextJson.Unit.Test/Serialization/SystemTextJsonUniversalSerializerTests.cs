using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Serialization.SystemTextJson;
using Moq;
using NUnit.Framework;
using System;

namespace Fmacias.TplQueue.Serialization.SystemTextJson.Test
{
    [TestFixture]
    public class SystemTextJsonUniversalSerializerTests
    {
        private sealed class TestPayload : IPayload
        {
            public TestPayload(string name)
            {
                Name = name;
                HandlerId = Guid.NewGuid();
            }

            public string Name { get; }
            public string PayloadId => "test";
            public DateTime CollectionTime => DateTime.UtcNow;
            public Guid HandlerId { get; }
        }

        [Test]
        public void SerializeDeserialize_RoundTripGenericPayload()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();
            var payload = new TestPayload("alpha");

            var json = serializer.Serialize(payload);
            var roundTripped = serializer.Deserialize<TestPayload>(json);

            Assert.That(roundTripped.Name, Is.EqualTo(payload.Name));
        }

        [Test]
        public void Serialize_WhenTypeDoesNotMatchValue_Throws()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();
            var payload = new TestPayload("alpha");

            Assert.Throws<ArgumentException>(() => serializer.Serialize(payload, typeof(string)));
        }

        [Test]
        public void SerializeCarrier_WhenPayloadIsNull_Throws()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();
            var carrier = new Mock<IDataJob>();
            carrier.Setup(c => c.GetPayload()).Returns((object)null!);
            carrier.Setup(c => c.PayloadType).Returns(typeof(TestPayload));

            Assert.Throws<InvalidOperationException>(() => serializer.Serialize(carrier.Object));
        }
    }
}
