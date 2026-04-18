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

IUniversalDataSerializer jsonSerializer = api.SystemTextSerializerFactory().Serializer();
IUniversalDataSerializer xmlSerializer = api.XmlSerializerFactory().Serializer();
ITypeResolver typeResolver = RuntimeNodeTypeResolverFactory.Create().Resolver();

IMemCache cache = api.Cache<IMemCache>(
    MemCacheFactory.Create(),
    jsonSerializer,
    typeResolver);
```

If payload types must be resolved from a dedicated `AppDomain`, provide a custom `ITypeResolver` implementation and pass it to `CreateCache(...)`. `MemCache` depends only on the abstraction, not on the concrete runtime resolver.

Use the same cache object to dehydrate a payload root, hydrate it back, and dispatch it through a queue:

```csharp
cache.Dehydrate(payloadRoot, isFifo: false);

ILogger<IParallelQ> queueLogger = loggerFactory.CreateLogger<IParallelQ>();

if (cache.TryHydrateNextJob(out IDataJobRoot hydratedRoot, out ICacheEntry lease))
{
    IParallelQ queue = api.QFactory.Parallel("main", queueLogger);

    queue.Enqueue(hydratedRoot, CancellationToken.None);
    queue.Start();

    await hydratedRoot.WaitUntilFinishedAsync();
}
```

Use `xmlSerializer` instead of `jsonSerializer` in `api.Cache<IMemCache>(...)` when XML payload storage is required.

## Compatibility note

Some public persisted members still expose JSON-oriented names such as `PayloadJson`.
Those names are retained for compatibility and should be read as serializer-specific payload content. `MemCache` can store payload content produced by either the JSON or XML `IUniversalDataSerializer` implementation selected during cache creation.

## Runtime type resolution status

Current state:

- `MemCache` depends only on `ITypeResolver`, so it can work with the current AppDomain-based runtime resolver

Deferred work:

- if plugin payload types start being loaded from dedicated runtime boundaries in modern .NET, the preferred evolution is an `AssemblyLoadContext`-aware resolver behind `ITypeResolver`
