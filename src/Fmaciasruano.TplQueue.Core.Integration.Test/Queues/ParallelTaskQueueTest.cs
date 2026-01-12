using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Queues
{
    [TestFixture()]
    public class ParallelTaskQueueTest
    {
        private IParallelTaskDispatcher _queue = null!;
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
        public void Setup()
        {
            var noRetryPolicyFactory = () => _retryPolicyFactory.CreateNoRetryPolicy();
            
            _queue = _taskDispatcherFactory.CreateParallel(
                "Default test-TaskDipatcher", 
                noRetryPolicyFactory, 
                maxParallelism: 8,
                _logger, 
                pulseMs: 20);
        }

        [TearDown]
        public void TearDown()
        {
            _queue.Dispose();
        }

        [Test]
        public async Task Enqueue_Action_JobExecutes()
        {
            int count = 0;
            string[] executionOrder = new string[2] { "", "" };

            _queue
                .Enqueue((ct, eo) =>
                {
                    Task.Delay(100).Wait();
                    count++;
                    eo[0] = count.ToString();
                }, executionOrder, CancellationToken.None)
                 .Enqueue((ct, eo) =>
                 {
                     Task.Delay(20).Wait();
                     count++;
                     eo[1] = count.ToString();
                 }, executionOrder, CancellationToken.None);

            _queue.StartPolling();

            await Task.Delay(200);
            Assert.AreEqual("2", executionOrder[0]);
            Assert.AreEqual("1", executionOrder[1]);
            _queue.Dispose();
        }

        [Test]
        public async Task Enqueue_FuncTask_JobExecutes()
        {
            int count = 0;
            string[] executionOrder = new string[2] { "", "" };

            _queue
                .Enqueue(async (ct, eo) =>
                {
                    await Task.Delay(100);
                    count++;
                    eo[0] = count.ToString();
                }, executionOrder, CancellationToken.None)
                .Enqueue(async (ct, eo) =>
                {
                    await Task.Delay(20);
                    count++;
                    eo[1] = count.ToString();
                }, executionOrder, CancellationToken.None);

            _queue.StartPolling();

            await Task.Delay(200);
            Assert.AreEqual("2", executionOrder[0]);
            Assert.AreEqual("1", executionOrder[1]);
            _queue.Dispose();
        }
    }
}

