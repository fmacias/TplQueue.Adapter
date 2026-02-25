using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
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
    ///    Factor to be automatically populated from the descriptor.
    ///  - If the constructor is internal, use InternalsVisibleTo so this assembly
    ///    can instantiate it.
    /// ]]>
    /// </summary>
    public sealed class RetryPolicyGenericFactory: IRetryPolicyGenericFactory
    {
        private RetryPolicyGenericFactory(){}
        
        /// <summary>
        /// Factory method hiding the concrete type.
        /// </summary>
        public static RetryPolicyGenericFactory Create()
        {
            return new RetryPolicyGenericFactory();
        }
        
        /// <inheritdoc />
        public IRetryPolicy PolicyByName(string name, IReadOnlyDictionary<string, IRetryPolicyDescriptor> options)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Retry policy name cannot be null or empty.", nameof(name));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (!options.TryGetValue(name, out var retryPolicyDescriptor))
            {
                return NoRetryPolicy.Create();
            }
            return PolicyByDescriptor(retryPolicyDescriptor);
        }

        public T PolicyByName<T>(string name, IReadOnlyDictionary<string, IRetryPolicyDescriptor> options)
            where T: class, IRetryPolicy
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Retry policy name cannot be null or empty.", nameof(name));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (!options.TryGetValue(name, out var retryPolicyDescriptor))
            {
                throw new KeyNotFoundException($"No retry policy descriptor was found for key '{name}'.");
            }

            var policy = PolicyByDescriptor(retryPolicyDescriptor);
            if (policy is T typedPolicy)
                return typedPolicy;

            throw new InvalidOperationException(
                $"Retry policy '{name}' resolved to '{policy.GetType().FullName}', which cannot be cast to '{typeof(T).FullName}'.");
        }


        /// <inheritdoc />
        public IRetryPolicy PolicyByDescriptor(IRetryPolicyDescriptor descriptor)
        {
            if (descriptor is null)
                throw new ArgumentNullException(nameof(descriptor));

            return CreateCustomFromDescriptor(descriptor);
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="policy"></param>
        /// <returns></returns>
        public bool TryGetPolicy<T>(out T policy) where T : class, IRetryPolicy
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
        /// <param name="descriptor"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static IRetryPolicy CreateCustomFromDescriptor(IRetryPolicyDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            IRetryPolicy policyInstance = PolicyByType(descriptor.RetryPolicyType);
            policyInstance.SetFromDescriptor(descriptor);
            return policyInstance;
        }

        private static IRetryPolicy PolicyByType(Type? policyType)
        {
            if (policyType == null) return NoRetryPolicy.Create();

            if (!typeof(IRetryPolicy).IsAssignableFrom(policyType))
            {
                throw new InvalidOperationException(
                    $"RetryPolicyType '{policyType.FullName}' must implement {nameof(IRetryPolicy)}.");
            }

            var constructorInfo = ConstructPolicy(policyType);
            var policyInstance = (IRetryPolicy)constructorInfo.Invoke(null);
            return policyInstance;
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
            ConstructorInfo ctor = ConstructPolicy(policyType);
            policy = (T)ctor.Invoke(null);
            return policy != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="policyType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static ConstructorInfo ConstructPolicy(Type policyType)
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
