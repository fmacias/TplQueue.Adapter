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
        private Mock<IJobFactory> _runnerFactory = null!;
        private Mock<IJobRootFactory> _rootFactory = null!;
        private Mock<IQFactory> _dispatcherFactory = null!;
        private Mock<IRetryPolicyFactory> _retryFactory = null!;
        private Dictionary<string, IQOptions> _dispatcherOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _runnerFactory = new Mock<IJobFactory>();
            _rootFactory = new Mock<IJobRootFactory>();
            _dispatcherFactory = new Mock<IQFactory>();
            _retryFactory = new Mock<IRetryPolicyFactory>();

            _coreApi = new Mock<ICoreApi>();
            _coreApi.Setup(a => a.GetJobFactory()).Returns(_runnerFactory.Object);
            _coreApi.Setup(a => a.GetJobRootFactory()).Returns(_rootFactory.Object);

            _dispatcherOptions = new Dictionary<string, IQOptions>
            {
                { "default", Mock.Of<IQOptions>(o => o.MaxParallelism == 1 && o.PulseMs == 5 && o.RetryPolicy == "none") }
            };
            _coreApi.Setup(a => a.GetQFactory(_dispatcherOptions, _retryFactory.Object))
                .Returns(_dispatcherFactory.Object);
        }

        [Test]
        public void GetFactories_DelegatesToInnerCoreApi()
        {
            var api = API.Create(_coreApi.Object);

            Assert.That(api.GetJobFactory(), Is.SameAs(_runnerFactory.Object));
            Assert.That(api.GetJobRootFactory(), Is.SameAs(_rootFactory.Object));
            Assert.That(api.GetQFactory(_dispatcherOptions, _retryFactory.Object), Is.SameAs(_dispatcherFactory.Object));

            _coreApi.Verify(a => a.GetJobFactory(), Times.AtLeastOnce);
            _coreApi.Verify(a => a.GetJobRootFactory(), Times.AtLeastOnce);
            _coreApi.Verify(a => a.GetQFactory(_dispatcherOptions, _retryFactory.Object), Times.Once);
        }

        [Test]
        public void GetCacheFactory_ReturnsNewInstances()
        {
            var api = API.Create(_coreApi.Object);

            var first = api.GetCacheFactory();
            var second = api.GetCacheFactory();

            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void Instance_WhenCoreApiIsNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => API.Create(null!));
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
