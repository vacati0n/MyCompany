# Workflow Specification: Code Quality Scan

## Goal

Produce a single-pass, evidence-based review of a bounded repository scope that identifies
junk, redundant logic, dead code, duplicated patterns, generated low-value noise, and
over-engineered abstractions, and hand the resulting review package to a human tech lead
for the cleanup, refactor, or rejection decision.

This workflow reviews and stops. It changes no source code, fixes nothing, and records no
merge or release decision. Its terminal state is a gate held for a human decision, because
the judgement it feeds — whether code is worth keeping — is a delivery tradeoff the
framework assigns to a person, not to the agent that produced the evidence.

## Entry Conditions

- A quality-scan scope is supplied: the repository or directory boundary under review, the
  paths explicitly excluded, any ticket or pull-request context, and the risk threshold the
  requester cares about.
- The repository under review is readable at a stable revision.
- No expectation exists that this run will modify, approve, or merge anything.

## Participating Agents

- dev-2-reviewer
- tech-lead

## Operating Rules

These rules bind the owning agent for the whole run and restate its own contract in this
workflow's terms:

- Do not modify source code, tests, or committed run evidence.
- Do not auto-fix, approve, or merge; the package is evidence for a human decision.
- Do not claim certainty without evidence: every finding names a location and the standard
  it is measured against, and quotes or cites the code it describes.
- A finding whose evidence is insufficient is recorded at its honest confidence and carries
  an open question (`Q-nnn`) naming what a human must validate; it is never silently
  upgraded to a claim.
- Report only what was found in the code. File paths and symbols that were not observed are
  not invented.

## Phase Model

Canonical, machine-resolvable phase identifiers for this workflow. Phase order is the row
order of this table. Phase identifiers are the routing keys used by
`config/agent-routing.md`, by agent manifests (`supportedWorkflows[].phase`), and by the
runtime gateway in `runtime/framework_runtime.py`.

| Phase | Owner Agent | Participation | Input | Output Artifact | Gate | Required Skills |
|---|---|---|---|---|---|---|
| `repository-quality-scan` | `dev-2-reviewer` | primary | supplied quality-scan scope, repository under review at a stable revision | `review-package.md`, carrying the quality-scan findings under the junk-detection categories | Quality Handoff Gate | S01, S02, S03, S07 |

### Phase Identifier Sources

| Phase | Identifier source |
|---|---|
| `repository-quality-scan` | Declared by `agents/dev-2-reviewer/manifest.yaml`, `supportedWorkflows[code-quality-scan].phase`. New identifier: no prior state machine names this phase, and it is deliberately distinct from `code-quality-review`, which reviews a change; this phase reviews current repository state. |

### Resolution Rules

- A phase resolves to exactly one owner agent. `tech-lead` participates through the
  Quality Handoff Gate, never through phase ownership.
- The owner agent's manifest declares this workflow and this phase identifier in
  `supportedWorkflows`. The runtime rejects a mismatch rather than guessing.
- The phase's required skills resolve through `registry/skills.yaml`: S01 for structural
  and abstraction judgement, S02 for domain-model drift, S03 for platform idiom, S07 for
  judging what the test suite actually exercises.
- The output artifact is `review-package.md`, the one artifact type that carries every
  review output of this framework (`templates/review-package.md` records why). The
  Category column is what distinguishes a quality scan from a change review: this phase
  reports its findings under the junk-detection categories `duplication`, `dead-code`,
  `over-abstraction`, `generated-noise`, `legacy-drift`, and `reviewability`, alongside
  the shared vocabulary where a finding is better named by it.
- This is a single-phase workflow, so the run's dependency graph is one node with no
  edges; the run completes when the phase completes and its gate is decided.

## Scan Procedure

The dispatched agent works the supplied scope in one pass, in this order:

1. **Scope discovery.** Enumerate the files and modules inside the review boundary, apply
   the supplied exclusions, and record the resulting boundary — and why it matters — under
   `Review Scope`. Files outside the boundary are out of scope by declaration, not by
   omission.
2. **Static quality scan.** Detect, with code evidence for each:
   - duplicated code blocks, repeated logic, repeated validation, parsing, formatting, or
     conversion routines (`duplication`);
   - dead code, unused functions and imports, unreachable branches, stale flags, abandoned
     helpers and old wrappers (`dead-code`);
   - helper functions that only wrap one call, adapters with no meaningful behavior,
     indirection with no consumer that needs it (`over-abstraction`);
   - repetitive synthetic-looking patterns: many tiny functions with low business value,
     naming variations over the same logic, verbose code with little domain logic, code
     hard to trace from requirement to implementation (`generated-noise`);
   - code that no longer matches the current domain model (`legacy-drift`);
   - code whose primary cost is human review load rather than runtime behavior
     (`reviewability`).
