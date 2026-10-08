# AGENTS.md

## Repository context

You are working in the `TplQueue.Adapter` git repository, part of the overall `fmacias` workspace.

Related repositories include:

- `TplQueue.Adapter`
- `TplQueue.Core`
- `TplQueue.Abstractions`
- `TplQueue.Usage`

Treat every repository as an independent git boundary even when code, packages, documentation, tests, or samples depend on another repository.

When this repository is checked out inside the overall `fmacias` workspace and a parent workspace `AGENTS.md` is available, apply those common instructions first.

This file adds the repository-specific instructions for `TplQueue.Adapter`.

The current repository contains:

- `src` for production packages
- `test` for repository test projects
- `TplQueue.Adapter.sln` as the main solution entry point
- `pack-local.ps1` as the local packaging pipeline

Production packages target `netstandard2.0`.

Test projects target `net8.0`.

`Directory.Build.props` may enable modern C# language features and analyzers, but public API and runtime behavior in production packages must remain compatible with `netstandard2.0` unless the human explicitly requests otherwise.

When working inside a package folder, read the nearest package-level `AGENTS.md` in addition to this file.

When working under `docs/`, also read [`docs/Agents.md`](docs/Agents.md).

`docs/Agents.md` is the authoritative instruction file for end-user documentation work.

This root `AGENTS.md` remains authoritative for repository-wide architecture, package boundaries, terminology, compatibility, current contracts, and runtime behavior.

---

## Instruction and source-of-truth hierarchy

Apply instructions and evidence in this order:

1. explicit human task
2. applicable workspace-level `AGENTS.md`
3. this repository root `AGENTS.md`
4. nearest package-level `AGENTS.md`
5. documentation-specific instructions such as `docs/Agents.md`
6. current source code
7. current tests
8. applicable migration and architecture documentation
9. installed Agent Skills as workflow guidance

Skills complement repository instructions.

Skills do not replace repository-local source of truth.

If a packaged Skill describes an older baseline than the current repository, follow the repository's current `AGENTS.md`, source, tests, and applicable documentation.

Do not silently reconcile contradictory instructions or cross-repository contracts when the intended source of truth is unclear.

---

## Agent Skills

When the corresponding marketplace plugins are installed, use their Skills as reusable workflow guidance.

### Generic engineering Skills

The `engineering` plugin provides generic workflows including:

- `review-csharp`
- `review-concurrency`
- `review-public-api`
- `architecture-review`
- `refactor-code`
- `implement-feature`
- `testing-dotnet`
- `validate-dotnet-change`
- `align-cross-repository-documentation`
- `document-csharp-api`
- `summarize-staged-commit`

Use those Skills for generic engineering procedure rather than duplicating their rules in this file.

### TplQueue.Adapter Skills

The `tplqueue-adapter` plugin provides repository/domain-specific workflows:

- `review-adapter-architecture`
- `modify-adapter-public-contracts`
- `modify-adapter-retry-policy`
- `modify-adapter-observer-flow`
- `modify-adapter-cacheq-lifecycle`
- `modify-adapter-di-registration`
- `update-adapter-public-documentation`

Use the relevant Adapter Skill when work enters one of those domains.

Package-level `AGENTS.md` files may define additional constraints that are more specific than these Skills.

Additional package-specific Skills may later be added to the `tplqueue-adapter` plugin when a package contains a sufficiently distinct reusable workflow or behavioral model.

Do not assume that every Adapter package requires its own Skill.

---

## Current repository structure

The current production packages are:

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

Prefer changes that stay inside the owning production package and its matching test project.

Treat every production package as a first-class package boundary.

---

## Package architecture

The MIT-licensed wrapper package `Fmacias.TplQueue` is a thin facade package.

It depends on `Fmacias.TplQueue.Abstractions` and repo-local Adapter packages such as:

- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.Observers`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Serialization.SystemTextJson`
- `Fmacias.TplQueue.Serialization.Xml`

Other packages, including:

- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Microsoft.DependencyInjection`

are supporting Adapter modules and must also be treated as first-class package boundaries.

Do not move specialized implementation into `Fmacias.TplQueue` merely for convenience.

Do not introduce circular project or package dependencies.

Respect ownership between Adapter subcomponents.

If a requested change requires modification of Core, Abstractions, Usage, or another repository, identify that cross-repository requirement explicitly and respect the independent git boundary.

---

## Related repository: TplQueue.Usage

`TplQueue.Usage` is a separate consumer/integration repository.

It contains samples, executable usage scenarios, integration examples, and other consumer-side demonstrations of TplQueue components.

Adapter changes may affect Usage, particularly when changing:

- public APIs
- queue construction
- observer contracts
- CacheQ behavior
- retry configuration
- dependency-injection configuration
- serialization behavior
- public integration guidance

When an Adapter change affects Usage:

1. identify the impact explicitly;
2. do not place Usage-specific implementation inside Adapter;
3. do not modify `TplQueue.Usage` as part of an Adapter-only task unless the human explicitly requests a cross-repository change;
4. treat `TplQueue.Usage` as an independent git boundary;
5. when available, use the `tplqueue-usage` plugin for Usage-specific workflows.

The Adapter plugin may understand that Usage is a consumer, but detailed Usage architecture belongs to the `tplqueue-usage` plugin.

---

## Domain terminology

Use the terminology established by the current codebase.

The current core domain contracts include:

- `IJob`
- `IJobRoot`
- `IDataJob`
- `IDataJobRoot`
- `IParallelQ`
- `IFifoQ`
- `ICacheQ`

Use these names consistently in:

- analysis
- implementation
- refactoring
- tests
- comments
- XML documentation
- public documentation

Do not rename these concepts or introduce alternative terminology unless the human explicitly requests it.

Older names such as `TaskRunner`, `TaskRunnerRoot`, and related previous abstractions are legacy terminology.

Preserve legacy terminology where compatibility or existing source requires it, but use the current Job-based terminology for new work.

Do not introduce parallel vocabulary such as `job`, `task runner`, and `work item` for the same abstraction.

Do not perform broad terminology migrations unless explicitly requested.

---

## Architectural intent

TplQueue provides reusable infrastructure for:

- controlled asynchronous and concurrent execution
- strict FIFO execution where required
- parallel dispatch where allowed
- retry-policy-driven execution
- observable execution flow through the Observer pattern
- optional payload handling through `IDataJob` and `IDataJobRoot`
- optional cache-backed persistence before enqueueing into memory-based dispatchers
- integration with monitoring/front-end systems without coupling UI concerns into execution infrastructure

Potential consumers include:

- web dashboards
- SignalR-based applications
- desktop applications
- reactive front ends
- legacy applications progressively externalizing asynchronous work

A dispatcher queue acts as an in-memory buffer of `IJobRoot` elements.

Depending on the concrete dispatcher, execution may be strict FIFO or parallel.

The architecture is intended to let applications progressively externalize, control, observe, and modernize asynchronous work without requiring an immediate full application or UI rewrite.

Preserve this architectural direction unless the explicit task deliberately changes it.

---

## Repository constraints

- Do not change namespaces unless strictly necessary.
- Do not change public API signatures unless strictly necessary to fix a bug, implement the requested feature, or perform an explicitly requested migration.
- Do not introduce new external dependencies.
- Preserve `netstandard2.0` compatibility for production packages unless the human explicitly instructs otherwise.
- Prefer existing project terminology and patterns over inventing new abstractions.
- Keep changes understandable, cohesive, and as small as reasonably possible.
- Respect package ownership.
- Do not silently redesign architecture.
- Do not silently change cross-repository contracts.
- Unless the task is review-only, apply requested changes directly when the intent is sufficiently clear.

Generic review, implementation, refactoring, testing, validation, concurrency, documentation, and commit-message procedures belong to the common workspace instructions and the `engineering` plugin when available.

---

## Current public contract and migration baseline

Preserve the current source-line behavior for:

- `WaitAsync`
- `IPayload.HandlerKey`
- factory-based CacheQ creation
- the current moved `Then` extensions

When modifying these areas, follow the coordinated API migration guidance in:

[`docs/en/operations/api-migration.md`](docs/en/operations/api-migration.md)

These are current repository contracts.

They may be deliberately changed by a later feature, bug fix, or API migration.

Do not treat them as immutable historical guarantees, but do not change them accidentally.

When one of these contracts deliberately changes, update coherently:

- implementation
- tests
- this `AGENTS.md` when its baseline changes
- affected package-level instructions
- migration guidance
- affected public documentation
- affected consumers

Identify cross-repository impact explicitly.

---

## Retry-policy semantics

Configured queue retry-policy names are optional.

Current behavior is:

- `null` selects NoRetry
- empty selects NoRetry
- whitespace selects NoRetry
- an explicit root NoRetry overrides the queue retry policy
- an unspecified root retry policy inherits the queue retry policy

Preserve the distinction between:

- unspecified root retry policy
- explicit root NoRetry
- named retry policy

Do not accidentally collapse these states while modifying:

- retry factories
- configuration
- dependency injection
- root policy resolution
- queue policy resolution
- retry execution

If these semantics deliberately change, update source, tests, instructions, public documentation, and affected consumers together.

---

## Dependency-injection configuration semantics

DI settings are configuration-time builders.

Registration captures independent option snapshots.

Later `Upsert` calls do not reconfigure APIs or services captured by previous registrations.

Do not accidentally introduce shared mutable configuration state that changes previously captured registrations.

When modifying DI behavior, verify as applicable:

- registration independence
- builder mutation after registration
- multiple registrations
- factory behavior
- service lifetime
- resource ownership
- disposal ownership

---

## Observer notification semantics

Use `IQ.Subscribe` for all Job lifecycle notifications, including `Enqueued`.

`IQ.OnJobEventChanged` has been removed.

Do not reintroduce `IQ.OnJobEventChanged`.

Do not reintroduce an inline event-handler notification route inside Publish or enqueue.

Lifecycle notification delivery uses the Observer/subscription path.

Preserve the asynchronous behavior and ordering guarantees actually established by the current implementation and tests.

Do not assume stronger ordering, acknowledgment, or completion guarantees than the repository provides.

Changes to observable/public event behavior may affect `TplQueue.Usage` monitoring and integration scenarios. Identify that consumer impact explicitly.

---

## CacheQ lifecycle semantics

CacheQ owns:

- a private cache observer
- that observer's subscription to the supplied queue

Terminal cache transitions run through that observer.

Enqueue and dispatch do not invoke terminal cache transitions inline.

### WaitAsync

`WaitAsync` waits for underlying queue work.

`WaitAsync` does not wait for:

- Observer delivery
- cache acknowledgment

Slow subscribers on the shared Observer hub can delay cache updates.

Do not accidentally strengthen `WaitAsync` into an Observer-delivery or cache-acknowledgment barrier.

### Dispose

`CacheQ.Dispose` unsubscribes its observer.

Queued notifications can therefore remain unapplied after unsubscription or disposal.

`CacheQ.Dispose` is not an acknowledgment barrier.

Do not accidentally strengthen `Dispose` into a guarantee that all queued Observer/cache work has completed.

Do not bypass the Observer path merely to force synchronous cache state.

---

## Tests

Tests may be:

- **unit tests** — isolated tests, normally using mocks where appropriate;
- **integration tests** — tests that compose concrete collaborating implementations.

Integration tests must remain readable and useful to a human reviewer.

Prefer changing the matching test project for the production package being modified.

When repository-specific behavioral invariants in this file are affected, update or add tests that demonstrate the intended behavior.

Generic test methodology belongs to the common workspace instructions and the `testing-dotnet` Skill when available.

---

## Documentation

For documentation work:

1. apply this root `AGENTS.md` for repository-wide architecture and current behavior;
2. apply [`docs/Agents.md`](docs/Agents.md) for detailed end-user documentation rules;
3. treat `docs/en/` and `docs/de/` as the publishable source-of-truth documentation trees;
4. treat `docs/Agents.md` as an instruction file only;
5. do not mirror `docs/Agents.md` into public site output;
6. keep `README.md` concise as the repository/package entry point;
7. do not duplicate detailed documentation-writing rules from `docs/Agents.md` in this root file;
8. preserve Adapter as the current public documentation publishing boundary;
9. when public behavior changes, update source, tests, migration guidance, and public documentation coherently.

When documentation must align with another repository, use the cross-repository documentation workflow and do not silently normalize contradictions.

Usage-specific documentation remains owned by `TplQueue.Usage`, even when Adapter documentation references Usage as a consumer.

---

## Validation

For production-code changes, validate the affected scope.

As applicable:

- build affected projects
- run relevant unit tests
- run relevant integration tests
- validate local packaging with `pack-local.ps1` when package output or dependency composition is affected
- report any validation step that could not be run

Use `validate-dotnet-change` from the `engineering` plugin when available for the generic validation workflow.

---

## Change-scope guidance

Prefer the smallest coherent change that satisfies the requested behavior.

A change should normally remain within:

- the owning Adapter package
- its matching tests
- directly affected documentation

Expand the scope only when the actual contract or architecture requires it.

If another Adapter package is affected, identify why.

If Core, Abstractions, or Usage is affected, identify the cross-repository impact explicitly rather than silently expanding an Adapter-only task.