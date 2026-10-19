# Skill: Error Handling

## Purpose
Provide reusable error handling patterns that improve reliability, debuggability, and user trust.

## Principles
- Errors are part of normal system behavior.
- Failures should be explicit and categorized.
- Recovery behavior should be intentional.
- User-facing messages should be clear and safe.

## Best Practices
- Classify errors by domain, validation, dependency, and infrastructure types.
- Return stable error contracts at public boundaries.
- Preserve stack and context when rethrowing exceptions.
- Use retries only for transient failures with bounded policies.

## Anti-patterns
- Catch-all blocks that hide root cause.
- Throwing generic exceptions without context.
- Returning success responses for failed operations.

## Examples
```csharp
catch (SqlException ex) when (IsTransient(ex))
{
    throw new DependencyTransientException("Database temporarily unavailable", ex);
}
```

## Decision Rules
- Use exceptions for exceptional paths, not control flow.
- Use domain result objects for expected business validation failures.
- Escalate repeated transient failures to circuit-breaking strategy.

## Common Mistakes
- Mapping all failures to HTTP 500.
- Omitting retry idempotency checks.
- Losing original exception context during translation.
