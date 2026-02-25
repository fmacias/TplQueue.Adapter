using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using NUnit.Framework;

namespace Fmacias.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class ExponentialBackoffRetryPolicyTests
    {
        [Test]
        public async Task ExecuteAsync_SuccessFirst_NoRetry()
        {
            var policy = new ExponentialBackoff(
                maxRetries: 3,delayMs: 5, factor: 2.0);
            var calls = 0;
            var res = await policy.ExecuteAsync(ct =>
            {
                calls++;
                return Task.FromResult(7);
            }, CancellationToken.None);

            Assert.That(res, Is.EqualTo(7));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(policy.RetryCount, Is.EqualTo(0));
            Assert.That(policy.MaxRetries, Is.EqualTo(3));
            Assert.That(policy.Delay.TotalMilliseconds, Is.EqualTo(5).Within(0.1));
        }

        [Test]
        public async Task ExecuteAsync_EventuallySucceeds()
        {
            var policy = new ExponentialBackoff(
                maxRetries: 3, delayMs:1,factor: 2.0);

            var calls = 0;
            var result = await policy.ExecuteAsync(ct =>
            {
                calls++;
                if (calls < 3) throw new ApplicationException("x");
                return Task.FromResult(123);
            }, CancellationToken.None);

            Assert.That(result, Is.EqualTo(123));
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(policy.RetryCount, Is.EqualTo(2));
        }

        [Test]
        public void ExecuteAsync_ExhaustsRetries_Throws()
        {
            var policy = new ExponentialBackoff(
                maxRetries: 2, delayMs:1, factor: 2.0);

            var calls = 0;

            Assert.ThrowsAsync<ApplicationException>(async () =>
            {
                await policy.ExecuteAsync<int>(ct =>
                {
                    calls++;
                    throw new ApplicationException("boom");
                }, CancellationToken.None);
            });

            // calls:
            // 1st attempt + 2 retries (because MaxRetries=2) => 3 total before throw
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(policy.RetryCount, Is.EqualTo(3));
        }

        [Test]
        [TestCase(-1, 2.0, 10)]
        [TestCase(1, 0.0, 10)]
        [TestCase(1, 2.0, 0)]
        public void Create_WithInvalidParameters_UsingDefaults(int maxRetries, double factor, int baseDelayMs)
        {
            var policy = new ExponentialBackoff(maxRetries,baseDelayMs, factor);
            Assert.That(policy.MaxRetries, Is.GreaterThanOrEqualTo(1));
            Assert.That(policy.Factor, Is.GreaterThanOrEqualTo(1.0));
            Assert.That(policy.Delay, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(1)));
        }

        [Test]
        public void Exponential_SetFromDescriptor()
        {
            // Arrange
            var descriptor = RetryPolicyOptions.Create(
                maxRetries: 5,
                baseDelayMs: 350,
                factor: 3.0);

            // Act
            var policy = new ExponentialBackoff();
            policy.SetFromDescriptor(descriptor);

            // Assert
            Assert.That(policy, Is.TypeOf<ExponentialBackoff>());
            var exp = (IExponentialBackoff)policy;
            Assert.That(exp.MaxRetries, Is.EqualTo(5));
            Assert.That(exp.Delay.TotalMilliseconds, Is.EqualTo(350).Within(0.1));
            Assert.That(exp.Factor, Is.EqualTo(3.0));
        }

        [Test]
        public void Exponential_SetFromDescriptor_UsesDefaultValues()
        {
            //Arrange
            var descriptor = RetryPolicyOptions.Create(
                maxRetries: 0,
                baseDelayMs: 0,
                factor: 0.0);
            var policy = new ExponentialBackoff();

            //ACT
            policy.SetFromDescriptor(descriptor);

            //ASSERT
            Assert.That(policy.MaxRetries, Is.GreaterThan(0));
            Assert.That(policy.Delay.TotalMinutes, Is.GreaterThan(0));
            Assert.That(policy.Factor, Is.GreaterThan(0d));
        }

        [Test]
        public void ExecuteAsync_WithNullAction_ThrowsArgumentNullException()
        {
            var policy = new ExponentialBackoff();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await policy.ExecuteAsync<int>(null!, CancellationToken.None));
        }

    }
}

