# Queues erweitern

Die meisten Anwendungen sollten die öffentlichen Queue-Factories verwenden, statt Queue-Implementierungen direkt zu subclassen.

Bevorzugen Sie `IQFactory` für Runtime-Queues und `IQFactoryAdapter` für die Erstellung benannter Queues aus Konfiguration.
