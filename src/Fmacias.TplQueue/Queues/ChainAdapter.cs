using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Queues
{
    /// <summary>
    /// Thin MIT adapter that wraps a proprietary <see cref="IJobsChain"/> instance.
    /// Uses lazy initialization to avoid constructing the proprietary object until first use.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<Pending>")]
    public class ChainAdapter : IJobsChain, IJobsChainAdapter
    {
        private readonly Lazy<IJobsChain> _innerChain;

        /// <summary>
        /// Creates an adapter from a factory delegate. The factory is invoked once, thread-safely, on first use.
        /// </summary>
        public ChainAdapter(Func<IJobsChain> innerFactory)
        {
            if (innerFactory is null) throw new ArgumentNullException(nameof(innerFactory));
            _innerChain = new Lazy<IJobsChain>(() =>
            {
                var queue = innerFactory();
                return queue ?? throw new InvalidOperationException("The inner dispatcher factory returned null.");
            }, isThreadSafe: true);
        }

        /// <summary>
        /// Creates an adapter over an already constructed proprietary queue.
        /// </summary>
        public ChainAdapter(IJobsChain inner)
        {
            _innerChain = new Lazy<IJobsChain>(() => inner ?? throw new ArgumentNullException(nameof(inner)), isThreadSafe: true);
        }

        private IJobsChain Q => _innerChain.Value;

        // IObservable
        public IDisposable Subscribe(IObserver<IJobEvent> observer) => Q.Subscribe(observer);

        public IJobsChain Enqueue(IJobRoot jobRoot, CancellationToken ct)
            => Q.Enqueue(jobRoot, ct);

        public IJobsChain EnqueueFifo(IJobRoot jobRoot, CancellationToken ct)
            => Q.EnqueueFifo(jobRoot, ct);

        // Lifecycle
        public virtual void Start() => Q.Start();
        public virtual void Pause() => Q.Pause();
        public bool IsDisposed => Q.IsDisposed;

        /// <inheritdoc />
        public virtual Func<IJobEvent, Task> InternalEventDelegator
        {
            get => Q.InternalEventDelegator;
            set => Q.InternalEventDelegator = value;
        }

        public string Name => Q.Name;

        public int MaxParallelism => Q.MaxParallelism;

        public Func<IRetryPolicy> RetryPolicyFactory => Q.RetryPolicyFactory;

        public int PulseMs => Q.PulseMs;

        public SemaphoreSlim Semaphore => Q.Semaphore;

        // IDisposable
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
        public virtual void Dispose()
        {
            Q.Dispose();
        }

        public IJobsChain GetInnerChain()
        {
            return Q;
        }

        public IJobsChain AddToQueue(IJobRoot jobRoot, bool isFifo, CancellationToken cancellationToken)
        {
            return Q.AddToQueue(jobRoot, isFifo, cancellationToken);
        }

        public async Task WaitRunnerUntilFinishedAsync(Guid jobId)
        {
            await Q.WaitRunnerUntilFinishedAsync(jobId).ConfigureAwait(false);
        }
    }
}
