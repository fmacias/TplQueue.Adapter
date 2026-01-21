using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fmacias.TplQueue.RetryPolicies
{
    /// <summary>
    /// <![CDATA[
    /// Concrete RetryPolicyFactory that supports:
    ///
    ///  - Named creation via an options dictionary (string -> RetryPolicyOptions).
    ///  - Direct creation from RetryPolicyOptions.
    ///  - Rehydration from IRetryPolicyDescriptor (including plugin policies).
    ///  - Explicit helpers for NoRetry / Linear / Exponential.
    ///
    /// Plugin policies:
    ///  - Provide a concrete type implementing IRetryPolicy.
    ///  - Ensure a parameterless constructor (public or internal).
    ///  - Optionally expose settable properties named MaxRetries, BaseDelayMs,
    ///    Factor, ShouldRetry to be automatically populated from the descriptor.
    ///  - If the constructor is internal, use InternalsVisibleTo so this assembly
    ///    can instantiate it.
    /// ]]>
    /// </summary>
    public sealed class RetryPolicyFactory : IRetryPolicyFactory
    {
        private readonly IReadOnlyDictionary<string, RetryPolicyOptions> _optionsByName;

        private RetryPolicyFactory(IReadOnlyDictionary<string, RetryPolicyOptions> optionsByName)
        {
            _optionsByName = optionsByName ?? throw new ArgumentNullException(nameof(optionsByName));
        }

        /// <summary>
        /// Factory method hiding the concrete type.
        /// </summary>
        public static IRetryPolicyFactory Instance(IReadOnlyDictionary<string, RetryPolicyOptions> options)
        {
            return new RetryPolicyFactory(options);
        }

        /// <inheritdoc />
        public IRetryPolicy Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Retry policy name cannot be null or empty.", nameof(name));

            if (!_optionsByName.TryGetValue(name, out var retryPolicyOptions))
                throw new KeyNotFoundException($"Retry policy '{name}' not found.");

            return Create(retryPolicyOptions);
        }

        /// <inheritdoc />
        public IRetryPolicy Create(RetryPolicyOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            if (options.MaxRetries == 0)
            {
                return NoRetryPolicy.Create();
            }

            if (!options.Factor.HasValue)
            {
                var linearPolicy = ConstructRetryPolicy<LinearBackoffRetryPolicy>();
                linearPolicy.SetFromOptions(options);
                return linearPolicy;
            }

            var factor = options.Factor.Value;
            if (factor <= 0d)
                throw new ArgumentOutOfRangeException(nameof(options), "Factor must be greater than zero for exponential retry policies.");

            var exponentialPolicy = ConstructRetryPolicy<ExponentialBackoffRetryPolicy>();
            exponentialPolicy.SetFromOptions(options);

            return exponentialPolicy;
        }

        /// <inheritdoc />
        public IRetryPolicy Create(IRetryPolicyDescriptor descriptor)
        {
            if (descriptor is null)
                throw new ArgumentNullException(nameof(descriptor));

            if (IsRetryDisabled(descriptor))
            {
                return NoRetryPolicy.Create();
            }

            var kind = (descriptor.Kind ?? "none").ToUpperInvariant();

            switch (kind)
            {
                case "NONE":
                    return NoRetryPolicy.Create();

                case "LINEAR":
                    return CreateLinearFromDescriptor(descriptor);

                case "EXPONENTIAL":
                    return CreateExponentialFromDescriptor(descriptor);
            }

            if (descriptor.RetryPolicyType is { } type && !type.IsInterface)
            {
                return CreateCustomFromDescriptor(descriptor);
            }
            return NoRetryPolicy.Create();
        }

        /// <inheritdoc />
        public INoRetryPolicy CreateNoRetryPolicy()
            => NoRetryPolicy.Create();

        /// <inheritdoc />
        public IExponentialFactorRetryPolicy CreateExponentialBackoff(
            int maxRetries,
            double factor,
            bool shouldRetry,
            int baseDelayMilliseconds)
        {
            var options = RetryPolicyOptions.Exponential(
                baseDelayMs: baseDelayMilliseconds,
                maxRetries: maxRetries,
                factor: factor);

            return (IExponentialFactorRetryPolicy)Create(options);
        }

        /// <inheritdoc />
        public ILinearBackoffRetryPolicy CreateLinearBackoff(
            int maxRetries,
            int baseDelayMilliseconds)
        {
            var options = RetryPolicyOptions.Linear(
                baseDelayMs: baseDelayMilliseconds,
                maxRetries: maxRetries);
            var policy = ConstructRetryPolicy<LinearBackoffRetryPolicy>();
            policy.SetFromOptions(options);
            return policy;
        }

        /// <inheritdoc />
        public T GetRetryPolicy<T>(string name) where T : class, IRetryPolicy
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Retry policy name cannot be null or empty.", nameof(name));

            if (!_optionsByName.TryGetValue(name, out var retryPolicyOptions))
                throw new KeyNotFoundException($"Retry policy '{name}' not found.");

            return (T)ConstructRetryPolicy<T>().SetFromOptions(retryPolicyOptions);
        }

        private static bool IsRetryDisabled(IRetryPolicyDescriptor descriptor)
            => descriptor.ShouldRetry.HasValue && descriptor.ShouldRetry.Value == false;

        private static LinearBackoffRetryPolicy CreateLinearFromDescriptor(IRetryPolicyDescriptor descriptor)
        {
            var linearBackofRetryPolicy = ConstructRetryPolicy<LinearBackoffRetryPolicy>();
            linearBackofRetryPolicy.SetFromDescriptor(descriptor);
            return linearBackofRetryPolicy;
        }

        private static ExponentialBackoffRetryPolicy CreateExponentialFromDescriptor(IRetryPolicyDescriptor descriptor)
        {
            var exponentialPolicy = ConstructRetryPolicy<ExponentialBackoffRetryPolicy>();
            exponentialPolicy.SetFromDescriptor(descriptor);
            return exponentialPolicy;
        }

        private static IRetryPolicy CreateCustomFromDescriptor(IRetryPolicyDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            var constructorInfo = ContructPolicy(descriptor.RetryPolicyType!);

            var policyInstance = (IRetryPolicy)constructorInfo.Invoke(null);
            policyInstance.SetFromDescriptor(descriptor);
            return policyInstance;
        }

        private static T ConstructRetryPolicy<T>() where T: IRetryPolicy
        {
            var policyType = typeof(T);
            ConstructorInfo ctor = ContructPolicy(policyType);
            var policyInstance = (T)ctor.Invoke(null);

            if (policyInstance is null)
            {
                throw new InvalidOperationException(
                    $"Failed to create retry policy instance of type '{policyType.FullName}'.");
            }
            return policyInstance;
        }

        private static ConstructorInfo ContructPolicy(Type policyType)
        {
            var ctor = policyType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"RetryPolicyType '{policyType.FullName}' must provide a parameterless constructor " +
                    "so that it can be instantiated dynamically by the retry policy factory.");
            }

            return ctor;
        }
    }
}
