using Fmacias.TplQueue.Defaults;
using NUnit.Framework;

namespace Fmacias.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class RetryPolicyOptionsTest
    {
        [Test]
        public void Constructor_DisallowsNegativeValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyOptions.Create(baseDelayMs: -1, maxRetries: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyOptions.Create(baseDelayMs: 10, maxRetries: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyOptions.Create(baseDelayMs: 10, maxRetries: 1, factor: -1d));
        }

        [Test]
        public void Constructor_AllowsZero()
        {
            Assert.IsInstanceOf<RetryPolicyOptions>(RetryPolicyOptions.Create(baseDelayMs: 0, maxRetries: 1));
            Assert.IsInstanceOf<RetryPolicyOptions>(RetryPolicyOptions.Create(baseDelayMs: 0, maxRetries: 2, factor: 2.0));
        }

        [Test]
        public void Constructor_AllowsNoRetryConfiguration()
        {
            var options = RetryPolicyOptions.Create(baseDelayMs: 0, maxRetries: 0);
            Assert.That(options.BaseDelayMs, Is.EqualTo(0));
            Assert.That(options.MaxRetries, Is.EqualTo(0));
            Assert.That(options.Factor, Is.EqualTo(0d));
        }
    }
}
