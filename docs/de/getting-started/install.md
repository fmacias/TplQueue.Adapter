# Installieren

Installieren Sie die Runtime- und Integrations-Packages, die zu Ihrem Host passen.

## Minimales Package-Set

Für ASP.NET oder jeden Host, der die DI-Helper verwenden möchte, installieren Sie:

```bash
dotnet add package Fmacias.TplQueue.Core --version 0.1.0-preview.1
dotnet add package Fmacias.TplQueue.Microsoft.DependencyInjection --version 0.1.0-preview.1
```

Fügen Sie die dünne öffentliche Facade hinzu, wenn Sie `API` explizit im Composition-Code erstellen möchten:

```bash
dotnet add package Fmacias.TplQueue --version 0.1.0-preview.1
```

## Optionale öffentliche Module

- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Serialization.SystemTextJson`
- `Fmacias.TplQueue.Observers`

Wählen Sie die optionalen Module nur dann aus, wenn Ihre Anwendung cachegestützten Dispatch, eingebaute Retry-Policies, Serializer-Helper oder Observer-Integrationen benötigt.

## Lizenzhinweis

`TplQueue.Adapter`-Module sind MIT-lizenziert. `TplQueue.Core` ist als Package öffentlich konsumierbar, steht aber nicht unter MIT. Halten Sie die rechtliche und Support-Grenze klar, indem Sie Folgendes lesen:

- [Core-Lizenz](../license/core-license.md)
- [MIT-Adapter-Module](../license/mit-adapter-modules.md)
- [Produktive Nutzung](../license/production-use.md)
