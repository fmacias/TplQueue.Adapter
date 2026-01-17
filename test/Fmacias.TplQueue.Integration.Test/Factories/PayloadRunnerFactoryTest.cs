using System.Linq;
using System.Text.Json.Serialization;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Cache;
using Fmaciasruano.TplQueue.Cache.Abstract;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.RetryPolicies;
using Fmaciasruano.TplQueue.Runner;
using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Serialization.SystemTextJson;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Factories
{
    internal class FakePayload : IPayloadCommand
    {
        public string HandlerId => "handler-1";

        [JsonIgnore]
        public int x { get; set; }
        
        [JsonConstructor]
        public FakePayload(int x = 5)
        {
            x = 5;
        }
        public Task ExecuteAsync(CancellationToken ct)
        {
            x = 6;
            return Task.CompletedTask;
        }
    }

    [TestFixture]
    public class PayloadRunnerFactoryTests
    {
        private IPayloadRunnerFactory _payloadRunnerFactory = null!;
        private IUniversalPayloadSerializer _universalPayloadSerializer = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var retryFactory = RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>
            {
                { "none", RetryPolicyOptions.Linear(0, 0) }
            });
            var dispatcherOptions = new Dictionary<string, IDispatcherOptions>();
            _payloadRunnerFactory = PayloadRunnerFactory.Instance(
                TaskRunnerFactory.Instance(),
                TaskRunnerRootFactory.Instance(),
                retryFactory);
            _universalPayloadSerializer = SystemTextJsonUniversalSerializer.Create();
        }

        [Test]
        public void Create_WithPayload_ReturnsPayloadTaskRunner()
        {
            var payload = new FakePayload();

            var runner = _payloadRunnerFactory.Create<FakePayload>(payload, 
                _universalPayloadSerializer, "job-name");

            Assert.That(runner, Is.Not.Null);
            Assert.That(runner.Payload, Is.SameAs(payload));
            Assert.That(runner.Name, Is.EqualTo("job-name"));
            Assert.That(runner.PayloadType, Is.EqualTo(typeof(FakePayload)));
        }

        [Test]
        public void CreateRoot_WithPayload_ReturnsPayloadTaskRunnerRoot()
        {
            var payload = new FakePayload();

            var root = _payloadRunnerFactory.CreateRoot(payload, 
                _universalPayloadSerializer, retryPolicyFactory: () => NoRetryPolicy.Create(), 
                name: "root-job");

            Assert.That(root, Is.Not.Null);
            Assert.That(root.Payload, Is.SameAs(payload));
            Assert.That(root.Name, Is.EqualTo("root-job"));
        }

        [Test]
        public void Load_UsesReflection_ToRehydratePayloadRunner()
        {
            var cache = MemCache.Create(_payloadRunnerFactory, _universalPayloadSerializer);
            var payload = new FakePayload();
            var root = _payloadRunnerFactory.CreateRoot(payload,
                _universalPayloadSerializer,
                retryPolicyFactory: () => NoRetryPolicy.Create(),
                name: "root-job");

            var child = _payloadRunnerFactory.Create(
                payload,
                _universalPayloadSerializer,
                name: "rehydrated");

            root.After(child);

            cache.Append(root, isFifo: false);

            Assert.That(cache.TryLeaseNextRoot(out var payloadCarrierRoot, out var lease), Is.True);

            var childEntry = cache.GetByTaskRunnerId(child.Id);
            Assert.That(childEntry, Is.Not.Null, "Child entry should be present in cache after append.");

            var carrier = _payloadRunnerFactory.Load(childEntry, _universalPayloadSerializer);
            Assert.That(carrier, Is.Not.Null);
            Assert.That(carrier.PayloadType, Is.EqualTo(typeof(FakePayload)));
            Assert.That(carrier.Name, Is.EqualTo("rehydrated"));
            Assert.That(payloadCarrierRoot.Id, Is.EqualTo(root.Id));
            Assert.That(payloadCarrierRoot.GetPayloadDependencies().Select(c => c.Id), Does.Contain(child.Id));
        }
    }
}


