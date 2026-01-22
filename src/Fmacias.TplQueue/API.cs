using Fmacias.TplQueue.Cache;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Jobs;
using Fmacias.TplQueue.Observers;
using Fmacias.TplQueue.Queues;
using Fmacias.TplQueue.RetryPolicies;
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
        public static IApi Instance(ICoreApi api)
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
        public IPayloadJobFactory GetPayloadJobFactory()
        {
            var retryFactory = RetryPolicyFactory.Instance(
                new Dictionary<string, RetryPolicyOptions>());

            return PayloadRunnerFactory.Instance(
                _coreApi.GetJobFactory(),
                _coreApi.GetJobRootFactory(),
                retryFactory);
        }

        public IRetryPolicyFactory GetRetryPolicyFactory(IReadOnlyDictionary<string, RetryPolicyOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return RetryPolicyFactory.Instance(options);
        }

        public ICacheableQFactory GetSerializableDispatcherFactory()
        {
            return CacheableQFactory.Instance();
        }

        public IQFactory GetTaskDispatcherFactory(IReadOnlyDictionary<string, IQOptions> options, IRetryPolicyFactory retries)
        {
            return _coreApi.GetTaskDispatcherFactory(options, retries);
        }
        public IJobFactory GetJobFactory()
        {
            return _coreApi.GetJobFactory();
        }
        public IJobRootFactory GetJobRootFactory()
        {
            return _coreApi.GetJobRootFactory();
        }
    }
}
