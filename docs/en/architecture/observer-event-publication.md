# Observer Event Publication

Every `IQ` implements `IObservable<IJobEvent>`. Subscribe through `IQ.Subscribe` to receive job lifecycle notifications; the separate `OnJobEventChanged` property has been removed.

- Ordinary job failures are published through `OnNext`.
- `OnError` is reserved for fatal dispatcher failures.
- `Enqueued` and execution lifecycle events are delivered through the same observer stream. Publication buffers notifications; it does not invoke subscriber code inline under insertion locks.
- `Enqueued` means acceptance into the underlying in-memory dispatcher, after ownership is assigned. Persistence into a cache before hydration is a separate stage.

CacheQ owns a private observer and its subscription to the supplied queue. Terminal events drive cache acknowledgment, failure or cancellation updates, and cleanup. Applications can register their own observers independently; they cannot replace the private cache observer.

Delivery is asynchronous. `WaitAsync` waits for underlying queue work and does not wait for observers or cache acknowledgment. The shared observer hub uses one background pump, so a slow subscriber can delay cache updates and subsequent cache leasing. Keep observer callbacks short.

CacheQ.Dispose unsubscribes its observer before clearing its tracking state. Queued terminal notifications can remain unapplied when disposal starts, even after `WaitAsync` completes. Applications requiring confirmed cache acknowledgment before shutdown must verify it separately before disposal. See the [migration notes](../operations/api-migration.md) when replacing the removed callback API.
