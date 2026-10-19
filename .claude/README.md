# AI Engineering Framework

## Purpose

Establish a governed engineering framework for structured delivery, defect resolution,
technical investigation, review, and release operations.

## Architecture

The framework is organized in layered responsibilities:

1. Context and configuration define constraints, policies, and routing.
2. Agents provide role-specific execution ownership.
3. Skills provide reusable technical knowledge.
4. Workflows define lifecycle phases, gates, and recoveries.
5. Commands expose operator entry points into workflows.
6. Templates standardize artifacts.
7. Memory persists durable project intelligence.
8. Validation verifies framework completeness and quality.

Development of the framework itself runs through these same layers. `config/self-hosting-profile.md`
is the one command profile that routes a framework-internal change to a command, requires its
change proposal to link the run artifacts that carried it, and binds it to
`validation/framework-release-checklist.md`. `runtime/verify_self_hosting.py` decides whether that
held.

## Folder Descriptions

- `context/`: authoritative product, technical, and release context.
- `config/`: routing, quality-gate, and runtime governance policies.
- `agents/`: role contracts and collaboration boundaries.
- `skills/`: reusable domain and engineering knowledge.
- `workflows/`: phase-based lifecycle specifications.
- `commands/`: command API and execution contracts.
- `templates/`: standardized artifact schemas.
- `memory/`: persistent, versioned engineering knowledge.
- `validation/`: validation rules and checklist definitions, including the framework release
  checklist a framework update must satisfy.
- `reports/`: generated validation and readiness outputs.
- `proposals/`: change proposals, one per framework-internal change, each linking the run
  artifacts that carried it.
- `runs/`: run evidence written by the runtime, one directory per run.

## Contributor Usage

1. Read context and config before execution.
2. Select the command that matches task intent. For a change to the framework itself, the command
   is not selected by judgment: classify and route it with
   `python .claude/runtime/self_hosting.py classify --path <path>` and
   `python .claude/runtime/self_hosting.py route --intent <change-class>`.
3. Execute the linked workflow and satisfy all approval gates.
4. Produce artifacts from templates and update memory when knowledge is durable. A
   framework-internal change also produces a change proposal from
   `templates/framework-change-proposal.md`.
5. Run framework validation before release or governance changes. For a framework update, run
   `python .claude/runtime/verify_self_hosting.py --release-checklist`.
