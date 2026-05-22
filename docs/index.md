# TplQueue.Adapter docs

This folder contains the repository-level documentation for `TplQueue.Adapter`.

Use the tree below as the stable entry point for future MkDocs import.

## Tree

- [Usage](usage/index.md)
- [Architecture](architecture/index.md)
- [Development](development/index.md)
- [Operations](operations/index.md)
- [Full reference](reference.md)

## Scope

`TplQueue.Adapter` owns the integration layer that sits on top of `TplQueue.Core`.

It is the right repository when you need to understand:

- the `API` facade and adapter-side factories
- named queue creation and retry-policy resolution
- cache, serialization, observer, and DI integration
- how the adapter packages are built and released
