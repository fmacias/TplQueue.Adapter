using System;
using Fmacias.TplQueue.Cache.Abstract.Models;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract.Factories
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
            IJobNodeRecord jobNodeDto,
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
