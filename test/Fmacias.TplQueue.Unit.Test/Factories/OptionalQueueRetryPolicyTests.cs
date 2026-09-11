using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Factories;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Factories
{
    [TestFixture]
    public class OptionalQueueRetryPolicyTests
    {
        [Test]
        public void QueueOptions_SelectDefaultOrNamedPolicy(
            [Values(null, "", " ", "retry")] string? name,
            [Values(false, true)] bool fifo,
            [Values(false, true)] bool namedQueue)
        {
            // Arrange: capture the factory passed to Core, using nullable custom options.
            var options = Mock.Of<IQOptions>(q => q.Id == Guid.NewGuid() && q.MaxParallelism == 2 && q.RetryPolicy == name);
            var configuredPolicy = Mock.Of<IRetryPolicy>();
            var policies = new Dictionary<string, IRetryPolicyOptions>();
            var retryFactory = new Mock<IRetryPolicyAbstractFactory>(MockBehavior.Strict);
            retryFactory.Setup(f => f.PolicyByName("retry", policies)).Returns(configuredPolicy);
            Func<IRetryPolicy>? selected = null;
            var core = new Mock<IQFactory>();
            core.Setup(f => f.Parallel(It.IsAny<Guid>(), "main", 2, It.IsAny<ILogger>(), It.IsAny<Func<IRetryPolicy>>()))
                .Callback<Guid, string, int, ILogger, Func<IRetryPolicy>>((_, _, _, _, factory) => selected = factory)
                .Returns(Mock.Of<IParallelQ>());
            core.Setup(f => f.Fifo(It.IsAny<Guid>(), "main", It.IsAny<ILogger>(), It.IsAny<Func<IRetryPolicy>>()))
                .Callback<Guid, string, ILogger, Func<IRetryPolicy>>((_, _, _, factory) => selected = factory)
                .Returns(Mock.Of<IFifoQ>());
            var adapter = QFactoryAdapter.Create(core.Object, retryFactory.Object,
                new Dictionary<string, IQOptions> { ["main"] = options }, policies);

            // Act
            var logger = Mock.Of<ILogger>();
            if (fifo)
            {
                _ = namedQueue ? adapter.Fifo("main", logger) : adapter.Fifo(options, "main", logger);
            }
            else
            {
                _ = namedQueue ? adapter.Parallel("main", logger) : adapter.Parallel(options, "main", logger);
            }
            Assert.That(selected, Is.Not.Null);
            var policy = selected!();

            // Assert: unnamed policies bypass lookup and execute a failing job exactly once.
            if (string.IsNullOrWhiteSpace(name))
            {
                Assert.That(policy, Is.InstanceOf<INoRetryPolicy>());
                var attempts = 0;
                var failure = new InvalidOperationException("job failed");
                var error = Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync<int>(_ =>
                {
                    attempts++;
                    return Task.FromException<int>(failure);
                }, CancellationToken.None));
                Assert.That(error, Is.SameAs(failure));
                Assert.That(attempts, Is.EqualTo(1));
                retryFactory.VerifyNoOtherCalls();
            }
            else
            {
                Assert.That(policy, Is.SameAs(configuredPolicy));
                retryFactory.Verify(f => f.PolicyByName("retry", policies), Times.Once);
            }
        }
    }
}
