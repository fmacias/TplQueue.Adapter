using System;
using Fmaciasruano.TplQueue.Abstractions;
using NUnit.Framework;

namespace Fmaciasruano.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class RetryPolicyOptionsTest
    {
        [Test]
        public void Constructor_DisallowsNegativeValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicyOptions(baseDelayMs: -1, maxRetries: 1, factor: null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicyOptions(baseDelayMs: 10, maxRetries: -1, factor: null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicyOptions(baseDelayMs: 10, maxRetries: 1, factor: 0));
        }

        [Test]
        public void Constructor_DisallowsZeroDelayWhenRetriesRequested()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicyOptions(baseDelayMs: 0, maxRetries: 1, factor: null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicyOptions(baseDelayMs: 0, maxRetries: 2, factor: 2.0));
        }

        [Test]
        public void Constructor_AllowsNoRetryConfiguration()
        {
            var options = new RetryPolicyOptions(baseDelayMs: 0, maxRetries: 0, factor: null);
            Assert.That(options.BaseDelayMs, Is.EqualTo(0));
            Assert.That(options.MaxRetries, Is.EqualTo(0));
            Assert.That(options.Factor, Is.Null);
        }
    }
}
