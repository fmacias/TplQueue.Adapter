using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Factories
{
    [TestFixture]
    public class TaskRunnerRootFactoryIntegrationTests
    {
        private ITaskRunnerRootFactory _rootFactory = null!;
        private ITaskRunnerFactory _runnerFactory = null!;
        private ITaskDispatcherFactory _dispatcherFactory = null!;
        private ILoggerFactory _loggerFactory = null!;
        private Dictionary<string, IDispatcherOptions> _dispatcherOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _rootFactory = TaskRunnerRootFactory.Instance();
            _runnerFactory = TaskRunnerFactory.Instance();
            _loggerFactory = LoggerFactory.Create(builder => builder.ClearProviders());
            _dispatcherOptions = new Dictionary<string, IDispatcherOptions>
            {
                { "fifo", new DispatcherOptions(maxParallelism: 1, pulseMs: 10, retryPolicy: "no-retry") }
            };
            var retryOptions = new Dictionary<string, Abstractions.RetryPolicyOptions>
            {
                { "no-retry", new Abstractions.RetryPolicyOptions(baseDelayMs: 0, maxRetries: 0, factor: null) }
            };
            _dispatcherFactory = TaskDispatcherFactory.Instance(_dispatcherOptions, RetryPolicyFactory.Instance(retryOptions));
        }

        [TearDown]
        public void TearDown()
        {
            _loggerFactory?.Dispose();
        }

        [Test]
        public void Instance_WithSameParameters_ReturnsSingleton()
        {
            var next = TaskRunnerRootFactory.Instance();
            Assert.That(next, Is.SameAs(_rootFactory));
        }

        [Test]
        public async Task CreateRoot_PropagatesRetryPolicyToChildGraph()
        {
            int calls = 0;
            Func<IRetryPolicy> rootPolicy = () => new CountingRetryPolicy(() => Interlocked.Increment(ref calls));

            var root = _rootFactory.Create(ct => Task.CompletedTask, rootPolicy, "root");
            var child = _runnerFactory.Create(ct => Task.CompletedTask, "child");
            root.After(child);

            var dispatcher = _dispatcherFactory.CreateStrictFifo("fifo", _loggerFactory.CreateLogger<IStrictFifoTaskDispatcher>());

            dispatcher.Enqueue(root, CancellationToken.None);
            dispatcher.StartPolling();

            await root.WaitUntilFinishedAsync();
            dispatcher.Dispose();

            Assert.That(calls, Is.EqualTo(2));
        }
    }
}

