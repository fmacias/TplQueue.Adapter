using Microsoft.Extensions.Logging;
using System;

namespace Fmacias.TplQueue.Log
{
    /// <summary>
    /// Strongly-typed log message delegates.
    /// All delegates MUST be static readonly and created via LoggerMessage.Define*
    /// so we avoid allocations on hot paths.
    /// </summary>
    public static class LogMessages
    {
        public static readonly Action<ILogger, Guid, string, Exception?> PayloadJobRootDeserialized =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Debug,
                EventCatalog.PayloadJobSerialization,
                "Cache-Collected: Job {Id} ({Name}).");

        public static readonly Action<ILogger, Exception?> PayloadJobRootDeserializedInfo =
            LoggerMessage.Define(
                LogLevel.Information,
                EventCatalog.PayloadJobSerialization,
                "No PayloadJobRoot found for deserialization.");
        
        public static readonly Action<ILogger, Exception?> PayloadJobNotSerializableError =
            LoggerMessage.Define(
                LogLevel.Error,
                EventCatalog.PayloadJobSerialization,
                "Job must implement ISerializedPayload to be included into the CacheableQ workflow");

        public static readonly Action<ILogger, Guid, string, Exception?> JobStarted =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Information,
                EventCatalog.JobStarted,
                "Starting Job {Id} ({Name}).");

        public static readonly Action<ILogger, Guid, string, Exception?> JobRunning =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Debug,
                EventCatalog.JobRunning,
                "Running Job {Id} ({Name}).");

        public static readonly Action<ILogger, Guid, string, Exception?> JobCompleted =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Information,
                EventCatalog.JobCompleted,
                "Completed Job {Id} ({Name}).");

        public static readonly Action<ILogger, Guid, string, Exception?> JobCanceled =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Warning,
                EventCatalog.JobCanceled,
                "Canceled Job {Id} ({Name}).");

        public static readonly Action<ILogger, Guid, string, Exception?> JobFailed =
            LoggerMessage.Define<Guid, string>(
                LogLevel.Error,
                EventCatalog.JobFailed,
                "Job {Id} ({Name}) failed.");

        public static readonly Action<ILogger, Guid, string, string, Exception?> JobStateChanged =
            LoggerMessage.Define<Guid, string, string>(
                LogLevel.Debug,
                EventCatalog.jobStateChanged,
                "Job {Id} ({Name}) -> {NewState}.");

        public static readonly Action<ILogger, Guid, Guid, Exception?> CacheLeaseDequeued =
            LoggerMessage.Define<Guid, Guid>(
                LogLevel.Debug,
                EventCatalog.CacheLeaseDequeued,
                "Dequeued lease for Root {Id} / Job {Id}.");

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
