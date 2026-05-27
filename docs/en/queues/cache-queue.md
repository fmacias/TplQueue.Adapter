# Cache Queue

`ICacheQ` extends queue execution with cache-backed dehydration and leasing of payload roots.

It is created from:

- an existing `IParallelQ`
- an `IDataJobCache`
- an `ILogger<ICacheQ>`

`ICacheQ.Enqueue(...)` dehydrates the payload graph into the cache first. After `ResumePolling()`, `CacheQ` leases pending roots, hydrates them, and enqueues the hydrated root into the wrapped queue.
