using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using NUnit.Framework;

namespace Fmacias.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class FactoryAbstractTest
    {
        private class TestRetryPolicy : IExponentialBackoff
        {
            public int RetryCount { get; private set; }

            public int MaxRetries { get; private set; }

            public TimeSpan Delay { get; private set; }

            public double Factor { get; private set; }

            public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken)
            {
                return action(cancellationToken);
            }

            public IRetryPolicy SetFromDescriptor(IRetryPolicyOptions descriptor)
            {
                MaxRetries = descriptor.MaxRetries;
                Factor = descriptor.Factor;
                Delay = TimeSpan.FromMilliseconds(descriptor.BaseDelayMs);
                return this;
            }

            public IRetryPolicyOptions ToDescriptor()
            {
                return RetryPolicyOptions
                    .Create(MaxRetries, (int)Math.Round(Delay.TotalMilliseconds));
            }
        }
        private class TestRetryPolicyFactory : FactoryAbstract<TestRetryPolicy>
        {
            private TestRetryPolicyFactory()
            { }
            public static TestRetryPolicyFactory Create()
            {
                return new TestRetryPolicyFactory();
            }
            public override TestRetryPolicy CreatePolicy()
            {
                return GetDefault();
            }

            public override TestRetryPolicy CreatePolicy(IRetryPolicyOptions descriptor)
            {
                return (TestRetryPolicy) new TestRetryPolicy().SetFromDescriptor(descriptor);
            }

            protected override TestRetryPolicy GetDefault()
            {
                return new TestRetryPolicy();
            }
        }
        [Test]
        public void Create_ByName_ReturnsConfiguredPolicy()
        {
            var opts = new Dictionary<string, IRetryPolicyOptions>
            {
                { "exp", RetryPolicyOptions.Create(baseDelayMs:100, maxRetries:5, factor:2.0) }
            };

            var f = TestRetryPolicyFactory.Create();

            var p = f.CreatePolicy("exp",opts);
            Assert.That(p, Is.TypeOf<TestRetryPolicy>());
            Assert.That(p.MaxRetries, Is.EqualTo(5));
            Assert.That(p.Factor, Is.EqualTo(2.0));
        }

        [Test]
        public void Create_ByName_Unknown_GetDefault()
        {
            var opts = new Dictionary<string, IRetryPolicyOptions>
            {
                { "exp", RetryPolicyOptions.Create(baseDelayMs:100, maxRetries:5, factor:2.0) }
            };
            var f = TestRetryPolicyFactory.Create();
            var defaultPolicy = f.CreatePolicy("unknown_name", opts);
            Assert.IsInstanceOf<TestRetryPolicy>(defaultPolicy);
        }

        [Test]
        public void Create_FromOptions_WithFactor()
        {
            var o = RetryPolicyOptions.Create(baseDelayMs:50, maxRetries:3,factor:2.0);
            var f = TestRetryPolicyFactory.Create();
            var p = f.CreatePolicy(o);
            Assert.That(p, Is.TypeOf<TestRetryPolicy>());
            Assert.That(p.Factor, Is.EqualTo(2.0));
        }

        [Test]
        public void Create_FromOptions()
        {
            var o = RetryPolicyOptions.Create(baseDelayMs:10, maxRetries:3);
            var f = TestRetryPolicyFactory.Create();
            var p = f.CreatePolicy(o);
            Assert.That(p, Is.TypeOf<TestRetryPolicy>());
            Assert.That(p.Delay.TotalMilliseconds, Is.EqualTo(10));
            Assert.That(p.MaxRetries, Is.EqualTo(3));
        }

        [Test]
        public void Create_FromOptions_ZeroDefaults()
        {
            var o = RetryPolicyOptions.Create(baseDelayMs:0,maxRetries:0);
            var f = TestRetryPolicyFactory.Create();
            var p = f.CreatePolicy(o);
            Assert.That(p.Delay.TotalMilliseconds, Is.EqualTo(0));
            Assert.That(p.MaxRetries, Is.EqualTo(0));
        }

        [Test]
        public void GetRetryPolicy_CastsOrThrows()
        {
            var opts = new Dictionary<string, IRetryPolicyOptions>
            {
                { "lin", RetryPolicyOptions.Create(baseDelayMs:10, maxRetries:3) }
            };
            var f = RetryPolicyAbstractFactory.Create();
            var policy = f.GetPolicy<TestRetryPolicy>();
            Assert.That(policy, Is.TypeOf<TestRetryPolicy>());
        }
    }
}


