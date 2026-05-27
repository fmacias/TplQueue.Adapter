# Runtime Model

`TplQueue.Core` owns the runtime model for:

- `Job` and `JobRoot` graph composition
- queue-based dispatch through `IQ`, `IParallelQ`, `IFifoQ`, and `ICacheQ`
- bounded concurrency using `SemaphoreSlim`
- strict FIFO execution where required
- queue-level or root-level retry-policy selection
- observable execution through `IObservable<IJobEvent>`
