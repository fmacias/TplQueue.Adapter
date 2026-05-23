# TplQueue.Adapter docs

This folder contains the repository-level tutorial and reference material for `TplQueue.Adapter`.

Use this page as the stable entry point for future MkDocs import.

This `docs/de/` tree is the source of truth mirrored into the public `TplQueue` section on `fmacias.github.io`.

## Tree

- [Getting Started](usage/index.md)
- [Architecture](architecture/index.md)
- [Development](development/index.md)
- [Operations](operations/index.md)
- [Reference hub](reference.md)

## Scope

`TplQueue.Adapter` owns the integration layer that sits on top of `TplQueue.Core`.

Start here when you need to understand:

- the `API` facade and adapter-side factories
- named queue creation and retry-policy resolution
- cache, serialization, observer, and DI integration
- how to wire TplQueue into a .NET or ASP.NET application
- how the adapter packages are built and released
