using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace Fmacias.TplQueue.Queues
{
    /// <summary>
    /// Default factory that creates task dispatchers from named <see cref="DispatcherOptions"/>
    /// or from explicit parameters.
    /// </summary>
    internal sealed class SerializableDispatcherFactory : ISerializableDispatcherFactory
    {
        private SerializableDispatcherFactory() { }
        public static ISerializableDispatcherFactory Instance()
        {
            return new SerializableDispatcherFactory();
        }
        public ISerializablePayloadDispatcher Create(
            ILogger<ISerializablePayloadDispatcher> logger,
            IPayloadLeaseCache payloadLeaseCache,
            ITaskDispatcher dispatcher)
        {
            if (payloadLeaseCache is null) throw new ArgumentNullException(nameof(payloadLeaseCache));
            return SerializableDispatcher.Create(logger, payloadLeaseCache, dispatcher);
        }
    }
}
