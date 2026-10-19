# Skill: Testing Strategy

## Purpose
Define reusable testing practices that provide fast feedback and strong confidence across change types.

## Principles
- Test behavior, not implementation details.
- Match test depth to risk and impact.
- Keep tests deterministic and isolated.
- Treat regression prevention as mandatory for fixes.

## Best Practices
- Use unit tests for business rules and pure logic.
- Use integration tests for external boundaries and adapters.
- Use end-to-end tests for critical user journeys.
- Map tests directly to acceptance criteria.

## Anti-patterns
- Relying only on end-to-end tests.
- Large fixture setups that hide intent.
- Flaky tests accepted as normal.

## Examples
```csharp
[Fact]
public void CannotShipOrder_WhenPaymentPending()
{
	var order = Order.CreatePendingPayment();
	var result = order.TryShip();
	Assert.False(result.IsSuccess);
}
```

## Decision Rules
- Add regression tests for every confirmed bug fix.
- Increase integration coverage when boundary behavior changes.
- Block release when critical-path tests fail.

## Common Mistakes
- Missing negative-path coverage.
- Asserting too many conditions in one test.
- Ignoring production-like data in integration tests.
