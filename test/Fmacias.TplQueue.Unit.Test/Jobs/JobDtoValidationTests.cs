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
            Assert.Throws<ArgumentNullException>(() => DataJob<TestPayload>.Create(null!, _payload));
            Assert.Throws<ArgumentNullException>(() => DataJob<TestPayload>.Create(Mock.Of<IJob>(), null!));
        }

        [Test]
        public void JobRootDto_Create_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => DataJobRoot<TestPayload>.CreateRoot(
                null!,
                _payload)
            );

            Assert.Throws<ArgumentNullException>(() => DataJobRoot<TestPayload>.CreateRoot(
                Mock.Of<IJobRoot>(),
                null!)
            );

            Assert.IsInstanceOf<IDataJobRoot<TestPayload>>(
                DataJobRoot<TestPayload>.CreateRoot(
                    Mock.Of<IJobRoot>(), _payload)
            );
        }

        [Test]
        public void PayloadJobFactory_CreateRoot_AlsoWithoutRelatedHandler()
        {
            var jobFactory = new Mock<IJobFactory>(MockBehavior.Strict);
            jobFactory
                .Setup(o => o.Job<IUniversaPayloadHandler, TestPayload>(
                    It.IsAny<Func<CancellationToken, IUniversaPayloadHandler, TestPayload, Task>>(),
                    It.IsAny<IUniversaPayloadHandler>(),
                    It.IsAny<TestPayload>(),
                    It.IsAny<string>()))
                .Returns(Mock.Of<IJob>());

            var factory = DataJobFactory.Create(
                jobFactory.Object,
                Mock.Of<IJobRootFactory>(),
                Mock.Of<IRetryPolicyGenericFactory>(),
                CreateHandlerResolver()
            );

            Assert.That(factory.DataJobRoot(_payload, null!), Is.InstanceOf<IDataJob<TestPayload>>());
        }

        [Test]
        public void PayloadJobFactory_CreatePayloadJobRoot_WithoutHandler_WorksAlso()
        {
            var jobRootFactory = new Mock<IJobRootFactory>(MockBehavior.Strict);
            jobRootFactory
                .Setup(o => o.JobRoot<IUniversaPayloadHandler, TestPayload>(
                    It.IsAny<Func<CancellationToken, IUniversaPayloadHandler, TestPayload, Task>>(),
                    It.IsAny<IUniversaPayloadHandler>(),
                    It.IsAny<TestPayload>(),
                    It.IsAny<Func<IRetryPolicy>>(),
                    It.IsAny<string>()))
                .Returns(Mock.Of<IJobRoot>());

            var factory = DataJobFactory.Create(
                Helper.GetApi().JobFactory.Value,
                jobRootFactory.Object,
                Helper.GetApi().RetryPolicyGenericFactory,
                CreateHandlerResolver());

            var payloadJobRoot = factory.DataJobRoot(_payload);

            Assert.That(payloadJobRoot, Is.InstanceOf<IDataJobRoot<TestPayload>>());
        }

        [Test]
        public void PayloadJobFactory_Load_ShouldValidateSerializer()
        {
            var jobRootFactory = new Mock<IJobRootFactory>(MockBehavior.Strict);
            jobRootFactory
                .Setup(o => o.JobRoot<IUniversaPayloadHandler, IPayload>(
                    It.IsAny<Func<CancellationToken, IUniversaPayloadHandler, IPayload, Task>>(),
                    It.IsAny<IUniversaPayloadHandler>(),
                    It.IsAny<IPayload>(),
                    It.IsAny<Func<IRetryPolicy>>(),
                    It.IsAny<string>()))
                .Returns(Mock.Of<IJobRoot>());

            var factory = DataJobFactory.Create(
                Mock.Of<IJobFactory>(),
                jobRootFactory.Object,
                Mock.Of<IRetryPolicyGenericFactory>(),
                CreateHandlerResolver());

            Assert.Throws<ArgumentNullException>(() => factory.DataJobRoot<IPayload>(null!));

            var payload = new Mock<IPayload>();
            payload.SetupGet(p => p.PayloadId).Returns("handler");
            payload.SetupGet(p => p.HandlerId).Returns(Guid.NewGuid());
            payload.SetupGet(p => p.CollectionTime).Returns(DateTime.UtcNow);

            Assert.DoesNotThrow(() => factory.DataJobRoot(payload.Object));
        }

        private class TestPayload : IPayload
        {
            public string PayloadId => "handler";

            public DateTime CollectionTime => DateTime.UtcNow;

            public Guid HandlerId => Guid.NewGuid();
        }

        private static IPayloadHandlerResolver CreateHandlerResolver()
        {
            var resolver = new Mock<IPayloadHandlerResolver>(MockBehavior.Strict);
            resolver
                .Setup(r => r.Resolve(It.IsAny<Guid>()))
                .Throws<KeyNotFoundException>();
            return resolver.Object;
        }
    }
}
