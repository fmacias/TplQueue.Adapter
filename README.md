# TplQueue.Adapter

## Sumario
- Operaciones: el empaquetado local es manual via `pack-local.ps1` o `WorkspaceTplQueue\pack.ps1`.
- Operaciones: `pack-local.ps1` asegura el orden de empaquetado antes del pack del repo.

## Empaquetado local (DevOps)
El empaquetado local es manual y ejecuta `pack-local.ps1`.
Este script empaqueta primero Log, RetryPolicies, Observers.ViewModel, Serialization, DI y Cache.Abstract, y luego el paquete `Fmacias.TplQueue`.
Salida esperada: `.nupkg` y `.snupkg` en `..\TplQueue.NugetLocal`.
Para ejecucion manual: `powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1`.

## How to (step by step)
1) Build only (fast loop) from the workspace:
```powershell
..\WorkspaceTplQueue\build.ps1 -Configuration Debug
```

2) Pack Adapter only:
```powershell
.\pack-local.ps1
```

3) Pack all repos in order (workspace):
```powershell
..\WorkspaceTplQueue\pack.ps1
```

## Why this design (justification)
- Keeps Adapter independently buildable while supporting a shared workspace.
- Avoids packing during every build; packaging is explicit and ordered.
- Reduces restore issues by standardizing the pack sequence and cache behavior.

## Local package caching policy
- The local feed `..\TplQueue.NugetLocal` is the primary source for dev packages.
- Pack scripts force restore to avoid stale global cache packages.
- If you still see old types, delete the global cache folder for the package:
  `C:\Users\<user>\.nuget\packages\fmacias.tplqueue.abstractions\1.0.0`


TplQueue.Adapter contains MIT-licensed adapter components and building blocks for TplQueue: abstractions, retry policies, cache contracts, serialization helpers, DI integration, and observer utilities. It is intended to be used together with TplQueue.Core (EULA) by referencing the Core binary as a NuGet package or project.

## Relationship to TplQueue.Core
TplQueue.Core is the core orchestration engine. TplQueue.Adapter provides MIT-licensed extensions and integrations such as SerializableDispatcher, PayloadTaskRunner/Root, concrete retry policies, observer implementations, cache abstractions and implementations, and DI helpers. See [TplQueue.Core](../TplQueue.Core/README.md) for the engine overview.

