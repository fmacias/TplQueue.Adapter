using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.Core.Runners;
using Fmaciasruano.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Runners
{
    [TestFixture]
    public class TaskRunnerIntegrationTests
    {
        private IParallelTaskDispatcher _dispatcher = null!;
        private ILogger<IParallelTaskDispatcher> _logger = null!;
        private IRetryPolicyFactory _retryPolicyFactory = null!;
        private ITaskDispatcherFactory _taskDispatcherFactory = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _logger = Helper.GetLogger<IParallelTaskDispatcher>();
            var retryPolicyOptions = new Dictionary<string, RetryPolicyOptions>();
            _retryPolicyFactory = RetryPolicyFactory.Instance(retryPolicyOptions);

            var dispatcherOptions = new Dictionary<string, IDispatcherOptions>();
            _taskDispatcherFactory = TaskDispatcherFactory.Instance(dispatcherOptions, _retryPolicyFactory);
        }

        [SetUp]
        public void SetUp()
        {
            var noRetryPolicyFactory = () => _retryPolicyFactory.CreateNoRetryPolicy();
            _dispatcher = _taskDispatcherFactory.CreateParallel(
                "Runner integration dispatcher",
                noRetryPolicyFactory,
                maxParallelism: 4,
                _logger,
                pulseMs: 25);
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher.Dispose();
        }

        [Test]
        public async Task EnqueueRoot_WithDependency_ExecutesGraphInOrder()
        {
            List<string> execution = new();

            var child = TaskRunner.Create(ct => execution.Add("child"), "child");
            var root = TaskRunnerRoot.Create(ct => execution.Add("root"), name: "root");
            root.After(child);

            _dispatcher.Enqueue(root, CancellationToken.None);
            _dispatcher.StartPolling();

            await Task.WhenAll(
                root.WaitUntilFinishedAsync(),
                child.WaitUntilFinishedAsync()).WaitAsync(TimeSpan.FromSeconds(5));

            CollectionAssert.AreEqual(new[] { "child", "root" }, execution);
        }

        [Test]
        public async Task EnqueueRoot_CancellationToken_PreventsExecution()
        {
            bool executed = false;
            using var cts = new CancellationTokenSource();

            var root = TaskRunnerRoot.Create(ct => executed = true, name: "cancel-root");

            _dispatcher.Enqueue(root, cts.Token);
            _dispatcher.StartPolling();

            cts.Cancel();

            var waitTask = root.WaitUntilFinishedAsync();
            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromMilliseconds(250)));

            Assert.That(completed, Is.Not.SameAs(waitTask), "Runner should not complete when cancelled.");
            Assert.IsFalse(waitTask.IsCompleted);
            Assert.IsFalse(executed);
        }
    }
}

