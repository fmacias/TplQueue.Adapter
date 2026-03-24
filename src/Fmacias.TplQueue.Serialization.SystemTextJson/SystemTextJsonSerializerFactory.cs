using Fmacias.TplQueue.Contracts;
using System.Text.Json;

namespace Fmacias.TplQueue.Serialization.SystemTextJson
{
    public class SystemTextJsonSerializerFactory : ISystemTextJsonSerializerFactory
    {
        private SystemTextJsonSerializerFactory() { }
        public static SystemTextJsonSerializerFactory Create()
        {
            return new SystemTextJsonSerializerFactory();
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
