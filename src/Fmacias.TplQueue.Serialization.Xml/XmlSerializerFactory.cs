using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Serialization.Xml
{
    /// <summary>
    /// Factory for XML payload serializers.
    /// </summary>
    public sealed class XmlSerializerFactory : IXmlSerializerFactory
    {
        private XmlSerializerFactory()
        {
        }

        /// <summary>
        /// Creates an XML serializer factory.
        /// </summary>
        /// <returns>A new XML serializer factory instance.</returns>
        public static XmlSerializerFactory Create()
        {
            return new XmlSerializerFactory();
        }

        /// <summary>
        /// Creates an XML serializer that can be passed to cache and payload hydration APIs.
        /// </summary>
        /// <returns>An XML serializer backed by <see cref="System.Xml.Serialization.XmlSerializer" />.</returns>
        public IXmlUniversalSerializer Serializer()
        {
            return XmlUniversalSerializer.Create();
        }
    }
}
