using Fmaciasruano.TplQueue.Abstractions.Contracts;

namespace Fmaciasruano.TplQueue.Cache
{
    internal sealed class CacheFactory : ICacheFactory
    {
        private CacheFactory() { }

        public static ICacheFactory Instance()
        {
            return new CacheFactory();
        }

        public IMemCache CreateMemCache(IPayloadRunnerFactory payloadRunnerFactory,
            IUniversalPayloadSerializer serializer)
        {
            return MemCache.Create(payloadRunnerFactory, serializer);
        }
    }
}
