using NUnit.Framework;

namespace Fmaciasruano.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class LinearBackoffRetryPolicyTests
    {
        [Test]
        public async Task ExecuteAsync_Succeeds_FirstTry_NoRetry()
        {
            var policy = new LinearBackoffRetryPolicy(maxRetries: 3, delayMs: 10);
            var calls = 0;

            var res = await policy.ExecuteAsync(ct =>
            {
                calls++;
                return Task.FromResult(5);
            }, CancellationToken.None);

            Assert.That(res, Is.EqualTo(5));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(policy.RetryCount, Is.EqualTo(0));
            Assert.That(policy.MaxRetries, Is.EqualTo(3));
            Assert.That(policy.Delay.TotalMilliseconds, Is.EqualTo(10).Within(0.1));
        }

        [Test]
        public async Task ExecuteAsync_RetriesUntilSuccess()
        {
            var policy = new LinearBackoffRetryPolicy(maxRetries: 3, delayMs: 1);
            var calls = 0;
            var check = 0;
            var result = await policy.ExecuteAsync(async ct =>
            {
                await Task.Delay(10, ct);
                calls++;
                if (calls < 3)
                    throw new InvalidOperationException("fail");
                check = 99;
                return Task.CompletedTask;
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.That(check, Is.EqualTo(99));
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(policy.RetryCount, Is.EqualTo(2));
        }

        [Test]
        public void ExecuteAsync_ExceedsMaxRetries_Throws()
        {
            var policy = new LinearBackoffRetryPolicy(maxRetries: 2, delayMs: 1);
            var calls = 0;

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await policy.ExecuteAsync<int>(ct =>
                {
                    calls++;
                    throw new InvalidOperationException("nope");
                }, CancellationToken.None);
            });

            // calls should be 3 total: initial + 2 retries => then throw
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(policy.RetryCount, Is.EqualTo(3));
        }
    }
}

