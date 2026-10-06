# TplQueue agent workflows

This directory owns the shared implementation, review and refactoring procedures.
The source files are version-controlled with Adapter; repository AGENTS.md files
retain local constraints, architecture, commands and documentation boundaries.

| Task | Skill |
| --- | --- |
| Feature or bug fix | [tplqueue-implement](skills/tplqueue-implement/SKILL.md) |
| Review proposed code changes | [tplqueue-review](skills/tplqueue-review/SKILL.md) |
| Preserve behavior while improving code | [tplqueue-refactor](skills/tplqueue-refactor/SKILL.md) |

Use only the matching workflow. A documentation-only or commit-only task does not
need all three skills. Explicit human instructions and repository constraints
remain applicable. Skills do not authorize extra changes or external actions.

## Discovery and maintenance

Codex discovers these repository skills under `.agents/skills`. To make the same
source available when opening a sibling repository, install directory links in the
user skill directory using `install-user-skills.ps1` from this folder. It leaves
existing unrelated skills untouched and refuses to replace conflicting destinations.
The default user location is `$HOME/.agents/skills`; a different skills root can
be passed explicitly for a client using a legacy location.

```powershell
powershell -NoProfile -File .\.agents\install-user-skills.ps1
```

Keep personal preferences in `$CODEX_HOME/AGENTS.md` (normally `~/.codex/AGENTS.md`).
Do not put TplQueue-specific .NET or publishing constraints in that global file.
After installation, start a new thread and confirm the three named skills are
available. A running thread may retain its previous instruction catalog.
See [official instruction discovery](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
and [skill discovery](https://learn.chatgpt.com/docs/build-skills).

For a standalone checkout of another TplQueue repository, install these skills
from an Adapter checkout or provide their exact SKILL.md files. A repository's
AGENTS.md must not silently assume that a parent workspace file was automatically
loaded across a Git boundary. Installed skills and explicit sibling links provide
the workflow; repository-local constraints remain readable without this setup.

The 2026-09 instruction migration consolidates repeated procedures from the parent
workspace, Abstractions and Usage. The staged-diff gate applies to review of existing
changes, not to beginning implementation or refactoring. Abstractions still requires
explicit authorization before staging edits. Legacy instruction files are pointers,
not a second copy of the rules.

## Publishing boundary

Agent workflows are maintainer configuration, outside `docs/en/` and `docs/de/`.
Do not copy `.agents/` into the public documentation site or NuGet content. Changes
to skills are maintained here, not mirrored into each repository or language tree.
Public product documentation continues to be published solely from Adapter's
language trees; no publishing or package-reference configuration changes are needed.
