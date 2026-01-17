using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.Core.Runners;
using Fmaciasruano.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Runners
{
    [TestFixture]
    public class TaskRunnerRootIntegrationTests
    {
        private IParallelTaskDispatcher _dispatcher = null!;
        private ILogger<IParallelTaskDispatcher> _logger = null!;
        private IRetryPolicyFactory _retryPolicyFactory = null!;
        private ITaskDispatcherFactory _taskDispatcherFactory = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _logger = Helper.GetLogger<IParallelTaskDispatcher>();
            _retryPolicyFactory = RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>());
            _taskDispatcherFactory = TaskDispatcherFactory.Instance(new Dictionary<string, IDispatcherOptions>(), _retryPolicyFactory);
        }

        [SetUp]
        public void SetUp()
        {
            var dispatcherPolicyFactory = () => _retryPolicyFactory.CreateNoRetryPolicy();
            _dispatcher = _taskDispatcherFactory.CreateParallel(
                "root-runner-integration-dispatcher",
                dispatcherPolicyFactory,
                maxParallelism: 2,
                _logger,
                pulseMs: 25);
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher.Dispose();
        }

        [Test]
        public async Task EnqueueRoot_UsesRootRetryPolicyInsteadOfDispatcherDefault()
        {
            int dispatcherPolicyCalls = 0;
            int rootPolicyCalls = 0;

            Func<IRetryPolicy> dispatcherPolicy = () => new CountingRetryPolicy(() => dispatcherPolicyCalls++);
            Func<IRetryPolicy> rootPolicy = () => new CountingRetryPolicy(() => rootPolicyCalls++);

            _dispatcher.Dispose();
            _dispatcher = _taskDispatcherFactory.CreateParallel(
                "root-policy-test",
                dispatcherPolicy,
                maxParallelism: 1,
                _logger,
                pulseMs: 15);

            var root = TaskRunnerRoot.Create(ct => Task.CompletedTask, rootPolicy, "root");
            var child = TaskRunnerFactory.Instance().Create(ct => Task.CompletedTask, "child");
            root.After(child);

            _dispatcher.Enqueue(root, CancellationToken.None);
            _dispatcher.StartPolling();

            await root.WaitUntilFinishedAsync();

            Assert.That(rootPolicyCalls, Is.GreaterThanOrEqualTo(1), "Root-specific policy should be invoked.");
            Assert.That(dispatcherPolicyCalls, Is.EqualTo(0), "Dispatcher default policy should not be used when root provides one.");
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

