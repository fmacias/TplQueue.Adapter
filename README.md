# TplQueue.Adapter

TplQueue.Adapter contains the MIT-licensed extension modules that complement `TplQueue.Core`. The repository now centers on modular integrations and a thin adapter facade rather than owning the queue/job runtime itself.

## Table of contents

- [Relationship to TplQueue.Core](#relationship-to-tplqueuecore)
- [Current modules](#current-modules)
- [Thin facade](#thin-facade)
- [Queues and cache-oriented orchestration](#queues-and-cache-oriented-orchestration)
- [Retry policies](#retry-policies)
- [Observers](#observers)
- [Cache](#cache)
- [Serialization](#serialization)
- [DI integration](#di-integration)
- [Further documentation](#further-documentation)
- [License](#license)

## Relationship to TplQueue.Core

[`TplQueue.Core`](../TplQueue.Core/README.md) is the orchestration engine. It owns the execution model for `Job` graphs, queue dispatchers, retry hooks, and lifecycle events.

`TplQueue.Adapter` builds on top of that engine with concrete integrations and composition:

- named and descriptor-based retry policy creation
- cache abstractions and cache-backed orchestration helpers
- serialization support
- observer implementations and UI dispatch patterns
- dependency-injection registration helpers

## Current modules

The repository currently contains these main modules:

- `Fmacias.TplQueue`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.Serialization.SystemTextJson`
- `Fmacias.TplQueue.Microsoft.DependencyInjection`
- `Fmacias.TplQueue.Observers.ViewModel`
- `Fmacias.TplQueue.Log`

At repository level, this README is the entry point. Module-level READMEs provide more focused details where they already exist.

## Thin facade

`Fmacias.TplQueue` is now the thin facade package. It composes:

- `TplQueue.Core`
- retry-policy factories
- serializer factories
- cache providers
- observer packages

Payload-aware runtime types and cache-backed queue runtime behavior now live with the Core execution model instead of being re-owned by the top-level adapter.

`API` is the main adapter facade:

```csharp
using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Core;

ICoreApi core = CoreApi.Create();

IApi api = API.Create(
    core,
    retryPolicyOptions,
    queueOptions);
```

From that facade you can obtain:

- `IDataJobFactory` through `DataJobFactory(...)`
- `ICoreQFactoryAdapter` through `CoreQFactories`
- `ICacheQFactory` through `CacheQFactory`
- retry policies through `RetryPolicy<T>(...)`
- serializer factories and observer factories

## Queues and cache-oriented orchestration

`ICoreQFactoryAdapter` builds on `ICoreQFactory` and adds queue creation driven by registered `IQOptions`.

It supports patterns such as:

- create a queue by name from option dictionaries
- create a queue from explicit queue options
- reuse the Core queue contracts while resolving retry policies from Adapter descriptors

`CacheQ` now belongs to the Core-side runtime model, but it still composes Adapter-side abstractions such as `IDataJobCache`. It combines:

- an `IParallelQ`
- an `IDataJobCache`
- adapter-side queueing semantics for payload-aware job graphs

This split keeps execution semantics in Core while preserving cache implementations and serializer integrations as modular adapter concerns.

## Retry policies

Adapter contains the concrete retry modules used by Core integrations.

Current retry concepts and types include:

- `GenericFactory`
- creation by policy name
- creation by `IRetryPolicyDescriptor`
- `NoRetryPolicy`
- `LinearBackoff`
- `ExponentialBackoff`

The usual model is:

1. define retry policy descriptors in a dictionary
2. define queue options in a dictionary
3. create `API`
4. create queues through `CoreQFactories`
5. let Adapter resolve the concrete retry policy when the queue or root needs it

Example:

```csharp
IApi api = API.Create(core, retryPolicyOptions, queueOptions);

IParallelQ queue = api.CoreQFactories.Value.Parallel("main", logger);
```

This design keeps retry policy creation outside Core while preserving consistent queue behavior.

Related modules:

- [`Fmacias.TplQueue.RetryPolicies`](src/Fmacias.TplQueue.RetryPolicies/README.md)
- [`TplQueue.Core` retry overview](../TplQueue.Core/README.md#retry-policies)

## Observers

Core publishes lifecycle events through `IObservable<IJobEvent>`. Adapter adds practical observer implementations and UI-facing patterns.

Relevant observer components include:

- `ViewModelObserver`
- `IObserverDispatcher`
- `DirectObserverDispatcher`
- logging-oriented observers from `Fmacias.TplQueue.Log`

Observer use cases include:

- operational logging
- metrics and profiling
- view-model updates
- real-time dashboards
- SignalR or Rx forwarding

### UI observer integration

The current UI observer guidance was previously stored in `TplQueue.Core/ObersversIntegrationReadme.md`. Its useful content is summarized here and aligned to the current model.

The pattern is:

- `ViewModelObserver` consumes `IJobEvent`
- `IObserverDispatcher` abstracts marshaling to the UI thread or scheduler
- each UI platform provides its own dispatcher implementation

Typical dispatcher adapters:

- WPF: use `Dispatcher.Invoke`
- UWP: use `CoreDispatcher.RunAsync`
- MAUI: use `MainThread.BeginInvokeOnMainThread`
- WinUI: use `DispatcherQueue.TryEnqueue`
- Blazor: use `SynchronizationContext.Post` or component `InvokeAsync`

For testing, a direct dispatcher keeps the observer synchronous and predictable:

```csharp
public sealed class TestObserverDispatcher : IObserverDispatcher
{
    public void Invoke(Action action) => action();
}
```

Reactive and web scenarios are also natural fits:

- Rx pipelines can transform and route `IJobEvent` streams
- SignalR observers can push queue events to browser clients in real time

The key rule from Core still applies: observer delivery should not block orchestration.

Related modules:

- [`Fmacias.TplQueue.Observers.ViewModel`](src/Fmacias.TplQueue.Observers.ViewModel/README.md)
- [`TplQueue.Core` observer model](../TplQueue.Core/README.md#observers)

## Cache

Cache support lives in Adapter, not Core.

Relevant abstractions and modules include:

- `IDataJobCache`
- `IJobNodeDto`
- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Cache.MemCache`

These components support dehydration and hydration of payload-aware graphs, runtime node metadata, and interchangeable cache providers.

`Fmacias.TplQueue.Cache.MemCache` is the in-memory example provider. It is useful for testing and for lightweight scenarios where persistence outside process memory is not required.

Related modules:

- [`Fmacias.TplQueue.Cache.Abstract`](src/Fmacias.TplQueue.Cache.Abstract/README.md)
- [`Fmacias.TplQueue.Cache.MemCache`](src/Fmacias.TplQueue.Cache.MemCache/README.md)
- [`TplQueue.Core` cache and persistence overview](../TplQueue.Core/README.md#cache-and-persistence)

## Serialization

Serialization support is provided by `Fmacias.TplQueue.Serialization.SystemTextJson`.

The module supplies Adapter-side serializer implementations and factory support so payload-aware job graphs can be persisted or rehydrated without adding serialization concerns to Core.

Key entry point:

- `JsonSerializerFactory`

This is typically used together with cache and payload-handler components.

## DI integration

`Fmacias.TplQueue.Microsoft.DependencyInjection` provides integration with `Microsoft.Extensions.DependencyInjection`.

Current registration helpers include:

- `ServiceCollectionExtensions.AddTplQueue(...)`
- `TplQueueOptionsBuilder`

Supported registration styles include configuration-based and code-based registration for:

- `IApi`
- retry policy descriptors
- queue options
- related adapter services

Related module:

- [`Fmacias.TplQueue.Microsoft.DependencyInjection`](src/Fmacias.TplQueue.Microsoft.DependencyInjection/README.md)

## Further documentation

This root README is the repository entry point. Additional detail may exist in module READMEs under `src/`.

Useful entry points:

- [`TplQueue.Core` fundamentals](../TplQueue.Core/README.md)
- [`Fmacias.TplQueue`](src/Fmacias.TplQueue/README.md)
- [`Fmacias.TplQueue.RetryPolicies`](src/Fmacias.TplQueue.RetryPolicies/README.md)
- [`Fmacias.TplQueue.Observers.ViewModel`](src/Fmacias.TplQueue.Observers.ViewModel/README.md)
- [`Fmacias.TplQueue.Cache.Abstract`](src/Fmacias.TplQueue.Cache.Abstract/README.md)
- [`Fmacias.TplQueue.Cache.MemCache`](src/Fmacias.TplQueue.Cache.MemCache/README.md)
- [`Fmacias.TplQueue.Microsoft.DependencyInjection`](src/Fmacias.TplQueue.Microsoft.DependencyInjection/README.md)

## License

TplQueue.Adapter is distributed under the MIT license. It is designed to be used together with `TplQueue.Core`, which remains the orchestration engine of the ecosystem.
