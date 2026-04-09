using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue
{
    /// <summary>
    /// Default payload handler registry used by adapter-side composition and cache hydration.
    /// It resolves handlers through stable plugin-style payload handler keys.
    /// </summary>
    internal sealed class PayloadHandlers : IPayloadHandlers, IPayloadHandlerRegistry
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, IHandler> _handlersByKey =
            new Dictionary<string, IHandler>(StringComparer.Ordinal);

        private PayloadHandlers()
        {
        }

        /// <summary>
        /// Creates an empty payload handler registry.
        /// </summary>
        public static PayloadHandlers Create()
        {
            return new PayloadHandlers();
        }

        /// <summary>
        /// Registers an already materialized payload handler by its stable handler key.
        /// </summary>
        public PayloadHandlers Register(string payloadHandlerKey, IHandler handler)
        {
            RegisterByKey(payloadHandlerKey, handler);
            return this;
        }

        /// <summary>
        /// Registers a payload handler factory by its stable handler key.
        /// The factory can resolve handlers from any IoC container or composition root.
        /// </summary>
        public PayloadHandlers Register(string payloadHandlerKey, Func<IHandler> handlerFactory)
        {
            if (handlerFactory == null) throw new ArgumentNullException(nameof(handlerFactory));

            return Register(payloadHandlerKey, new FactoryHandler(handlerFactory));
        }

        /// <summary>
        /// Registers an untyped payload handler delegate by its stable handler key.
        /// </summary>
        public PayloadHandlers Register(string payloadHandlerKey, Func<IPayload, CancellationToken, Task> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return Register(payloadHandlerKey, new DelegateHandler(handler));
        }

        /// <summary>
        /// Registers a typed payload handler delegate by its stable handler key.
        /// </summary>
        public PayloadHandlers Register<TPayload>(string payloadHandlerKey, Func<TPayload, CancellationToken, Task> handler)
            where TPayload : IPayload
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return Register(payloadHandlerKey, new DelegateHandler((payload, ct) =>
            {
                if (payload == null) throw new ArgumentNullException(nameof(payload));
                if (!(payload is TPayload typedPayload))
                {
                    var actualType = payload.GetType().FullName ?? payload.GetType().Name;
                    throw new InvalidOperationException(
                        $"Registered payload handler expects '{typeof(TPayload).FullName}' but received '{actualType}'.");
                }

                return handler(typedPayload, ct);
            }));
        }

        /// <summary>
        /// Applies the registrations contributed by a plugin module.
        /// </summary>
        public PayloadHandlers RegisterPlugin(IPayloadHandlerPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));

            plugin.Register(this);
            return this;
        }

        /// <inheritdoc />
        public IHandler Handler(string payloadHandlerKey)
        {
            if (string.IsNullOrWhiteSpace(payloadHandlerKey))
                throw new ArgumentException("Payload handler key cannot be null or empty.", nameof(payloadHandlerKey));

            lock (_sync)
            {
                if (_handlersByKey.TryGetValue(payloadHandlerKey, out var handler))
                {
                    return handler;
                }
            }

            throw new KeyNotFoundException(
                $"Handler not registered for payload handler key '{payloadHandlerKey}'.");
        }

        void IPayloadHandlerRegistry.Register(string payloadHandlerKey, IHandler handler)
        {
            Register(payloadHandlerKey, handler);
        }

        void IPayloadHandlerRegistry.Register(string payloadHandlerKey, Func<IHandler> handlerFactory)
        {
            Register(payloadHandlerKey, handlerFactory);
        }

        private void RegisterByKey(string payloadHandlerKey, IHandler handler)
        {
            if (string.IsNullOrWhiteSpace(payloadHandlerKey))
                throw new ArgumentException("Payload handler key cannot be null or empty.", nameof(payloadHandlerKey));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_sync)
            {
                if (_handlersByKey.TryGetValue(payloadHandlerKey, out var existing) &&
                    !ReferenceEquals(existing, handler))
                {
                    throw new InvalidOperationException(
                        $"A different handler is already registered for payload handler key '{payloadHandlerKey}'.");
                }

                _handlersByKey[payloadHandlerKey] = handler;
            }
        }

        private sealed class FactoryHandler : IHandler
        {
            private readonly Func<IHandler> _handlerFactory;

            public FactoryHandler(Func<IHandler> handlerFactory)
            {
                _handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
            }

            public Task HandleAsync(IPayload payload, CancellationToken cancellationToken)
            {
                if (payload == null) throw new ArgumentNullException(nameof(payload));

                var handler = _handlerFactory();
                if (handler == null)
                {
                    throw new InvalidOperationException("Payload handler factory returned null.");
                }

                return handler.HandleAsync(payload, cancellationToken);
            }
        }

        private sealed class DelegateHandler : IHandler
        {
            private readonly Func<IPayload, CancellationToken, Task> _handler;

            public DelegateHandler(Func<IPayload, CancellationToken, Task> handler)
            {
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }

            public Task HandleAsync(IPayload payload, CancellationToken cancellationToken)
            {
                if (payload == null) throw new ArgumentNullException(nameof(payload));

                return _handler(payload, cancellationToken);
            }
        }
    }
}
