namespace MediaCompany.Domain.Work;

/// <summary>
/// The publishing workflow definition (module M-013), declared over the DELIVERED lifecycle.
///
/// STRUCTURAL ABSENCE TWO, and it is delivered here rather than in a set of its own on purpose.
/// The engine advances a unit to the next position the workflow definition it is given declares,
/// so the only way to make "there is no position after composition" a property of the ENGINE
/// — rather than a property of an enumeration sitting beside it — is for the publishing workflow
/// to be a real definition the engine consumes.
///
/// A unit that finishes <see cref="LifecyclePosition.PublishingComposed"/> therefore reaches a
/// terminal claim state, because <see cref="WorkflowDefinition.Next"/> returns null for it and the
/// engine writes the terminal state in that case. There is no position to move to, so there is
/// nothing to hang a transport behind: adding one would mean adding a stage to this list, which is
/// a code change the build-time boundary check refuses.
/// </summary>
public static class PublishingWorkflow
{
    /// <summary>The workflow name, as the job record carries it.</summary>
    public const string Name = "publishing";

    /// <summary>
    /// The closed, ordered position set. Composition is LAST, and the boundary check asserts that
    /// over this declaration and over every other workflow definition the build declares.
    /// </summary>
    public static WorkflowDefinition Definition { get; } = new(
        Name,
        [
            LifecyclePosition.Queued,
            LifecyclePosition.PublishingSurfaceProduction,
            LifecyclePosition.PublishingTimingComputation,
            LifecyclePosition.PublishingGateEvaluation,
            LifecyclePosition.PublishingComposed,
        ],
        FailurePolicy.Default);

    /// <summary>
    /// The position a unit reaches when it finishes composition.
    ///
    /// Derived from the definition rather than stated: if the definition ever declared a stage
    /// after composition this would return that stage, which is precisely why the boundary check
    /// asserts it cannot.
    /// </summary>
    public static LifecyclePosition AfterComposition() =>
        Definition.Next(LifecyclePosition.PublishingComposed) ?? LifecyclePosition.Completed;

    /// <summary>Whether composition is terminal in this workflow. The whole of absence two.</summary>
    public static bool CompositionIsTerminal() =>
        Definition.IsTerminal(LifecyclePosition.PublishingComposed);
}
