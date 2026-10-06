---
name: tplqueue-implement
description: Implement a TplQueue feature or bug fix with focused TDD, compatibility checks and repository validation. Use for requested code changes, not review-only, documentation-only or commit-only tasks.
---

# Implement a TplQueue change

Read the owning repository's AGENTS.md, relevant configuration and affected code.
Respect the human's scope, existing architecture, terminology and public behavior
except where the feature or bug fix requires a change. This skill grants no extra
repository, staging, commit, publishing or dependency-change authorization.

## Establish the change

- Inspect Git status and preserve unrelated staged and unstaged work. Implementation
  can start from a clean tree or unstaged code; the staged-review prerequisite is
  not an entry gate for implementation.
- Inspect applicable .props/.targets and project settings before code changes.
  Preserve the declared framework and language policy, including netstandard2.0
  production compatibility and net8.0 test conventions where applicable. If the
  configuration is inconsistent, report it first; correct it only within the
  authorized task, otherwise record the blocker.
- Infer routine details from the code and documentation. Resolve only ambiguities
  that affect scope or observable behavior. Find the cause of a bug, not just its symptom.
- Change a dependency repository only when necessary and within explicitly permitted
  cross-repository scope. Otherwise describe the blocking dependency and likely fix.

## Test and implement

- Apply TDD for behavior changes: add or adapt the relevant unit/integration test,
  run it to demonstrate the missing behavior, then implement the smallest coherent
  change. If the baseline prevents execution, record that instead of claiming a red test.
- Use NUnit, Moq when appropriate, and Arrange / Act / Assert for C#. Cover valid
  paths and relevant edge cases, arguments, state transitions and exceptions.
  Keep real-composition integration tests readable. Do not expand unrelated tests.
- Preserve valid tests. Remove a test only if it is objectively invalid, obsolete
  or replaced by better coverage, and explain the removal. Prefer adapting coverage.
- Fit existing abstractions; avoid speculative frameworks, parallel ad-hoc patterns
  and silent redesign. Favor simple, cohesive changes with minimal public API impact.
- Inspect concurrency changes for races, shared state, ordering, cancellation,
  retry consistency, observer isolation and awaited/disposed asynchronous work.
  Use controlled synchronization in tests when timing would obscure the assertion.
- Keep static helpers stateless and normally internal; do not use them to mutate
  instance state. Internal static construction factories can be intentional.
- Add precise English XML documentation for new or substantially changed C# APIs;
  update affected behavior documentation without duplicating another repository's source of truth.

## Validate and finish

Use the owning repository's maintained entry points. When applicable, build affected
projects, run unit tests, pack through the local packaging script, then run integration
tests that consume those packages. Skip packaging when no packaged output changes.
Distinguish sibling-source validation from actual package consumption. Record exact
commands, outcomes, pre-existing failures and skipped checks with reasons.

Inspect the final diff for defects, duplication, guards, hidden effects, cohesion,
serialization and compatibility. If a staged review or commit is requested, stage
only authorized changes and use the installed tplqueue-review skill (or its sibling
SKILL.md). Otherwise follow the repository's staging policy. Report what changed,
why, validation, remaining limits and any required handoff. A commit-text request
only asks for a human-readable summary of the staged issues, not a commit.
