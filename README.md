# TplQueue.Adapter

TplQueue.Adapter is the **MIT-licensed extension layer** for the TplQueue ecosystem. It complements **TplQueue.Core** (EULA) by providing opt-in modules such as:

- retry policy implementations,
- cache abstractions + providers,
- observers (logging + ViewModel/UI),
- serialization helpers (e.g., System.Text.Json),
- dependency injection helpers.

> **Core vs Adapter**
>
> - **TplQueue.Core**: execution kernel (jobs + queues + lifecycle events + retry integration points).
> - **TplQueue.Adapter**: concrete implementations and integrations built on top of Core’s abstractions.

---

## Table of contents

- [Repository layout](#repository-layout)
- [Getting started](#getting-started)
  - [Build, test, pack](#build-test-pack)
  - [Using Adapter packages with Core](#using-adapter-packages-with-core)
- [Retry policies](#retry-policies)
  - [How to: configure and use retry policies](#how-to-configure-and-use-retry-policies)
- [Observers](#observers)
  - [What observers are for](#what-observers-are-for)
  - [How to: subscribe and forward events](#how-to-subscribe-and-forward-events)
  - [UI observer integration](#ui-observer-integration)
- [Cache](#cache)
  - [How to: plug a cache provider into your persistence layer](#how-to-plug-a-cache-provider-into-your-persistence-layer)
- [Persistence and serialization](#persistence-and-serialization)
- [Links between Core and Adapter docs](#links-between-core-and-adapter-docs)
- [License](#license)

---

## Repository layout

Current module layout:

- `src/Fmacias.TplQueue`: API facade and adapter composition.
- `src/Fmacias.TplQueue.RetryPolicies`: retry policy implementations/factories.
- `src/Fmacias.TplQueue.Cache.Abstract`: reusable cache orchestration abstractions.
- `src/Fmacias.TplQueue.Cache.MemCache`: in-memory cache provider.
- `src/Fmacias.TplQueue.Serialization.SystemTextJson`: serializer implementation.
- `src/Fmacias.TplQueue.Microsoft.DependencyInjection`: DI registration extensions.
- `src/Fmacias.TplQueue.Observers.ViewModel` and `src/Fmacias.TplQueue.Log`: observer/log helper modules.

---

## Getting started

### Build, test, pack

Run commands from repository root (`TplQueue.Adapter`).

1) Build solution:

```powershell
dotnet build .\TplQueue.Adapter.sln
```

2) Run all tests:

```powershell
dotnet test .\TplQueue.Adapter.sln
```

3) Pack local packages in dependency order:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1
```

Expected output:
- `..\TplQueue.NugetLocal\*.nupkg`
- `..\TplQueue.NugetLocal\*.snupkg`

### Using Adapter packages with Core

In a consumer application, you typically reference Core + selected Adapter packages:

```powershell
dotnet add package Fmacias.TplQueue.Core
dotnet add package Fmacias.TplQueue.Abstractions

# Optional Adapter modules (choose what you need)
dotnet add package Fmacias.TplQueue.RetryPolicies
dotnet add package Fmacias.TplQueue.Log
dotnet add package Fmacias.TplQueue.Cache.Abstract
dotnet add package Fmacias.TplQueue.Cache.MemCache
dotnet add package Fmacias.TplQueue.Serialization.SystemTextJson
dotnet add package Fmacias.TplQueue.Microsoft.DependencyInjection
```

> Link placeholder: Core README `../TplQueue.Core/README.md`

---

## Retry policies

Adapter provides concrete retry policy implementations that plug into Core’s retry integration points.

### How to: configure and use retry policies

The standard wiring pattern is:

1. Build a named dictionary of retry policy options.
2. Create an `IRetryPolicyFactory` from those options.
3. Pass the factory into the queue factory exposed by `CoreApi`.
4. Create job roots that select a policy name/options (API differs per version).

```csharp
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Core;
using Fmaciasruano.TplQueue.RetryPolicies;

ICoreApi core = CoreApi.Create();

// 1) Configure named policies (shape depends on your options types)
IReadOnlyDictionary<string, RetryPolicyOptions> retryOptions =
    new Dictionary<string, RetryPolicyOptions>
    {
        ["default"] = new RetryPolicyOptions(/* ... */),
    };

// 2) Create the factory (implementation lives in Adapter)
IRetryPolicyFactory retryFactory = RetryPolicyFactory.Instance(retryOptions);

// 3) Create queue factory with retry support
ITaskDispatcherFactory dispatcherFactory = core.GetTaskDispatcherFactory(options, retryFactory);
```

> Link placeholder: Core section “Retry policies” `../TplQueue.Core/README.md#retry-policies`

---

## Observers

### What observers are for

Core queues publish job lifecycle events via `IObservable<...>`. Adapter provides ready-made observers for:

- logging and diagnostics,
- UI (ViewModel) updates via a dispatcher abstraction,
- forwarders (e.g., to SignalR) depending on the module you add.

### Delivery semantics (Core contract)

Core is designed so observer processing **does not block** job execution:

- events are published **asynchronously** (fire-and-forget),
- Core does not await observer handlers,
- observer implementations are responsible for exception handling and any ordering guarantees.

For UI and reactive pipelines, the adapter’s dispatcher abstractions are the preferred way to marshal events to the correct thread/scheduler.

### How to: subscribe and forward events

You can always subscribe directly:

```csharp
IDisposable subscription = dispatcher.Subscribe(evt =>
{
    // Forward to logs/metrics/UI as needed.
});
```

Or register an observer implementation (when your adapter/facade exposes it).

> Link placeholder: Core section “Observers” `../TplQueue.Core/README.md#observers`

### UI observer integration

The following content is integrated from the project’s UI observer integration notes (currently located in Core as `ObersversIntegrationReadme.md`). In a later cleanup iteration, consider moving the source file to this repo under a consistent name such as `docs/UIObserversIntegration.md`.

# Job Observers and Front-End Integration

This document outlines how to implement and extend `IObserver<IJobEvent>` observers to work across various front-end UI platforms such as WPF, MAUI, WinUI, UWP, Blazor, and also with Reactive Extensions (Rx).

## Overview

This component offers a **powerful, flexible, and extensible architecture** for observing and broadcasting task execution updates across various UI technologies.

Why it’s especially useful:

- ✅ **Plug-and-play Observers** for different UI stacks.
- ✅ **Scalable and testable core logic**.
- ✅ **Separation of concerns** between business logic and UI/threading.
- ✅ **Real-time monitoring** using SignalR, suitable for dashboards.
- ✅ **Extensibility** for reactive pipelines with Rx, making it suitable for analytical/logging tools or distributed systems.

This design makes the [Job]() job component ideal for both desktop and web environments. Examples and strategies for each integration are included below.

---

## Architecture Overview

This solution is based on the MS [**observer-dispatcher** pattern](), which decouples task monitoring logic from platform-specific UI threading concerns.

- `JobViewModelObserver`: Logic to observe `IJobEvent` changes and push updates to the UI.
- `IObserverDispatcher`: Platform-agnostic interface to invoke actions on the UI thread.
- Platform-specific dispatcher implementations handle the actual invocation mechanism.

This architecture allows writing **unit-testable and cross-platform observers**.

---

## Platform Dispatchers

### ✅ WPF

```csharp
public class WpfDispatcher : IObserverDispatcher
{
    private readonly Dispatcher _dispatcher;

    public WpfDispatcher(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public void Invoke(Action action) => _dispatcher.Invoke(action);
}
```

### ✅ UWP

```csharp
public class UwpDispatcher : IObserverDispatcher
{
    private readonly CoreDispatcher _dispatcher;

    public UwpDispatcher(CoreDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public void Invoke(Action action) => _ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
}
```

### ✅ MAUI (with MainThread)

```csharp
public class MauiDispatcher : IObserverDispatcher
{
    public void Invoke(Action action)
    {
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(action);
    }
}
```

### ✅ WinUI

```csharp
public class WinUiDispatcher : IObserverDispatcher
{
    private readonly DispatcherQueue _dispatcherQueue;

    public WinUiDispatcher(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
    }

    public void Invoke(Action action)
    {
        _dispatcherQueue.TryEnqueue(() => action());
    }
}
```

### ✅ Blazor (Server/Client)

Blazor doesn't have a Dispatcher-like object but can use `InvokeAsync` on a component or `SynchronizationContext`.

```csharp
public class BlazorDispatcher : IObserverDispatcher
{
    private readonly SynchronizationContext _syncContext;

    public BlazorDispatcher(SynchronizationContext syncContext)
    {
        _syncContext = syncContext ?? throw new ArgumentNullException(nameof(syncContext));
    }

    public void Invoke(Action action)
    {
        _syncContext.Post(_ => action(), null);
    }
}
```

## DirectDispatcher (for Unit Testing)

```csharp
public class DirectObserverDispatcher : IObserverDispatcher
{
    public void Invoke(Action action)
    {
        action();
    }
}
```

## Reactive Extensions (Rx)

Reactive Extensions (Rx) can be used to process streams of `IJobEvent` updates and bind them to UIs or logging systems.

### Sample Observer Subscription with Rx

```csharp
IDisposable subscription = jobEvents
    .ToObservable()
    .ObserveOn(Scheduler.CurrentThread) // or use a platform-specific scheduler
    .Subscribe(evt => viewModel.Update(evt));
```

You can also implement `IObservable<IJobEvent>` at a custom event source, such as a task monitor.

### Example Rx Subject Implementation

```csharp
public class JobEventSource : IObservable<IJobEvent>
{
    private readonly Subject<IJobEvent> _subject = new();

    public IDisposable Subscribe(IObserver<IJobEvent> observer) => _subject.Subscribe(observer);

    public void Push(IJobEvent evt) => _subject.OnNext(evt);
}
```

This way you can flexibly route events to any system using Rx pipelines (logging, dashboards, UIs).

---

## SignalR Observer for Real-Time Front-End Binding

To integrate with modern web frameworks like React, Angular, or Vue.js, you can implement a SignalR-based observer that pushes events in real time.

### Example: `JobSignalRObserver`

```csharp
public class JobSignalRObserver : IObserver<IJobEvent>
{
    private readonly IHubContext<JobHub> _hubContext;

    public JobSignalRObserver(IHubContext<JobHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public void OnNext(IJobEvent value)
    {
        _hubContext.Clients.All.SendAsync("JobUpdated", value);
    }

    public void OnError(Exception error) { /* Log if necessary */ }
    public void OnCompleted() { }
}
```

On the client side (e.g., in React):

```js
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/taskrunnerhub")
  .build();

connection.on("JobUpdated", taskUpdate => {
  console.log("Received update", taskUpdate);
  // Update state/UI accordingly
});

connection.start();
```

You can also extend this to send only updates for specific task IDs, authenticated users, or broadcast groups.

---

## ✅ Summary

By implementing one `IObserverDispatcher` per platform, your `JobViewModelObserver` becomes reusable across all your front-end layers. You only need to inject the correct dispatcher into your application.

Additionally, Reactive Extensions (Rx) allows for powerful stream composition, filtering, and transformation of `IJobEvent` updates, suitable for responsive and reactive UIs.

You can also implement a `JobSignalRObserver` to push real-time task updates to browser-based clients through SignalR and synchronize front-end state.

---

## Cache

Adapter contains cache abstractions and providers. The cache is typically used together with persistence/payload modules to:

- store serialized payloads and/or graphs,
- support recovery workflows,
- decouple ingestion from transient failures.

### How to: plug a cache provider into your persistence layer

The exact integration depends on which persistence/serialization module you are using, but the design goal is:

- cache operations are **idempotent**,
- cache transitions align with job lifecycle (Ack/Fail/Cancel),
- the persistence layer can swap cache providers (memory vs file vs SQLite vs EF).

> Link placeholder: Core section “Cache and persistence” `../TplQueue.Core/README.md#cache-and-persistence`

---

## Persistence and serialization

Adapter provides serialization modules (e.g., System.Text.Json) and, depending on your current implementation, may provide a serializable queue/dispatcher that persists graphs/payloads and supports recovery workflows.

> TODO: Add concrete links once the serializable queue/persistence module README sections are finalized.

---

## Links between Core and Adapter docs

Placeholders (workspace layout):

- Core README: `../TplQueue.Core/README.md`
- Adapter README: `../TplQueue.Adapter/README.md`

Recommended cross-links to keep stable:

- Core → Adapter:
  - Retry policies
  - Observers
  - Cache
  - Persistence/serialization
- Adapter → Core:
  - Job and queue fundamentals
  - How retries and observers are integrated in the Core engine

---

## License

TplQueue.Adapter is distributed under the **MIT license**. It is designed to integrate with **TplQueue.Core** (EULA) as the runtime execution engine.
