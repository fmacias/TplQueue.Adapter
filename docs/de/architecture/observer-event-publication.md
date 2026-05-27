# Observer Event Publication

Every `IQ` implements `IObservable<IJobEvent>`. `OnError` is reserved for fatal dispatcher failures, while ordinary job failures are published through `OnNext`.
