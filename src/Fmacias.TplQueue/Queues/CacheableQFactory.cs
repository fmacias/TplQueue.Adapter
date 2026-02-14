using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace Fmacias.TplQueue.Queues
{
    /// <summary>
    /// Default factory that creates task dispatchers from named <see cref="QOptions"/>
    /// or from explicit parameters.
    /// </summary>
    internal sealed class CacheableQFactory : ICacheableQFactory
    {
        private CacheableQFactory() { }
        public static ICacheableQFactory Instance()
        {
            return new CacheableQFactory();
        }
        public ICacheablePayloadQ Create(
            ILogger<ICacheablePayloadQ> logger,
            IPayloadJobCache payloadLeaseCache,
            IJobQ dispatcher)
        {
            if (payloadLeaseCache is null) throw new ArgumentNullException(nameof(payloadLeaseCache));
            return CacheableQ.Create(logger, payloadLeaseCache, dispatcher);
        }
    }
}
