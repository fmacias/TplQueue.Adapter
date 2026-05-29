# Fmacias.TplQueue.Serialization.Xml

`Fmacias.TplQueue.Serialization.Xml` provides the XML-based serializer implementation for TplQueue payload persistence, snapshot materialization, and cache hydration scenarios.

See also:

- [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md)
- [TplQueue serialization modules guide](https://fmacias.github.io/tplqueue/development/serialization-modules/)
- [TplQueue cache-backed recovery guide](https://fmacias.github.io/tplqueue/operations/cache-backed-recovery/)
- [TplQueue.Usage QueueObserverSignalRDashboard sample](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)
- [Fmacias.TplQueue README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)

Repository-wide packaging and release operations are documented in the [TplQueue public operations guide](https://fmacias.github.io/tplqueue/operations/).

## Install

```bash
dotnet add package Fmacias.TplQueue.Serialization.Xml --version 0.1.0-preview.1
```

## Canonical sample

The public console sample uses the XML serializer for the input document like this:

```csharp
IUniversalDataSerializer serializer =
    api.XmlSerializerFactory().Serializer();
```

Full runnable solution:

- [QueueObserverConsole](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverConsole)

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

## Repository operations

Repository build, test, coverage, packaging, and release steps are documented in the [TplQueue public operations guide](https://fmacias.github.io/tplqueue/operations/).
