# TplQueue.Adapter

`TplQueue.Adapter` contains the integration packages that complement `TplQueue.Core`.

It provides the top-level `API` facade and the concrete modules for retry policies, observers, cache implementations, serialization, and Microsoft dependency injection.

## Install

The repository publishes several packages. The main facade package is:

```bash
dotnet add package Fmacias.TplQueue --version 0.1.0-preview.1
```

For ASP.NET Core or generic-host microsoft DI integration, install:

```bash
dotnet add package Fmacias.TplQueue.Microsoft.DependencyInjection --version 0.1.0-preview.1
```

Install the companion adapter modules only when your application needs them.

## Repository modules

- [Fmacias.TplQueue](src/Fmacias.TplQueue/README.md)
- [Fmacias.TplQueue.Cache.Abstract](src/Fmacias.TplQueue.Cache.Abstract/README.md)
- [Fmacias.TplQueue.Cache.MemCache](src/Fmacias.TplQueue.Cache.MemCache/README.md)
- [Fmacias.TplQueue.Microsoft.DependencyInjection](src/Fmacias.TplQueue.Microsoft.DependencyInjection/README.md)
- [Fmacias.TplQueue.Observers](src/Fmacias.TplQueue.Observers/README.md)
- [Fmacias.TplQueue.RetryPolicies](src/Fmacias.TplQueue.RetryPolicies/README.md)
- [Fmacias.TplQueue.Serialization.SystemTextJson](src/Fmacias.TplQueue.Serialization.SystemTextJson/README.md)
- [Fmacias.TplQueue.Serialization.Xml](src/Fmacias.TplQueue.Serialization.Xml/README.md)

## Documentation map

Repository-level documentation now lives under [docs/](docs/index.md):

- [Getting Started](docs/usage/index.md)
- [Architecture](docs/architecture/index.md)
- [Development](docs/development/index.md)
- [Operations](docs/operations/index.md)
- [Reference hub](docs/reference.md)

## Documentation rebuild prompt

If you want to rebuild or refresh the repository documentation with an agent, use [docs/Agents.md](docs/Agents.md) as the instruction file and start from this prompt:

```text
Rebuild the TplQueue.Adapter documentation under docs/ according to docs/Agents.md. Treat README.md as the concise repository and package entry point, rewrite docs/usage/index.md as the main “TplQueue with .NET and ASP.NET” tutorial, keep docs/reference.md as a compact reference hub, preserve the current MkDocs-friendly structure, and ground every API example in the current TplQueue.Adapter source code and the runnable samples in TplQueue.Usage.
```

## Quick operations

Run the repository test surface:

```powershell
dotnet test .\TplQueue.Adapter.sln
```

Run repository coverage:

```powershell
.\coverage.ps1
.\coverage.ps1 -EnforceBaseline
```

Build local preview packages:

```powershell
.\pack-local.ps1
```

For signed official packaging and the public publish flow, use the coordinated workspace scripts documented in [docs/operations/index.md](docs/operations/index.md).

## Public usage repository

For package-based samples, public integration tests, and observer-facing validation, see [TplQueue.Usage](https://github.com/fmacias/TplQueue.Usage).

## License

`TplQueue.Adapter` is distributed under the MIT license.

`TplQueue.Core`, which the adapter complements, is distributed separately under its own package-specific license terms. The published Core binaries are publicly consumable, but the Core source repository, private unit tests, and private integration tests remain outside the public repositories and require separate approval.
