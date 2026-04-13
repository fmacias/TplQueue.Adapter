using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using System;
using System.Collections.Generic;

namespace Fmacias.TplQueue.RetryPolicies
{
    /// <summary>
    /// Resolves retry policies from configured options and creates default retry policy instances.
    /// </summary>
    /// <remarks>
    /// Generic methods support the built-in retry policy interfaces directly. Custom policies
    /// should be requested by concrete type and must expose a public parameterless constructor.
    /// </remarks>
    public sealed class RetryPolicyAbstractFactory : IRetryPolicyAbstractFactory
    {
        private RetryPolicyAbstractFactory()
        {
        }

        /// <summary>
        /// Creates a retry policy abstract factory instance.
        /// </summary>
        public static RetryPolicyAbstractFactory Create()
        {
            return new RetryPolicyAbstractFactory();
        }

        /// <inheritdoc />
        public IRetryPolicy PolicyByName(string name, IReadOnlyDictionary<string, IRetryPolicyOptions> options)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Retry policy name cannot be null or empty.", nameof(name));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (!options.TryGetValue(name, out var retryPolicyOptions))
            {
                return NoRetryPolicy.Create();
            }

            return PolicyByOptions(retryPolicyOptions);
        }

        /// <inheritdoc />
        public T PolicyByName<T>(string name, IReadOnlyDictionary<string, IRetryPolicyOptions> options)
            where T : class, IRetryPolicy
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Retry policy name cannot be null or empty.", nameof(name));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (!options.TryGetValue(name, out var retryPolicyOptions))
            {
                throw new KeyNotFoundException($"No retry policy descriptor was found for key '{name}'.");
            }

            return CreateTypedPolicy<T>(retryPolicyOptions);
        }

        /// <inheritdoc />
        public IRetryPolicy PolicyByOptions(IRetryPolicyOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            return CreateBuiltInPolicyFromOptions(options);
        }

        /// <inheritdoc />
        public T GetPolicy<T>() where T : class, IRetryPolicy
        {
            return CreatePolicyByType<T>();
        }

        /// <summary>
        /// Creates a built-in retry policy from options that describe the retry shape.
        /// </summary>
        /// <param name="options">The retry policy options to apply.</param>
        /// <returns>
        /// A no-retry policy when the options do not describe an enabled retry policy, otherwise
        /// a linear or exponential backoff policy.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        private static IRetryPolicy CreateBuiltInPolicyFromOptions(IRetryPolicyOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (!CanCreatePolicy(options))
            {
                return NoRetryPolicy.Create();
            }

            if (options.Factor > 0d)
            {
                return new ExponentialBackoff().SetFromDescriptor(options);
            }

            return new LinearBackoff().SetFromDescriptor(options);
        }

        /// <summary>
        /// Determines whether options represent a retrying policy instead of the no-retry fallback.
        /// </summary>
        /// <param name="options">The retry policy options to inspect.</param>
        /// <returns><c>true</c> when the options can create a retrying policy; otherwise <c>false</c>.</returns>
        private static bool CanCreatePolicy(IRetryPolicyOptions options)
        {
            return options.MaxRetries > 0
                && options.BaseDelayMs >= 0
                && !double.IsNaN(options.Factor)
                && !double.IsInfinity(options.Factor)
                && options.Factor >= 0d;
        }

        /// <summary>
        /// Creates and configures a retry policy from options.
        /// </summary>
        /// <typeparam name="T">
        /// The requested retry policy type. Built-in retry policy interfaces are mapped to their
        /// internal implementations. Custom policies must use a concrete type with a public
        /// parameterless constructor.
        /// </typeparam>
        /// <param name="options">The retry policy options to apply.</param>
        /// <returns>The configured retry policy instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <typeparamref name="T"/> returns a different policy type from
        /// <see cref="IRetryPolicySerializable.SetFromDescriptor(IRetryPolicyOptions)"/>.
        /// </exception>
        private static T CreateTypedPolicy<T>(IRetryPolicyOptions options)
            where T : class, IRetryPolicy
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var policy = PolicyByType(typeof(T)).SetFromDescriptor(options);
            if (policy is T typedPolicy)
            {
                return typedPolicy;
            }

            throw new InvalidOperationException(
                $"Retry policy '{GetPolicyTypeName(typeof(T))}' returned '{GetPolicyTypeName(policy.GetType())}' " +
                "when configured from retry policy options.");
        }

        private static T CreatePolicyByType<T>()
            where T : class, IRetryPolicy
        {
            var policy = PolicyByType(typeof(T));
            if (policy is T typedPolicy)
            {
                return typedPolicy;
            }

            throw new InvalidOperationException(
                $"Retry policy '{GetPolicyTypeName(policy.GetType())}' could not be returned as '{GetPolicyTypeName(typeof(T))}'.");
        }

        private static IRetryPolicy PolicyByType(Type policyType)
        {
            if (policyType == null)
                throw new ArgumentNullException(nameof(policyType));

            if (policyType == typeof(INoRetryPolicy) || policyType == typeof(NoRetryPolicy))
            {
                return NoRetryPolicy.Create();
            }

            if (policyType == typeof(ILinearBackoff) || policyType == typeof(LinearBackoff))
            {
                return new LinearBackoff();
            }

            if (policyType == typeof(IExponentialBackoff) || policyType == typeof(ExponentialBackoff))
            {
                return new ExponentialBackoff();
            }

            if (policyType.IsInterface || policyType.IsAbstract)
            {
                throw new InvalidOperationException(
                    $"RetryPolicyType '{GetPolicyTypeName(policyType)}' must be a supported built-in interface or a concrete retry policy type.");
            }

            var constructorInfo = policyType.GetConstructor(Type.EmptyTypes);
            if (constructorInfo == null)
            {
                throw new InvalidOperationException(
                    $"RetryPolicyType '{GetPolicyTypeName(policyType)}' must provide a public parameterless constructor " +
                    "so that it can be instantiated by the retry policy factory.");
            }

            return (IRetryPolicy)constructorInfo.Invoke(null);
        }

        /// <summary>
        /// Returns a stable display name for exception messages.
        /// </summary>
        /// <param name="policyType">The retry policy type to display.</param>
        /// <returns>The full type name when available; otherwise the simple type name.</returns>
        private static string GetPolicyTypeName(Type policyType)
        {
            return policyType.FullName ?? policyType.Name;
        }
    }
}
