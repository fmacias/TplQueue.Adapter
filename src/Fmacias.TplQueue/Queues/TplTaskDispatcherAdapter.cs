using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Queues
{
    /// <summary>
    /// Thin MIT adapter that wraps a proprietary <see cref="ITaskDispatcher"/> instance.
    /// Uses lazy initialization to avoid constructing the proprietary object until first use.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<Pending>")]
    public class TplTaskDispatcherAdapter : ITaskDispatcher, ITaskDispatcherAdapter
    {
        private readonly Lazy<ITaskDispatcher> _inner;

        /// <summary>
        /// Creates an adapter from a factory delegate. The factory is invoked once, thread-safely, on first use.
        /// </summary>
        public TplTaskDispatcherAdapter(Func<ITaskDispatcher> innerFactory)
        {
            if (innerFactory is null) throw new ArgumentNullException(nameof(innerFactory));
            _inner = new Lazy<ITaskDispatcher>(() =>
            {
                var queue = innerFactory();
                return queue ?? throw new InvalidOperationException("The inner dispatcher factory returned null.");
            }, isThreadSafe: true);
        }

        /// <summary>
        /// Creates an adapter over an already constructed proprietary queue.
        /// </summary>
        public TplTaskDispatcherAdapter(ITaskDispatcher inner)
        {
            _inner = new Lazy<ITaskDispatcher>(() => inner ?? throw new ArgumentNullException(nameof(inner)), isThreadSafe: true);
        }

        private ITaskDispatcher Q => _inner.Value;

        // IObservable
        public IDisposable Subscribe(IObserver<ITaskRunnerEvent> observer) => Q.Subscribe(observer);

        public ITaskDispatcher Enqueue(ITaskRunnerRoot taskRunnerRoot, CancellationToken ct)
            => Q.Enqueue(taskRunnerRoot, ct);

        public ITaskDispatcher EnqueueFifo(ITaskRunnerRoot taskRunnerRoot, CancellationToken ct)
            => Q.EnqueueFifo(taskRunnerRoot, ct);

        // Lifecycle
        public virtual void StartPolling() => Q.StartPolling();
        public virtual void StopPolling() => Q.StopPolling();
        public bool IsDisposed => Q.IsDisposed;

        /// <inheritdoc />
        public virtual Func<ITaskRunnerEvent, Task> InternalEventDelegator
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

        public ITaskDispatcher GetInnerQueue()
        {
            return Q;
        }

        public ITaskDispatcher AddToQueue(ITaskRunnerRoot taskRunnerRoot, bool isFifo, CancellationToken cancellationToken)
        {
            return Q.AddToQueue(taskRunnerRoot, isFifo, cancellationToken);
        }

        public async Task WaitRunnerUntilFinishedAsync(Guid taskRunnerId)
        {
            await Q.WaitRunnerUntilFinishedAsync(taskRunnerId).ConfigureAwait(false);
        }
    }
}
