# Commands Specification

## Command API

Commands are stable operator entry points that map intent to workflows and
required agent participation.

## Naming Convention

- Public commands use the `/name` format.
- Command filenames use kebab-case and map directly to command intent.
- Grouped commands are stored by lifecycle domain.

## Command Contracts and Grouped Runbooks

Two kinds of file live under `commands/`, and only one of them is a command contract.

- `commands/<name>.md` is a **command contract**. Each carries a record in
  `registry/commands.yaml` naming exactly one primary workflow, and each is the entry point
  the runtime accepts as `--command <name>`. There are eleven, indexed by `command-catalog.md`.
- `commands/<domain>/<name>.md` is a **grouped runbook**: an operator-facing procedure for
  starting a lifecycle, exposed by the host as `/<domain>:<name>`. Runbooks carry no registry
  record, because each would duplicate the routing a contract already owns. A runbook that
  should become an entry point in its own right needs a contract file and a record; until then
  it documents how to use one.

## Routing a Change to This Framework

A change to the framework itself is routed by `config/self-hosting-profile.md`, not chosen. That
profile's Scope Rule decides whether a change is framework-internal, and its Routing Table maps
each change class to exactly one of the command contracts below. Nothing in it adds a
command or alters a command's primary workflow; it decides which existing command an operator
enters, and requires the change to carry a validated change proposal under `proposals/`.

## Execution Rules

- Each command must resolve to a primary workflow.
- Commands must declare required artifacts and gate expectations.
- Command execution cannot bypass workflow approval gates.
- If intent is ambiguous, orchestration must route to clarification first.

## Parameter Rules

- Parameters are split into required and optional sets.
- Required parameters must be sufficient to classify scope and risk.
- Optional parameters may refine depth, environment, or output format.
- Invalid or missing required parameters must fail fast with remediation guidance.
