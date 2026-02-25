# Fmacias.TplQueue.RetryPolicies

Retry policy implementations and factories used by TplQueue adapters and job runners.

## Contents
- `RetryPolicyGenericFactory` for creation by name and descriptor.
- `LinearBackoff` and `ExponentialBackoff` policy implementations.
- `LinearBackoffFactory` and `ExponentialBackoffFactory`.
- Descriptor rehydration support for persisted job graphs.

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
Legacy retry policy facade wiring was replaced by generic/type-safe factory composition aligned with current adapter APIs.