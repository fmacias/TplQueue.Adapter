using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.Core.Runners;
using Fmaciasruano.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Queues
{
    [TestFixture]
    public class StrictFifoTaskQueueTest
    {
        private IStrictFifoTaskDispatcher _queue = null!;
        private ILogger<IStrictFifoTaskDispatcher> _logger = null!;
        private IRetryPolicyFactory _retryPolicyFactory = null!;
        private ITaskDispatcherFactory _taskDispatcherFactory = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _logger = Helper.GetLogger<IStrictFifoTaskDispatcher>();
            var retryPolicyOptions = new Dictionary<string, RetryPolicyOptions>();
            _retryPolicyFactory = RetryPolicyFactory.Instance(retryPolicyOptions);

            var dispatcherOptions = new Dictionary<string, IDispatcherOptions>();
            _taskDispatcherFactory = TaskDispatcherFactory.Instance(dispatcherOptions, _retryPolicyFactory);
        }

        [SetUp]
        public void Setup()
        {
            var noRetryPolicyFactory = () => _retryPolicyFactory.CreateNoRetryPolicy();

            _queue = _taskDispatcherFactory.CreateStrictFifo(
                "Strict fifo test-dispatcher",
                noRetryPolicyFactory,
                pulseMs: 20,
                _logger);
        }

        [TearDown]
        public void TearDown()
        {
            _queue.Dispose();
        }

        [Test]
        public async Task Enqueue_Action_JobExecutesInOrder()
        {
            var executionOrder = new List<int>();
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue
                .Enqueue((ct, eo) =>
                {
                    executionOrder.Add(1);
                }, executionOrder, CancellationToken.None)
                 .Enqueue((ct, eo) =>
                 {
                     executionOrder.Add(2);
                     completion.TrySetResult();
                 }, executionOrder, CancellationToken.None);

            _queue.StartPolling();

            Assert.IsTrue(await WaitForCompletion(completion.Task), "Dispatcher did not finish before timeout.");
            CollectionAssert.AreEqual(new[] { 1, 2 }, executionOrder);
        }

        [Test]
        public async Task Enqueue_FuncTask_JobExecutesInOrder()
        {
            var executionOrder = new List<int>();
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue
                .Enqueue(async (ct, eo) =>
                {
                    executionOrder.Add(1);
                    await Task.Delay(10, ct);
                }, executionOrder, CancellationToken.None)
                .Enqueue(async (ct, eo) =>
                {
                    executionOrder.Add(2);
                    completion.TrySetResult();
                    await Task.Delay(10, ct);
                }, executionOrder, CancellationToken.None);

            _queue.StartPolling();

            Assert.IsTrue(await WaitForCompletion(completion.Task), "Dispatcher did not finish before timeout.");
            CollectionAssert.AreEqual(new[] { 1, 2 }, executionOrder);
        }
        [Test]
        public async Task AddToQueue_EnforcesFifoOrdering()
        {
            var executionOrder = new List<int>();
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var first = TaskRunnerRoot.Create(ct =>
            {
                executionOrder.Add(1);
                return Task.CompletedTask;
            }, name: "first");

            var second = TaskRunnerRoot.Create(ct =>
            {
                executionOrder.Add(2);
                completion.TrySetResult();
                return Task.CompletedTask;
            }, name: "second");

            _queue.AddToQueue(first, isFifo: false, cancellationToken: CancellationToken.None);
            _queue.AddToQueue(second, isFifo: false, cancellationToken: CancellationToken.None);

            _queue.StartPolling();

            Assert.IsTrue(await WaitForCompletion(completion.Task), "Dispatcher did not finish before timeout.");
            CollectionAssert.AreEqual(new[] { 1, 2 }, executionOrder);
        }
        private static async Task<bool> WaitForCompletion(Task task, int timeoutMs = 1000)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs)) == task;
            return completed && task.IsCompleted;
        }
    }
}

