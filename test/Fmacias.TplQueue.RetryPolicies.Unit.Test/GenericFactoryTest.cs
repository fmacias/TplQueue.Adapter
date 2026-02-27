using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using NUnit.Framework;

namespace Fmacias.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class GenericFactoryTest
    {
        private readonly GenericFactory _factory = GenericFactory.Create();

        [Test]
        public void Factory_Create_FromOptions_UsesCorrectPolicyKind()
        {
            var optionsByName = new Dictionary<string, IRetryPolicyDescriptor>(StringComparer.OrdinalIgnoreCase)
            {
                { "none",  RetryPolicyOptions.Create(baseDelayMs: 0,   maxRetries: 0).SetRetryPolicyType(typeof(NoRetryPolicy)) },
                { "linear", RetryPolicyOptions.Create(baseDelayMs: 200, maxRetries: 3).SetRetryPolicyType(typeof(LinearBackoff)) },
                { "exp",   RetryPolicyOptions.Create(baseDelayMs: 300, maxRetries: 4, factor: 2.5).SetRetryPolicyType(typeof(ExponentialBackoff)) }
            };

            var none = _factory.PolicyByName("none", optionsByName);
            var linear = _factory.PolicyByName("linear", optionsByName);
            var exp = _factory.PolicyByName("exp", optionsByName);

            Assert.That(none, Is.TypeOf<NoRetryPolicy>());
            Assert.That(linear, Is.TypeOf<LinearBackoff>());
            Assert.That(exp, Is.TypeOf<ExponentialBackoff>());
        }

        [Test]
        public void Factory_Create_SetFromDescriptor_BuiltInKinds()
        {
            // None
            var retryPolicyOptions = RetryPolicyOptions.Create(0, 0, 0).SetRetryPolicyType(typeof(NoRetryPolicy));
            var nonePolicy = _factory.PolicyByDescriptor(retryPolicyOptions);
            Assert.That(nonePolicy, Is.TypeOf<NoRetryPolicy>());

            // Linear
            var linearDescriptor = RetryPolicyOptions.Create(150,3).SetRetryPolicyType(typeof(LinearBackoff));
            var linearPolicy = _factory.PolicyByDescriptor(linearDescriptor);
            Assert.That(linearPolicy, Is.TypeOf<LinearBackoff>());

            // Exponential
            var expDescriptor = RetryPolicyOptions.Create(275,4,1.8).SetRetryPolicyType(typeof(ExponentialBackoff));
            var expPolicy = _factory.PolicyByDescriptor(expDescriptor);
            Assert.That(expPolicy, Is.TypeOf<ExponentialBackoff>());
        }

        [Test]
        public void Factory_Create_SetFromDescriptor_CustomPolicy_PopulatesProperties()
        {
            var descriptor = RetryPolicyOptions.Create(1234, 7, 1.5)
                .SetRetryPolicyType(typeof(CustomRetryPolicy));

            var policy = _factory.PolicyByDescriptor(descriptor);
            Assert.That(policy, Is.TypeOf<CustomRetryPolicy>());
       
            var custom = (CustomRetryPolicy)policy;
            Assert.That(custom.MaxRetries, Is.EqualTo(7));
            Assert.That(custom.BaseDelayMs, Is.EqualTo(1234));
            Assert.That(custom.Factor, Is.EqualTo(1.5));
            Assert.That(custom.ShouldRetry, Is.True, "Should Retry is a customized property of CustomRetryProlicy.");
        }

        [Test]
        public void PolicyByDescriptor_NullDescriptor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _factory.PolicyByDescriptor(null!));
        }

        [Test]
        public void PolicyByName_GenericTypeMismatch_ThrowsInvalidOperationException()
        {
            var options = new Dictionary<string, IRetryPolicyDescriptor>
            {
                { "linear", RetryPolicyOptions.Create(baseDelayMs: 50, maxRetries: 2).SetRetryPolicyType(typeof(LinearBackoff)) }
            };

            Assert.Throws<InvalidOperationException>(() => _factory.PolicyByName<IExponentialBackoff>("linear", options));
        }

        [Test]
        public void PolicyByName_MissingKey_ReturnsNoRetryPolicy()
        {
            var options = new Dictionary<string, IRetryPolicyDescriptor>
            {
                { "linear", RetryPolicyOptions.Create(baseDelayMs: 50, maxRetries: 2).SetRetryPolicyType(typeof(LinearBackoff)) }
            };

            var policy = _factory.PolicyByName("missing", options);

            Assert.That(policy, Is.TypeOf<NoRetryPolicy>());
        }

        [Test]
        public void PolicyByDescriptor_InvalidPolicyType_ThrowsInvalidOperationException()
        {
            var descriptor = new FakeRetryPolicyDescriptor(typeof(string), maxRetries: 1, baseDelayMs: 10, factor: 0);
            Assert.Throws<InvalidOperationException>(() => _factory.PolicyByDescriptor(descriptor));
        }

        [Test]
        public void GetPolicy_NoParameterlessConstructor_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _factory.GetPolicy<NoDefaultCtorPolicy>());
        }

        /// <summary>
        /// Custom policy used to validate the reflection-based plugin mechanism in RetryPolicyFactory.
        /// </summary>
        private sealed class CustomRetryPolicy : IRetryPolicy
        {
            // Properties expected by the reflective factory when populating a custom policy.
            public int MaxRetries { get; set; }
            public int BaseDelayMs { get; set; }
            public double Factor { get; set; }
            public bool ShouldRetry { get; set; }

            public int RetryCount { get; private set; }

            public async Task<TResult> ExecuteAsync<TResult>(
                Func<CancellationToken, Task<TResult>> action,
                CancellationToken cancellationToken)
            {
                if (action is null) throw new ArgumentNullException(nameof(action));

                RetryCount = 0;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        return await action(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        RetryCount++;

                        if (!ShouldRetry || RetryCount > MaxRetries)
                            throw;

                        await Task.Delay(TimeSpan.FromMilliseconds(BaseDelayMs), cancellationToken)
                                  .ConfigureAwait(false);
                    }
                }
            }

            public IRetryPolicy SetFromDescriptor(IRetryPolicyDescriptor descriptor)
            {
                BaseDelayMs = descriptor.BaseDelayMs;
                Factor = descriptor.Factor;
                ShouldRetry = true;
                MaxRetries = descriptor.MaxRetries;
                return this;
            }

            public IRetryPolicyDescriptor ToDescriptor(Type retryPolicyType)
            {
                if (retryPolicyType == null) throw new ArgumentNullException(nameof(retryPolicyType));

                return RetryPolicyOptions
                    .Create(BaseDelayMs, MaxRetries, Factor)
                    .SetRetryPolicyType(retryPolicyType);
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

            public IRetryPolicy SetFromDescriptor(IRetryPolicyDescriptor descriptor)
            {
                return this;
            }

            public IRetryPolicyDescriptor ToDescriptor(Type retryPolicyType)
            {
                return RetryPolicyOptions.Create(10, 1).SetRetryPolicyType(retryPolicyType);
            }
        }

        private sealed class FakeRetryPolicyDescriptor : IRetryPolicyDescriptor
        {
            public FakeRetryPolicyDescriptor(Type retryPolicyType, int maxRetries, int baseDelayMs, double factor)
            {
                RetryPolicyType = retryPolicyType;
                MaxRetries = maxRetries;
                BaseDelayMs = baseDelayMs;
                Factor = factor;
            }

            public int MaxRetries { get; }
            public int BaseDelayMs { get; }
            public double Factor { get; }
            public Type? RetryPolicyType { get; private set; }

            public IRetryPolicyDescriptor SetRetryPolicyType(Type retryPolicyType)
            {
                RetryPolicyType = retryPolicyType;
                return this;
            }
        }
    }
}
