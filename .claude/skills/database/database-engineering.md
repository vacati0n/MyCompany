# Skill: Database Engineering

## Purpose
Provide reusable data modeling and persistence practices for correctness, scalability, and operational safety.

## Principles
- Data integrity is non-negotiable.
- Schema changes must be backward-compatible when possible.
- Query performance is designed, not accidental.
- Transactions protect consistency boundaries.

## Best Practices
- Model entities around domain invariants.
- Use explicit migration scripts with rollback strategy.
- Index based on query patterns, not guesswork.
- Keep read and write paths observable with metrics.

## Anti-patterns
- Nullable fields used to represent multiple states ambiguously.
- Unbounded queries in user-facing endpoints.
- Combining unrelated updates in a single transaction.

## Examples
```sql
CREATE INDEX IX_Orders_CustomerId_CreatedAt
ON Orders(CustomerId, CreatedAt DESC);
```

## Decision Rules
- Add index only for measured query bottlenecks.
- Normalize by default; denormalize for proven read-scale needs.
- Use optimistic concurrency where concurrent edits are expected.

## Common Mistakes
- Missing migration verification in staging.
- Ignoring lock contention under load.
- Deleting data without retention policy checks.
