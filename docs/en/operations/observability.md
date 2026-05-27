# Observability

Every queue implements `IObservable<IJobEvent>`, which makes TplQueue observable without coupling the execution pipeline to a specific logging or UI framework.

Useful event fields include `Status`, `JobInfo.Id`, `JobInfo.Name`, `JobInfo.CrossQueueId`, `Timestamp`, `RetryCount`, and `Exception`.
