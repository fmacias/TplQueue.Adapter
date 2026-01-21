using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Serialization.SystemTextJson;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test
{
    [TestFixture]
    public class ApiTests
    {
        private Mock<ICoreApi> _coreApi = null!;
        private Mock<ITaskRunnerFactory> _runnerFactory = null!;
        private Mock<ITaskRunnerRootFactory> _rootFactory = null!;
        private Mock<ITaskDispatcherFactory> _dispatcherFactory = null!;
        private Mock<IRetryPolicyFactory> _retryFactory = null!;
        private Dictionary<string, IDispatcherOptions> _dispatcherOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _runnerFactory = new Mock<ITaskRunnerFactory>();
            _rootFactory = new Mock<ITaskRunnerRootFactory>();
            _dispatcherFactory = new Mock<ITaskDispatcherFactory>();
            _retryFactory = new Mock<IRetryPolicyFactory>();

            _coreApi = new Mock<ICoreApi>();
            _coreApi.Setup(a => a.GetTaskRunnerFactory()).Returns(_runnerFactory.Object);
            _coreApi.Setup(a => a.GetTaskRunnerRootFactory()).Returns(_rootFactory.Object);

            _dispatcherOptions = new Dictionary<string, IDispatcherOptions>
            {
                { "default", Mock.Of<IDispatcherOptions>(o => o.MaxParallelism == 1 && o.PulseMs == 5 && o.RetryPolicy == "none") }
            };
            _coreApi.Setup(a => a.GetTaskDispatcherFactory(_dispatcherOptions, _retryFactory.Object))
                .Returns(_dispatcherFactory.Object);
        }

        [Test]
        public void GetFactories_DelegatesToInnerCoreApi()
        {
            var api = API.Instance(_coreApi.Object);

            Assert.That(api.GetTaskRunnerFactory(), Is.SameAs(_runnerFactory.Object));
            Assert.That(api.GetTaskRunnerRootFactory(), Is.SameAs(_rootFactory.Object));
            Assert.That(api.GetTaskDispatcherFactory(_dispatcherOptions, _retryFactory.Object), Is.SameAs(_dispatcherFactory.Object));

            _coreApi.Verify(a => a.GetTaskRunnerFactory(), Times.AtLeastOnce);
            _coreApi.Verify(a => a.GetTaskRunnerRootFactory(), Times.AtLeastOnce);
            _coreApi.Verify(a => a.GetTaskDispatcherFactory(_dispatcherOptions, _retryFactory.Object), Times.Once);
        }

        [Test]
        public void GetCacheFactory_ReturnsNewInstances()
        {
            var api = API.Instance(_coreApi.Object);

            var first = api.GetCacheFactory();
            var second = api.GetCacheFactory();

            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void Instance_WhenCoreApiIsNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => API.Instance(null!));
        }
        public sealed class RecordingPayload : IPayloadCommand
        {
            public RecordingPayload(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public string HandlerId => "recording";
            public bool Executed { get; private set; }

            public Task ExecuteAsync(CancellationToken ct)
            {
                Executed = true;
                return Task.CompletedTask;
            }

            public override string ToString() => Name;
        }
    }
}
