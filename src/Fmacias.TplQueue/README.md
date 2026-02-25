# Fmacias.TplQueue

Facade module that composes adapter factories around `ICoreApi`, retry policy descriptors, and queue options.

## Responsibilities
- Exposes a single `IApi` implementation (`API`) for adapter composition.
- Creates `IDataJobFactory` instances from core factories plus retry policy factory.
- Exposes lazy queue factory access (`ICoreQFactoryAdapter`, `ICacheQFactory`).
- Resolves cache instances through `ICacheFactory<T>`.

## Local pipeline
Run from `TplQueue.Adapter` root:

1. Build module:
```powershell
dotnet build .\src\Fmacias.TplQueue\Fmacias.TplQueue.csproj
```

2. Run facade/adapter tests:
```powershell
dotnet test .\test\Fmacias.TplQueue.Unit.Test\Fmacias.TplQueue.Test.csproj
```

3. Pack with repo pipeline:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1
```

## Refactor note
This module now references externalized submodules for retry policies and cache providers. Previous embedded cache/retry concrete implementations were moved to dedicated projects.