# Erster Job-Graph

TplQueue bevorzugt explizite Graphen gegenüber versteckten Callback-Ketten.

## Einen verwurzelten Graphen zusammensetzen

`QueueObserverConsole` zeigt eine kompakte `Extract -> Transform -> Load`-Pipeline, die mit `IJob` und `IJobRoot` aufgebaut wird:

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

Dies ist das empfohlene Muster, wenn Ihre Anwendung bereits einen Abhängigkeitsgraphen hat und der enqueuebare Terminalknoten das `IJobRoot` ist.

## Eine eigenständige Async-Closure enqueueen

Sie können auch eine einzelne isolierte Operation enqueueen, ohne vorher einen Graphen aufzubauen:

```csharp
queue.Enqueue(
    ct => localHelperOperation.ExecuteAsync(ct),
    CancellationToken.None,
    "Standalone helper task");
```

## FIFO-Closure-Pipeline

Wenn eine vollständige Sequenz serialisiert bleiben muss, kann `IFifoQ` Async-Closures in Enqueue-Reihenfolge ausführen:

```csharp
queue.Enqueue(async ct => await ExtractAsync(ct), CancellationToken.None, "Extract");
queue.Enqueue(async ct => await TransformAsync(ct), CancellationToken.None, "Transform");
queue.Enqueue(async ct => await LoadAsync(ct), CancellationToken.None, "Load");
queue.Enqueue(async ct => await AfterLoadAsync(ct), CancellationToken.None, "AfterLoad");
```

Siehe auch:

- [Jobs](../core-concepts/jobs.md)
- [Job Roots](../core-concepts/job-roots.md)
- [Job-Graphen](../core-concepts/job-graphs.md)
- [FIFO-Queue](../queues/fifo-queue.md)
