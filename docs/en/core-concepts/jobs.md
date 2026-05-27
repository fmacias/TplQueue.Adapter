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
