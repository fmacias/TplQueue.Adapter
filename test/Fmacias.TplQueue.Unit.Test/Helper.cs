using Fmacias.TplQueue.Cache.Contracts;
using Fmacias.TplQueue.Cache.Factories;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.RetryPolicies;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;

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
            var jobRootFactoryMock = new Mock<IJobRootFactory>();
            var queueFactoryCoreMock = GetQFactoryCoreMock();
            return GetCoreApiMock(
                jobFactoryMock.Object,
                jobRootFactoryMock.Object,
                queueFactoryCoreMock.Object);
        }

        public static Mock<ICoreApi> GetCoreApiMock(
            IJobFactory jobFactory,
            IJobRootFactory jobRootFactory,
            ICoreQFactory queueFactoryCore)
        {
            var coreApiMock = new Mock<ICoreApi>();
            coreApiMock.Setup(a => a.QFactory).Returns(queueFactoryCore);
            coreApiMock.Setup(a => a.JobFactory).Returns(jobFactory);
            coreApiMock.Setup(a => a.JobRootFactory).Returns(jobRootFactory);
            return coreApiMock;
        }

        public static IApi GetApi()
        {
            return API.Create(
                GetCoreApiMock().Object,
                new Dictionary<string, IRetryPolicyDescriptor>(),
                new Dictionary<string, IQOptions>());
        }

        public static Mock<ICoreQFactory> GetQFactoryCoreMock()
        {
            var coreQFactory = new Mock<ICoreQFactory>();
            coreQFactory
                .Setup(p => p.Fifo(
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
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<ILogger>(),
                    It.IsAny<Func<IRetryPolicy>>()))
                .Returns(() => parallelQDefault.Object);

            return coreQFactory;
        }

        public static Mock<IRetryPolicyGenericFactory> GetRetryPolicyFactoryMock()
        {
            var retryPolicyMock = new Mock<IRetryPolicyGenericFactory>();
            retryPolicyMock
                .Setup(r => r.PolicyByName(It.IsAny<string>(), It.IsAny<Dictionary<string, IRetryPolicyDescriptor>>()))
                .Returns(Mock.Of<IRetryPolicy>());
            retryPolicyMock
                .Setup(r => r.PolicyByDescriptor(It.IsAny<IRetryPolicyDescriptor>()))
                .Returns(Mock.Of<IRetryPolicy>());
            return retryPolicyMock;
        }

        public static Mock<IPayloadHandlerResolver> GetJobHandlerResolverMock()
        {
            var resolver = new Mock<IPayloadHandlerResolver>();
            resolver
                .Setup(r => r.Resolve(It.IsAny<Guid>()))
                .Returns(Mock.Of<IUniversaPayloadHandler>());
            return resolver;
        }

        public static Mock<INodeTypeResolver> GetNodeTypeResolverMock()
        {
            var resolver = new Mock<INodeTypeResolver>();
            resolver
                .Setup(r => r.Resolve(It.IsAny<string>()))
                .Returns(typeof(object));
            return resolver;
        }

        public static Mock<IRetryPolicyGenericFactory> GetRetryPolicyFactoryAbstractMock()
        {
            return new Mock<IRetryPolicyGenericFactory>();
        }
    }
}
