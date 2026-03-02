using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Log;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
namespace Fmacias.TplQueue.Queues;
/// <summary>
/// <![CDATA[
/// Dispatcher that persists payload graphs into an IPayloadLeaseCache and
/// rehydrates/leases them into an inner IJobsChain when execution
/// capacity is available.
///
/// The execution semantics are:
/// - Enqueue/EnqueueFifo: the payload root is appended to the cache in Pending state;
/// - StartPolling: a lightweight leasing loop is enabled;
/// - StopPolling: leasing is paused, but the loop remains alive (restartable);
/// - Leasing loop: while leasing is enabled and the inner dispatcher has free
///   semaphore slots, TryLeaseWorkOnce() is invoked to lease a single root
///   and enqueue it into the inner dispatcher;
/// - Job events (Completed/Failed/Canceled) update the lease cache
///   via the InternalEventDelegator callback.
///
/// This dispatcher does not execute work directly. It only bridges between
/// the durable lease cache and the in-memory task dispatcher.
/// ]]>
///</summary>
internal sealed class CacheQ : ParallelQAdapter, ICacheQ
{
    /// <summary>
    /// <![CDATA[
    /// Delay, in milliseconds, between leasing iterations in the background loop.
    /// This value controls how frequently the dispatcher attempts to lease work
    /// from the cache, while avoiding a busy-wait CPU loop.
    /// ]]>
    /// </summary>
    private const int LEASING_PULSE_MS = 100;

    private readonly ILogger<ICacheQ> _logger;
    private readonly IDataJobCache _leaseCache;
    private readonly Dictionary<Guid, CancellationToken> _cancelationTokentByRootId = new();
    private readonly CancellationTokenSource _leasingCts = new();
    private int _leasingPulseMs = LEASING_PULSE_MS;
    private int _leasingTicking;
    private Task? _leasingTask;

    /// <summary>
    /// <![CDATA[
    /// Flag indicating whether the leasing loop is currently allowed
    /// to lease work from the cache. This is toggled by StartPolling
    /// and StopPolling and is independent from disposal.
    /// ]]>
    /// </summary>
    private volatile bool _isLeasingEnabled;

    /// <summary>
    /// Gets or sets the leasing pulse interval in milliseconds.
    /// Non-positive values revert to the internal default.
    /// </summary>
    public int LeasingPulseMs
    {
        get => _leasingPulseMs;
        set
        {
            // Fail-fast to default on non-positive values.
            // Do NOT assign the invalid value.
            _leasingPulseMs = value > 0
                ? value
                : LEASING_PULSE_MS;
        }
    }

    /// <summary>
    /// <![CDATA[
    /// Initializes a new instance of the SerializableDispatcher class.
    ///
    /// The dispatcher wraps an inner IJobsChain implementation and uses an
    /// IPayloadLeaseCache to persist and later rehydrate payload graphs.
    ///
    /// InternalEventDelegator is wired to a local callback that keeps the lease
    /// cache in sync with runner completion/failure/cancellation.
    /// ]]>
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic messages.</param>
    /// <param name="leaseCache">Lease cache that stores payload graphs and their leasing state.</param>
    /// <param name="queue">Inner task dispatcher that executes the rehydrated runners.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="logger"/> or <paramref name="leaseCache"/> is <c>null</c>.
    /// </exception>
    private CacheQ(
        ILogger<ICacheQ> logger,
        IDataJobCache leaseCache,
        IParallelQ queue)
        : base(queue)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _leaseCache = leaseCache ?? throw new ArgumentNullException(nameof(leaseCache));

