using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Cache;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.RetryPolicies;
using Fmaciasruano.TplQueue.Runner;
using Fmaciasruano.TplQueue.Serialization.SystemTextJson;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json.Serialization;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Cache
{
    public class RecordingPayload : IPayloadCommand
    {
        public List<string> Log { get; set; }
        
        public string Name { get; set; }
        public string HandlerId => "recording";
        
        [JsonConstructor]
        public RecordingPayload(string name)
        {
            Name = name;
            Log = new List<string>();
        }

        public Task ExecuteAsync(CancellationToken ct)
        {
            Log?.Add(Name);
            return Task.CompletedTask;
        }
    }

    [TestFixture]
    public class CacheFactoryTests
    {
        private IPayloadRunnerFactory _payloadRunnerFactory = null!;
        private IUniversalPayloadSerializer _serializer = null!;
        private ICacheFactory _cacheFactory = null!;
        private IRetryPolicyFactory _retryFactory = null!;

        [SetUp]
        public void SetUp()
        {
            _retryFactory = RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>
            {
                { "none", RetryPolicyOptions.Linear(0, 0) }
            });
            _payloadRunnerFactory = PayloadRunnerFactory.Instance(
                TaskRunnerFactory.Instance(),
                TaskRunnerRootFactory.Instance(),
                _retryFactory);
            _serializer = SystemTextJsonUniversalSerializer.Create();
            _cacheFactory = CacheFactory.Instance();
        }

        [Test]
        public void CreateMemCache_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => _cacheFactory.CreateMemCache(null!, _serializer));
            Assert.Throws<ArgumentNullException>(() => _cacheFactory.CreateMemCache(_payloadRunnerFactory, null!));
            Assert.Throws<ArgumentNullException>(() => _cacheFactory.CreateMemCache(null!, null!));
        }

        [Test]
        public async Task CreateMemCache_LeasesGraphWithDependencies()
        {
            var cache = _cacheFactory.CreateMemCache(_payloadRunnerFactory, _serializer);

            var child = _payloadRunnerFactory.Create(new RecordingPayload("child"), _serializer, "child");
            var root = _payloadRunnerFactory.CreateRoot(new RecordingPayload("root"), _serializer, () => _retryFactory.Create("none"), "root");
            root.After(child);

            var entries = cache.Append(root, isFifo: true);

            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(cache.TryLeaseNextRoot(out var leasedRoot, out var lease), Is.True);
            Assert.That(lease.Status, Is.EqualTo(EntryStatus.Pending));
            Assert.That(leasedRoot.Dependencies.Count(), Is.EqualTo(1));

            using var dispatcher = TaskDispatcherFactory.Instance(
                new Dictionary<string, IDispatcherOptions>(), _retryFactory)
                .CreateParallel("cache-factory", 
                () => _retryFactory.Create("none"), maxParallelism: 2, NullLogger.Instance, pulseMs: 5);
            dispatcher.StartPolling();
            leasedRoot.Enqueue(dispatcher, CancellationToken.None);

            await leasedRoot.WaitUntilFinishedAsync();
            var rootPayloadObject = leasedRoot.GetPayload();
            Assert.IsInstanceOf<RecordingPayload>(rootPayloadObject);
            Assert.That(((RecordingPayload)rootPayloadObject).Log[0], Is.EqualTo("root"));

            var childPayloadObject = leasedRoot.GetPayloadDependencies()[0].GetPayload();
            Assert.IsInstanceOf<RecordingPayload>(childPayloadObject);
            Assert.That(((RecordingPayload)childPayloadObject).Log[0], Is.EqualTo("child"));
        }
    }
}


