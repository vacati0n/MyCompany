# Skill: Avalonia UI Engineering

## Purpose
Provide reusable guidance for building maintainable, accessible, and predictable Avalonia desktop interfaces.

## Principles
- UI logic belongs in view models, not code-behind.
- State transitions must be explicit and observable.
- Accessibility is a baseline requirement.
- UI behavior should degrade gracefully under failure.

## Best Practices
- Use MVVM with testable commands and bindings.
- Define reusable styles and control templates centrally.
- Model loading, empty, success, and error states explicitly.
- Keep view models small and feature-focused.

## Anti-patterns
- Business logic in event handlers.
- Hard-coded visual constants across multiple views.
- Silent UI failures without user feedback.

## Examples
```csharp
public bool IsBusy { get; private set; }
public async Task LoadAsync()
{
	IsBusy = true;
	try { Items = await _service.GetItemsAsync(); }
	finally { IsBusy = false; }
}
```

## Decision Rules
- Create a new reusable control when UI behavior repeats in multiple views.
- Prefer data binding over manual UI manipulation.
- Require keyboard navigation support for all core actions.

## Common Mistakes
- Missing cancellation handling for long-running commands.
- Coupling view model logic directly to visual controls.
- Ignoring focus and accessibility behavior during testing.
