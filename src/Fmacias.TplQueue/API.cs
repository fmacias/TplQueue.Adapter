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
        private readonly IPayloadHandlerResolver _payloadHandlerResolver;
        private readonly IReadOnlyDictionary<string, IQOptions> _queueOptions;
        private readonly IReadOnlyDictionary<string, IRetryPolicyOptions> _retryPolicyOptions;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="payloadHandlerResolver"></param>
        /// <param name="queueOptions"></param>
        /// 
        /// <exception cref="ArgumentNullException"></exception>
        /// <param name="retryPolicyOptions"></param>
        private API(
            ICoreApi api,
            IPayloadHandlerResolver payloadHandlerResolver,
            IReadOnlyDictionary<string, IQOptions> queueOptions, 
            IReadOnlyDictionary<string, IRetryPolicyOptions> retryPolicyOptions)
        {
            _coreApi = api ?? throw new ArgumentNullException(nameof(api));
            _payloadHandlerResolver = payloadHandlerResolver ?? throw new ArgumentNullException(nameof(payloadHandlerResolver));
            _retryPolicyAbstractFactory = RetryPolicies.RetryPolicyAbstractFactory.Create();
            _queueOptions = queueOptions ?? throw new ArgumentNullException(nameof(queueOptions));
            _retryPolicyOptions = retryPolicyOptions ?? throw new ArgumentNullException(nameof(retryPolicyOptions));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="payloadHandlerResolver"></param>
        /// <param name="retryPolicyOptions"></param>
        /// <returns></returns>
        /// 
        /// <param name="queueOptions"></param>
        public static IApi Create(
            ICoreApi api,
            IPayloadHandlerResolver payloadHandlerResolver,
            IReadOnlyDictionary<string, IRetryPolicyOptions> retryPolicyOptions, 
            IReadOnlyDictionary<string, IQOptions> queueOptions)
        {
            return new API(api, payloadHandlerResolver, queueOptions, retryPolicyOptions);
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

            return cacheFactory.CreateCache(serializer, DataJobFactory, typeResolver, _payloadHandlerResolver, _retryPolicyAbstractFactory);
        }

        public T RetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory, string name) where T : IRetryPolicy
        {
            if (retryPolicyFactory is null) throw new ArgumentNullException(nameof(retryPolicyFactory));

            return retryPolicyFactory.CreatePolicy(name, _retryPolicyOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IObserverFactory ObserverFactory()
        {
            return Factories.ObserverFactory.Instance();
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
