using System;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.Contracts
{
    public interface ICacheEntryFactory
    {
        ICacheEntry CreateEntry(Guid leaseId, Guid jobRootId, IJobNodeDto jobNodeDto, DateTime cacheUtc);
    }
}
