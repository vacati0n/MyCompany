# Skill Specification

## Purpose

Define the canonical skill domain concept for reusable engineering knowledge
applied across agents and workflows.

## Responsibilities

- Capture stable, reusable practices and decision heuristics.
- Define quality expectations for implementation and review.
- Provide anti-patterns and failure-avoidance guidance.
- Support cross-agent consistency through shared references.

## Required Properties

- Skill ID
- Name
- Category
- Purpose Statement
- Decision Rules
- Best Practices
- Anti-patterns
- Applicability Scope
- Dependency Metadata
- Status
- Version

## Lifecycle

1. Proposed: candidate knowledge submitted.
2. Curated: content quality reviewed.
3. Approved: available for workflow usage.
4. Active: referenced by agents and workflows.
5. Deprecated: replacement guidance designated.
6. Retired: archived and excluded from active mapping.

## Relationships

- Referenced by agents as capability dependencies.
- Referenced by workflows as phase requirements.
- May depend on configuration policies.
- Influences template output quality through standards.

## Constraints

- Skills must be domain-scoped, not task-scoped.
- Normative statements must be verifiable.
- Conflicting skill rules require explicit precedence resolution.
- Skills cannot redefine workflow gate authority.

## Versioning Strategy

- Semantic versioning per skill definition.
- MAJOR for breaking rule or structure changes.
- MINOR for additive guidance and examples.
- PATCH for wording fixes and metadata corrections.

## Validation Rules

- Required properties must be complete.
- Category must match approved taxonomy.
- Referenced dependencies must exist.
- Decision rules and anti-patterns must not conflict.

## Extension Points

- Category-specific annexes for specialized domains.
- Policy adaptation blocks for compliance contexts.
- Optional maturity levels for capability progression.

## Backward Compatibility

- Preserve Skill ID across non-breaking updates.
- Maintain alias mapping for renamed categories.
- Provide migration notes when rule precedence changes.
