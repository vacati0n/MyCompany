# Command Specification: /quality-scan

## Purpose

Run a single-pass code quality and technical-debt scan over a bounded repository scope and
stop with a review package a human tech lead can decide on. The scan identifies junk,
redundant logic, dead code, duplicated patterns, generated low-value noise, and
over-engineered abstractions — with code evidence for every claim — and never modifies,
fixes, approves, or merges anything.

## Inputs

One supplied input of type `quality-scan-scope`, a markdown document stating:

- Repository path or review boundary (required).
- Review scope: the paths or modules to review (defaults to the whole boundary).
- Excluded paths (generated code, vendored dependencies, run evidence).
- Ticket or pull-request context, where one exists.
- Risk threshold: what severity the requester considers actionable.

The command materializes this document from the operator's arguments when one is not
supplied as a file.

## Workflow Triggered

Code Quality Scan (`workflows/code-quality-scan.md`), a single-phase workflow whose
`repository-quality-scan` phase is owned by `dev-2-reviewer` and whose only gate, the
Quality Handoff Gate, is decided by `tech-lead` — the human handoff the command exists
to end at.

## One-Command Execution

The whole lifecycle is one dispatch cycle. From the repository root:

1. Write the scope document to `.claude/runs/inputs/<name>-quality-scan-scope.md`.
2. `python .claude/runtime/framework_runtime.py dispatch --command quality-scan
   --input quality-scan-scope=.claude/runs/inputs/<name>-quality-scan-scope.md`
   — this plans the run, enqueues the single phase, and hands the invocation envelope to
   the host adapter, which invokes `dev-2-reviewer` through
   `agents/dev-2-reviewer.agent.md`.
3. `python .claude/runtime/framework_runtime.py complete --run-id <run-id> --phase
   repository-quality-scan ...` with the produced artifact — the runtime validates it with
   `runtime/review_package_validator.py` and transitions the phase.
4. The run now holds at the Quality Handoff Gate. The command is done: the review package
   is the deliverable, and the gate decision belongs to the human tech lead
   (`framework_runtime.py gate` records it when they make it).

Steps 2–3 are what the host performs when an operator types `/quality-scan <scope>`; the
operator's single command is the whole contract.

## Expected Outputs

- `review-package.md` for the scanned scope: severity-classified findings typed by the
  junk-detection categories (`duplication`, `dead-code`, `over-abstraction`,
  `generated-noise`, `legacy-drift`, `reviewability`), each with location, measured
  requirement, code evidence, and stated confidence.
- Prioritized correction requests (remove, refactor, split, centralize) as the tech lead's
  action list.
- Open questions naming every finding that needs human validation.
- A recommendation verdict, held at the Quality Handoff Gate for the human decision.

## Success Criteria

- Every finding is evidence-backed: location, requirement, and confidence are present.
- Ambiguous findings are classified as needing human validation, never asserted.
- No source file was modified by the run.
- The run stops at the Quality Handoff Gate with the package attached.

## Failure Handling

- An unresolvable scope blocks at context integrity; correct the scope document and rerun —
  a changed input is a new run.
- A package that fails validation returns to the reviewer with the validator's findings.
- A gate rejection returns the run to `repository-quality-scan` with the rejection
  rationale; disputes about whether code is junk escalate to the tech lead as open
  questions, not as re-asserted findings.

## Boundaries

This command reviews and stops. Cleanup work it motivates enters through `/refactor`;
defects it uncovers enter through `/bugfix`; the merge decision it informs stays with
`/review` and the humans who own it.
