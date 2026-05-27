# Cache and Persistence Boundary

Persistence providers are not implemented directly inside `TplQueue.Core`.

Core provides payload-aware jobs, `ICacheQ`, cache queue orchestration hooks, and payload-handler support.

Adapter provides `IDataJobCache` abstractions, concrete cache implementations, serializer modules, and payload-handler registration support.
