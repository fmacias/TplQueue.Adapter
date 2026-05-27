# Architecture

This section explains how the public TplQueue documentation splits execution-kernel responsibilities from adapter-side integration responsibilities.

## Main topics

- [Runtime Model](runtime-model.md)
- [Core / Adapter Boundary](core-adapter-boundary.md)
- [Payload Handler Integration](payload-handler-integration.md)
- [Observer Event Publication](observer-event-publication.md)
- [Cache and Persistence Boundary](cache-persistence-boundary.md)
- [Diagrams](diagrams.md)

## Boundary with Core

`TplQueue.Core` owns:

- job graph execution
- queue dispatch semantics
- retry-policy selection points
- queue event publication

`TplQueue.Adapter` owns:

- adapter-side composition through `API`
- named queue and retry-policy dictionaries
- concrete retry-policy implementations
- built-in observers
- cache implementations and hydration helpers
- serializer implementations
- DI registration helpers

## Module structure

At repository level, the adapter line is split into focused packages instead of one large integration assembly.

That keeps:

- the public facade available through `Fmacias.TplQueue`
- optional integrations installable only when needed
- cache, observer, serializer, and DI concerns modular

## Payload and cache split

Execution-side payload nodes are part of Core through `IDataJob` and `IDataJobRoot`.

Adapter adds the infrastructure around those nodes:

- payload handler registration
- dehydration and hydration
- serializer selection
- type-resolution boundaries

This split keeps the runtime model small while allowing richer integration scenarios above it.
