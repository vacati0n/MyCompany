# Skill: Business Analysis and Domain Modeling

## Purpose
Create shared business understanding and robust domain models that can be validated and implemented consistently.

## Principles
- Business language is the source of truth for domain concepts.
- Invariants are explicit and enforceable.
- Scope boundaries are clear before implementation starts.
- Acceptance criteria must be testable.

## Best Practices
- Define bounded contexts before modeling entities.
- Use value objects for immutable business concepts.
- Map business events and state transitions explicitly.
- Trace each requirement to a validation approach.

## Anti-patterns
- Ambiguous terms with multiple meanings.
- Entity models that combine unrelated responsibilities.
- Requirements that describe implementation instead of behavior.

## Examples
- Business rule: "An order cannot be shipped unless payment is confirmed."
- Derived invariant: `Order.Status` cannot transition to `Shipped` from `PendingPayment`.

## Decision Rules
- Create a new bounded context when terminology or ownership diverges.
- Reject requirements that cannot be verified by objective criteria.
- Escalate conflicting rules before coding begins.

## Common Mistakes
- Skipping negative scenarios in requirement definition.
- Allowing hidden assumptions to drive implementation.
- Failing to define non-goals, causing scope creep.
