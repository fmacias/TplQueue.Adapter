using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Queues
{
    /// <summary>
    /// Thin MIT adapter that wraps a proprietary <see cref="IJobQ"/> instance.
    /// Uses lazy initialization to avoid constructing the proprietary object until first use.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<Pending>")]
    public class QAdapter : IJobQ, IJobQAdapter
    {
        private readonly Lazy<IJobQ> _innerQ;

        /// <summary>
        /// Creates an adapter from a factory delegate. The factory is invoked once, thread-safely, on first use.
        /// </summary>
        public QAdapter(Func<IJobQ> innerFactory)
        {
            if (innerFactory is null) throw new ArgumentNullException(nameof(innerFactory));
            _innerQ = new Lazy<IJobQ>(() =>
            {
                var queue = innerFactory();
                return queue ?? throw new InvalidOperationException("The inner dispatcher factory returned null.");
            }, isThreadSafe: true);
        }

        /// <summary>
        /// Creates an adapter over an already constructed proprietary queue.
        /// </summary>
        public QAdapter(IJobQ inner)
        {
            _innerQ = new Lazy<IJobQ>(() => inner ?? throw new ArgumentNullException(nameof(inner)), isThreadSafe: true);
        }

        private IJobQ Q => _innerQ.Value;

        // IObservable
        public IDisposable Subscribe(IObserver<IJobEvent> observer) => Q.Subscribe(observer);

        public IJobQ Enqueue(IJobRoot jobRoot, CancellationToken ct)
        {
            _ = Q.Enqueue(jobRoot, ct);
            return this;
        }
        public IJobQ EnqueueFifo(IJobRoot jobRoot, CancellationToken ct)
        {
            _ = Q.EnqueueFifo(jobRoot, ct);
            return this;
        }

        // Lifecycle
        public virtual void Start() => Q.Start();
        public virtual void Pause() => Q.Pause();
        public bool IsDisposed => Q.IsDisposed;

        /// <inheritdoc />
        public virtual Func<IJobEvent, Task> OnJobEventChanged
        {
            get => Q.OnJobEventChanged;
            set => Q.OnJobEventChanged = value;
        }

        public string Name => Q.Name;

        public int MaxParallelism => Q.MaxParallelism;
        public SemaphoreSlim Semaphore => Q.Semaphore;
        public Func<IRetryPolicy> RetryPolicyFactory => Q.RetryPolicyFactory;

        // IDisposable
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
        public virtual void Dispose()
        {
            Q.Dispose();
        }

        public IJobQ GetInnerQ()
        {
            return Q;
        }

        public IJobQ Enqueue(IJobRoot jobRoot, bool isFifo, CancellationToken cancellationToken)
        {
            _ = Q.Enqueue(jobRoot, isFifo, cancellationToken);
            return this;
        }

        public async Task Wait(int stateAtMs = 0)
        {
            await Q.Wait(stateAtMs).ConfigureAwait(false);
        }

        public IJobQ SetRetryPolicyFactory(Func<IRetryPolicy> retryPolicy)
        {
            Q.SetRetryPolicyFactory(retryPolicy);
            return this;
        }
    }
}
