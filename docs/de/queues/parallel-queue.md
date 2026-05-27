# Parallel-Queue

`IParallelQ` führt Arbeit mit begrenzter Parallelität aus. Die Grenze wird intern über `SemaphoreSlim` durchgesetzt.

```csharp
IParallelQ parallelQ = core.QFactory.Parallel(
    Guid.NewGuid(),
    "parallel-main",
    maxParallelism: 4,
    logger: parallelLogger);
```

`IParallelQ` unterstützt auch FIFO-begrenztes Enqueueing, wenn ein Teilbereich der Arbeit geordnet bleiben muss.
