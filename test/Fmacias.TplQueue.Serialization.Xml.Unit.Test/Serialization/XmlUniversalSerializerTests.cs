using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Serialization.Xml;
using Moq;
using NUnit.Framework;
using System;

namespace Fmacias.TplQueue.Serialization.Xml.Test
{
    [TestFixture]
    [Category("TPLQ-023")]
    public class XmlUniversalSerializerTests
    {
        public sealed class TestPayload : IPayload
        {
            public TestPayload()
            {
                PayloadId = string.Empty;
                Name = string.Empty;
                CollectionTime = DateTime.MinValue;
            }

            public TestPayload(string name, int count)
            {
                PayloadId = "xml-test/v1";
                Name = name;
                Count = count;
                CollectionTime = new DateTime(2026, 4, 17, 12, 30, 0, DateTimeKind.Utc);
            }

            public string PayloadId { get; set; }
            public string Name { get; set; }
            public int Count { get; set; }
            public DateTime CollectionTime { get; set; }
        }

        [Test]
        public void XmlSerializerFactory_Create_ProducesApprovedFactoryContract()
        {
            var factory = XmlSerializerFactory.Create();

            Assert.That(factory, Is.InstanceOf<IXmlSerializerFactory>());
        }

        [Test]
        public void XmlSerializerFactory_Serializer_ReturnsApprovedSerializerContracts()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();

            Assert.Multiple(() =>
            {
                Assert.That(serializer, Is.InstanceOf<IXmlUniversalSerializer>());
                Assert.That(serializer, Is.InstanceOf<IUniversalDataSerializer>());
            });
        }

        [Test]
        public void SerializeDeserialize_GenericPayload_RoundTrips()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();
            var payload = new TestPayload("alpha", 7);

            string xml = serializer.Serialize(payload);
            TestPayload roundTripped = serializer.Deserialize<TestPayload>(xml);

            Assert.Multiple(() =>
            {
                Assert.That(xml.TrimStart(), Does.StartWith("<"));
                Assert.That(roundTripped.PayloadId, Is.EqualTo(payload.PayloadId));
                Assert.That(roundTripped.Name, Is.EqualTo(payload.Name));
                Assert.That(roundTripped.Count, Is.EqualTo(payload.Count));
            });
        }

        [Test]
        public void SerializeDeserialize_ByRuntimeType_RoundTripsPayload()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();
            var payload = new TestPayload("alpha", 7);

            string xml = serializer.Serialize(payload, typeof(TestPayload));
            var roundTripped = serializer.Deserialize(xml, typeof(TestPayload));

            Assert.Multiple(() =>
            {
                Assert.That(xml.TrimStart(), Does.StartWith("<"));
                Assert.That(roundTripped, Is.InstanceOf<TestPayload>());
                Assert.That(((TestPayload)roundTripped).PayloadId, Is.EqualTo(payload.PayloadId));
                Assert.That(((TestPayload)roundTripped).Name, Is.EqualTo(payload.Name));
                Assert.That(((TestPayload)roundTripped).Count, Is.EqualTo(payload.Count));
            });
        }

        [Test]
        public void Serialize_WhenTypeDoesNotMatchValue_ThrowsArgumentException()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();
            var payload = new TestPayload("alpha", 7);

            Assert.Throws<ArgumentException>(() => serializer.Serialize(payload, typeof(string)));
        }

        [Test]
        public void Serialize_ByRuntimeType_WhenValueIsNull_ThrowsArgumentNullException()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null!, typeof(TestPayload)));
        }

        [Test]
        public void Serialize_ByRuntimeType_WhenTypeIsNull_ThrowsArgumentNullException()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Serialize(new TestPayload("alpha", 7), null!));
        }

        [Test]
        public void Serialize_Generic_WhenValueIsNull_ThrowsArgumentNullException()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Serialize<TestPayload>(null!));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Deserialize_ByRuntimeType_WhenXmlIsNullEmptyOrWhiteSpace_ThrowsArgumentNullException(string xml)
        {
            var serializer = XmlSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize(xml, typeof(TestPayload)));
        }

        [Test]
        public void Deserialize_ByRuntimeType_WhenTypeIsNull_ThrowsArgumentNullException()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize("<payload />", null!));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Deserialize_Generic_WhenXmlIsNullEmptyOrWhiteSpace_ThrowsArgumentNullException(string xml)
        {
            var serializer = XmlSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize<TestPayload>(xml));
        }

        [Test]
        public void Deserialize_ByRuntimeType_WhenXmlIsInvalid_ThrowsInvalidOperationException()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();

            Assert.Throws<InvalidOperationException>(() => serializer.Deserialize("<payload>", typeof(TestPayload)));
        }

        [Test]
        public void Deserialize_Generic_WhenXmlIsInvalid_ThrowsInvalidOperationException()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();

            Assert.Throws<InvalidOperationException>(() => serializer.Deserialize<TestPayload>("<payload>"));
        }

        [Test]
        public void SerializeCarrier_WhenCarrierIsNull_ThrowsArgumentNullException()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Serialize((IDataJobNode)null!));
        }

        [Test]
        public void SerializeCarrier_WhenPayloadIsNull_ThrowsInvalidOperationException()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();
            var carrier = new Mock<IDataJob>();
            carrier.Setup(c => c.GetPayload()).Returns((object)null!);
            carrier.Setup(c => c.PayloadType).Returns(typeof(TestPayload));

            Assert.Throws<InvalidOperationException>(() => serializer.Serialize((IDataJobNode)carrier.Object));
        }

        [Test]
        public void SerializeCarrier_WhenPayloadTypeIsNull_UsesPayloadRuntimeType()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();
            var payload = new TestPayload("alpha", 7);
            var carrier = new Mock<IDataJob>();
            carrier.Setup(c => c.GetPayload()).Returns(payload);
            carrier.Setup(c => c.PayloadType).Returns((Type)null!);

            var xml = serializer.Serialize((IDataJobNode)carrier.Object);
            var roundTripped = serializer.Deserialize<TestPayload>(xml);

            Assert.That(roundTripped.Name, Is.EqualTo(payload.Name));
        }

        [Test]
        public void SerializeCarrier_WhenPayloadTypeDoesNotMatchPayload_ThrowsArgumentException()
        {
            var serializer = XmlSerializerFactory.Create().Serializer();
            var carrier = new Mock<IDataJob>();
            carrier.Setup(c => c.GetPayload()).Returns(new TestPayload("alpha", 7));
            carrier.Setup(c => c.PayloadType).Returns(typeof(string));

            Assert.Throws<ArgumentException>(() => serializer.Serialize((IDataJobNode)carrier.Object));
        }
    }
}
