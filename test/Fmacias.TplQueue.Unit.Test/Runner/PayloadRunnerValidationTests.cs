using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Factories;
using Fmacias.TplQueue.Jobs;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Runner
{
    [TestFixture]
    public class PayloadRunnerValidationTests
    {
        private readonly TestPayload _payload = new();

        [Test]
        public void PayloadJob_Create_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => PayloadJob<TestPayload>.Create(null!, Mock.Of<IJsonUniversalPayloadSerializer>(), Mock.Of<IJobFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadJob<TestPayload>.Create(_payload, null!, Mock.Of<IJobFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadJob<TestPayload>.Create(_payload, Mock.Of<IJsonUniversalPayloadSerializer>(), null!));
        }

        [Test]
        public void PayloadJob_Load_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => PayloadJob<TestPayload>.Load(Mock.Of<ICacheLeaseEntry>(), null!, Mock.Of<IJobFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadJob<TestPayload>.Load(Mock.Of<ICacheLeaseEntry>(), Mock.Of<IJsonUniversalPayloadSerializer>(), null!));
        }

        [Test]
        public void PayloadJobRoot_Create_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => PayloadJobRoot<TestPayload>.Create(Guid.NewGuid(), null!, Mock.Of<IJsonUniversalPayloadSerializer>(), Mock.Of<IJobRootFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadJobRoot<TestPayload>.Create(Guid.NewGuid(), _payload, null!, Mock.Of<IJobRootFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadJobRoot<TestPayload>.Create(Guid.NewGuid(), _payload, Mock.Of<IJsonUniversalPayloadSerializer>(), null!));
        }

        [Test]
        public void PayloadJobRoot_Load_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => PayloadJobRoot<TestPayload>.Load(Mock.Of<ICacheLeaseEntry>(), null!, Mock.Of<IJobRootFactory>(), Mock.Of<IRetryPolicyFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadJobRoot<TestPayload>.Load(Mock.Of<ICacheLeaseEntry>(), Mock.Of<IJsonUniversalPayloadSerializer>(), null!, Mock.Of<IRetryPolicyFactory>()));
        }

        [Test]
        public void PayloadRunnerFactory_CreateRoot_ShouldValidateSerializer()
        {
            var factory = PayloadRunnerFactory.Instance(Mock.Of<IJobFactory>(), Mock.Of<IJobRootFactory>(),Mock.Of<IRetryPolicyFactory>());
            Assert.Throws<ArgumentNullException>(() => factory.CreateRoot(Guid.NewGuid(), _payload, null!));
            Assert.Throws<ArgumentNullException>(() => factory.CreateRoot(_payload, null!));
        }

        [Test]
        public void PayloadRunnerFactory_Create_ShouldValidateSerializer()
        {
            var factory = PayloadRunnerFactory.Instance(Mock.Of<IJobFactory>(), Mock.Of<IJobRootFactory>(), Mock.Of<IRetryPolicyFactory>());
            Assert.Throws<ArgumentNullException>(() => factory.Create(_payload, null!));
            Assert.Throws<ArgumentNullException>(() => factory.Create(Guid.NewGuid(), _payload, null!));
        }

        [Test]
        public void PayloadRunnerFactory_Load_ShouldValidateSerializer()
        {
            var factory = PayloadRunnerFactory.Instance(Mock.Of<IJobFactory>(), Mock.Of<IJobRootFactory>(), Mock.Of<IRetryPolicyFactory>());
            Assert.Throws<ArgumentNullException>(() => factory.Load(Mock.Of<ICacheLeaseEntry>(), null!));
            Assert.Throws<ArgumentNullException>(() => factory.LoadRoot(Mock.Of<ICacheLeaseEntry>(), null!));
        }


        private class TestPayload : IPayloadCommand
        {
            public string HandlerId => "handler";

            public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
        }

        private sealed class CountingRetryPolicy : IRetryPolicy
        {
            private readonly Action _onExecute;

            public CountingRetryPolicy(Action onExecute)
            {
                _onExecute = onExecute;
            }

            public int RetryCount { get; private set; }

            public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken)
            {
                RetryCount++;
                _onExecute?.Invoke();
                return action(cancellationToken);
            }

            public Func<IRetryPolicy> FromDescriptor(IRetryPolicyDescriptor descriptor)
            {
                throw new NotImplementedException();
            }

            public IRetryPolicy SetFromDescriptor(IRetryPolicyDescriptor descriptor)
            {
                throw new NotImplementedException();
            }

            public IRetryPolicy SetFromOptions(RetryPolicyOptions options)
            {
                throw new NotImplementedException();
            }

            public IRetryPolicyDescriptor ToDescriptor()
            {
                throw new NotImplementedException();
            }
        }
    }
}
