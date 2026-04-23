# TplQueue.Adapter

`TplQueue.Adapter` contains the modular integration packages that complement [TplQueue.Core](https://github.com/fmacias/TplQueue.Core/blob/main/README.md). It provides the top-level `API` facade and concrete modules for retry-policy creation, observer integration, cache implementations, serialization, and dependency-injection integration.

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
- [Strong-name signing](#strong-name-signing)
- [License](#license)

## Relationship to TplQueue.Core

[`TplQueue.Core`](https://github.com/fmacias/TplQueue.Core/blob/main/README.md) owns the runtime execution model:

- `Job` and `JobRoot` graphs
- `IQ`, `IParallelQ`, `IFifoQ`, and `ICacheQ`
- queue scheduling and bounded concurrency
- lifecycle events through `IObservable<IJobEvent>`
- retry-policy integration points

`TplQueue.Adapter` builds on top of that runtime with concrete modules and convenience wiring.

Use Core when you want the execution primitives directly.
Use Adapter when you want the integration layer that wires those primitives together with named configuration, serializer support, cache implementations, observers, or DI registration.

For the current graph-composition rules, especially the requirement that `IJobRoot` and `IDataJobRoot` remain the enqueueable terminal nodes, see [TplQueue.Core README](https://github.com/fmacias/TplQueue.Core/blob/main/README.md#job-roots).

## Repository modules

The repository currently contains these main modules:

- [Fmacias.TplQueue](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)
- [Fmacias.TplQueue.RetryPolicies](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.RetryPolicies/README.md)
- [Fmacias.TplQueue.Cache.Abstract](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Cache.Abstract/README.md)
- [Fmacias.TplQueue.Cache.MemCache](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Cache.MemCache/README.md)
- [Fmacias.TplQueue.Serialization.SystemTextJson](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Serialization.SystemTextJson/README.md)
- [Fmacias.TplQueue.Serialization.Xml](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Serialization.Xml/README.md)
- [Fmacias.TplQueue.Microsoft.DependencyInjection](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Microsoft.DependencyInjection/README.md)
- [Fmacias.TplQueue.Observers](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Observers/README.md)

At repository level, this README is the entry point. Individual modules may contain their own focused documentation.

## The `API` facade

[`Fmacias.TplQueue.API`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md) is the top-level facade for adapter-side composition.

It wraps:

- an `ICoreApi` instance
- an `IRetryPolicyAbstractFactory`
- an internal payload handler registry
- a named queue-options dictionary
- a named retry-policy-options dictionary

Create it like this:

```csharp
using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Core;

ICoreApi core = CoreApi.Create();

API api = API.Create(
    core,
    retryPolicyOptions,
    queueOptions);

api.RegisterPayloadHandler(
    MeasurementPayload.HandlerKey,
    new MeasurementPayloadHandler());
```

```csharp
public static API Create(
    ICoreApi api,
    IReadOnlyDictionary<string, IRetryPolicyOptions> retryPolicyOptions,
    IReadOnlyDictionary<string, IQOptions> queueOptions);

public IApi RegisterPayloadHandler(string payloadHandlerKey, IHandler handler);
public IApi RegisterPayloadHandler(string payloadHandlerKey, Func<IHandler> handlerFactory);
public IApi RegisterPayloadHandler(string payloadHandlerKey, Func<IPayload, CancellationToken, Task> handler);
public IApi RegisterPayloadHandler<TPayload>(string payloadHandlerKey, Func<TPayload, CancellationToken, Task> handler)
    where TPayload : IPayload;
public IApi RegisterPayloadHandlerPlugin(IPayloadHandlerPlugin plugin);

public IQFactoryAdapter QFactory
    => QFactoryAdapter.Create(_coreApi.QFactory, _retryPolicyAbstractFactory, _queueOptions, _retryPolicyOptions);

public IDataJobFactory DataJobFactory => _coreApi.DataJobFactory;
public IJobFactory JobFactory => _coreApi.JobFactory;
public ISystemTextJsonSerializerFactory SystemTextSerializerFactory();
public IXmlSerializerFactory XmlSerializerFactory();
public T Cache<T>(ICacheFactory<T> cacheFactory, IUniversalDataSerializer serializer)
    where T : IDataJobCache;
public T Cache<T>(ICacheFactory<T> cacheFactory, IUniversalDataSerializer serializer, ITypeResolver typeResolver)
    where T : IDataJobCache;
```

From the `IApi` facade you obtain:

- `IJobFactory`
- `IDataJobFactory`
- `IQFactoryAdapter`
- `IRetryPolicyAbstractFactory`
- `IObserverFactory`
- `ISystemTextJsonSerializerFactory`
- `IXmlSerializerFactory`
- cache creation helpers through `Cache<T>(...)`

This keeps the application entry point compact while leaving the underlying modules independently replaceable.

## Payload-aware jobs and handlers

Payload-aware runtime nodes are part of the public model through `IDataJob`, `IDataJob<T>`, `IDataJobRoot`, and `IDataJobRoot<T>`.

The execution-side payload model lives in Core, while Adapter provides the integration pieces commonly needed around it:

- payload handler registration through `IApi.RegisterPayloadHandler(...)`
- plugin-style registration through `IPayloadHandlerPlugin` and `IPayloadHandlerRegistry`
- cache abstractions for dehydration and hydration
- serializer implementations
- queue creation helpers that combine retry and queue options

This split is intentional: execution semantics stay in Core, while application-specific payload resolution and persistence remain modular.

The stable payload handler key is the payload `PayloadId`. Adapter caches persist that key and use it during hydration to map payload jobs back to their registered handler behavior through `IPayloadHandlers`.

Use versioned keys for any payload that can outlive the current deployment in a cache. A good default shape is `<domain>.<operation>/v<version>`, for example `measurements.persist/v1`. Do not reuse a key for incompatible payload shape or handler behavior; introduce a new key such as `measurements.persist/v2` and keep the old handler registered while old cached jobs may still hydrate.

Preferred direct registration:

```csharp
api.RegisterPayloadHandler(
    MeasurementPayload.HandlerKey,
    new MeasurementPayloadHandler());

api.RegisterPayloadHandler<MeasurementPayload>(
    MeasurementPayload.HandlerKey,
    (payload, ct) =>
    {
        return Task.CompletedTask;
    });
```

Plugin-style registration is useful when a package or module contributes several handlers at once:

```csharp
public sealed class MeasurementPayloadPlugin : IPayloadHandlerPlugin
{
    public void Register(IPayloadHandlerRegistry registry)
    {
        registry.Register(
            payloadHandlerKey: "measurements.persist/v1",
            handlerFactory: () => new MeasurementPayloadHandler());
    }
}

public sealed class MeasurementPayloadHandler : IHandler
{
    public Task HandleAsync(IPayload payload, CancellationToken ct)
    {
        var typed = (MeasurementPayload)payload;
        return Task.CompletedTask;
    }
}
```

Payload-aware composition follows the same rule as plain Core graphs: create the payload jobs first, then terminate the graph with an `IDataJobRoot`. Use `payloadRoot.After(payloadJob)` or `payloadJob.Then(payloadRoot)`, not the reverse direction.

## Payload handler contract status

Current state:

- register payload handlers through `IApi.RegisterPayloadHandler(...)`
- use `IPayload.PayloadId` as the stable persisted handler key
- version persisted handler keys when payload shape or handler behavior changes incompatibly
- resolve hydrated payload jobs through `IPayloadHandlers`

Deferred work:

- add optional higher-level plugin discovery helpers once the key-based contract is fully adopted
- keep direct registration on `IApi` as the public composition path

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

Adapter contains the concrete retry-policy modules and factories used by application code. See also [Fmacias.TplQueue.RetryPolicies](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.RetryPolicies/README.md) and the [Core retry overview](https://github.com/fmacias/TplQueue.Core/blob/main/README.md#retry-policies).

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

For the abstract-factory path, missing names fall back to `NoRetryPolicy`. The typed factory overloads keep the behavior of the specific factory instance you provide.

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
using Fmacias.TplQueue.Defaults;
using Fmacias.TplQueue.RetryPolicies;

LinearBackoffFactory linearFactory = LinearBackoffFactory.Create();
ExponentialBackoffFactory exponentialFactory = ExponentialBackoffFactory.Create();

ILinearBackoff defaultLinear = api.RetryPolicy(linearFactory);
ILinearBackoff namedLinear = api.RetryPolicy(linearFactory, "linear-default");

IExponentialBackoff exponentialByOptions = api.RetryPolicy(
    exponentialFactory,
    RetryPolicyOptions.Create(baseDelayMs: 250, maxRetries: 4, factor: 2d));

IExponentialBackoff explicitExponential = api.RetryPolicy(
    exponentialFactory,
    maxRetries: 4,
    delayMs: 250,
    factor: 2d);
```

The concrete retry-policy factories are intentionally public in [Fmacias.TplQueue.RetryPolicies](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.RetryPolicies/README.md), and `Create()` returns the concrete factory instance itself. Use them directly when you want low-level control, or pass them to `API.RetryPolicy(...)` when you want centralized adapter composition.

Or queue creation driven by named options:

```csharp
ILogger<IParallelQ> logger = loggerFactory.CreateLogger<IParallelQ>();
IParallelQ queue = api.QFactory.Parallel("main", logger);
```

This allows queue configuration to stay externalized while the queue runtime itself remains in Core.

## Observers

Core exposes events through `IObservable<IJobEvent>`. Adapter provides the [Fmacias.TplQueue.Observers](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Observers/README.md) package for built-in observers, default dispatcher creation, and consumer-side observer integration.

Relevant modules and types include:

- `IConsoleObserver`
- `ILoggingObserver`
- `IFileLoggingObserver`
- `IProfilingObserver`
- `IObserverDispatcher`
- `IObserverFactory`

Example factory usage:

```csharp
IObserverFactory observers = api.ObserverFactory();
IConsoleObserver consoleObserver = observers.CreateConsoleObserver();
ILoggingObserver loggingObserver = observers.CreateLoggingObserver(
    loggerFactory.CreateLogger<ILoggingObserver>());
IFileLoggingObserver fileObserver = observers.CreateFileLoggingObserver(
    loggerFactory.CreateLogger("TplQueue.Main"),
    queueName: "main");

using IDisposable consoleSubscription = queue.Subscribe(consoleObserver);
using IDisposable logSubscription = queue.Subscribe(loggingObserver);
using IDisposable fileSubscription = queue.Subscribe(fileObserver);
```

The concrete built-in observers live inside the observer package and are intentionally internal. Use `IObserverFactory` when you want a provided observer, and implement `IObserver<IJobEvent>` in the consumer application when the integration belongs to the application itself.

That is the important extensibility point for real systems: an existing WPF, WinForms, or ASP.NET application can keep its current UI and workflow while a custom observer forwards `IJobEvent` data to a modern dashboard, SignalR hub, metrics pipeline, or logging system.

UI integrations can implement `IObserverDispatcher` to marshal callbacks onto the correct platform context.

Typical mappings are:

- WPF: `Dispatcher.Invoke`
- WinForms: `Control.BeginInvoke`
- WinUI: `DispatcherQueue.TryEnqueue`
- MAUI: `MainThread.BeginInvokeOnMainThread`
- Blazor: component `InvokeAsync`

The default dispatcher returned by `CreateObserverDispatcher()` is especially useful for tests or non-UI scenarios where no special marshaling is needed.

For the full observer guide, including custom observer examples for dashboard and legacy UI integration, see [Fmacias.TplQueue.Observers README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Observers/README.md).

## Cache

Adapter contains the cache abstractions and concrete cache implementations used by `ICacheQ` and payload-aware recovery scenarios. See also the [Core cache section](https://github.com/fmacias/TplQueue.Core/blob/main/README.md#cache-and-persistence), [Fmacias.TplQueue.Cache.Abstract](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Cache.Abstract/README.md), and [Fmacias.TplQueue.Cache.MemCache](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Cache.MemCache/README.md).

Key modules:

- [Fmacias.TplQueue.Cache.Abstract](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Cache.Abstract/README.md)
- [Fmacias.TplQueue.Cache.MemCache](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Cache.MemCache/README.md)

Important contracts and models include:

- `IDataJobCache`
- `ICacheFactory<T>`
- `ICacheEntry`
- `IJobGraphDto`
- `IJobNodeDto`
- `ITypeResolver`
- `IRuntimeNodeTypeResolver`

Default cache creation through the adapter facade:

```csharp
using Fmacias.TplQueue.Cache.MemCache;

IUniversalDataSerializer serializer = api.SystemTextSerializerFactory().Serializer();

var cache = api.Cache(
    MemCacheFactory.Create(),
    serializer);
```

[Fmacias.TplQueue.Cache.MemCache](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Cache.MemCache/README.md) is the in-memory implementation. It is useful for tests, development, and lightweight scenarios. More persistent cache providers can be added behind the same abstractions.

During hydration the cache first resolves `PayloadTypeName` through `ITypeResolver`, then passes the resulting CLR `Type` into `IUniversalDataSerializer.Deserialize(string, Type)`. By default, `Fmacias.TplQueue.API` creates that resolver internally for `api.Cache(..., serializer)`. Keep the explicit overload when you need a dedicated `AppDomain` or a custom whitelist-based resolution policy.

Advanced explicit resolver example:

```csharp
using Fmacias.TplQueue.Cache.Abstract.Factories;
using Fmacias.TplQueue.Cache.MemCache;

IUniversalDataSerializer serializer = api.SystemTextSerializerFactory().Serializer();
ITypeResolver typeResolver = RuntimeNodeTypeResolverFactory.Create().Resolver();

var cache = api.Cache(
    MemCacheFactory.Create(),
    serializer,
    typeResolver);
```

### Cache to queue dispatch

This is the end-to-end adapter flow from serializer creation, through cache hydration, into queue dispatch:

```csharp
public sealed class MeasurementPayload : IPayload
{
    public const string HandlerKey = "measurements.persist/v1";

    public string SensorId { get; set; } = string.Empty;
    public double Value { get; set; }
    public string PayloadId => HandlerKey;
    public DateTime CollectionTime => DateTime.UtcNow;
}

public sealed class MeasurementPayloadHandler : IHandler
{
    public Task HandleAsync(IPayload payload, CancellationToken ct)
    {
        var measurement = (MeasurementPayload)payload;
        return Task.CompletedTask;
    }
}

IHandler handler = new MeasurementPayloadHandler();

api.RegisterPayloadHandler(MeasurementPayload.HandlerKey, handler);

IUniversalDataSerializer serializer = api.SystemTextSerializerFactory().Serializer();
IMemCache cache = api.Cache<IMemCache>(
    MemCacheFactory.Create(),
    serializer);

IDataJobRoot<MeasurementPayload> root = api.DataJobFactory.DataJobRoot(
    new MeasurementPayload { SensorId = "S-01", Value = 12.5 },
    handler,
    name: "PersistMeasurement");

cache.Dehydrate(root, isFifo: false);

ILogger<IParallelQ> queueLogger = loggerFactory.CreateLogger<IParallelQ>();

if (cache.TryHydrateNextJob(out IDataJobRoot hydratedRoot, out ICacheEntry lease))
{
    IParallelQ queue = api.QFactory.Parallel("main", queueLogger);

    queue.Enqueue(hydratedRoot, CancellationToken.None);
    queue.Start();

    await hydratedRoot.WaitUntilFinishedAsync();
}
```

Use `api.XmlSerializerFactory().Serializer()` in the same flow when XML payload storage is required.

### Cache type-resolution status

Current state:

- the default cache type-resolution path is `RuntimeNodeTypeResolver`, which is AppDomain-based
- that path is acceptable for compatibility and simple runtime probing

Deferred work:

- if dynamic plugin folders or dedicated runtime loading boundaries become a real requirement, prefer `AssemblyLoadContext` in modern .NET
- treat `AppDomain` as a .NET Framework-era mechanism and consider the current AppDomain-based resolver a transitional compatibility layer
- keep the migration at the `ITypeResolver` boundary rather than folding runtime loading concerns into `IUniversalDataSerializer`

## Serialization

Current serialization support is provided by `Fmacias.TplQueue.Serialization.SystemTextJson` and `Fmacias.TplQueue.Serialization.Xml`.

Important types include:

- `SystemTextJsonSerializerFactory`
- `SystemTextJsonUniversalSerializer`
- `XmlSerializerFactory`
- `XmlUniversalSerializer`

These components are typically used together with:

- payload-aware jobs
- cache dehydration and hydration
- runtime node reconstruction

The serializer concern stays outside Core so that the execution runtime does not become coupled to one concrete serialization technology. Type-name resolution remains a separate cache-hydration concern through `ITypeResolver`; it is not embedded into `IUniversalDataSerializer`.

Create serializers through the facade:

```csharp
IUniversalDataSerializer jsonSerializer =
    api.SystemTextSerializerFactory().Serializer();

IUniversalDataSerializer xmlSerializer =
    api.XmlSerializerFactory().Serializer();
```

XML serializer surface:

- cache creation and hydration continue to depend on `IUniversalDataSerializer`
- XML support uses `IXmlSerializerFactory`
- XML serializers implement `IXmlUniversalSerializer : IUniversalDataSerializer`
- the facade exposes `IApi.XmlSerializerFactory()` beside the existing JSON factory
- the concrete XML module is `Fmacias.TplQueue.Serialization.Xml`
- no serializer plugin discovery, serializer registry, or external serializer dependency is part of this scope

Existing JSON-oriented public names remain compatibility concerns. The legacy `SystemTexSerializerFactory()` typo is still available for compatibility; new code should use `SystemTextSerializerFactory()`. Persisted members such as `PayloadJson` should not be renamed as part of XML support. Treat those names as compatibility aliases for serializer-specific payload content.

## Dependency injection

[`Fmacias.TplQueue.Microsoft.DependencyInjection`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Microsoft.DependencyInjection/README.md) provides integration with `Microsoft.Extensions.DependencyInjection`.

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

API api = API.Create(
    core,
    retryPolicyOptions,
    queueOptions);

ILogger<IParallelQ> queueLogger = loggerFactory.CreateLogger<IParallelQ>();
IParallelQ queue = api.QFactory.Parallel("main", queueLogger);

IJob validate = api.JobFactory.Job(
    async ct => await Task.CompletedTask,
    name: "Validate");

IJobRoot root = api.JobFactory.JobRoot(
    async ct => await Task.CompletedTask,
    name: "Root");

root.After(validate);
queue.Enqueue(root, CancellationToken.None);
```

For execution semantics and queue behavior details, see [TplQueue.Core](https://github.com/fmacias/TplQueue.Core/blob/main/README.md).

## Strong-name signing

Normal source builds are unsigned. Official release packages are strong-named only when the pack script receives an external private key path and the matching full public key.

```powershell
.\pack-local.ps1 `
  -Version 0.1.0-preview.1 `
  -StrongNameKeyFile C:\secure\keys\Fmacias.TplQueue.official.snk `
  -StrongNamePublicKey <public-key>
```

The private `.snk` file is never stored in this repository. Adapter component projects append the public key to `InternalsVisibleTo` declarations only for official signed builds. For key creation, public-key extraction, and verification, see `..\WorkspaceTplQueue\docs\strong-name-signing.md`.

Component READMEs describe package-specific usage. Repository-wide packaging and signing rules live in this README.

## License

`TplQueue.Adapter` is distributed under the MIT license.

It is designed to complement [TplQueue.Core](https://github.com/fmacias/TplQueue.Core/blob/main/README.md), which is distributed separately under EULA terms.

# Roadmap

Completed recently:

1. `Fmacias.TplQueue.API` now owns a default runtime `ITypeResolver` for cache creation and exposes both `api.Cache(cacheFactory, serializer)` and `api.Cache(cacheFactory, serializer, typeResolver)`.

2. The cache documentation now presents the facade-owned resolver path as the default and keeps the explicit `ITypeResolver` overload for advanced scenarios.

3. README links were expanded across [TplQueue.Core](https://github.com/fmacias/TplQueue.Core/blob/main/README.md), this repository root, and the related adapter submodules so the navigation is bidirectional.
