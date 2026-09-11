# Payload-Handler-Integration

Core stellt `IHandler` als öffentlichen Execution-Contract bereit, der von `IDataJobFactory` verwendet wird.

Für Cache-Hydration und pluginartige Auflösung sollten Sie die adapterseitige Registrierung über `IApi.RegisterPayloadHandler(...)` bevorzugen, damit `IPayload.HandlerKey` als stabiler persistierter Routing-Schlüssel verwendet wird. `IPayload.PayloadId` identifiziert die einzelne Payload-Instanz und ist kein Handler-Schlüssel.
