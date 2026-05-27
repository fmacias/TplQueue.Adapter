# Job Roots

`IJobRoot` is the enqueueable entry point of a graph.

```csharp
IJobRoot root = core.JobFactory.JobRoot(
    async ct => await Task.CompletedTask,
    name: "ImportRoot");

root.After(transform);
```

The root is expected to remain the terminal node of the graph. It is the element that a queue claims and enqueues.
