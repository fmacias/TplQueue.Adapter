# Extending Observers

Custom observers should implement `IObserver<IJobEvent>` and remain operationally lightweight.

Keep `OnNext` fast, forward heavy work elsewhere, and treat `OnError` as a fatal dispatcher signal rather than the normal job-failure path.
