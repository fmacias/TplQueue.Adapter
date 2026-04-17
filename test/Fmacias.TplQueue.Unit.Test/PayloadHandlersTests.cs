using Fmacias.TplQueue.Contracts;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Test
{
    [TestFixture]
    public sealed class PayloadHandlersTests
    {
        [Test]
        public async Task Register_WithHandlerInstance_ResolvesAndExecutesHandler()
        {
            const string handlerKey = "plugins/test/v1";
            var recorder = new RecordingService();
            var payloadHandlers = PayloadHandlers.Create()
                .Register(handlerKey, new RecordingHandler(recorder));

            await payloadHandlers.Handler(handlerKey).HandleAsync(new TestPayload("ok", handlerKey), CancellationToken.None);

            Assert.That(recorder.Values, Is.EqualTo(new[] { "ok" }));
        }

        [Test]
        public async Task Register_WithFactory_ResolvesHandlersThroughCompositionRoot()
        {
            const string handlerKey = "plugins/test/factory-v1";
            var recorder = new RecordingService();
            var createdHandlers = 0;
            var payloadHandlers = PayloadHandlers.Create()
                .Register(handlerKey, () =>
                {
                    createdHandlers++;
                    return new RecordingHandler(recorder);
                });

            await payloadHandlers.Handler(handlerKey).HandleAsync(new TestPayload("first", handlerKey), CancellationToken.None);
            await payloadHandlers.Handler(handlerKey).HandleAsync(new TestPayload("second", handlerKey), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(createdHandlers, Is.EqualTo(2));
                Assert.That(recorder.Values, Is.EqualTo(new[] { "first", "second" }));
            });
        }

        [Test]
        public async Task Register_WithUntypedDelegate_ResolvesAndExecutesHandler()
        {
            const string handlerKey = "plugins/test/untyped-v1";
            object? receivedPayload = null;
            var payloadHandlers = PayloadHandlers.Create()
                .Register(handlerKey, (payload, ct) =>
                {
                    receivedPayload = payload;
                    return Task.CompletedTask;
                });

            var payload = new TestPayload("untyped", handlerKey);
            await payloadHandlers.Handler(handlerKey).HandleAsync(payload, CancellationToken.None);

            Assert.That(receivedPayload, Is.SameAs(payload));
        }

        [Test]
        public void RegisterPlugin_DelegatesRegistrationsToPlugin()
        {
            var payloadHandlers = PayloadHandlers.Create()
                .RegisterPlugin(new TestPlugin());

            var handler = payloadHandlers.Handler("plugins/test/plugin-v1");

            Assert.That(handler, Is.Not.Null);
        }

        [Test]
        public void Register_WhenDuplicateKeyUsesDifferentHandler_ThrowsInvalidOperationException()
        {
            var payloadHandlers = PayloadHandlers.Create()
                .Register("plugins/test/duplicate-v1", (payload, ct) => Task.CompletedTask);

            Assert.Throws<InvalidOperationException>(() =>
                payloadHandlers.Register("plugins/test/duplicate-v1", (payload, ct) => Task.CompletedTask));
        }

        [Test]
        public void Handler_WhenKeyIsMissing_ThrowsKeyNotFoundException()
        {
            var payloadHandlers = PayloadHandlers.Create();

            Assert.Throws<KeyNotFoundException>(() => payloadHandlers.Handler("plugins/test/missing-v1"));
        }

        [Test]
        public void Register_TypedHandler_WhenPayloadTypeDoesNotMatch_ThrowsInvalidOperationException()
        {
            const string handlerKey = "plugins/test/type-check-v1";
            var payloadHandlers = PayloadHandlers.Create()
                .Register<TestPayload>(handlerKey, (payload, ct) => Task.CompletedTask);

            var handler = payloadHandlers.Handler(handlerKey);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await handler.HandleAsync(new OtherPayload(handlerKey), CancellationToken.None));
        }

        private sealed class TestPlugin : IPayloadHandlerPlugin
        {
            public void Register(IPayloadHandlerRegistry registry)
            {
                registry.Register("plugins/test/plugin-v1", new NoopHandler());
            }
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
