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
        private Mock<ICoreQFactory> _queueFactoryCoreMock = null!;
        private Mock<IPayloadHandlerResolver> _jobHandlerResolver = null!;
        private Mock<IRetryPolicyGenericFactory> _retryPolicyFactory = null!;
        private Mock<INodeTypeResolver> _nodeTypeResolver = null!;
        private Dictionary<string, IQOptions> _queueOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _jobFactoryMock = new Mock<IJobFactory>();
            _jobRootFactoryMock = new Mock<IJobRootFactory>();
            _queueFactoryCoreMock = Helper.GetQFactoryCoreMock();
            _jobHandlerResolver = Helper.GetJobHandlerResolverMock();
            _retryPolicyFactory = Helper.GetRetryPolicyFactoryMock();
            _nodeTypeResolver = Helper.GetNodeTypeResolverMock();

            _coreApi = Helper.GetCoreApiMock(
                _jobFactoryMock.Object,
                _jobRootFactoryMock.Object,
                _queueFactoryCoreMock.Object);
            _queueOptions = new Dictionary<string, IQOptions>
            {
                { "default", Mock.Of<IQOptions>(o => o.MaxParallelism == 1 && o.RetryPolicy == "none") }
            };
        }

        [Test]
        public void GetFactories_DelegatesToInnerCoreApi()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyDescriptor>(),
                _queueOptions);

            Assert.That(api.JobFactory.Value, Is.SameAs(_jobFactoryMock.Object));
            Assert.That(api.JobRootFactory.Value, Is.SameAs(_jobRootFactoryMock.Object));
            Assert.IsInstanceOf<ICoreQFactoryAdapter>(api.CoreQFactories.Value);

            _coreApi.Verify(a => a.JobFactory, Times.AtLeastOnce);
            _coreApi.Verify(a => a.JobRootFactory, Times.AtLeastOnce);
            _coreApi.Verify(a => a.QFactory, Times.Once);
        }

        [Test]
        public void Create_WhenCoreApiIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => API.Create(
                null!,
                new Dictionary<string, IRetryPolicyDescriptor>(),
                _queueOptions));
        }

        [Test]
        public void Create_WhenQueueOptionsIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => API.Create(
                _coreApi.Object,
                null!,
                _queueOptions));
        }

        [Test]
        public void RetryPolicyFactory_WhenOptionsIsNull_ThrowsArgumentNullException()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyDescriptor>(),
                _queueOptions);

            Assert.Throws<ArgumentNullException>(() => api.RetryPolicy<IExponentialBackoff>(null!,"someName"));
        }
        
        [Test]
        public void DataJobFactory_WithNullResolver_ThrowsArgumentNullException()
        {
            var api = API.Create(
                _coreApi.Object,
                new Dictionary<string, IRetryPolicyDescriptor>(),
                _queueOptions);

            Assert.Throws<ArgumentNullException>(() =>
                api.DataJobFactory(null!));
        }
    }
}
