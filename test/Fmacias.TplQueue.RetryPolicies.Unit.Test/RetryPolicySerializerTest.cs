using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using NUnit.Framework;

namespace Fmaciasruano.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class RetryPolicySerializerTest
    {
        [Test]
        public void Exponential_FromDescriptor_UsesDescriptorValues()
        {
            // Arrange
            var descriptor = RetryPolicyDescriptor.Exponential(
                maxRetries: 5,
                baseDelayMs: 350,
                factor: 3.0);

            // Act
            var policy = new ExponentialBackoffRetryPolicy();
            policy.SetFromDescriptor(descriptor);

            // Assert
            Assert.That(policy, Is.TypeOf<ExponentialBackoffRetryPolicy>());
            var exp = (IExponentialFactorRetryPolicy)policy;
            Assert.That(exp.MaxRetries, Is.EqualTo(5));
            Assert.That(exp.Delay.TotalMilliseconds, Is.EqualTo(350).Within(0.1));
            Assert.That(exp.Factor, Is.EqualTo(3.0));
        }

        [Test]
        public void Linear_FromDescriptor_UsesDescriptorValues()
        {
            // Arrange
            var descriptor = RetryPolicyDescriptor.Linear(
                maxRetries: 4,
                baseDelayMs: 250);

            // Act
            var policy = new LinearBackoffRetryPolicy();
            policy.SetFromDescriptor(descriptor);
            // Assert
            Assert.That(policy, Is.TypeOf<LinearBackoffRetryPolicy>());
            var linear = (ILinearBackoffRetryPolicy)policy;
            Assert.That(linear.MaxRetries, Is.EqualTo(4));
            Assert.That(linear.Delay.TotalMilliseconds, Is.EqualTo(250).Within(0.1));
        }

        [Test]
        public void Linear_FromDescriptor_InvalidValues_Throw()
        {
            // MaxRetries <= 0
            var descInvalidRetries = RetryPolicyDescriptor.Linear(
                maxRetries: 0,
                baseDelayMs: 100);

            var policy = new LinearBackoffRetryPolicy();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => policy.SetFromDescriptor(descInvalidRetries));

            // BaseDelayMs <= 0
            var descInvalidDelay = RetryPolicyDescriptor.Linear(
                maxRetries: 3,
                baseDelayMs: 0);

            policy = new LinearBackoffRetryPolicy();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => policy.SetFromDescriptor(descInvalidDelay));
        }

        [Test]
        public void Exponential_FromDescriptor_InvalidValues_Throw()
        {
            var descriptor = RetryPolicyDescriptor.Exponential(
                maxRetries: -1,
                baseDelayMs: 0,
                factor: 0.0);
            var policy = new ExponentialBackoffRetryPolicy();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => policy.SetFromDescriptor(descriptor));
        }

        [Test]
        public void Factory_Create_FromOptions_UsesCorrectPolicyKind()
        {
            var optionsByName = new Dictionary<string, RetryPolicyOptions>(StringComparer.OrdinalIgnoreCase)
            {
                { "none",  RetryPolicyOptions.Linear(baseDelayMs: 0,   maxRetries: 0) },
                { "linear", RetryPolicyOptions.Linear(baseDelayMs: 200, maxRetries: 3) },
                { "exp",   RetryPolicyOptions.Exponential(baseDelayMs: 300, maxRetries: 4, factor: 2.5) }
            };

            var factory = RetryPolicyFactory.Instance(optionsByName);

            var none = factory.Create("none");
            var linear = factory.Create("linear");
            var exp = factory.Create("exp");

            Assert.That(none, Is.TypeOf<NoRetryPolicy>());
            Assert.That(linear, Is.TypeOf<LinearBackoffRetryPolicy>());
            Assert.That(exp, Is.TypeOf<ExponentialBackoffRetryPolicy>());
        }

        [Test]
        public void Factory_Create_FromDescriptor_BuiltInKinds()
        {
            var factory = RetryPolicyFactory.Instance(
                new Dictionary<string, RetryPolicyOptions>(StringComparer.OrdinalIgnoreCase));

            // None
            var noneDescriptor = RetryPolicyDescriptor.None;
            var nonePolicy = factory.Create(noneDescriptor);
            Assert.That(nonePolicy, Is.TypeOf<NoRetryPolicy>());

            // Linear
            var linearDescriptor = RetryPolicyDescriptor.Linear(
                maxRetries: 3,
                baseDelayMs: 150);
            var linearPolicy = factory.Create(linearDescriptor);
            Assert.That(linearPolicy, Is.TypeOf<LinearBackoffRetryPolicy>());

            // Exponential
            var expDescriptor = RetryPolicyDescriptor.Exponential(
                maxRetries: 4,
                baseDelayMs: 275,
                factor: 1.8);
            var expPolicy = factory.Create(expDescriptor);
            Assert.That(expPolicy, Is.TypeOf<ExponentialBackoffRetryPolicy>());
        }

        [Test]
        public void Factory_Create_FromDescriptor_CustomPolicy_PopulatesProperties()
        {
            var optionsByName = new Dictionary<string, RetryPolicyOptions>(StringComparer.OrdinalIgnoreCase);
            var factory = RetryPolicyFactory.Instance(optionsByName);

            var descriptor = RetryPolicyDescriptor.Personalized(
                kind: "custom",
                retrypolicyType: typeof(CustomRetryPolicy),
                maxRetries: 7,
                baseDelayMs: 1234,
                factor: 1.5,
                shouldRetry: true);

            var policy = factory.Create(descriptor);

            Assert.That(policy, Is.TypeOf<CustomRetryPolicy>());
            var custom = (CustomRetryPolicy)policy;

            Assert.That(custom.MaxRetries, Is.EqualTo(7));
            Assert.That(custom.BaseDelayMs, Is.EqualTo(1234));
            Assert.That(custom.Factor, Is.EqualTo(1.5));
            Assert.That(custom.ShouldRetry, Is.True);
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
                BaseDelayMs = descriptor.BaseDelayMs ?? 0;
                Factor = descriptor.Factor ?? 0d;
                ShouldRetry = descriptor.ShouldRetry ?? true;
                MaxRetries = descriptor.MaxRetries ?? 0;
                return this;
            }

            public IRetryPolicy SetFromOptions(RetryPolicyOptions options)
            {
                throw new NotImplementedException();
            }

            public IRetryPolicyDescriptor ToDescriptor()
            {
                return RetryPolicyDescriptor.Personalized(
                    kind: "custom",
                    retrypolicyType: typeof(CustomRetryPolicy),
                    maxRetries: MaxRetries,
                    baseDelayMs: BaseDelayMs,
                    factor: Factor,
                    shouldRetry: ShouldRetry);
            }
        }
    }
}
