using NUnit.Framework;
using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.RetryPolicies;

namespace Fmaciasruano.TplQueue.Test.Factories
{
    [TestFixture]
    public class RetryPolicyFactoryTests
    {
        [Test]
        public void Create_ByName_ReturnsConfiguredPolicy()
        {
            var opts = new Dictionary<string, RetryPolicyOptions>
            {
                { "exp", new RetryPolicyOptions(baseDelayMs:100, maxRetries:5, factor:2.0) }
            };

            var f = RetryPolicyFactory.Instance(opts);

            var p = f.Create("exp");
            Assert.That(p, Is.TypeOf<ExponentialBackoffRetryPolicy>());
        }

        [Test]
        public void Create_ByName_Unknown_Throws()
        {
            var f = RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>());
            Assert.Throws<KeyNotFoundException>(() => f.Create("missing"));
        }

        [Test]
        public void Create_FromOptions_WithFactor_UsesExponential()
        {
            var o = new RetryPolicyOptions(baseDelayMs:50, maxRetries:3,factor:2.0);
            var emptyOptions = new Dictionary<string, RetryPolicyOptions>();
            var f = RetryPolicyFactory.Instance(emptyOptions);
            var p = f.Create(o);
            Assert.That(p, Is.TypeOf<ExponentialBackoffRetryPolicy>());
        }

        [Test]
        public void Create_FromOptions_NoFactor_UsesLinear_WhenNonZero()
        {
            var o = new RetryPolicyOptions(baseDelayMs:10, maxRetries:3, null);
            var emptyOptions = new Dictionary<string, RetryPolicyOptions>();
            var f = RetryPolicyFactory.Instance(emptyOptions);
            var p = f.Create(o);
            Assert.That(p, Is.TypeOf<LinearBackoffRetryPolicy>());
        }

        [Test]
        public void Create_FromOptions_ZeroDefaults_ToNoRetry()
        {
            var o = new RetryPolicyOptions(baseDelayMs:0,maxRetries:0, null);
            var emptyOptions = new Dictionary<string, RetryPolicyOptions>();
            var f = RetryPolicyFactory.Instance(emptyOptions);
            var p = f.Create(o);
            Assert.That(p, Is.TypeOf<NoRetryPolicy>());
        }

        [Test]
        public void GetRetryPolicy_CastsOrThrows()
        {
            var opts = new Dictionary<string, RetryPolicyOptions>
            {
                { "lin", new RetryPolicyOptions(baseDelayMs:10, maxRetries:3, null) }
            };
            var f = RetryPolicyFactory.Instance(opts);

            var lin = f.GetRetryPolicy<LinearBackoffRetryPolicy>("lin");
            Assert.That(lin, Is.TypeOf<LinearBackoffRetryPolicy>());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                f.GetRetryPolicy<ExponentialBackoffRetryPolicy>("lin");
            });
        }
    }
}


