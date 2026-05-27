# Extending Queues

Most applications should use the public queue factories instead of subclassing queue implementations directly.

Prefer `IQFactory` for runtime queues and `IQFactoryAdapter` for named queue creation from configuration.
