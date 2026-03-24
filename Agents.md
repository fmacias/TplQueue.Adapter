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

When working inside a package folder, read the nearest package-level `AGENTS.md` as well. The root `Agents.md` remains the primary repository-wide instruction set.

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

Use the terminology already present in the current codebase and public contracts.

Current core domain contracts include:

- `IJob`
- `IJobRoot`
- `IDataJob`
- `IDataJobRoot`
- `IQ`
- `IParallelQ`
- `IFifoQ`
- `ICacheQ`

Current adapter-facing contracts and names include:

- `IApi`
- `IQFactoryAdapter`
- `IDataJobFactory`
- `IDataJobCache`
- `IRetryPolicyOptions`
- `IRetryPolicyAbstractFactory`
- `ITypeResolver`
- `IObserverDispatcher`
- `SystemTextJsonSerializerFactory`

Use these names consistently in analysis, refactoring, implementation, comments, tests, and documentation.

Do not introduce alternative vocabulary for the same concept. In particular:

- prefer `Job`, `JobRoot`, `DataJob`, and `DataJobRoot`
- prefer `Queue`, `dispatcher`, or `queue dispatcher`
- prefer `observer` for event subscribers and `dispatcher` for UI-thread marshaling abstractions
- prefer `thin facade` or `wrapper package` when referring to `Fmacias.TplQueue`

Treat the following names as legacy unless you are explicitly preserving compatibility or documenting historical context:

- `TaskRunner`
- `TaskRunnerRoot`
- `RetryPolicyGenericFactory`
- `IRetryPolicyDescriptor`
- `ICoreQFactoryAdapter`
- `INodeTypeResolver`
- `JsonSerializerFactory`

Do not reintroduce legacy names into new code or new documentation unless the human explicitly requests that.

## Architectural intent and package ownership

`TplQueue.Core` is the orchestration engine. It owns queue execution, orchestration semantics, runtime queue behavior, and the core job execution model.

`TplQueue.Adapter` owns modular integrations and composition around that engine, including:

- the thin facade package `Fmacias.TplQueue`
- retry policy implementations and factories
- cache abstractions and cache providers
- serialization modules
- observer packages
- dependency-injection helpers

Respect the current ownership boundaries:

- Keep `Fmacias.TplQueue` thin. It should compose services and delegate to Core and to adapter subpackages.
- Do not move concrete logging observers back into `Fmacias.TplQueue`; they belong in `Fmacias.TplQueue.Log`.
- Do not move UI-dispatch or view-model observer logic back into `Fmacias.TplQueue`; it belongs in `Fmacias.TplQueue.Observers.ViewModel`.
- Keep `System.Text.Json` integration inside `Fmacias.TplQueue.Serialization.SystemTextJson`.
- Keep retry policy implementations and factories inside `Fmacias.TplQueue.RetryPolicies`.
- Keep cache dehydration/hydration abstractions inside `Fmacias.TplQueue.Cache.Abstract` and provider-specific behavior inside `Fmacias.TplQueue.Cache.MemCache` or other provider packages.
- Keep DI registration logic inside `Fmacias.TplQueue.Microsoft.DependencyInjection`.

If a requested change clearly belongs in `TplQueue.Core` or `TplQueue.Abstractions`, stop and explain the boundary instead of silently redesigning Adapter.

## Test structure

Tests in this repository are primarily unit tests, organized by package. Some broader tests may validate composition across modules, but they should remain readable and should still follow Arrange / Act / Assert structure.

When adding coverage:

- place wrapper behavior tests in `Fmacias.TplQueue.Unit.Test`
- place retry tests in `Fmacias.TplQueue.RetryPolicies.Unit.Test`
- place serializer tests in `Fmacias.TplQueue.Serialization.SystemTextJson.Unit.Test`
- place cache abstraction tests in `Fmacias.TplQueue.Cache.Abstract.Test`
- place cache provider tests in `Fmacias.TplQueue.Cache.MemCache.Test`
- place logging observer tests in `Fmacias.TplQueue.Log.Unit.Test`
- place UI observer tests in `Fmacias.TplQueue.Observers.ViewModel.Unit.Test`
- place DI registration tests in `Fmacias.TplQueue.Microsoft.DependencyInjection.Unit.Test`

## Documentation consistency

