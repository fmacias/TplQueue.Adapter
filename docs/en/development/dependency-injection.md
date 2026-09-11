# Dependency Injection

Register the adapter with an `ICoreApi` and your application's root configuration:

```csharp
using Fmacias.TplQueue.Core;
using Fmacias.TplQueue.Microsoft.DependencyInjection;

services.AddTplQueue(configuration, CoreApi.Create());
```

`AddTplQueue` reads `TplQueue:RetryPolicies` and `TplQueue:Queues`. See the [first queue](../getting-started/first-queue.md) for the JSON shape. Each configured queue requires a positive `MaxParallelism`. Its `RetryPolicy` name is optional; null, empty, or whitespace selects NoRetry. An unregistered nonblank policy name falls back to NoRetry. An omitted queue `Id` is generated once per settings instance; configure an explicit ID to retain it across restarts.

For code-based configuration, use `ITplQueueSettings.Upsert`:

```csharp
using Fmacias.TplQueue.Defaults;

services.AddTplQueue(settings =>
{
    settings.Upsert("retry", RetryPolicyOptions.Create(200, 3, 2));
    settings.Upsert("main", new QOptions(Guid.NewGuid(), 2, "retry"));
}, CoreApi.Create());
```

The dictionary overload is `AddTplQueue(coreApi, retryPolicies, queues, configuration)`. Configuration is optional; explicit dictionary entries override configured entries with the same name, ignoring case. Other configured entries are retained.

Registration exposes `IApi`, job/queue/retry/observer/serializer factories, `ITplQueueSettings`, both read-only option dictionaries, and transient JSON/XML serializers. The read-only dictionaries are the exact snapshots exposed by the registered API.

Settings are configuration-time builders. Registration captures independent option snapshots. A later `Upsert` on the settings does not reconfigure the API, factories or existing queues; concurrent settings mutation is unsupported.

For old overloads and configuration names, follow the [API migration notes](../operations/api-migration.md).
