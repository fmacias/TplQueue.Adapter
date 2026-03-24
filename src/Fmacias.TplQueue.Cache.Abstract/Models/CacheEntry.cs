using System;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;

namespace Fmacias.TplQueue.Cache.Abstract.Models
{
    /// <summary>
    /// Default in-memory implementation of <see cref="ICacheEntry"/>.
    /// Represents the cached state and lifecycle of a single task runner node.
    /// </summary>
    internal sealed class CacheEntry : ICacheEntry
    {
        public Guid LeaseId { get; }
        public Guid JobRootId { get; }
        public Guid JobId { get; }
        public Guid ParentJobId { get; }
        public IJobNodeRecord JobNodeRecordDto { get; }
        public bool IsFifo { get; }
        public DateTime CacheUtc { get; }
        public EntryStatus Status { get; private set; }
        public IRetryPolicyOptions RetryPolicyOptions => JobNodeRecordDto.RetryPolicyOptions;
        public bool IsRoot { get; private set; }
        public bool Deleted { get; private set; }
        public bool RootSuccessed { get; private set; }

        private CacheEntry(
            Guid leaseId,
            Guid jobRootId,
            Guid jobId,
            Guid parentJobId,
            IJobNodeRecord jobNodeRecordDto,
            DateTime cacheUtc)
        {
            if (leaseId == Guid.Empty) throw new ArgumentException("Lease id cannot be empty.", nameof(leaseId));
            if (jobRootId == Guid.Empty) throw new ArgumentException("Root id cannot be empty.", nameof(jobRootId));
            if (jobId == Guid.Empty) throw new ArgumentException("Task runner id cannot be empty.", nameof(jobId));
            if (jobNodeRecordDto is null) throw new ArgumentNullException(nameof(jobNodeRecordDto));

            LeaseId = leaseId;
            JobRootId = jobRootId;
            JobId = jobId;
            ParentJobId = parentJobId;
            JobNodeRecordDto = jobNodeRecordDto;
            CacheUtc = cacheUtc;
            Status = EntryStatus.Pending;
            IsRoot = jobNodeRecordDto.IsRoot;
            IsFifo = jobNodeRecordDto.IsFifo;
        }

        /// <summary>
        /// Factory method to create a new <see cref="ICacheEntry"/> instance.
        /// </summary>
        public static ICacheEntry Create(
            Guid leaseId,
            Guid jobRootId,
            Guid jobId,
            Guid parentJobId,
            IJobNodeRecord jobNodeRecordDto,
            DateTime cacheUtc)
        {
            return new CacheEntry(
                leaseId,
                jobRootId,
                jobId,
                parentJobId,
                jobNodeRecordDto,
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
        public void MarkAck(ISerializable payloadData, IUniversalDataSerializer jsonUniversalPayloadSerializer)
        {
            if (payloadData is null) throw new ArgumentNullException(nameof(payloadData));
            if (jsonUniversalPayloadSerializer == null) throw new ArgumentNullException(nameof(jsonUniversalPayloadSerializer));

            var jsonOutput = payloadData.Serialize(jsonUniversalPayloadSerializer);

            if (!string.IsNullOrEmpty(jsonOutput))
            {
                JobNodeRecordDto.UpdatePayloadJson(jsonOutput);
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
