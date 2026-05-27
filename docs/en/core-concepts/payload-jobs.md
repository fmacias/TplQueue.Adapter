# Payload Jobs

Use `IDataJob` and `IDataJobRoot` when execution must carry payload data together with the work item.

Publicly, the model is exposed through:

- `IDataJob`
- `IDataJob<T>`
- `IDataJobRoot`
- `IDataJobRoot<T>`
- `IPayload`
- `IHandler`

When payload jobs must be dehydrated and hydrated through adapter-side caches, register the handler behavior through the Adapter `IApi` facade. Core executes the public `IHandler` contract, while Adapter owns payload-handler registration and hydration-time resolution.
