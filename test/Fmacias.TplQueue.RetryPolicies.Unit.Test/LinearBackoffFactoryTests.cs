using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using NUnit.Framework;

namespace Fmacias.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class LinearBackoffFactoryTests
    {
        [Test]
        public void CreatePolicy_WithUnknownName_ReturnsDefaultPolicy()
        {
            IRetryPolicyFactory<ILinearBackoff> factory = LinearBackoffFactory.Create();
            var options = new Dictionary<string, IRetryPolicyOptions>();

            var policy = factory.CreatePolicy("missing", options);

            Assert.That(policy, Is.TypeOf<LinearBackoff>());
            Assert.That(policy.MaxRetries, Is.GreaterThan(0));
        }

        [Test]
        public void CreatePolicy_WithNullDescriptorInDictionary_ThrowsArgumentException()
        {
            IRetryPolicyFactory<ILinearBackoff> factory = LinearBackoffFactory.Create();
            var options = new Dictionary<string, IRetryPolicyOptions>
            {
                { "broken", null! }
            };

            Assert.Throws<ArgumentException>(() => factory.CreatePolicy("broken", options));
        }

        [Test]
        public void LinearBackoff_UsesProvidedValues()
        {
            var factory = LinearBackoffFactory.Create();

            var policy = factory.LinearBackoff(maxRetries: 5, delayMs: 300);

            Assert.That(policy.MaxRetries, Is.EqualTo(5));
            Assert.That(policy.Delay.TotalMilliseconds, Is.EqualTo(300).Within(0.1));
        }

        [Test]
        public void Create_ReturnsLinearBackoffFactoryInstance()
        {
            var factory = LinearBackoffFactory.Create();

            Assert.That(factory, Is.TypeOf<LinearBackoffFactory>());
        }
    }
}
