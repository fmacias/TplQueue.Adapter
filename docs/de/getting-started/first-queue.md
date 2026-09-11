# Erste Queue

Der empfohlene Einstiegspunkt für ASP.NET ist `Fmacias.TplQueue.Microsoft.DependencyInjection`.

## Queue- und Retry-Konfiguration laden

Die Adapter-Queue-Factories konsumieren benannte Retry-Policy- und Queue-Dictionaries. Halten Sie diese Metadaten unter `TplQueue:RetryPolicies` und `TplQueue:Queues`; `AddTplQueue` bindet sie und erstellt die Options-Snapshots.

```json
{
  "TplQueue": {
    "RetryPolicies": {
      "dashboard-default": {
        "BaseDelayMs": 200,
        "MaxRetries": 3,
        "Factor": 2.0
      }
    },
    "Queues": {
      "dashboard-metadata": {
        "Id": "2bdba3c7-7d17-4ea5-b2cb-7cf3f7ea14b9",
        "MaxParallelism": 1,
        "RetryPolicy": "dashboard-default"
      }
    }
  }
}
```

Fügen Sie eine explizite `Id` hinzu, wenn die Queue-Identität über Neustarts hinweg deterministisch bleiben muss oder wenn externe Systeme mit derselben Dispatcher-Identität korrelieren sollen.

Eine konfigurierte Queue darf `RetryPolicy` weglassen; null, eine leere Zeichenfolge oder nur Leerzeichen wählen NoRetry. Ein nicht registrierter, nicht leerer Name verwendet NoRetry. Eine fehlende `Id` wird einmal pro Settings-Instanz erzeugt und bleibt bei weiteren Abfragen gleich; für eine stabile Identität nach Neustarts geben Sie sie explizit an.

## Facade und Queue-Dictionaries registrieren

```csharp
using Fmacias.TplQueue.Core;
using Fmacias.TplQueue.Microsoft.DependencyInjection;

services.AddTplQueue(configuration, CoreApi.Create());
```

## Eine benannte `IParallelQ` erstellen

```csharp
var queueFactory = serviceProvider.GetRequiredService<IQFactoryAdapter>();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

IParallelQ queue = queueFactory.Parallel(
    "dashboard-metadata",
    loggerFactory.CreateLogger<IParallelQ>());
```

Der Adapter bietet außerdem explizite Overloads, wenn die Anwendung eine Queue direkt aus `IQOptions` oder aus rohen Werten wie `Guid`, `name` und `maxParallelism` instanziieren möchte.

## Eine benannte `IFifoQ` erstellen

```csharp
IFifoQ fifo = queueFactory.Fifo(
    "dashboard-metadata",
    loggerFactory.CreateLogger<IFifoQ>());
```

Relevante Source-Einstiegspunkte:

- [`API.Create(...)`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/API.cs)
- [`QFactoryAdapter`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/Factories/QFactoryAdapter.cs)
- [`AddTplQueue(...)`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Microsoft.DependencyInjection/ServiceCollectionExtensions.cs)

## Fokussierter öffentlicher ASP.NET-Sample-Ausschnitt

Das öffentliche `QueueObserverSignalRDashboard`-Sample in `TplQueue.Usage` veranschaulicht die Web-Host-Komposition:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddDashboardSample(builder.Configuration);
```

Verwenden Sie dieses Sample, wenn Sie die vollständige öffentliche Komposition rund um benannte Dispatcher, Runtime-Services und Web-Host-Wiring sehen möchten.

## Öffentliche Source-Hinweise

- [`QueueObserverSignalRDashboard/Program.cs`](https://github.com/fmacias/TplQueue.Usage/blob/main/samples/QueueObserverSignalRDashboard/Program.cs)
- [`QueueObserverSignalRDashboard/DashboardSampleServiceCollectionExtensions.cs`](https://github.com/fmacias/TplQueue.Usage/blob/main/samples/QueueObserverSignalRDashboard/DashboardSampleServiceCollectionExtensions.cs)
- [`QueueObserverSignalRDashboard/TplQueueDashboardSettings.cs`](https://github.com/fmacias/TplQueue.Usage/blob/main/samples/QueueObserverSignalRDashboard/TplQueueDashboardSettings.cs)
- [`IQ`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IQ.cs)
- [`IParallelQ`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IParallelQ.cs)
- [`IFifoQ`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IFifoQ.cs)
