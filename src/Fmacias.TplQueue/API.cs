using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Factories;
using Fmacias.TplQueue.Serialization.SystemTextJson;
using System;
using System.Collections.Generic;

namespace Fmacias.TplQueue
{
    public sealed class API : IApi
    {
        private readonly ICoreApi _coreApi;
        private readonly IRetryPolicyAbstractFactory _retryPolicyAbstractFactory;
        private readonly PayloadHandlers _payloadHandlers;
        private readonly IReadOnlyDictionary<string, IQOptions> _queueOptions;
        private readonly IReadOnlyDictionary<string, IRetryPolicyOptions> _retryPolicyOptions;

        /// <summary>
        /// Initializes a new <see cref="API"/> instance using the provided internal payload handler registry.
        /// </summary>
        /// <param name="api">The underlying core facade.</param>
        /// <param name="payloadHandlers">The internal payload handlers used for cache hydration.</param>
        /// <param name="queueOptions">The configured queue options.</param>
        /// <param name="retryPolicyOptions">The configured retry policy options.</param>
        private API(
            ICoreApi api,
            PayloadHandlers payloadHandlers,
            IReadOnlyDictionary<string, IQOptions> queueOptions, 
            IReadOnlyDictionary<string, IRetryPolicyOptions> retryPolicyOptions)
        {
            _coreApi = api ?? throw new ArgumentNullException(nameof(api));
            _payloadHandlers = payloadHandlers ?? throw new ArgumentNullException(nameof(payloadHandlers));
            _retryPolicyAbstractFactory = RetryPolicies.RetryPolicyAbstractFactory.Create();
            _queueOptions = queueOptions ?? throw new ArgumentNullException(nameof(queueOptions));
            _retryPolicyOptions = retryPolicyOptions ?? throw new ArgumentNullException(nameof(retryPolicyOptions));
        }

        /// <summary>
        /// Creates a facade with a caller-provided payload handler builder.
        /// </summary>
        /// <param name="api">The underlying core facade.</param>
        /// <param name="payloadHandlersBuilder">The builder used to compose the internal payload handlers for cache hydration.</param>
        /// <param name="retryPolicyOptions">The configured retry policy options.</param>
        /// <param name="queueOptions">The configured queue options.</param>
        public static API Create(
            ICoreApi api,
            PayloadHandlersBuilder payloadHandlersBuilder,
            IReadOnlyDictionary<string, IRetryPolicyOptions> retryPolicyOptions, 
            IReadOnlyDictionary<string, IQOptions> queueOptions)
        {
            if (payloadHandlersBuilder == null) throw new ArgumentNullException(nameof(payloadHandlersBuilder));

            return new API(api, payloadHandlersBuilder.BuildInternal(), queueOptions, retryPolicyOptions);
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
            return new API(api, PayloadHandlers.Create(), queueOptions, retryPolicyOptions);
        }

        public IQFactoryAdapter QFactory => QFactoryAdapter.Create(_coreApi.QFactory, _retryPolicyAbstractFactory, _queueOptions, _retryPolicyOptions);
        public IRetryPolicyAbstractFactory RetryPolicyAbstractFactory => _retryPolicyAbstractFactory;
        public IDataJobFactory DataJobFactory => _coreApi.DataJobFactory; 
        public IReadOnlyDictionary<string, IRetryPolicyOptions> RetryPolicyOptions => _retryPolicyOptions;
        public IReadOnlyDictionary<string, IQOptions> QueueOptions => _queueOptions;
        public IJobFactory JobFactory => _coreApi.JobFactory;

        /// <summary>
        /// todo where statement needs to be a class? 
        /// ex.: where T: Class, IDataJobCache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheFactory"></param>
        /// <param name="serializer"></param>
        /// <param name="typeResolver"></param>
        /// 
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// 
        public T Cache<T>(ICacheFactory<T> cacheFactory,
            IUniversalDataSerializer serializer,
            ITypeResolver typeResolver) where T : IDataJobCache
        {
            if (cacheFactory == null) throw new ArgumentNullException(nameof(cacheFactory));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (typeResolver == null) throw new ArgumentNullException(nameof(typeResolver));

            return cacheFactory.CreateCache(serializer, DataJobFactory, typeResolver, _payloadHandlers, _retryPolicyAbstractFactory);
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
        /// 
        /// </summary>
        /// <returns></returns>
        public ISystemTextJsonSerializerFactory SystemTexSerializerFactory()
        {
            return SystemTextJsonSerializerFactory.Create();
        }

    }
}
