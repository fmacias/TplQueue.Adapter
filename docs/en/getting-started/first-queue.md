# First Queue

The recommended starting point for ASP.NET is `Fmacias.TplQueue.Microsoft.DependencyInjection`.

## Load queue and retry configuration

The adapter queue factories consume named retry-policy and queue dictionaries. Keep that metadata under `TplQueue:RetryPolicies` and `TplQueue:Queues`; `AddTplQueue` binds it and creates the option snapshots.

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

Add an explicit `Id` when the queue identity must remain deterministic across restarts or when external systems need to correlate with the same dispatcher identity.

A configured queue may omit `RetryPolicy`; null, empty, or whitespace selects NoRetry. An unregistered nonblank name falls back to NoRetry. An omitted `Id` is generated once per settings instance and remains stable across reads; specify it to retain identity across restarts.

## Register the facade and queue dictionaries

```csharp
using Fmacias.TplQueue.Core;
using Fmacias.TplQueue.Microsoft.DependencyInjection;

services.AddTplQueue(configuration, CoreApi.Create());
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

## Focused public ASP.NET sample excerpt

The public `QueueObserverSignalRDashboard` sample in `TplQueue.Usage` illustrates web-host composition:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddDashboardSample(builder.Configuration);
```

Use that sample when you want the full public composition around named dispatchers, runtime services, and web-host wiring.

## Public source trail

- [`QueueObserverSignalRDashboard/Program.cs`](https://github.com/fmacias/TplQueue.Usage/blob/main/samples/QueueObserverSignalRDashboard/Program.cs)
- [`QueueObserverSignalRDashboard/DashboardSampleServiceCollectionExtensions.cs`](https://github.com/fmacias/TplQueue.Usage/blob/main/samples/QueueObserverSignalRDashboard/DashboardSampleServiceCollectionExtensions.cs)
- [`QueueObserverSignalRDashboard/TplQueueDashboardSettings.cs`](https://github.com/fmacias/TplQueue.Usage/blob/main/samples/QueueObserverSignalRDashboard/TplQueueDashboardSettings.cs)
- [`IQ`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IQ.cs)
- [`IParallelQ`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IParallelQ.cs)
- [`IFifoQ`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IFifoQ.cs)
