# Dependency Injection

Für ASP.NET und andere Hosts auf Basis von `Microsoft.Extensions.DependencyInjection` ist der empfohlene Einstiegspunkt `Fmacias.TplQueue.Microsoft.DependencyInjection`.

```csharp
var api = API.Create(CoreApi.Create(), retryPolicies, dispatchers);
services.AddTplQueue(api, retryPolicies, dispatchers);
```
