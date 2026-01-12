using Fmaciasruano.TplQueue;
using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;

namespace Fmaciasruano.TplQueue.Core.Integration.Test.Core
{
    internal sealed class TestDispatcherOptions : IDispatcherOptions
    {
        public TestDispatcherOptions(int maxParallelism, int pulseMs, string retryPolicy)
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
    public class CoreApiIntegrationTests
    {
        private ILogger<IParallelTaskDispatcher> _logger = null!;
        private ILoggerFactory _loggerFactory = null!;
        private IParallelTaskDispatcher? _dispatcher;
        private Dictionary<string, IDispatcherOptions> _dispatcherOptions = null!;
        private Dictionary<string, RetryPolicyOptions> _retryOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _loggerFactory = LoggerFactory.Create(builder => builder.ClearProviders());
            _logger = _loggerFactory.CreateLogger<IParallelTaskDispatcher>();
            _dispatcherOptions = new Dictionary<string, IDispatcherOptions>
            {
                { "main", new TestDispatcherOptions(2, 20, "no-retry") }
            };
            _retryOptions = new Dictionary<string, RetryPolicyOptions>
            {
                { "no-retry", RetryPolicyOptions.Linear(baseDelayMs: 0, maxRetries: 0) }
            };
        }

        [Test]
        [TearDown]
        public void TearDown()
        {
            _dispatcher?.Dispose();
            _loggerFactory?.Dispose();
        }

        [Test]
        public async Task CoreApi_Factories_CreateDispatcherAndRunGraph()
        {
            var coreApi = CoreApi.Instance();
            var retryFactory = RetryPolicyFactory.Instance(_retryOptions);
            var dispatcherFactory = coreApi.GetTaskDispatcherFactory(_dispatcherOptions, retryFactory);
            var runnerFactory = coreApi.GetTaskRunnerFactory();
            var rootFactory = coreApi.GetTaskRunnerRootFactory();

            _dispatcher = dispatcherFactory.GetDispatcher<IParallelTaskDispatcher>("main", _loggerFactory);

            var execution = new List<string>();
            var child = runnerFactory.Create(ct => execution.Add("child"), "child");
            var root = rootFactory.Create(ct => execution.Add("root"), () => retryFactory.Create("no-retry"), "root");
            root.After(child);

            _dispatcher.Enqueue(root, CancellationToken.None);
            _dispatcher.StartPolling();

            await root.WaitUntilFinishedAsync();
            
            CollectionAssert.AreEqual(new[] { "child", "root" }, execution);
        }

        [Test]
        public async Task ApiFacade_Factories_CreateDispatcherAndRunGraph()
        {
            var coreApi = CoreApi.Instance();
            var api = API.Instance(coreApi);
            var retryFactory = api.GetRetryPolicyFactory(_retryOptions);
            var dispatcherFactory = api.GetTaskDispatcherFactory(_dispatcherOptions, retryFactory);
            var runnerFactory = api.GetTaskRunnerFactory();
            var rootFactory = api.GetTaskRunnerRootFactory();

            _dispatcher = dispatcherFactory.GetDispatcher<IParallelTaskDispatcher>("main", _loggerFactory);

            var execution = new List<string>();
            var child = runnerFactory.Create(ct => execution.Add("child"), "child");
            var root = rootFactory.Create(ct => execution.Add("root"), () => retryFactory.Create("no-retry"), "root");
            root.After(child);

            _dispatcher.Enqueue(root, CancellationToken.None);
            _dispatcher.StartPolling();

            await root.WaitUntilFinishedAsync();

            CollectionAssert.AreEqual(new[] { "child", "root" }, execution);
        }
    }
}

