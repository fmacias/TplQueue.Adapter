using Fmacias.TplQueue;
using NUnit.Framework;

namespace Fmacias.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class NoRetryPolicyTests
    {
        [Test]
        public async Task ExecuteAsync_RunsOnceAndReturnsResult()
        {
            var policy = NoRetryPolicy.Create();
            int calls = 0;

            var result = await policy.ExecuteAsync(ct =>
            {
                calls++;
                return Task.FromResult(42);
            }, CancellationToken.None);

            Assert.That(result, Is.EqualTo(42));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(policy.RetryCount, Is.EqualTo(0));
        }

        [Test]
        public void ExecuteAsync_ThrowsExceptionWithoutRetry()
        {
            var policy = NoRetryPolicy.Create();
            int calls = 0;

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await policy.ExecuteAsync<int>(ct =>
                {
                    calls++;
                    throw new InvalidOperationException("boom");
                }, CancellationToken.None);
            });

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(policy.RetryCount, Is.EqualTo(0));
        }
    }
}

