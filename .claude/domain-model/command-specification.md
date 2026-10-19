# Command Specification

## Purpose

Define the canonical command domain concept that exposes framework operations
through stable, validated entry points.

## Responsibilities

- Accept execution intent and parameters.
- Resolve intent to a primary workflow.
- Enforce parameter validation and policy checks.
- Initiate execution with traceable context.
- Return structured outcomes and failure diagnostics.

## Required Properties

- Command ID
- Public Name
- Intent Type
- Workflow Mapping
- Parameter Schema
- Validation Schema
- Output Contract
- Error Contract
- Access Policy
- Status
- Version

## Lifecycle

1. Defined: command contract authored.
2. Validated: schema and mapping checks pass.
3. Approved: command published in command catalog.
4. Active: available for execution.
5. Deprecated: replacement command announced.
6. Retired: removed from active API.

## Relationships

- Invokes one primary workflow.
- Indirectly coordinates participating agents via workflow.
- Enforces configuration policies at invocation time.
- Produces outputs mapped to templates and memory updates.

## Constraints

- Exactly one primary workflow mapping per command.
- Required parameters must be sufficient for risk classification.
- Output and error contracts must be deterministic.
- Unauthorized execution paths must fail closed.

## Versioning Strategy

- Semantic versioning per command contract.
- MAJOR for breaking parameter or output changes.
- MINOR for additive parameters with safe defaults.
- PATCH for non-breaking clarifications.

## Validation Rules

- Parameter schema must declare required and optional fields.
- Workflow mapping must reference an active workflow.
- Output contract must include status, artifacts, and gate outcomes.
- Error contract must include code, message, and remediation guidance.

## Extension Points

- Optional parameter groups for domain-specific execution.
- Pre-execution hooks for policy or risk scoring.
- Post-execution hooks for reporting and telemetry.

## Backward Compatibility

- Preserve public command name across non-breaking versions.
- Support deprecated parameters for at least one major cycle.
- Provide compatibility shims for renamed output fields.
