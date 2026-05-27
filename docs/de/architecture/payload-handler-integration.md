# Payload-Handler-Integration

Core stellt `IHandler` als öffentlichen Execution-Contract bereit, der von `IDataJobFactory` verwendet wird.

Für Cache-Hydration und pluginartige Auflösung sollten Sie die adapterseitige Registrierung über `IApi.RegisterPayloadHandler(...)` bevorzugen, damit `PayloadId`-Werte als stabile persistierte Handler-Schlüssel erhalten bleiben.
