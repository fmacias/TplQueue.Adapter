using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NUnit.Framework;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Factories
{
    internal class DispatcherOptions : IDispatcherOptions
    {
        public DispatcherOptions(int maxParallelism, int pulseMs, string retryPolicy)
        {
            MaxParallelism = maxParallelism;
            PulseMs = pulseMs;
            RetryPolicy = retryPolicy;
        }

        public int MaxParallelism { get; }
        public int PulseMs { get; }
        public string RetryPolicy { get; }
    }

    [TestFixture]
    public class TaskDispatcherFactoryTests
    {
        private ILoggerFactory _loggerFactory = null!;
        private ITaskDispatcherFactory _factory = null!;
        private Dictionary<string, IDispatcherOptions> _dispatcherOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddNLog();
            });
            _dispatcherOptions = new Dictionary<string, IDispatcherOptions>
            {
                { "parallel", new DispatcherOptions(maxParallelism: 3, pulseMs: 25, retryPolicy: "linear") },
                { "fifo", new DispatcherOptions(maxParallelism: 1, pulseMs: 15, retryPolicy: "linear") }
            };

            var retryOptions = new Dictionary<string, Abstractions.RetryPolicyOptions>
            {
                { "linear", Abstractions.RetryPolicyOptions.Linear(baseDelayMs: 5, maxRetries: 2) }
            };

            _factory = TaskDispatcherFactory.Instance(_dispatcherOptions, RetryPolicyFactory.Instance(retryOptions));
        }

        [TearDown]
        public void TearDown()
        {
            _loggerFactory.Dispose();
        }

        [Test]
        public void CreateParallel_FromNamedOptions_UsesRetryPolicyAndConfiguration()
        {
            var dispatcher = _factory.CreateParallel("parallel", _loggerFactory.CreateLogger<IParallelTaskDispatcher>());

            Assert.That(dispatcher.Name, Is.EqualTo("parallel"));
            Assert.That(dispatcher.MaxParallelism, Is.EqualTo(_dispatcherOptions["parallel"].MaxParallelism));
            Assert.That(dispatcher.PulseMs, Is.EqualTo(_dispatcherOptions["parallel"].PulseMs));

            var retryPolicy = dispatcher.RetryPolicyFactory();
            Assert.That(retryPolicy, Is.InstanceOf<ILinearBackoffRetryPolicy>());
            Assert.That(((ILinearBackoffRetryPolicy)retryPolicy).MaxRetries, Is.EqualTo(2));

            dispatcher.Dispose();
        }

        [Test]
        public async Task GetDispatcher_StrictFifoFromConfiguration_ExecutesSequentially()
        {
            var dispatcher = _factory.GetDispatcher<IStrictFifoTaskDispatcher>("fifo", _loggerFactory);
            var results = new List<int>();
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            dispatcher
                .Enqueue(ct =>
                {
                    results.Add(1);
                    return Task.CompletedTask;
                }, CancellationToken.None)
                .Enqueue(async ct =>
                {
                    await Task.Delay(10, ct);
                    results.Add(2);
                    completion.TrySetResult();
                }, CancellationToken.None);

            dispatcher.StartPolling();

            Assert.IsTrue(await WaitForCompletion(completion.Task), "Dispatcher did not complete work on time.");
            CollectionAssert.AreEqual(new[] { 1, 2 }, results);
            Assert.That(dispatcher.PulseMs, Is.EqualTo(_dispatcherOptions["fifo"].PulseMs));

            dispatcher.Dispose();
        }

        private static async Task<bool> WaitForCompletion(Task task, int timeoutMs = 1000)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs)) == task;
            return completed && task.IsCompleted;
        }
    }
}

