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
- `TplQueueOptionsBuilder` for fluent retry-policy and queue registration.
- Registration of `IApi`, read-only option dictionaries, and related adapter services.

## Canonical sample

The public SignalR dashboard sample registers TplQueue through DI like this:

```csharp
var settings = TplQueueDashboardSettings.Load(configuration);
var retryPolicies = settings.CreateRetryPolicies();
var dispatchers = settings.CreateDispatchers();
var api = API.Create(CoreApi.Create(), retryPolicies, dispatchers);

services.AddTplQueue(api, retryPolicies, dispatchers);
```

Full runnable solution:

- [QueueObserverSignalRDashboard](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)

## Repository operations

Repository build, test, coverage, packaging, and release steps are documented in the [TplQueue public operations guide](https://fmacias.github.io/tplqueue/operations/).

## Registration modes
- `AddTplQueue(IServiceCollection, IConfiguration, IApi)`
- `AddTplQueue(IServiceCollection, Action<TplQueueOptionsBuilder>, IApi)`
- `AddTplQueue(IServiceCollection, IApi, IDictionary<string, IRetryPolicyOptions>, IDictionary<string, IQOptions>)`
