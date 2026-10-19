# Skill: React Engineering

## Purpose
Provide reusable frontend engineering guidance for building maintainable, testable, and performant React applications.

## Principles
- Components should have single, clear responsibilities.
- State ownership should be explicit and minimal.
- Data flow should remain predictable.
- UI behavior must be resilient to loading and error conditions.

## Best Practices
- Keep presentational and container responsibilities separated.
- Use custom hooks for shared stateful behavior.
- Co-locate tests with components and hooks.
- Implement clear loading, empty, and error states.

## Anti-patterns
- Prop drilling across deep trees without composition strategy.
- Uncontrolled side effects inside render logic.
- Global state for local component concerns.

## Examples
```tsx
function UserList() {
  const { users, isLoading, error } = useUsers();
  if (isLoading) return <Spinner />;
  if (error) return <ErrorPanel message={error.message} />;
  if (users.length === 0) return <EmptyState />;
  return <List items={users} />;
}
```

## Decision Rules
- Use local state by default and promote only when shared usage requires it.
- Memoize only where measurements show rendering pressure.
- Split components when they mix unrelated responsibilities.

## Common Mistakes
- Overusing context for frequently changing values.
- Skipping dependency arrays or suppressing hook warnings.
- Not testing user-visible state transitions.
