using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.Runner;
using Moq;
using NUnit.Framework;

namespace Fmaciasruano.TplQueue.Test.Runner
{
    [TestFixture]
    public class PayloadRunnerValidationTests
    {
        private readonly TestPayload _payload = new();

        [Test]
        public void PayloadTaskRunner_Create_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => PayloadTaskRunner<TestPayload>.Create(null!, Mock.Of<IUniversalPayloadSerializer>(), Mock.Of<ITaskRunnerFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadTaskRunner<TestPayload>.Create(_payload, null!, Mock.Of<ITaskRunnerFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadTaskRunner<TestPayload>.Create(_payload, Mock.Of<IUniversalPayloadSerializer>(), null!));
        }

        [Test]
        public void PayloadTaskRunner_Load_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => PayloadTaskRunner<TestPayload>.Load(Mock.Of<ICacheLeaseEntry>(), null!, Mock.Of<ITaskRunnerFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadTaskRunner<TestPayload>.Load(Mock.Of<ICacheLeaseEntry>(), Mock.Of<IUniversalPayloadSerializer>(), null!));
        }

        [Test]
        public void PayloadTaskRunnerRoot_Create_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => PayloadTaskRunnerRoot<TestPayload>.Create(Guid.NewGuid(), null!, Mock.Of<IUniversalPayloadSerializer>(), Mock.Of<ITaskRunnerRootFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadTaskRunnerRoot<TestPayload>.Create(Guid.NewGuid(), _payload, null!, Mock.Of<ITaskRunnerRootFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadTaskRunnerRoot<TestPayload>.Create(Guid.NewGuid(), _payload, Mock.Of<IUniversalPayloadSerializer>(), null!));
        }

        [Test]
        public void PayloadTaskRunnerRoot_Load_ShouldValidateInputs()
        {
            Assert.Throws<ArgumentNullException>(() => PayloadTaskRunnerRoot<TestPayload>.Load(Mock.Of<ICacheLeaseEntry>(), null!, Mock.Of<ITaskRunnerRootFactory>(), Mock.Of<IRetryPolicyFactory>()));
            Assert.Throws<ArgumentNullException>(() => PayloadTaskRunnerRoot<TestPayload>.Load(Mock.Of<ICacheLeaseEntry>(), Mock.Of<IUniversalPayloadSerializer>(), null!, Mock.Of<IRetryPolicyFactory>()));
        }

        [Test]
        public void PayloadRunnerFactory_CreateRoot_ShouldValidateSerializer()
        {
            var factory = PayloadRunnerFactory.Instance(Mock.Of<ITaskRunnerFactory>(), Mock.Of<ITaskRunnerRootFactory>(),Mock.Of<IRetryPolicyFactory>());
            Assert.Throws<ArgumentNullException>(() => factory.CreateRoot(Guid.NewGuid(), _payload, null!));
            Assert.Throws<ArgumentNullException>(() => factory.CreateRoot(_payload, null!));
        }

        [Test]
        public void PayloadRunnerFactory_Create_ShouldValidateSerializer()
        {
            var factory = PayloadRunnerFactory.Instance(Mock.Of<ITaskRunnerFactory>(), Mock.Of<ITaskRunnerRootFactory>(), Mock.Of<IRetryPolicyFactory>());
            Assert.Throws<ArgumentNullException>(() => factory.Create(_payload, null!));
            Assert.Throws<ArgumentNullException>(() => factory.Create(Guid.NewGuid(), _payload, null!));
        }

        [Test]
        public void PayloadRunnerFactory_Load_ShouldValidateSerializer()
        {
            var factory = PayloadRunnerFactory.Instance(Mock.Of<ITaskRunnerFactory>(), Mock.Of<ITaskRunnerRootFactory>(), Mock.Of<IRetryPolicyFactory>());
            Assert.Throws<ArgumentNullException>(() => factory.Load(Mock.Of<ICacheLeaseEntry>(), null!));
            Assert.Throws<ArgumentNullException>(() => factory.LoadRoot(Mock.Of<ICacheLeaseEntry>(), null!));
        }

        [Test]
        public void PayloadTaskRunner_CopyInfo_SerializesPayloadSnapshot()
        {
            var serializer = new Mock<IUniversalPayloadSerializer>();
            serializer.Setup(s => s.Serialize(_payload)).Returns("{\"ok\":true}");
            var taskRunnerFactory = TaskRunnerFactory.Instance();
            var runner = PayloadTaskRunner<TestPayload>
                .Create(_payload, serializer.Object, taskRunnerFactory);

            var info = runner.CopyInfo();

            Assert.That(runner.PayloadSerializedData.JsonInput, Is.EqualTo("{\"ok\":true}"));
            Assert.That(info.PayloadSerializedData.JsonOutput, Is.EqualTo("{\"ok\":true}"));
        }

        [Test]
        public async Task PayloadTaskRunnerRoot_UsesProvidedRetryPolicyFactory()
        {
            var serializer = new Mock<IUniversalPayloadSerializer>();
            serializer.Setup(s => s.Serialize(_payload)).Returns("{}");
            int calls = 0;
            Func<IRetryPolicy> retryFactory = () => new CountingRetryPolicy(() => calls++);
            var taskRunnerRootFactory = TaskRunnerRootFactory.Instance();
            var root = PayloadTaskRunnerRoot<TestPayload>.Create(
                _payload,
                serializer.Object,
                taskRunnerRootFactory,
                retryFactory);

            var policy = root.GetRetryPolicyFactory()();
            await policy.ExecuteAsync(ct => Task.FromResult(true), CancellationToken.None);

            Assert.That(policy, Is.InstanceOf<CountingRetryPolicy>());
            Assert.That(calls, Is.EqualTo(1));
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
