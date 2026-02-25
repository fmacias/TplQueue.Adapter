# Fmacias.TplQueue.Cache.Abstract

Reusable cache orchestration primitives for data-job graph dehydration/hydration and lease lifecycle handling.

## Contents
- `CacheAbstract`: base workflow implementation.
- Domain models (`CacheEntry`, `JobNodeDto`, runtime node metadata).
- Factories: `CacheEntryFactory`, `RuntimeNodeTypeResolverFactory`.
- Contracts for resolver/factory and cache entry creation.

## Local pipeline
Run from `TplQueue.Adapter` root:

1. Build module:
```powershell
dotnet build .\src\Fmacias.TplQueue.Cache.Abstract\Fmacias.TplQueue.Cache.Abstract.csproj
```

2. Run module tests:
```powershell
dotnet test .\test\Fmacias.TplQueue.Cache.Abstract.Test\Fmacias.TplQueue.Cache.Abstract.Test.csproj
```

3. Pack through ordered repo pipeline:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1
```

## Refactor note
Node type resolution was renamed to runtime-based naming (`RuntimeNodeTypeResolver*`) to align with current semantic usage.