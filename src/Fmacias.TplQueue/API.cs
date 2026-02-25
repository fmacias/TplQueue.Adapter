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
        private readonly IRetryPolicyGenericFactory _retryPolicyGenericFactory;
        private readonly IReadOnlyDictionary<string, IQOptions> _queueOptions;
        private readonly IReadOnlyDictionary<string, IRetryPolicyDescriptor> _retryPolicyOptions;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="retryPolicyGenericFactory"></param>
        /// <param name="queueOptions"></param>
        /// <param name="retryPolicyOptions"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private API(
            ICoreApi api,
            IRetryPolicyGenericFactory retryPolicyGenericFactory,
            IReadOnlyDictionary<string, IQOptions> queueOptions,
            IReadOnlyDictionary<string, IRetryPolicyDescriptor> retryPolicyOptions)
        {
            _coreApi = api ?? throw new ArgumentNullException(nameof(api));
            _retryPolicyGenericFactory = retryPolicyGenericFactory ?? throw new ArgumentNullException(nameof(retryPolicyGenericFactory));
            _queueOptions = queueOptions ?? throw new ArgumentNullException(nameof(queueOptions));
            _retryPolicyOptions = retryPolicyOptions ?? throw new ArgumentNullException(nameof(retryPolicyOptions));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="retryPolicygenericFactory"></param>
        /// <param name="retryPolicyOptions"></param>
        /// <returns></returns>
        /// <param name="queueOptions"></param>
        public static IApi Create(
            ICoreApi api,
            IRetryPolicyGenericFactory retryPolicygenericFactory,
            IReadOnlyDictionary<string, IRetryPolicyDescriptor> retryPolicyOptions,
            IReadOnlyDictionary<string, IQOptions> queueOptions)
        {
            return new API(api, retryPolicygenericFactory, queueOptions, retryPolicyOptions);
        }
        public IRetryPolicyGenericFactory RetryPolicyGenericFactory => _retryPolicyGenericFactory;
        public IDataJobFactory DataJobFactory(IPayloadHandlerResolver payloadHandlerResolver) 
            => Factories.DataJobFactory.Create(
                _coreApi.JobFactory,
                _coreApi.JobRootFactory,
                _retryPolicyGenericFactory,
                payloadHandlerResolver); 
        public IReadOnlyDictionary<string, IRetryPolicyDescriptor> RetryPolicyOptions => _retryPolicyOptions;
        public IReadOnlyDictionary<string, IQOptions> QueueOptions => _queueOptions;
        public Lazy<ICacheQFactory> CacheQFactory => new(() => Factories.CacheQFactory.Create());
        public Lazy<ICoreQFactoryAdapter> CoreQFactories => new(() 
            => CoreQFactoryAdapter.Create(
                _coreApi.QFactory,
                _retryPolicyGenericFactory,
                _queueOptions,
                _retryPolicyOptions));
        public Lazy<IJobRootFactory> JobRootFactory => new(() => _coreApi.JobRootFactory);
        public Lazy<IJobFactory> JobFactory => new(() => _coreApi.JobFactory);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheFactory"></param>
        /// <param name="serializer"></param>
        /// <param name="typeResolver"></param>
        /// 
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <param name="payloadHandlerResolver"></param>
        public T Cache<T>(ICacheFactory<T> cacheFactory,
            IUniversalDataSerializer serializer,
            INodeTypeResolver typeResolver, 
            IPayloadHandlerResolver payloadHandlerResolver) where T : IDataJobCache
        {
            if (cacheFactory == null) throw new ArgumentNullException(nameof(cacheFactory));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (typeResolver == null) throw new ArgumentNullException(nameof(typeResolver));

            return cacheFactory.CreateCache(serializer, DataJobFactory(payloadHandlerResolver), typeResolver);
        }

        public T RetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory, string name) 
            where T : IRetryPolicy
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
            return JsonSerializerFactory.Create();
        }
    }
}