## Core runners (TplQueue.Core)
These runners live in the Core repository and are linked here for convenience:
- [TaskRunner (ITaskRunnerFactory)](../TplQueue.Core/README.md#taskrunner-itaskrunnerfactory)
- [TaskRunnerRoot (ITaskRunnerRootFactory)](../TplQueue.Core/README.md#taskrunnerroot-itaskrunnerrootfactory)

## Dispatchers and runners
<a id="serializabledispatcher-placeholder"></a>
### SerializableDispatcher
SerializableDispatcher extends ITaskDispatcher to persist payload graphs in an IPayloadLeaseCache and lease them back into an inner dispatcher when capacity is available. It is the primary adapter for job persistence scenarios and uses a lightweight leasing loop controlled by StartPolling/StopPolling and LeasingPulseMs.

Key behaviors:
- Enqueue/EnqueueFifo persist payload roots in Pending state.
- StartPolling enables the leasing loop and forwards leased graphs to the inner dispatcher.
- StopPolling pauses leasing without disposing the dispatcher.

Basic usage (inspired by integration tests):
```csharp
using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Cache;
using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.Queues;
using Fmaciasruano.TplQueue.RetryPolicies;
using Fmaciasruano.TplQueue.Runner;
using Fmaciasruano.TplQueue.Serialization.SystemTextJson;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ISerializablePayloadDispatcher>();
var retryPolicyFactory = RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>());
var dispatcherFactory = TaskDispatcherFactory.Instance(new Dictionary<string, IDispatcherOptions>(), retryPolicyFactory);
var serializableFactory = SerializableDispatcherFactory.Instance();
var payloadRunnerFactory = PayloadRunnerFactory.Instance(
    TaskRunnerFactory.Instance(),
    TaskRunnerRootFactory.Instance(),
    RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions> { { "none", RetryPolicyOptions.Linear(0, 0) } }));
var serializer = SystemTextJsonUniversalSerializer.Create();
var cacheFactory = CacheFactory.Instance();
var memCache = cacheFactory.CreateMemCache(payloadRunnerFactory, serializer);

var inner = dispatcherFactory.CreateParallel(
    name: "default-dispatcher",
    retryPolicyFactory: () => retryPolicyFactory.CreateNoRetryPolicy(),
    maxParallelism: 8,
    logger: logger,
    pulseMs: 250);

var dispatcher = serializableFactory.Create(logger, memCache, inner);

// DummyPayload implements IPayloadCommand.
var rootPayload = new DummyPayload();
var childPayload = new DummyPayload();
var root = payloadRunnerFactory.CreateRoot(
    rootPayload,
    serializer,
    () => retryPolicyFactory.CreateNoRetryPolicy(),
    name: "root-job");
var child = payloadRunnerFactory.Create(childPayload, serializer, "child-job");
root.After(child);

dispatcher.Enqueue(root, CancellationToken.None);
dispatcher.StartPolling();
```

<a id="payloadtaskrunner-placeholder"></a>
### PayloadTaskRunner
PayloadTaskRunner wraps a TaskRunner with payload serialization, enabling persistence and rehydration of payload graphs. It is created via PayloadRunnerFactory and carries both the payload instance and its serialized representation.

Usage:
```csharp
using Fmaciasruano.TplQueue.Runner;
using Fmaciasruano.TplQueue.Serialization.SystemTextJson;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.RetryPolicies;
using System.Collections.Generic;

var serializer = SystemTextJsonUniversalSerializer.Create();
var payloadRunnerFactory = PayloadRunnerFactory.Instance(
    TaskRunnerFactory.Instance(),
    TaskRunnerRootFactory.Instance(),
    RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>()));

// DummyPayload implements IPayloadCommand.
var payload = new DummyPayload();
var node = payloadRunnerFactory.Create(payload, serializer, "payload-node");
```

Rehydration from cache (when leasing persisted graphs):
```csharp
IPayloadCarrier node = payloadRunnerFactory.Load(cacheLeaseEntry, serializer);
```

<a id="payloadtaskrunnerroot-placeholder"></a>
### PayloadTaskRunnerRoot
PayloadTaskRunnerRoot is the enqueueable root for payload graphs. It preserves retry policy configuration and can be scheduled on any ITaskDispatcher or persisted via SerializableDispatcher.

Usage:
```csharp
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.RetryPolicies;
using Fmaciasruano.TplQueue.Runner;
using Fmaciasruano.TplQueue.Serialization.SystemTextJson;
using System.Collections.Generic;

var serializer = SystemTextJsonUniversalSerializer.Create();
var payloadRunnerFactory = PayloadRunnerFactory.Instance(
    TaskRunnerFactory.Instance(),
    TaskRunnerRootFactory.Instance(),
    RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>()));

// DummyPayload implements IPayloadCommand.
var payload = new DummyPayload();
var root = payloadRunnerFactory.CreateRoot(
    payload,
    serializer,
    retryPolicyFactory: () => NoRetryPolicy.Create(),
    name: "payload-root");

dispatcher.Enqueue(root, CancellationToken.None);
dispatcher.StartPolling();
```

Rehydration from cache (includes retry policy descriptor):
```csharp
IPayloadCarrierRoot root = payloadRunnerFactory.LoadRoot(cacheLeaseEntry, serializer);
```

## Observers
<a id="observers-placeholder"></a>
Observers react to task/job lifecycle events for logging, metrics, UI updates, and notifications.

Common setup (factory + subscription):
```csharp
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Observers;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var observerFactory = ObserverFactory.Instance();
var logging = observerFactory.CreateLoggingObserver(loggerFactory.CreateLogger<ITaskQueueLoggingObserver>());

IDisposable subscription = dispatcher.Subscribe(logging);
```

### TaskQueueLoggingObserver
Centralized queue logging via ILogger.
```csharp
var logging = observerFactory.CreateLoggingObserver(loggerFactory.CreateLogger<ITaskQueueLoggingObserver>());
dispatcher.Subscribe(logging);
```

### TaskRunnerConsoleObserver
Writes task lifecycle events to the console.
```csharp
var consoleObserver = observerFactory.CreateConsoleObserver();
dispatcher.Subscribe(consoleObserver);
```

### TaskRunnerFileLoggingObserver
Writes lifecycle events to an ILogger-backed file sink (per queue).
```csharp
using Fmaciasruano.TplQueue.Observers;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
dispatcher.SubscribeFileLogger(loggerFactory, queueName: "queue-1");
```

### TaskRunnerProfilingObserver
Logs per-event memory and GC data to help identify pressure hot spots.
```csharp
var profiling = observerFactory.CreateProfilingObserver(loggerFactory.CreateLogger<IProfilingObserver>());
dispatcher.Subscribe(profiling);
```

### TaskRunnerViewModelObserver
Emits ViewModel-friendly events using the Observers.ViewModel package.
```csharp
var observerDispatcher = observerFactory.CreateObserverDispatcher();
var vmObserver = observerFactory.CreateViewModeObserver(observerDispatcher);
dispatcher.Subscribe(vmObserver);
```

### DirectObserverDispatcher
Simple dispatcher used for console/testing scenarios.
```csharp
var observerDispatcher = observerFactory.CreateObserverDispatcher();
observerDispatcher.Invoke(() => { /* update UI or state */ });
```

UI integration scenarios include:
- WPF/WinUI/MAUI view models that surface task state and progress updates.
- Blazor or SignalR dashboards that stream lifecycle events to clients.
- Desktop apps that bind observers to status panels and notifications.

### How to implement your own observer
Implement IObserver<ITaskRunnerEvent>, keep handlers thread-safe, avoid blocking the dispatcher thread, and dispatch UI updates to the appropriate UI context.

## RetryPolicies
Retry policies provide reusable failure-handling strategies for dispatchers and runners.

### RetryPolicyFactory
```csharp
using Fmaciasruano.TplQueue.RetryPolicies;
using System.Collections.Generic;

var options = new Dictionary<string, RetryPolicyOptions>
{
    { "linear-3", RetryPolicyOptions.Linear(baseDelayMs: 200, maxRetries: 3) },
    { "exp-5", RetryPolicyOptions.Exponential(baseDelayMs: 250, maxRetries: 5, factor: 2.0) }
};

IRetryPolicyFactory factory = RetryPolicyFactory.Instance(options);
```

<a id="noretry-placeholder"></a>
### NoRetry
NoRetryPolicy performs no retries and immediately surfaces failures.
```csharp
var noRetry = factory.CreateNoRetryPolicy();
```

<a id="linearbackoff-placeholder"></a>
### LinearBackoff
LinearBackoffRetryPolicy increases the delay linearly between attempts.
```csharp
var linear = factory.CreateLinearBackoff(maxRetries: 3, baseDelayMilliseconds: 200);
```

<a id="exponentialbackoff-jitter-placeholder"></a>
### ExponentialBackoff with Jitter
ExponentialBackoffRetryPolicy uses exponential delays with jitter to reduce contention.
```csharp
var exponential = factory.CreateExponentialBackoff(
    maxRetries: 5,
    factor: 2.0,
    shouldRetry: true,
    baseDelayMilliseconds: 250);
```

### How to implement your own retry policy
Implement IRetryPolicy (and the serialization helpers), expose ToDescriptor/SetFromDescriptor, and register the policy so it can be rehydrated by name or descriptor.
```csharp
using Fmaciasruano.TplQueue.Abstractions;
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class CustomRetryPolicy : IRetryPolicy
{
    public int RetryCount { get; private set; }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken ct)
    {
        // Custom logic here
        return await action(ct).ConfigureAwait(false);
    }

    public IRetryPolicyDescriptor ToDescriptor()
        => RetryPolicyDescriptor.Personalized(
            "custom",
            typeof(CustomRetryPolicy),
            maxRetries: 2,
            baseDelayMs: 100,
            factor: null,
            shouldRetry: true);

    public IRetryPolicy SetFromDescriptor(IRetryPolicyDescriptor descriptor) => this;
    public IRetryPolicy SetFromOptions(RetryPolicyOptions options) => this;
}
```

## Cache
<a id="cache-placeholder"></a>
Cache integration enables payload and job graph persistence for recovery and decoupling from transient failures.

Cache-related types in this repository:
- CacheFactory: entry point for cache creation.
- MemCache: in-memory implementation of IPayloadLeaseCache.

### CacheFactory
```csharp
using Fmaciasruano.TplQueue.Abstractions.Contracts;
using Fmaciasruano.TplQueue.Cache;
using Fmaciasruano.TplQueue.Core.Factories;
using Fmaciasruano.TplQueue.RetryPolicies;
using Fmaciasruano.TplQueue.Runner;
using Fmaciasruano.TplQueue.Serialization.SystemTextJson;
using System.Collections.Generic;

var cacheFactory = CacheFactory.Instance();
var payloadRunnerFactory = PayloadRunnerFactory.Instance(
    TaskRunnerFactory.Instance(),
    TaskRunnerRootFactory.Instance(),
    RetryPolicyFactory.Instance(new Dictionary<string, RetryPolicyOptions>()));
var serializer = SystemTextJsonUniversalSerializer.Create();

IMemCache cache = cacheFactory.CreateMemCache(payloadRunnerFactory, serializer);
```

### MemCache
In-memory lease cache suitable for tests, local runs, or small workloads.
```csharp
IMemCache cache = MemCache.Create(payloadRunnerFactory, serializer);
```

### How to implement your own cache
Implement IPayloadLeaseCache (or derive from Cache.Abstract) to persist and lease graphs. Ensure idempotency, honor lease semantics, and keep Append/Ack/Fail/Cancel consistent with dispatcher events.

## Further documentation
Each submodule (Cache, RetryPolicies, Serialization, DI, Observers) will have its own documentation under its respective `docs` folders. This README is the entry point.

## Visual Studio session note
Avoid opening `WorkspaceTplQueue.sln` and any `TplQueue.*.sln` in separate VS sessions at the same time. The workspace swaps to project references, while standalone solutions stay package-based, and running both can lead to confusing dependency views or build output conflicts.


