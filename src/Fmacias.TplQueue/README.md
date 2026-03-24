# Fmacias.TplQueue

Thin adapter facade over `TplQueue.Core` and the modular integration packages.

## Table of contents

- [Summary](#summary)
- [Module purpose](#module-purpose)
- [How to use](#how-to-use)
- [Step-by-step example](#step-by-step-example)
- [Design justification](#design-justification)

## Summary

`Fmacias.TplQueue` is no longer the home of the queue or job runtime. It composes:

- `TplQueue.Core` for execution/runtime behavior
- `Fmacias.TplQueue.RetryPolicies` for concrete retry creation
- `Fmacias.TplQueue.Serialization.SystemTextJson` for serializer creation
- `Fmacias.TplQueue.Log` and `Fmacias.TplQueue.Observers.ViewModel` for concrete observers

## Module purpose

This package keeps the adapter-facing facade only:

- `API`
- thin factory adapters such as `CoreQFactoryAdapter`
- dependency wiring across Core and the modular packages

Concrete payload job execution, cache-backed queue runtime behavior, and queue adapters now live in `TplQueue.Core`.

## How to use

Create `API` from an `ICoreApi` plus the named queue and retry-policy dictionaries.

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

From there you can obtain:

- `IDataJobFactory`
- `ICoreQFactoryAdapter`
- `ICacheQFactory`
- `IObserverFactory`
- serializer and retry-policy factories

## Step-by-step example

1. Create the Core facade.
2. Define retry-policy descriptors and queue options.
3. Create `API`.
4. Resolve the queue factory adapter and payload-job factory from `API`.

```csharp
ICoreApi core = CoreApi.Create();

IApi api = API.Create(core, retryPolicyOptions, queueOptions);

IDataJobFactory dataJobs = api.DataJobFactory(payloadHandlerResolver);
IParallelQ queue = api.CoreQFactories.Value.Parallel("main", logger);
```

## Design justification

This package stays thin on purpose:

- runtime orchestration belongs in `TplQueue.Core`
- concrete integrations belong in focused packages
- the top-level adapter should compose dependencies, not re-own them

That split keeps the public facade stable while reducing coupling between runtime behavior, logging, UI observers, cache providers, serializers, and retry-policy implementations.
