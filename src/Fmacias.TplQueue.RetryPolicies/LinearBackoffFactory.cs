using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.RetryPolicies
{
    public class LinearBackoffFactory : FactoryAbstract<ILinearBackoff>, ILinearBackoffFactory
    {
        private LinearBackoffFactory()
        {
        }
        public static IRetryPolicyFactory<ILinearBackoff> Create()
        {
            return new LinearBackoffFactory();
        }
        public override ILinearBackoff CreatePolicy(IRetryPolicyOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            return (ILinearBackoff)new LinearBackoff().SetFromDescriptor(options);
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
