using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fmacias.TplQueue.RetryPolicies
{
    /// <summary>
    /// Factory responsible for resolving the built-in retry policies from the
    /// values contained in <see cref="IRetryPolicyOptions"/>.
    /// </summary>
    public sealed class RetryPolicyAbstractFactory: IRetryPolicyAbstractFactory
    {
        private RetryPolicyAbstractFactory(){}
        
        /// <summary>
        /// Factory method hiding the concrete type.
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

        public T PolicyByName<T>(string name, IReadOnlyDictionary<string, IRetryPolicyOptions> options)
            where T: class, IRetryPolicy
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

            return CreateCustomFromOptions(options);
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="policy"></param>
        /// <returns></returns>
        private static bool TryGetPolicy<T>(out T policy) where T : class, IRetryPolicy
        {
            if (TryConstructRetryPolicy<T>(out policy))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static IRetryPolicy CreateCustomFromOptions(IRetryPolicyOptions options)
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

        private static bool CanCreatePolicy(IRetryPolicyOptions options)
        {
            return options.MaxRetries > 0
                && options.BaseDelayMs >= 0
                && !double.IsNaN(options.Factor)
                && !double.IsInfinity(options.Factor)
                && options.Factor >= 0d;
        }

        private static IRetryPolicy PolicyByType(Type policyType)
        {
            if (policyType == null)
                throw new ArgumentNullException(nameof(policyType));

            if (policyType == typeof(NoRetryPolicy) || policyType == typeof(INoRetryPolicy))
            {
                return NoRetryPolicy.Create();
            }

            if (policyType == typeof(LinearBackoff) || policyType == typeof(ILinearBackoff))
            {
                return new LinearBackoff();
            }

            if (policyType == typeof(ExponentialBackoff) || policyType == typeof(IExponentialBackoff))
            {
                return new ExponentialBackoff();
            }

            if (!typeof(IRetryPolicy).IsAssignableFrom(policyType))
            {
                throw new InvalidOperationException(
                    $"RetryPolicyType '{policyType.FullName}' must implement {nameof(IRetryPolicy)}.");
            }
            var constructorInfo = GetConstructor(policyType);
            var policyInstance = (IRetryPolicy)constructorInfo.Invoke(null);
            return policyInstance;
        }

        private static T CreateTypedPolicy<T>(IRetryPolicyOptions options) where T : class, IRetryPolicy
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var policy = PolicyByType(typeof(T));
            policy.SetFromDescriptor(options);

            if (policy is T typedPolicy)
            {
                return typedPolicy;
            }

            throw new InvalidOperationException(
                $"Retry policy '{typeof(T).FullName}' could not be instantiated.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="policy"></param>
        /// <returns></returns>
        private static bool TryConstructRetryPolicy<T>(out T policy) where T : IRetryPolicy
        {
            var policyType = typeof(T);
            ConstructorInfo ctor = GetConstructor(policyType);
            policy = (T)ctor.Invoke(null);
            return policy != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="policyType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static ConstructorInfo GetConstructor(Type policyType)
        {
            if (policyType == null)
                throw new ArgumentNullException(nameof(policyType));

            if (policyType.IsAbstract || policyType.IsInterface)
            {
                throw new InvalidOperationException(
                    $"RetryPolicyType '{policyType.FullName}' must be a concrete type.");
            }

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

        public T GetPolicy<T>() where T : class, IRetryPolicy
        {
            if (TryGetPolicy<T>(out var policy))
                return policy;

            throw new InvalidOperationException($"Policy Type '{typeof(T).FullName}' could not be instantiated");
        }
    }
}
