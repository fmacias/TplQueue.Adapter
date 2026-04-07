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

Create `API` from an `ICoreApi`, a payload handler resolver, and the named retry-policy and queue option dictionaries:

```csharp
using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Core;

ICoreApi core = CoreApi.Create();

API api = API.Create(
    core,
    payloadHandlerResolver,
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
var serializer = api.SystemTexSerializerFactory().Create();

var cache = api.Cache(
    cacheFactory,
    serializer,
    typeResolver);
```

## Design justification

This package stays thin on purpose:

- runtime orchestration belongs in `TplQueue.Core`
- concrete integration modules stay in focused Adapter packages
- the top-level facade centralizes composition without hiding the public factories that advanced callers may still use directly

That split keeps application entry points compact while avoiding unnecessary coupling between queue execution, retry-policy creation, serialization, cache support, and observer integration.
