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
        public static readonly EventId TaskStateChanged =
            new EventId(1001, nameof(TaskStateChanged));

        public static readonly EventId PayloadCarrierRootDeserialized =
            new EventId(1002, nameof(PayloadCarrierRootDeserialized));

        public static readonly EventId TaskStarted =
            new EventId(1003, nameof(TaskStarted));

        public static readonly EventId TaskRunning =
            new EventId(1004, nameof(TaskRunning));

        public static readonly EventId TaskCompleted =
            new EventId(1005, nameof(TaskCompleted));

        public static readonly EventId TaskCanceled =
            new EventId(1006, nameof(TaskCanceled));

        public static readonly EventId TaskFailed =
            new EventId(1007, nameof(TaskFailed));

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
