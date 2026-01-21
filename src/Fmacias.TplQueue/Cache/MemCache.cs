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
        private readonly IPayloadRunnerFactory _payloadRunnerFactory;
        private MemCache(IPayloadRunnerFactory payloadRunnerFactory,
            IUniversalPayloadSerializer serializer)
            : base(serializer)
        {
            if (payloadRunnerFactory == null) throw new ArgumentNullException(nameof(payloadRunnerFactory));

            _payloadRunnerFactory = payloadRunnerFactory;
        }

        public static MemCache Create(IPayloadRunnerFactory payloadRunnerFactory,
            IUniversalPayloadSerializer serializer)
            => new MemCache(payloadRunnerFactory, serializer);

        protected override Action<ITaskRunnerNodeDto, Guid> AppendNodeCallBack 
            => (nodeDto, rootId) =>
            {
                AddDtoNodeToCache(
                    edgedNode: nodeDto,
                    leaseId: Guid.NewGuid(),
                    taskRunnerRootId: rootId);
            };
        public override void AckNode(Guid nodeId, ISerializedPayload payloadData)
        {
            var lease = GetByIdOrDefault(nodeId);
            lease?.MarkAck(payloadData);
        }

        public override void FailNode(Guid taskRunnerId, string? error)
        {
            var lease = GetByIdOrDefault(taskRunnerId);
            lease?.MarkFailed();
        }

        public override void CancelNode(Guid taskRunnerId)
        {
            var lease = GetByIdOrDefault(taskRunnerId);
            lease?.MarkCanceled();
        }

        public override void SuccessRootNode(Guid taskRunnerRootId)
        {
            var leaseRoot = GetByTaskRunnerId(taskRunnerRootId);
            
            if (leaseRoot == null) return;

            if (!leaseRoot.IsRoot) return;
            SuccessedRootFinalized(taskRunnerRootId);
        }

        public override bool DeleteRootNode(Guid rootId)
        {
            var leaseRoot = GetByTaskRunnerId(rootId);

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
                    v.TaskRunnerRootId == leaseRoot.TaskRunnerRootId &&
                    v.IsRoot == false).ToList().ForEach(e =>
                    {
                        e.MarkAsDeleted();
                    });
        }

        public override bool TryLeaseNextRoot(out IPayloadCarrierRoot payloadCarrierRoot, out ICacheLeaseEntry lease)
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
            ITaskRunnerNodeDto edgedNode,
            Guid leaseId,
            Guid taskRunnerRootId)
        {
            var entry = Facade.CreateLeaseEntry(
                leaseId: leaseId,
                rootId: taskRunnerRootId,
                taskRunnerId: edgedNode.TaskRunnerId,
                parentTaskRunnerId: edgedNode.ParentTaskRunnerId, 
                nodeDto: edgedNode,
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
                    entry.TaskRunnerId,
                    (id) => entry,
                    (id, currentEntry) => entry);
            }
        }

        private ICacheLeaseEntry GetByIdOrDefault(Guid taskRunnerId)
        {
            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations){
                entries = [.. _indexedEntries];
            }
            return entries
                .Where(kv => kv.Key == taskRunnerId)
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
                        v.TaskRunnerRootId == rootId && 
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
            var rootId = rootEntry.TaskRunnerRootId;

            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations)
            {
                entries = [.. _indexedEntries];
            }

            var nonSuccessedFound = entries.Select(kv => kv.Value)
                .Where(v => v.Status != EntryStatus.Acknownledged &&
                v.TaskRunnerRootId == rootId).Any();

            if (nonSuccessedFound)
                return;

            entries
                .Select(kv => kv.Value)
                .Where(v => v.Status == EntryStatus.Acknownledged &&
                    v.TaskRunnerRootId == rootId).ToList().ForEach((entry) => { 
                        entry.MarkAsRootSuccessed(); 
                    });
        }

        private bool TryCreatePayloadCarrierRoot(
            out IPayloadCarrierRoot payloadRootElement)
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
            
            var childs = RelatedChildsByParentNode(oldestRootCachedEntry.TaskRunnerId, entriesSnapshot);

            List<IPayloadCarrier> dependentElements = new();

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
            out IPayloadCarrier payloadElement,
            HashSet<Guid>? visited = null)
        {
            payloadElement = null!;

            if (leaseEntry == null) return false;           
            visited ??= new HashSet<Guid>();

            if (!visited.Add(leaseEntry.LeaseId))
                return false;
        
            if (entriesSnapshot.Length == 0) return false;

            var childs = RelatedChildsByParentNode(leaseEntry.TaskRunnerId, entriesSnapshot);

            List<IPayloadCarrier> dependentElements = new();

            foreach (var cacheLeaseEntry in childs)
            {
                if (TryCreatePayloadCarrier(
                    cacheLeaseEntry,
                    entriesSnapshot, 
                    out var currentPayloadCarrier, 
                    visited))
                    dependentElements.Add(currentPayloadCarrier);
                else
                    throw new InvalidOperationException($"Cannot create PayloadCarrier from cache [{cacheLeaseEntry.LeaseId}] of runner ({cacheLeaseEntry.TaskRunnerId}) ");
            }

            payloadElement  = _payloadRunnerFactory.Load(leaseEntry, Serializer);
            payloadElement.After([.. dependentElements]);
            return payloadElement != null;
        }
        private static IOrderedEnumerable<ICacheLeaseEntry> RelatedChildsByParentNode(
            Guid parentTaskRunnerId,
            ICacheLeaseEntry[] entriesSnapshot)
        {
            return entriesSnapshot
                .Where(l =>
                    l.ParentTaskRunnerId == parentTaskRunnerId &&
                    l.Status == EntryStatus.Pending)
                .OrderBy(l => l.TaskRunnerNodeDto.NodeCreationUtc);
        }
        private static ICacheLeaseEntry OldestRootCachedEntry(ICacheLeaseEntry[] entriesSnapshot)
        {
            return entriesSnapshot
                .Where(lease => lease.IsRoot == true && lease.Status == EntryStatus.Pending)
                .OrderBy(lease => lease.TaskRunnerNodeDto.NodeCreationUtc)
                .FirstOrDefault();
        }

        public override ICacheLeaseEntry GetByTaskRunnerId(Guid id)
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
                _indexedEntries.TryRemove(item.TaskRunnerId, out var removed);
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
            var rootId = rootEntry.TaskRunnerRootId;

            KeyValuePair<Guid, ICacheLeaseEntry>[] entries;

            lock (SyncDataOperations)
            {
                entries = [.. _indexedEntries];
            }
            entries.Select(kv => kv.Value)
                .Where(entry => entry.TaskRunnerRootId == rootId)
                .ToList().ForEach((entry) => entry.MarkLeased());
        }
    }
}
