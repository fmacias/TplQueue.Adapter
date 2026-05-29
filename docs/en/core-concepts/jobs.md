# Jobs

`IJob` represents one executable unit inside a graph.

Create jobs through `IJobFactory`:

```csharp
ICoreApi core = CoreApi.Create();

IJob extract = core.JobFactory.Job(
    async ct => await Task.CompletedTask,
    name: "Extract");

IJob transform = core.JobFactory.Job(
    async ct => await Task.CompletedTask,
    name: "Transform");
```

Dependencies are expressed explicitly through `After(...)` or the fluent `Then(...)` extension:

```csharp
extract.Then(transform);
```

## Public contract source

`IJob` itself is owned by the public `TplQueue.Abstractions` repository:

```csharp
public interface IJob : IJobNode
{
    IJob After(params IJob[] previousTasks);
}
```

Public source links:

- [`IJob`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IJob.cs)
- [`IJobFactory`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IJobFactory.cs)
