# AGENTS.md

## Context

You are working in the `TplQueue.Adapter` git repository, part of the overall `fmacias` workspace, which contains three separate git repositories:

- `TplQueue.Adapter`
- `TplQueue.Core`
- `TplQueue.Abstractions`

Treat each repository as an independent git boundary even when code or packaging depends on another repository.

This repository contains:

- `src` for production packages
- `test` for repository test projects
- `TplQueue.Adapter.sln` as the main solution entry point
- `pack-local.ps1` as the local packaging pipeline

Production packages in this repository target `netstandard2.0`. Test projects target `net8.0`. `Directory.Build.props` enables modern C# and analyzers, but public API and runtime behavior must remain compatible with `netstandard2.0` unless the human explicitly requests otherwise.

The MIT-licensed wrapper package `Fmacias.TplQueue` is now a thin facade package. It depends on `Fmacias.TplQueue.Abstractions` and on repo-local adapter packages such as:

- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.Log`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Serialization.SystemTextJson`
- `Fmacias.TplQueue.Observers.ViewModel`

Other packages in this repository, such as `Fmacias.TplQueue.Cache.Abstract` and `Fmacias.TplQueue.Microsoft.DependencyInjection`, are supporting adapter modules and must be treated as first-class package boundaries.

When working inside a package folder, read the nearest package-level `AGENTS.md` as well. When working on the repository documentation under `docs/en/` or `docs/de/`, use [`docs/Agents.md`](docs/Agents.md) as the authoritative end-user documentation instruction file. The root `Agents.md` remains the primary repository-wide instruction set for code and repository work.

## Current repository structure

The current adapter packages are:

- `Fmacias.TplQueue`
- `Fmacias.TplQueue.Cache.Abstract`
- `Fmacias.TplQueue.Cache.MemCache`
- `Fmacias.TplQueue.Log`
- `Fmacias.TplQueue.Microsoft.DependencyInjection`
- `Fmacias.TplQueue.Observers.ViewModel`
- `Fmacias.TplQueue.RetryPolicies`
- `Fmacias.TplQueue.Serialization.SystemTextJson`

The current repository test projects are:

- `Fmacias.TplQueue.Unit.Test`
- `Fmacias.TplQueue.Cache.Abstract.Test`
- `Fmacias.TplQueue.Cache.MemCache.Test`
- `Fmacias.TplQueue.Log.Unit.Test`
- `Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test`
- `Fmacias.TplQueue.Observers.ViewModel.Unit.Test`
- `Fmacias.TplQueue.RetryPolicies.Unit.Test`
- `Fmacias.TplQueue.Serialization.SystemTextJson.Unit.Test`

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

## Documentation consistency

If you find contradictions, obsolete terminology, or architectural inconsistencies in this document or in nearby documentation, report them clearly in your final response.

---

## General operating rules

1. Work only within the scope requested by the human:
   - **Review** = analyze and report
   - **Refactor** = improve existing code without changing intended behavior
   - **Implementation** = fix a bug or add a feature

2. Prefer small, safe, understandable changes unless the human explicitly asks for a broader refactor.

3. Preserve the current architectural style unless there is a clear defect, contradiction, or explicit requirement to change it.

4. Do not silently redesign the library.

5. Keep public behavior stable unless a bug fix or the explicit task requires a change.

6. Respect the existing architecture, naming, layering, and design intent before proposing broader changes.

---

## Rules for code review

When asked to perform a **code review**, follow this process:

1. Verify that the changes to be reviewed are staged in the relevant git repository.
   - If they are not staged, stop the review and inform the human.
   - Reason: staged changes are easier to inspect, discuss, and revert safely.

2. Verify the relevant project-level configuration files, such as `.props`, solution-level settings, or project settings, to confirm that:
   - the C# language version is appropriate for `.NET Standard 2.0`
   - the project configuration is consistent with the intended compatibility targets

   If the configuration is inconsistent, stop and report the issue first.

3. Review the code according to these principles:
   - SOLID
   - DRY
   - KISS
   - YAGNI
   - Separation of Concerns
   - Fail Fast
   - Defensive programming
   - Immutability by default where reasonable
   - Readability and maintainability
   - Thread safety where relevant
   - Safe async usage
   - Serialization safety where relevant

4. Review all relevant public and internal services within the affected scope.

5. Identify and report:
   - duplication
   - long or overly complex methods
   - missing validation or guard clauses
   - hidden side effects
   - poor separation of responsibilities
   - misleading naming
   - test gaps
   - documentation gaps
   - risks to backward compatibility

