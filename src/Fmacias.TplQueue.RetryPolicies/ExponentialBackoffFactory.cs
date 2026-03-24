using Fmacias.TplQueue.Contracts;
using System;

namespace Fmacias.TplQueue.RetryPolicies
{
    public class ExponentialBackoffFactory : FactoryAbstract<IExponentialBackoff>, IExponentialBackofFactory
    {
        private ExponentialBackoffFactory()
        {
        }
        public static IRetryPolicyFactory<IExponentialBackoff> Create()
        {
            return new ExponentialBackoffFactory();
        }
        public override IExponentialBackoff CreatePolicy(IRetryPolicyOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            return (IExponentialBackoff) new ExponentialBackoff().SetFromDescriptor(options);
        }

        public IExponentialBackoff CreateExponentialBackoff(int maxRetries, int delayMs, double factor)
        {
            return new ExponentialBackoff(maxRetries, delayMs, factor);
        }
        public override IExponentialBackoff CreatePolicy()
        {
            return GetDefault();
        }

        protected override IExponentialBackoff GetDefault()
        {
            return new ExponentialBackoff();
        }
    }
}
