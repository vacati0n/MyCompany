# Skill: Security Engineering

## Purpose
Provide reusable secure engineering practices that reduce exploitability and protect confidentiality, integrity, and availability.

## Principles
- Secure by default, explicit by exception.
- Least privilege for identities and operations.
- Defense in depth across application layers.
- Security controls must be testable and observable.

## Best Practices
- Validate and sanitize all external input.
- Use parameterized queries and output encoding.
- Protect secrets using managed secret stores.
- Enforce authentication and authorization at boundaries.

## Anti-patterns
- Hardcoded credentials or tokens in source code.
- Broad role grants without audit rationale.
- Logging sensitive payload fields.

## Examples
```csharp
if (!user.HasPermission(Permissions.OrdersApprove))
{
    return Results.Forbid();
}
```

## Decision Rules
- Treat unauthenticated and unauthorized access separately.
- Block release when critical vulnerabilities are unresolved.
- Require threat review for new external exposure.

## Common Mistakes
- Trusting client-side validation.
- Missing dependency vulnerability checks.
- Ignoring security headers and cookie policies.
