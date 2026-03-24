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
        private Mock<IQFactory> _queueFactoryCoreMock = null!;
        private Mock<IPayloadHandlerResolver> _jobHandlerResolver = null!;
        private Mock<IRetryPolicyAbstractFactory> _retryPolicyFactory = null!;
        private Mock<ITypeResolver> _nodeTypeResolver = null!;
        private Dictionary<string, IQOptions> _queueOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _jobFactoryMock = new Mock<IJobFactory>();
            _queueFactoryCoreMock = Helper.GetQFactoryCoreMock();
            _jobHandlerResolver = Helper.GetJobHandlerResolverMock();
            _retryPolicyFactory = Helper.GetRetryPolicyFactoryMock();
            _nodeTypeResolver = Helper.GetNodeTypeResolverMock();

            _coreApi = Helper.GetCoreApiMock(
                _jobFactoryMock.Object,
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
                _jobHandlerResolver.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            Assert.That(api.JobFactory, Is.SameAs(_jobFactoryMock.Object));
            Assert.That(api.DataJobFactory, Is.SameAs(_coreApi.Object.DataJobFactory));
            Assert.IsInstanceOf<IQFactoryAdapter>(api.QFactory);

            _coreApi.Verify(a => a.JobFactory, Times.AtLeastOnce);
            _coreApi.Verify(a => a.QFactory, Times.Once);
            _coreApi.Verify(a => a.DataJobFactory, Times.AtLeastOnce);
        }

        [Test]
        public void Create_WhenCoreApiIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => API.Create(
                null!,
                _jobHandlerResolver.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions));
        }

        [Test]
        public void Create_WhenPayloadHandlerResolverIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => API.Create(
                _coreApi.Object,
                null!,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions));
        }

        [Test]
        public void Create_WhenRetryPolicyOptionsIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => API.Create(
                _coreApi.Object,
                _jobHandlerResolver.Object,
                null!,
                _queueOptions));
        }

        [Test]
        public void RetryPolicyFactory_WhenOptionsIsNull_ThrowsArgumentNullException()
        {
            var api = API.Create(
                _coreApi.Object,
                _jobHandlerResolver.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            Assert.Throws<ArgumentNullException>(() => api.RetryPolicy<IExponentialBackoff>(null!, "someName"));
        }

        [Test]
        public void Cache_WithNullResolver_ThrowsArgumentNullException()
        {
            var api = API.Create(
                _coreApi.Object,
                _jobHandlerResolver.Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                _queueOptions);

            Assert.Throws<ArgumentNullException>(() =>
                api.Cache(Mock.Of<ICacheFactory<IDataJobCache>>(), Mock.Of<IUniversalDataSerializer>(), null!));
        }
    }
}