3. **Evidence collection.** Each finding records file path and symbol in `Location`, the
   standard or rule it is measured against in `Requirement`, and in `Finding` the exact
   code evidence, why it is considered junk or redundant, and its impact on
   maintainability, readability, auditability, or reviewability.
4. **Severity and confidence classification.** Severity uses the shared vocabulary
   (`critical`, `high`, `medium`, `low`). Each finding's text opens with its confidence —
   `confidence: high | medium | low` — because the review-package structure carries no
   confidence column and an unconfident claim stated as fact would violate the evidence
   rule. A `low`-confidence finding is a candidate, not a verdict, and carries an open
   question.
5. **Test-suite inventory.** Record under `Test Adequacy Assessment` what verification
   evidence was actually examined: the test suite enumerated or executed read-only, and
   which findings (dead code especially) are corroborated or contradicted by it. A scan
   that examined no verification evidence says so — and then cannot close with an
   unqualified approval, per the reviewer's own P6 rule.
6. **Handoff package.** Close with a verdict — `approve` (no material junk),
   `approve-with-corrections` (cleanup recommended), or `reject` (cleanup required before
   further review or merge) — and stop. The Quality Handoff Gate holds the run for the
   human tech lead.

## Report Mapping

The requester's report shape maps onto the review-package contract; nothing is reported
outside it:

| Requested section | Where it lives in `review-package.md` |
|---|---|
| Executive summary | `Review Scope` (what was reviewed) plus `Verdict` rationale (finding count, top risk areas, overall assessment) |
| Findings table | `Findings` (severity, file and symbol, category as issue type, evidence, status) |
| Detailed findings | `Findings` rows; each `Finding` cell carries root cause, why it is problematic, and confidence |
| Likely generated or redundant patterns | `Findings` rows under `generated-noise` and `duplication`, with the repeated pattern named |
| Review risk assessment | `Residual Risk` (why the code is hard to validate, what future maintenance risk it creates) |
| Recommended actions | `Correction Requests` (prioritized: remove, refactor, split, centralize), owner `tech-lead` for arbitration items |
| Human review checklist | `Open Questions` (`Q-nnn`), one per judgement only a human can make, including every needs-human-validation finding |

## Execution Order

Prose form of the Phase Model above. The identifier in parentheses is canonical.

1. Scan the supplied repository scope and produce the handoff package
   (`repository-quality-scan`).
2. Hold at the Quality Handoff Gate for the human tech lead's decision.

## Deliverables

- Severity-classified, evidence-backed findings over the declared scope, typed by the
  junk-detection categories.
- Prioritized correction requests a tech lead can turn into cleanup or refactor work.
- Open questions naming every ambiguous case that needs human validation.
- A readiness verdict that recommends and never decides.

## Exit Criteria

- Every finding carries location, requirement, severity, category, status, and stated
  confidence.
- Every ambiguous or low-confidence finding carries an open question.
- The package validates against `runtime/review_package_validator.py`.
- The Quality Handoff Gate is decided by the human tech lead. Approval of the gate accepts
  the package as the review of record; it does not approve any code change, because none
  was made.

## Failure Recovery

- A scope that does not resolve (missing paths, unreadable revision) blocks the phase with
  a context-integrity failure rather than scanning a guessed boundary.
- A package rejected at the gate returns to `repository-quality-scan` with the rejection
  rationale as correction input; the scan is re-run against the same frozen scope.
- Disputes about whether a finding is junk are not resolved here; they leave as open
  questions owned by `tech-lead`.

## Approval Gates

Gate names are the canonical ones in `workflows/workflow-gate-matrix.md`, which is the
authority the runtime reads for gate ownership.

- Quality Handoff Gate: dev-2-reviewer and tech-lead.

The gate carries a second owner because the first owner produces the evidence the gate
assesses, and the Producer Exclusion Rule forbids approving one's own output. The deciding
owner is tech-lead — the human handoff this workflow exists to end at. Gate decisions
default to human under `config/gate-policy.md`; running this workflow under an auto
gate-policy does not change who may decide, only whether a clean-evidence approval may be
recorded without a prompt, and the recommended invocation keeps the default human mode.
