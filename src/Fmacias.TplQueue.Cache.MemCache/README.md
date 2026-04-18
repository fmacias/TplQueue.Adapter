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
- `ITypeResolver`
- `IPayloadHandlers`
- `IRetryPolicyAbstractFactory`

Then use cache dehydration/hydration APIs from `IMemCache`.

Default runtime resolver example:

```csharp
using Fmacias.TplQueue;
using Fmacias.TplQueue.Cache.Abstract.Factories;
using Fmacias.TplQueue.Cache.MemCache;
using Fmacias.TplQueue.Contracts;

var serializer = api.SystemTextSerializerFactory().Serializer();
ITypeResolver typeResolver = RuntimeNodeTypeResolverFactory.Create().Resolver();

IMemCache cache = api.Cache<IMemCache>(
    MemCacheFactory.Create(),
    serializer,
    typeResolver);
```

If payload types must be resolved from a dedicated `AppDomain`, provide a custom `ITypeResolver` implementation and pass it to `CreateCache(...)`. `MemCache` depends only on the abstraction, not on the concrete runtime resolver.

## Roadmap

Current state:

- `MemCache` depends only on `ITypeResolver`, so it can work with the current AppDomain-based runtime resolver

Next step:

- if plugin payload types start being loaded from dedicated runtime boundaries in modern .NET, the preferred evolution is an `AssemblyLoadContext`-aware resolver behind `ITypeResolver`
- the current `AppDomain` path should be treated as a compatibility-oriented path rather than the long-term plugin-loading design
