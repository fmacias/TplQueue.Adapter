using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.Generic;

namespace Fmacias.TplQueue.RetryPolicies
{
    public abstract class RetryPolicyFactoryAbstract<TPolicy>: IRetryPolicyFactory<TPolicy>
        where TPolicy : IRetryPolicy
    {
        public abstract TPolicy CreatePolicy();

        public TPolicy CreatePolicy(string name, IReadOnlyDictionary<string, IRetryPolicyDescriptor> options)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Retry policy name cannot be null or empty.", nameof(name));

            if (options == null) 
                throw new ArgumentNullException(nameof(options));

            if (options.TryGetValue(name, out var option))
            {
                if (option == null)
                    throw new ArgumentException($"Retry policy descriptor for '{name}' cannot be null.", nameof(options));

                return CreatePolicy(option);
            }
            return GetDefault();
        }
        /// <inheritdoc />
        protected abstract TPolicy CreatePolicy(IRetryPolicyDescriptor descriptor);
        protected abstract TPolicy GetDefault();
    }
}
