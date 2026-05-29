# First Job Graph

TplQueue favors explicit graphs over hidden callback chains.

## Compose a rooted graph

`QueueObserverConsole` shows a compact `Extract -> Transform -> Load` pipeline built with `IJob` and `IJobRoot`:

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

## Minimal package-only smoke scenario

`PackageConsumptionSmokeConsole` keeps a smaller public `IJob` / `IJobRoot` example for release-smoke validation:

```csharp
var child = api.JobFactory.Job(
    ct =>
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    },
    name: "smoke-child");

var root = api.JobFactory.JobRoot(
    ct =>
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    },
    name: "smoke-root");

root.After(child);
queue.Enqueue(root, CancellationToken.None);
```

## Enqueue a standalone async closure

You can also enqueue one isolated operation without building a graph first:

```csharp
queue.Enqueue(
    ct => localHelperOperation.ExecuteAsync(ct),
    CancellationToken.None,
    "Standalone helper task");
```

## FIFO closure pipeline

When a whole sequence must remain serialized, `IFifoQ` can execute async closures in enqueue order:

```csharp
queue.Enqueue(async ct => await ExtractAsync(ct), CancellationToken.None, "Extract");
queue.Enqueue(async ct => await TransformAsync(ct), CancellationToken.None, "Transform");
queue.Enqueue(async ct => await LoadAsync(ct), CancellationToken.None, "Load");
queue.Enqueue(async ct => await AfterLoadAsync(ct), CancellationToken.None, "AfterLoad");
```

See also:

- [Jobs](../core-concepts/jobs.md)
- [Job Roots](../core-concepts/job-roots.md)
- [Job Graphs](../core-concepts/job-graphs.md)
- [FIFO Queue](../queues/fifo-queue.md)

## Public source trail

- [`QueueObserverConsole/Program.cs`](https://github.com/fmacias/TplQueue.Usage/blob/main/samples/QueueObserverConsole/Program.cs)
- [`PackageConsumptionSmokeConsole/Program.cs`](https://github.com/fmacias/TplQueue.Usage/blob/main/samples/PackageConsumptionSmokeConsole/Program.cs)
- [`IJob`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IJob.cs)
- [`IJobRoot`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IJobRoot.cs)
