using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
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
    /// the IExponentialBackoff abstraction.
    /// ]]>
    /// </summary>
    internal sealed class ExponentialBackoff : IExponentialBackoff
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
        private readonly Func<JitterUtil> _jitterUtilFactory;

        private readonly TimeSpan _maxDelay = TimeSpan.FromSeconds(30);

        /// <inheritdoc />
        public int RetryCount { get; private set; }

        /// <inheritdoc />
        public int MaxRetries
        {
            get => _maxRetries;
            private set
            {
                if (value <= 0)
                    _maxRetries = DefaultMaxRetries;
                else
                    _maxRetries = value;
            }
        }

        /// <inheritdoc />
        public TimeSpan Delay
        {
            get => _delay;
            private set
            {
                var ms = value.TotalMilliseconds;

                if (ms <= 0)
                    _delay = TimeSpan.FromMilliseconds(DefaultBaseDelayMilliseconds);
                else
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
                    _factor = DefaultFactor;
                else
                    _factor = value;
            }
        }

        public ExponentialBackoff()
            : this(DefaultMaxRetries, DefaultBaseDelayMilliseconds, DefaultFactor)
        {
        }

        public ExponentialBackoff(int maxRetries, int delayMs, double factor)
            : this(maxRetries, delayMs, factor, () => JitterUtil.Create())
        {
        }

        internal ExponentialBackoff(int maxRetries, int delayMs, double factor, Func<JitterUtil> jitterUtilFactory)
        {
            _jitterUtilFactory = jitterUtilFactory ?? throw new ArgumentNullException(nameof(jitterUtilFactory));

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

            using var jitterUtil = _jitterUtilFactory();

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
                    var jitteredMs = jitterUtil.JitterMs(nextMs, DefaultJitterPercent);
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
        public IRetryPolicyDescriptor ToDescriptor(Type retryPolicyType)
        {
            if (retryPolicyType == null) throw new ArgumentNullException(nameof(retryPolicyType));

            return RetryPolicyOptions
                .Create(MaxRetries, (int)Math.Round(Delay.TotalMilliseconds), Factor)
                .SetRetryPolicyType(retryPolicyType);
        }

        public IRetryPolicy SetFromDescriptor(IRetryPolicyDescriptor descriptor)
        {
            if (descriptor is null)
                throw new ArgumentNullException(nameof(descriptor));

            MaxRetries = descriptor.MaxRetries;
            Factor = descriptor.Factor;
            Delay = TimeSpan.FromMilliseconds(descriptor.BaseDelayMs);
            return this;
        }
    }
}