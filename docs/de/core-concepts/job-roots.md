# Job Roots

`IJobRoot` ist der enqueuebare Einstiegspunkt eines Graphen.

```csharp
IJobRoot root = core.JobFactory.JobRoot(
    async ct => await Task.CompletedTask,
    name: "ImportRoot");

root.After(transform);
```

Die Root sollte der Terminalknoten bleiben. Sie ist das Element, das eine Queue übernimmt und enqueueed.

## Öffentliche Contract-Quelle

Der öffentliche Root-Contract liegt in `TplQueue.Abstractions`:

```csharp
public interface IJobRoot : IJobNode
{
    IJobRoot After(params IJobNode[] previousTasks);
    IQ Enqueue(IQ jobQ, CancellationToken ct);
}
```

Öffentliche Source-Links:

- [`IJobRoot`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IJobRoot.cs)
- [`IQ`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IQ.cs)
