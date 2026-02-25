using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.RetryPolicies
{
    public class LinearBackoffFactory : RetryPolicyFactoryAbstract<ILinearBackoff>, ILinearBackoffFactory
    {
        private LinearBackoffFactory()
        {
        }
        public static IRetryPolicyFactory<ILinearBackoff> Create()
        {
            return new LinearBackoffFactory();
        }
        protected override ILinearBackoff CreatePolicy(IRetryPolicyDescriptor descriptor)
        {
            if (descriptor is null) throw new ArgumentNullException(nameof(descriptor));
            return (ILinearBackoff)new LinearBackoff().SetFromDescriptor(descriptor);
        }
        public ILinearBackoff CreateLienarBackoff(int maxRetries, int delayMs)
        {
            return new LinearBackoff(maxRetries, delayMs);
        }

        public override ILinearBackoff CreatePolicy()
        {
            return GetDefault();
        }

        protected override ILinearBackoff GetDefault()
        {
            return new LinearBackoff();
        }
    }
}
