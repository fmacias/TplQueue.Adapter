# Fmacias.TplQueue.Microsoft.DependencyInjection

Dependency Injection integration for [TplQueue.Adapter](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md) using `Microsoft.Extensions.DependencyInjection`.

See also:

- [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md)
- [TplQueue.Core README](https://github.com/fmacias/TplQueue.Core/blob/main/README.md)
- [TplQueue.Usage QueueObserverSignalRDashboard sample](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)
- [Fmacias.TplQueue README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)

Repository-wide packaging and strong-name signing rules are documented in the [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md#strong-name-signing).

Use this package when your host application is built around `IServiceCollection` and you want to register TplQueue queues, retry policies, serializers, and adapter services through familiar `Microsoft.Extensions.DependencyInjection` patterns.

## Install

```bash
dotnet add package Fmacias.TplQueue.Microsoft.DependencyInjection --version 0.1.0-preview.1
```

## Contents
- `ServiceCollectionExtensions.AddTplQueue(...)` overloads.
- `TplQueueOptionsBuilder` for fluent retry-policy and queue registration.
- Registration of `IApi`, read-only option dictionaries, and related adapter services.

## Repository build notes
Run from `TplQueue.Adapter` root:

1. Build module:
```powershell
dotnet build .\src\Fmacias.TplQueue.Microsoft.DependencyInjection\Fmacias.TplQueue.Microsoft.DependencyInjection.csproj
```

2. Run module tests:
```powershell
dotnet test .\test\Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test\Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test.csproj
```

3. Pack through repo pipeline:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1
```

## Registration modes
- `AddTplQueue(IServiceCollection, IConfiguration, IApi)`
- `AddTplQueue(IServiceCollection, Action<TplQueueOptionsBuilder>, IApi)`
- `AddTplQueue(IServiceCollection, IApi, IDictionary<string, IRetryPolicyOptions>, IDictionary<string, IQOptions>)`
