# Fmacias.TplQueue.Serialization.Xml

`Fmacias.TplQueue.Serialization.Xml` provides the XML-based serializer implementation for TplQueue payload persistence, snapshot materialization, and cache hydration scenarios.

See also:

- [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md)
- [TplQueue.Adapter serialization section](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md#serialization)
- [TplQueue.Core cache section](https://github.com/fmacias/TplQueue.Core/blob/main/README.md#cache-and-persistence)
- [TplQueue.Usage QueueObserverSignalRDashboard sample](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)
- [Fmacias.TplQueue README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)

Repository-wide packaging and strong-name signing rules are documented in the [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md#strong-name-signing).

## Install

```bash
dotnet add package Fmacias.TplQueue.Serialization.Xml --version 0.1.0-preview.1
```

## When to use this package

Choose this package when:

- your integration boundary requires XML payload storage
- existing systems already exchange or archive XML documents
- you want the XML serializer implementation while preserving the same `IUniversalDataSerializer` abstraction used by the cache flow

## Factory-first usage

Use the serializer through the public factory exposed by the adapter facade:

```csharp
using Fmacias.TplQueue.Contracts;

IUniversalDataSerializer serializer =
    api.XmlSerializerFactory().Serializer();
```

The same serializer can be passed into cache creation:

```csharp
IMemCache cache = api.Cache<IMemCache>(
    MemCacheFactory.Create(),
    serializer);
```

## Compatibility note

The shared cache and serializer contracts still retain some JSON-oriented member names for backward compatibility. Those names should be interpreted as serializer-specific payload storage members, not as a limitation on XML usage.

## Repository build notes

Run from `TplQueue.Adapter` root:

```powershell
dotnet build .\src\Fmacias.TplQueue.Serialization.Xml\Fmacias.TplQueue.Serialization.Xml.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1
```
