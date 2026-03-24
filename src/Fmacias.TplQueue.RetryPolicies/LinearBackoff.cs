using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.RetryPolicies
{
    /// <summary>
    /// <![CDATA[
    /// Simple linear backoff retry policy.
    ///
    /// Semantics:
    ///   - Retries up to _maxRetries times after the initial attempt.
    ///   - Uses a constant delay between retries.
    /// ]]>
    /// </summary>
    internal class LinearBackoff : ILinearBackoff
    {
        private const int DefaultMaxRetries = 3;
        private const int DefaultBaseDelayMilliseconds = 100;
        private int _maxRetries;
        private TimeSpan _delay;

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

        public LinearBackoff()
        {
            MaxRetries = DefaultMaxRetries;
            Delay = TimeSpan.FromMilliseconds(DefaultBaseDelayMilliseconds);
        }
        public LinearBackoff(int maxRetries, int delayMs)
        {
            MaxRetries = maxRetries;
            Delay = TimeSpan.FromMilliseconds(delayMs);
        }

        /// <inheritdoc />
        public async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action,
            CancellationToken cancellationToken)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            RetryCount = 0;

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

                    await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public IRetryPolicyOptions ToDescriptor()
        {
            return RetryPolicyOptions.Create(MaxRetries, (int)Math.Round(Delay.TotalMilliseconds));
        }

        public IRetryPolicy SetFromDescriptor(IRetryPolicyOptions descriptor)
        {
            if (descriptor is null)
                throw new ArgumentNullException(nameof(descriptor));

            MaxRetries = descriptor.MaxRetries;
            Delay = TimeSpan.FromMilliseconds(descriptor.BaseDelayMs);
            return this;
        }
    }
}
