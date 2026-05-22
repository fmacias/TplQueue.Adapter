# Fmacias.TplQueue.Cache.Abstract

Reusable cache orchestration primitives for data-job graph dehydration/hydration and lease lifecycle handling.

See also:

- [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md)
- [TplQueue.Core cache section](https://github.com/fmacias/TplQueue.Core/blob/main/docs/reference.md#cache-and-persistence)
- [TplQueue.Usage QueueObserverSignalRDashboard sample](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)
- [Fmacias.TplQueue README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)
- [Fmacias.TplQueue.Cache.MemCache README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Cache.MemCache/README.md)

Repository-wide packaging and release operations are documented in the [TplQueue.Adapter operations guide](https://github.com/fmacias/TplQueue.Adapter/blob/main/docs/operations/index.md).

Use this package when you need the cache contracts and reusable hydration workflow without taking a dependency on the concrete in-memory cache implementation.

## Install

```bash
dotnet add package Fmacias.TplQueue.Cache.Abstract --version 0.1.0-preview.1
```

## Canonical sample

The public smoke and dashboard samples exercise the cache flow through a small sequence like this:

```csharp
cache.Dehydrate(root, isFifo: false);

if (cache.TryHydrateNextJob(out IDataJobRoot hydratedRoot, out ICacheEntry lease))
{
    queue.Enqueue(hydratedRoot, CancellationToken.None);
    await queue.Wait().ConfigureAwait(false);
}
```

Full runnable solutions:

- [PackageConsumptionSmokeConsole](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/PackageConsumptionSmokeConsole)
- [QueueObserverSignalRDashboard](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)

## Contents
- `CacheAbstract`: base workflow implementation.
- Domain models (`CacheEntry`, `JobNodeDto`, runtime node metadata).
- Factories: `CacheEntryFactory`, `RuntimeNodeTypeResolverFactory`.
- Cache hydration helpers centered on `ITypeResolver` and `IUniversalDataSerializer`.

## When to use this package

Choose `Fmacias.TplQueue.Cache.Abstract` when you are:

- implementing a custom cache provider for `IDataJobRoot` dehydration and hydration
- consuming cache-related contracts from another integration package
- standardizing payload-type resolution and serializer usage without committing to `MemCache`

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
    queue.ResumePolling();

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

## Repository operations

Repository build, test, coverage, packaging, and release steps are documented in the [TplQueue.Adapter operations guide](https://github.com/fmacias/TplQueue.Adapter/blob/main/docs/operations/index.md).
