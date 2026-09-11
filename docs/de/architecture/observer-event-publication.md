# Observer-Event-Publikation

Jede `IQ` implementiert `IObservable<IJobEvent>`. Abonnieren Sie Job-Lebenszyklusereignisse über `IQ.Subscribe`; die separate Eigenschaft `OnJobEventChanged` wurde entfernt.

- Gewöhnliche Job-Fehler werden über `OnNext` veröffentlicht.
- `OnError` ist für fatale Dispatcher-Fehler reserviert.
- `Enqueued` und Ausführungsereignisse werden über denselben Observer-Stream zugestellt. Die Veröffentlichung puffert Benachrichtigungen; sie ruft keinen Subscriber-Code direkt unter den Einfügesperren auf.
- `Enqueued` bedeutet die Annahme durch den internen In-Memory-Dispatcher nach Zuweisung des Queue-Besitzers. Persistierung im Cache vor der Hydrierung ist eine separate Phase.

CacheQ besitzt einen privaten Observer und dessen Subscription auf die übergebene Queue. Terminale Ereignisse steuern Cache-Bestätigungen, Fehler- und Abbruchstatus sowie die Bereinigung. Anwendungen können unabhängig eigene Observer registrieren; sie können den privaten Cache-Observer nicht ersetzen.

Die Zustellung erfolgt asynchron. `WaitAsync` wartet auf die Arbeit der zugrunde liegenden Queue, nicht auf Observer oder Cache-Bestätigungen. Der gemeinsame Observer-Hub verwendet eine einzelne Hintergrund-Pump. Ein langsamer Subscriber kann deshalb Cache-Aktualisierungen und nachfolgendes Leasing verzögern. Halten Sie Observer-Callbacks kurz.

CacheQ.Dispose beendet die Subscription seines Observers, bevor es den Tracking-Zustand löscht. Gepufferte terminale Benachrichtigungen können beim Beginn der Freigabe unverarbeitet bleiben, auch wenn `WaitAsync` bereits abgeschlossen ist. Anwendungen, die vor dem Herunterfahren bestätigte Cache-Aktualisierungen benötigen, müssen diese vor Dispose separat prüfen. Beachten Sie beim Ersetzen der entfernten Callback-API die [Migrationshinweise](../operations/api-migration.md).
