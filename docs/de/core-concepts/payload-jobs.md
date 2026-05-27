# Payload Jobs

Verwenden Sie `IDataJob` und `IDataJobRoot`, wenn die Ausführung Payload-Daten zusammen mit dem Work Item transportieren muss.

Öffentlich wird das Modell über Folgendes bereitgestellt:

- `IDataJob`
- `IDataJob<T>`
- `IDataJobRoot`
- `IDataJobRoot<T>`
- `IPayload`
- `IHandler`

Wenn Payload-Jobs über adapterseitige Caches dehydriert und hydriert werden müssen, registrieren Sie das Handler-Verhalten über die Adapter-`IApi`-Facade. Core führt den öffentlichen `IHandler`-Contract aus, während Adapter die Payload-Handler-Registrierung und die Auflösung zur Hydrierungszeit übernimmt.
