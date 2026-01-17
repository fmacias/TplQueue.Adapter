using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmaciasruano.TplQueue.RetryPolicies
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
    internal class LinearBackoffRetryPolicy : ILinearBackoffRetryPolicy
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
                    throw new ArgumentOutOfRangeException(nameof(MaxRetries));

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
                    throw new ArgumentOutOfRangeException(nameof(Delay));

                _delay = value;
            }
        }

        public LinearBackoffRetryPolicy()
        {
            MaxRetries = DefaultMaxRetries;
            Delay = TimeSpan.FromMilliseconds(DefaultBaseDelayMilliseconds);
        }
        public LinearBackoffRetryPolicy(int maxRetries, int delayMs)
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
        public IRetryPolicyDescriptor ToDescriptor()
        {
            var delayMs = (int)Math.Round(Delay.TotalMilliseconds);
            return RetryPolicyDescriptor.Linear(MaxRetries, delayMs);
        }

        public IRetryPolicy SetFromDescriptor(IRetryPolicyDescriptor descriptor)
        {
            if (descriptor is null)
                throw new ArgumentNullException(nameof(descriptor));

            MaxRetries = descriptor.MaxRetries ?? 0;
            var baseDelayMs = descriptor.BaseDelayMs ?? 0;
            Delay = TimeSpan.FromMilliseconds(baseDelayMs);
            return this;
        }

        public IRetryPolicy SetFromOptions(RetryPolicyOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            MaxRetries = options.MaxRetries;
            Delay = TimeSpan.FromMilliseconds(options.BaseDelayMs);
            return this;
        }
    }
}
