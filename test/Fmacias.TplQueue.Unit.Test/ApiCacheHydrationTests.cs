using Fmacias.TplQueue.Cache.MemCache;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Test
{
    [TestFixture]
    public sealed class ApiCacheHydrationTests
    {
        [Test]
        public void CacheHydration_UsesHandlerRegisteredThroughApi()
        {
            const string payloadHandlerKey = "test/cache-api-instance-v1";
            var payload = new TestPayload("root", payloadHandlerKey);
            var registeredHandler = Mock.Of<IHandler>();
            IHandler capturedHandler = null!;
            var rootId = Guid.NewGuid();
            var hydratedRoot = CreateHydratedRoot(payload);
            var dataJobFactory = CreateDataJobFactory(rootId, payload, hydratedRoot.Object, handler => capturedHandler = handler);
            var api = CreateApi(dataJobFactory.Object);
            api.RegisterPayloadHandler(payloadHandlerKey, registeredHandler);
            var cache = CreateCache(api, payload);

            cache.Dehydrate(CreateRoot(rootId, payload).Object, isFifo: false);
            var hydrated = cache.TryHydrateNextJob(out var payloadJobRoot, out var lease);

            Assert.Multiple(() =>
            {
                Assert.That(hydrated, Is.True);
                Assert.That(payloadJobRoot, Is.SameAs(hydratedRoot.Object));
                Assert.That(lease, Is.Not.Null);
                Assert.That(capturedHandler, Is.SameAs(registeredHandler));
            });
        }

        [Test]
        public void CacheHydration_WhenHandlerKeyIsMissing_ThrowsKeyNotFoundException()
        {
            const string payloadHandlerKey = "test/cache-api-missing-v1";
            var payload = new TestPayload("root", payloadHandlerKey);
            var rootId = Guid.NewGuid();
            var dataJobFactory = new Mock<IDataJobFactory>(MockBehavior.Strict);
            var api = CreateApi(dataJobFactory.Object);
            var cache = CreateCache(api, payload);

            cache.Dehydrate(CreateRoot(rootId, payload).Object, isFifo: false);

            Assert.Throws<KeyNotFoundException>(() =>
                cache.TryHydrateNextJob(out _, out _));
        }

        [Test]
        public async Task CacheHydration_UsesTypedHandlerRegisteredThroughApi()
        {
            const string payloadHandlerKey = "test/cache-api-typed-v1";
            var payload = new TestPayload("root", payloadHandlerKey);
            var receivedValues = new List<string>();
            IHandler capturedHandler = null!;
            var rootId = Guid.NewGuid();
            var hydratedRoot = CreateHydratedRoot(payload);
            var dataJobFactory = CreateDataJobFactory(rootId, payload, hydratedRoot.Object, handler => capturedHandler = handler);
            var api = CreateApi(dataJobFactory.Object);
            api.RegisterPayloadHandler<TestPayload>(payloadHandlerKey, (typedPayload, ct) =>
            {
                receivedValues.Add(typedPayload.Value);
                return Task.CompletedTask;
            });
            var cache = CreateCache(api, payload);

            cache.Dehydrate(CreateRoot(rootId, payload).Object, isFifo: false);
            var hydrated = cache.TryHydrateNextJob(out var payloadJobRoot, out _);
            await capturedHandler.HandleAsync(payload, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(hydrated, Is.True);
                Assert.That(payloadJobRoot, Is.SameAs(hydratedRoot.Object));
                Assert.That(receivedValues, Is.EqualTo(new[] { "root" }));
            });
        }

        [Test]
        public void CacheHydration_WithTypedHandler_WhenPayloadTypeDoesNotMatch_ThrowsInvalidOperationException()
        {
            const string payloadHandlerKey = "test/cache-api-type-check-v1";
            var payload = new OtherPayload(payloadHandlerKey);
            IHandler capturedHandler = null!;
            var rootId = Guid.NewGuid();
            var hydratedRoot = CreateHydratedRoot(payload);
            var dataJobFactory = CreateDataJobFactory(rootId, payload, hydratedRoot.Object, handler => capturedHandler = handler);
            var api = CreateApi(dataJobFactory.Object);
            api.RegisterPayloadHandler<TestPayload>(payloadHandlerKey, (typedPayload, ct) => Task.CompletedTask);
            var cache = CreateCache(api, payload);

            cache.Dehydrate(CreateRoot(rootId, payload).Object, isFifo: false);
            _ = cache.TryHydrateNextJob(out _, out _);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await capturedHandler.HandleAsync(payload, CancellationToken.None));
        }

        private static IApi CreateApi(IDataJobFactory dataJobFactory)
        {
            var coreApi = new Mock<ICoreApi>(MockBehavior.Strict);
            coreApi.SetupGet(api => api.DataJobFactory).Returns(dataJobFactory);

            return API.Create(
                coreApi.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                new Dictionary<string, IQOptions>());
        }

        private static IMemCache CreateCache(IApi api, IPayload payload)
        {
            var serializer = new Mock<IUniversalDataSerializer>(MockBehavior.Strict);
            serializer
                .Setup(s => s.Serialize(It.IsAny<object>(), It.IsAny<Type>()))
                .Returns("{}");
            serializer
                .Setup(s => s.Deserialize("{}", payload.GetType()))
                .Returns(payload);

            var typeResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            typeResolver
                .Setup(r => r.Resolve(payload.GetType().AssemblyQualifiedName!))
                .Returns(payload.GetType());

            return api.Cache(MemCacheFactory.Create(), serializer.Object, typeResolver.Object);
        }

        private static Mock<IDataJobFactory> CreateDataJobFactory(
            Guid rootId,
            IPayload payload,
            IDataJobRoot<IPayload> hydratedRoot,
            Action<IHandler> onHandlerCaptured)
        {
            var dataJobFactory = new Mock<IDataJobFactory>(MockBehavior.Strict);
            dataJobFactory
                .Setup(f => f.DataJobRoot<IPayload>(
                    rootId,
                    It.Is<IPayload>(candidate => ReferenceEquals(candidate, payload)),
                    It.IsAny<IHandler>(),
                    "root",
                    It.IsAny<Func<IRetryPolicy>>()))
                .Callback<Guid, IPayload, IHandler, string, Func<IRetryPolicy>>(
                    (_, _, handler, _, _) => onHandlerCaptured(handler))
                .Returns(hydratedRoot);

            return dataJobFactory;
        }

        private static Mock<IDataJobRoot<TPayload>> CreateRoot<TPayload>(
            Guid rootId,
            TPayload payload)
            where TPayload : IPayload
        {
            var retryPolicy = new Func<IRetryPolicy>(() => NoRetryPolicy.Create());
            var root = new Mock<IDataJobRoot<TPayload>>(MockBehavior.Strict);
            root.SetupGet(r => r.Id).Returns(rootId);
            root.SetupGet(r => r.Name).Returns("root");
            root.SetupGet(r => r.Payload).Returns(payload);
            root.As<IDataJobNode>().SetupGet(r => r.PayloadType).Returns(typeof(TPayload));
            root.As<IDataJobInfo>().SetupGet(r => r.PayloadHandlerKey).Returns(payload.PayloadId);
            root.As<IDataJobNode>().Setup(r => r.GetPayload()).Returns(payload);
            root.As<IDataJobNode>().Setup(r => r.GetDependentDataJobs()).Returns(Array.Empty<IDataJob>());
            root.Setup(r => r.GetRetryPolicyFactory()).Returns(retryPolicy);
            root.As<ISerializable>()
                .Setup(r => r.Serialize(It.IsAny<IUniversalDataSerializer>()))
                .Returns("{}");

            return root;
        }

        private static Mock<IDataJobRoot<IPayload>> CreateHydratedRoot(IPayload payload)
        {
            var root = new Mock<IDataJobRoot<IPayload>>(MockBehavior.Strict);
            root.SetupGet(r => r.Payload).Returns(payload);
            root.Setup(r => r.After(It.IsAny<IJobNode[]>())).Returns(root.Object);
            return root;
        }

        public sealed class TestPayload : IPayload
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

        public sealed class OtherPayload : IPayload
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
