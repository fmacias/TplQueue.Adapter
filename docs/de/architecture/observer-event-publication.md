# Observer-Event-Publikation

Jede `IQ` implementiert `IObservable<IJobEvent>`.

- Gewöhnliche Job-Fehler werden über `OnNext` veröffentlicht.
- `OnError` ist für fatale Dispatcher-Fehler reserviert.
- `OnJobEventChanged` existiert für leichtgewichtige Async-Weiterleitung.

Intern verwendet `JobObserverHub` eine gequeue-te Pump auf Basis von `ConcurrentQueue<ObserverMessage>` und `SemaphoreSlim`, statt pro Event ein eigenes `Task.Run` zu starten.
