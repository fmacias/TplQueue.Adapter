using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Fmacias.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test
{
    [TestFixture]
    public class ApiTests
    {
        private Mock<ICoreApi> _coreApi = null!;
        private Mock<IJobFactory> _jobFactoryMock = null!;
        private Mock<IQFactory> _queueFactoryCoreMock = null!;
        private Mock<ITypeResolver> _nodeTypeResolver = null!;
        private Dictionary<string, IQOptions> _queueOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _jobFactoryMock = new Mock<IJobFactory>();
            _queueFactoryCoreMock = Helper.GetQFactoryCoreMock();
            _nodeTypeResolver = Helper.GetNodeTypeResolverMock();

            _coreApi = Helper.GetCoreApiMock(
                _jobFactoryMock.Object,
                _queueFactoryCoreMock.Object);
            _queueOptions = new Dictionary<string, IQOptions>
            {
                { "default", Mock.Of<IQOptions>(o => o.MaxParallelism == 1 && o.RetryPolicy == "none") }
            };
        }

        [Test]
        public void GetFactories_DelegatesToInnerCoreApi()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            Assert.That(api.JobFactory, Is.SameAs(_jobFactoryMock.Object));
            Assert.That(api.DataJobFactory, Is.SameAs(_coreApi.Object.DataJobFactory));
            Assert.IsInstanceOf<IQFactoryAdapter>(api.QFactory);

            _coreApi.Verify(a => a.JobFactory, Times.AtLeastOnce);
            _coreApi.Verify(a => a.QFactory, Times.Once);
            _coreApi.Verify(a => a.DataJobFactory, Times.AtLeastOnce);
        }

        [Test]
        public void Create_WhenCoreApiIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => API.Create(
                null!,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions));
        }

        [Test]
        public void Create_WhenPayloadHandlersBuilderIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => API.Create(
                _coreApi.Object,
                (PayloadHandlersBuilder)null!,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions));
        }

        [Test]
        public void Create_WhenRetryPolicyOptionsIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => API.Create(
                _coreApi.Object,
                null!,
                _queueOptions));
        }

        [Test]
        public void RetryPolicyFactory_WhenOptionsIsNull_ThrowsArgumentNullException()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            Assert.Throws<ArgumentNullException>(() => api.RetryPolicy<IExponentialBackoff>(null!, "someName"));
        }

        [Test]
        public void RetryPolicyFactory_WithFactory_ReturnsCreatedPolicy()
        {
            var factory = new Mock<IRetryPolicyFactory<IExponentialBackoff>>();
            var expectedPolicy = Mock.Of<IExponentialBackoff>();
            factory.Setup(f => f.CreatePolicy()).Returns(expectedPolicy);

            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var policy = api.RetryPolicy(factory.Object);

            Assert.That(policy, Is.SameAs(expectedPolicy));
        }

        [Test]
        public void RetryPolicyFactory_WithFactory_WhenFactoryIsNull_ThrowsArgumentNullException()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            Assert.Throws<ArgumentNullException>(() => api.RetryPolicy<IExponentialBackoff>(null!));
        }

        [Test]
        public void RetryPolicyFactory_WithOptions_ReturnsCreatedPolicy()
        {
            var expectedPolicy = Mock.Of<IExponentialBackoff>();
            var options = RetryPolicyOptions.Create(250, 4);
            var factory = new Mock<IRetryPolicyFactory<IExponentialBackoff>>();
            factory.Setup(f => f.CreatePolicy(options)).Returns(expectedPolicy);

            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var policy = api.RetryPolicy(factory.Object, options);

            Assert.That(policy, Is.SameAs(expectedPolicy));
        }

        [Test]
        public void RetryPolicyFactory_WithOptions_WhenOptionsIsNull_ThrowsArgumentNullException()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            Assert.Throws<ArgumentNullException>(() => api.RetryPolicy(
                Mock.Of<IRetryPolicyFactory<IExponentialBackoff>>(),
                (IRetryPolicyOptions)null!));
        }

        [Test]
        public void RetryPolicy_WithExponentialFactory_ReturnsConfiguredPolicy()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var policy = api.RetryPolicy((IExponentialBackofFactory)ExponentialBackoffFactory.Create(), 4, 150, 2.5);

            Assert.That(policy.MaxRetries, Is.EqualTo(4));
            Assert.That(policy.Delay.TotalMilliseconds, Is.EqualTo(150).Within(0.1));
            Assert.That(policy.Factor, Is.EqualTo(2.5));
        }

        [Test]
        public void RetryPolicy_WithLinearFactory_ReturnsConfiguredPolicy()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var policy = api.RetryPolicy((ILinearBackoffFactory)LinearBackoffFactory.Create(), 5, 300);

            Assert.That(policy.MaxRetries, Is.EqualTo(5));
            Assert.That(policy.Delay.TotalMilliseconds, Is.EqualTo(300).Within(0.1));
        }

        [Test]
        public void ObserverFactory_ReturnsObserverPackageFactory()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var factory = api.ObserverFactory();
            var loggingObserver = factory.CreateLoggingObserver(Mock.Of<ILogger<ILoggingObserver>>());

            Assert.That(factory.GetType().Assembly.GetName().Name, Is.EqualTo("Fmacias.TplQueue.Observers"));
            Assert.That(loggingObserver, Is.InstanceOf<ILoggingObserver>());
        }

        [Test]
        public void Cache_WithNullTypeResolver_ThrowsArgumentNullException()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            Assert.Throws<ArgumentNullException>(() =>
                api.Cache(Mock.Of<ICacheFactory<IDataJobCache>>(), Mock.Of<IUniversalDataSerializer>(), null!));
        }

        [Test]
        public void Create_WithoutPayloadHandlers_UsesInternalResolverWhenCreatingCache()
        {
            IPayloadHandlers capturedResolver = null!;
            var expectedCache = Mock.Of<IDataJobCache>();
            var cacheFactory = new Mock<ICacheFactory<IDataJobCache>>();
            cacheFactory
                .Setup(f => f.CreateCache(
                    It.IsAny<IUniversalDataSerializer>(),
                    It.IsAny<IDataJobFactory>(),
                    It.IsAny<ITypeResolver>(),
                    It.IsAny<IPayloadHandlers>(),
                    It.IsAny<IRetryPolicyAbstractFactory>()))
                .Callback<IUniversalDataSerializer, IDataJobFactory, ITypeResolver, IPayloadHandlers, IRetryPolicyAbstractFactory>(
                    (_, _, _, payloadHandlers, _) => capturedResolver = payloadHandlers)
                .Returns(expectedCache);

            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var cache = api.Cache(cacheFactory.Object, Mock.Of<IUniversalDataSerializer>(), _nodeTypeResolver.Object);

            Assert.That(cache, Is.SameAs(expectedCache));
            Assert.That(capturedResolver, Is.Not.Null);
            Assert.Throws<KeyNotFoundException>(() => capturedResolver.Handler("plugins/test/missing-v1"));
        }

        [Test]
        public void Create_WithPayloadHandlersBuilder_UsesRegisteredHandlersWhenCreatingCache()
        {
            IPayloadHandlers capturedResolver = null!;
            var registeredHandler = Mock.Of<IHandler>();
            var cacheFactory = new Mock<ICacheFactory<IDataJobCache>>();
            cacheFactory
                .Setup(f => f.CreateCache(
                    It.IsAny<IUniversalDataSerializer>(),
                    It.IsAny<IDataJobFactory>(),
                    It.IsAny<ITypeResolver>(),
                    It.IsAny<IPayloadHandlers>(),
                    It.IsAny<IRetryPolicyAbstractFactory>()))
                .Callback<IUniversalDataSerializer, IDataJobFactory, ITypeResolver, IPayloadHandlers, IRetryPolicyAbstractFactory>(
                    (_, _, _, payloadHandlers, _) => capturedResolver = payloadHandlers)
                .Returns(Mock.Of<IDataJobCache>());

            var payloadHandlersBuilder = PayloadHandlersBuilder.Create()
                .Register("plugins/test/registered-v1", registeredHandler);

            var api = API.Create(
                _coreApi.Object,
                payloadHandlersBuilder,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            _ = api.Cache(cacheFactory.Object, Mock.Of<IUniversalDataSerializer>(), _nodeTypeResolver.Object);

            Assert.That(capturedResolver.Handler("plugins/test/registered-v1"), Is.SameAs(registeredHandler));
        }

    }
}