6. Static helper classes:
   - should remain stateless
   - should not mutate instance state
   - should be internal unless a public static API is truly justified

7. During review, do not refactor broadly unless the human explicitly requested review plus fixes.
   - Minor non-invasive corrections are acceptable only if explicitly requested.

8. When reporting SOLID concerns, pay special attention to the user’s design preferences:
   - internal construction logic may intentionally use static factories
   - not every internal implementation is meant to be substitutable
   - testability is still required, including for internal and non-public services where appropriate

9. If you detect a major design issue involving OCP or LSP that would require architectural change rather than a safe local improvement:
   - stop before making invasive changes
   - explain the issue clearly
   - propose the safest next step

10. After review, report findings in a structured way:
   - critical issues
   - design issues
   - maintainability issues
   - test gaps
   - optional improvements

---

## Rules for refactoring

When asked to perform a **refactor**, follow all review rules above first, then:

1. Refactor only within the repository scope requested by the human.

2. Do not modify dependent repositories unless the human explicitly requested cross-repository changes.

3. If a defect in a dependency prevents a correct refactor:
   - stop
   - explain the blocking dependency
   - describe the likely fix required in the dependent component

4. Allowed refactoring actions include:
   - fixing clearly incorrect logic
   - adding guard clauses and argument validation
   - simplifying control flow
   - extracting private helper methods
   - removing dead private code
   - improving XML documentation
   - improving internal naming where it does not break the public API
   - reducing duplication
   - improving readability and cohesion

5. Refactoring must preserve intended behavior unless a bug is being fixed as part of the task.

6. Refactoring should improve code quality without introducing speculative abstractions or unnecessary architectural changes.

---

## Rules for implementation

When asked to perform an **implementation** task, including a bug fix or a new feature:

1. First apply the same analysis discipline used in review and refactor mode.

2. If the request is ambiguous, infer the safest interpretation from the codebase and surrounding context.
   - Avoid unnecessary clarification questions when the intent can reasonably be recovered from the existing code and documentation.

3. You may modify dependent repositories only if this is necessary to implement the feature or bug fix correctly and the requested scope allows cross-repository changes.

4. Prefer solutions that:
   - preserve the existing architecture
   - minimize public API changes
   - keep backward compatibility where practical
   - fit the existing code style and conventions

5. For bug fixes:
   - identify the root cause, not only the symptom
   - add or adapt tests covering the failing case

6. For new features:
   - integrate them into the existing abstractions rather than introducing parallel ad-hoc patterns
   - keep the feature extensible, but do not over-engineer

7. When implementing concurrency-related changes, pay special attention to:
   - thread safety
   - race conditions
   - cancellation flow
   - retry consistency
   - ordering guarantees
   - shared mutable state
   - async correctness
8. Apply TDD ( Test Driven Design). Upadate or add the Unit test and integration test belongs to the changes. Deletes are first not desired. prefer to have obsolete unit tests instead.

---

## Test expectations

Whenever code changes are made:

1. Update or add tests as needed.

2. Cover:
   - valid paths
   - edge cases
   - invalid arguments
   - invalid state transitions
   - exception paths

3. Use:
   - NUnit
   - Moq where appropriate
   - Arrange / Act / Assert structure

4. Keep integration tests readable and representative of real composition behavior.

5. Do not remove tests unless they are objectively invalid, obsolete, or replaced by better coverage.
   - If a test is removed, explain why.

6. Do not generate or expand tests unless they are necessary for the requested change, bug fix, or refactor scope.

# Providing a commit text ready to pase of staged changes

Sometime, the human will request from you to check the staged changes to commit, by applied differencies.
In this case, check the applied differencies, deduce the changes and provide the output humanized and summarized per implemented issue into one commit text.

---

## Build and validation workflow

When code changes are made, run the relevant validation steps in this order when possible:

1. Build the affected projects
2. Run unit tests
3. Pack locally using the repository’s local packaging script if available, for example `pack-local`
4. Run integration tests that depend on packaged outputs, if applicable

If any step cannot be executed, state that clearly and explain why.


---

## Code Documentation expectations

When creating or substantially modifying C# code:

1. Add or improve XML documentation comments in English where relevant.
2. Keep documentation technically precise, concise, and consistent with the actual behavior.
3. Do not leave misleading, outdated, or speculative comments in the code.
---

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
