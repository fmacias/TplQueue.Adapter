# Observer Event Publication

Every `IQ` implements `IObservable<IJobEvent>`.

- Ordinary job failures are published through `OnNext`.
- `OnError` is reserved for fatal dispatcher failures.
- `OnJobEventChanged` exists for lightweight async forwarding.

Internally, `JobObserverHub` uses a queued pump based on `ConcurrentQueue<ObserverMessage>` and `SemaphoreSlim` instead of spawning one `Task.Run` per event.
