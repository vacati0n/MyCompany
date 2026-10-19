# Memory: Architecture

## Purpose

Persist durable architectural knowledge that guides system structure, boundaries, and integration strategy.

## Structure

- Architecture Principle ID
- Statement
- Scope (system, bounded context, service, module)
- Rationale
- Constraints and implications
- Related decisions and references

## Ownership

- Primary Owner: Tech Lead
- Approver: Architect
- Contributors: Backend Developer, Frontend Developer, Reviewer

## Update Rules

- Update when architecture-affecting change is approved.
- Include rationale and impact on existing components.
- Mark superseded principles explicitly.
- Record update date and responsible owner.

## Consumers

- Orchestrator for workflow routing and risk checks.
- Architect and Tech Lead for design governance.
- Implementers and Reviewers for compliance validation.

## Lifecycle

- Draft during design exploration.
- Active after architect approval.
- Deprecated when replaced by newer principle.
- Archived when no active systems depend on it.

