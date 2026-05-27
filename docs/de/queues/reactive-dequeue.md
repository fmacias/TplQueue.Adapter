# Reaktive Dequeue-Strategie

TplQueue bevorzugt reaktive Signalisierung gegenüber kontinuierlichem Polling.

Die Runtime zielt auf `.NET Standard 2.0`, daher nutzt sie fokussierte Async-Koordinationsprimitiven wie `AsyncAutoResetEvent`, `AsyncOrderedQueue` und `SemaphoreSlim` statt Busy Loops.
