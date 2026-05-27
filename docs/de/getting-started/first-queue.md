# Erste Queue

Der empfohlene Einstiegspunkt für ASP.NET ist `Fmacias.TplQueue.Microsoft.DependencyInjection`.

## Queue- und Retry-Konfiguration laden

Die Adapter-Queue-Factories konsumieren benannte Retry-Policy- und Queue-Dictionaries. Halten Sie diese Metadaten in der normalen Anwendungskonfiguration und konvertieren Sie sie in `RetryPolicyOptions` und `QOptions`.

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
    "Dispatchers": {
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

## Facade und Queue-Dictionaries registrieren

```csharp
var settings = TplQueueDashboardSettings.Load(configuration);
var retryPolicies = settings.CreateRetryPolicies();
var dispatchers = settings.CreateDispatchers();
var api = API.Create(CoreApi.Create(), retryPolicies, dispatchers);

services.AddSingleton(settings);
services.AddTplQueue(api, retryPolicies, dispatchers);
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
