# Install

Install the runtime and the integration packages that match your host.

## Minimum package set

For ASP.NET or any host that wants the DI helpers, install:

```bash
dotnet add package Fmacias.TplQueue.Core --version 0.1.0-preview.1
dotnet add package Fmacias.TplQueue.Microsoft.DependencyInjection --version 0.1.0-preview.1
```

Add the thin public facade when you want to create `API` explicitly in composition code:

```bash
dotnet add package Fmacias.TplQueue --version 0.1.0-preview.1
```

## Optional public modules

- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Serialization.SystemTextJson`
- `Fmacias.TplQueue.Observers`

Choose the optional modules only when your application needs cache-backed dispatch, built-in retry policies, serializer helpers, or observer integrations.

## Licensing note

`TplQueue.Adapter` modules are MIT-licensed. `TplQueue.Core` is publicly consumable as a package, but it is not MIT. Keep the legal and support boundary clear by reading:

- [Core License](../license/core-license.md)
- [MIT Adapter Modules](../license/mit-adapter-modules.md)
- [Production Use](../license/production-use.md)
