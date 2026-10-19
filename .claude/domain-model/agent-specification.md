# Agent Specification

## Purpose

Define the canonical agent domain concept, including ownership boundaries,
interaction contracts, and execution accountability.

## Responsibilities

- Own one or more workflow phases within a bounded role.
- Consume inputs defined by workflow and command contracts.
- Produce traceable outputs mapped to templates.
- Escalate decisions outside role authority.
- Enforce policy compliance during execution.

## Required Properties

- Agent ID
- Name
- Domain Role
- Authority Scope
- Supported Workflow Phases
- Input Contract
- Output Contract
- Decision Rights
- Escalation Targets
- Status
- Version

## Lifecycle

1. Defined: agent contract authored.
2. Approved: governance review completed.
3. Active: agent assigned in workflows.
4. Deprecated: replacement agent designated.
5. Retired: removed from active routing.

## Relationships

- Belongs to one or more workflows as phase participant.
- Depends on zero or more skills for execution quality.
- Is invoked by commands through workflow routing.
- Produces artifacts persisted in memory when durable.
- Is governed by configuration and validation rules.

## Constraints

- One primary owner per task scope.
- No execution outside declared authority scope.
- Mandatory handoff at phase boundaries.
- Outputs must be auditable and reproducible.

## Versioning Strategy

- Semantic versioning per agent contract: MAJOR.MINOR.PATCH.
- MAJOR for breaking contract changes.
- MINOR for backward-compatible capability additions.
- PATCH for clarifications and non-behavioral fixes.

## Validation Rules

- Required properties must be present and non-empty.
- Referenced workflows and skills must exist.
- Escalation targets must be resolvable.
- Output contract must map to existing templates.

## Extension Points

- Add domain-specific capabilities via optional capability blocks.
- Add policy hooks for security, compliance, or risk checks.
- Add additional handoff contracts for cross-team execution.

## Backward Compatibility

- Preserve existing Agent ID across non-breaking updates.
- Keep deprecated fields readable for at least one major cycle.
- Provide explicit migration notes for breaking changes.
