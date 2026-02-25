using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Moq;
namespace Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test
{
    internal sealed class FakeApi : IApi
    {
        public ICoreApi CoreApi { get; }

        public IPayloadHandlerResolver PayloadHandlerResolver => throw new NotImplementedException();

        public IRetryPolicyGenericFactory RetryPolicyGenericFactory => throw new NotImplementedException();

        Lazy<IJobRootFactory> IApi.JobRootFactory => throw new NotImplementedException();

        Lazy<IJobFactory> IApi.JobFactory => throw new NotImplementedException();

        public Lazy<IDataJobFactory> DataJobFactory => throw new NotImplementedException();

        public Lazy<ICacheQFactory> CacheQFactory => throw new NotImplementedException();

        public Lazy<ICoreQFactoryAdapter> CoreQFactories => throw new NotImplementedException();

        public IReadOnlyDictionary<string, IRetryPolicyDescriptor> RetryPolicyOptions => throw new NotImplementedException();

        public IReadOnlyDictionary<string, IQOptions> QueueOptions => throw new NotImplementedException();

        public FakeApi(ICoreApi coreApi)
        {
            CoreApi = coreApi ?? throw new ArgumentNullException(nameof(coreApi));
        }

        public IObserverFactory ObserverFactory() => throw new NotImplementedException();

        public IDataJobFactory PayloadJobFactory(
            IReadOnlyDictionary<string, RetryPolicyOptions>? options = null) => throw new NotImplementedException();

        public IDataJobFactory PayloadJobFactory(
            IPayloadHandlerResolver jobHandlerResolver,
            IReadOnlyDictionary<string, RetryPolicyOptions>? options = null) => throw new NotImplementedException();

        public ICacheQFactory CacheableQFactory() => throw new NotImplementedException();

        public ICoreApi GetCoreApi() => CoreApi;

        public ICoreQFactoryAdapter QFactory(
            IReadOnlyDictionary<string, IQOptions> options,
            IReadOnlyDictionary<string, RetryPolicyOptions>? retryPolicyOptions = null) => throw new NotImplementedException();

        public ISystemTextJsonSerializerFactory SystemTexSerializerFactory() => throw new NotImplementedException();

        public T RetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory, string name) where T : IRetryPolicy
        {
            throw new NotImplementedException();
        }

        public T CreateRetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory, IRetryPolicyDescriptor options) where T : IRetryPolicy
        {
            throw new NotImplementedException();
        }

        public IRetryPolicyGenericFactory RetryPolicyAbstractFactory()
        {
            throw new NotImplementedException();
        }

        public IDataJobFactory PayloadJobFactory()
        {
            throw new NotImplementedException();
        }

        public ICoreQFactoryAdapter QFactory()
        {
            throw new NotImplementedException();
        }

        public T Cache<T>(ICacheFactory<T> cacheFactory, IUniversalDataSerializer serializer, INodeTypeResolver typeResolver, IPayloadHandlerResolver payloadHandlerResolver) where T : IDataJobCache
        {
            throw new NotImplementedException();
        }

        public T NodeTypeResolver<T>(INodeTypeResolverFactory<T> resolverFactory) where T : INodeTypeResolver
        {
            throw new NotImplementedException();
        }

        IDataJobFactory IApi.DataJobFactory(IPayloadHandlerResolver payloadHandlerResolver)
        {
            throw new NotImplementedException();
        }
    }

    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddTplQueue_WithDictionaries_RegistersApiAndCoreApi()
        {
            var services = new ServiceCollection();
            var retryPolicies = new Dictionary<string, IRetryPolicyDescriptor>
            {
                { "default", RetryPolicyOptions.Create(100,3) }
            };
            var queueOptions = new Dictionary<string, IQOptions>();
            var fakeApi = new FakeApi(new DummyCoreApi());

            services.AddTplQueue(fakeApi, retryPolicies, queueOptions);
            var provider = services.BuildServiceProvider();

            Assert.That(provider.GetService<IApi>(), Is.SameAs(fakeApi));
            Assert.That(provider.GetService<ICoreApi>(), Is.Not.Null);
        }

        [Test]
        public void AddTplQueue_WithDictionaries_RegistersReadOnlyDictionaries()
        {
            var services = new ServiceCollection();
            var retryPolicies = new Dictionary<string, IRetryPolicyDescriptor>
            {
                { "default", RetryPolicyOptions.Create(100,3) }
            };
            var queueOptions = new Dictionary<string, IQOptions>
            {
                { "default", Mock.Of<IQOptions>() }
            };

            services.AddTplQueue(
                new FakeApi(new DummyCoreApi()),
                retryPolicies,
                queueOptions);

            var provider = services.BuildServiceProvider();
            var registeredRetries = provider.GetService<IReadOnlyDictionary<string, RetryPolicyOptions>>();
            var registeredDispatchers = provider.GetService<IReadOnlyDictionary<string, IQOptions>>();

            Assert.That(registeredRetries, Is.Not.Null);
            Assert.That(registeredDispatchers, Is.Not.Null);
            Assert.That(registeredRetries!.ContainsKey("default"), Is.True);
            Assert.That(registeredDispatchers!.ContainsKey("default"), Is.True);
        }

        [Test]
        public void AddTplQueue_WhenConfigureApiIsNull_Throws()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => services.AddTplQueue(_ => { }, null!));
        }

        [Test]
        public void AddTplQueue_WithDictionaries_WhenApiIsNull_Throws()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => services.AddTplQueue(
                null!,
                new Dictionary<string, IRetryPolicyDescriptor>(),
                new Dictionary<string, IQOptions>()));
        }

        private sealed class DummyCoreApi : ICoreApi
        {

            public ICoreQFactory QFactory => Mock.Of<ICoreQFactory>();

            public IJobFactory JobFactory => Mock.Of<IJobFactory>();

            public IJobRootFactory JobRootFactory => Mock.Of<IJobRootFactory>();
        }
    }
}
