# Dependency Injection

For ASP.NET and other `Microsoft.Extensions.DependencyInjection` hosts, the recommended entry point is `Fmacias.TplQueue.Microsoft.DependencyInjection`.

```csharp
var api = API.Create(CoreApi.Create(), retryPolicies, dispatchers);
services.AddTplQueue(api, retryPolicies, dispatchers);
```
