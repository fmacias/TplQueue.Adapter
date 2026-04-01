# TplQueue.Adapter

`TplQueue.Adapter` contains the modular integration packages that complement `TplQueue.Core`. It provides the top-level `API` facade and concrete modules for retry-policy creation, logging and UI observers, cache implementations, serialization, and dependency-injection integration.

`TplQueue.Core` remains the execution kernel. `TplQueue.Adapter` composes and extends that kernel for practical application scenarios.

## Table of contents

- [Relationship to TplQueue.Core](#relationship-to-tplqueuecore)
- [Repository modules](#repository-modules)
- [The `API` facade](#the-api-facade)
- [Payload-aware jobs and handlers](#payload-aware-jobs-and-handlers)
- [Queues and queue factory adapters](#queues-and-queue-factory-adapters)
- [Retry policies](#retry-policies)
- [Observers](#observers)
- [Cache](#cache)
- [Serialization](#serialization)
- [Dependency injection](#dependency-injection)
- [Minimal example](#minimal-example)
- [License](#license)

## Relationship to TplQueue.Core

`TplQueue.Core` owns the runtime execution model:

- `Job` and `JobRoot` graphs
- `IQ`, `IParallelQ`, `IFifoQ`, and `ICacheQ`
- queue scheduling and bounded concurrency
- lifecycle events through `IObservable<IJobEvent>`
- retry-policy integration points

`TplQueue.Adapter` builds on top of that runtime with concrete modules and convenience wiring.

Use Core when you want the execution primitives directly.
Use Adapter when you want the integration layer that wires those primitives together with named configuration, serializer support, cache implementations, logging observers, or DI registration.

## Repository modules

The repository currently contains these main modules:

- `Fmacias.TplQueue`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.Serialization.SystemTextJson`
- `Fmacias.TplQueue.Microsoft.DependencyInjection`
- `Fmacias.TplQueue.Observers.ViewModel`
- `Fmacias.TplQueue.Log`

At repository level, this README is the entry point. Individual modules may contain their own focused documentation.

## The `API` facade

`Fmacias.TplQueue.API` is the top-level facade for adapter-side composition.

It wraps:

- an `ICoreApi` instance
- an `IRetryPolicyAbstractFactory`
- a payload handler resolver
- a named queue-options dictionary
- a named retry-policy-options dictionary

Create it like this:

```csharp
using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Core;

ICoreApi core = CoreApi.Create();

IApi api = API.Create(
    core,
    payloadHandlerResolver,
    retryPolicyOptions,
    queueOptions);
```

> It explains the adapter’s real role very well—it does not replace Core, it composes it.

```csharp
public static IApi Create(
    ICoreApi api,
    IPayloadHandlerResolver payloadHandlerResolver,
    IReadOnlyDictionary<string, IRetryPolicyOptions> retryPolicyOptions,
    IReadOnlyDictionary<string, IQOptions> queueOptions)
{
    return new API(api, payloadHandlerResolver, queueOptions, retryPolicyOptions);
}

public IQFactoryAdapter QFactory
    => QFactoryAdapter.Create(_coreApi.QFactory, _retryPolicyAbstractFactory, _queueOptions, _retryPolicyOptions);

public IDataJobFactory DataJobFactory => _coreApi.DataJobFactory;
public IJobFactory JobFactory => _coreApi.JobFactory;
```

From `IApi` you obtain:

- `IJobFactory`
- `IDataJobFactory`
- `IQFactoryAdapter`
- `IRetryPolicyAbstractFactory`
- `IObserverFactory`
- `ISystemTextJsonSerializerFactory`
- cache creation helpers through `Cache<T>(...)`

This keeps the application entry point compact while leaving the underlying modules independently replaceable.

## Payload-aware jobs and handlers

Payload-aware runtime nodes are part of the public model through `IDataJob`, `IDataJob<T>`, `IDataJobRoot`, and `IDataJobRoot<T>`.

The execution-side payload model lives in Core, while Adapter provides the integration pieces commonly needed around it:

- payload handler resolution through `IPayloadHandlerResolver`
- cache abstractions for dehydration and hydration
- serializer implementations
- queue creation helpers that combine retry and queue options

This split is intentional: execution semantics stay in Core, while application-specific payload resolution and persistence remain modular.

## Queues and queue factory adapters

`IQFactoryAdapter` extends `IQFactory` with named queue creation.

Key capabilities:

- create a queue from an explicit `IQOptions`
- create a queue by logical name from registered dictionaries
- resolve queue-level retry policies from the adapter-side retry factory
- retrieve typed queues through `GetCoreQ<T>(...)`

Example:

```csharp
using Microsoft.Extensions.Logging;

ILogger<IParallelQ> logger = loggerFactory.CreateLogger<IParallelQ>();
IParallelQ queue = api.QFactory.GetCoreQ<IParallelQ>("main", logger);
```

The adapter does not re-implement queue execution. It delegates actual queue construction to the Core `IQFactory` and enriches creation with configuration-driven policy resolution.

> It shows exactly how Adapter enriches Core queue creation with named configuration and retry-policy resolution.

```csharp
public IParallelQ Parallel(
    IQOptions queueOptions,
    string name,
    ILogger logger)
{
    if (queueOptions == null) throw new ArgumentNullException(nameof(queueOptions));
    if (logger == null) throw new ArgumentNullException(nameof(logger));

    ValidateQueueOptions(queueOptions);

    var retryPolicyCreator =
        () => _retryPolicyFactory.PolicyByName(queueOptions.RetryPolicy, _retryPolicyOptions);

    return Parallel(queueOptions.Id, name, queueOptions.MaxParallelism, logger, retryPolicyCreator);
}
```

## Retry policies

Adapter contains the concrete retry-policy modules and factories used by application code.

Important types include:

- `IRetryPolicyAbstractFactory`
- `RetryPolicyAbstractFactory`
- `FactoryAbstract<TPolicy>`
- `NoRetryPolicy`
- `LinearBackoff`
- `ExponentialBackoff`
- `JitterUtil`

Supported creation styles include:

- creation by name from a retry-policy dictionary
- creation from descriptors or options
- creation of concrete retry types through dedicated factories
- plugin-style rehydration when a policy type can be instantiated dynamically

> It documents the most important retry abstraction behavior: named lookup with safe fallback, and descriptor-driven construction. 

```csharp
public IRetryPolicy CreateByName(string name, IReadOnlyDictionary<string, IRetryPolicyDescriptor> options)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Retry policy name cannot be null or empty.", nameof(name));

    if (options == null)
        throw new ArgumentNullException(nameof(options));

    if (!options.TryGetValue(name, out var retryPolicyDescriptor))
    {
        return NoRetryPolicy.Create();
    }

    return Create(retryPolicyDescriptor);
}

public IRetryPolicy Create(IRetryPolicyDescriptor descriptor)
{
    if (descriptor is null)
        throw new ArgumentNullException(nameof(descriptor));

    return CreateCustomFromDescriptor(descriptor);
}
```

Typical usage through the adapter facade:

```csharp
var linear = api.RetryPolicy(
    LinearBackoffFactory.Create(),
    "linear-default");
```

Or queue creation driven by named options:

```csharp
ILogger<IParallelQ> logger = loggerFactory.CreateLogger<IParallelQ>();
IParallelQ queue = api.QFactory.Parallel("main", logger);
```

This allows queue configuration to stay externalized while the queue runtime itself remains in Core.

## Observers

Core exposes events through `IObservable<IJobEvent>`. Adapter provides concrete observer implementations and dispatch helpers for common scenarios.

Relevant modules and types include:

- `ConsoleObserver`
- `LoggingObserver`
- `FileLoggingObserver`
- `ProfilingObserver`
- `ViewModelObserver`
- `IObserverDispatcher`
- `DirectObserverDispatcher`
- `IObserverFactory`

Example factory usage:

```csharp
IObserverFactory observers = api.ObserverFactory();
IConsoleObserver consoleObserver = observers.CreateConsoleObserver();
```

```csharp
public IConsoleObserver CreateConsoleObserver()
{
    return ConsoleObserver.Create();
}

public ILoggingObserver CreateLoggingObserver(ILogger<ILoggingObserver> logger)
{
    return LoggingObserver.Create(logger);
}

public IObserverDispatcher CreateObserverDispatcher()
{
    return DirectObserverDispatcher.Create();
}

public IViewModelObserver CreateViewModeObserver(IObserverDispatcher observerDispatcher)
{
    return ViewModelObserver.Create(observerDispatcher);
}
```
For UI integration, `ViewModelObserver` is paired with an `IObserverDispatcher` abstraction so the actual marshaling strategy can be adapted per UI stack.

Typical mappings are:

- WPF: `Dispatcher.Invoke`
- WinUI: `DispatcherQueue.TryEnqueue`
- MAUI: `MainThread.BeginInvokeOnMainThread`
- Blazor: component `InvokeAsync`

`DirectObserverDispatcher` is especially useful for tests or non-UI scenarios where no special marshaling is needed.

## Cache

Adapter contains the cache abstractions and concrete cache implementations used by `ICacheQ` and payload-aware recovery scenarios.

Key modules:

- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Cache.MemCache`

Important contracts and models include:

- `IDataJobCache`
- `ICacheFactory<T>`
- `ICacheEntry`
- `IJobGraphDto`
- `IJobNodeDto`
- `IRuntimeNodeTypeResolver`

Example cache creation through the adapter facade:

```csharp
var serializerFactory = api.SystemTexSerializerFactory();
var serializer = serializerFactory.Create();

var cache = api.Cache(
    memCacheFactory,
    serializer,
    typeResolver);
```

`Fmacias.TplQueue.Cache.MemCache` is the in-memory implementation. It is useful for tests, development, and lightweight scenarios. More persistent cache providers can be added behind the same abstractions.

## Serialization

Serialization support is provided by `Fmacias.TplQueue.Serialization.SystemTextJson`.

Important types include:

- `SystemTextJsonSerializerFactory`
- `SystemTextJsonUniversalSerializer`

These components are typically used together with:

- payload-aware jobs
- cache dehydration and hydration
- runtime node reconstruction

The serializer concern stays outside Core so that the execution runtime does not become coupled to one concrete serialization technology.

## Dependency injection

`Fmacias.TplQueue.Microsoft.DependencyInjection` provides integration with `Microsoft.Extensions.DependencyInjection`.

Main entry points:

- `ServiceCollectionExtensions.AddTplQueue(...)`
- `TplQueueOptionsBuilder`

Supported registration styles include:

- configuration-based registration through `IConfiguration`
- code-based registration through `TplQueueOptionsBuilder`
- registration from existing retry and queue dictionaries

Example:

```csharp
services.AddTplQueue(
    builder =>
    {
        builder.AddRetryPolicy("linear-default", retryPolicyOptions);
        builder.AddDispatcher("main", queueOptions);
    },
    api);
```

This module registers the facade and the adapter-side factories needed for application composition.

## Minimal example

```csharp
using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Core;
using Microsoft.Extensions.Logging;

ICoreApi core = CoreApi.Create();

IApi api = API.Create(
    core,
    payloadHandlerResolver,
    retryPolicyOptions,
    queueOptions);

ILogger<IParallelQ> queueLogger = loggerFactory.CreateLogger<IParallelQ>();
IParallelQ queue = api.QFactory.Parallel("main", queueLogger);

IJobRoot root = api.JobFactory.JobRoot(
    async ct => await Task.CompletedTask,
    name: "Root");

queue.Enqueue(root, CancellationToken.None);
```

For execution semantics and queue behavior details, see `TplQueue.Core`.

## License

`TplQueue.Adapter` is distributed under the MIT license.

It is designed to complement `TplQueue.Core`, which is distributed separately under EULA terms.
