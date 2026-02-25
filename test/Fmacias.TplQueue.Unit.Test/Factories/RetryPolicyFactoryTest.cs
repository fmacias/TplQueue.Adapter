using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Fmacias.TplQueue.RetryPolicies;
using NUnit.Framework;

namespace Fmacias.TplQueue.Test.Factories
{
    [TestFixture]
    public class RetryPolicyFactoryTests
    {
        private class TestRetryPolicy : IBackoffRetryPolicy
        {
            public int RetryCount => 1;

            public int MaxRetries => 1;

            public TimeSpan Delay => TimeSpan.FromMilliseconds(100);

            public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken)
            {
                return action(cancellationToken);
            }

            public IRetryPolicy SetFromDescriptor(IRetryPolicyDescriptor descriptor)
            {
                return this;
            }

            public IRetryPolicyDescriptor ToDescriptor(Type retryPolicyType)
            {
                return RetryPolicyOptions
                    .Create(MaxRetries, (int)Math.Round(Delay.TotalMilliseconds))
                    .SetRetryPolicyType(retryPolicyType);
            }
        }
        private class TestRetryPolicyFactory : RetryPolicyFactoryAbstract<TestRetryPolicy>
        {
            private TestRetryPolicyFactory()
            { }
            public static TestRetryPolicyFactory Create()
            {
                return new TestRetryPolicyFactory();
            }
            public override TestRetryPolicy CreatePolicy()
            {
                return new TestRetryPolicy();
            }

            protected override TestRetryPolicy CreatePolicy(IRetryPolicyDescriptor descriptor)
            {
                return (TestRetryPolicy) new TestRetryPolicy().SetFromDescriptor(descriptor);
            }

            protected override TestRetryPolicy GetDefault()
            {
                throw new NotImplementedException();
            }
        }
        [Test]
        public void Create_ByName_ReturnsConfiguredPolicy()
        {
            var opts = new Dictionary<string, IRetryPolicyDescriptor>
            {
                { "exp", RetryPolicyOptions.Create(baseDelayMs:100, maxRetries:5, factor:2.0) }
            };

            var f = TestRetryPolicyFactory.Create();

            var p = f.CreatePolicy();
            Assert.That(p, Is.TypeOf<ExponentialBackoff>());
        }

        [Test]
        public void Create_ByName_Unknown_Throws()
        {
            var f = TestRetryPolicyFactory.Create();
            Assert.Throws<KeyNotFoundException>(() => f.CreatePolicy());
        }

        [Test]
        public void Create_FromOptions_WithFactor_UsesExponential()
        {
            var o = RetryPolicyOptions.Create(baseDelayMs:50, maxRetries:3,factor:2.0);
            var emptyOptions = new Dictionary<string, RetryPolicyOptions>();
            var f = TestRetryPolicyFactory.Create();
            var p = f.CreatePolicy();
            Assert.That(p, Is.TypeOf<ExponentialBackoff>());
        }

        [Test]
        public void Create_FromOptions_NoFactor_UsesLinear_WhenNonZero()
        {
            var o = RetryPolicyOptions.Create(baseDelayMs:10, maxRetries:3);
            var emptyOptions = new Dictionary<string, RetryPolicyOptions>();
            var f = TestRetryPolicyFactory.Create();
            var p = f.CreatePolicy();
            Assert.That(p, Is.TypeOf<LinearBackoff>());
        }

        [Test]
        public void Create_FromOptions_ZeroDefaults_ToNoRetry()
        {
            var o = RetryPolicyOptions.Create(baseDelayMs:0,maxRetries:0);
            var emptyOptions = new Dictionary<string, RetryPolicyOptions>();
            var f = TestRetryPolicyFactory.Create();
            var p = f.CreatePolicy();
            Assert.That(p, Is.TypeOf<NoRetryPolicy>());
        }

        [Test]
        public void GetRetryPolicy_CastsOrThrows()
        {
            var opts = new Dictionary<string, IRetryPolicyDescriptor>
            {
                { "lin", RetryPolicyOptions.Create(baseDelayMs:10, maxRetries:3) }
            };
            var f = RetryPolicyGenericFactory.Create();
            var lin = f.GetPolicy<LinearBackoff>();
            Assert.That(lin, Is.TypeOf<LinearBackoff>());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                f.GetPolicy<ExponentialBackoff>();
            });
        }
    }
}


