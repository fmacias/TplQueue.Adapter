# Fmacias.TplQueue.RetryPolicies

Retry-policy implementations and factories used by [TplQueue.Adapter](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md).

See also:

- [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md)
- [TplQueue retry guide](https://fmacias.github.io/tplqueue/operations/retry-policies/)
- [TplQueue.Usage QueueObserverSignalRDashboard sample](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)
- [Fmacias.TplQueue README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)

Repository-wide packaging and release operations are documented in the [TplQueue public operations guide](https://fmacias.github.io/tplqueue/operations/).

Use this package when you want the built-in TplQueue retry-policy implementations and factories without taking the broader adapter facade package.

## Install

```bash
dotnet add package Fmacias.TplQueue.RetryPolicies --version 0.1.0-preview.1
```

## Canonical sample

The public console sample builds a queue-level retry policy like this:

```csharp
Func<IRetryPolicy> retryPolicyFactory = () => api.RetryPolicy(
    ExponentialBackoffFactory.Create(),
    maxRetries: 3,
    delayMs: 250,
    factor: 2d);
```

The SignalR dashboard sample complements that pattern with configuration-driven retry policy selection through named options.

Full runnable solutions:

- [QueueObserverConsole](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverConsole)
- [QueueObserverSignalRDashboard](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)

## Contents

- `RetryPolicyAbstractFactory` for named and options-based creation.
- `FactoryAbstract<TPolicy>` for typed retry-policy factories.
- `NoRetryPolicy`, `LinearBackoff`, and `ExponentialBackoff`.
- `LinearBackoffFactory` and `ExponentialBackoffFactory`.
- `JitterUtil` for delay randomization where applicable.

## Factory usage

The concrete factories are intentionally public and `Create()` returns the concrete factory instance itself:

```csharp
LinearBackoffFactory linearFactory = LinearBackoffFactory.Create();
ILinearBackoff linear = linearFactory.LinearBackoff(maxRetries: 3, delayMs: 100);

ExponentialBackoffFactory exponentialFactory = ExponentialBackoffFactory.Create();
IExponentialBackoff exponential = exponentialFactory.ExponentialBackof(maxRetries: 4, delayMs: 200, factor: 2d);
```

When using the top-level [Fmacias.TplQueue API facade](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md), the same factories can be passed to `api.RetryPolicy(...)` overloads for creation by default, by name, by `IRetryPolicyOptions`, or by explicit built-in arguments.

## Abstract factory usage

`RetryPolicyAbstractFactory` resolves built-in retry policy interfaces without exposing the internal implementation classes:

```csharp
IRetryPolicyAbstractFactory factory = RetryPolicyAbstractFactory.Create();

ILinearBackoff linear = factory.PolicyByName<ILinearBackoff>("linear-default", retryPolicyOptions);
IExponentialBackoff exponential = factory.GetPolicy<IExponentialBackoff>();
INoRetryPolicy noRetry = factory.GetPolicy<INoRetryPolicy>();
```

The non-generic `PolicyByName(...)` overload keeps the queue-level fallback behavior:

```csharp
IRetryPolicy policy = factory.PolicyByName("missing-policy", retryPolicyOptions);
```

When the name is missing, the non-generic overload returns `NoRetryPolicy`. The generic `PolicyByName<T>(...)` overload is a typed lookup and throws `KeyNotFoundException` when the name is missing, because a `NoRetryPolicy` fallback may not be assignable to the requested type.

Custom retry policies should be requested by concrete type:

```csharp
CustomRetryPolicy custom = factory.GetPolicy<CustomRetryPolicy>();
```

The custom policy must implement `IRetryPolicy` and expose a public parameterless constructor. Custom interfaces are not resolved automatically by `RetryPolicyAbstractFactory`; use a concrete custom type unless a registration mechanism is added later.

## Repository operations

Repository build, test, coverage, packaging, and release steps are documented in the [TplQueue public operations guide](https://fmacias.github.io/tplqueue/operations/).

## Refactor note

Retry-policy creation is documented around the current public contract names:

- `LinearBackoffFactory.LinearBackoff(...)`
- `ExponentialBackoffFactory.ExponentialBackof(...)`
- `API.RetryPolicy(...)` wrappers over `IRetryPolicyFactory<TPolicy>`
