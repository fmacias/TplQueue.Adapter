# FIFO Queue

`IFifoQ` enforces serialized execution for the whole queue.

```csharp
IFifoQ fifoQ = core.QFactory.Fifo(
    Guid.NewGuid(),
    "fifo-main",
    logger: fifoLogger);
```

Use this queue when ordering is part of the contract and not only an optimization detail.
