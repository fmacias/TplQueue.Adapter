# IDataJobFactory

Verwenden Sie `IDataJobFactory` für payloadfähige Jobs und Payload-Roots.

```csharp
var dataRoot = core.DataJobFactory.DataJobRoot(payload, handler, name: "MeasurementRoot");
var dataChild = core.DataJobFactory.DataJob(payload, handler, name: "MeasurementChild");
dataRoot.After(dataChild);
```
