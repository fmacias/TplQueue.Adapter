# First Queue

The recommended starting point for ASP.NET is `Fmacias.TplQueue.Microsoft.DependencyInjection`.

## Load queue and retry configuration

The adapter queue factories consume named retry-policy and queue dictionaries. Keep that metadata in regular application configuration and convert it to `RetryPolicyOptions` and `QOptions`.

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

Add an explicit `Id` when the queue identity must remain deterministic across restarts or when external systems need to correlate with the same dispatcher identity.

## Register the facade and queue dictionaries

```csharp
var settings = TplQueueDashboardSettings.Load(configuration);
var retryPolicies = settings.CreateRetryPolicies();
var dispatchers = settings.CreateDispatchers();
var api = API.Create(CoreApi.Create(), retryPolicies, dispatchers);

services.AddSingleton(settings);
services.AddTplQueue(api, retryPolicies, dispatchers);
```

## Create a named `IParallelQ`

```csharp
var queueFactory = serviceProvider.GetRequiredService<IQFactoryAdapter>();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

IParallelQ queue = queueFactory.Parallel(
    "dashboard-metadata",
    loggerFactory.CreateLogger<IParallelQ>());
```

The adapter also exposes explicit overloads when the application wants to instantiate a queue from `IQOptions` directly or from raw values such as `Guid`, `name`, and `maxParallelism`.

## Create a named `IFifoQ`

```csharp
IFifoQ fifo = queueFactory.Fifo(
    "dashboard-metadata",
    loggerFactory.CreateLogger<IFifoQ>());
```

Relevant source entry points:

- [`API.Create(...)`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/API.cs)
- [`QFactoryAdapter`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/Factories/QFactoryAdapter.cs)
- [`AddTplQueue(...)`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Microsoft.DependencyInjection/ServiceCollectionExtensions.cs)
