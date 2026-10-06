---
name: tplqueue-review
description: Review staged TplQueue code changes for correctness, compatibility, design and test gaps. Use for an explicit code review or required staged completion review, not as a prerequisite for starting implementation or refactoring.
---

# Review staged TplQueue changes

Review means analyze and report. Do not edit, refactor, stage, commit or publish
unless the human separately authorized that action. A required completion review
within an authorized implementation may identify fixes within that implementation's scope.

## Establish the review boundary

1. Read the owning repository's AGENTS.md and inspect Git status. Verify the changes
   requested for review are staged in that repository. If they are not, stop the
   review and tell the human; do not stage their work merely to satisfy this gate.
   If both staged and unstaged changes exist, review the staged versions and do not
   accidentally attribute working-tree edits to the proposed commit.
2. Check relevant .props/.targets, solution and project settings first. Confirm
   language/framework compatibility, especially netstandard2.0 production code and
   the repository's separate test targets. Stop and report inconsistent configuration
   before reviewing the rest. Do not infer compatibility from the SDK version alone.
3. Read the staged diff and the relevant surrounding public and internal services.
   Trace callers, dependencies and tests far enough to establish actual behavior.
   Keep dependent repositories read-only unless the task explicitly includes changes there.

## Evaluate the affected behavior

- Use SOLID, DRY, KISS, YAGNI, separation of concerns, fail-fast validation,
  defensive programming and reasonable immutability as decision criteria.
  Report concrete consequences rather than a generic principle checklist.
- Look for incorrect logic, missing guards, duplication, complex methods, hidden
  side effects, misleading names, weak cohesion, serialization hazards and public
  compatibility regressions. Include test and documentation gaps.
- For concurrent/async code, trace ownership, ordering, races, cancellation, retries,
  shared mutable state, exception observation and cleanup. Distinguish runtime
  completion from observer delivery or cache acknowledgment according to the contract.
- Static helpers should be stateless, avoid instance mutation, and normally remain
  internal. Internal construction factories are intentional where appropriate;
  not every internal implementation needs substitution. Require suitable testability.
- A major OCP/LSP concern requiring architectural change is a finding, not permission
  for invasive work. Explain it and propose the safest next step.

## Report

Group findings as critical issues, design issues, maintainability issues, test gaps
and optional improvements when those groups are useful. Give file/line evidence,
the triggering case, practical impact and a bounded correction. State when no
findings were found; do not manufacture findings to fill categories.

Report exact validation performed and its limits. Tests run against a working tree
with unstaged code do not prove the staged candidate passes. Follow repository
build/test/package entry points when validation is part of the task. Review-only
work does not authorize broad fixes, including supposedly minor corrections.
