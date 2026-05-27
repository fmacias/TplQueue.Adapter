# Job Graphs

TplQueue graphs are dependency-aware and root-terminal by design.

## Root-terminal rule

The current runtime prevents non-root jobs from depending on an `IJobRoot`. In practice, `job.After(root)` is invalid because the enqueued root must remain the last node in the graph.

Use one of these valid forms instead:

```csharp
extract.Then(transform);
transform.Then(root);
```
