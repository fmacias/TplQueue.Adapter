using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Fmacias.TplQueue.Test
{
    internal static class Helper
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
            var queueFactoryCoreMock = GetQFactoryCoreMock();
            return GetCoreApiMock(
                jobFactoryMock.Object,
                queueFactoryCoreMock.Object);
        }

        public static Mock<ICoreApi> GetCoreApiMock(
            IJobFactory jobFactory,
            IQFactory queueFactoryCore)
        {
            var coreApiMock = new Mock<ICoreApi>();
            coreApiMock.Setup(a => a.QFactory).Returns(queueFactoryCore);
            coreApiMock.Setup(a => a.JobFactory).Returns(jobFactory);
            coreApiMock.Setup(a => a.DataJobFactory).Returns(Mock.Of<IDataJobFactory>());
            return coreApiMock;
        }

        public static IApi GetApi()
        {
            return API.Create(
                GetCoreApiMock().Object,
                new Dictionary<string, IRetryPolicyOptions>(),
                new Dictionary<string, IQOptions>());
        }

        public static Mock<IQFactory> GetQFactoryCoreMock()
        {
            var coreQFactory = new Mock<IQFactory>();
            coreQFactory
                .Setup(p => p.Fifo(
                    It.IsAny<Guid>(), 
                    It.IsAny<string>(),
                    It.IsAny<ILogger>(),
                    It.IsAny<Func<IRetryPolicy>>()))
                .Returns(() => Mock.Of<IFifoQ>());

            var parallelQDefault = new Mock<IParallelQ>();
            parallelQDefault
                .Setup(o => o.MaxParallelism)
                .Returns(()=> Environment.ProcessorCount);

            coreQFactory
                .Setup(p => p.Parallel(
                    It.IsAny<Guid>(), 
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<ILogger>(),
                    It.IsAny<Func<IRetryPolicy>>()))
                .Returns(() => parallelQDefault.Object);

            return coreQFactory;
        }

        public static Mock<IRetryPolicyAbstractFactory> GetRetryPolicyFactoryMock()
        {
            var retryPolicyMock = new Mock<IRetryPolicyAbstractFactory>();
            retryPolicyMock
                .Setup(r => r.PolicyByName(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, IRetryPolicyOptions>>()))
                .Returns(Mock.Of<IRetryPolicy>());
            retryPolicyMock
                .Setup(r => r.PolicyByOptions(It.IsAny<IRetryPolicyOptions>()))
                .Returns(Mock.Of<IRetryPolicy>());
            return retryPolicyMock;
        }

        public static Mock<ITypeResolver> GetNodeTypeResolverMock()
        {
            var resolver = new Mock<ITypeResolver>();
            resolver
                .Setup(r => r.Resolve(It.IsAny<string>()))
                .Returns(typeof(object));
            return resolver;
        }

        public static Mock<IRetryPolicyAbstractFactory> GetRetryPolicyFactoryAbstractMock()
        {
            return new Mock<IRetryPolicyAbstractFactory>();
        }
    }
}
