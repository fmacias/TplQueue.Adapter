# Core / Adapter Boundary

`TplQueue.Core` owns execution semantics. `TplQueue.Adapter` owns integration surfaces such as named queue creation, retry dictionaries, serializer modules, cache modules, built-in observers, and DI helpers.
