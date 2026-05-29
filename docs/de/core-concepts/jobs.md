# Jobs

`IJob` repräsentiert eine ausführbare Einheit innerhalb eines Graphen.

Erstellen Sie Jobs über `IJobFactory`:

```csharp
ICoreApi core = CoreApi.Create();

IJob extract = core.JobFactory.Job(
    async ct => await Task.CompletedTask,
    name: "Extract");

IJob transform = core.JobFactory.Job(
    async ct => await Task.CompletedTask,
    name: "Transform");
```

Abhängigkeiten werden explizit über `After(...)` oder die fluente `Then(...)`-Extension ausgedrückt:

```csharp
extract.Then(transform);
```

## Öffentliche Contract-Quelle

`IJob` selbst gehört zum öffentlichen Repository `TplQueue.Abstractions`:

```csharp
public interface IJob : IJobNode
{
    IJob After(params IJob[] previousTasks);
}
```

Öffentliche Source-Links:

- [`IJob`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IJob.cs)
- [`IJobFactory`](https://github.com/fmacias/TplQueue.Abstractions/blob/main/src/Contracts/IJobFactory.cs)
