using Microsoft.Extensions.Logging;

namespace Fmacias.TplQueue.Log
{
    /// <summary>
    /// EventId catalog. Grouped by concern. Keep IDs stable once published.
    /// Ranges are arbitrary but grouped to help scanning logs:
    /// 1000-1999 Task lifecycle / dispatcher runtime
    /// 2000-2999 Memory / cache / leasing
    /// 3000-3999 Errors / observer / background
    /// 4000-4999 Retry / policy selection
    /// </summary>
    public static class EventCatalog
    {
        // ---- Dispatcher / task lifecycle ----
        public static readonly EventId jobStateChanged =
            new EventId(1001, nameof(jobStateChanged));

        public static readonly EventId PayloadJobSerialization =
            new EventId(1002, nameof(PayloadJobSerialization));

        public static readonly EventId JobStarted =
            new EventId(1003, nameof(JobStarted));

        public static readonly EventId JobRunning =
            new EventId(1004, nameof(JobRunning));

        public static readonly EventId JobCompleted =
            new EventId(1005, nameof(JobCompleted));

        public static readonly EventId JobCanceled =
            new EventId(1006, nameof(JobCanceled));

        public static readonly EventId JobFailed =
            new EventId(1007, nameof(JobFailed));

        // "Observer lifecycle"
        public static readonly EventId ObserverCompleted =
            new EventId(1099, nameof(ObserverCompleted));

        // ---- Cache / leasing / memory pressure ----
        public static readonly EventId SignificantMemoryIncrease =
            new EventId(2000, nameof(SignificantMemoryIncrease));

        public static readonly EventId CacheLeaseDequeued =
            new EventId(2001, nameof(CacheLeaseDequeued));

        public static readonly EventId CacheLeaseDequeueFailed =
            new EventId(2002, nameof(CacheLeaseDequeueFailed));

        public static readonly EventId CacheRootTerminalAck =
            new EventId(2003, nameof(CacheRootTerminalAck));

        public static readonly EventId LeaseTickError =
            new EventId(2004, nameof(LeaseTickError));

        // ---- Errors / infra ----
        public static readonly EventId ObserverError =
            new EventId(3000, nameof(ObserverError));

        public static readonly EventId BackgroundError =
            new EventId(3001, nameof(BackgroundError));

        public static readonly EventId RetryPolicyApplied =
            new EventId(4000, nameof(RetryPolicyApplied));
    }
}
