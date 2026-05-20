# Fmacias.TplQueue.Serialization.SystemTextJson

`Fmacias.TplQueue.Serialization.SystemTextJson` provides the default `System.Text.Json`-based serializer implementation for TplQueue payload persistence, snapshot materialization, and cache hydration scenarios.

See also:

- [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md)
- [TplQueue.Adapter serialization section](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md#serialization)
- [TplQueue.Core cache section](https://github.com/fmacias/TplQueue.Core/blob/main/README.md#cache-and-persistence)
- [TplQueue.Usage QueueObserverSignalRDashboard sample](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)
- [Fmacias.TplQueue README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)

Repository-wide packaging and strong-name signing rules are documented in the [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md#strong-name-signing).

## Install

```bash
dotnet add package Fmacias.TplQueue.Serialization.SystemTextJson --version 0.1.0-preview.1
```

## When to use this package

Choose this package when:

- your application already standardizes on `System.Text.Json`
- cached payload jobs should be persisted in JSON form
- you want the default JSON serializer implementation without taking the XML serializer module

## Factory-first usage

Use the serializer through the public factory exposed by the adapter facade:

```csharp
using Fmacias.TplQueue.Contracts;

IUniversalDataSerializer serializer =
    api.SystemTextSerializerFactory().Serializer();
```

The same serializer can be passed into cache creation:

```csharp
IMemCache cache = api.Cache<IMemCache>(
    MemCacheFactory.Create(),
    serializer);
```

## Compatibility note

Some persisted members still expose JSON-oriented names such as `PayloadJson`. Those names are retained for compatibility and should be read as serializer-specific payload content, not as a restriction on the cache flow itself.

## Repository build notes

Run from `TplQueue.Adapter` root:

```powershell
dotnet build .\src\Fmacias.TplQueue.Serialization.SystemTextJson\Fmacias.TplQueue.Serialization.SystemTextJson.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1
```
