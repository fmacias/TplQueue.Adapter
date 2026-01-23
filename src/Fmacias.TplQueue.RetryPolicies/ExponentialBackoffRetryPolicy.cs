using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.RetryPolicies
{
    /// <summary>
    /// <![CDATA[
    /// Exponential backoff retry policy with optional jitter.
    ///
    /// Semantics:
    ///   - Retries up to _maxRetries times after the initial attempt.
    ///   - Delay starts at _initialDelay and is multiplied by _factor on each retry,
    ///     capped to _maxDelay.
    ///   - A small jitter (±10%) is applied to avoid thundering herds.
    ///
    /// This type is internal to the RetryPolicies assembly but exposed via
    /// the IExponentialFactorRetryPolicy abstraction.
    /// ]]>
    /// </summary>
    internal sealed class ExponentialBackoffRetryPolicy : IExponentialBackoffRetryPolicy
    {
        private const double DefaultJitterPercent = 0.10;
        /// <summary>
        /// Default exponential factor (2.0).
        /// </summary>
        public const double DefaultFactor = 2.0;
        private const int DefaultMaxRetries = 3;
        private const int DefaultBaseDelayMilliseconds = 100;
        private int _maxRetries;
        private TimeSpan _delay;
        private double _factor;
        /// <inheritdoc />
        public int RetryCount { get; private set; }
        /// <inheritdoc />
        public int MaxRetries
        {
            get => _maxRetries;
            private set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(MaxRetries));

                _maxRetries = value;
            }
        }
        /// <inheritdoc />
        public TimeSpan Delay {
            get => _delay;
            private set
            {
                var ms = value.TotalMilliseconds;

                if (ms <= 0)
                    throw new ArgumentOutOfRangeException(nameof(Delay));
                
                _delay = value;
            } 
        }
        /// <inheritdoc />
        public double Factor
        {
            get => _factor;
            private set
            {
                if (value <= 0d)
                    throw new ArgumentOutOfRangeException(nameof(Factor));

                _factor = value;
            }
        }

        private readonly TimeSpan _maxDelay = TimeSpan.FromSeconds(30);

        public ExponentialBackoffRetryPolicy()
        {
            MaxRetries = DefaultMaxRetries;
            Delay = TimeSpan.FromMilliseconds(DefaultBaseDelayMilliseconds);
            Factor = DefaultFactor;
        }
        public ExponentialBackoffRetryPolicy(int maxRetries, int delayMs, double factor)
        {
            MaxRetries = maxRetries;
            Delay = TimeSpan.FromMilliseconds(delayMs);
            Factor = factor;
        }

        /// <inheritdoc />
        public async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action,
            CancellationToken cancellationToken)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            RetryCount = 0;
            var delay = Delay;

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
                    if (RetryCount > MaxRetries)
                        throw;
                    
                    var nextMs = (int)Math.Min(delay.TotalMilliseconds * Factor, _maxDelay.TotalMilliseconds);
                    var jitteredMs = (double)JitterUtil.JitterMs(nextMs, DefaultJitterPercent);
                    var finalDelay = ComputeNextDelay(jitteredMs);

                    await Task.Delay(finalDelay, cancellationToken).ConfigureAwait(false);

                    delay = TimeSpan.FromMilliseconds(
                        Math.Min(delay.TotalMilliseconds * Factor, _maxDelay.TotalMilliseconds));
                }
            }
        }

        /// <summary>
        /// Ensures the resulting delay is non-negative and returns it as a <see cref="TimeSpan"/>.
        /// </summary>
        private static TimeSpan ComputeNextDelay(double nextMs)
        {
            return TimeSpan.FromMilliseconds(Math.Max(0.0, nextMs));
        }

        /// <inheritdoc />
        public IRetryPolicyDescriptor ToDescriptor()
        {
            var delayMs = (int)Math.Round(Delay.TotalMilliseconds);
            return RetryPolicyDescriptor.Exponential(MaxRetries, delayMs, Factor);
        }

        public IRetryPolicy SetFromDescriptor(IRetryPolicyDescriptor descriptor)
        {
            if (descriptor is null)
                throw new ArgumentNullException(nameof(descriptor));

            MaxRetries = descriptor.MaxRetries ?? 0;
            Factor = descriptor.Factor ?? 0d;
            var baseDelayMs = descriptor.BaseDelayMs ?? 0;
            Delay = TimeSpan.FromMilliseconds(baseDelayMs);
            return this;
        }

        public IRetryPolicy SetFromOptions(RetryPolicyOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            MaxRetries = options.MaxRetries;
            Factor = options.Factor ?? 0d;
            Delay = TimeSpan.FromMilliseconds(options.BaseDelayMs);
            return this;
        }
    }
}
