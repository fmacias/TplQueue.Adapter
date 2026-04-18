using Fmacias.TplQueue.Contracts;
using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Fmacias.TplQueue.Serialization.Xml
{
    /// <summary>
    /// Universal payload serializer backed by <see cref="System.Xml.Serialization.XmlSerializer" />.
    /// </summary>
    internal sealed class XmlUniversalSerializer : IXmlUniversalSerializer
    {
        private XmlUniversalSerializer()
        {
        }

        /// <summary>
        /// Creates an XML universal serializer.
        /// </summary>
        /// <returns>A serializer that implements <see cref="IXmlUniversalSerializer" />.</returns>
        public static XmlUniversalSerializer Create()
        {
            return new XmlUniversalSerializer();
        }

        /// <summary>
        /// Serializes the provided value as XML using the specified runtime type.
        /// </summary>
        /// <param name="value">The payload value to serialize.</param>
        /// <param name="type">The runtime type used to create the XML serializer.</param>
        /// <returns>The serialized XML payload content.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="value" /> or <paramref name="type" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value" /> is not assignable to <paramref name="type" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the configured XML serializer cannot serialize <paramref name="value" />.
        /// </exception>
        public string Serialize(object value, Type type)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (type is null) throw new ArgumentNullException(nameof(type));

            if (!type.IsInstanceOfType(value))
            {
                throw new ArgumentException(
                    $"The provided value of type '{value.GetType().FullName}' is not an instance of '{type.FullName}'.",
                    nameof(value));
            }

            var serializer = new System.Xml.Serialization.XmlSerializer(type);
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                serializer.Serialize(writer, value);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Deserializes XML payload content into an instance of the specified runtime type.
        /// </summary>
        /// <param name="xml">The serialized XML payload content.</param>
        /// <param name="type">The runtime type used to create the XML serializer.</param>
        /// <returns>The deserialized payload instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="xml" /> is <see langword="null" />, empty, whitespace, or when
        /// <paramref name="type" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the XML is invalid, the XML serializer cannot deserialize the payload, or deserialization returns
        /// <see langword="null" />.
        /// </exception>
        public object Deserialize(string xml, Type type)
        {
            if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentNullException(nameof(xml));
            if (type is null) throw new ArgumentNullException(nameof(type));

            var serializer = new System.Xml.Serialization.XmlSerializer(type);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using var reader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(reader, settings);
            var result = serializer.Deserialize(xmlReader);

            return result is null
                ? throw new InvalidOperationException(
                    $"Deserialization produced null for target type '{type.FullName}'. " +
                    "Check the XML and the target type.")
                : result;
        }

        /// <summary>
        /// Serializes the provided value as XML using the compile-time generic type.
        /// </summary>
        /// <typeparam name="T">The compile-time payload type.</typeparam>
        /// <param name="value">The payload value to serialize.</param>
        /// <returns>The serialized XML payload content.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="value" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the configured XML serializer cannot serialize <paramref name="value" />.
        /// </exception>
        public string Serialize<T>(T value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            return Serialize(value, typeof(T));
        }

        /// <summary>
        /// Deserializes XML payload content into the compile-time generic type.
        /// </summary>
        /// <typeparam name="T">The target payload type.</typeparam>
        /// <param name="xml">The serialized XML payload content.</param>
        /// <returns>The deserialized payload instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="xml" /> is <see langword="null" />, empty, or whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the XML is invalid, the XML serializer cannot deserialize the payload, or deserialization returns
        /// <see langword="null" />.
        /// </exception>
        public T Deserialize<T>(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentNullException(nameof(xml));
            return (T)Deserialize(xml, typeof(T));
        }

        /// <summary>
        /// Serializes the payload carried by an <see cref="IDataJobNode" /> as XML.
        /// </summary>
        /// <param name="carrier">The data job node that carries the payload to serialize.</param>
        /// <returns>The serialized XML payload content.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="carrier" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the carrier payload is not assignable to the carrier payload type.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the carrier payload is <see langword="null" /> or when the configured XML serializer cannot
        /// serialize the payload.
        /// </exception>
        public string Serialize(IDataJobNode carrier)
        {
            if (carrier is null) throw new ArgumentNullException(nameof(carrier));
            var payload = carrier.GetPayload() ?? throw new InvalidOperationException("Carrier payload is null.");
            var type = carrier.PayloadType ?? payload.GetType();
            return Serialize(payload, type);
        }
    }
}
