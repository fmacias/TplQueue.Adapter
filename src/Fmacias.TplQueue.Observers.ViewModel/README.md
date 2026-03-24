# TplQueue.Observers.ViewModel

View-model and UI-dispatch observer support for `TplQueue`.

## Table of contents

- [Summary](#summary)
- [Module purpose](#module-purpose)
- [How to use](#how-to-use)
- [Step-by-step example](#step-by-step-example)
- [Design justification](#design-justification)

## Summary

This package contains UI-facing observer helpers such as `ViewModelObserver` and a simple direct dispatcher used for tests and non-UI scenarios.

## Module purpose

This package keeps view-model concerns separate from both Core runtime behavior and the thin top-level adapter facade.

## How to use

Create a dispatcher and pass it to `ViewModelObserver`.

```csharp
IObserverDispatcher dispatcher = DirectObserverDispatcher.Create();
var observer = ViewModelObserver.Create(dispatcher);
```

## Step-by-step example

1. Create an `IObserverDispatcher`.
2. Create `ViewModelObserver`.
3. Subscribe it to an `IQ`.

```csharp
var dispatcher = DirectObserverDispatcher.Create();
var observer = ViewModelObserver.Create(dispatcher);
using var subscription = queue.Subscribe(observer);
```

## Design justification

UI-thread marshaling is not a runtime orchestration concern and should not live in `TplQueue.Core`. It also should not stay inside the top-level adapter package, because it is a concrete integration. Keeping it here preserves separation of concerns.
