using Fmacias.TplQueue.Cache;
using Fmacias.TplQueue.Cache.Factories;
using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Factories
{
    internal sealed class CacheFactory : ICacheFactory
    {
        private CacheFactory() { }

        public static ICacheFactory Create()
        {
            return new CacheFactory();
        }

        public IMemCache CreateMemCache(
            IPayloadJobFactory payloadRunnerFactory,
            IUniversalPayloadSerializer serializer,
            ICacheEntryFactory cacheFacade)
        {
            if (payloadRunnerFactory == null) throw new ArgumentNullException(nameof(payloadRunnerFactory));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (cacheFacade == null) throw new ArgumentNullException(nameof(cacheFacade));

            return MemCache.Create(
                serializer,
                DefaultNodeTypeResolverFactory.Create().CreateResolver(),
                payloadRunnerFactory,
                cacheFacade);
        }
    }
}
