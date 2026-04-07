# Fmacias.TplQueue.Microsoft.DependencyInjection

Dependency Injection integration for `TplQueue.Adapter` using `Microsoft.Extensions.DependencyInjection`.

## Contents
- `ServiceCollectionExtensions.AddTplQueue(...)` overloads.
- `TplQueueOptionsBuilder` for fluent retry-policy and queue registration.
- Registration of `IApi`, read-only option dictionaries, and related adapter services.

## Local pipeline
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
