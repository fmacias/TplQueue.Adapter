using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Serialization.SystemTextJson;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Test.Serialization
{
    [TestFixture]
    public class SystemTextJsonUniversalSerializerTests
    {
        private sealed class TestPayload : IPayloadCommand
        {
            public TestPayload(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public string HandlerId => "test";

            public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
        }

        [Test]
        public void SerializeDeserialize_RoundTripGenericPayload()
        {
            var serializer =  SystemTextJsonUniversalSerializer.Create();
            var payload = new TestPayload("alpha");

            var json = serializer.Serialize(payload);
            var roundTripped = serializer.Deserialize<TestPayload>(json);

            Assert.That(roundTripped.Name, Is.EqualTo(payload.Name));
        }

        [Test]
        public void Serialize_WhenTypeDoesNotMatchValue_Throws()
        {
            var serializer = SystemTextJsonUniversalSerializer.Create();
            var payload = new TestPayload("alpha");

            Assert.Throws<ArgumentException>(() => serializer.Serialize(payload, typeof(string)));
        }

        [Test]
        public void SerializeCarrier_WhenPayloadIsNull_Throws()
        {
            var serializer = SystemTextJsonUniversalSerializer.Create();
            var carrier = new Mock<IPayloadCarrierJob>();
            carrier.Setup(c => c.GetPayload()).Returns((object)null!);
            carrier.Setup(c => c.PayloadType).Returns(typeof(TestPayload));

            Assert.Throws<InvalidOperationException>(() => serializer.Serialize(carrier.Object));
        }
    }
}
