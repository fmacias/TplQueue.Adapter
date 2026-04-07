# Fmacias.TplQueue.RetryPolicies

Retry-policy implementations and factories used by `TplQueue.Adapter`.

## Contents

- `RetryPolicyAbstractFactory` for named and descriptor-based creation.
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

When using the top-level `API` facade, the same factories can be passed to `api.RetryPolicy(...)` overloads for creation by default, by name, by `IRetryPolicyOptions`, or by explicit built-in arguments.

## Local pipeline

Run from `TplQueue.Adapter` root:

1. Build:
```powershell
dotnet build .\src\Fmacias.TplQueue.RetryPolicies\Fmacias.TplQueue.RetryPolicies.csproj
```

2. Test:
```powershell
dotnet test .\test\Fmacias.TplQueue.RetryPolicies.Unit.Test\Fmacias.TplQueue.RetryPolicies.Test.csproj
```

3. Pack in repo order:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1
```

## Refactor note

Retry-policy creation is documented around the current public contract names:

- `LinearBackoffFactory.LinearBackoff(...)`
- `ExponentialBackoffFactory.ExponentialBackof(...)`
- `API.RetryPolicy(...)` wrappers over `IRetryPolicyFactory<TPolicy>`
