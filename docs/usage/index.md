# Usage

This section covers how the adapter package line is meant to be consumed.

## Main topics

- `Fmacias.TplQueue.API` as the adapter composition facade
- named queue creation through `IQFactoryAdapter`
- payload handler registration through `IApi.RegisterPayloadHandler(...)`
- concrete retry-policy factories
- built-in observers and observer dispatch helpers
- cache and serializer integration
- Microsoft dependency injection integration

## Module guides

Package-specific guidance remains in the module READMEs:

- [Fmacias.TplQueue](../../src/Fmacias.TplQueue/README.md)
- [Fmacias.TplQueue.Cache.Abstract](../../src/Fmacias.TplQueue.Cache.Abstract/README.md)
- [Fmacias.TplQueue.Cache.MemCache](../../src/Fmacias.TplQueue.Cache.MemCache/README.md)
- [Fmacias.TplQueue.Microsoft.DependencyInjection](../../src/Fmacias.TplQueue.Microsoft.DependencyInjection/README.md)
- [Fmacias.TplQueue.Observers](../../src/Fmacias.TplQueue.Observers/README.md)
- [Fmacias.TplQueue.RetryPolicies](../../src/Fmacias.TplQueue.RetryPolicies/README.md)
- [Fmacias.TplQueue.Serialization.SystemTextJson](../../src/Fmacias.TplQueue.Serialization.SystemTextJson/README.md)
- [Fmacias.TplQueue.Serialization.Xml](../../src/Fmacias.TplQueue.Serialization.Xml/README.md)

## Public package-consumption examples

The canonical runnable consumer examples live in [TplQueue.Usage](https://github.com/fmacias/TplQueue.Usage).

Representative adapter-side composition from the public samples looks like this:

```csharp
var api = API.Create(CoreApi.Create(), retryPolicies, dispatchers);
using IParallelQ queue = api.QFactory.Parallel(
    Guid.NewGuid(),
    "greetings-pipeline",
    maxParallelism: 1,
    queueLogger,
    retryPolicyFactory);
```

Use these runnable sample entry points:

- [PackageConsumptionSmokeConsole](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/PackageConsumptionSmokeConsole)
  Key focus: minimal package-consumption checks for queues, retry policy selection, observers, and payload-cache hydration.
- [QueueObserverConsole](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverConsole)
  Key focus: `API` facade composition, built-in observers, XML input, JSON output, and one queue reused by both a rooted graph and a standalone task.
- [QueueObserverSignalRDashboard](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)
  Key focus: `AddTplQueue(...)`, configuration-driven dispatchers, and DTO projection from observer events.

## Deeper detail

The previous long-form repository guide is preserved in [../reference.md](../reference.md).
