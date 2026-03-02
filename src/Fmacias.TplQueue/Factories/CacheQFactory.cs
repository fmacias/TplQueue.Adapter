using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace Fmacias.TplQueue.Factories
{
    /// <summary>
    /// Default factory that creates task dispatchers from named <see cref="QOptions"/>
    /// or from explicit parameters.
    /// </summary>
    internal sealed class CacheQFactory : ICacheQFactory
    {
        private CacheQFactory() { }
        public static ICacheQFactory Create()
        {
            return new CacheQFactory();
        }
        public ICacheQ CacheQ(
            ILogger<ICacheQ> logger,
            IDataJobCache payloadLeaseCache,
            IParallelQ queue)
        {
            if (payloadLeaseCache is null) throw new ArgumentNullException(nameof(payloadLeaseCache));
            return Queues.CacheQ.Create(logger, payloadLeaseCache, queue);
        }
    }
}
