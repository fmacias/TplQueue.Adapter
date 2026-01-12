using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Log;
using Microsoft.Extensions.Logging;
using System;

namespace Fmaciasruano.TplQueue.Observers
{
    /// <summary>
    /// <inheritdoc cref="IProfilingObserver"/>
    /// Useful to log profiling information in case of memory usage problems.
    /// </summary>
    internal sealed class TaskRunnerProfilingObserver : IProfilingObserver
    {
        private readonly ILogger<IProfilingObserver> _logger;
        private long _lastMemoryUsage;
        private int[] _lastGcCollections;

        /// <summary>
        /// Construct the observer with a given logger.
        /// </summary>
        private TaskRunnerProfilingObserver(ILogger<IProfilingObserver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lastMemoryUsage = GC.GetTotalMemory(forceFullCollection: false);
            _lastGcCollections = GetGcCollectionCounts();
        }

        public static TaskRunnerProfilingObserver Create(ILogger<IProfilingObserver> logger)
            => new TaskRunnerProfilingObserver(logger);

        /// <summary>
        /// <inheritdoc cref="IObserver{T}.OnNext(T)"/>
        /// Notification is logged with memory usage data at each status change.
        /// </summary>
        public void OnNext(ITaskRunnerEvent value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var runnerName = value.RunnerDTO?.Name ?? "<unknown>";

            long currentMemory = GC.GetTotalMemory(forceFullCollection: false);
            int[] currentGcCollections = GetGcCollectionCounts();

            long memoryDelta = currentMemory - _lastMemoryUsage;

            bool gcOccurred = false;
            for (int i = 0; i < _lastGcCollections.Length; i++)
            {
                if (currentGcCollections[i] > _lastGcCollections[i])
                {
                    gcOccurred = true;
                    break;
                }
            }

            _logTaskStateChanged(
                _logger,
                value.Timestamp,
                runnerName,
                value.Status.ToString(),
                currentMemory,
                memoryDelta,
                gcOccurred ? "YES" : "NO",
                null
            );

            // Heuristic: warn if memory jumped by > 5MB since last event
            if (memoryDelta > 5 * 1024 * 1024)
            {
                _logSignificantMemoryIncrease(
                    _logger,
                    runnerName,
                    memoryDelta,
                    null
                );
            }

            _lastMemoryUsage = currentMemory;
            _lastGcCollections = currentGcCollections;
        }

        /// <summary>
        /// <inheritdoc cref="IObserver{T}.OnError(Exception)"/>
        /// </summary>
        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));

            _logPerfObserverError(_logger, error);
        }

        /// <summary>
        /// <inheritdoc cref="IObserver{T}.OnCompleted()"/>
        /// </summary>
        public void OnCompleted()
        {
            _logPerfObserverCompleted(_logger, null);
        }

        private static int[] GetGcCollectionCounts()
            => new[]
            {
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2)
            };

        private static readonly Action<ILogger, DateTime, string, string, long, long, string, Exception?> _logTaskStateChanged =
            LoggerMessage.Define<DateTime, string, string, long, long, string>(
                LogLevel.Information,
                EventCatalog.TaskStateChanged,
                "[{EventTime}] Task '{TaskName}' changed state to '{Status}'. Memory: {Memory:N0} bytes (Δ: {Delta:N0}). GC: {GcOccurred}"
            );

        private static readonly Action<ILogger, string, long, Exception?> _logSignificantMemoryIncrease =
            LoggerMessage.Define<string, long>(
                LogLevel.Warning,
                EventCatalog.SignificantMemoryIncrease,
                "⚠️ Significant memory increase detected after task '{TaskName}'. Δ = {Delta:N0} bytes"
            );

        private static readonly Action<ILogger, Exception> _logPerfObserverError =
            LoggerMessage.Define(
                LogLevel.Error,
                EventCatalog.ObserverError,
                "Error occurred in TaskRunnerProfilingObserver"
            );

        private static readonly Action<ILogger, Exception?> _logPerfObserverCompleted =
            LoggerMessage.Define(
                LogLevel.Information,
                EventCatalog.ObserverCompleted, // 1099 per your constants
                "TaskRunnerProfilingObserver completed"
            );
    }
}
