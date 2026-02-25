# Fmacias.TplQueue.Cache.MemCache

In-memory cache provider built on top of `Fmacias.TplQueue.Cache.Abstract`.

## Contents
- `MemCacheFactory` creation entry point.
- `MemCache` in-memory implementation of `IMemCache`.
- Internal in-memory repository model for cache entries.

## Local pipeline
Run from `TplQueue.Adapter` root:

1. Build module:
```powershell
dotnet build .\src\Fmacias.TplQueue.Cache.MemCache\Fmacias.TplQueue.Cache.MemCache.csproj
```

2. Run module tests:
```powershell
dotnet test .\test\Fmacias.TplQueue.Cache.MemCache.Test\Fmacias.TplQueue.Cache.MemCache.Test.csproj
```

3. Pack through repo pipeline:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1
```

## Usage outline
Create with:
- `IUniversalDataSerializer`
- `IDataJobFactory`
- `INodeTypeResolver`

Then use cache dehydration/hydration APIs from `IMemCache`.