# IDataJobFactory

Use `IDataJobFactory` for payload-aware jobs and payload roots.

```csharp
var dataRoot = core.DataJobFactory.DataJobRoot(payload, handler, name: "MeasurementRoot");
var dataChild = core.DataJobFactory.DataJob(payload, handler, name: "MeasurementChild");
dataRoot.After(dataChild);
```
