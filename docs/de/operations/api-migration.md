# Aktuelle API-Migration

Die aktuelle Source-Linie enthält inkompatible Änderungen vor der stabilen Version. Aktualisieren und bauen Sie Abstractions, Core, Adapter-Module und konsumierende Anwendungen gemeinsam neu. Bereits kompilierte Anwendungen sind mit umbenannten Methoden oder verschobenen Extension-Typen nicht binärkompatibel. Diese Hinweise beschreiben die Source-Migration; Paketveröffentlichung und Versionswahl sind separate Release-Schritte.

| Bisherige Verwendung | Aktuelle Verwendung |
|---|---|
| `await queue.Wait()` | `await queue.WaitAsync()` |
| `CacheQ(logger, cache, queue)` | `CacheQ(() => cache, logger, queue)`; für eine eigene Instanz eine erzeugende Factory übergeben |
| Handler-Routing über PayloadId | `IPayload.HandlerKey` als stabilen Routing-Schlüssel implementieren; PayloadId identifiziert die Instanz |
| Core JobExtensions.Then | `Fmacias.TplQueue.Extensions` importieren; explizite statische Aufrufe verwenden JobExtension.Then aus Abstractions |
| Eigene IDataJob/IDataJobRoot-Implementierungen | Zusätzliche typisierte After-Member und geerbte Verträge implementieren |
| `AddTplQueue(api, retryPolicies, queues)` | `AddTplQueue(coreApi, retryPolicies, queues)` |
| TplQueueOptionsBuilder | `Action<ITplQueueSettings>` mit überladenen Upsert-Methoden |
| `TplQueue:Dispatchers` | `TplQueue:Queues` für den eingebauten Konfigurationsbinder |
| `IQ.OnJobEventChanged` | Entfernt; `IQ.Subscribe(IObserver<IJobEvent>)` für Annahme- und Ausführungsereignisse verwenden |

Verwenden Sie den [DI-Leitfaden](../development/dependency-injection.md) zusammen mit [Erste Queue](../getting-started/first-queue.md). Retry-Policy-Namen konfigurierter Queues sind optional; null, eine leere Zeichenfolge oder nur Leerzeichen wählen NoRetry. Beide schreibgeschützten Options-Dictionaries bleiben zusätzlich zu den Settings injizierbar. Änderungen an Settings nach der Registrierung ändern den API-Snapshot nicht.

Explizite Root-Policies einschließlich NoRetry behalten Vorrang. Nur eine nicht angegebene Root-Policy erbt die Queue-Policy. Fehler in Observer-Callbacks werden diagnostisch gemeldet und beenden den Dispatcher nicht. Unerwartete Fehler einer Retry-Factory bleiben fatal, auch Cancellation-/Disposal-Ausnahmen ohne Bezug zum Herunterfahren der Queue.

CacheQ besitzt nun eine private Observer-Subscription für terminale Cache-Aktualisierungen. `WaitAsync` wartet weiterhin auf die Arbeit der zugrunde liegenden Queue, nicht auf Observer-Zustellung oder Cache-Bestätigungen. Langsame Subscriber im gemeinsamen Hub können Cache-Aktualisierungen und nachfolgendes Leasing verzögern.

CacheQ.Dispose beendet diese Subscription und kann ausstehende terminale Benachrichtigungen unverarbeitet lassen. Ein abgeschlossenes `WaitAsync` mit unmittelbar anschließendem Dispose garantiert deshalb keine abgeschlossene Cache-Bestätigung. Synchronisieren Sie bei Bedarf separat mit dem Cache. Weitere Hinweise stehen unter [Observer-Event-Publikation](../architecture/observer-event-publication.md).

Behalten Sie bei der Migration persistierter Payloads deren Routing-Schlüssel oder ordnen Sie sie denselben Handlern zu. Bewahren Sie vorhandene Instanz-IDs und Zeitstempel bei der Deserialisierung; berechnen Sie sie nicht bei jedem Property-Zugriff neu. Prüfen Sie vor dem Deployment die Hydration bestehender Anwendungsdaten.
