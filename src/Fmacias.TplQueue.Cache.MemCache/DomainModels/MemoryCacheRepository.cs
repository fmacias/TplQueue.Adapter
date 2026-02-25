using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Fmacias.TplQueue.Cache.MemCache.DomainModels
{
    internal sealed class MemoryCacheRepository : ICacheRepository
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
