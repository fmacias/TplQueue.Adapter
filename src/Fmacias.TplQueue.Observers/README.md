# Fmacias.TplQueue.Observers

Observer support package for `TplQueue`.

## Table of contents

- [Summary](#summary)
- [Module purpose](#module-purpose)
- [How to use](#how-to-use)
- [Step-by-step example](#step-by-step-example)
- [Design justification](#design-justification)

## Summary

`Fmacias.TplQueue.Observers` contains reusable concrete observers and logging-oriented observer helpers.

## Module purpose

This package owns:

- `EventCatalog`
- `LogMessages`
- `LoggingObserver`
- `FileLoggingObserver`
- `ProfilingObserver`
- `ConsoleObserver`
- `DirectObserverDispatcher`
- logging subscription helpers

## How to use

Subscribe a logging observer to any `IQ` event stream.

```csharp
var observer = FileLoggingObserver.Create(logger, "main");
using var subscription = queue.Subscribe(observer);
```

## Step-by-step example

1. Create a queue from Core or from the top-level adapter.
2. Create a logger.
3. Create and subscribe a logging observer.

```csharp
var observer = LoggingObserver.Create(logger);
using var subscription = queue.Subscribe(observer);
```

## Design justification

Concrete observers do not belong in the thin top-level adapter package. Moving them here keeps logging concerns modular and lets `Fmacias.TplQueue` depend on this package instead of owning the implementations directly.
