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

        public ISystemTextJsonUniversalSerializer Serializer(JsonSerializerOptions options)
        {
            return SystemTextJsonUniversalSerializer.Create(options);
        }

        public IUniversalDataSerializer Serializer()
        {
            return SystemTextJsonUniversalSerializer.Create();
        }
    }
}
