using System;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Abstract
{
    /// <summary>
    /// Provides factory helpers to create cache-related DTOs and lease entries.
    /// </summary>
    public sealed class Facade
    {
        /// <summary>
        /// Creates a cache lease entry for a given task runner node.
        /// </summary>
        /// <param name="leaseId">The unique lease identifier.</param>
        /// <param name="jobRootId">The identifier of the root task runner.</param>
        /// <param name="jobId">The identifier of the task runner.</param>
        /// <param name="parentJobId">The identifier of the parent task runner.</param>
        /// <param name="jobNodeDto">The task runner node DTO.</param>
        /// <param name="cacheUtc">The UTC timestamp when the node was cached.</param>
        /// <returns>A new <see cref="ICacheLeaseEntry"/> instance.</returns>
        public static ICacheLeaseEntry CreateLeaseEntry(
            Guid leaseId,
            Guid jobRootId,
            Guid jobId,
            Guid parentJobId,
            IJobNodeDto jobNodeDto,
            DateTime cacheUtc)
        {
            if (jobNodeDto is null) throw new ArgumentNullException(nameof(jobNodeDto));

            return CacheLeaseEntry.Create(
                leaseId,
                jobRootId,
                jobId,
                parentJobId,
                jobNodeDto,
                cacheUtc);
        }
    }
}
