# Dependency Injection

Registrieren Sie den Adapter mit einer `ICoreApi` und der Root-Konfiguration Ihrer Anwendung:

```csharp
using Fmacias.TplQueue.Core;
using Fmacias.TplQueue.Microsoft.DependencyInjection;

services.AddTplQueue(configuration, CoreApi.Create());
```

`AddTplQueue` liest `TplQueue:RetryPolicies` und `TplQueue:Queues`. Die JSON-Struktur steht unter [Erste Queue](../getting-started/first-queue.md). Jede konfigurierte Queue benötigt eine positive `MaxParallelism`. Der `RetryPolicy`-Name ist optional; null, eine leere Zeichenfolge oder nur Leerzeichen wählen NoRetry. Ein nicht registrierter, nicht leerer Policy-Name verwendet NoRetry. Eine fehlende Queue-`Id` wird einmal pro Settings-Instanz erzeugt; für eine stabile Identität nach Neustarts geben Sie sie explizit an.

Für die Konfiguration im Code verwenden Sie `ITplQueueSettings.Upsert`:

```csharp
using Fmacias.TplQueue.Defaults;

services.AddTplQueue(settings =>
{
    settings.Upsert("retry", RetryPolicyOptions.Create(200, 3, 2));
    settings.Upsert("main", new QOptions(Guid.NewGuid(), 2, "retry"));
}, CoreApi.Create());
```

Der Dictionary-Overload lautet `AddTplQueue(coreApi, retryPolicies, queues, configuration)`. Die Konfiguration ist optional. Explizite Dictionary-Einträge überschreiben konfigurierte Einträge mit demselben Namen unabhängig von Groß-/Kleinschreibung. Andere konfigurierte Einträge bleiben erhalten.

Registriert werden `IApi`, Job-/Queue-/Retry-/Observer-/Serializer-Factories, `ITplQueueSettings`, beide schreibgeschützten Options-Dictionaries und transiente JSON-/XML-Serializer. Die Dictionaries entsprechen genau den vom registrierten API bereitgestellten Snapshots.

Settings dienen der Konfiguration vor der Registrierung. Die Registrierung erstellt unabhängige Options-Snapshots. Ein späteres `Upsert` ändert weder API und Factories noch bestehende Queues; gleichzeitige Änderungen der Settings werden nicht unterstützt.

Für ältere Overloads und Konfigurationsnamen folgen Sie den [API-Migrationshinweisen](../operations/api-migration.md).
