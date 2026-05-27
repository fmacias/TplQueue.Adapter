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

Repository-level documentation now lives under the multilingual source-of-truth tree:

- [English landing page](docs/en/index.md)
- [German landing page](docs/de/index.md)
- [English getting started](docs/en/getting-started/index.md)
- [English core concepts](docs/en/core-concepts/index.md)
- [English queues](docs/en/queues/index.md)
- [English architecture](docs/en/architecture/index.md)
- [English development](docs/en/development/index.md)
- [English operations](docs/en/operations/index.md)
- [English reference hub](docs/en/reference/index.md)
- [English license section](docs/en/license/index.md)

`docs/Agents.md` is the repository-local instruction file for rebuilding those documentation trees. It is not part of the published documentation and must not be mirrored into `fmacias.github.io`.

## Documentation rebuild prompt

If you want to rebuild or refresh the repository documentation with an agent, use [docs/Agents.md](docs/Agents.md) as the instruction file and start from this prompt:

```text
Rebuild the TplQueue.Adapter documentation under docs/en/ and docs/de/ according to docs/Agents.md. Treat README.md as the concise repository and package entry point, keep docs/en/ and docs/de/ structurally aligned, use docs/en/getting-started/index.md as the main entry for onboarding, keep docs/en/reference/index.md as a compact reference hub, and ground every API example in the current TplQueue.Adapter source code and the runnable samples in TplQueue.Usage.
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

For signed official packaging and the public publish flow, use the coordinated workspace scripts documented in [docs/en/operations/index.md](docs/en/operations/index.md).

## Public usage repository

For package-based samples, public integration tests, and observer-facing validation, see [TplQueue.Usage](https://github.com/fmacias/TplQueue.Usage).

## License

`TplQueue.Adapter` is distributed under the MIT license.

`TplQueue.Core`, which the adapter complements, is distributed separately under its own package-specific license terms. The published Core binaries are publicly consumable, but the Core source repository, private unit tests, and private integration tests remain outside the public repositories and require separate approval.
