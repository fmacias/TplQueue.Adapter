using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Queues
{
    /// <summary>
    /// Thin MIT adapter that wraps a proprietary <see cref="IQ"/> instance.
    /// Uses lazy initialization to avoid constructing the proprietary object until first use.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<Pending>")]
    public class ParallelQAdapter : IParallelJobQAdapter
    {
        private readonly Lazy<IParallelQ> _innerQ;

        /// <summary>
        /// Creates an adapter from a factory delegate. The factory is invoked once, thread-safely, on first use.
        /// </summary>
        public ParallelQAdapter(Func<IParallelQ> innerFactory)
        {
            if (innerFactory is null) throw new ArgumentNullException(nameof(innerFactory));
            _innerQ = new Lazy<IParallelQ>(() =>
            {
                var queue = innerFactory();
                return queue ?? throw new InvalidOperationException("The inner queue factory returned null.");
            }, isThreadSafe: true);
        }

        /// <summary>
        /// Creates an adapter over an already constructed proprietary queue.
        /// </summary>
        public ParallelQAdapter(IParallelQ inner)
        {
            _innerQ = new Lazy<IParallelQ>(() => inner ?? throw new ArgumentNullException(nameof(inner)), isThreadSafe: true);
        }

        private IParallelQ Q => _innerQ.Value;

        /// <inheritdoc />
        public virtual Func<IJobEvent, Task> OnJobEventChanged
        {
            get => Q.OnJobEventChanged;
            set => Q.OnJobEventChanged = value;
        }

        public bool IsDisposed => Q.IsDisposed;

        public string Name => Q.Name;

        public int MaxParallelism => Q.MaxParallelism;

        public Func<IRetryPolicy> RetryPolicyFactory => Q.RetryPolicyFactory;

        public SemaphoreSlim Semaphore => Q.Semaphore;

        // IDisposable
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
        public virtual void Dispose()
        {
            Q.Dispose();
        }

        public IParallelQ GetInnerQ() => _innerQ.Value;

        public IParallelQ Enqueue(IJobRoot jobRoot, bool isFifo, CancellationToken cancellationToken)
        {
            _ = Q.Enqueue(jobRoot, isFifo, cancellationToken);
            return this;
        }

        public IParallelQ EnqueueFifo(IJobRoot jobRoot, CancellationToken ct)
        {
            _ = Q.Enqueue(jobRoot, ct);
            return this;
        }

        public IParallelQ Enqueue(Action<CancellationToken> action, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.Enqueue(action, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ Enqueue(Func<CancellationToken, Task> func, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.Enqueue(func, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ Enqueue<T>(Action<CancellationToken, T> action, T arg, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.Enqueue(action, arg, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ Enqueue<T>(Func<CancellationToken, T, Task> func, T arg, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.Enqueue(func, arg, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ Enqueue<T1, T2>(Action<CancellationToken, T1, T2> action, T1 arg1, T2 arg2, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.Enqueue(action, arg1, arg2, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ Enqueue<T1, T2>(Func<CancellationToken, T1, T2, Task> func, T1 arg1, T2 arg2, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.Enqueue(func, arg1, arg2, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ EnqueueFifo(Action<CancellationToken> action, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.EnqueueFifo(action, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ EnqueueFifo(Func<CancellationToken, Task> func, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.EnqueueFifo(func, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ EnqueueFifo<T>(Action<CancellationToken, T> action, T arg, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.EnqueueFifo(action, arg, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ EnqueueFifo<T>(Func<CancellationToken, T, Task> func, T arg, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.EnqueueFifo(func, arg, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ EnqueueFifo<T1, T2>(Action<CancellationToken, T1, T2> action, T1 arg1, T2 arg2, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.EnqueueFifo(action, arg1, arg2, ct, name, retryPolicyFactory);
            return this;
        }

        public IParallelQ EnqueueFifo<T1, T2>(Func<CancellationToken, T1, T2, Task> func, T1 arg1, T2 arg2, CancellationToken ct, string name = "", Func<IRetryPolicy>? retryPolicyFactory = null)
        {
            _ = Q.EnqueueFifo(func, arg1, arg2, ct, name, retryPolicyFactory);
            return this;
        }

        public virtual void Start() => Q.Start();

        public virtual void Pause() => Q.Pause();

        public IQ Enqueue(IJobRoot jobRoot, CancellationToken ct)
        {
            _ = Q.Enqueue(jobRoot, ct);
            return this;
        }

        public async Task Wait(int stateAtMs = 0)
        {
            await Q.Wait(stateAtMs).ConfigureAwait(false);
        }

        public IQ SetRetryPolicyFactory(Func<IRetryPolicy> retryPolicy)
        {
            _ = Q.SetRetryPolicyFactory(retryPolicy);
            return this; 
        }

        public IDisposable Subscribe(IObserver<IJobEvent> observer)
        {
            return Q.Subscribe(observer);
        }
    }
}
