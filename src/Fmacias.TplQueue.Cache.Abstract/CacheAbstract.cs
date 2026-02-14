using Fmacias.TplQueue.Cache.Helpers;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fmacias.TplQueue.Cache
{
    /// <summary>
    /// Base class for implementations of <see cref="IPayloadJobCache"/>.
    /// It encapsulates graph extraction and provides a synchronization primitive for data operations.
    /// Concrete caches (in-memory, EF, etc.) are responsible for the actual persistence and leasing logic.
    /// </summary>
    public abstract class CacheAbstract : IPayloadJobCache
    {
        private readonly object _syncDataOperations = new object();
        private readonly IUniversalPayloadSerializer _serializer;
        private readonly ICacheRepository _cacheRepository;
        private readonly INodeTypeResolver _typeResolver;
        private readonly IPayloadJobFactory _payloadJobFactory;
        private readonly ICacheEntryFactory _cacheEntryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheAbstract"/> class.
        /// </summary>
        protected CacheAbstract(IUniversalPayloadSerializer serializer,
            ICacheRepository cacheRepository,
            INodeTypeResolver jobNodeTypeResolver,
            IPayloadJobFactory payloadJobFactory,
            ICacheEntryFactory cacheEntryFactory)
        {
            _serializer = serializer
                ?? throw new ArgumentNullException(nameof(serializer));
            _cacheRepository = cacheRepository
                ?? throw new ArgumentNullException(nameof(cacheRepository));
            _typeResolver = jobNodeTypeResolver
                ?? throw new ArgumentNullException(nameof(jobNodeTypeResolver));
            _payloadJobFactory = payloadJobFactory
                ?? throw new ArgumentNullException(nameof(payloadJobFactory));
            _cacheEntryFactory = cacheEntryFactory
                ?? throw new ArgumentNullException(nameof(cacheEntryFactory));
        }

        /// <summary>
        /// Synchronization object for derived classes. Use this for all stateful operations that
        /// read/write underlying storage to ensure thread safety.
        /// </summary>
        protected object SyncDataOperations => _syncDataOperations;

        /// <summary>
        /// Serializer used to (de)serialize payloads.
        /// </summary>
        protected IUniversalPayloadSerializer Serializer => _serializer;
        protected ICacheRepository CacheRepository => _cacheRepository;
        protected INodeTypeResolver TypeResolver => _typeResolver;
        protected IPayloadJobFactory PayloadJobFactory => _payloadJobFactory;
        protected ICacheEntryFactory CacheEntryFactory => _cacheEntryFactory;

        /// <summary>
        /// Callback invoked for every node of the extracted job graph. Implementations use this to
        /// persist the node in their underlying storage (e.g. in-memory structures, EF, etc.).
        /// </summary>
        protected abstract Action<IJobNodeDto, Guid> OnDehydration { get; }

        public virtual void AckNode(Guid jobId, ISerializable payloadData)
        {
            if (jobId == Guid.Empty) throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
            if (payloadData is null) throw new ArgumentNullException(nameof(payloadData));

            var lease = GetByIdOrDefault(jobId);
            lease?.MarkAck(payloadData, Serializer);
        }
        public virtual void FailNode(Guid jobId, string? errorMessage)
        {
            if (jobId == Guid.Empty) throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
            _ = errorMessage;
            var lease = GetByIdOrDefault(jobId);
            lease?.MarkFailed();
        }

        /// <inheritdoc />
        public virtual void CancelNode(Guid jobId)
        {
            if (jobId == Guid.Empty) throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
            var lease = GetByIdOrDefault(jobId);
            lease?.MarkCanceled();
        }

        /// <inheritdoc />
        public virtual void SuccessRootNode(Guid jobRootId)
        {
            if (jobRootId == Guid.Empty) throw new ArgumentException("Root job id cannot be empty.", nameof(jobRootId));

            var leaseRoot = GetByIdOrDefault(jobRootId);
            if (leaseRoot == null || !leaseRoot.IsRoot) return;
            SuccessedRootFinalized(jobRootId);
        }

        /// <inheritdoc />
        public virtual bool DeleteRootNode(Guid jobRootId)
        {
            if (jobRootId == Guid.Empty) throw new ArgumentException("Root job id cannot be empty.", nameof(jobRootId));

            lock (SyncDataOperations)
            {
                var leaseRoot = GetByJobId(jobRootId);
                if (leaseRoot == null || !leaseRoot.IsFinalized())
                {
                    return false;
                }

                if (AreRootChildsNotFinalized(jobRootId))
                    throw new TplQueueErrorException($"Non finalized Cache entries " +
                        $"found for finalized root([{jobRootId}])");

                MarkAsDeleted(leaseRoot);
                return true;
            }
        }

        /// <inheritdoc />
        public virtual ICacheEntry GetByJobId(Guid jobId)
        {
            if (jobId == Guid.Empty) throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
            return GetByIdOrDefault(jobId);
        }

        public virtual void LeaseRootNode(ICacheEntry leaseEntry)
        {
            if (leaseEntry == null) throw new ArgumentNullException(nameof(leaseEntry));
            if (!leaseEntry.IsRoot) return;

            LeaseRootGraph(leaseEntry);
        }

        #region Default overwriteable implementation
        /// <summary>
        /// Traverses a payload graph and returns its serialized DTO nodes.
        /// </summary>
        public virtual IReadOnlyList<IJobNodeDto> Dehydrate<TPayload>(
            IPayloadJobRoot<TPayload> root,
            bool isFifo)
            where TPayload : IPayload
        {
            if (root is null) throw new ArgumentNullException(nameof(root));

            return JobGraphDto
                .Create(_serializer, root, isFifo)
                .ExtractNodes(OnDehydration);
        }

        /// <inheritdoc />
        public virtual bool TryHydrateNextJob(
            out IPayloadJobRoot payloadJobRoot,
            out ICacheEntry lease)
        {
            payloadJobRoot = null!;
            lease = null!;
            var oldestRootCachedEntry = _cacheRepository.SelectOldestPendingRoot();

            if (oldestRootCachedEntry == null)
            {
                return false;
            }
            var rootPayload = DeserializePayload(
                oldestRootCachedEntry,
                parentJobId: null,
                rootJobId: oldestRootCachedEntry.JobId
            );
            var rootJob = _payloadJobFactory.CreateJobRoot(oldestRootCachedEntry.JobNodeDto, rootPayload);
            if (rootJob == null)
            {
                throw new InvalidOperationException(
                    $"Payload job factory returned null root for JobId '{oldestRootCachedEntry.JobId}'.");
            }

            var dependencies = new List<IPayloadCarrierJob>();
            var visited = new HashSet<Guid> { oldestRootCachedEntry.JobId };

            foreach (var childEntry in _cacheRepository.SelectPendingChildren(oldestRootCachedEntry.JobId))
            {
                var childJob = CreatePayloadCarrier(
                    childEntry,
                    rootJobId: oldestRootCachedEntry.JobId,
                    visited: visited
                );
                dependencies.Add(childJob);
            }
            rootJob.After(dependencies.ToArray());
            payloadJobRoot = rootJob;
            lease = oldestRootCachedEntry;
            return true;
        }
        #endregion

        protected virtual IPayload DeserializePayload(
            ICacheEntry leaseEntry,
            Guid? parentJobId,
            Guid rootJobId)
        {
            if (leaseEntry == null) throw new ArgumentNullException(nameof(leaseEntry));

            var dto = leaseEntry.JobNodeDto;
            var payloadType = _typeResolver.Resolve(dto.PayloadTypeName);
            var obj = _serializer.Deserialize(dto.PayloadJson, payloadType);

            if (obj is IPayload payload)
            {
                return payload;
            }
            var parentPart = parentJobId.HasValue
                ? $", ParentJobId '{parentJobId.Value}'"
                : string.Empty;

            var actualType = obj?.GetType().FullName ?? "null";

            throw new InvalidOperationException(
                $"Invalid hydrated payload type for JobId '{dto.JobId}', PayloadTypeName '{dto.PayloadTypeName}', RootJobId '{rootJobId}'{parentPart}, LeaseId '{leaseEntry.LeaseId}'. ActualType '{actualType}'.");
        }

        protected virtual IPayloadCarrierJob CreatePayloadCarrier(
            ICacheEntry leaseEntry,
            Guid rootJobId,
            HashSet<Guid> visited)
        {
            if (leaseEntry == null) throw new ArgumentNullException(nameof(leaseEntry));
            if (visited == null) throw new ArgumentNullException(nameof(visited));


            if (!visited.Add(leaseEntry.JobId))
            {
                throw new InvalidOperationException(
                    $"Cycle detected while hydrating payload graph. JobId '{leaseEntry.JobId}', RootJobId '{rootJobId}'.");
            }
            var childDependencies = new List<IPayloadCarrierJob>();
            foreach (var childEntry in _cacheRepository.SelectPendingChildren(leaseEntry.JobId))
            {
                try
                {
                    childDependencies.Add(CreatePayloadCarrier(
                        childEntry,
                        rootJobId,
                        visited));
                }
                catch (Exception ex) when (!(ex is InvalidOperationException))
                {
                    throw new InvalidOperationException(
                        $"Failed to create child payload node. ChildJobId '{childEntry.JobId}', ParentJobId '{leaseEntry.JobId}', PayloadTypeName '{childEntry.JobNodeDto.PayloadTypeName}', LeaseId '{childEntry.LeaseId}'.",
                        ex);
                }
            }
            var payload = DeserializePayload(
                leaseEntry,
                parentJobId: leaseEntry.ParentJobId,
                rootJobId: rootJobId
            );
            var node = _payloadJobFactory.CreateJob(leaseEntry.JobNodeDto, payload);
            if (node == null)
            {
                throw new InvalidOperationException(
                    $"Payload job factory returned null job for JobId '{leaseEntry.JobId}', RootJobId '{rootJobId}'.");
            }

            node.After(childDependencies.ToArray());
            return node;
        }
        protected virtual ICacheEntry GetByIdOrDefault(Guid jobId)
        {
            CacheRepository.TryGet(jobId, out var entry);
            return entry;
        }
        protected virtual bool AreRootChildsNotFinalized(Guid rootId)
        {
            var entriesSnapshot = CacheRepository.SnapshotAll();
            return entriesSnapshot
                .Any(v => !v.IsFinalized() && v.JobRootId == rootId && !v.IsRoot);
        }
        protected virtual void SuccessRootChildEntries(ICacheEntry rootEntry)
        {
            if (rootEntry == null) throw new ArgumentNullException(nameof(rootEntry));

            lock (SyncDataOperations)
            {
                var rootId = rootEntry.JobRootId;
                var entriesSnapshot = CacheRepository.SnapshotAll();

                var nonSuccessedFound = entriesSnapshot
                    .Any(v => v.Status != EntryStatus.Acknownledged && v.JobRootId == rootId);

                if (nonSuccessedFound)
                    return;

                entriesSnapshot
                    .Where(v => v.Status == EntryStatus.Acknownledged && v.JobRootId == rootId)
                    .ToList()
                    .ForEach(entry => entry.MarkAsRootSuccessed());
            }
        }
        protected virtual void SuccessedRootFinalized(Guid rootId)
        {
            var rootEntry = GetByIdOrDefault(rootId);
            if (rootEntry == null || !rootEntry.IsRoot)
                return;

            SuccessRootChildEntries(rootEntry);
        }
        protected virtual void LeaseRootGraph(ICacheEntry rootEntry)
        {
            if (rootEntry == null) throw new ArgumentNullException(nameof(rootEntry));

            lock (SyncDataOperations)
            {
                var rootId = rootEntry.JobRootId;
                var entriesSnapshot = CacheRepository.SnapshotAll();
                entriesSnapshot
                    .Where(entry => entry.JobRootId == rootId)
                    .ToList()
                    .ForEach(entry => entry.MarkLeased());
            }
        }

        protected virtual void MarkAsDeleted(ICacheEntry leaseRoot)
        {
            if (leaseRoot == null) throw new ArgumentNullException(nameof(leaseRoot));

            leaseRoot.MarkAsDeleted();

            var entriesSnapshot = CacheRepository.SnapshotAll();
            entriesSnapshot
                .Where(v => v.IsFinalized() && v.JobRootId == leaseRoot.JobRootId && !v.IsRoot)
                .ToList()
                .ForEach(e => e.MarkAsDeleted());
        }
    }
}
