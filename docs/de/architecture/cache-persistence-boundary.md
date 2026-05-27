# Cache- und Persistenzgrenze

Persistenzprovider werden nicht direkt innerhalb von `TplQueue.Core` implementiert.

Core stellt payloadfähige Jobs, `ICacheQ`, Orchestrierungs-Hooks für Cache-Queues und Payload-Handler-Support bereit.

Adapter stellt `IDataJobCache`-Abstraktionen, konkrete Cache-Implementierungen, Serializer-Module und Support für die Payload-Handler-Registrierung bereit.
