using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Jobs;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Jobs
{
    [TestFixture]
    public class PayloadJobHandlerResolutionTests
    {
        [Test]
        public async Task PayloadJob_Create_ResolvesHandler_WithHandlerId()
        {
            var payload = new TestPayload("handler-1");

            var handlerCalled = false;
            var resolver = new Mock<IJobHandlerResolver>(MockBehavior.Strict);
            resolver
                .Setup(r => r.Resolve(typeof(TestPayload), payload.PayloadId))
                .Returns((CancellationToken ct) =>
                {
                    handlerCalled = true;
                    return Task.CompletedTask;
                });

            var payloadJob = DataJob<TestPayload>.Create(Mock.Of<IJob>(), payload);

            Assert.That(payloadJob, Is.Not.Null);

            var resolvedAction = resolver.Object.Resolve(typeof(TestPayload), payload.PayloadId);
            await resolvedAction(CancellationToken.None);

            resolver.Verify(r => r.Resolve(typeof(TestPayload), payload.PayloadId), Times.Once);
            Assert.That(handlerCalled, Is.True);
        }

        private sealed class TestPayload : IPayload
        {
            public TestPayload(string handlerId)
            {
                PayloadId = handlerId;
            }

            public string PayloadId { get; }

            public DateTime CollectionTime => DateTime.UtcNow;

            public Guid HandlerId => Guid.NewGuid();
        }
    }
}
