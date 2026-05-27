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
