# Job Roots

`IJobRoot` is the enqueueable entry point of a graph.

```csharp
IJobRoot root = core.JobFactory.JobRoot(
    async ct => await Task.CompletedTask,
    name: "ImportRoot");

root.After(transform);
```

The root is expected to remain the terminal node of the graph. It is the element that a queue claims and enqueues.

## Public contract source

The public root contract lives in `TplQueue.Abstractions`:

```csharp
public interface IJobRoot : IJobNode
{
    IJobRoot After(params IJobNode[] previousTasks);
    IQ Enqueue(IQ jobQ, CancellationToken ct);
}
```

Public source links:

- [`IJobRoot`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IJobRoot.cs)
- [`IQ`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IQ.cs)
