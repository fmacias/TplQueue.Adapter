using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Fmacias.TplQueue.Cache
{
    internal sealed class MemCache : CacheAbstract, IMemCache
    {
        private MemCache(
            IUniversalPayloadSerializer serializer,
            ICacheRepository cacheRepository,
            INodeTypeResolver typeResolver, 
            IPayloadJobFactory payloadJobFactory,
            ICacheEntryFactory cacheEntryFactory)
            : base(serializer,cacheRepository,typeResolver,payloadJobFactory, cacheEntryFactory){}

        public static MemCache Create(
            IUniversalPayloadSerializer serializer,
            INodeTypeResolver jobNodeTypeResolver,
            IPayloadJobFactory payloadJobFactory,
            ICacheEntryFactory cacheEntryFactory)
        {
            return new MemCache(serializer, MemoryCacheRepository.Create(), 
                jobNodeTypeResolver, payloadJobFactory, cacheEntryFactory);
        }

        protected override Action<IJobNodeDto, Guid> OnDehydration => (nodeDto, rootId) =>
        {
            CacheRepository.Upsert(
                CacheEntryFactory.CreateCacheEntry(
                    leaseId: Guid.NewGuid(),
                    jobRootId: rootId,
                    jobNodeDto: nodeDto,
                    cacheUtc: DateTime.UtcNow
                )
            );
        };

        public IPayloadJobCache CleanDeleted()
        {
            var markedAsDeleted = CacheRepository
                .SnapshotAll()
                .Where(v => v.Deleted);

            Delete(markedAsDeleted);
            return this;
        }

        public IPayloadJobCache CleanFinalized()
        {
            var markedAsFinalized = CacheRepository
                .SnapshotAll()
                .Where(v => v.IsFinalized());

            Delete(markedAsFinalized);
            return this;
        }

        private void Delete(IEnumerable<ICacheEntry> itemsToRemove)
        {
            foreach (var item in itemsToRemove)
            {
                CacheRepository.TryRemove(item.JobId);
            }
        }
        private sealed class MemoryCacheRepository : ICacheRepository
        {
            private readonly ConcurrentDictionary<Guid, ICacheEntry> _entries;

            private MemoryCacheRepository()
            {
                _entries = new ConcurrentDictionary<Guid, ICacheEntry>();
            }
            public static MemoryCacheRepository Create()
            {
                return new MemoryCacheRepository();
            }
            public void Upsert(ICacheEntry entry)
            {
                if (entry is null) throw new ArgumentNullException(nameof(entry));
                _entries.AddOrUpdate(entry.JobId, entry, (_, _) => entry);
            }

            public bool TryGet(Guid jobId, out ICacheEntry entry)
            {
                return _entries.TryGetValue(jobId, out entry);
            }

            public ICacheEntry[] SnapshotAll()
            {
                return _entries.Values.ToArray();
            }

            public void TryRemove(Guid jobId)
            {
                _entries.TryRemove(jobId, out _);
            }

            public ICacheEntry? SelectOldestPendingRoot()
            {
                var entriesSnapshot = _entries.Values.ToArray();
                return entriesSnapshot
                    .Where(lease =>
                        lease.IsRoot &&
                        !lease.Deleted &&
                        lease.Status == EntryStatus.Pending)
                    .OrderBy(lease => lease.JobNodeDto.NodeCreationUtc)
                    .FirstOrDefault();
            }
            public IOrderedEnumerable<ICacheEntry> SelectPendingChildren(Guid parentJobId)
            {
                var entriesSnapshot = _entries.Values.ToArray();

                return entriesSnapshot
                    .Where(lease =>
                        !lease.IsRoot &&
                        !lease.Deleted &&
                        lease.ParentJobId == parentJobId &&
                        lease.Status == EntryStatus.Pending)
                    .OrderBy(lease => lease.JobNodeDto.NodeCreationUtc);
            }
        }
    }
}
