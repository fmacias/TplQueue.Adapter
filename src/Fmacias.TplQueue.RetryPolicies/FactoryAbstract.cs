using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.Generic;

namespace Fmacias.TplQueue.RetryPolicies
{
    public abstract class FactoryAbstract<TPolicy>: IRetryPolicyFactory<TPolicy>
        where TPolicy : IRetryPolicy
    {
        public abstract TPolicy CreatePolicy();

        public TPolicy CreatePolicy(string name, IReadOnlyDictionary<string, IRetryPolicyOptions> retrypoliciesByName)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Retry policy name cannot be null or empty.", nameof(name));

            if (retrypoliciesByName == null) 
                throw new ArgumentNullException(nameof(retrypoliciesByName));

            if (retrypoliciesByName.TryGetValue(name, out var option))
            {
                if (option == null)
                    throw new ArgumentException($"Retry policy descriptor for '{name}' cannot be null.", nameof(retrypoliciesByName));

                return CreatePolicy(option);
            }
            return GetDefault();
        }
        /// <inheritdoc />
        public abstract TPolicy CreatePolicy(IRetryPolicyOptions options);
        protected abstract TPolicy GetDefault();
    }
}
