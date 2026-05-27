# FIFO-Queue

`IFifoQ` erzwingt serialisierte Ausführung für die gesamte Queue.

```csharp
IFifoQ fifoQ = core.QFactory.Fifo(
    Guid.NewGuid(),
    "fifo-main",
    logger: fifoLogger);
```

Verwenden Sie diese Queue, wenn die Reihenfolge Teil des Vertrags ist und nicht nur ein Optimierungsdetail.
