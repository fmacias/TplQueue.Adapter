using Fmacias.TplQueue.Contracts;
using NUnit.Framework;

namespace Fmacias.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class ExponentialBackoffFactoryTests
    {
        [Test]
        public void CreatePolicy_WithUnknownName_ReturnsDefaultPolicy()
        {
            IRetryPolicyFactory<IExponentialBackoff> factory = ExponentialBackoffFactory.Create();
            var options = new Dictionary<string, IRetryPolicyOptions>();

            var policy = factory.CreatePolicy("missing", options);

            Assert.That(policy, Is.TypeOf<ExponentialBackoff>());
            Assert.That(policy.MaxRetries, Is.GreaterThan(0));
        }

        [Test]
        public void CreatePolicy_WithNullDescriptorInDictionary_ThrowsArgumentException()
        {
            IRetryPolicyFactory<IExponentialBackoff> factory = ExponentialBackoffFactory.Create();
            var options = new Dictionary<string, IRetryPolicyOptions>
            {
                { "broken", null! }
            };

            Assert.Throws<ArgumentException>(() => factory.CreatePolicy("broken", options));
        }

        [Test]
        public void CreateExponentialBackoff_UsesProvidedValues()
        {
            var factory = (IExponentialBackofFactory)ExponentialBackoffFactory.Create();

            var policy = factory.CreateExponentialBackoff(maxRetries: 4, delayMs: 150, factor: 2.5);

            Assert.That(policy.MaxRetries, Is.EqualTo(4));
            Assert.That(policy.Delay.TotalMilliseconds, Is.EqualTo(150).Within(0.1));
            Assert.That(policy.Factor, Is.EqualTo(2.5));
        }
    }
}
