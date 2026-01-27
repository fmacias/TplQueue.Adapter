using System;
using System.Collections.Generic;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract
{
    /// <summary>
    /// Base class for implementations of <see cref="IPayloadLeaseCache"/>.
    /// It encapsulates graph extraction and provides a synchronization primitive for data operations.
    /// Concrete caches (in-memory, EF, etc.) are responsible for the actual persistence and leasing logic.
    /// </summary>
    public abstract class CacheAbstract : IPayloadLeaseCache
    {
        private readonly object _syncDataOperations = new object();
        private readonly IJsonUniversalPayloadSerializer _universalPayloadSerializer;

        /// <summary>
        /// Synchronization object for derived classes. Use this for all stateful operations that
        /// read/write underlying storage to ensure thread safety.
        /// </summary>
        protected object SyncDataOperations => _syncDataOperations;

        /// <summary>
        /// Serializer used to (de)serialize payloads.
        /// </summary>
        protected IJsonUniversalPayloadSerializer Serializer => _universalPayloadSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheAbstract"/> class.
        /// </summary>
        protected CacheAbstract(
            IJsonUniversalPayloadSerializer serializer)
        {
//            _retryPolicySerializer = retryPolicySerializer
//                                     ?? throw new ArgumentNullException(nameof(retryPolicySerializer));
            _universalPayloadSerializer = serializer
                                          ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// Callback invoked for every node of the extracted task graph. Implementations use this to
        /// persist the node in their underlying storage (e.g. in-memory structures, EF, etc.).
        /// </summary>
        protected abstract Action<IJobNodeDto, Guid> AppendNodeCallBack { get; }

        /// <inheritdoc />
        public IReadOnlyList<IJobNodeDto> Append<TPayload>(
            IPayloadJobRoot<TPayload> root,
            bool isFifo)
            where TPayload : IPayloadCommand
        {
            if (root is null) throw new ArgumentNullException(nameof(root));

            return ExtractNodes(root, isFifo);
        }

        /// <inheritdoc />
        public abstract bool TryLeaseNextRoot(
            out IPayloadJobRoot payloadCarrierRoot,
            out ICacheLeaseEntry lease);

        /// <inheritdoc />
        public abstract void AckNode(Guid jobId, ISerializedPayload payloadData);

        /// <inheritdoc />
        public abstract void FailNode(Guid jobId, string? errorMessage);

        /// <inheritdoc />
        public abstract void CancelNode(Guid jobId);

        /// <inheritdoc />
        public abstract void SuccessRootNode(Guid jobRootId);

        /// <inheritdoc />
        public abstract bool DeleteRootNode(Guid jobRootId);

        /// <inheritdoc />
        public abstract ICacheLeaseEntry GetByJobId(Guid jobId);

        /// <inheritdoc />
        public abstract IPayloadLeaseCache CleanDeleted();
        public abstract IPayloadLeaseCache CleanFinalized();
        public abstract void LeaseRootNode(ICacheLeaseEntry leaseEntry);

        /// <summary>
        /// Extracts the task runner nodes from the given root using <see cref="TaskGraphDto"/>.
        /// </summary>
        protected IReadOnlyList<IJobNodeDto> ExtractNodes<TPayload>(
            IPayloadJobRoot<TPayload> JobRoot, bool isFifo)
            where TPayload : IPayloadCommand
        {
            if (JobRoot is null) throw new ArgumentNullException(nameof(JobRoot));

            return TaskGraphDto
                .Create(_universalPayloadSerializer, JobRoot, isFifo)
                .ExtractNodes(AppendNodeCallBack);
        }

        /// <summary>
        /// Allows derived caches to persist changes performed during <see cref="Append"/> when
        /// the resolved <paramref name="entry"/> is tied to an external store (e.g. EF entities).
        /// In-memory implementations can keep the default no-op behavior because the same
        /// reference is already being mutated in memory.
        /// </summary>
        /// <param name="entry">The resolved lease entry for the root task runner.</param>
        protected virtual void PersistEntryUpdate(ICacheLeaseEntry entry)
        {
            // Default: nothing to persist.
        }
    }
}
