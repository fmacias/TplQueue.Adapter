using Fmacias.TplQueue.Cache.Contracts;
using Fmacias.TplQueue.Cache.Factories;
using Fmacias.TplQueue.Cache.MemCache.DomainModels;
using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fmacias.TplQueue.Cache.MemCache
{
    internal sealed class MemCache : CacheAbstract, IMemCache
    {
        private MemCache(
            IUniversalDataSerializer serializer,
            ICacheRepository cacheRepository,
            INodeTypeResolver typeResolver,
            IDataJobFactory payloadJobFactory,
            ICacheEntryFactory cacheEntryFactory)
            : base(serializer, cacheRepository, payloadJobFactory, cacheEntryFactory, typeResolver)
        {
        }

        public static MemCache Create(
            IUniversalDataSerializer serializer,
            IDataJobFactory payloadJobFactory,
            INodeTypeResolver jobNodeTypeResolver)
        {
            return new MemCache(serializer, 
                MemoryCacheRepository.Create(), 
                jobNodeTypeResolver, 
                payloadJobFactory, 
                CacheEntryFactory.Create());
        }

        protected override Action<IJobNodeDto, Guid> OnDehydration => (nodeDto, rootId) =>
        {
            CacheRepository.Upsert(
                EntryFactory.CreateEntry(
                    leaseId: Guid.NewGuid(),
                    jobRootId: rootId,
                    jobNodeDto: nodeDto,
                    cacheUtc: DateTime.UtcNow));
        };

        public IDataJobCache CleanDeleted()
        {
            var markedAsDeleted = CacheRepository
                .SnapshotAll()
                .Where(v => v.Deleted);

            Delete(markedAsDeleted);
            return this;
        }

        public IDataJobCache CleanFinalized()
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
    }
}
