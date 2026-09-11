# Payload Handler Integration

Core exposes `IHandler` as the public execution contract used by `IDataJobFactory`.

For cache hydration and plugin-style resolution, prefer adapter-side `IApi.RegisterPayloadHandler(...)` registration so payload `HandlerKey` values remain the stable persisted routing keys. `PayloadId` identifies each payload instance.
