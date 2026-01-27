using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Factories;
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
        public ICacheFactory GetCacheFactory()
        {
            return CacheFactory.Instance();
        }

        public ICoreApi GetCoreApi()
        {
            return _coreApi;
        }

        public IObserverFactory GetObserverFactory()
        {
            return ObserverFactory.Instance();
        }
        public IPayloadJobFactory GetPayloadJobFactory(IReadOnlyDictionary<string, RetryPolicyOptions>? options = null)
        {
            var retryPolicyOptions = options ?? new Dictionary<string, RetryPolicyOptions>();
            return PayloadRunnerFactory.Instance(
                _coreApi.GetJobFactoryCore(),
                _coreApi.GetJobRootFactoryCore(),
                RetryPolicyFactory.Instance(retryPolicyOptions));
        }

        public IRetryPolicyFactory GetRetryPolicyFactory(IReadOnlyDictionary<string, RetryPolicyOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return RetryPolicyFactory.Instance(options);
        }

        public ICacheableQFactory GetCacheableQFactory()
        {
            return CacheableQFactory.Instance();
        }

        public IQFactoryAdapter GetQFactory(IReadOnlyDictionary<string, IQOptions> options, IReadOnlyDictionary<string, RetryPolicyOptions>? retryPolicyOptions = null)
        {
            var retryOptions = retryPolicyOptions ?? new Dictionary<string, RetryPolicyOptions>();
            return QFactoryAdapter.Create(_coreApi.GetQFactoryCore(),
                options, RetryPolicyFactory.Instance(retryOptions));
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

        public ISystemTextJsonSerializerFactory GetSystemTextJsonSerializerFactory()
        {
            return JsonSerializerFactory.Create();
        }
    }
}
