using System;
using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract
{
    /// <summary>
    /// Default in-memory implementation of <see cref="ICacheLeaseEntry"/>.
    /// Represents the cached state and lifecycle of a single task runner node.
    /// </summary>
    internal sealed class CacheLeaseEntry : ICacheLeaseEntry
    {
        public Guid LeaseId { get; }
        public Guid JobRootId { get; }
        public Guid JobId { get; }
        public Guid ParentJobId { get; }
        public IJobNodeDto JobNodeDto { get; private set; }
        public bool IsFifo { get; private set; }
        public DateTime CacheUtc { get; }
        public EntryStatus Status { get; private set; }
        public IRetryPolicyDescriptor RetryDescriptor { get; set; }
        public bool IsRoot { get; private set; }
        public bool Deleted { get; private set; }
        public bool RootSuccessed { get; private set; }

        private CacheLeaseEntry(
            Guid leaseId,
            Guid jobRootId,
            Guid jobId,
            Guid parentJobId,
            IJobNodeDto jobNodeDto,
            DateTime cacheUtc)
        {
            if (leaseId == Guid.Empty) throw new ArgumentException("Lease id cannot be empty.", nameof(leaseId));
            if (jobRootId == Guid.Empty) throw new ArgumentException("Root id cannot be empty.", nameof(jobRootId));
            if (jobId == Guid.Empty) throw new ArgumentException("Task runner id cannot be empty.", nameof(jobId));
            if (jobNodeDto is null) throw new ArgumentNullException(nameof(jobNodeDto));

            LeaseId = leaseId;
            this.JobRootId = jobRootId;
            JobId = jobId;
            ParentJobId = parentJobId;
            JobNodeDto = jobNodeDto;
            CacheUtc = cacheUtc;
            RetryDescriptor = jobNodeDto.RetryDescriptor;
            Status = EntryStatus.Pending;
            IsRoot = jobNodeDto.IsRoot;
            IsFifo = jobNodeDto.IsFifo;
        }

        /// <summary>
        /// Factory method to create a new <see cref="ICacheLeaseEntry"/> instance.
        /// </summary>
        public static ICacheLeaseEntry Create(
            Guid leaseId,
            Guid jobRootId,
            Guid jobId,
            Guid parentJobId,
            IJobNodeDto jobNodeDto,
            DateTime cacheUtc)
        {
            return new CacheLeaseEntry(
                leaseId,
                jobRootId,
                jobId,
                parentJobId,
                jobNodeDto,
                cacheUtc);
        }

        /// <summary>
        /// Marks the entry as leased. This indicates that a worker has taken ownership of the node.
        /// </summary>
        public void MarkLeased()
        {
            Status = EntryStatus.Leased;
        }

        /// <summary>
        /// Marks the entry as acknowledged, updating the payload JSON if the execution produced an output.
        /// </summary>
        public void MarkAck(ISerializedPayload payloadData)
        {
            if (payloadData is null) throw new ArgumentNullException(nameof(payloadData));

            var jsonOutput = payloadData.JsonOutput ?? string.Empty;

            if (!string.IsNullOrEmpty(jsonOutput))
            {
                JobNodeDto.UpdatePayloadJson(jsonOutput);
            }

            Status = EntryStatus.Acknownledged;
        }

        /// <summary>
        /// Marks the entry as failed.
        /// </summary>
        public void MarkFailed()
        {
            Status = EntryStatus.Failed;
        }

        /// <summary>
        /// Marks the entry as canceled.
        /// </summary>
        public void MarkCanceled()
        {
            Status = EntryStatus.Canceled;
        }

        /// <summary>
        /// Marks the entry as removed. This is typically used after a root has been fully processed.
        /// </summary>
        public void MarkAsDeleted()
        {
            Deleted = true;
        }

        public bool IsFinalized()
        {
            return Status == EntryStatus.Acknownledged ||
                Status == EntryStatus.Canceled ||
                Status == EntryStatus.Failed;
        }

        public void MarkAsRootSuccessed()
        {
            RootSuccessed = true;
        }
    }
}
