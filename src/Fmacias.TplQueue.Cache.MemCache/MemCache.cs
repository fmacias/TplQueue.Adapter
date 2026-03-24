using Fmacias.TplQueue.Cache.Abstract;
using Fmacias.TplQueue.Cache.Abstract.Factories;
using Fmacias.TplQueue.Cache.MemCache.Models;
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
            ITypeResolver typeResolver,
            IDataJobFactory payloadJobFactory,
            ICacheEntryFactory cacheEntryFactory,
            IPayloadHandlerResolver payloadHandlerResolver,
            IRetryPolicyAbstractFactory retryPolicyAbstractFactory)
            : base(serializer, cacheRepository, payloadJobFactory, cacheEntryFactory, typeResolver, payloadHandlerResolver, retryPolicyAbstractFactory)
        {
        }

        public static MemCache Create(
            IUniversalDataSerializer serializer,
            IDataJobFactory payloadJobFactory,
            ITypeResolver jobNodeTypeResolver, 
            IPayloadHandlerResolver payloadHandlerResolver, 
            IRetryPolicyAbstractFactory retryPolicyAbstractFactory)
        {
            return new MemCache(serializer, 
                MemoryCacheRepository.Create(), 
                jobNodeTypeResolver, 
                payloadJobFactory, 
                CacheEntryFactory.Create(),
                payloadHandlerResolver,
                retryPolicyAbstractFactory);
        }

        protected override Action<IJobNodeRecord, Guid> OnDehydration => (nodeDto, rootId) =>
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