        OnJobEventChanged = JobEventCallback;
        _isLeasingEnabled = false;
    }

    /// <summary>
    /// <![CDATA[
    /// Creates a new CacheableChain instance for the given logger,
    /// lease cache and inner chain.
    /// ]]>
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic messages.</param>
    /// <param name="cache">Lease cache used to persist and lease payload graphs.</param>
    /// <param name="queue">Inner task dispatcher that will execute leased graphs.</param>
    /// <returns>A configured ISerializablePayloadDispatcher instance.</returns>
    public static CacheQ Create(
        ILogger<ICacheQ> logger,
        IDataJobCache cache,
        IParallelQ queue)
    {
        return new CacheQ(logger, cache, queue);
    }

    /// <summary>
    /// <![CDATA[
    /// Persists the specified payload root into the lease cache in Pending state.
    ///
    /// The graph is not immediately executed. It will be leased later by the
    /// background leasing loop (started via StartPolling) when the inner dispatcher
    /// has free capacity.
    /// ]]>
    /// </summary>
    /// <typeparam name="TPayload">Type of the payload command.</typeparam>
    /// <param name="payloadJobRoot">Root payload runner that represents the graph to be persisted.</param>
    /// <param name="ct">
    /// Cancellation token associated with this job. If cancellation is requested
    /// before leasing/execution, the lease cache may mark the entry as canceled.
    /// </param>
    /// <returns>The current dispatcher instance, to allow fluent calls.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="payloadJobRoot"/> is <c>null</c>.
    /// </exception>
    public ICacheQ Enqueue<TPayload>(
        IDataJobRoot<TPayload> payloadJobRoot,
        CancellationToken ct)
        where TPayload : IPayload
    {
        if (payloadJobRoot is null) throw new ArgumentNullException(nameof(payloadJobRoot));
        CacheNode(payloadJobRoot, isFifo:false, ct);
        return this;
    }

    /// <summary>
    /// <![CDATA[
    /// Persists the specified payload root into the lease cache in Pending state
    /// with FIFO semantics for the root graph.
    ///
    /// The FIFO flag is later used when leasing the root back into the inner
    /// dispatcher so that the corresponding job is enqueued using FIFO
    /// semantics at the task dispatcher level.
    /// ]]>
    /// </summary>
    /// <typeparam name="TPayload">Type of the payload command.</typeparam>
    /// <param name="payloadJobRoot">Root payload runner to be persisted.</param>
    /// <param name="ct">
    /// Cancellation token associated with this job. If cancellation is requested
    /// before leasing/execution, the lease cache may mark the entry as canceled.
    /// </param>
    /// <returns>The current dispatcher instance, to allow fluent calls.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="payloadJobRoot"/> is <c>null</c>.
    /// </exception>
    public ICacheQ EnqueueFifo<TPayload>(
        IDataJobRoot<TPayload> payloadJobRoot,
        CancellationToken ct)
        where TPayload : IPayload
    {
        if (payloadJobRoot is null) throw new ArgumentNullException(nameof(payloadJobRoot));
        CacheNode(payloadJobRoot, isFifo:true, ct);
        return this;
    }

    /// <summary>
    /// <![CDATA[
    /// Starts polling on the inner dispatcher and enables the background leasing loop.
    ///
    /// If a leasing loop task has not yet been started, it is created here.
    /// If a task already exists and is still running, leasing is simply re-enabled.
    /// ]]>
    /// </summary>
    public override void Start()
    {
        base.Start();

        _isLeasingEnabled = true;

        if (_leasingTask == null || _leasingTask.IsCompleted)
        {
            _leasingTask = Task.Run(() => LeaseLoopAsync(_leasingCts.Token));
        }
    }

    /// <summary>
    /// <![CDATA[
    /// Stops polling on the inner dispatcher and disables leasing,
    /// without cancelling the background leasing task.
    ///
    /// Leasing can later be re-enabled by calling StartPolling() again,
    /// as long as this dispatcher has not been disposed.
    /// ]]>
    /// </summary>
    public override void Pause()
    {
        _isLeasingEnabled = false;
        base.Pause();
    }

    /// <summary>
    /// <![CDATA[
    /// Background leasing loop that periodically tries to lease a single root
    /// from the lease cache when leasing is enabled and the inner dispatcher
    /// has free semaphore slots.
    ///
    /// The loop terminates when the cancellation token is signaled or when
    /// the dispatcher is disposed. When leasing is disabled via StopPolling,
    /// the loop remains alive but idle, so that StartPolling can re-enable it.
    /// ]]>
    /// </summary>
    /// <param name="ct">Cancellation token that controls the loop lifetime.</param>
    private async Task LeaseLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && !IsDisposed)
            {
                if (!_isLeasingEnabled)
                {
                    await Task.Delay(LeasingPulseMs, ct).ConfigureAwait(false);
                    continue;
                }

                if (Interlocked.Exchange(ref _leasingTicking, 1) == 0)
                {
                    try
                    {
                        var inner = GetInnerQ();

                        if (inner.Semaphore.CurrentCount > 0)
                        {
                            TryLeaseWorkOnce();
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        LogMessages.BackgroundError(_logger, ex);
                    }
                    finally
                    {
                        Volatile.Write(ref _leasingTicking, 0);
                    }
                }

                await Task.Delay(LeasingPulseMs, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// <![CDATA[
    /// Attempts to lease a single payload root from the lease cache and enqueue
    /// it into the inner dispatcher if there is available capacity.
    ///
    /// This method is intentionally cheap and side-effect free when no slots
    /// are available or when the cache does not contain any pending roots.
    /// ]]>
    /// </summary>
    /// <returns>
    /// <c>true</c> if a root was successfully leased and added to the inner queue;
    /// <c>false</c> if there was no capacity or no root to lease.
    /// </returns>
    internal bool TryLeaseWorkOnce()
    {
        var semaphore = GetInnerQ().Semaphore;

        if (semaphore.CurrentCount <= 0)
        {
            return false;
        }

        if (_leaseCache.TryHydrateNextJob(out var payloadCarrierRoot, out var rootLeaseNode))
        {
            _leaseCache.LeaseRootNode(rootLeaseNode);

            LogMessages.PayloadJobRootDeserialized(
                _logger,
                payloadCarrierRoot.Id,
                payloadCarrierRoot.Name ?? string.Empty,
                null);

            CancellationToken currentCt = RelatedCancelationToken(payloadCarrierRoot.Id);
            Enqueue(payloadCarrierRoot, isFifo: rootLeaseNode.IsFifo, currentCt);
            return true;
        }
        return false;
    }

    private CancellationToken RelatedCancelationToken(Guid rootId)
    {
        var currentCt = CancellationToken.None;

        if (_cancelationTokentByRootId.TryGetValue(rootId, out var relatedCt))
        {
            currentCt = relatedCt;
        }
        return currentCt;
    }

    /// <summary>
    /// <![CDATA[
    /// Internal callback used as InternalEventDelegator for the inner dispatcher.
    ///
    /// It keeps the lease cache in sync with the execution state of each node:
    /// - Completed => AckNode + possible AckRoot if the full graph is terminal;
    /// - Failed    => FailNode;
    /// - Canceled  => CancelNode.
    ///
    /// All exceptions are swallowed and logged so that observer failures never
    /// compromise the dispatcher.
    /// ]]>
    /// </summary>
    /// <param name="e">Task runner event produced by the inner dispatcher.</param>
    /// <returns>A completed task; no asynchronous work is performed.</returns>
    [SuppressMessage("Design", "CA1031", Justification = "Observers must not break the dispatcher.")]
    private Task JobEventCallback(IJobEvent e)
    {
        if (e is null || e.JobInfo is null)
        {
            return Task.CompletedTask;
        }

        if (e.JobInfo is not ISerializable serializedPayload)
        {
            LogMessages.PayloadJobNotSerializableError(_logger,null);
            return Task.CompletedTask;
        }

        var jobInfoDto = e.JobInfo;
        var jobId = jobInfoDto.Id;

        try
        {
            switch (e.Status)
            {
                case JobEventStatus.Successed:
                    _leaseCache.AckNode(jobId, serializedPayload);
                    break;

                case JobEventStatus.Failed:
                    _leaseCache.FailNode(jobId, e.Exception?.Message);
                    break;

                case JobEventStatus.Canceled:
                    _leaseCache.CancelNode(jobId);
                    break;
                case JobEventStatus.RootSuccessed:
                    _leaseCache.SuccessRootNode(jobId);
                    break;
            }

            if (_leaseCache.DeleteRootNode(jobId))
            {
                LogMessages.CacheRootTerminalAck(
                    _logger,
                    jobId,
                    null);
            }
        }
        catch (Exception ex)
        {
            LogMessages.ObserverError(_logger, ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// <![CDATA[
    /// Releases resources used by the SerializableDispatcher.
    ///
    /// This method:
    /// - Cancels the background leasing loop;
    /// - Waits briefly for the loop to terminate;
    /// - Disposes the leasing CancellationTokenSource;
    /// - Delegates to the base TplTaskDispatcherAdapter.Dispose() implementation,
    ///   which finalizes the inner dispatcher.
    /// ]]>
    /// </summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Dispose must not throw; background errors are already logged in the loop.")]

    public override void Dispose()
    {
        if (!_leasingCts.IsCancellationRequested)
        {
            _leasingCts.Cancel();
        }

        try
        {
            _leasingTask?.Wait(200);
        }
        catch
        {
        }

        _leasingCts.Dispose();
        base.Dispose();
    }
    private void CacheNode<TPayload>(IDataJobRoot<TPayload> payloadRunnerRoot, bool isFifo, CancellationToken ct) where TPayload : IPayload
    {
        IReadOnlyList<IJobNodeDto> jobNodes = _leaseCache.Dehydrate(payloadRunnerRoot, isFifo);
        _cancelationTokentByRootId.Add(payloadRunnerRoot.Id, ct);
    }
}
