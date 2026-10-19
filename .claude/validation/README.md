# Validation Specification

## Framework Validation

Framework validation verifies that structure, contracts, governance, and traceability
remain complete and internally consistent.

## Quality Rules

- Required folders and core files must exist.
- Agents, workflows, skills, and commands must comply with their module contracts.
- Workflow gates must map to accountable owners.
- Templates and memory updates must support artifact traceability.
- Validation outputs must include completion status, missing items, and remediation actions.

Two checklists exist and they ask different questions.

- `framework-validation-checklist.md` asks whether the framework is well-formed: folders,
  contracts, registries, and one executable execution check.
- `framework-release-checklist.md` asks whether a single framework update may ship. It is the
  checklist `config/self-hosting-profile.md` binds every framework-internal change to, its
  items are machine-readable, and `runtime/verify_self_hosting.py --release-checklist` executes
  every item that names a command.

Validation results are recorded as dated reports in the reports module.
