using Castle.Components.DictionaryAdapter;
using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Fmacias.TplQueue.Test
{
    internal class Helper
    {
        public static ILogger<T> GetLogger<T>()
        {
            var loggerMock = new Mock<ILogger<T>>();
            loggerMock.SetupAllProperties();
            return loggerMock.Object;
        }
        public static Mock<ICoreApi> GetCoreApiMock()
        {
            var jobFactoryMock = new Mock<IJobFactory>();
            var jobRootFactoryMock = new Mock<IJobRootFactory>();
            var queueFactoryCoreMock = GetQFactoryCoreMock();
            return GetCoreApiMock(jobFactoryMock.Object, jobRootFactoryMock.Object,
                queueFactoryCoreMock.Object);
        }

        public static Mock<ICoreApi> GetCoreApiMock(IJobFactory jobFactory,
            IJobRootFactory jobRootFactory, IQFactoryCore queueFactoryCore)
        {
            var coreApiMock = new Mock<ICoreApi>();
            coreApiMock.Setup(a => a.GetQFactoryCore())
                .Returns(queueFactoryCore);
            coreApiMock.Setup(a => a.GetJobFactoryCore())
                .Returns(jobFactory);
            coreApiMock.Setup(a => a.GetJobRootFactoryCore())
                .Returns(jobRootFactory);
            return coreApiMock;
        }
        public static IApi GetApi()
        {
            return API.Create(GetCoreApiMock().Object);
        }

        public static Mock<IQFactoryCore> GetQFactoryCoreMock()
        {
            var coreQFactory = new Mock<IQFactoryCore>();
            coreQFactory
                .Setup(p => p.CreateFifo(
                    It.IsAny<string>(),
                    It.IsAny<ILogger>(),
                    It.IsAny<Func<IRetryPolicy>>())
                ).Returns(() => Mock.Of<IFifoQ>());

            coreQFactory
                .Setup(p => p.CreateParallel(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<ILogger>(),
                    It.IsAny<Func<IRetryPolicy>>())
                ).Returns(() => Mock.Of<IParallelQ>());
            return coreQFactory;
        }
        public static Mock<IRetryPolicyFactory> GetRetryPolicyFactoryMock()
        {
            var retryPolicyMock =  new Mock<IRetryPolicyFactory>();
            retryPolicyMock
                .Setup(r => r.Create(It.IsAny<string>()))
                .Returns(Mock.Of<IRetryPolicy>());
            return retryPolicyMock;
        }
    }
}
