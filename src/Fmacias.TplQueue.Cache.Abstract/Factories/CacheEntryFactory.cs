using System;
using Fmacias.TplQueue.Cache.Contracts;
using Fmacias.TplQueue.Cache.DomainModels;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Factories
{
    /// <summary>
    /// Provides factory helpers to create cache-related DTOs and lease entries.
    /// </summary>
    public sealed class CacheEntryFactory : ICacheEntryFactory
    {
        private CacheEntryFactory() { }
        public static ICacheEntryFactory Create()
        {
            return new CacheEntryFactory();
        }

        public ICacheEntry CreateEntry(
            Guid leaseId,
            Guid jobRootId,
            IJobNodeDto jobNodeDto,
            DateTime cacheUtc)
        {
            if (jobNodeDto is null) throw new ArgumentNullException(nameof(jobNodeDto));

            return CacheEntry.Create(
                leaseId,
                jobRootId,
                jobNodeDto.JobId,
                jobNodeDto.ParentJobId,
                jobNodeDto,
                cacheUtc);
        }
    }
}
