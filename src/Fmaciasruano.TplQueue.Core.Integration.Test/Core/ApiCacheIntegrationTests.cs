using Fmaciasruano.TplQueue;
using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core;
using Fmaciasruano.TplQueue.RetryPolicies;
using Fmaciasruano.TplQueue.Serialization.SystemTextJson;
using Fmaciasruano.TplQueue.Runner;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Core
{
    [TestFixture]
    public class ApiCacheIntegrationTests
    {
        private ILoggerFactory _loggerFactory = null!;
        private ILogger<IParallelTaskDispatcher> _logger = null!;
        private Dictionary<string, IDispatcherOptions> _dispatcherOptions = null!;
        private Dictionary<string, RetryPolicyOptions> _retryOptions = null!;
        private IParallelTaskDispatcher? _dispatcher;

        [SetUp]
        public void SetUp()
        {
            _loggerFactory = LoggerFactory.Create(builder => builder.ClearProviders());
            _logger = _loggerFactory.CreateLogger<IParallelTaskDispatcher>();
            _dispatcherOptions = new Dictionary<string, IDispatcherOptions>
            {
                { "main", new IntegrationDispatcherOptions(maxParallelism: 2, pulseMs: 5, retryPolicy: "no-retry") }
            };
            _retryOptions = new Dictionary<string, RetryPolicyOptions>
            {
                { "no-retry", RetryPolicyOptions.Linear(0, 0) }
            };
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher?.Dispose();
            _loggerFactory?.Dispose();
        }
        /// <summary>
        /// This test ilustrates how to use the cache object from a ITaskDispatcher
        /// different than the ISerializableDispatcher. The SerializableDispatcher manages the 
        /// the state of the cache. If you use any other dispatcher, you have to manage the 
        /// states of the cache.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task ApiAndCacheFactory_LeaseAndRunPayloadGraph()
        {
            var api = API.Instance(CoreApi.Instance());
            var retryFactory = api.GetRetryPolicyFactory(_retryOptions);
            var dispatcherFactory = api.GetTaskDispatcherFactory(_dispatcherOptions, retryFactory);
            var payloadRunnerFactory = api.GetPayloadRunnerFactory();
            var cacheFactory = api.GetCacheFactory();

            var serializer = SystemTextJsonUniversalSerializer.Create();
 //           var retrySerializer = api.GetRetryPolicySerializer();
            var cache = cacheFactory.CreateMemCache(payloadRunnerFactory, serializer);

            RecordingPayload.Executions.Clear();
            var child = payloadRunnerFactory.Create(new RecordingPayload { Label = "child" }, serializer, "child");
            var root = payloadRunnerFactory.CreateRoot(new RecordingPayload { Label = "root" }, serializer, () => retryFactory.Create("no-retry"), "root");
            root.After(child);

            cache.Append(root, isFifo: true);

            Assert.That(cache.TryLeaseNextRoot(out var payloadCarrierRoot, out var lease), Is.True);
            var extractedChild = payloadCarrierRoot.Dependencies.First();
            cache.LeaseRootNode(lease);

            _dispatcher = dispatcherFactory.GetDispatcher<IParallelTaskDispatcher>("main", _loggerFactory);
            _dispatcher.StartPolling();
            payloadCarrierRoot.Enqueue(_dispatcher, CancellationToken.None);

            await payloadCarrierRoot.WaitUntilFinishedAsync();
            bool rootSuccessfullyExecuted = payloadCarrierRoot.ExecutionTime > TimeSpan.MinValue;
            bool childSuccessChildfullyExecuted = payloadCarrierRoot.Dependencies.First().ExecutionTime > TimeSpan.MinValue;

            Assert.That(rootSuccessfullyExecuted, Is.True);
            Assert.That(childSuccessChildfullyExecuted, Is.True);

            cache.AckNode(extractedChild.Id, extractedChild.PayloadSerializedData);
            cache.AckNode(payloadCarrierRoot.Id, payloadCarrierRoot.PayloadSerializedData);

            cache.SuccessRootNode(payloadCarrierRoot.Id);
            CollectionAssert.AreEqual(new[] { "child", "root" }, RecordingPayload.Executions.ToArray());

            var childLeasetiem = cache.GetByTaskRunnerId(extractedChild.Id);
            Assert.That(lease.Status, Is.EqualTo(EntryStatus.Acknownledged));
            Assert.That(childLeasetiem.Status, Is.EqualTo(EntryStatus.Acknownledged));
            Assert.That(lease.RootSuccessed, Is.True);
            Assert.That(childLeasetiem.RootSuccessed, Is.True);

        }

        private sealed class IntegrationDispatcherOptions : IDispatcherOptions
        {
            public IntegrationDispatcherOptions(int maxParallelism, int pulseMs, string retryPolicy)
            {
                MaxParallelism = maxParallelism;
                PulseMs = pulseMs;
                RetryPolicy = retryPolicy;
            }

            public int MaxParallelism { get; }
            public int PulseMs { get; }
            public string RetryPolicy { get; }
        }

        private sealed class RecordingPayload : IPayloadCommand
        {
            public static ConcurrentQueue<string> Executions { get; } = new ConcurrentQueue<string>();
            public string Label { get; init; } = string.Empty;
            public string HandlerId => "recording";

            public Task ExecuteAsync(CancellationToken ct)
            {
                Executions.Enqueue(Label);
                return Task.CompletedTask;
            }
        }
    }
}


