# Skill: .NET Engineering

## Purpose
Provide reusable .NET implementation patterns for reliability, maintainability, and operational clarity.

## Principles
- Prefer explicit dependencies and clear object lifetimes.
- Keep async flows cancellation-aware.
- Treat contracts as versioned commitments.
- Design for observability from the start.

## Best Practices
- Use dependency injection with explicit composition roots.
- Keep application services thin and domain logic cohesive.
- Use cancellation tokens across I/O boundaries.
- Validate inputs at boundaries and fail fast with context.

## Anti-patterns
- Static service locators and hidden dependencies.
- Sync-over-async in request paths.
- Swallowing exceptions without telemetry.

## Examples
```csharp
public async Task<Result<OrderDto>> HandleAsync(Command cmd, CancellationToken ct)
{
	if (cmd.CustomerId == Guid.Empty) return Result.Invalid("CustomerId is required");
	var entity = await _repo.GetByIdAsync(cmd.CustomerId, ct);
	return entity is null ? Result.NotFound("Customer not found") : Result.Ok(Map(entity));
}
```

## Decision Rules
- Introduce abstraction when it protects a boundary or enables testability.
- Use records for immutable data transfer models.
- Prefer resilient retries only for transient external failures.

## Common Mistakes
- Over-abstracting simple flows.
- Forgetting cancellation in database and HTTP calls.
- Logging only exception messages without correlation context.
