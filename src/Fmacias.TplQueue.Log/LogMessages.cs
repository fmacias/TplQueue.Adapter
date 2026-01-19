using Microsoft.Extensions.Logging;
using System;

namespace Fmaciasruano.TplQueue.Log
{
    /// <summary>
    /// Strongly-typed log message delegates.
    /// All delegates MUST be static readonly and created via LoggerMessage.Define*
    /// so we avoid allocations on hot paths.
    /// </summary>
    public static class LogMessages
    {
        public static readonly Action<ILogger, Guid, string, Exception?> PayloadCarrierRootDeserialized =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Debug,
                EventCatalog.PayloadCarrierRootDeserialized,
                "Cache-Collected: runner {RunnerId} ({RunnerName}).");

        public static readonly Action<ILogger, Exception?> PayloadCarrierRootDeserializedInfo =
            LoggerMessage.Define(
                LogLevel.Information,
                EventCatalog.PayloadCarrierRootDeserialized,
                "No PayloadCarrierRoot found for deserialization.");

        public static readonly Action<ILogger, Guid, string, Exception?> TaskStarted =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Information,
                EventCatalog.TaskStarted,
                "Starting runner {RunnerId} ({RunnerName}).");

        public static readonly Action<ILogger, Guid, string, Exception?> TaskRunning =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Debug,
                EventCatalog.TaskRunning,
                "Running runner {RunnerId} ({RunnerName}).");

        public static readonly Action<ILogger, Guid, string, Exception?> TaskCompleted =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Information,
                EventCatalog.TaskCompleted,
                "Completed runner {RunnerId} ({RunnerName}).");

        public static readonly Action<ILogger, Guid, string, Exception?> TaskCanceled =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Warning,
                EventCatalog.TaskCanceled,
                "Canceled runner {RunnerId} ({RunnerName}).");

        public static readonly Action<ILogger, Guid, string, Exception?> TaskFailed =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Error,
                EventCatalog.TaskFailed,
                "Runner {RunnerId} ({RunnerName}) failed.");

        public static readonly Action<ILogger, Guid, string, string, Exception?> TaskStateChanged =
            LoggerMessage.Define<Guid, string, string>(
                LogLevel.Debug,
                EventCatalog.TaskStateChanged,
                "Runner {RunnerId} ({RunnerName}) -> {NewState}.");

        public static readonly Action<ILogger, Guid, Guid, Exception?> CacheLeaseDequeued =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Debug,
                EventCatalog.CacheLeaseDequeued,
                "Dequeued lease for Root {RootId} / Runner {RunnerId}.");

        public static readonly Action<ILogger, Guid, Guid, Exception?> CacheLeaseDequeueFailed =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Debug,
                EventCatalog.CacheLeaseDequeueFailed,
                "Lease dequeue rejected for Root {RootId} / Runner {RunnerId} (stale or not pending).");

        public static readonly Action<ILogger, Guid, Exception?> CacheRootTerminalAck =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                EventCatalog.CacheRootTerminalAck,
                "Root {RootId} reached terminal state and was cleaned from cache.");

        public static readonly Action<ILogger, long, Exception?> SignificantMemoryIncrease =
            LoggerMessage.Define<long>(
                LogLevel.Warning,
                EventCatalog.SignificantMemoryIncrease,
                "Significant memory increase detected. Current approx bytes in cache: {ApproxBytes}.");

        public static readonly Action<ILogger, Guid, string, int, int, int, Exception?> RetryPolicyApplied =
            LoggerMessage.Define<Guid, string, int, int, int>(
                LogLevel.Debug,
                EventCatalog.RetryPolicyApplied,
                "Applying retry policy {PolicyKind} to Runner {RunnerId} ({RunnerName}). MaxRetries={MaxRetries}, BaseDelayMs={BaseDelayMs}.");

        public static readonly Action<ILogger, Exception> ObserverError =
            LoggerMessage.Define(
                LogLevel.Error,
                EventCatalog.ObserverError,
                "Observer callback threw.");

        public static readonly Action<ILogger, Exception> BackgroundError =
            LoggerMessage.Define(
                LogLevel.Error,
                EventCatalog.BackgroundError,
                "Background processing error.");

        public static readonly Action<ILogger, Exception> LeaseTickError =
            LoggerMessage.Define(
                LogLevel.Error,
                EventCatalog.LeaseTickError,
                "Error while feeding leased payload(s) from cache.");

        public static readonly Action<ILogger, Exception?> ObserverCompleted =
            LoggerMessage.Define(
                LogLevel.Debug,
                EventCatalog.ObserverCompleted,
                "Observer reported completion.");
    }
}
