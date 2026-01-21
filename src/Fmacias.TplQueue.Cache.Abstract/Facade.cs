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
        /// <param name="rootId">The identifier of the root task runner.</param>
        /// <param name="taskRunnerId">The identifier of the task runner.</param>
        /// <param name="parentTaskRunnerId">The identifier of the parent task runner.</param>
        /// <param name="nodeDto">The task runner node DTO.</param>
        /// <param name="cacheUtc">The UTC timestamp when the node was cached.</param>
        /// <returns>A new <see cref="ICacheLeaseEntry"/> instance.</returns>
        public static ICacheLeaseEntry CreateLeaseEntry(
            Guid leaseId,
            Guid rootId,
            Guid taskRunnerId,
            Guid parentTaskRunnerId,
            ITaskRunnerNodeDto nodeDto,
            DateTime cacheUtc)
        {
            if (nodeDto is null) throw new ArgumentNullException(nameof(nodeDto));

            return CacheLeaseEntry.Create(
                leaseId,
                rootId,
                taskRunnerId,
                parentTaskRunnerId,
                nodeDto,
                cacheUtc);
        }
    }
}
