# IJobFactory

Use `IJobFactory` to create regular jobs and job roots.

```csharp
IJob validate = core.JobFactory.Job(async ct => await Task.CompletedTask, name: "Validate");
IJobRoot root = core.JobFactory.JobRoot(async ct => await Task.CompletedTask, name: "Root");
root.After(validate);
```
