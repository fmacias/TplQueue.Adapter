using Fmacias.TplQueue.Contracts;
using System.Text.Json;

namespace Fmacias.TplQueue.Serialization.SystemTextJson
{
    public class JsonSerializerFactory : ISystemTextJsonSerializerFactory
    {
        private JsonSerializerFactory() { }
        public static JsonSerializerFactory Create()
        {
            return new JsonSerializerFactory();
        }

        public ISystemTextJsonUniversalSerializer CreateSerializer(JsonSerializerOptions options)
        {
            return SystemTextJsonUniversalSerializer.Create(options);
        }

        public IUniversalPayloadSerializer CreateSerializer()
        {
            return SystemTextJsonUniversalSerializer.Create();
        }
    }
}
