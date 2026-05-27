# IJobFactory

Verwenden Sie `IJobFactory`, um reguläre Jobs und Job Roots zu erstellen.

```csharp
IJob validate = core.JobFactory.Job(async ct => await Task.CompletedTask, name: "Validate");
IJobRoot root = core.JobFactory.JobRoot(async ct => await Task.CompletedTask, name: "Root");
root.After(validate);
```
