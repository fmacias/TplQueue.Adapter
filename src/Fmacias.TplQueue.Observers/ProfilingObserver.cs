using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults.Log;
using Microsoft.Extensions.Logging;
using System;

namespace Fmacias.TplQueue.Observers
{
    internal sealed class ProfilingObserver : IProfilingObserver
    {
        private readonly ILogger<IProfilingObserver> _logger;
        private long _lastMemoryUsage;
        private int[] _lastGcCollections;

        private ProfilingObserver(ILogger<IProfilingObserver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lastMemoryUsage = GC.GetTotalMemory(false);
            _lastGcCollections = GetGcCollectionCounts();
        }

        public static ProfilingObserver Create(ILogger<IProfilingObserver> logger)
        {
            return new ProfilingObserver(logger);
        }

        public void OnNext(IJobEvent value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var jobName = value.JobInfo?.Name ?? "<unknown>";
            var currentMemory = GC.GetTotalMemory(false);
            var currentGcCollections = GetGcCollectionCounts();
            var memoryDelta = currentMemory - _lastMemoryUsage;

            var gcOccurred = false;
            for (var i = 0; i < _lastGcCollections.Length; i++)
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
                jobName,
                value.Status.ToString(),
                currentMemory,
                memoryDelta,
                gcOccurred ? "YES" : "NO",
                null);

            if (memoryDelta > 5 * 1024 * 1024)
            {
                _logSignificantMemoryIncrease(_logger, jobName, memoryDelta, null);
            }

            _lastMemoryUsage = currentMemory;
            _lastGcCollections = currentGcCollections;
        }

        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            _logPerfObserverError(_logger, error);
        }

        public void OnCompleted()
        {
            _logPerfObserverCompleted(_logger, null);
        }

        private static int[] GetGcCollectionCounts()
        {
            return new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
        }

        private static readonly Action<ILogger, DateTime, string, string, long, long, string, Exception?> _logTaskStateChanged =
            LoggerMessage.Define<DateTime, string, string, long, long, string>(
                LogLevel.Information,
                EventCatalog.jobStateChanged,
                "[{EventTime}] Task '{TaskName}' changed state to '{Status}'. Memory: {Memory:N0} bytes (Delta: {Delta:N0}). GC: {GcOccurred}");

        private static readonly Action<ILogger, string, long, Exception?> _logSignificantMemoryIncrease =
            LoggerMessage.Define<string, long>(
                LogLevel.Warning,
                EventCatalog.SignificantMemoryIncrease,
                "Significant memory increase detected after task '{TaskName}'. Delta = {Delta:N0} bytes");

        private static readonly Action<ILogger, Exception?> _logPerfObserverError =
            LoggerMessage.Define(
                LogLevel.Error,
                EventCatalog.ObserverError,
                "Error occurred in JobProfilingObserver");

        private static readonly Action<ILogger, Exception?> _logPerfObserverCompleted =
            LoggerMessage.Define(
                LogLevel.Information,
                EventCatalog.ObserverCompleted,
                "JobProfilingObserver completed");
    }
}
