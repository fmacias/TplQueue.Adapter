# Fmacias.TplQueue

Thin adapter facade over `TplQueue.Core` and the modular integration packages.

## Table of contents

- [Summary](#summary)
- [Module purpose](#module-purpose)
- [Creating the facade](#creating-the-facade)
- [Creating retry policies](#creating-retry-policies)
- [Creating queues and cache helpers](#creating-queues-and-cache-helpers)
- [Creating observers](#creating-observers)
- [Design justification](#design-justification)

## Summary

`Fmacias.TplQueue` composes the Core orchestration engine with the concrete Adapter modules used by application code:

- `TplQueue.Core` for queue execution and job graph orchestration
- `Fmacias.TplQueue.RetryPolicies` for concrete retry-policy factories
- `Fmacias.TplQueue.Serialization.SystemTextJson` for serializer creation
- `Fmacias.TplQueue.Serialization.Xml` for XML serializer creation
- `Fmacias.TplQueue.Observers` for concrete observers

## Module purpose

This package exposes the adapter-facing entry points:

- `API`
- `IQFactoryAdapter`
- cache creation helpers through `Cache<T>(...)`
- retry-policy creation helpers that wrap `IRetryPolicyFactory<TPolicy>` and the built-in backoff factories

Concrete queue execution, job graphs, and payload-aware runtime semantics still belong to `TplQueue.Core`.

## Creating the facade

Create `API` from an `ICoreApi` and the named retry-policy and queue option dictionaries. When payload-aware cache hydration is required, register payload handlers directly on the API facade:

```csharp
using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Core;

ICoreApi core = CoreApi.Create();

API api = API.Create(
    core,
    retryPolicyOptions,
    queueOptions)
    .RegisterPayloadHandlerPlugin(new MeasurementPayloadPlugin());
```

`API.Create(...)` returns the concrete `API` instance. You can still reference it through `IApi` when you only want the abstraction.

From the facade you obtain:

- `IJobFactory`
- `IDataJobFactory`
- `IQFactoryAdapter`
- `IRetryPolicyAbstractFactory`
- `IObserverFactory`
- `ISystemTextJsonSerializerFactory`
- `IXmlSerializerFactory`

## Registering payload handlers

`API` owns the payload handler registry internally.
If your application needs payload-aware cache hydration or plugin-style handler composition, register handlers through `IApi.RegisterPayloadHandler(...)` or `IApi.RegisterPayloadHandlerPlugin(...)`.
The stable persisted execution identity remains `IPayload.PayloadId`, and cache hydration uses the API-owned internal handler registry.

```csharp
API api = API.Create(
    core,
    retryPolicyOptions,
    queueOptions)
    .RegisterPayloadHandlerPlugin(new MeasurementPayloadPlugin());

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
        var measurementPayload = (MeasurementPayload)payload;
        return Task.CompletedTask;
    }
}
```

## Creating retry policies

`API` wraps the `IRetryPolicyFactory<TPolicy>` contract so callers can create retry policies from the same facade used for queues, payload handlers, and cache helpers.

The typed factories remain intentionally public in `Fmacias.TplQueue.RetryPolicies`, and their `Create()` methods return the concrete factory instance itself. That means you can either use the factory directly or pass it through the facade.

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

For queue-level named resolution through `IRetryPolicyAbstractFactory`, missing names fall back to `NoRetryPolicy` when using the non-generic `PolicyByName(...)` overload. Generic lookup through `PolicyByName<T>(...)` and `GetPolicy<T>()` supports the built-in retry policy interfaces `INoRetryPolicy`, `ILinearBackoff`, and `IExponentialBackoff`. Custom retry policies should be requested by concrete type and must expose a public parameterless constructor.

```csharp
IRetryPolicyAbstractFactory abstractFactory = api.RetryPolicyAbstractFactory;

ILinearBackoff linearByName = abstractFactory.PolicyByName<ILinearBackoff>(
    "linear-default",
    api.RetryPolicyOptions);

IExponentialBackoff exponentialDefault = abstractFactory.GetPolicy<IExponentialBackoff>();
```

## Creating queues and cache helpers

Use `IQFactoryAdapter` when you want named queue creation backed by the adapter dictionaries:

```csharp
using Microsoft.Extensions.Logging;

ILogger<IParallelQ> logger = loggerFactory.CreateLogger<IParallelQ>();
IParallelQ queue = api.QFactory.Parallel("main", logger);
```

Use the same facade for payload-aware cache creation:

```csharp
using Fmacias.TplQueue.Cache.Abstract.Factories;

var serializer = api.SystemTexSerializerFactory().Serializer();
var typeResolver = RuntimeNodeTypeResolverFactory.Create().Resolver();

var cache = api.Cache(
    cacheFactory,
    serializer,
    typeResolver);
```

The cache helper keeps serializer and type-resolution concerns separate on purpose:

- `ITypeResolver` resolves the persisted payload CLR type name during hydration
- `IUniversalDataSerializer` deserializes the payload data for that resolved CLR type

If your application needs a dedicated `AppDomain` or a whitelist-based resolution policy, replace the default runtime resolver with your own `ITypeResolver`.

Serializer surface:

- JSON remains available through `SystemTexSerializerFactory()`
- XML support is available through `XmlSerializerFactory()`
- cache creation continues to accept `IUniversalDataSerializer` instead of a JSON- or XML-specific serializer contract
- XML support uses `IXmlSerializerFactory` and `IXmlUniversalSerializer : IUniversalDataSerializer`
- serializer plugin discovery and serializer registries are outside the current facade scope

Existing JSON-oriented public names are compatibility concerns. They should not be renamed as part of adding XML support.

## Creating observers

`API.ObserverFactory()` returns the factory owned by `Fmacias.TplQueue.Observers`. The facade exposes the observer package without owning the built-in observer implementations.

```csharp
IObserverFactory observers = api.ObserverFactory();

ILoggingObserver loggingObserver = observers.CreateLoggingObserver(
    loggerFactory.CreateLogger<ILoggingObserver>());
IConsoleObserver consoleObserver = observers.CreateConsoleObserver();

using IDisposable logSubscription = queue.Subscribe(loggingObserver);
using IDisposable consoleSubscription = queue.Subscribe(consoleObserver);
```

The built-in observer classes are internal to the observer package. Use the factory contracts for console, logging, file logging, profiling, and default dispatcher creation. Implement `IObserver<IJobEvent>` in the consumer application when you need to feed a WPF, WinForms, ASP.NET, SignalR, metrics, or dashboard integration.

For details, see the observer package README at `../Fmacias.TplQueue.Observers/README.md`.

## Design justification

This package stays thin on purpose:

- runtime orchestration belongs in `TplQueue.Core`
- concrete integration modules stay in focused Adapter packages
- the top-level facade centralizes composition without hiding the public factories that advanced callers may still use directly

That split keeps application entry points compact while avoiding unnecessary coupling between queue execution, retry-policy creation, serialization, cache support, and observer integration.

## Payload handler roadmap

Current step:

- prefer `IApi.RegisterPayloadHandler(...)` and `IApi.RegisterPayloadHandlerPlugin(...)`
- persist and resolve handlers through the stable string key carried by `IPayload.PayloadId`
- let `API` own the default internal payload handler registry

Next step:

- rely exclusively on plugin-style string keys during hydration
- keep plugin discovery outside the facade while direct handler registration remains on `IApi`
- if dedicated runtime loading becomes necessary for plugin payload types, prefer an `AssemblyLoadContext`-based resolver in modern .NET and treat the current AppDomain-based path as transitional compatibility behavior
