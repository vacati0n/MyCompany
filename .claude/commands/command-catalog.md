# Command Catalog

## Purpose

Define operator command entry points and their workflow bindings.

| Command | Primary Workflow | Owning Phase | Phase Owner Agent |
|---|---|---|---|
| `/implement` | `workflows/implement-feature.md` | `execution-planning` onward, full lifecycle | planner, architect, dev-1-implement |
| `/bugfix` | `workflows/fix-bug.md` | `triage-and-impact` onward, full lifecycle | dev-1-bug-analyst |
| `/refactor` | `workflows/refactor.md` | `scope-invariants-and-risk-profile` onward, full lifecycle | architect |
| `/investigate` | `workflows/investigate.md` | `problem-framing` onward, full lifecycle | business-analyst |
| `/research` | `workflows/research.md` | `research-framing` onward, full lifecycle | business-analyst |
| `/review` | `workflows/review-pull-request.md` | `code-quality-review` onward, full lifecycle | dev-2-reviewer |
| `/quality-scan` | `workflows/code-quality-scan.md` | `repository-quality-scan`, the whole single-phase lifecycle | dev-2-reviewer |
| `/optimize-memory` | `workflows/refactor.md` | `scope-invariants-and-risk-profile` onward, scoped to knowledge surfaces | architect |
| `/release` | `workflows/release.md` | `readiness-assessment` onward, full lifecycle | tech-lead |
| `/document` | `workflows/implement-feature.md` | `documentation-and-release-handoff` | documentation |
| `/test` | `workflows/review-pull-request.md` | `test-risk-validation` | qa |

Every row resolves to exactly one active record in `registry/workflows.yaml`, and every named
phase owner is host-invocable through `agents/<agent-id>.agent.md`. `/document` and `/test` are
scoped to a single phase of their primary workflow; the other eight enter their workflow at the
first phase and traverse it — for `/quality-scan` that first phase is also the whole lifecycle,
because its workflow reviews and stops at a human gate. `commands/document.md` and
`commands/test.md` record where the same kind of work appears in other lifecycles and which
command reaches it.

## Command Policy

- Commands must resolve to one primary workflow intent.
- A command whose work appears in several lifecycles names one primary workflow and records the
  other lifecycles as cross-references, never as a second mapping.
- Commands should produce artifacts mapped to templates.
- A command record enters `registry/commands.yaml` only when its specification file exists and
  its primary workflow is an active workflow record.
