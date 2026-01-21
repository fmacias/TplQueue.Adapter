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
        public Guid TaskRunnerRootId { get; }
        public Guid TaskRunnerId { get; }
        public Guid ParentTaskRunnerId { get; }
        public ITaskRunnerNodeDto TaskRunnerNodeDto { get; private set; }
        public bool IsFifo { get; private set; }
        public DateTime CacheUtc { get; }
        public EntryStatus Status { get; private set; }
        public IRetryPolicyDescriptor RetryDescriptor { get; set; }
        public bool IsRoot { get; private set; }
        public bool Deleted { get; private set; }
        public bool RootSuccessed { get; private set; }

        private CacheLeaseEntry(
            Guid leaseId,
            Guid rootId,
            Guid taskRunnerId,
            Guid parentTaskRunnerId,
            ITaskRunnerNodeDto nodeDto,
            DateTime cacheUtc)
        {
            if (leaseId == Guid.Empty) throw new ArgumentException("Lease id cannot be empty.", nameof(leaseId));
            if (rootId == Guid.Empty) throw new ArgumentException("Root id cannot be empty.", nameof(rootId));
            if (taskRunnerId == Guid.Empty) throw new ArgumentException("Task runner id cannot be empty.", nameof(taskRunnerId));
            if (nodeDto is null) throw new ArgumentNullException(nameof(nodeDto));

            LeaseId = leaseId;
            TaskRunnerRootId = rootId;
            TaskRunnerId = taskRunnerId;
            ParentTaskRunnerId = parentTaskRunnerId;
            TaskRunnerNodeDto = nodeDto;
            CacheUtc = cacheUtc;
            RetryDescriptor = nodeDto.RetryDescriptor;
            Status = EntryStatus.Pending;
            IsRoot = nodeDto.IsRoot;
            IsFifo = nodeDto.IsFifo;
        }

        /// <summary>
        /// Factory method to create a new <see cref="ICacheLeaseEntry"/> instance.
        /// </summary>
        public static ICacheLeaseEntry Create(
            Guid leaseId,
            Guid rootId,
            Guid taskRunnerId,
            Guid parentTaskRunnerId,
            ITaskRunnerNodeDto nodeDto,
            DateTime cacheUtc)
        {
            return new CacheLeaseEntry(
                leaseId,
                rootId,
                taskRunnerId,
                parentTaskRunnerId,
                nodeDto,
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
                TaskRunnerNodeDto.UpdatePayloadJson(jsonOutput);
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
