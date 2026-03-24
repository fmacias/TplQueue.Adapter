using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.Cache.MemCache
{
    public class MemCacheFactory : IMemCacheFactory
    {
        public static MemCacheFactory Create()
        {
            return new MemCacheFactory();
        }

        public IMemCache CreateCache(IUniversalDataSerializer serializer,
            IDataJobFactory payloadJobFactory,
            ITypeResolver typeResolver, 
            IPayloadHandlerResolver payloadHandlerResolver, 
            IRetryPolicyAbstractFactory retryPolicyAbstractFactory)
        {
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (payloadJobFactory == null) throw new ArgumentNullException(nameof(payloadJobFactory));
            if (typeResolver == null) throw new ArgumentNullException(nameof(typeResolver));

            return MemCache.Create(serializer, payloadJobFactory, typeResolver, payloadHandlerResolver,retryPolicyAbstractFactory);
        } 
    }
}
