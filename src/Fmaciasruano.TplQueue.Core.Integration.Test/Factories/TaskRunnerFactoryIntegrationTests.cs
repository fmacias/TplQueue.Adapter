using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.Core.Runners.Internals;
using Fmaciasruano.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Factories
{
    [TestFixture]
    public class TaskRunnerFactoryIntegrationTests
    {
        private ITaskRunnerFactory _runnerFactory = null!;
        private ITaskRunnerRootFactory _rootFactory = null!;
        private ITaskDispatcherFactory _dispatcherFactory = null!;
        private ILogger<IParallelTaskDispatcher> _logger = null!;

        [SetUp]
        public void SetUp()
        {
            _runnerFactory = TaskRunnerFactory.Instance();
            _rootFactory = TaskRunnerRootFactory.Instance();
            _dispatcherFactory = TaskDispatcherFactory.Instance(
                new Dictionary<string, IDispatcherOptions>(),
                RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>()));
            _logger = Helper.GetLogger<IParallelTaskDispatcher>();
        }

        [Test]
        public async Task CreateGraph_WithFactories_DispatcherExecutesInDependencyOrder()
        {
            // Arrange
            var dispatcher = _dispatcherFactory.CreateParallel(
                name: "order",
                retryPolicyFactory: () => NoRetryPolicy.Create(),
                maxParallelism: 2,
                logger: _logger,
                pulseMs: 10);

            var execution = new List<string>();

            var child1 = _runnerFactory.Create(ct => execution.Add("child-1"), name: "child-1");
            var child2 = _runnerFactory.Create(ct => execution.Add("child-2"), name: "child-2");
            var root = _rootFactory.Create(ct => execution.Add("root"), name: "root");

            root.After(child2);
            child2.After(child1);

            // Act
            dispatcher.Enqueue(root, CancellationToken.None);
            dispatcher.StartPolling();

            /*
             * ALTERNATIVE: await for all the runners in this manner
             * 
             * But it is not neccessary, because the Root element will always be the last runner to be executed.
             * 
             * Example to away explicitelly all Runners
             * ----------------------------------------
             var all = Task.WhenAll(
             root.WaitUntilFinishedAsync(),
             child1.WaitUntilFinishedAsync(),
             child2.WaitUntilFinishedAsync());
             var completed = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(5)));
            */
            //act: Await the root in a deterministic way
            //     to avoid setting a  timer for testing purpouses.
            //     But is not one obligation to await a runner.
            await ((ITaskRunnerCommandAsync)root).WaitUntilFinishedAsync();
            dispatcher.Dispose();

            // Assert
            //Assert.That(completed, Is.EqualTo(all), "Graph execution should finish within timeout.");
            Assert.That((root.ExecutionEnd > child2.ExecutionEnd 
                && (child2.ExecutionEnd > child1.ExecutionEnd)), 
                Is.EqualTo(true));
            CollectionAssert.AreEqual(new[] { "child-1", "child-2", "root" }, execution);
        }

        [Test]
        public async Task CreateRoot_WithCustomRetryPolicy_TakesPrecedenceOverDispatcherDefault()
        {
            // Arrange
            int dispatcherPolicyInvocations = 0;
            int rootPolicyInvocations = 0;

            Func<IRetryPolicy> dispatcherPolicyFactory =
                () => new CountingRetryPolicy(() => Interlocked.Increment(ref dispatcherPolicyInvocations));

            Func<IRetryPolicy> rootPolicyFactory =
                () => new CountingRetryPolicy(() => Interlocked.Increment(ref rootPolicyInvocations));

            var dispatcher = _dispatcherFactory.CreateParallel(
                name: "custom-retry",
                retryPolicyFactory: dispatcherPolicyFactory,
                maxParallelism: 1,
                logger: _logger,
                pulseMs: 10);

            var root = _rootFactory.Create(
                body: ct => Task.CompletedTask,
                retryPolicyFactory: rootPolicyFactory,
                name: null);

            var child = _runnerFactory.Create(ct => Task.CompletedTask, name: null);
            root.After(child);

            // Act
            dispatcher.Enqueue(root, CancellationToken.None);
            dispatcher.StartPolling();

            await root.WaitUntilFinishedAsync();
            dispatcher.Dispose();

            // Assert
            Assert.That(rootPolicyInvocations, Is.GreaterThanOrEqualTo(1), "Root-specific retry policy should be used.");
            Assert.That(dispatcherPolicyInvocations, Is.EqualTo(0), "Dispatcher-level retry policy should not be used when runner provides one.");
            Assert.That(root.Name, Is.EqualTo(string.Empty));
            Assert.That(child.Name, Is.EqualTo(string.Empty));
            Assert.That(root.ExecutionEnd > child.ExecutionEnd, Is.EqualTo(true));
        }
    }

    internal sealed class CountingRetryPolicy : IRetryPolicy
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


