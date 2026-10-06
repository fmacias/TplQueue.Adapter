---
name: tplqueue-refactor
description: Refactor existing TplQueue code while preserving intended behavior and repository boundaries. Use for requested structural improvements, not new features, review-only work or documentation-only reorganization.
---

# Refactor TplQueue code

Read the owning repository's AGENTS.md, applicable configuration and affected code.
Preserve intended behavior, architecture, public API and terminology unless the
human explicitly includes a bug fix or behavior change. Do not silently redesign
the library or modify dependent repositories outside the requested scope.

## Establish the baseline

- Inspect Git status and preserve unrelated changes. Refactoring can begin from
  clean or unstaged code. The staging prerequisite applies to an explicit review
  of proposed changes, not to starting a refactor.
- Verify relevant .props/.targets and project/solution configuration, including
  the repository's netstandard2.0 compatibility and language policy. Report an
  inconsistency first; correct it only if authorized, otherwise record the blocker.
- Trace affected public and internal services and their tests. Apply SOLID, DRY,
  KISS, YAGNI, separation of concerns, fail-fast validation and immutability where
  useful. Establish observable behavior before changing structure.
- If a dependency defect prevents a correct refactor, stop and explain the needed
  dependency fix. Do not quietly broaden the repository scope.

## Make a bounded improvement

- Simplify control flow; extract private helpers; improve internal names, cohesion
  and XML documentation; reduce duplication; remove dead private code. Guard
  clauses must not change supported behavior accidentally. Clearly incorrect logic
  may be corrected when its bug fix is within the task, with regression coverage.
- Keep static helpers stateless, avoid instance mutation and keep them internal
  unless a public static API is justified. Internal construction factories and
  non-substitutable internals can be intentional; retain suitable testability.
- Stop before invasive architecture changes for major OCP/LSP issues. Explain the
  issue and safest next step rather than introducing speculative abstractions.
- Preserve async/concurrency semantics: ordering, cancellation, retry consistency,
  shared-state ownership, observer isolation, exceptions and resource disposal.
- Preserve valid tests. Add or adapt only necessary characterization/regression
  coverage using NUnit, Moq where useful, and Arrange / Act / Assert. Cover relevant
  argument, state, edge and exception paths. Demonstrate a failing regression test
  before a bug fix; behavior-preserving characterization tests may already pass.
  Explain any removal of invalid, obsolete or replaced tests.
- Keep English XML comments precise for substantially changed C# code and update
  affected documentation according to the repository's ownership rules.

## Validate and finish

Use maintained repository commands: build, unit tests, applicable local packaging,
then integration tests against packaged outputs. Do not repack unchanged products.
Distinguish source-reference checks from package-consumption checks. Record commands,
results, baseline failures and skipped checks rather than claiming unsupported success.

Inspect the final diff for behavior preservation, guards, hidden effects, complexity,
serialization and compatibility. Follow repository staging policy; stage only when
authorized. Use tplqueue-review for a requested staged review, via the installed
skill or its sibling SKILL.md. Report the concrete improvement, validation and limits.
Do not commit or push without task authorization.
