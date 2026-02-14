using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Factories;
using Fmacias.TplQueue.Jobs;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Jobs
{
    [TestFixture]
    public class JobDtoValidationTests
    {
        private readonly TestPayload _payload = new();

        [Test]
        public void JobDto_Create_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => PayloadJob<TestPayload>.Create(null!, _payload));
            Assert.Throws<ArgumentNullException>(() => PayloadJob<TestPayload>.Create(Mock.Of<IJob>(), null!));
        }

        [Test]
        public void JobRootDto_Create_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => PayloadJobRoot<TestPayload>.CreateRoot(
                null!,
                _payload)
            );

            Assert.Throws<ArgumentNullException>(() => PayloadJobRoot<TestPayload>.CreateRoot(
                Mock.Of<IJobRoot>(),
                null!)
            );

            Assert.IsInstanceOf<IPayloadJobRoot<TestPayload>>(
                PayloadJobRoot<TestPayload>.CreateRoot(
                    Mock.Of<IJobRoot>(), _payload)
            );
        }

        [Test]
        public void PayloadJobFactory_CreateRoot_AlsoWithoutRelatedHandler()
        {
            var jobFactory = new Mock<IJobFactory>(MockBehavior.Strict);
            jobFactory
                .Setup(o => o.CreateJob<IUniversaDtoHandler2, TestPayload>(
                    It.IsAny<Func<CancellationToken, IUniversaDtoHandler2, TestPayload, Task>>(),
                    It.IsAny<IUniversaDtoHandler2>(),
                    It.IsAny<TestPayload>(),
                    It.IsAny<string>()))
                .Returns(Mock.Of<IJob>());

            var factory = PayloadJobFactory.Create(
                jobFactory.Object,
                Mock.Of<IJobRootFactory>(),
                Mock.Of<IRetryPolicyFactory>(),
                CreateHandlerResolver()
            );

            Assert.That(factory.CreateJob(_payload, null!), Is.InstanceOf<IPayloadJob<TestPayload>>());
        }

        [Test]
        public void PayloadJobFactory_CreatePayloadJobRoot_WithoutHandler_WorksAlso()
        {
            var jobRootFactory = new Mock<IJobRootFactory>(MockBehavior.Strict);
            jobRootFactory
                .Setup(o => o.CreateJob<IUniversaDtoHandler2, TestPayload>(
                    It.IsAny<Func<CancellationToken, IUniversaDtoHandler2, TestPayload, Task>>(),
                    It.IsAny<IUniversaDtoHandler2>(),
                    It.IsAny<TestPayload>(),
                    It.IsAny<Func<IRetryPolicy>>(),
                    It.IsAny<string>()))
                .Returns(Mock.Of<IJobRoot>());

            var factory = PayloadJobFactory.Create(
                Helper.GetApi().GetJobFactoryCore(),
                jobRootFactory.Object,
                Helper.GetApi().RetryPolicyFactory(new Dictionary<string, RetryPolicyOptions>()),
                CreateHandlerResolver());

            var payloadJobRoot = factory.CreateJobRoot(_payload);

            Assert.That(payloadJobRoot, Is.InstanceOf<IPayloadJobRoot<TestPayload>>());
        }

        [Test]
        public void PayloadJobFactory_Load_ShouldValidateSerializer()
        {
            var jobRootFactory = new Mock<IJobRootFactory>(MockBehavior.Strict);
            jobRootFactory
                .Setup(o => o.CreateJob<IUniversaDtoHandler2, IPayload>(
                    It.IsAny<Func<CancellationToken, IUniversaDtoHandler2, IPayload, Task>>(),
                    It.IsAny<IUniversaDtoHandler2>(),
                    It.IsAny<IPayload>(),
                    It.IsAny<Func<IRetryPolicy>>(),
                    It.IsAny<string>()))
                .Returns(Mock.Of<IJobRoot>());

            var factory = PayloadJobFactory.Create(
                Mock.Of<IJobFactory>(),
                jobRootFactory.Object,
                Mock.Of<IRetryPolicyFactory>(),
                CreateHandlerResolver());

            Assert.Throws<ArgumentNullException>(() => factory.CreateJobRoot<IPayload>(null!));

            var payload = new Mock<IPayload>();
            payload.SetupGet(p => p.PayloadId).Returns("handler");
            payload.SetupGet(p => p.HandlerId).Returns(Guid.NewGuid());
            payload.SetupGet(p => p.CollectionTime).Returns(DateTime.UtcNow);

            Assert.DoesNotThrow(() => factory.CreateJobRoot(payload.Object));
        }

        private class TestPayload : IPayload
        {
            public string PayloadId => "handler";

            public DateTime CollectionTime => DateTime.UtcNow;

            public Guid HandlerId => Guid.NewGuid();
        }

        private static IJobHandlerResolver2 CreateHandlerResolver()
        {
            var resolver = new Mock<IJobHandlerResolver2>(MockBehavior.Strict);
            resolver
                .Setup(r => r.Resolve(It.IsAny<Guid>()))
                .Throws<KeyNotFoundException>();
            return resolver.Object;
        }
    }
}
