# AGENTS.md

## Context

You are working in the `TplQueue.Adapter` git repository, part of the overall `fmacias` workspace, which contains three separate git repositories:

- `TplQueue.Adapter`
- `TplQueue.Core`
- `TplQueue.Abstractions`

Apply the common instructions described at AGENtS.md file of the parent overall workspace `fmacias`.

Treat each repository as an independent git boundary even when code or packaging depends on another repository.

This repository contains:

- `src` for production packages
- `test` for repository test projects
- `TplQueue.Adapter.sln` as the main solution entry point
- `pack-local.ps1` as the local packaging pipeline

Production packages in this repository target `netstandard2.0`. Test projects target `net8.0`. `Directory.Build.props` enables modern C# and analyzers, but public API and runtime behavior must remain compatible with `netstandard2.0` unless the human explicitly requests otherwise.

The MIT-licensed wrapper package `Fmacias.TplQueue` is now a thin facade package. It depends on `Fmacias.TplQueue.Abstractions` and on repo-local adapter packages such as:

- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.Observers`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Serialization.SystemTextJson`
- `Fmacias.TplQueue.Serialization.Xml`

Other packages in this repository, such as `Fmacias.TplQueue.Cache.Abstract` and `Fmacias.TplQueue.Microsoft.DependencyInjection`, are supporting adapter modules and must be treated as first-class package boundaries.

When working inside a package folder, read the nearest package-level `AGENTS.md` as well. When working on the repository documentation under `docs/en/` or `docs/de/`, use [`docs/Agents.md`](docs/Agents.md) as the authoritative end-user documentation instruction file. The root `Agents.md` remains the primary repository-wide instruction set for code and repository work.
## Implementation Rules

## Current repository structure

The current adapter packages are:

- `Fmacias.TplQueue`
- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.Microsoft.DependencyInjection`
- `Fmacias.TplQueue.Observers`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Serialization.SystemTextJson`
- `Fmacias.TplQueue.Serialization.Xml`

The current repository test projects are:

- `Fmacias.TplQueue.Unit.Test`
- `Fmacias.TplQueue.Cache.Abstract.Test`
- `Fmacias.TplQueue.Cache.MemCache.Test`
- `Fmacias.TplQueue.Integration.Test`
- `Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test`
- `Fmacias.TplQueue.Observers.Unit.Test`
- `Fmacias.TplQueue.RetryPolicies.Unit.Test`
- `Fmacias.TplQueue.Serialization.SystemTextJson.Unit.Test`
- `Fmacias.TplQueue.Serialization.Xml.Unit.Test`

Prefer changes that stay inside the owning package and its matching test project.

## Important terminology

Use the terminology already present in the codebase.

The current core domain contracts are:

- `IJob`
- `IJobRoot`
- `IDataJob`
- `IDataJobRoot`
- `IParallelQ`
- `IFifoQ`
- `ICacheQ`

Use these names consistently in analysis, refactoring, implementation, comments, and documentation.

Do **not** rename these concepts or replace them with alternative terminology unless the human explicitly requests it.

If some older code or documentation still refers to previous names such as `TaskRunner`, `TaskRunnerRoot`, or related historical abstractions, treat those names as **legacy terminology**. Preserve compatibility where required, but prefer the current `Job`-based naming in new work.

Do not introduce parallel vocabulary for the same concept. For example, do not mix `job`, `task runner`, `work item`, or similar terms if they refer to the same abstraction.

Some parts of the codebase may still contain legacy names from earlier design iterations. Do not perform broad terminology migration unless explicitly requested. When modifying existing code, preserve local naming consistency while respecting the current official public contracts.

## Architectural intent

The purpose of these libraries is to provide reusable infrastructure for:

- controlled asynchronous and concurrent execution
- strict FIFO execution where required
- parallel dispatch where allowed
- retry-policy-driven execution
- observable execution flow through the Observer pattern
- optional payload handling through `IDataJob` and `IDataJobRoot`
- optional cache-backed persistence before enqueueing into memory-based dispatchers
- future integration with front-end monitoring systems such as:
  - web dashboards, for example React + SignalR
  - desktop UI applications
  - reactive front ends

A dispatcher queue acts as an in-memory buffer of `IJobRoot` elements. Depending on the concrete dispatcher, execution may be strict FIFO or parallel.

This architecture allows legacy applications to progressively externalize, monitor, and modernize asynchronous work without requiring an immediate full UI or architecture rewrite.

This workspace is intended to solve multithreading and concurrency-control problems by abstracting executable work into jobs (`IJob`, `IJobRoot`, `IDataJob`, `IDataJobRoot`) and dispatching them through queue-based components that support either strict FIFO or parallel execution policies.

## Test structure

Tests may be either:

- **Unit tests**: isolated tests, usually using mocks where appropriate
- **Integration tests**: tests that compose concrete dependencies and verify collaboration between real implementations, while still following a clear Arrange / Act / Assert structure

Integration tests must remain readable and useful to a human reviewer.


## Documentation work

For documentation tasks:

1. Treat this root file as the repository-wide operating guide.
2. Treat [`docs/Agents.md`](docs/Agents.md) as the authoritative instruction set for rebuilding or extending the end-user documentation tree under `docs/`.
   - The publishable source-of-truth trees are `docs/en/` and `docs/de/`.
   - `docs/Agents.md` is an instruction file only and must not be mirrored into public site output.
3. Keep `README.md` concise as the repository and package entry point.
4. Do not duplicate the detailed documentation-writing rules from `docs/Agents.md` in this root file.

---

## Constraints

- Do **not** change namespaces unless strictly necessary.
- Do **not** change public API signatures unless strictly necessary to fix a bug or implement the requested feature.
- Do **not** introduce new external dependencies.
- Keep changes understandable, consistent, and as small as reasonably possible.
- Preserve `.NET Standard 2.0` compatibility unless the human explicitly instructs otherwise.
- Prefer the existing project terminology and patterns over inventing new abstractions.
- Unless the task is review-only, apply changes directly in the workspace without asking for confirmation.

## Current contract and configuration notes

- Preserve `WaitAsync`, `IPayload.HandlerKey`, factory-based CacheQ creation and the moved `Then` extensions when modifying this source line.
- Configured queue retry-policy names are optional; null, empty, or whitespace selects NoRetry. An explicit root NoRetry overrides the queue policy; an unspecified root policy inherits it.
- DI settings are configuration-time builders. Registration captures independent option snapshots; later Upsert calls do not reconfigure the API.
- Follow the [coordinated API migration notes](docs/en/operations/api-migration.md). Public documentation continues to be published only from Adapter's language trees; this change does not alter the publishing boundary.

- Use `IQ.Subscribe` for all job lifecycle notifications, including Enqueued. `IQ.OnJobEventChanged` has been removed; do not reintroduce an inline event-handler route in Publish or enqueue.
- CacheQ owns a private cache observer and its subscription to the supplied queue. Terminal cache transitions run through that observer; enqueue and dispatch do not invoke it inline.
- Preserve `WaitAsync` as a wait for underlying queue work. It does not wait for observer delivery or cache acknowledgment. Slow subscribers on the shared hub can delay cache updates. CacheQ.Dispose unsubscribes and can leave queued notifications unapplied; it is not an acknowledgment barrier.
