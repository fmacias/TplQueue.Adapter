using Fmacias.TplQueue.Cache;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Factories
{
    internal sealed class CacheFactory : ICacheFactory
    {
        private CacheFactory() { }

        public static ICacheFactory Instance()
        {
            return new CacheFactory();
        }

        public IMemCache CreateMemCache(IPayloadJobFactory payloadRunnerFactory,
            IJsonUniversalPayloadSerializer serializer)
        {
            return MemCache.Create(payloadRunnerFactory, serializer);
        }
    }
}
