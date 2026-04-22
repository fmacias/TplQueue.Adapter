using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Factories;
using Fmacias.TplQueue.Serialization.SystemTextJson;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue
{
    public sealed class API : IApi
    {
        private readonly ICoreApi _coreApi;
        private readonly PayloadHandlers _cacheDataHandlers;
        private readonly IRetryPolicyAbstractFactory _retryPolicyAbstractFactory;
        private readonly IReadOnlyDictionary<string, IQOptions> _queueOptions;
        private readonly IReadOnlyDictionary<string, IRetryPolicyOptions> _retryPolicyOptions;

        /// <summary>
        /// Initializes a new <see cref="API"/> instance.
        /// </summary>
        /// <param name="api">The underlying core facade.</param>
        /// <param name="queueOptions">The configured queue options.</param>
        /// <param name="retryPolicyOptions">The configured retry policy options.</param>
        private API(
            ICoreApi api,
            IReadOnlyDictionary<string, IQOptions> queueOptions,
            IReadOnlyDictionary<string, IRetryPolicyOptions> retryPolicyOptions)
        {
            _coreApi = api ?? throw new ArgumentNullException(nameof(api));
            _cacheDataHandlers = PayloadHandlers.Create();
            _retryPolicyAbstractFactory = RetryPolicies.RetryPolicyAbstractFactory.Create();
            _queueOptions = queueOptions ?? throw new ArgumentNullException(nameof(queueOptions));
            _retryPolicyOptions = retryPolicyOptions ?? throw new ArgumentNullException(nameof(retryPolicyOptions));
        }

        /// <summary>
        /// Creates a facade with an empty internal payload handler resolver.
        /// </summary>
        /// <param name="api">The underlying core facade.</param>
        /// <param name="retryPolicyOptions">The configured retry policy options.</param>
        /// <param name="queueOptions">The configured queue options.</param>
        public static API Create(
            ICoreApi api,
            IReadOnlyDictionary<string, IRetryPolicyOptions> retryPolicyOptions,
            IReadOnlyDictionary<string, IQOptions> queueOptions)
        {
            return new API(api, queueOptions, retryPolicyOptions);
        }

        public IQFactoryAdapter QFactory => QFactoryAdapter.Create(_coreApi.QFactory, _retryPolicyAbstractFactory, _queueOptions, _retryPolicyOptions);
        public IRetryPolicyAbstractFactory RetryPolicyAbstractFactory => _retryPolicyAbstractFactory;
        public IDataJobFactory DataJobFactory => _coreApi.DataJobFactory; 
        public IReadOnlyDictionary<string, IRetryPolicyOptions> RetryPolicyOptions => _retryPolicyOptions;
        public IReadOnlyDictionary<string, IQOptions> QueueOptions => _queueOptions;
        public IJobFactory JobFactory => _coreApi.JobFactory;

        /// <inheritdoc />
        public IApi RegisterPayloadHandler(string payloadHandlerKey, IHandler handler)
        {
            _cacheDataHandlers.Register(payloadHandlerKey, handler);
            return this;
        }

        /// <inheritdoc />
        public IApi RegisterPayloadHandler(string payloadHandlerKey, Func<IHandler> handlerFactory)
        {
            _cacheDataHandlers.Register(payloadHandlerKey, handlerFactory);
            return this;
        }

        /// <inheritdoc />
        public IApi RegisterPayloadHandler(string payloadHandlerKey, Func<IPayload, CancellationToken, Task> handler)
        {
            _cacheDataHandlers.Register(payloadHandlerKey, handler);
            return this;
        }

        /// <inheritdoc />
        public IApi RegisterPayloadHandler<TPayload>(string payloadHandlerKey, Func<TPayload, CancellationToken, Task> handler)
            where TPayload : IPayload
        {
            _cacheDataHandlers.Register(payloadHandlerKey, handler);
            return this;
        }

        /// <inheritdoc />
        public IApi RegisterPayloadHandlerPlugin(IPayloadHandlerPlugin plugin)
        {
            _cacheDataHandlers.RegisterPlugin(plugin);
            return this;
        }

        /// <summary>
        /// Creates a cache using the facade-owned default runtime type resolver.
        /// </summary>
        /// <typeparam name="T">The concrete cache contract.</typeparam>
        /// <param name="cacheFactory">The cache factory.</param>
        /// <param name="serializer">The serializer used to persist payload data.</param>
        /// <returns>The created cache instance.</returns>
        public T Cache<T>(ICacheFactory<T> cacheFactory,
            IUniversalDataSerializer serializer) where T : IDataJobCache
        {
            return Cache(cacheFactory, serializer, DefaultTypeResolver.Create());
        }

        /// <summary>
        /// Creates a cache using an explicit payload type resolver.
        /// </summary>
        /// <typeparam name="T">The concrete cache contract.</typeparam>
        /// <param name="cacheFactory">The cache factory.</param>
        /// <param name="serializer">The serializer used to persist payload data.</param>
        /// <param name="typeResolver">The payload type resolver.</param>
        /// <returns>The created cache instance.</returns>
        public T Cache<T>(ICacheFactory<T> cacheFactory,
            IUniversalDataSerializer serializer,
            ITypeResolver typeResolver) where T : IDataJobCache
        {
            if (cacheFactory == null) throw new ArgumentNullException(nameof(cacheFactory));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (typeResolver == null) throw new ArgumentNullException(nameof(typeResolver));

            return cacheFactory.CreateCache(serializer, DataJobFactory, typeResolver, _cacheDataHandlers, _retryPolicyAbstractFactory);
        }

        public T RetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory) where T : IRetryPolicy
        {
            if (retryPolicyFactory is null) throw new ArgumentNullException(nameof(retryPolicyFactory));

            return retryPolicyFactory.CreatePolicy();
        }

        public T RetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory, string name) where T : IRetryPolicy
        {
            if (retryPolicyFactory is null) throw new ArgumentNullException(nameof(retryPolicyFactory));

            return retryPolicyFactory.CreatePolicy(name, _retryPolicyOptions);
        }
        public T RetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory, IRetryPolicyOptions retryPolicyOptions) where T : IRetryPolicy
        {
            if (retryPolicyFactory is null) throw new ArgumentNullException(nameof(retryPolicyFactory));
            if (retryPolicyOptions is null) throw new ArgumentNullException(nameof(retryPolicyOptions));

            return retryPolicyFactory.CreatePolicy(retryPolicyOptions);
        }
        public IExponentialBackoff RetryPolicy(IExponentialBackofFactory exponentialBackofFactory, int maxRetries, int delayMs, double factor)
        {
            if (exponentialBackofFactory is null) throw new ArgumentNullException(nameof(exponentialBackofFactory));

            return exponentialBackofFactory.ExponentialBackof(maxRetries, delayMs, factor);
        }

        public ILinearBackoff RetryPolicy(ILinearBackoffFactory linearBackofFactory, int maxRetries, int delayMs)
        {
            if (linearBackofFactory is null) throw new ArgumentNullException(nameof(linearBackofFactory));

            return linearBackofFactory.LinearBackoff(maxRetries, delayMs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IObserverFactory ObserverFactory()
        {
            return Observers.ObserverFactory.Create();
        }

        /// <summary>
        /// Creates the System.Text.Json serializer factory exposed by the adapter facade.
        /// </summary>
        /// <returns>A System.Text.Json serializer factory.</returns>
        public ISystemTextJsonSerializerFactory SystemTextSerializerFactory()
        {
            return SystemTextJsonSerializerFactory.Create();
        }

        /// <summary>
        /// Creates the System.Text.Json serializer factory exposed by the adapter facade.
        /// </summary>
        /// <remarks>
        /// This typo-preserving member is retained for source and binary compatibility.
        /// Prefer <see cref="SystemTextSerializerFactory"/>.
        /// </remarks>
        /// <returns>A System.Text.Json serializer factory.</returns>
        public ISystemTextJsonSerializerFactory SystemTexSerializerFactory()
        {
            return SystemTextSerializerFactory();
        }

        /// <summary>
        /// Creates the XML serializer factory exposed by the adapter facade.
        /// </summary>
        /// <returns>An XML serializer factory.</returns>
        public IXmlSerializerFactory XmlSerializerFactory()
        {
            return Fmacias.TplQueue.Serialization.Xml.XmlSerializerFactory.Create();
        }

    }
}
