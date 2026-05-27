# Observability

Jede Queue implementiert `IObservable<IJobEvent>`, wodurch TplQueue beobachtbar wird, ohne die Ausführungspipeline an ein bestimmtes Logging- oder UI-Framework zu koppeln.

Nützliche Event-Felder sind unter anderem `Status`, `JobInfo.Id`, `JobInfo.Name`, `JobInfo.CrossQueueId`, `Timestamp`, `RetryCount` und `Exception`.
