using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test
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

        public IPayloadJobFactory GetPayloadJobFactory(IReadOnlyDictionary<string, RetryPolicyOptions> options = null)
        {
            throw new NotImplementedException();
        }

        public ICacheableQFactory GetCacheableQFactory()
        {
            throw new NotImplementedException();
        }

        public IRetryPolicyFactory GetRetryPolicyFactory(IReadOnlyDictionary<string, RetryPolicyOptions> options)
        {
            throw new NotImplementedException();
        }

        public IQFactoryCore GetQFactory(IReadOnlyDictionary<string, IQOptions> options, IRetryPolicyFactory retries)
        {
            throw new NotImplementedException();
        }

        public IJobFactory GetJobFactoryCore()
        {
            throw new NotImplementedException();
        }

        public IJobRootFactory GetJobRootFactoryCore()
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

        public IQFactoryCore GetQFactoryCore()
        {
            throw new NotImplementedException();
        }

        IQFactoryAdapter IApi.GetQFactory(IReadOnlyDictionary<string, IQOptions> options, IReadOnlyDictionary<string, RetryPolicyOptions>? retryPolicyOptions = null)
        {
            throw new NotImplementedException();
        }

        public ISystemTextJsonSerializerFactory GetSystemTextJsonSerializerFactory()
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
            var queueOptions = new Dictionary<string, IQOptions>();

            // Act
            services.AddTplQueue(
                new FakeApi(new DummyCoreApi()),
                retryPolicies,
                queueOptions);

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
            public IQFactoryCore DispatcherFactory => throw new NotImplementedException();
            public IJobRootFactory RunnerRootFactory => throw new NotImplementedException();
            public IReadOnlyDictionary<string, RetryPolicyOptions> RetryPolicies => new Dictionary<string, RetryPolicyOptions>();
            public IReadOnlyDictionary<string, IQOptions> Dispatchers => new Dictionary<string, IQOptions>();

            public IQFactoryCore GetQFactory(IReadOnlyDictionary<string, IQOptions> options, IRetryPolicyFactory retries)
            {
                throw new NotImplementedException();
            }

            public IJobFactory GetJobFactoryCore()
            {
                throw new NotImplementedException();
            }

            public IJobRootFactory GetJobRootFactoryCore()
            {
                throw new NotImplementedException();
            }

            public IQFactoryCore GetQFactoryCore()
            {
                throw new NotImplementedException();
            }
        }
    }
}
