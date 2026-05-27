# Cache-Backed Recovery

`ICacheQ` is the public runtime surface used when a payload-aware graph must be persisted before in-memory execution.

1. The payload graph is dehydrated into the cache.
2. `ResumePolling()` enables the leasing loop.
3. `CacheQ` leases pending roots when the wrapped `IParallelQ` has capacity.
4. The leased root is hydrated and enqueued into the wrapped queue.
