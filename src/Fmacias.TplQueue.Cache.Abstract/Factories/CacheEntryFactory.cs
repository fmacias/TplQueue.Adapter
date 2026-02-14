using System;
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
        /// <summary>
        /// Creates a cache lease entry for a given task runner node.
        /// </summary>
        /// <param name="leaseId">The unique lease identifier.</param>
        /// <param name="jobRootId">The identifier of the root task runner.</param>
        /// <param name="jobNodeDto">The task runner node DTO.</param>
        /// <param name="cacheUtc">The UTC timestamp when the node was cached.</param>
        ///
        /// 
        /// <returns>A new <see cref="ICacheEntry"/> instance.</returns>
        public ICacheEntry CreateCacheEntry(
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
