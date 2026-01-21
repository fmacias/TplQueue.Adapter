using Fmacias.TplQueue.Cache.Abstract;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Fmacias.TplQueue.Cache
{
    internal sealed class MemCache : CacheAbstract, IMemCache
    {
        private readonly ConcurrentDictionary<Guid, ICacheLeaseEntry> _indexedEntries = new();
        private readonly IPayloadJobFactory _payloadRunnerFactory;
        private MemCache(IPayloadJobFactory payloadRunnerFactory,
            IUniversalPayloadSerializer serializer)
            : base(serializer)
        {
            if (payloadRunnerFactory == null) throw new ArgumentNullException(nameof(payloadRunnerFactory));

            _payloadRunnerFactory = payloadRunnerFactory;
        }

        public static MemCache Create(IPayloadJobFactory payloadRunnerFactory,
            IUniversalPayloadSerializer serializer)
            => new MemCache(payloadRunnerFactory, serializer);

        protected override Action<IJobNodeDto, Guid> AppendNodeCallBack 
            => (nodeDto, rootId) =>
            {
                AddDtoNodeToCache(
                    edgedNode: nodeDto,
                    leaseId: Guid.NewGuid(),
                    jobRootId: rootId);
            };
        public override void AckNode(Guid nodeId, ISerializedPayload payloadData)
        {
            var lease = GetByIdOrDefault(nodeId);
            lease?.MarkAck(payloadData);
        }

        public override void FailNode(Guid jobId, string? error)
        {
            var lease = GetByIdOrDefault(jobId);
            lease?.MarkFailed();
        }

        public override void CancelNode(Guid jobId)
        {
            var lease = GetByIdOrDefault(jobId);
            lease?.MarkCanceled();
        }

        public override void SuccessRootNode(Guid jobRootId)
        {
            var leaseRoot = GetByJobId(jobRootId);
            
            if (leaseRoot == null) return;

            if (!leaseRoot.IsRoot) return;
            SuccessedRootFinalized(jobRootId);
        }

        public override bool DeleteRootNode(Guid rootId)
        {
            var leaseRoot = GetByJobId(rootId);

            if (leaseRoot == null || !leaseRoot.IsFinalized())
            {
                return false;
            }

            if (AreRootChildsNotFinalized(rootId))
                throw new TplQueueErrorException($"Non finalized Cache entries found for finalized root([{rootId}])");

            MarkAsDeleted(leaseRoot);
            return true;
        }

        private void MarkAsDeleted(ICacheLeaseEntry leaseRoot)
        {
            leaseRoot.MarkAsDeleted();

            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations)
            {
                entries = [.. _indexedEntries];
            }
            entries
                .Select(kv => kv.Value)
                .Where(v => v.IsFinalized() == true &&
                    v.JobRootId == leaseRoot.JobRootId &&
                    v.IsRoot == false).ToList().ForEach(e =>
                    {
                        e.MarkAsDeleted();
                    });
        }

        public override bool TryLeaseNextRoot(out IPayloadJobRoot payloadCarrierRoot, out ICacheLeaseEntry lease)
        {
            payloadCarrierRoot = null!;
            lease = null!;

            if (!TryCreatePayloadCarrierRoot(out payloadCarrierRoot))
                return false;

            lease = GetByIdOrDefault(payloadCarrierRoot.Id);

            if (lease == null) return false;

            return true;
        }

        private void AddDtoNodeToCache(
            IJobNodeDto edgedNode,
            Guid leaseId,
            Guid jobRootId)
        {
            var entry = Facade.CreateLeaseEntry(
                leaseId: leaseId,
                jobRootId: jobRootId,
                jobId: edgedNode.JobId,
                parentJobId: edgedNode.ParentJobId, 
                jobNodeDto: edgedNode,
                cacheUtc: DateTime.UtcNow);

            PersistEntryUpdate(entry);
        }

        protected override void PersistEntryUpdate(ICacheLeaseEntry entry) 
        {
            if (entry is null)
                throw new ArgumentNullException(nameof(entry));
            
            lock (SyncDataOperations)
            {
                _indexedEntries.AddOrUpdate(
                    entry.JobId,
                    (id) => entry,
                    (id, currentEntry) => entry);
            }
        }

        private ICacheLeaseEntry GetByIdOrDefault(Guid jobId)
        {
            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations){
                entries = [.. _indexedEntries];
            }
            return entries
                .Where(kv => kv.Key == jobId)
                .Select(kv => kv.Value)
                .FirstOrDefault();
        }


        private bool AreRootChildsNotFinalized(Guid rootId)
        {
            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations)
            {
                entries = [.. _indexedEntries];
            }

            return entries
                    .Select(kv => kv.Value)
                    .Where(v => v.IsFinalized() == false && 
                        v.JobRootId == rootId && 
                        v.IsRoot == false).Any();
        }
        private void SuccessedRootFinalized(Guid rootId)
        {
            var rootEntry = GetByIdOrDefault(rootId);

            if (rootEntry == null || !rootEntry.IsRoot)
                return;

            SuccessRootChildEntries(rootEntry);
        }

        private void SuccessRootChildEntries(ICacheLeaseEntry rootEntry)
        {
            var rootId = rootEntry.JobRootId;

            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations)
            {
                entries = [.. _indexedEntries];
            }

            var nonSuccessedFound = entries.Select(kv => kv.Value)
                .Where(v => v.Status != EntryStatus.Acknownledged &&
                v.JobRootId == rootId).Any();

            if (nonSuccessedFound)
                return;

            entries
                .Select(kv => kv.Value)
                .Where(v => v.Status == EntryStatus.Acknownledged &&
                    v.JobRootId == rootId).ToList().ForEach((entry) => { 
                        entry.MarkAsRootSuccessed(); 
                    });
        }

        private bool TryCreatePayloadCarrierRoot(
            out IPayloadJobRoot payloadRootElement)
        {
            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations)
            {
                entries = [.. _indexedEntries];
            }

            var entriesSnapshot = entries.Select(kv => kv.Value).ToArray();

            payloadRootElement = default!;
            
            if (entriesSnapshot.Length == 0) return false;
            
            ICacheLeaseEntry oldestRootCachedEntry;
            oldestRootCachedEntry = OldestRootCachedEntry(entriesSnapshot);

            if (oldestRootCachedEntry == null) return false;
            
            var childs = RelatedChildsByParentNode(oldestRootCachedEntry.JobId, entriesSnapshot);

            List<IPayloadCarrierJob> dependentElements = new();

            foreach (var cacheLeaseEntry in childs)
            {
                if (TryCreatePayloadCarrier(cacheLeaseEntry, 
                    entriesSnapshot, 
                    out var payloadCarrier))
                    dependentElements.Add(payloadCarrier);
            }

            payloadRootElement = _payloadRunnerFactory.LoadRoot(oldestRootCachedEntry, Serializer);
            payloadRootElement.After([.. dependentElements]);
            return payloadRootElement != null;
        }
        private bool TryCreatePayloadCarrier(
            ICacheLeaseEntry leaseEntry,
            ICacheLeaseEntry[] entriesSnapshot,
            out IPayloadCarrierJob payloadElement,
            HashSet<Guid>? visited = null)
        {
            payloadElement = null!;

            if (leaseEntry == null) return false;           
            visited ??= new HashSet<Guid>();

            if (!visited.Add(leaseEntry.LeaseId))
                return false;
        
            if (entriesSnapshot.Length == 0) return false;

            var childs = RelatedChildsByParentNode(leaseEntry.JobId, entriesSnapshot);

            List<IPayloadCarrierJob> dependentElements = new();

            foreach (var cacheLeaseEntry in childs)
            {
                if (TryCreatePayloadCarrier(
                    cacheLeaseEntry,
                    entriesSnapshot, 
                    out var currentPayloadCarrier, 
                    visited))
                    dependentElements.Add(currentPayloadCarrier);
                else
                    throw new InvalidOperationException($"Cannot create PayloadCarrier from cache [{cacheLeaseEntry.LeaseId}] of runner ({cacheLeaseEntry.JobId}) ");
            }

            payloadElement  = _payloadRunnerFactory.Load(leaseEntry, Serializer);
            payloadElement.After([.. dependentElements]);
            return payloadElement != null;
        }
        private static IOrderedEnumerable<ICacheLeaseEntry> RelatedChildsByParentNode(
            Guid parentJobId,
            ICacheLeaseEntry[] entriesSnapshot)
        {
            return entriesSnapshot
                .Where(l =>
                    l.ParentJobId == parentJobId &&
                    l.Status == EntryStatus.Pending)
                .OrderBy(l => l.JobNodeDto.NodeCreationUtc);
        }
        private static ICacheLeaseEntry OldestRootCachedEntry(ICacheLeaseEntry[] entriesSnapshot)
        {
            return entriesSnapshot
                .Where(lease => lease.IsRoot == true && lease.Status == EntryStatus.Pending)
                .OrderBy(lease => lease.JobNodeDto.NodeCreationUtc)
                .FirstOrDefault();
        }

        public override ICacheLeaseEntry GetByJobId(Guid id)
        {
            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations)
            {
                entries = [.. _indexedEntries];
            }

            return entries
                .Where(kv => kv.Key == id)
                .Select(kv => kv.Value)
                .FirstOrDefault();
        }
        public override IPayloadLeaseCache CleanDeleted()
        {
            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations)
            {
                entries = [.. _indexedEntries];
            }
            var markedAsDeleted = entries
                    .Select(kv => kv.Value)
                    .Where(v => v.Deleted == true);

            Delete(markedAsDeleted);
            return this;
        }
        public override IPayloadLeaseCache CleanFinalized()
        {
            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations)
            {
                entries = [.. _indexedEntries];
            }
            var markedAsFailed = entries
                    .Select(kv => kv.Value)
                    .Where(v => v.IsFinalized() == true);

            Delete(markedAsFailed);
            return this;
        }
        private void Delete(IEnumerable<ICacheLeaseEntry> itemsToRemove)
        {
            foreach (var item in itemsToRemove)
            {
                _indexedEntries.TryRemove(item.JobId, out var removed);
            }
        }

        public override void LeaseRootNode(ICacheLeaseEntry leaseEntry)
        {
            if (leaseEntry == null) throw new ArgumentNullException(nameof(leaseEntry));

            if (!leaseEntry.IsRoot)
                return;
            LeaseRootGraph(leaseEntry);
        }

        private void LeaseRootGraph(ICacheLeaseEntry rootEntry)
        {
            var rootId = rootEntry.JobRootId;

            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations)
            {
                entries = [.. _indexedEntries];
            }
            entries.Select(kv => kv.Value)
                .Where(entry => entry.JobRootId == rootId)
                .ToList().ForEach((entry) => entry.MarkLeased());
        }
    }
}
