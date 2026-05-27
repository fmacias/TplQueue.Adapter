# Job Roots

`IJobRoot` ist der enqueuebare Einstiegspunkt eines Graphen.

```csharp
IJobRoot root = core.JobFactory.JobRoot(
    async ct => await Task.CompletedTask,
    name: "ImportRoot");

root.After(transform);
```

Die Root sollte der Terminalknoten bleiben. Sie ist das Element, das eine Queue übernimmt und enqueueed.
