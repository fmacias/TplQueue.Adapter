using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Moq;
namespace Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test
{
    internal sealed class FakeCoreApi : ICoreApi
    {
        public IQFactory QFactory => Mock.Of<IQFactory>();

        public IJobFactory JobFactory => Mock.Of<IJobFactory>();

        public IDataJobFactory DataJobFactory => Mock.Of<IDataJobFactory>();
    }

    internal sealed class FakeApi : IApi
    {
        public IRetryPolicyAbstractFactory RetryPolicyAbstractFactory => Mock.Of<IRetryPolicyAbstractFactory>();

        public IJobFactory JobFactory => Mock.Of<IJobFactory>();

        public IDataJobFactory DataJobFactory => Mock.Of<IDataJobFactory>();

        public IQFactoryAdapter QFactory => Mock.Of<IQFactoryAdapter>();

        public IReadOnlyDictionary<string, IRetryPolicyOptions> RetryPolicyOptions => new Dictionary<string, IRetryPolicyOptions>();

        public IReadOnlyDictionary<string, IQOptions> QueueOptions => new Dictionary<string, IQOptions>();

        public IObserverFactory ObserverFactory() => Mock.Of<IObserverFactory>();

        public ISystemTextJsonSerializerFactory SystemTextSerializerFactory() => Mock.Of<ISystemTextJsonSerializerFactory>();

        public IXmlSerializerFactory XmlSerializerFactory() => Mock.Of<IXmlSerializerFactory>();

        public IApi RegisterPayloadHandler(string payloadHandlerKey, IHandler handler)
        {
            return this;
        }

        public IApi RegisterPayloadHandler(string payloadHandlerKey, Func<IHandler> handlerFactory)
        {
            return this;
        }

        public IApi RegisterPayloadHandler(string payloadHandlerKey, Func<IPayload, CancellationToken, Task> handler)
        {
            return this;
        }

        public IApi RegisterPayloadHandler<TPayload>(string payloadHandlerKey, Func<TPayload, CancellationToken, Task> handler)
            where TPayload : IPayload
        {
            return this;
        }

        public T RetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory, string name) where T : IRetryPolicy
        {
            throw new NotImplementedException();
        }

        public T Cache<T>(ICacheFactory<T> cacheFactory, IUniversalDataSerializer serializer)
            where T : IDataJobCache
        {
            throw new NotImplementedException();
        }

        public T Cache<T>(ICacheFactory<T> cacheFactory, IUniversalDataSerializer serializer, ITypeResolver typeResolver)
            where T : IDataJobCache
        {
            throw new NotImplementedException();
        }

        public T RetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory) where T : IRetryPolicy
        {
            throw new NotImplementedException();
        }

        public T RetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory, IRetryPolicyOptions retryPolicyOptions) where T : IRetryPolicy
        {
            throw new NotImplementedException();
        }

        public IExponentialBackoff RetryPolicy(IExponentialBackoffFactory exponentialBackoffFactory, int maxRetries, int delayMs, double factor)
        {
            throw new NotImplementedException();
        }

        public ILinearBackoff RetryPolicy(ILinearBackoffFactory linearBackofFactory, int maxRetries, int delayMs)
        {
            throw new NotImplementedException();
        }
    }

    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddTplQueue_WithDictionaries_RegistersApi()
        {
            var services = new ServiceCollection();
            var retryPolicies = new Dictionary<string, IRetryPolicyOptions>
            {
                { "default", RetryPolicyOptions.Create(100, 3) }
            };
            var queueOptions = new Dictionary<string, IQOptions>();
            var fakeCoreApi = new FakeCoreApi();

            services.AddTplQueue(fakeCoreApi, retryPolicies, queueOptions);
            var provider = services.BuildServiceProvider(); 
            Assert.That(provider.GetService<IApi>(), Is.SameAs(provider.GetService<IApi>()));
        }

        [Test]
        public void AddTplQueue_WithDictionaries_RegistersReadOnlyDictionaries()
        {
            var services = new ServiceCollection();
            var retryPolicies = new Dictionary<string, IRetryPolicyOptions>
            {
                { "default", RetryPolicyOptions.Create(100, 3) }
            };
            var queueOptions = new Dictionary<string, IQOptions>
            {
                { "default", new QOptions(Guid.NewGuid(), 1, "none") }
            };

            services.AddTplQueue(
                new FakeCoreApi(),
                retryPolicies,
                queueOptions);

            var provider = services.BuildServiceProvider();
            var registeredRetries = provider.GetRequiredService<IReadOnlyDictionary<string, IRetryPolicyOptions>>();
            var registeredDispatchers = provider.GetRequiredService<IReadOnlyDictionary<string, IQOptions>>();

            Assert.That(registeredRetries, Is.Not.Null);
            Assert.That(registeredDispatchers, Is.Not.Null);
            Assert.That(registeredRetries!.ContainsKey("default"), Is.True);
            Assert.That(registeredDispatchers!.ContainsKey("default"), Is.True);
        }

        [Test]
        public void AddTplQueue_WithDictionaries_RegistersSerializerFactories()
        {
            var services = new ServiceCollection();

            services.AddTplQueue(
                new FakeCoreApi(),
                new Dictionary<string, IRetryPolicyOptions>(),
                new Dictionary<string, IQOptions>());

            var provider = services.BuildServiceProvider();

            Assert.Multiple(() =>
            {
                Assert.That(provider.GetService<ISystemTextJsonSerializerFactory>(), Is.Not.Null);
                Assert.That(provider.GetService<IXmlSerializerFactory>(), Is.Not.Null);
            });
        }

        [Test]
        public void AddTplQueue_WhenConfigureApiIsNull_Throws()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() => services.AddTplQueue(
                configure: _ => { },
                coreApi: null!));
        }

        [Test]
        public void AddTplQueue_WhenConfigureIsNull_Throws()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() => services.AddTplQueue(null!));
        }

        [Test]
        public void AddTplQueue_WithDictionaries_WhenApiIsNull_Throws()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() => services.AddTplQueue(
                null!,
                new Dictionary<string, IRetryPolicyOptions>(),
                new Dictionary<string, IQOptions>()));
        }
    }
}
