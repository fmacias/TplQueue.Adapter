using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Factories;
using Fmacias.TplQueue.Handlers;
using Fmacias.TplQueue.Queues;
using Fmacias.TplQueue.RetryPolicies;
using Fmacias.TplQueue.Serialization.SystemTextJson;
using System;
using System.Collections.Generic;

namespace Fmacias.TplQueue
{
    public class API : IApi
    {
        private readonly ICoreApi _coreApi;
        private API(ICoreApi api)
        {
            _coreApi = api ?? throw new ArgumentNullException(nameof(api));
        }
        public static IApi Create(ICoreApi api)
        {
            return new API(api);
        }
        public ICacheFactory CacheFactory()
        {
            return Factories.CacheFactory.Create();
        }

        public ICoreApi GetCoreApi()
        {
            return _coreApi;
        }

        public IObserverFactory ObserverFactory()
        {
            return Factories.ObserverFactory.Instance();
        }
        public IPayloadJobFactory PayloadJobFactory(IReadOnlyDictionary<string, RetryPolicyOptions>? options = null)
        {
            var retryPolicyOptions = options ?? new Dictionary<string, RetryPolicyOptions>();
            return Factories.PayloadJobFactory.Create(
                _coreApi.GetJobFactoryCore(),
                _coreApi.GetJobRootFactoryCore(),
                RetryPolicies.RetryPolicyFactory.Instance(retryPolicyOptions),
                new ThrowingJobHandlerResolver());
        }

        public IPayloadJobFactory PayloadJobFactory(IJobHandlerResolver2 jobHandlerResolver,
            IReadOnlyDictionary<string, RetryPolicyOptions>? options = null)
        {
            if (jobHandlerResolver == null) throw new ArgumentNullException(nameof(jobHandlerResolver));

            var retryPolicyOptions = options ?? new Dictionary<string, RetryPolicyOptions>();
            return Factories.PayloadJobFactory.Create(
                _coreApi.GetJobFactoryCore(),
                _coreApi.GetJobRootFactoryCore(),
                RetryPolicies.RetryPolicyFactory.Instance(retryPolicyOptions),
                jobHandlerResolver);
        }

        public IRetryPolicyFactory RetryPolicyFactory(IReadOnlyDictionary<string, RetryPolicyOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return RetryPolicies.RetryPolicyFactory.Instance(options);
        }

        public ICacheableQFactory CacheableQFactory()
        {
            return Queues.CacheableQFactory.Instance();
        }

        public IQFactoryAdapter QFactory(IReadOnlyDictionary<string, IQOptions> options, IReadOnlyDictionary<string, RetryPolicyOptions>? retryPolicyOptions = null)
        {
            var retryOptions = retryPolicyOptions ?? new Dictionary<string, RetryPolicyOptions>();
            return QFactoryAdapter.Create(_coreApi.GetQFactoryCore(),
                options, RetryPolicies.RetryPolicyFactory.Instance(retryOptions));
        }
    
        public IJobFactory GetJobFactoryCore()
        {
            return _coreApi.GetJobFactoryCore();
        }
        public IJobRootFactory GetJobRootFactoryCore()
        {
            return _coreApi.GetJobRootFactoryCore();
        }

        public IQFactoryCore GetQFactoryCore()
        {
            return _coreApi.GetQFactoryCore();
        }

        public ISystemTextJsonSerializerFactory SystemTexSerializerFactory()
        {
            return JsonSerializerFactory.Create();
        }
    }
}
