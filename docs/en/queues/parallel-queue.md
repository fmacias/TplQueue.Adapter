# Parallel Queue

`IParallelQ` executes work with bounded concurrency. The bound is enforced internally through `SemaphoreSlim`.

```csharp
IParallelQ parallelQ = core.QFactory.Parallel(
    Guid.NewGuid(),
    "parallel-main",
    maxParallelism: 4,
    logger: parallelLogger);
```

`IParallelQ` also supports FIFO-scoped enqueueing when one subset of work must stay ordered.
