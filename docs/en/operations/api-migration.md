# Current API migration

The current source line contains breaking changes before the stable release. Upgrade and rebuild Abstractions, Core, Adapter modules and consuming applications together. Previously compiled consumers are not binary-compatible with renamed methods or moved extension types. These notes describe the source migration; package publication and its version remain separate release steps.

| Previous usage | Current usage |
|---|---|
| `await queue.Wait()` | `await queue.WaitAsync()` |
| `CacheQ(logger, cache, queue)` | `CacheQ(() => cache, logger, queue)`; provide a factory that creates a cache when a distinct instance is needed |
| Handler routing through PayloadId | Implement `IPayload.HandlerKey` as a stable routing key; keep PayloadId as instance identity |
| Core JobExtensions.Then | Import `Fmacias.TplQueue.Extensions`; explicit static calls use `JobExtension.Then` from Abstractions |
| Custom IDataJob/IDataJobRoot implementations | Implement the added typed After members as well as inherited contracts |
| `AddTplQueue(api, retryPolicies, queues)` | `AddTplQueue(coreApi, retryPolicies, queues)` |
| TplQueueOptionsBuilder | `Action<ITplQueueSettings>` with overloaded Upsert methods |
| `TplQueue:Dispatchers` | `TplQueue:Queues` for the built-in configuration binder |
| `IQ.OnJobEventChanged` | Removed; use `IQ.Subscribe(IObserver<IJobEvent>)` for both acceptance and execution events |

Use the [DI guide](../development/dependency-injection.md) and [first queue](../getting-started/first-queue.md) examples together. Configured queue retry-policy names are optional; null, empty, or whitespace selects NoRetry. Both read-only option dictionaries remain injectable, alongside the configuration settings. Settings changes after registration do not update the registered API snapshot.

Retry selection preserves explicit root policies, including NoRetry. Only an unspecified root policy inherits the queue policy. Observer callback failures are diagnostic and do not terminate dispatch. Unexpected retry-factory failures remain fatal, including cancellation/disposal exceptions unrelated to queue shutdown.

CacheQ now owns a private observer subscription for terminal cache updates. `WaitAsync` still waits for underlying queue work; it does not wait for observer delivery or cache acknowledgment. Slow subscribers on the shared hub can delay cache updates and subsequent leasing.

CacheQ.Dispose unsubscribes that observer and may leave pending terminal notifications unapplied. A completed `WaitAsync` followed immediately by disposal therefore does not guarantee completed cache acknowledgment. Synchronize with the cache separately when the application requires that guarantee. See [observer event publication](../architecture/observer-event-publication.md).

When migrating persisted payloads, retain or map their routing keys to the same handlers. Preserve existing instance IDs and timestamps during deserialization; do not replace them with values computed on every property read. Test hydration against the application's previously persisted data before deployment.
