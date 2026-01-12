using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Cache;
using Fmaciasruano.TplQueue.Observers;
using Fmaciasruano.TplQueue.Queues;
using Fmaciasruano.TplQueue.RetryPolicies;
using Fmaciasruano.TplQueue.Runner;
using System;
using System.Collections.Generic;

namespace Fmaciasruano.TplQueue
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
        public IPayloadRunnerFactory GetPayloadRunnerFactory()
        {
            var retryFactory = RetryPolicyFactory.Instance(
                new Dictionary<string, RetryPolicyOptions>());

            return PayloadRunnerFactory.Instance(
                _coreApi.GetTaskRunnerFactory(),
                _coreApi.GetTaskRunnerRootFactory(),
                retryFactory);
        }

        public IRetryPolicyFactory GetRetryPolicyFactory(IReadOnlyDictionary<string, RetryPolicyOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return RetryPolicyFactory.Instance(options);
        }

        public ISerializableDispatcherFactory GetSerializableDispatcherFactory()
        {
            return SerializableDispatcherFactory.Instance();
        }

        public ITaskDispatcherFactory GetTaskDispatcherFactory(IReadOnlyDictionary<string, IDispatcherOptions> options, IRetryPolicyFactory retries)
        {
            return _coreApi.GetTaskDispatcherFactory(options, retries);
        }
        public ITaskRunnerFactory GetTaskRunnerFactory()
        {
            return _coreApi.GetTaskRunnerFactory();
        }
        public ITaskRunnerRootFactory GetTaskRunnerRootFactory()
        {
            return _coreApi.GetTaskRunnerRootFactory();
        }
    }
}
