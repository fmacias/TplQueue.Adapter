# TplQueue

This documentation tree is the public entry point for the whole TplQueue ecosystem.

It brings together:

- `TplQueue.Core` runtime concepts that consumers need in order to build queues and job graphs
- `TplQueue.Adapter` integration guidance for packages, dependency injection, caching, observers, serialization, and named queue creation
- `TplQueue.Usage` sample references for runnable package-consumption scenarios

Core usability documentation is integrated directly into the sections below. License and legal boundaries stay under [License](license/index.md).

## Start here

- [Getting Started](getting-started/index.md)
- [Core Concepts](core-concepts/index.md)
- [Queues](queues/index.md)
- [Architecture](architecture/index.md)
- [Operations](operations/index.md)
- [Development](development/index.md)
- [Reference](reference/index.md)
- [License](license/index.md)

## What TplQueue provides

- `IJob` and `IJobRoot` graph composition
- `IDataJob` and `IDataJobRoot` payload-aware execution
- `IParallelQ`, `IFifoQ`, and `ICacheQ` dispatch models
- queue-level and root-level retry-policy selection
- observer-based runtime visibility
- adapter-side support for dependency injection, serialization, cache modules, and named queue configuration

## Public package surfaces

- `Fmacias.TplQueue.Core`
- `Fmacias.TplQueue`
- `Fmacias.TplQueue.Microsoft.DependencyInjection`
- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Serialization.SystemTextJson`
- `Fmacias.TplQueue.Observers`

## Compatibility note

Older public documentation used `usage/` and `reference.md` as the main entry points. Those paths remain available as compatibility pages, but the primary source of truth is now the section structure shown above.
