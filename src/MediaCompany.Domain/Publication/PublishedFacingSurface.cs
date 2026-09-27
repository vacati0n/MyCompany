using MediaCompany.Domain.Dossier;

namespace MediaCompany.Domain.Publication;

/// <summary>
/// The five published-facing surfaces (module M-025). One component set behind one contract, so
/// the screening discipline and the declared-destination parameter are each stated once rather
/// than five times.
/// </summary>
public enum SurfaceKind
{
    Title = 1,
    Description = 2,
    TagSet = 3,
    ThumbnailSelection = 4,
    Captions = 5,
}

/// <summary>
/// The outcome the step that produced a surface recorded. A step that produced nothing records
/// <see cref="NotProduced"/> WITH its reason: it never resolves as absent or as skipped, because
/// an unrecorded non-production is indistinguishable from a step that was never run.
/// </summary>
public enum SurfaceProductionOutcome
{
    /// <summary>Produced and screened clean.</summary>
    Produced = 1,

    /// <summary>The step ran and deliberately produced nothing. The reason is mandatory.</summary>
    NotProduced = 2,

    /// <summary>Produced and refused at screening. The refusal names the condition or the term.</summary>
    RefusedAtScreening = 3,
}

/// <summary>
/// One produced surface with the outcome of the step that produced it, bound to the exact item
/// version it was produced from.
///
/// A surface is HELD only after it has been screened. The constructor refuses a
/// <see cref="SurfaceProductionOutcome.Produced"/> record that carries no screening assessment,
/// so an unscreened surface cannot enter the record at all — screening is a precondition of
/// holding rather than a check performed afterwards.
/// </summary>
public sealed record SurfaceRecord
{
    public SurfaceRecord(
        SurfaceKind kind,
        ItemId item,
        ItemVersion version,
        SurfaceProductionOutcome outcome,
        string content,
        string reason,
        IReadOnlyList<TreatmentVerdict> treatmentAssessment,
        DateTimeOffset producedAt)
    {
        ArgumentNullException.ThrowIfNull(treatmentAssessment);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Every surface record states why it has the outcome it has, including a produced one.",
                nameof(reason));
        }

        if (outcome == SurfaceProductionOutcome.Produced)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException(
                    "A produced surface carries its content.", nameof(content));
            }

            // Screening precedes holding. A produced surface without an assessment against all
            // nine treatment conditions is not a surface this record can carry.
            var assessed = treatmentAssessment.Select(v => v.Condition).Distinct().Count();
            if (assessed != Enum.GetValues<TreatmentCondition>().Length)
            {
                throw new ArgumentException(
                    "A produced surface carries a recorded assessment against all nine treatment conditions; "
                    + $"{assessed} of {Enum.GetValues<TreatmentCondition>().Length} were assessed.",
                    nameof(treatmentAssessment));
            }
        }

        Kind = kind;
        Item = item;
        Version = version;
        Outcome = outcome;
        Content = content;
        Reason = reason;
        TreatmentAssessment = treatmentAssessment;
        ProducedAt = producedAt;
    }

    public SurfaceKind Kind { get; }
    public ItemId Item { get; }
    public ItemVersion Version { get; }
    public SurfaceProductionOutcome Outcome { get; }

    /// <summary>The surface content. Empty for a surface that was deliberately not produced.</summary>
    public string Content { get; }

    /// <summary>Why the surface has the outcome it has. Mandatory for all three outcomes.</summary>
    public string Reason { get; }

    /// <summary>The recorded assessment against all nine treatment conditions.</summary>
    public IReadOnlyList<TreatmentVerdict> TreatmentAssessment { get; }

    public DateTimeOffset ProducedAt { get; }

    /// <summary>Whether this surface may be carried into a dispatch descriptor.</summary>
    public bool IsHoldable => Outcome != SurfaceProductionOutcome.RefusedAtScreening;
}
