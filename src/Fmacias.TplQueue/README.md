# Fmacias.TplQueue

Thin adapter facade over `TplQueue.Core` and the modular integration packages.

## Table of contents

- [Summary](#summary)
- [Module purpose](#module-purpose)
- [Creating the facade](#creating-the-facade)
- [Creating retry policies](#creating-retry-policies)
- [Creating queues and cache helpers](#creating-queues-and-cache-helpers)
- [Design justification](#design-justification)

## Summary

`Fmacias.TplQueue` composes the Core orchestration engine with the concrete Adapter modules used by application code:

- `TplQueue.Core` for queue execution and job graph orchestration
- `Fmacias.TplQueue.RetryPolicies` for concrete retry-policy factories
- `Fmacias.TplQueue.Serialization.SystemTextJson` for serializer creation
- `Fmacias.TplQueue.Log` and `Fmacias.TplQueue.Observers.ViewModel` for concrete observers

## Module purpose

This package exposes the adapter-facing entry points:

- `API`
- `IQFactoryAdapter`
- cache creation helpers through `Cache<T>(...)`
- retry-policy creation helpers that wrap `IRetryPolicyFactory<TPolicy>` and the built-in backoff factories

Concrete queue execution, job graphs, and payload-aware runtime semantics still belong to `TplQueue.Core`.

## Creating the facade

Create `API` from an `ICoreApi` and the named retry-policy and queue option dictionaries. When payload-aware cache hydration is required, compose the registrations in a `PayloadHandlersBuilder` and pass that builder into `API.Create(...)`:

```csharp
using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Core;

ICoreApi core = CoreApi.Create();

API api = API.Create(
    core,
    retryPolicyOptions,
    queueOptions);
```

`API.Create(...)` returns the concrete `API` instance. You can still reference it through `IApi` when you only want the abstraction.

From the facade you obtain:

- `IJobFactory`
- `IDataJobFactory`
- `IQFactoryAdapter`
- `IRetryPolicyAbstractFactory`
- `IObserverFactory`
- `ISystemTextJsonSerializerFactory`

## Registering payload handlers

`API` owns the payload handler registry internally.
If your application needs payload-aware cache hydration or plugin-style handler composition, compose the registrations in the application layer with `PayloadHandlersBuilder` and pass that builder into `API.Create(...)`.
The stable persisted execution identity remains `IPayload.PayloadId`, and `PayloadHandlersBuilder.Build()` exposes `IPayloadHandlers` when a caller needs direct resolver access.

```csharp
var payloadHandlersBuilder = PayloadHandlersBuilder.Create()
    .RegisterPlugin(new MeasurementPayloadPlugin());

API api = API.Create(
    core,
    payloadHandlersBuilder,
    retryPolicyOptions,
    queueOptions);

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

For queue-level named resolution through `IRetryPolicyAbstractFactory`, missing names fall back to `NoRetryPolicy`. The typed `IRetryPolicyFactory<TPolicy>` overloads keep the behavior of the provided factory.

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
- `IUniversalDataSerializer` deserializes the payload JSON for that resolved CLR type

If your application needs a dedicated `AppDomain` or a whitelist-based resolution policy, replace the default runtime resolver with your own `ITypeResolver`.

## Design justification

This package stays thin on purpose:

- runtime orchestration belongs in `TplQueue.Core`
- concrete integration modules stay in focused Adapter packages
- the top-level facade centralizes composition without hiding the public factories that advanced callers may still use directly

That split keeps application entry points compact while avoiding unnecessary coupling between queue execution, retry-policy creation, serialization, cache support, and observer integration.

## Payload handler roadmap

Current step:

- prefer `PayloadHandlersBuilder` and `IPayloadHandlerPlugin`
- persist and resolve handlers through the stable string key carried by `IPayload.PayloadId`
- let `API` own the default internal payload handler registry

Next step:

- rely exclusively on plugin-style string keys during hydration
- keep plugin loading and handler composition outside the facade, in the builder/application layer
- decide whether payload handler registration should remain in the external `PayloadHandlersBuilder` or move into `API` in a later iteration
- if dedicated runtime loading becomes necessary for plugin payload types, prefer an `AssemblyLoadContext`-based resolver in modern .NET and treat the current AppDomain-based path as transitional compatibility behavior
