using Fmacias.TplQueue.Contracts;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test
{
    [TestFixture]
    public class ApiTests
    {
        private Mock<ICoreApi> _coreApi = null!;
        private Mock<IJobFactory> _jobFactoryMock = null!;
        private Mock<IJobRootFactory> _jobRootFactoryMock = null!;
        private Mock<IQFactoryCore> _queueFactoryCoreMock = null!;
        private Dictionary<string, IQOptions> _queueOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _jobFactoryMock = new Mock<IJobFactory>();
            _jobRootFactoryMock = new Mock<IJobRootFactory>();
            _queueFactoryCoreMock = Helper.GetQFactoryCoreMock();

            _coreApi = Helper.GetCoreApiMock(_jobFactoryMock.Object,
                _jobRootFactoryMock.Object, _queueFactoryCoreMock.Object);
            _queueOptions = new Dictionary<string, IQOptions>
            {
                { "default", Mock.Of<IQOptions>(o => o.MaxParallelism == 1 && o.RetryPolicy == "none") }
            };
        }

        [Test]
        public void GetFactories_DelegatesToInnerCoreApi()
        {
            var api = API.Create(_coreApi.Object);
            Assert.That(api.GetJobFactoryCore(), Is.SameAs(_jobFactoryMock.Object));
            Assert.That(api.GetJobRootFactoryCore(), Is.SameAs(_jobRootFactoryMock.Object));
            Assert.That(api.GetQFactoryCore(), Is.SameAs(_queueFactoryCoreMock.Object));

            _coreApi.Verify(a => a.GetJobFactoryCore(), Times.AtLeastOnce);
            _coreApi.Verify(a => a.GetJobRootFactoryCore(), Times.AtLeastOnce);
            _coreApi.Verify(a => a.GetQFactoryCore(), Times.Once);
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
