# Memory Specification

## Memory Model

Memory stores durable engineering knowledge required for consistency across
decisions, implementation, review, and release.

## Persistence

- Memory is persistent across workflow runs.
- Entries must remain addressable and historically traceable.
- Superseded knowledge must be retained with replacement references.

## Update Rules

- Update only when knowledge is validated and reusable.
- Prefer concise, evidence-based statements.
- Avoid transient execution details and unresolved assumptions.
- Link significant updates to decision or release artifacts.

## Lessons

`lessons/` holds the lesson category: lessons from earlier runs, harvested mechanically from
recorded run evidence by `runtime/knowledge.py` and keyed by a semantic fingerprint. It is an
Open Knowledge Format (OKF) v0.2 bundle: one concept document per lesson (`L-<10 hex>.md`), a
derived `index.md`, and a derived change log `log.md`. Unlike the documents beside it, the bundle
is machine-maintained: it is never edited by hand, never rewritten by the memory optimizer, and
never shipped to installed copies.
A lesson moves through `candidate`, `active`, and `retired`; it is eligible for later dispatches
when active, or when a candidate recurs in at least two distinct runs (the threshold in
`config/knowledge-policy.json`). Whether a run is shown lessons is recorded once, at plan time:
on by default, off with the plan flag `--no-lesson-hydration`. `config/runtime.md`, "Lessons",
is the authority.

## Ownership

- Product Owner and Business Analyst: business rules and glossary.
- Architect and Tech Lead: architecture and technology stack.
- QA and Reviewer: known issues and quality-related updates.
- Orchestrator: memory consistency and lifecycle governance.
- Tech Lead (default owner in `config/knowledge-policy.json`): lessons -- approval, retirement,
  and operator remedies.
