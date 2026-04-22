# Fmacias.TplQueue.Cache.Abstract

Reusable cache orchestration primitives for data-job graph dehydration/hydration and lease lifecycle handling.

See also:

- [TplQueue.Adapter root README](../../README.md)
- [TplQueue.Core cache section](../../../TplQueue.Core/README.md#cache-and-persistence)
- [Fmacias.TplQueue README](../Fmacias.TplQueue/README.md)
- [Fmacias.TplQueue.Cache.MemCache README](../Fmacias.TplQueue.Cache.MemCache/README.md)

## Contents
- `CacheAbstract`: base workflow implementation.
- Domain models (`CacheEntry`, `JobNodeDto`, runtime node metadata).
- Factories: `CacheEntryFactory`, `RuntimeNodeTypeResolverFactory`.
- Cache hydration helpers centered on `ITypeResolver` and `IUniversalDataSerializer`.

## Hydration flow

`CacheAbstract` does not infer payload CLR types from serialized payload content. It hydrates payload nodes in two explicit steps:

1. `JobNodeDto` persists `PayloadTypeName` from `payload.GetType().AssemblyQualifiedName`.
2. `CacheAbstract.DeserializePayload(...)` resolves that stored string through `ITypeResolver`.
3. The resolved `Type` is passed into `IUniversalDataSerializer.Deserialize(string, Type)`.

That split is the reason `RuntimeNodeTypeResolver` exists: the serializer knows how to materialize payload content for a known CLR type, while the resolver knows how to map persisted type names back to runtime types.

## Runtime node type resolution

`RuntimeNodeTypeResolver` is the default runtime-oriented implementation of `ITypeResolver`. Internally it uses `TypeDeserializer.TryResolveType(...)` against an `AppDomain`, defaulting to `AppDomain.CurrentDomain`.

Default runtime usage through the `Fmacias.TplQueue` facade:

```csharp
using Fmacias.TplQueue;
using Fmacias.TplQueue.Cache.MemCache;
using Fmacias.TplQueue.Contracts;

IUniversalDataSerializer jsonSerializer = api.SystemTextSerializerFactory().Serializer();
IUniversalDataSerializer xmlSerializer = api.XmlSerializerFactory().Serializer();

IMemCache cache = api.Cache<IMemCache>(
    MemCacheFactory.Create(),
    jsonSerializer);
```

If you need an explicit resolver, keep using the advanced overload:

```csharp
using Fmacias.TplQueue.Cache.Abstract.Factories;

ITypeResolver typeResolver = RuntimeNodeTypeResolverFactory.Create().Resolver();

IMemCache cache = api.Cache<IMemCache>(
    MemCacheFactory.Create(),
    jsonSerializer,
    typeResolver);
```

Custom `AppDomain` usage through the public factory:

```csharp
using Fmacias.TplQueue.Cache.Abstract.Factories;
using Fmacias.TplQueue.Contracts;

AppDomain customAppDomain = AppDomain.CurrentDomain; // replace with your application-selected AppDomain when applicable
IRuntimeNodeTypeResolver runtimeResolver =
    RuntimeNodeTypeResolverFactory.Create().Resolver(customAppDomain);
```

If you need a stricter policy than runtime assembly scanning, provide your own `ITypeResolver`:

```csharp
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using System;

public sealed class PluginDomainTypeResolver : ITypeResolver
{
    private readonly AppDomain _appDomain;

    public PluginDomainTypeResolver(AppDomain appDomain)
    {
        _appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
    }

    public Type Resolve(string payloadTypeName)
    {
        if (string.IsNullOrWhiteSpace(payloadTypeName))
            throw new ArgumentException("Payload type name cannot be null or whitespace.", nameof(payloadTypeName));

        if (!TypeDeserializer.TryResolveType(payloadTypeName, out var type, _appDomain))
            throw new InvalidOperationException($"Cannot resolve payload CLR type '{payloadTypeName}'.");

        return type;
    }
}
```

Use that resolver exactly like the default one:

```csharp
AppDomain customAppDomain = AppDomain.CurrentDomain; // replace with your application-selected AppDomain when applicable
ITypeResolver typeResolver = new PluginDomainTypeResolver(customAppDomain);
```

## Hydration into queue dispatch

After a cache hydrates a payload graph, dispatch the returned `IDataJobRoot` through the normal queue API:

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

Use `jsonSerializer` or `xmlSerializer` during cache creation depending on the storage format you want. The hydration flow remains the same because cache modules depend on `IUniversalDataSerializer`.

## Design note

Keeping `ITypeResolver` separate from `IUniversalDataSerializer` is the cleaner SRP boundary for this module:

- type identity lookup is a runtime concern
- payload materialization is a serialization concern
- cache hydration composes both concerns but does not collapse them into one interface

## Compatibility note

Some public contracts still expose JSON-oriented names such as `PayloadJson` and `IUniversalDataSerializer.Deserialize(string json, Type type)`.
Those names are retained for compatibility and should be read as serializer-specific payload content, not as a JSON-only storage rule.

## Runtime type resolution status

Current state:

- `RuntimeNodeTypeResolver` uses `AppDomain` because the current implementation is compatibility-first and simple to wire
- this remains adequate while payload CLR types live in the default host runtime or another explicitly selected AppDomain

Deferred work:

- when dynamic plugin loading becomes a first-class scenario in modern .NET, move the dedicated-loading design toward `AssemblyLoadContext`
- preserve the `ITypeResolver` boundary so the runtime loading mechanism can change without redesigning the serializer contract

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
