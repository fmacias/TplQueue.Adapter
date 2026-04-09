using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue
{
    /// <summary>
    /// Public builder used by consumer applications to compose payload handlers and plugins
    /// into the adapter's internal payload handler registry.
    /// </summary>
    public sealed class PayloadHandlersBuilder : IPayloadHandlerRegistry
    {
        private readonly PayloadHandlers _payloadHandlers;

        private PayloadHandlersBuilder(PayloadHandlers payloadHandlers)
        {
            _payloadHandlers = payloadHandlers ?? throw new ArgumentNullException(nameof(payloadHandlers));
        }

        /// <summary>
        /// Creates an empty payload handler builder.
        /// </summary>
        public static PayloadHandlersBuilder Create()
        {
            return new PayloadHandlersBuilder(PayloadHandlers.Create());
        }

        /// <summary>
        /// Registers an already materialized payload handler by its stable handler key.
        /// </summary>
        public PayloadHandlersBuilder Register(string payloadHandlerKey, IHandler handler)
        {
            _payloadHandlers.Register(payloadHandlerKey, handler);
            return this;
        }

        /// <summary>
        /// Registers a payload handler factory by its stable handler key.
        /// The factory can resolve handlers from any IoC container or composition root.
        /// </summary>
        public PayloadHandlersBuilder Register(string payloadHandlerKey, Func<IHandler> handlerFactory)
        {
            _payloadHandlers.Register(payloadHandlerKey, handlerFactory);
            return this;
        }

        /// <summary>
        /// Registers an untyped payload handler delegate by its stable handler key.
        /// </summary>
        public PayloadHandlersBuilder Register(string payloadHandlerKey, Func<IPayload, CancellationToken, Task> handler)
        {
            _payloadHandlers.Register(payloadHandlerKey, handler);
            return this;
        }

        /// <summary>
        /// Registers a typed payload handler delegate by its stable handler key.
        /// </summary>
        public PayloadHandlersBuilder Register<TPayload>(string payloadHandlerKey, Func<TPayload, CancellationToken, Task> handler)
            where TPayload : IPayload
        {
            _payloadHandlers.Register(payloadHandlerKey, handler);
            return this;
        }

        /// <summary>
        /// Applies the registrations contributed by a plugin module.
        /// </summary>
        public PayloadHandlersBuilder RegisterPlugin(IPayloadHandlerPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));

            plugin.Register(this);
            return this;
        }

        /// <summary>
        /// Builds the payload handler resolver for callers that need direct access to <see cref="IPayloadHandlers"/>.
        /// </summary>
        public IPayloadHandlers Build()
        {
            return _payloadHandlers;
        }

        internal PayloadHandlers BuildInternal()
        {
            return _payloadHandlers;
        }

        void IPayloadHandlerRegistry.Register(string payloadHandlerKey, IHandler handler)
        {
            Register(payloadHandlerKey, handler);
        }

        void IPayloadHandlerRegistry.Register(string payloadHandlerKey, Func<IHandler> handlerFactory)
        {
            Register(payloadHandlerKey, handlerFactory);
        }
    }
}
