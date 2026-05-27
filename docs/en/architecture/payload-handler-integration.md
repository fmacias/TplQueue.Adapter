# Payload Handler Integration

Core exposes `IHandler` as the public execution contract used by `IDataJobFactory`.

For cache hydration and plugin-style resolution, prefer adapter-side `IApi.RegisterPayloadHandler(...)` registration so payload `PayloadId` values remain the stable persisted handler keys.
