# TplQueue with .NET and ASP.NET

`TplQueue.Adapter` is the integration layer that sits on top of `TplQueue.Core`. It is the repository to use when you want named queues, retry-policy dictionaries, built-in observers, serializer support, cache-backed dispatch, and Microsoft dependency injection integration in one place.

Use this tutorial together with:

- [TplQueue.Usage](https://github.com/fmacias/TplQueue.Usage) for runnable consumer samples
- [fmacias.github.io](https://fmacias.github.io/) for the public documentation portal
- [TplQueue.Core access request](https://fmacias.github.io/tplqueue/core-license/) for Core source-access, support, and maintenance requests

For ASP.NET applications, the recommended starting point is `Fmacias.TplQueue.Microsoft.DependencyInjection`.

## Prerequisites

Install the queue runtime and the DI integration package:

```bash
dotnet add package Fmacias.TplQueue.Core --version 0.1.0-preview.1
dotnet add package Fmacias.TplQueue.Microsoft.DependencyInjection --version 0.1.0-preview.1
```

Add `Fmacias.TplQueue` explicitly when you want to create `API` yourself in composition code:

```bash
dotnet add package Fmacias.TplQueue --version 0.1.0-preview.1
```

The published `TplQueue.Core` package can be consumed under its package license without source access. The Core access request path is the channel for source access, support, maintenance, and professional help when modernizing a legacy solution.

## Load configuration

The adapter queue factories consume named retry-policy and queue dictionaries. In ASP.NET, keep that information in normal application configuration and convert it to `RetryPolicyOptions` and `QOptions` before building the facade.

The public `QueueObserverSignalRDashboard` sample uses the same top-level shape:

```json
{
  "TplQueue": {
    "MetadataDispatcherName": "dashboard-metadata",
    "PayloadDispatcherName": "dashboard-payload",
    "RetryPolicies": {
      "dashboard-default": {
        "BaseDelayMs": 200,
        "MaxRetries": 3,
        "Factor": 2.0
      },
      "payload-default": {
        "BaseDelayMs": 150,
        "MaxRetries": 2,
        "Factor": 1.5
      }
    },
    "Dispatchers": {
      "dashboard-metadata": {
        "Id": "2bdba3c7-7d17-4ea5-b2cb-7cf3f7ea14b9",
        "MaxParallelism": 1,
        "RetryPolicy": "dashboard-default"
      },
      "dashboard-payload": {
        "Id": "0c803fe4-1f9d-420b-b455-fcd556a2ef97",
        "MaxParallelism": 1,
        "RetryPolicy": "payload-default"
      }
    }
  }
}
```

Add an explicit `Id` when the queue identity must remain deterministic across restarts or when external systems need to correlate with the same dispatcher identity. If you do not need that, generate the `Guid` during configuration mapping.

The sample registration path is:

```csharp
var settings = TplQueueDashboardSettings.Load(configuration);
var retryPolicies = settings.CreateRetryPolicies();
var dispatchers = settings.CreateDispatchers();
var api = API.Create(CoreApi.Create(), retryPolicies, dispatchers);

services.AddSingleton(settings);
services.AddTplQueue(api, retryPolicies, dispatchers);
```

Relevant source entry points:

- [`API.Create(...)`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/API.cs)
- [`IQFactoryAdapter` named queue creation](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/Factories/QFactoryAdapter.cs)
- [`AddTplQueue(...)` overloads](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Microsoft.DependencyInjection/ServiceCollectionExtensions.cs)

## Instance a ParallelQ queue from configuration

When the queue name already exists in the dispatcher dictionary, create the queue by name and let the adapter resolve `IQOptions` and the named retry policy:

```csharp
var queueFactory = serviceProvider.GetRequiredService<IQFactoryAdapter>();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

IParallelQ queue = queueFactory.Parallel(
    "dashboard-metadata",
    loggerFactory.CreateLogger<IParallelQ>());
```

The adapter also exposes explicit overloads when the application wants to instantiate a queue from `IQOptions` directly or from raw values such as `Guid`, `name`, and `maxParallelism`. See the source for the available overloads in [`QFactoryAdapter`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/Factories/QFactoryAdapter.cs) and the facade surface in [`API`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/API.cs).

### Extract -> Transform -> Load job graph

`QueueObserverConsole` shows a compact rooted graph built with `IJob` and `IJobRoot`:

```csharp
var extract = api.JobFactory.Job(
    ExtractAsync,
    state,
    context,
    name: "Extract");

var transform = api.JobFactory.Job(
    Transform,
    state,
    context,
    name: "Transform");

var load = api.JobFactory.JobRoot(
    Load,
    state,
    context,
    retryPolicyFactory,
    name: "Load");

extract.Then(transform).Then(load);

queue.Enqueue(load, workflowCancellation.Token);
```

This is the recommended pattern when your application already has a dependency graph and the enqueueable terminal node is the `IJobRoot`.

Sample link:

- [QueueObserverConsole](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverConsole)

### Standalone async closure in the same queue

The same sample also enqueues work that does not need a graph:

```csharp
queue.Enqueue(
    ct => localHelperOperation.ExecuteAsync(ct),
    CancellationToken.None,
    "Standalone helper task");
```

Use this shape when you want queue ownership, cancellation, and retry-policy integration for one isolated async operation without building an `IJob` graph first.

## Instance a FifoQ queue from configuration

For strict sequential execution, create the queue by name through `IQFactoryAdapter.Fifo(...)`:

```csharp
var queueFactory = serviceProvider.GetRequiredService<IQFactoryAdapter>();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

IFifoQ queue = queueFactory.Fifo(
    "dashboard-metadata",
    loggerFactory.CreateLogger<IFifoQ>());
```

The same named and explicit overload families used by `Parallel(...)` are available for `Fifo(...)`. See [`QFactoryAdapter`](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/Factories/QFactoryAdapter.cs) for the concrete entry points.

The simplest way to model a sequential workflow in a FIFO dispatcher is to enqueue async closures in order:

```csharp
var state = new SequentialImportState();

queue.Enqueue(
    async ct =>
    {
        ct.ThrowIfCancellationRequested();
        state.RawXml = await File.ReadAllTextAsync("Data/greetings.xml", ct).ConfigureAwait(false);
    },
    CancellationToken.None,
    name: "Extract");

queue.Enqueue(
    async ct =>
    {
        ct.ThrowIfCancellationRequested();
        state.JsonPayload = await TransformAsync(state.RawXml, ct).ConfigureAwait(false);
    },
    CancellationToken.None,
    name: "Transform");

queue.Enqueue(
    async ct =>
    {
        ct.ThrowIfCancellationRequested();
        await File.WriteAllTextAsync("Data/greetings.json", state.JsonPayload, ct).ConfigureAwait(false);
    },
    CancellationToken.None,
    name: "Load");

queue.Enqueue(
    async ct =>
    {
        ct.ThrowIfCancellationRequested();
        await AfterLoadAsync(state, ct).ConfigureAwait(false);
    },
    CancellationToken.None,
    name: "AfterLoad");

await queue.Wait().ConfigureAwait(false);
```

Even though every step is async, `IFifoQ` preserves the enqueue order. The public smoke sample validates the same closure-based pattern in a reduced form:

- [PackageConsumptionSmokeConsole](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/PackageConsumptionSmokeConsole)

## Instance a CacheQ with a dedicated configured parallel queue

`ICacheQ` is the right choice when a payload-aware root must be persisted before the in-memory dispatcher executes it. The backing runtime is still an `IParallelQ`, but the cache queue adds the persistence and lease layer in front of it.

Create the backing queue from configuration, build a serializer-backed cache, and then wrap both in `ICacheQ`:

```csharp
var queueFactory = serviceProvider.GetRequiredService<IQFactoryAdapter>();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

IParallelQ payloadQueue = queueFactory.Parallel(
    "dashboard-payload",
    loggerFactory.CreateLogger<IParallelQ>());

IMemCache cache = api.Cache<IMemCache>(
    MemCacheFactory.Create(),
    api.SystemTextSerializerFactory().Serializer(
        new JsonSerializerOptions { WriteIndented = true }));

using ICacheQ cacheQueue = api.QFactory.CacheQ(
    loggerFactory.CreateLogger<ICacheQ>(),
    cache,
    payloadQueue);
```

Create the handlers once, register them on `API`, and reuse the same behavior when you build the payload graph:

```csharp
IHandler extractHandler = new ExtractGreetingsHandler(_xmlSerializer, _jsonSerializer);
IHandler transformHandler = new TransformGreetingsHandler(_jsonSerializer, _jsonOutputDirectory);
IHandler loadHandler = new LoadGreetingsHandler();

api.RegisterPayloadHandler(ExtractGreetingsPayload.HandlerKey, extractHandler);
api.RegisterPayloadHandler(TransformGreetingsPayload.HandlerKey, transformHandler);
api.RegisterPayloadHandler(LoadGreetingsPayload.HandlerKey, loadHandler);
```

Then build the payload graph and enqueue the `IDataJobRoot` through `ICacheQ`:

```csharp
var extract = api.DataJobFactory.DataJob(
    extractPayload,
    extractHandler,
    name: "Payload Extract");

var transform = api.DataJobFactory.DataJob(
    transformPayload,
    transformHandler,
    name: "Payload Transform");

var load = api.DataJobFactory.DataJobRoot(
    loadPayload,
    loadHandler,
    name: "Payload Load",
    retryPolicy: retryPolicyFactory);

load.After(transform);
transform.After(extract);

cacheQueue.Enqueue(load, CancellationToken.None);
cacheQueue.ResumePolling();
await load.WaitUntilFinishedAsync().ConfigureAwait(false);
```

In a file-oriented ETL scenario, let `Transform` materialize each projected object as JSON before `Load` publishes or imports the final result:

```csharp
private async Task TransformAsync(
    TransformGreetingsPayload transformPayload,
    LoadGreetingsPayload loadPayload,
    CancellationToken ct)
{
    Directory.CreateDirectory(_jsonOutputDirectory);

    var cards = transformPayload.SourceGreetings
        .Select((greeting, index) => new GreetingCard
        {
            Language = greeting.Language,
            Headline = $"{index + 1}. {greeting.Language.ToUpperInvariant()}",
            Body = greeting.Text,
            Sequence = index + 1
        })
        .ToArray();

    foreach (var card in cards)
    {
        var path = Path.Combine(_jsonOutputDirectory, $"{card.Sequence:000}-{card.Language}.json");
        var json = _jsonSerializer.Serialize(card);
        await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
    }

    loadPayload.PublishedCards = cards;
}
```

This keeps the cache and serializer behavior visible in the sample flow and makes every transformed payload independently inspectable on disk.

Related samples and tests:

- [QueueObserverSignalRDashboard](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/QueueObserverSignalRDashboard)
- [PackageConsumptionSmokeConsole payload-cache mode](https://github.com/fmacias/TplQueue.Usage/tree/main/samples/PackageConsumptionSmokeConsole)

## Module guides

Package-specific guidance remains in the module READMEs:

- [Fmacias.TplQueue](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/README.md)
- [Fmacias.TplQueue.Cache.Abstract](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Cache.Abstract/README.md)
- [Fmacias.TplQueue.Cache.MemCache](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Cache.MemCache/README.md)
- [Fmacias.TplQueue.Microsoft.DependencyInjection](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Microsoft.DependencyInjection/README.md)
- [Fmacias.TplQueue.Observers](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Observers/README.md)
- [Fmacias.TplQueue.RetryPolicies](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.RetryPolicies/README.md)
- [Fmacias.TplQueue.Serialization.SystemTextJson](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Serialization.SystemTextJson/README.md)
- [Fmacias.TplQueue.Serialization.Xml](https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue.Serialization.Xml/README.md)
