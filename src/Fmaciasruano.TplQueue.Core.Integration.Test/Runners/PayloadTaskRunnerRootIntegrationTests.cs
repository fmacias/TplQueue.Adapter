using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.RetryPolicies;
using Fmaciasruano.TplQueue.Serialization.SystemTextJson;
using Fmaciasruano.TplQueue.Runner;
using Microsoft.Extensions.Logging;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Runners
{
    [TestFixture]
    public class PayloadTaskRunnerRootIntegrationTests
    {
        private IParallelTaskDispatcher _dispatcher = null!;
        private ILogger<IParallelTaskDispatcher> _logger = null!;
        private IPayloadRunnerFactory _payloadRunnerFactory = null!;
        private ITaskDispatcherFactory _taskDispatcherFactory = null!;
        private IRetryPolicyFactory _retryPolicyFactory = null!;
        private IUniversalPayloadSerializer _serializer = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _logger = Helper.GetLogger<IParallelTaskDispatcher>();
            _retryPolicyFactory = RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>());
            _taskDispatcherFactory = TaskDispatcherFactory.Instance(new Dictionary<string, IDispatcherOptions>(), _retryPolicyFactory);
            _serializer = SystemTextJsonUniversalSerializer.Create();
            var retryFactory = RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>
            {
                { "none", RetryPolicyOptions.Linear(0, 0) }
            });
            _payloadRunnerFactory = PayloadRunnerFactory.Instance(
                TaskRunnerFactory.Instance(),
                TaskRunnerRootFactory.Instance(),
                retryFactory);
        }

        [SetUp]
        public void SetUp()
        {
            var noRetry = () => _retryPolicyFactory.CreateNoRetryPolicy();
            _dispatcher = _taskDispatcherFactory.CreateParallel(
                "payload-root-dispatcher",
                noRetry,
                maxParallelism: 2,
                _logger,
                pulseMs: 20);
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher.Dispose();
        }

        [Test]
        public async Task EnqueuePayloadRoot_WithDependency_ExecutesInOrder()
        {
            var execution = new List<string>();
            var rootPayload = new DummyPayload(execution, "root");
            var childPayload = new DummyPayload(execution, "child");

            var child = _payloadRunnerFactory.Create(childPayload, _serializer, "child-node");
            var root = _payloadRunnerFactory.CreateRoot(rootPayload, _serializer, retryPolicyFactory: () => NoRetryPolicy.Create(), name: "root-node");
            root.After(child);

            _dispatcher.Enqueue(root, CancellationToken.None);
            _dispatcher.StartPolling();

            await root.WaitUntilFinishedAsync();
            CollectionAssert.AreEqual(new[] { "child", "root" }, execution);
            Assert.That(root.PayloadSerializedData.JsonInput, Does.Contain("dummy-handler"));
        }

        [Test]
        public async Task EnqueuePayloadRoot_UsesRootRetryPolicy()
        {
            int dispatcherCalls = 0;
            int rootCalls = 0;

            Func<IRetryPolicy> dispatcherPolicy = () => new CountingRetryPolicy(() => dispatcherCalls++);
            Func<IRetryPolicy> rootPolicy = () => new CountingRetryPolicy(() => rootCalls++);

            _dispatcher.Dispose();
            _dispatcher = _taskDispatcherFactory.CreateParallel(
                "payload-root-dispatcher-policy",
                dispatcherPolicy,
                maxParallelism: 1,
                _logger,
                pulseMs: 15);

            var payload = new DummyPayload(new List<string>(), "root");
            var root = _payloadRunnerFactory.CreateRoot(payload, _serializer, rootPolicy, "payload-root");

            _dispatcher.Enqueue(root, CancellationToken.None);
            _dispatcher.StartPolling();

            await root.WaitUntilFinishedAsync();
            Assert.That(rootCalls, Is.GreaterThanOrEqualTo(1), "Root retry policy should be invoked.");
            Assert.That(dispatcherCalls, Is.EqualTo(0), "Dispatcher policy must not override root policy.");
        }

        private sealed class DummyPayload : IPayloadCommand
        {
            private readonly IList<string> _execution;
            private readonly string _marker;

            public DummyPayload(IList<string> execution, string marker)
            {
                _execution = execution;
                _marker = marker;
            }

            public string HandlerId => "dummy-handler";

            public Task ExecuteAsync(CancellationToken ct)
            {
                _execution.Add(_marker);
                return Task.CompletedTask;
            }
        }

        private sealed class CountingRetryPolicy : IRetryPolicy
        {
            private readonly Action _onExecute;

            public CountingRetryPolicy(Action onExecute)
            {
                _onExecute = onExecute;
            }

            public int RetryCount { get; private set; }

            public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken)
            {
                RetryCount++;
                _onExecute?.Invoke();
                return action(cancellationToken);
            }

            public Func<IRetryPolicy> FromDescriptor(IRetryPolicyDescriptor descriptor)
            {
                throw new NotImplementedException();
            }

            public IRetryPolicy SetFromDescriptor(IRetryPolicyDescriptor descriptor)
            {
                throw new NotImplementedException();
            }

            public IRetryPolicy SetFromOptions(RetryPolicyOptions options)
            {
                throw new NotImplementedException();
            }

            public IRetryPolicyDescriptor ToDescriptor()
            {
                throw new NotImplementedException();
            }
        }
    }
}


