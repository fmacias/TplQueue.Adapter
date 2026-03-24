using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using NUnit.Framework;

namespace Fmacias.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class GenericFactoryTest
    {
        private readonly RetryPolicyAbstractFactory _factory = RetryPolicyAbstractFactory.Create();

        [Test]
        public void Factory_Create_FromOptions_UsesCorrectPolicyKind()
        {
            var optionsByName = new Dictionary<string, IRetryPolicyOptions>(StringComparer.OrdinalIgnoreCase)
            {
                { "none", RetryPolicyOptions.Create(baseDelayMs: 0, maxRetries: 0) },
                { "linear", RetryPolicyOptions.Create(baseDelayMs: 200, maxRetries: 3) },
                { "exp", RetryPolicyOptions.Create(baseDelayMs: 300, maxRetries: 4, factor: 2.5) }
            };

            var none = _factory.PolicyByName("none", optionsByName);
            var linear = _factory.PolicyByName("linear", optionsByName);
            var exp = _factory.PolicyByName("exp", optionsByName);

            Assert.That(none, Is.TypeOf<NoRetryPolicy>());
            Assert.That(linear, Is.TypeOf<LinearBackoff>());
            Assert.That(exp, Is.TypeOf<ExponentialBackoff>());
        }

        [Test]
        public void Factory_Create_FromOptions_DoesNotDependOnSerializedType()
        {
            var nonePolicy = _factory.PolicyByOptions(
                RetryPolicyOptions.Create(baseDelayMs: 0, maxRetries: 0));

            var linearPolicy = _factory.PolicyByOptions(
                RetryPolicyOptions.Create(baseDelayMs: 150, maxRetries: 3));

            var exponentialPolicy = _factory.PolicyByOptions(
                RetryPolicyOptions.Create(baseDelayMs: 275, maxRetries: 4, factor: 1.8));

            Assert.That(nonePolicy, Is.TypeOf<NoRetryPolicy>());
            Assert.That(linearPolicy, Is.TypeOf<LinearBackoff>());
            Assert.That(exponentialPolicy, Is.TypeOf<ExponentialBackoff>());
        }

        [Test]
        public void PolicyByDescriptor_NullDescriptor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _factory.PolicyByOptions(null!));
        }

        [Test]
        public void PolicyByName_Generic_ReturnsRequestedPolicyType()
        {
            var options = new Dictionary<string, IRetryPolicyOptions>
            {
                { "linear", RetryPolicyOptions.Create(baseDelayMs: 50, maxRetries: 2, factor: 3.0) }
            };

            var policy = _factory.PolicyByName<ILinearBackoff>("linear", options);

            Assert.That(policy, Is.TypeOf<LinearBackoff>());
            Assert.That(policy.MaxRetries, Is.EqualTo(2));
            Assert.That(policy.Delay.TotalMilliseconds, Is.EqualTo(50));
        }

        [Test]
        public void PolicyByName_Generic_MissingKey_ThrowsKeyNotFoundException()
        {
            var options = new Dictionary<string, IRetryPolicyOptions>
            {
                { "linear", RetryPolicyOptions.Create(baseDelayMs: 50, maxRetries: 2) }
            };

            Assert.Throws<KeyNotFoundException>(() => _factory.PolicyByName<ILinearBackoff>("missing", options));
        }

        [Test]
        public void PolicyByName_Generic_InterfaceWithoutKnownMapping_ThrowsInvalidOperationException()
        {
            var options = new Dictionary<string, IRetryPolicyOptions>
            {
                { "custom", RetryPolicyOptions.Create(baseDelayMs: 50, maxRetries: 2) }
            };

            Assert.Throws<InvalidOperationException>(() => _factory.PolicyByName<ICustomRetryPolicy>("custom", options));
        }

        [Test]
        public void PolicyByName_MissingKey_ReturnsNoRetryPolicy()
        {
            var options = new Dictionary<string, IRetryPolicyOptions>
            {
                { "linear", RetryPolicyOptions.Create(baseDelayMs: 50, maxRetries: 2) }
            };

            var policy = _factory.PolicyByName("missing", options);

            Assert.That(policy, Is.TypeOf<NoRetryPolicy>());
        }

        [Test]
        public void PolicyByOptions_InvalidOptions_ReturnsNoRetryPolicy()
        {
            var descriptor = new FakeRetryPolicyDescriptor(maxRetries: -1, baseDelayMs: 10, factor: 0);

            var policy = _factory.PolicyByOptions(descriptor);

            Assert.That(policy, Is.TypeOf<NoRetryPolicy>());
        }

        [Test]
        public void GetPolicy_NoParameterlessConstructor_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _factory.GetPolicy<NoDefaultCtorPolicy>());
        }

        [Test]
        public void GetPolicy_ConcreteCustomPolicy_CreatesRequestedType()
        {
            var policy = _factory.GetPolicy<CustomRetryPolicy>();

            Assert.That(policy, Is.TypeOf<CustomRetryPolicy>());
        }

        private interface ICustomRetryPolicy : IRetryPolicy
        {
        }

        private sealed class CustomRetryPolicy : IRetryPolicy
        {
            public int RetryCount => 0;

            public Task<TResult> ExecuteAsync<TResult>(
                Func<CancellationToken, Task<TResult>> action,
                CancellationToken cancellationToken)
            {
                if (action is null) throw new ArgumentNullException(nameof(action));
                return action(cancellationToken);
            }

            public IRetryPolicy SetFromDescriptor(IRetryPolicyOptions descriptor)
            {
                return this;
            }

            public IRetryPolicyOptions ToDescriptor()
            {
                return RetryPolicyOptions.Create(10, 1);
            }
        }

        private sealed class NoDefaultCtorPolicy : IRetryPolicy
        {
            public NoDefaultCtorPolicy(int maxRetries)
            {
                MaxRetries = maxRetries;
            }

            public int MaxRetries { get; }
            public int RetryCount => 0;

            public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken)
            {
                if (action is null) throw new ArgumentNullException(nameof(action));
                return action(cancellationToken);
            }

            public IRetryPolicy SetFromDescriptor(IRetryPolicyOptions descriptor)
            {
                return this;
            }

            public IRetryPolicyOptions ToDescriptor()
            {
                return RetryPolicyOptions.Create(10, 1);
            }
        }

        private sealed class FakeRetryPolicyDescriptor : IRetryPolicyOptions
        {
            public FakeRetryPolicyDescriptor(int maxRetries, int baseDelayMs, double factor)
            {
                MaxRetries = maxRetries;
                BaseDelayMs = baseDelayMs;
                Factor = factor;
            }

            public int MaxRetries { get; }
            public int BaseDelayMs { get; }
            public double Factor { get; }
        }
    }
}
