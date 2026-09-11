# Fmacias.TplQueue.Microsoft.DependencyInjection

Dependency Injection integration for [TplQueue.Adapter](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md) using `Microsoft.Extensions.DependencyInjection`.

See also:

- [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md)
- [TplQueue dependency injection guide](https://fmacias.github.io/tplqueue/development/dependency-injection/)
- [TplQueue.Usage QueueObserverSignalRDashboard sample](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)
- [Fmacias.TplQueue README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)

Repository-wide packaging and release operations are documented in the [TplQueue public operations guide](https://fmacias.github.io/tplqueue/operations/).

Use this package when your host application is built around `IServiceCollection` and you want to register TplQueue queues, retry policies, serializers, and adapter services through familiar `Microsoft.Extensions.DependencyInjection` patterns.

## Install

```bash
dotnet add package Fmacias.TplQueue.Microsoft.DependencyInjection --version 0.1.0-preview.1
```

## Contents
- `ServiceCollectionExtensions.AddTplQueue(...)` overloads.
- `ITplQueueSettings.Upsert(...)` for fluent retry-policy and queue registration.
- Registration of `IApi`, read-only option dictionaries, and related adapter services.

## Register from configuration

Pass the application root configuration and the Core API:

```csharp
using Fmacias.TplQueue.Core;
using Fmacias.TplQueue.Microsoft.DependencyInjection;

services.AddTplQueue(configuration, CoreApi.Create());
```

Configuration is read from `TplQueue:RetryPolicies` and `TplQueue:Queues`. Each configured queue requires a positive `MaxParallelism`. Its `RetryPolicy` name is optional; null, empty, or whitespace selects NoRetry. An unregistered nonblank policy name falls back to NoRetry. Omitted queue IDs are generated once per settings instance; configure IDs explicitly for identity across restarts.

For complete configuration examples and migration from earlier overloads, see the [DI guide](../../docs/en/development/dependency-injection.md) and [API migration notes](../../docs/en/operations/api-migration.md).

Related public application sample:

- [QueueObserverSignalRDashboard](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)

## Repository operations

Repository build, test, coverage, packaging, and release steps are documented in the [TplQueue public operations guide](https://fmacias.github.io/tplqueue/operations/).

## Registration modes
- `AddTplQueue(IServiceCollection, ICoreApi)`
- `AddTplQueue(IServiceCollection, IConfiguration, ICoreApi)`
- `AddTplQueue(IServiceCollection, Action<ITplQueueSettings>, ICoreApi)`
- `AddTplQueue(IServiceCollection, ICoreApi, IDictionary<string, IRetryPolicyOptions>, IDictionary<string, IQOptions>, IConfiguration? configuration = null)`

Explicit dictionary entries override configuration entries with matching names, ignoring case. Registration captures independent API option snapshots and exposes those same dictionaries as injectable read-only services. Later `ITplQueueSettings.Upsert` calls do not reconfigure the API, factories or existing queues. Settings mutation belongs to configuration time and is not concurrent.
