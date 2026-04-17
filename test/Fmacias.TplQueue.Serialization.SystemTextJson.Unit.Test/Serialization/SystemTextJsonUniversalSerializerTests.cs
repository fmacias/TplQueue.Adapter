using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Serialization.SystemTextJson;
using Moq;
using NUnit.Framework;
using System;
using System.Text.Json;

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
            }

            public string Name { get; }
            public string PayloadId => "test";
            public DateTime CollectionTime => DateTime.UtcNow;
        }

        [Test]
        public void SerializeDeserialize_GenericPayload_RoundTrips()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();
            var payload = new TestPayload("alpha");

            var json = serializer.Serialize(payload);
            var roundTripped = serializer.Deserialize<TestPayload>(json);

            Assert.That(roundTripped.Name, Is.EqualTo(payload.Name));
        }

        [Test]
        public void SerializeDeserialize_ByRuntimeType_RoundTripsPayload()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();
            var payload = new TestPayload("alpha");

            var json = serializer.Serialize(payload, typeof(TestPayload));
            var roundTripped = serializer.Deserialize(json, typeof(TestPayload));

            Assert.Multiple(() =>
            {
                Assert.That(roundTripped, Is.InstanceOf<TestPayload>());
                Assert.That(((TestPayload)roundTripped).Name, Is.EqualTo(payload.Name));
            });
        }

        [Test]
        public void Serialize_WhenTypeDoesNotMatchValue_Throws()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();
            var payload = new TestPayload("alpha");

            Assert.Throws<ArgumentException>(() => serializer.Serialize(payload, typeof(string)));
        }

        [Test]
        public void Serialize_ByRuntimeType_WhenValueIsNull_ThrowsArgumentNullException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null!, typeof(TestPayload)));
        }

        [Test]
        public void Serialize_ByRuntimeType_WhenTypeIsNull_ThrowsArgumentNullException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Serialize(new TestPayload("alpha"), null!));
        }

        [Test]
        public void Serialize_Generic_WhenValueIsNull_ThrowsArgumentNullException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Serialize<TestPayload>(null!));
        }

        [Test]
        public void Deserialize_ByRuntimeType_WhenJsonIsNull_ThrowsArgumentNullException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize(null!, typeof(TestPayload)));
        }

        [TestCase("")]
        [TestCase(" ")]
        public void Deserialize_ByRuntimeType_WhenJsonIsEmptyOrWhiteSpace_ThrowsArgumentNullException(string json)
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize(json, typeof(TestPayload)));
        }

        [Test]
        public void Deserialize_ByRuntimeType_WhenTypeIsNull_ThrowsArgumentNullException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize("{}", null!));
        }

        [Test]
        public void Deserialize_Generic_WhenJsonIsNull_ThrowsArgumentNullException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize<TestPayload>(null!));
        }

        [TestCase("")]
        [TestCase(" ")]
        public void Deserialize_Generic_WhenJsonIsEmptyOrWhiteSpace_ThrowsArgumentNullException(string json)
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize<TestPayload>(json));
        }

        [Test]
        public void Deserialize_ByRuntimeType_WhenJsonIsInvalid_ThrowsJsonException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<JsonException>(() => serializer.Deserialize("{ invalid json", typeof(TestPayload)));
        }

        [Test]
        public void Deserialize_Generic_WhenJsonIsInvalid_ThrowsJsonException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<JsonException>(() => serializer.Deserialize<TestPayload>("{ invalid json"));
        }

        [Test]
        public void Deserialize_ByRuntimeType_WhenJsonLiteralNull_ThrowsInvalidOperationException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<InvalidOperationException>(() => serializer.Deserialize("null", typeof(TestPayload)));
        }

        [Test]
        public void Deserialize_Generic_WhenJsonLiteralNull_ThrowsInvalidOperationException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<InvalidOperationException>(() => serializer.Deserialize<TestPayload>("null"));
        }

        [Test]
        public void Deserialize_UsesCaseInsensitivePropertyNamesByDefault()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            var roundTripped = serializer.Deserialize<TestPayload>("{\"name\":\"alpha\"}");

            Assert.That(roundTripped.Name, Is.EqualTo("alpha"));
        }

        [Test]
        public void SerializeCarrier_WhenCarrierIsNull_ThrowsArgumentNullException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Serialize((IDataJobNode)null!));
        }

        [Test]
        public void SerializeCarrier_WhenPayloadIsNull_Throws()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();
            var carrier = new Mock<IDataJob>();
            carrier.Setup(c => c.GetPayload()).Returns((object)null!);
            carrier.Setup(c => c.PayloadType).Returns(typeof(TestPayload));

            Assert.Throws<InvalidOperationException>(() => serializer.Serialize((IDataJobNode)carrier.Object));
        }

        [Test]
        public void SerializeCarrier_WhenPayloadTypeIsNull_UsesPayloadRuntimeType()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();
            var payload = new TestPayload("alpha");
            var carrier = new Mock<IDataJob>();
            carrier.Setup(c => c.GetPayload()).Returns(payload);
            carrier.Setup(c => c.PayloadType).Returns((Type)null!);

            var json = serializer.Serialize((IDataJobNode)carrier.Object);
            var roundTripped = serializer.Deserialize<TestPayload>(json);

            Assert.That(roundTripped.Name, Is.EqualTo(payload.Name));
        }

        [Test]
        public void SerializeCarrier_WhenPayloadTypeDoesNotMatchPayload_ThrowsArgumentException()
        {
            var serializer = SystemTextJsonSerializerFactory.Create().Serializer();
            var carrier = new Mock<IDataJob>();
            carrier.Setup(c => c.GetPayload()).Returns(new TestPayload("alpha"));
            carrier.Setup(c => c.PayloadType).Returns(typeof(string));

            Assert.Throws<ArgumentException>(() => serializer.Serialize((IDataJobNode)carrier.Object));
        }
    }
}
