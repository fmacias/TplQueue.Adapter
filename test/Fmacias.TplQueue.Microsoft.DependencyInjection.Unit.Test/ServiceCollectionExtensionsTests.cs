using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Fmaciasruano.TplQueue.Microsoft.DependencyInjection.Unit.Test
{
    internal sealed class FakeApi : IApi
    {
        public ICoreApi CoreApi { get; }

        public FakeApi(ICoreApi coreApi)
        {
            CoreApi = coreApi ?? throw new ArgumentNullException(nameof(coreApi));
        }

        public IObserverFactory GetObserverFactory()
        {
            throw new NotImplementedException();
        }

        public ICacheFactory GetCacheFactory()
        {
            throw new NotImplementedException();
        }

        public IPayloadRunnerFactory GetPayloadRunnerFactory()
        {
            throw new NotImplementedException();
        }

        public ISerializableDispatcherFactory GetSerializableDispatcherFactory()
        {
            throw new NotImplementedException();
        }

        public IRetryPolicyFactory GetRetryPolicyFactory(IReadOnlyDictionary<string, RetryPolicyOptions> options)
        {
            throw new NotImplementedException();
        }

        public ITaskDispatcherFactory GetTaskDispatcherFactory(IReadOnlyDictionary<string, IDispatcherOptions> options, IRetryPolicyFactory retries)
        {
            throw new NotImplementedException();
        }

        public ITaskRunnerFactory GetTaskRunnerFactory()
        {
            throw new NotImplementedException();
        }

        public ITaskRunnerRootFactory GetTaskRunnerRootFactory()
        {
            throw new NotImplementedException();
        }

        public ICoreApi GetCoreApi()
        {
            return CoreApi; 
        }

        public IRetryPoliciesFacade GetRetryPolicyFacade()
        {
            throw new NotImplementedException();
        }

        public IRetryPolicySerializable GetRetryPolicySerializer()
        {
            throw new NotImplementedException();
        }
    }

    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddTplQueue_RegistersApiAndCoreApi()
        {
            // Arrange
            var services = new ServiceCollection();

            var retryPolicies = new Dictionary<string, RetryPolicyOptions>
            {
                { "default", RetryPolicyOptions.Linear(3, 100) }
            };
            var dispatcherOptions = new Dictionary<string, IDispatcherOptions>();

            // Act
            services.AddTplQueue(
                new FakeApi(new DummyCoreApi()),
                retryPolicies,
                dispatcherOptions);

            var provider = services.BuildServiceProvider();

            // Assert
            var api = provider.GetService<IApi>();
            Assert.IsNotNull(api);
            Assert.IsInstanceOf<FakeApi>(api);

            var coreApi = provider.GetService<ICoreApi>();
            Assert.IsNotNull(coreApi);
        }

        [Test]
        public void AddTplQueue_WhenConfigureApiIsNull_Throws()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() =>
                services.AddTplQueue(_ => { }, null!));
        }

        private sealed class DummyCoreApi : ICoreApi
        {
            public ITaskDispatcherFactory DispatcherFactory => throw new NotImplementedException();
            public ITaskRunnerRootFactory RunnerRootFactory => throw new NotImplementedException();
            public IReadOnlyDictionary<string, RetryPolicyOptions> RetryPolicies => new Dictionary<string, RetryPolicyOptions>();
            public IReadOnlyDictionary<string, IDispatcherOptions> Dispatchers => new Dictionary<string, IDispatcherOptions>();

            public ITaskDispatcherFactory GetTaskDispatcherFactory(IReadOnlyDictionary<string, IDispatcherOptions> options, IRetryPolicyFactory retries)
            {
                throw new NotImplementedException();
            }

            public ITaskRunnerFactory GetTaskRunnerFactory()
            {
                throw new NotImplementedException();
            }

            public ITaskRunnerRootFactory GetTaskRunnerRootFactory()
            {
                throw new NotImplementedException();
            }
        }
    }
}
