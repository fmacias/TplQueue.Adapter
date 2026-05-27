# Observer erweitern

Eigene Observer sollten `IObserver<IJobEvent>` implementieren und operativ leichtgewichtig bleiben.

Halten Sie `OnNext` schnell, leiten Sie schwere Arbeit an andere Komponenten weiter und behandeln Sie `OnError` als fatales Dispatcher-Signal und nicht als normalen Job-Fehlerpfad.