If you find contradictions, obsolete terminology, or architectural inconsistencies in this document or in nearby documentation, report them clearly in your final response.

Pay special attention to documentation drift around the recent package split. Docs and examples must match the actual public API names currently implemented in code.

---

## General operating rules

1. Work only within the scope requested by the human:
   - `Review` = analyze and report
   - `Refactor` = improve existing code without changing intended behavior
   - `Implementation` = fix a bug or add a feature

2. Prefer small, safe, understandable changes unless the human explicitly asks for a broader refactor.

3. Preserve the current package boundaries, architectural style, and naming unless there is a clear defect or explicit instruction to change them.

4. Do not silently redesign the library or collapse modular packages back into the wrapper package.

5. Keep public behavior stable unless a bug fix or the explicit task requires a change.

6. Respect the current ownership split between Core, Abstractions, and Adapter before proposing broader changes.

---

## Rules for code review

When asked to perform a code review, follow this process:

1. Verify that the changes to be reviewed are staged in the relevant git repository.
   - If they are not staged, stop the review and inform the human.

2. Verify the relevant project configuration before reviewing behavior:
   - `Directory.Build.props`
   - the affected `.csproj` files
   - `TplQueue.Adapter.sln` when solution-level composition matters
   - `pack-local.ps1` when packaging behavior or local package ordering matters

3. Confirm that package ownership is respected.
   - Review whether the change is landing in the correct adapter package.
   - Review whether `Fmacias.TplQueue` remains a thin facade rather than re-owning concrete runtime or observer implementations.

4. Review the code according to these principles:
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

5. Identify and report:
   - duplication
   - long or overly complex methods
   - missing validation or guard clauses
   - hidden side effects
   - poor separation of responsibilities
   - misleading naming
   - package-boundary violations
   - documentation drift
   - test gaps
   - backward compatibility risks

6. Static helper classes:
   - should remain stateless
   - should not mutate instance state
   - should be internal unless a public static API is truly justified

7. During review, do not refactor broadly unless the human explicitly requested review plus fixes.

8. When reporting design issues, pay attention to the repository's current design direction:
   - static factory methods are intentionally common
   - `Fmacias.TplQueue` is intentionally thin
   - adapter packages are intentionally split by concern
   - tests must still validate internal behavior where appropriate

9. If you detect a major design issue that really belongs to Core or Abstractions:
   - stop before making invasive changes
   - explain the boundary clearly
   - propose the safest next step

10. After review, report findings in a structured way:
   - critical issues
   - design issues
   - maintainability issues
   - test gaps
   - optional improvements

---

## Rules for refactoring

When asked to perform a refactor, follow all review rules above first, then:

1. Refactor only within the repository scope requested by the human.

2. Prefer changes inside the owning package rather than spreading internal logic across multiple adapter packages.

3. Do not modify dependent repositories unless the human explicitly requested cross-repository changes.

4. If a defect in `TplQueue.Core` or `TplQueue.Abstractions` prevents a correct refactor:
   - stop
   - explain the blocking dependency
   - describe the likely fix required in the other repository

5. Allowed refactoring actions include:
   - fixing clearly incorrect logic
   - adding guard clauses and argument validation
   - simplifying control flow
   - extracting private helper methods
   - removing dead private code
   - improving XML documentation
   - improving internal naming where it does not break the public API
   - reducing duplication
   - improving readability and cohesion
   - aligning documentation and examples with the real API

6. Refactoring must preserve intended behavior unless a bug is being fixed as part of the task.

7. Refactoring should improve code quality without introducing speculative abstractions or unnecessary architectural changes.

---

## Rules for implementation

When asked to perform an implementation task, including a bug fix or a new feature:

1. First apply the same analysis discipline used in review and refactor mode.

2. If the request is ambiguous, infer the safest interpretation from the codebase, tests, nearest package README, and current public API surface.

3. You may modify dependent repositories only if this is necessary to implement the feature or bug fix correctly and the requested scope allows cross-repository changes.

4. Prefer solutions that:
   - preserve the current package split
   - minimize public API changes
   - keep backward compatibility where practical
   - fit the existing code style and conventions

5. For bug fixes:
   - identify the root cause, not only the symptom
   - add or adapt tests covering the failing case

