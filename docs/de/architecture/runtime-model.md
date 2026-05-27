# Runtime-Modell

`TplQueue.Core` verantwortet das Runtime-Modell für:

- die Graph-Komposition mit `Job` und `JobRoot`
- queuebasierten Dispatch über `IQ`, `IParallelQ`, `IFifoQ` und `ICacheQ`
- begrenzte Parallelität mit `SemaphoreSlim`
- strikt FIFO-basierte Ausführung, wenn erforderlich
- Retry-Selection auf Queue- oder Root-Ebene
- beobachtbare Ausführung über `IObservable<IJobEvent>`
