# Skill: Architecture Foundations

## Purpose
Provide reusable architecture guidance for designing maintainable, modular, and scalable systems.

## Principles
- Business rules remain independent from infrastructure.
- Dependency flow points from outer layers to inner abstractions.
- Boundaries are explicit and enforced by module structure.
- Integration concerns are isolated through adapters.

## Best Practices
- Model use cases in an application layer with clear inputs and outputs.
- Keep domain models pure and free of framework coupling.
- Define interface contracts at boundaries and implement adapters externally.
- Record architecture decisions when tradeoffs are significant.

## Anti-patterns
- Treating controllers as business-logic containers.
- Leaking persistence models into domain logic.
- Sharing utility modules that hide cross-layer dependencies.
- Speculative abstraction, generalization, or configuration for a requirement that does not exist.

## Examples
```csharp
public interface IOrderRepository
{
	Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
	Task SaveAsync(Order order, CancellationToken ct);
}

public sealed class PlaceOrderUseCase
{
	private readonly IOrderRepository _orders;
	public PlaceOrderUseCase(IOrderRepository orders) => _orders = orders;
}
```

## Decision Rules
- Introduce a new layer only when it reduces coupling or clarifies ownership.
- Prefer composition over inheritance across architectural boundaries.
- Reject designs that require inward layers to know infrastructure details.

## Common Mistakes
- Creating abstractions without real variation points.
- Mixing orchestration logic with domain invariants.
- Delaying architecture decisions until integration failures occur.
- Reducing line count by removing validation, error handling, or tests.

## Necessity and Reuse Ladder

Applies to every design option, every change-set entry, and every review of a change.
Understand the problem first: read the task and the code it touches, and trace the real
flow end to end. Then climb the ladder and stop at the first rung that holds.

1. Does this need to exist? A capability no accepted statement requires is not built; it
   is recorded as out of scope or raised as an open question to the owning role.
2. Does the codebase already have it? Reuse the existing helper, component, or pattern.
3. Does the standard library solve it? Use it.
4. Does the platform or framework in use solve it natively? Use it.
5. Does an already-installed dependency solve it? Use it.
6. Can it be written directly in a few lines at the call site? Write it there.
7. Only then: implement the minimum code necessary. A new abstraction, file, or dependency
   is introduced only when the reason no lower rung held is recorded.

### Minimum Necessary Change

Required behaviour, required safety, required integration, and required tests together
are the minimum necessary change. Everything outside that boundary requires a recorded
justification: opportunistic refactoring, cleanup of code the change does not touch,
speculative generalization, and configuration or extension points for requirements that
do not yet exist.

### Safety Floor

The ladder minimizes unnecessary code, never necessary protection. No simplification may
remove or weaken input validation at a trust boundary, error handling that prevents data
loss, authorization or audit paths, accessibility, observability the design requires, data
integrity, or the tests that prove the change. A shorter implementation is not
automatically better: correctness, readability, and maintainability are requirements, and
line count is not a criterion. Where two equally small solutions differ, the
edge-case-correct one is chosen.

### Review Questions

A change is measured against this ladder with seven questions, each answered against the
accepted change and the code, never against line count:

- Existence: was something built that no accepted statement requires?
- Reuse: does the change duplicate something the codebase already provides?
- Dependency: was a dependency added where the standard library, the platform, or an
  installed dependency already served?
- Abstraction: was an interface, wrapper, factory, helper class, or configuration point
  introduced before a second concrete use required it?
- Complexity: does a simpler implementation exist with the same behaviour and the same
  safety?
- Scope: did the change touch code the accepted change did not require?
- Safety: did a simplification remove or weaken anything the safety floor protects?

A finding under the first six questions is a maintainability or architecture finding at
the severity the evidence supports. A finding under the seventh is a correctness or
security finding.
