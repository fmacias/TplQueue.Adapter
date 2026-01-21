using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace Fmacias.TplQueue.Queues
{
    /// <summary>
    /// Default factory that creates task dispatchers from named <see cref="DispatcherOptions"/>
    /// or from explicit parameters.
    /// </summary>
    internal sealed class CacheableChainFactory : ICacheableChainFactory
    {
        private CacheableChainFactory() { }
        public static ICacheableChainFactory Instance()
        {
            return new CacheableChainFactory();
        }
        public ICacheablePayloadChain Create(
            ILogger<ICacheablePayloadChain> logger,
            IPayloadLeaseCache payloadLeaseCache,
            IJobsChain dispatcher)
        {
            if (payloadLeaseCache is null) throw new ArgumentNullException(nameof(payloadLeaseCache));
            return CacheableChain.Create(logger, payloadLeaseCache, dispatcher);
        }
    }
}
