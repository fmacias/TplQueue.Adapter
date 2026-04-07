using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Moq;
namespace Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test
{
    internal sealed class FakeApi : IApi
    {
        public IRetryPolicyAbstractFactory RetryPolicyAbstractFactory => Mock.Of<IRetryPolicyAbstractFactory>();

        public IJobFactory JobFactory => Mock.Of<IJobFactory>();

        public IDataJobFactory DataJobFactory => Mock.Of<IDataJobFactory>();

        public IQFactoryAdapter QFactory => Mock.Of<IQFactoryAdapter>();

        public IReadOnlyDictionary<string, IRetryPolicyOptions> RetryPolicyOptions => new Dictionary<string, IRetryPolicyOptions>();

        public IReadOnlyDictionary<string, IQOptions> QueueOptions => new Dictionary<string, IQOptions>();

        public IObserverFactory ObserverFactory() => Mock.Of<IObserverFactory>();

        public ISystemTextJsonSerializerFactory SystemTexSerializerFactory() => Mock.Of<ISystemTextJsonSerializerFactory>();

        public T RetryPolicy<T>(IRetryPolicyFactory<T> retryPolicyFactory, string name) where T : IRetryPolicy
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

        public IExponentialBackoff RetryPolicy(IExponentialBackofFactory exponentialBackofFactory, int maxRetries, int delayMs, double factor)
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
            var fakeApi = new FakeApi();

            services.AddTplQueue(fakeApi, retryPolicies, queueOptions);
            var provider = services.BuildServiceProvider();

            Assert.That(provider.GetService<IApi>(), Is.SameAs(fakeApi));
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
                { "default", Mock.Of<IQOptions>() }
            };

            services.AddTplQueue(
                new FakeApi(),
                retryPolicies,
                queueOptions);

            var provider = services.BuildServiceProvider();
            var registeredRetries = provider.GetService<IReadOnlyDictionary<string, IRetryPolicyOptions>>();
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

            Assert.Throws<ArgumentNullException>(() => services.AddTplQueue(
                configure: _ => { },
                apiImplementation: null!));
        }

        [Test]
        public void AddTplQueue_WhenConfigureIsNull_Throws()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() => services.AddTplQueue(
                configure: null!,
                apiImplementation: new FakeApi()));
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
