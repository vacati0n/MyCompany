# Skill: Logging and Observability

## Purpose
Provide reusable logging practices that accelerate diagnosis, auditing, and operational decision-making.

## Principles
- Logs are structured, contextual, and actionable.
- Correlation across service boundaries is mandatory.
- Signal quality is more important than log volume.
- Sensitive data must be protected.

## Best Practices
- Use structured fields for identifiers and outcomes.
- Include correlation and request identifiers in all critical paths.
- Log meaningful state transitions and failures.
- Define log levels consistently by operational impact.

## Anti-patterns
- Unstructured string-only logs.
- Logging in tight loops without sampling controls.
- Logging secrets or personal data.

## Examples
```csharp
_logger.LogInformation("Order approved {OrderId} by {UserId}", orderId, userId);
```

## Decision Rules
- Use `Information` for key business events.
- Use `Warning` for recoverable anomalies.
- Use `Error` for failed operations requiring action.

## Common Mistakes
- Missing correlation IDs in distributed calls.
- Excessive debug logs in production paths.
- Logging exceptions without domain context.
