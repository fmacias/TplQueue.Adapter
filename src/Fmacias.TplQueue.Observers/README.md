# Fmacias.TplQueue.Observers

Observer support package for `TplQueue` job lifecycle events.

See also:

- [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md)
- [TplQueue.Core observers section](https://github.com/fmacias/TplQueue.Core/blob/main/README.md#observers)
- [Fmacias.TplQueue README](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)

Repository-wide packaging and strong-name signing rules are documented in the [TplQueue.Adapter root README](https://github.com/fmacias/TplQueue.Adapter/blob/main/README.md#strong-name-signing).

## Table of contents

- [Summary](#summary)
- [Why observers matter](#why-observers-matter)
- [Module purpose](#module-purpose)
- [Factory-first usage](#factory-first-usage)
- [Built-in observers](#built-in-observers)
- [Queue integration](#queue-integration)
- [Custom observers in consumer applications](#custom-observers-in-consumer-applications)
- [Dispatcher integration for UI applications](#dispatcher-integration-for-ui-applications)
- [Dashboard and SignalR bridge example](#dashboard-and-signalr-bridge-example)
- [Reactive Extensions](#reactive-extensions)
- [Logging helper](#logging-helper)
- [Design justification](#design-justification)

## Summary

`Fmacias.TplQueue.Observers` owns the observer implementations and observer factory used by the adapter facade. Consumers work with the public contracts from `Fmacias.TplQueue.Contracts`, especially:

- `IObserverFactory`
- `IConsoleObserver`
- `ILoggingObserver`
- `IFileLoggingObserver`
- `IProfilingObserver`
- `IObserverDispatcher`
- `IObserver<IJobEvent>`

The built-in observer classes are internal implementation details. Use `ObserverFactory.Create()` or `api.ObserverFactory()` instead of constructing observer classes directly.

## Why observers matter

Observer integration is a key part of the `TplQueue` architecture. A queue emits `IJobEvent` values while it executes `IJobRoot` and `IDataJobRoot` graphs. Those events can feed logs, metrics, audit streams, operational dashboards, and UI status panels without coupling the queue engine to a specific UI framework or transport.

This is especially useful for existing applications. A legacy WPF, WinForms, or ASP.NET application can keep its current UI and workflow while a `TplQueue` observer forwards job lifecycle events to a modern dashboard, a SignalR hub, a metrics pipeline, or a logging system. The application does not need to replace its legacy UI first; it can add visibility around background execution in parallel.

## Module purpose

This package owns:

- the public `ObserverFactory`
- internal built-in observers for console, logging, file-style structured logging, and profiling scenarios
- the internal `DirectObserverDispatcher` implementation returned by the factory for inline dispatch
- logging subscription helpers for queue event streams

Core still owns event publication through [`IQ : IObservable<IJobEvent>`](https://github.com/fmacias/TplQueue.Core/blob/main/README.md#observers). This package owns the reusable observer implementations and the consumer-side construction entry point.

## Factory-first usage

Use the factory from this package directly:

```csharp
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Observers;
using Microsoft.Extensions.Logging;

IObserverFactory observers = ObserverFactory.Create();

IConsoleObserver consoleObserver = observers.CreateConsoleObserver();
ILoggingObserver loggingObserver = observers.CreateLoggingObserver(
    loggerFactory.CreateLogger<ILoggingObserver>());
IProfilingObserver profilingObserver = observers.CreateProfilingObserver(
    loggerFactory.CreateLogger<IProfilingObserver>());
IFileLoggingObserver fileObserver = observers.CreateFileLoggingObserver(
    loggerFactory.CreateLogger("TplQueue.Main"),
    queueName: "main");
IObserverDispatcher dispatcher = observers.CreateObserverDispatcher();
```

Or use the top-level adapter facade:

```csharp
IObserverFactory observers = api.ObserverFactory();
IConsoleObserver consoleObserver = observers.CreateConsoleObserver();

using IDisposable subscription = queue.Subscribe(consoleObserver);
```

## Built-in observers

The factory can create these built-in observer contracts:

- `IConsoleObserver`: writes basic event and error information to the console
- `ILoggingObserver`: writes job lifecycle events through `ILogger<ILoggingObserver>`
- `IFileLoggingObserver`: writes structured queue event lines through an application-provided `ILogger`
- `IProfilingObserver`: writes memory and GC-oriented profiling information through `ILogger<IProfilingObserver>`
- `IObserverDispatcher`: dispatches observer callbacks through the default inline dispatcher

The concrete classes are internal so the package can evolve their implementation without forcing consumers to depend on constructor details.

## Queue integration

Every queue implements `IObservable<IJobEvent>` through `IQ`. Subscribe observers directly to the queue:

```csharp
IObserverFactory observers = api.ObserverFactory();
ILoggingObserver loggingObserver = observers.CreateLoggingObserver(
    loggerFactory.CreateLogger<ILoggingObserver>());

using IDisposable subscription = queue.Subscribe(loggingObserver);
```

Useful event fields for monitoring include:

- `value.Status`
- `value.JobInfo.Id`
- `value.JobInfo.Name`
- `value.JobInfo.CrossQueueId`
- `value.Timestamp`
- `value.RetryCount`
- `value.Exception`

Observer callbacks should be treated as operational telemetry. They are useful for dashboards, logs, and diagnostics, but they should not become the transactional control path for the job itself.

## Custom observers in consumer applications

Applications can implement their own observers by implementing `IObserver<IJobEvent>`.

```csharp
using System;
using Fmacias.TplQueue.Contracts;

public sealed class DashboardObserver : IObserver<IJobEvent>
{
    private readonly IJobDashboardSink _dashboard;

    public DashboardObserver(IJobDashboardSink dashboard)
    {
        _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
    }

    public void OnNext(IJobEvent value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        _dashboard.Publish(new JobDashboardEvent
        {
            JobId = value.JobInfo.Id,
            JobName = value.JobInfo.Name,
            Status = value.Status.ToString(),
            Timestamp = value.Timestamp,
            RetryCount = value.RetryCount
        });
    }

    public void OnError(Exception error)
    {
        _dashboard.PublishError(error);
    }

    public void OnCompleted()
    {
        _dashboard.Complete();
    }
}
```

Recommended rules:

- keep `OnNext` fast
- avoid blocking I/O directly in observer callbacks
- isolate observer failures from application workflow logic
- marshal to the required scheduler or UI thread outside Core
- treat observer delivery as monitoring and integration data, not as a replacement for durable business transactions

## Dispatcher integration for UI applications

`IObserverDispatcher` is the small adapter service used when observer callbacks need to update UI-bound state. The observer package provides a direct inline dispatcher through `CreateObserverDispatcher()`. UI applications can implement their own dispatcher without adding UI framework dependencies to Core or to the observer contracts.

### WPF

```csharp
using System;
using System.Windows.Threading;
using Fmacias.TplQueue.Contracts;

public sealed class WpfObserverDispatcher : IObserverDispatcher
{
    private readonly Dispatcher _dispatcher;

    public WpfObserverDispatcher(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public void Invoke(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _dispatcher.Invoke(action);
    }
}
```

### WinForms

```csharp
using System;
using System.Windows.Forms;
using Fmacias.TplQueue.Contracts;

public sealed class WinFormsObserverDispatcher : IObserverDispatcher
{
    private readonly Control _control;

    public WinFormsObserverDispatcher(Control control)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
    }

    public void Invoke(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (_control.InvokeRequired)
        {
            _control.BeginInvoke(action);
            return;
        }

        action();
    }
}
```

### UWP

```csharp
using System;
using Windows.UI.Core;
using Fmacias.TplQueue.Contracts;

public sealed class UwpObserverDispatcher : IObserverDispatcher
{
    private readonly CoreDispatcher _dispatcher;

    public UwpObserverDispatcher(CoreDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public void Invoke(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
    }
}
```

### MAUI

```csharp
using System;
using Fmacias.TplQueue.Contracts;

public sealed class MauiObserverDispatcher : IObserverDispatcher
{
    public void Invoke(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(action);
    }
}
```

### WinUI

```csharp
using System;
using Microsoft.UI.Dispatching;
using Fmacias.TplQueue.Contracts;

public sealed class WinUiObserverDispatcher : IObserverDispatcher
{
    private readonly DispatcherQueue _dispatcherQueue;

    public WinUiObserverDispatcher(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
    }

    public void Invoke(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _dispatcherQueue.TryEnqueue(() => action());
    }
}
```

### Blazor or generic synchronization-context hosts

```csharp
using System;
using System.Threading;
using Fmacias.TplQueue.Contracts;

public sealed class SynchronizationContextObserverDispatcher : IObserverDispatcher
{
    private readonly SynchronizationContext _syncContext;

    public SynchronizationContextObserverDispatcher(SynchronizationContext syncContext)
    {
        _syncContext = syncContext ?? throw new ArgumentNullException(nameof(syncContext));
    }

    public void Invoke(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _syncContext.Post(_ => action(), null);
    }
}
```

### ASP.NET or background-hosted dashboard bridge

Server-side applications usually should not marshal to a UI thread. They can implement an observer that forwards events to an application service, channel, message bus, or SignalR hub.

```csharp
using System;
using Fmacias.TplQueue.Contracts;

public sealed class DashboardForwardingObserver : IObserver<IJobEvent>
{
    private readonly IDashboardEventPublisher _publisher;

    public DashboardForwardingObserver(IDashboardEventPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public void OnNext(IJobEvent value)
    {
        _publisher.PublishJobEvent(value);
    }

    public void OnError(Exception error)
    {
        _publisher.PublishObserverError(error);
    }

    public void OnCompleted()
    {
        _publisher.PublishObserverCompleted();
    }
}
```

## Dashboard and SignalR bridge example

The observer contract is suitable for real-time browser updates. Keep transport-specific code outside Core and outside the queue implementation.

```csharp
using System;
using Fmacias.TplQueue.Contracts;
using Microsoft.AspNetCore.SignalR;

public sealed class JobEventSignalRObserver : IObserver<IJobEvent>
{
    private readonly IHubContext<JobEventsHub> _hubContext;

    public JobEventSignalRObserver(IHubContext<JobEventsHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public void OnNext(IJobEvent value)
    {
        _ = _hubContext.Clients.All.SendAsync("JobUpdated", new
        {
            value.JobInfo.Id,
            value.JobInfo.Name,
            Status = value.Status.ToString(),
            value.Timestamp,
            value.RetryCount
        });
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }
}
```

The separation is deliberate:

- the queue emits `IJobEvent`
- the observer maps those events to the dashboard or transport payload
- the application owns the transport, UI framework, and delivery policy

## Reactive Extensions

If your application already uses `System.Reactive`, the queue can be consumed directly because `IQ` already implements `IObservable<IJobEvent>`.

That means operators such as filtering, buffering, throttling, and scheduler switching can be applied without a Core-specific adapter layer. Keep those Rx choices in the consumer application because they usually depend on the hosting model, scheduler, and dashboard or UI requirements.

## Logging helper

`SubscribeFileLogger` is a convenience extension for queue event streams:

```csharp
using Fmacias.TplQueue.Observers;

using IDisposable subscription = queue.SubscribeFileLogger(
    loggerFactory,
    queueName: "main");
```

Use the factory when you need to keep ownership of the observer instance:

```csharp
IFileLoggingObserver observer = api.ObserverFactory().CreateFileLoggingObserver(
    loggerFactory.CreateLogger("TplQueue.Main"),
    queueName: "main");

using IDisposable subscription = queue.Subscribe(observer);
```

## Design justification

Concrete observers do not belong in the thin top-level adapter facade. The observer package owns them because observer construction, default dispatching, and logging-oriented subscriptions are all part of the observer module.

Keeping the concrete observers internal gives consumers a stable contract-based surface:

- `Fmacias.TplQueue` can expose `api.ObserverFactory()` without owning observer implementations
- consumers can create the built-in observers without depending on concrete constructor details
- applications can add WPF, WinForms, ASP.NET, SignalR, or dashboard-specific observers without changing Core
- Core stays focused on job graph execution, queue scheduling, retry integration, and event publication
