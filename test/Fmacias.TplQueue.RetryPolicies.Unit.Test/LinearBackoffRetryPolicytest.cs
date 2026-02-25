using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using NUnit.Framework;

namespace Fmacias.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class LinearBackoffRetryPolicyTests
    {
        [Test]
        public async Task ExecuteAsync_Succeeds_FirstTry_NoRetry()
        {
            var policy = new LinearBackoff(maxRetries: 3, delayMs: 10);
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
            var policy = new LinearBackoff(maxRetries: 3, delayMs: 1);
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
            var policy = new LinearBackoff(maxRetries: 2, delayMs: 1);
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

        [Test]
        public void Linear_SetFromDescriptor_UsesDescriptorValues()
        {
            // Arrange
            var descriptor = RetryPolicyOptions.Create(
                maxRetries: 4,
                baseDelayMs: 250);

            // Act
            var policy = new LinearBackoff();
            policy.SetFromDescriptor(descriptor);
            // Assert
            Assert.That(policy, Is.TypeOf<LinearBackoff>());
            var linear = (ILinearBackoff)policy;
            Assert.That(linear.MaxRetries, Is.EqualTo(4));
            Assert.That(linear.Delay.TotalMilliseconds, Is.EqualTo(250).Within(0.1));
        }

        [Test]
        public void Linear_SetFromDescriptor_UseDefaultMaxRetries()
        {
            // MaxRetries is 0
            var descInvalidRetries = RetryPolicyOptions.Create(
                maxRetries: 0,
                baseDelayMs: 100);

            var policy = new LinearBackoff();
            policy.SetFromDescriptor(descInvalidRetries);
            Assert.That(policy.MaxRetries, Is.GreaterThan(0));
            
            // BaseDelayMs is 0
            var descInvalidDelay = RetryPolicyOptions.Create(
                maxRetries: 3,
                baseDelayMs: 0);

            policy = new LinearBackoff();
            policy.SetFromDescriptor(descInvalidDelay);
            Assert.That(policy.Delay.TotalMilliseconds, Is.GreaterThan(0));
        }

        [Test]
        public void ExecuteAsync_WithNullAction_ThrowsArgumentNullException()
        {
            var policy = new LinearBackoff();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await policy.ExecuteAsync<int>(null!, CancellationToken.None));
        }
    }
}
