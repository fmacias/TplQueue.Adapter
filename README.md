# TplQueue.Adapter

This repository contains MIT-licensed adapter components for TplQueue. It is intended to be used together with TplQueue.Core (EULA) via NuGet packages or project references.

## Modules
- TplQueue: The main adapter that wires Core to abstractions and concrete integration points.
- Abstractions: Public contracts and interfaces used by Core and adapter modules.
- RetryPolicies: Common retry policy implementations and options.
- Cache.Abstract: Cache contracts and DTOs used by caching integrations.
- Serialization.SystemTextJson: JSON serialization helpers for runner graphs and payloads.
- Microsoft.DependencyInjection: DI registrations and extensions.
- Observers.ViewModel: Observer utilities and view model notifications.

See each module's `docs/readme.md` for details.

## Workspace solution (optional)
This repo builds standalone. If you also clone the umbrella workspace `WorkspaceTplQueue`, this repo will automatically import the shared `Directory.Build.props` from `..\\WorkspaceTplQueue\\Directory.Build.props` via its local `Directory.Build.props`.
The import is conditional; if the workspace folder is not present, nothing changes.