6. For new features:
   - place the feature in the package that owns that concern
   - integrate through existing abstractions instead of adding parallel patterns
   - keep the wrapper `Fmacias.TplQueue` focused on composition
   - do not move concrete runtime behavior into the wrapper package unless the human explicitly asks for that redesign

7. Current package ownership guidance:
   - facade composition belongs in `Fmacias.TplQueue`
   - retry policy implementations belong in `Fmacias.TplQueue.RetryPolicies`
   - shared cache abstractions belong in `Fmacias.TplQueue.Cache.Abstract`
   - in-memory cache behavior belongs in `Fmacias.TplQueue.Cache.MemCache`
   - logging observers belong in `Fmacias.TplQueue.Log`
   - view-model and UI dispatch observers belong in `Fmacias.TplQueue.Observers.ViewModel`
   - `System.Text.Json` serializer integration belongs in `Fmacias.TplQueue.Serialization.SystemTextJson`
   - service registration belongs in `Fmacias.TplQueue.Microsoft.DependencyInjection`

8. When implementing concurrency-related changes, pay special attention to:
   - thread safety
   - race conditions
   - cancellation flow
   - retry consistency
   - ordering guarantees
   - shared mutable state
   - async correctness
   - observer isolation from queue execution

9. Apply TDD when practical. Update or add only the tests that belong to the affected package and behavior.

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

4. Keep tests readable and representative of the owning package behavior.

5. Do not remove tests unless they are objectively invalid, obsolete, or replaced by better coverage.
   - If a test is removed, explain why.

6. Do not expand test scope beyond the requested change.

7. Prefer adding tests to the owning package test project instead of broadening unrelated wrapper tests.

---

## Build and validation workflow

When code changes are made, run the relevant validation steps in this order when possible:

1. Build the affected project or the solution.
   - Example:
   - `dotnet build .\TplQueue.Adapter.sln --no-restore`

2. Run the relevant unit tests.
   - Example:
   - `dotnet test .\test\Fmacias.TplQueue.RetryPolicies.Unit.Test\Fmacias.TplQueue.RetryPolicies.Test.csproj`
   - or `dotnet test .\TplQueue.Adapter.sln`

3. Pack locally using the repository packaging pipeline.
   - `powershell -NoProfile -ExecutionPolicy Bypass -File .\pack-local.ps1`

4. Run any consumer or integration validation that depends on locally packed outputs, if applicable.

The current `pack-local.ps1` pipeline is part of the repository contract. It:

- ensures the `..\TplQueue.NugetLocal` feed exists
- registers the local NuGet source when needed
- clears stale local `fmacias.tplqueue*` package cache entries
- runs `..\TplQueue.Abstractions\pack-local.ps1` first
- packs local adapter projects before packing the solution

If any validation step cannot be executed, state that clearly and explain why.

---

## Documentation expectations

When creating or substantially modifying C# code:

1. Add or improve XML documentation comments in English where relevant.
2. Keep documentation technically precise, concise, and consistent with the actual behavior.
3. Do not leave misleading, outdated, or speculative comments in the code.

When modifying markdown documentation:

1. Keep the root `README.md` aligned with the real repository structure and current public API names.
2. Update the affected package README when behavior, examples, or public names in that package change.
3. Use the current API names in docs and examples, including:
   - `IQFactoryAdapter`
   - `IRetryPolicyOptions`
   - `IRetryPolicyAbstractFactory`
   - `ITypeResolver`
   - `SystemTextJsonSerializerFactory`
4. If legacy names must be mentioned, label them explicitly as legacy.

---

## Commit text for staged changes

Sometimes the human will ask for a commit text based on staged changes.

In that case:

1. Inspect the staged diff, not unstaged work.
2. Deduce the implemented change set.
3. Provide one humanized commit message that summarizes the implemented issues clearly and accurately.
4. Mention documentation or staging inconsistencies when they materially affect the summary.

---

## Constraints

- Do not change namespaces unless strictly necessary.
- Do not change public API signatures unless strictly necessary to fix a bug or implement the requested feature.
- Do not introduce new external dependencies.
- Keep changes understandable, consistent, and as small as reasonably possible.
- Preserve `netstandard2.0` compatibility for production packages unless the human explicitly instructs otherwise.
- Prefer the existing project terminology and package boundaries over inventing new abstractions.
- Unless the task is review-only, apply changes directly in the workspace without asking for confirmation.
