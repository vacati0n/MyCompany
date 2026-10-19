# Workflow Specification

## Purpose

Define the canonical workflow domain concept used to structure lifecycle execution,
quality gates, ownership, and recovery behavior.

## Responsibilities

- Decompose delivery intent into ordered phases.
- Define entry and exit criteria per phase.
- Assign agent ownership for phase execution and approval gates.
- Capture deliverables and traceability requirements.
- Define failure handling and re-entry behavior.

## Required Properties

- Workflow ID
- Name
- Intent Type
- Phase Sequence
- Entry Conditions
- Exit Conditions
- Gate Definitions
- Participant Agents
- Deliverable Set
- Recovery Rules
- Status
- Version

## Lifecycle

1. Drafted: workflow structure created.
2. Reviewed: architecture and governance review complete.
3. Approved: eligible for command routing.
4. Active: executed by operational commands.
5. Deprecated: superseded by replacement workflow.
6. Retired: removed from active use.

## Relationships

- Invoked by one or more commands.
- Orchestrates multiple agents by phase.
- Requires skills by phase or risk profile.
- Produces template-based artifacts.
- Updates memory with durable outcomes.
- Must comply with configuration policies.

## Constraints

- Phase order must be deterministic.
- Mandatory gates cannot be skipped.
- Gate owners must be explicit and unique per gate.
- Recovery path must exist for each gate failure.

## Versioning Strategy

- Semantic versioning per workflow contract.
- MAJOR for phase-structure or gate-structure breaking changes.
- MINOR for additive phases, gates, or metadata.
- PATCH for editorial or non-breaking clarifications.

## Validation Rules

- Entry and exit conditions must be testable.
- Every phase must have at least one responsible agent.
- Every gate must have owner and decision outcome criteria.
- Deliverables must reference existing template definitions.

## Extension Points

- Optional phase modules for domain-specific execution.
- Risk-based gate overlays for regulated environments.
- Alternate recovery strategies by severity tier.

## Backward Compatibility

- Existing commands must continue to resolve to a valid workflow path.
- Deprecated phases must provide transition mappings.
- Gate outcome semantics must remain stable across minor versions.
