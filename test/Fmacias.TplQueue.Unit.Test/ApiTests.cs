using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Fmacias.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

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
        public void XmlSerializerFactory_ReturnsXmlPackageFactory()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var factory = api.XmlSerializerFactory();
            var serializer = factory.Serializer();

            Assert.Multiple(() =>
            {
                Assert.That(factory.GetType().Assembly.GetName().Name, Is.EqualTo("Fmacias.TplQueue.Serialization.Xml"));
                Assert.That(serializer, Is.InstanceOf<IXmlUniversalSerializer>());
            });
        }

        [Test]
        public void SystemTextSerializerFactory_ReturnsSystemTextJsonPackageFactory()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var factory = api.SystemTextSerializerFactory();
            var serializer = factory.Serializer();

            Assert.Multiple(() =>
            {
                Assert.That(factory.GetType().Assembly.GetName().Name, Is.EqualTo("Fmacias.TplQueue.Serialization.SystemTextJson"));
                Assert.That(serializer, Is.InstanceOf<ISystemTextJsonUniversalSerializer>());
            });
        }

        [Test]
        public void SystemTexSerializerFactory_LegacyTypo_ReturnsSystemTextJsonPackageFactory()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var factory = api.SystemTexSerializerFactory();
            var serializer = factory.Serializer();

            Assert.Multiple(() =>
            {
                Assert.That(factory.GetType().Assembly.GetName().Name, Is.EqualTo("Fmacias.TplQueue.Serialization.SystemTextJson"));
                Assert.That(serializer, Is.InstanceOf<ISystemTextJsonUniversalSerializer>());
            });
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
        public void Cache_WithoutExplicitTypeResolver_UsesFacadeOwnedDefaultResolver()
        {
            ITypeResolver capturedTypeResolver = null!;
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
                    (_, _, typeResolver, _, _) => capturedTypeResolver = typeResolver)
                .Returns(expectedCache);

            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var cache = api.Cache(cacheFactory.Object, Mock.Of<IUniversalDataSerializer>());

            Assert.Multiple(() =>
            {
                Assert.That(cache, Is.SameAs(expectedCache));
                Assert.That(capturedTypeResolver, Is.Not.Null);
                Assert.That(capturedTypeResolver.Resolve(typeof(ApiTests).AssemblyQualifiedName!), Is.EqualTo(typeof(ApiTests)));
            });
        }

        [Test]
        public void Cache_WithExplicitTypeResolver_UsesProvidedResolver()
        {
            ITypeResolver capturedTypeResolver = null!;
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
                    (_, _, typeResolver, _, _) => capturedTypeResolver = typeResolver)
                .Returns(expectedCache);

            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            var cache = api.Cache(cacheFactory.Object, Mock.Of<IUniversalDataSerializer>(), _nodeTypeResolver.Object);

            Assert.Multiple(() =>
            {
                Assert.That(cache, Is.SameAs(expectedCache));
                Assert.That(capturedTypeResolver, Is.SameAs(_nodeTypeResolver.Object));
            });
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
            Assert.Throws<KeyNotFoundException>(() => capturedResolver.Handler("test/api/missing-v1"));
        }

        [Test]
        public void RegisterPayloadHandler_WithHandlerInstance_UsesRegisteredHandlerWhenCreatingCache()
        {
            const string handlerKey = "test/api/instance-v1";
            IApi api = CreateDefaultApi();
            var registeredHandler = Mock.Of<IHandler>();

            api.RegisterPayloadHandler(handlerKey, registeredHandler);

            var resolver = CapturePayloadHandlers(api);

            Assert.That(resolver.Handler(handlerKey), Is.SameAs(registeredHandler));
        }

        [Test]
        public async Task RegisterPayloadHandler_WithFactory_ResolvesHandlersThroughCompositionRoot()
        {
            const string handlerKey = "test/api/factory-v1";
            IApi api = CreateDefaultApi();
            var recorder = new RecordingService();
            var createdHandlers = 0;

            api.RegisterPayloadHandler(handlerKey, () =>
            {
                createdHandlers++;
                return new RecordingHandler(recorder);
            });

            var resolver = CapturePayloadHandlers(api);

            await resolver.Handler(handlerKey).HandleAsync(new TestPayload("first", handlerKey), CancellationToken.None);
            await resolver.Handler(handlerKey).HandleAsync(new TestPayload("second", handlerKey), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(createdHandlers, Is.EqualTo(2));
                Assert.That(recorder.Values, Is.EqualTo(new[] { "first", "second" }));
            });
        }

        [Test]
        public async Task RegisterPayloadHandler_WithUntypedDelegate_ResolvesAndExecutesHandler()
        {
            const string handlerKey = "test/api/untyped-v1";
            IApi api = CreateDefaultApi();
            object? receivedPayload = null;

            api.RegisterPayloadHandler(handlerKey, (payload, ct) =>
            {
                receivedPayload = payload;
                return Task.CompletedTask;
            });

            var resolver = CapturePayloadHandlers(api);
            var payload = new TestPayload("untyped", handlerKey);

            await resolver.Handler(handlerKey).HandleAsync(payload, CancellationToken.None);

            Assert.That(receivedPayload, Is.SameAs(payload));
        }

        [Test]
        public async Task RegisterPayloadHandler_WithTypedDelegate_ResolvesAndExecutesHandler()
        {
            const string handlerKey = "test/api/typed-v1";
            IApi api = CreateDefaultApi();
            var recorder = new RecordingService();

            api.RegisterPayloadHandler<TestPayload>(handlerKey, (payload, ct) =>
            {
                recorder.Values.Add(payload.Value);
                return Task.CompletedTask;
            });

            var resolver = CapturePayloadHandlers(api);

            await resolver.Handler(handlerKey).HandleAsync(new TestPayload("typed", handlerKey), CancellationToken.None);

            Assert.That(recorder.Values, Is.EqualTo(new[] { "typed" }));
        }

        [Test]
        public void RegisterPayloadHandler_WithTypedDelegate_WhenPayloadTypeDoesNotMatch_ThrowsInvalidOperationException()
        {
            const string handlerKey = "test/api/type-check-v1";
            IApi api = CreateDefaultApi();

            api.RegisterPayloadHandler<TestPayload>(handlerKey, (payload, ct) => Task.CompletedTask);

            var resolver = CapturePayloadHandlers(api);
            var handler = resolver.Handler(handlerKey);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await handler.HandleAsync(new OtherPayload(handlerKey), CancellationToken.None));
        }

        [Test]
        public void RegisterPayloadHandler_WhenDuplicateKeyUsesDifferentHandler_ThrowsInvalidOperationException()
        {
            const string handlerKey = "test/api/duplicate-v1";
            IApi api = CreateDefaultApi();

            api.RegisterPayloadHandler(handlerKey, Mock.Of<IHandler>());

            Assert.Throws<InvalidOperationException>(() =>
                api.RegisterPayloadHandler(handlerKey, Mock.Of<IHandler>()));
        }

        private IApi CreateDefaultApi()
        {
            return API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);
        }

        private IPayloadHandlers CapturePayloadHandlers(IApi api)
        {
            IPayloadHandlers capturedResolver = null!;
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

            _ = api.Cache(cacheFactory.Object, Mock.Of<IUniversalDataSerializer>(), _nodeTypeResolver.Object);

            return capturedResolver;
        }

        private sealed class RecordingHandler : IHandler
        {
            private readonly RecordingService _recordingService;

            public RecordingHandler(RecordingService recordingService)
            {
                _recordingService = recordingService ?? throw new ArgumentNullException(nameof(recordingService));
            }

            public Task HandleAsync(IPayload payload, CancellationToken cancellationToken)
            {
                if (!(payload is TestPayload testPayload))
                {
                    throw new InvalidOperationException("Unexpected payload type.");
                }

                _recordingService.Values.Add(testPayload.Value);
                return Task.CompletedTask;
            }
        }

        private sealed class NoopHandler : IHandler
        {
            public Task HandleAsync(IPayload payload, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class RecordingService
        {
            public List<string> Values { get; } = new List<string>();
        }

        private sealed class TestPayload : IPayload
        {
            public TestPayload(string value, string payloadId)
            {
                Value = value;
                PayloadId = payloadId;
            }

            public string Value { get; }
            public string PayloadId { get; }
            public DateTime CollectionTime => DateTime.UtcNow;
        }

        private sealed class OtherPayload : IPayload
        {
            public OtherPayload(string payloadId)
            {
                PayloadId = payloadId;
            }

            public string PayloadId { get; }
            public DateTime CollectionTime => DateTime.UtcNow;
        }

    }
}
