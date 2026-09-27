using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;

namespace MediaCompany.Domain.Analytics;

/// <summary>
/// The caveat that travels with a served tier or a split-derived figure (decision D-006).
///
/// It is a CONSTRUCTION INVARIANT, following the delivered exercise label that already refuses a
/// blank statement of class limits. A caveat that has to be added at presentation is a caveat the
/// first report does not carry, and no configuration value or display option can suppress
/// something a constructor demands.
/// </summary>
public sealed record SingleRecordCaveat
{
    public SingleRecordCaveat(MeasurementQuantity servedTierRecords, string statement)
    {
        ArgumentNullException.ThrowIfNull(servedTierRecords);

        if (string.IsNullOrWhiteSpace(statement))
        {
            throw new ArgumentException(
                "A served-tier output states what its record set does and does not establish; without it the figure is read as the measured split.",
                nameof(statement));
        }

        ServedTierRecords = servedTierRecords;
        Statement = statement;
    }

    /// <summary>How many served-tier records stand behind the output, as a measured quantity.</summary>
    public MeasurementQuantity ServedTierRecords { get; }

    public string Statement { get; }

    /// <summary>The standard statement. Authored here, never copied out of a record.</summary>
    public const string Standard =
        "A served-tier record establishes that the recording mechanism records what it claims to record. "
        + "It does not settle the assumed reasoning-tier split on which the per-item variable cost and the "
        + "monthly envelope both rest; that split remains an assumption and is not measured here.";

    public static SingleRecordCaveat For(MeasurementQuantity servedTierRecords) =>
        new(servedTierRecords, Standard);
}

/// <summary>
/// One refusal, answerable for the precondition that refused and the recorded state it refused
/// from — including where that recorded state was unknown, which stays visible here rather than
/// folding into a satisfied answer.
/// </summary>
public sealed record RefusalReadModel
{
    /// <summary>The precondition that refused.</summary>
    public required string Precondition { get; init; }

    /// <summary>The recorded state it refused from, stated as recorded: satisfied, not satisfied, unknown, or absent.</summary>
    public required string RecordedState { get; init; }

    public required string Detail { get; init; }
}

/// <summary>
/// A cost figure, answerable for the records behind it and for the prices in force when those
/// operations were metered.
/// </summary>
public sealed record CostReadModel
{
    /// <summary>The item this figure covers, or null where the figure covers a period.</summary>
    public ItemId? Item { get; init; }

    /// <summary>The period this figure covers, or null where the figure covers an item.</summary>
    public DateOnly? Period { get; init; }

    public required MeasurementQuantity Cost { get; init; }

    /// <summary>The number of recorded operations the figure was computed from.</summary>
    public required MeasurementQuantity Operations { get; init; }

    /// <summary>
    /// How the unit prices behind the figure are dated. Authored, and true of every row by
    /// construction: each recorded operation stores the price row it applied together with the
    /// unit prices that row carried at the moment of the write.
    /// </summary>
    public required string PriceBasis { get; init; }

    /// <summary>Whether any operation in the figure was accounted from estimated unit counts.</summary>
    public required bool ContainsEstimates { get; init; }
}

/// <summary>
/// One approval's effort, as three separately recorded quantities. No threshold, target or pass
/// line is stated against any of them, and the delivered exercise label travels with them.
/// </summary>
public sealed record ApprovalEffortReadModel
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }
    public required MeasurementQuantity Review { get; init; }
    public required MeasurementQuantity Queue { get; init; }
    public required MeasurementQuantity Rework { get; init; }
    public required ApprovalExerciseLabel Label { get; init; }
}

/// <summary>One production-path stage, as the surface reads it.</summary>
public sealed record StageOutcomeReadModel
{
    public required ProductionStage Stage { get; init; }

    /// <summary>
    /// The recorded outcome, or null where none was recorded. Where it is null
    /// <see cref="RecordedOutcomes"/> is unmeasured, and nothing is inferred from a later stage.
    /// </summary>
    public MediaCompany.Domain.Work.StageOutcome? RecordedOutcome { get; init; }

    /// <summary>How many outcomes are recorded for this stage. Unmeasured where none is.</summary>
    public required MeasurementQuantity RecordedOutcomes { get; init; }

    public required string Statement { get; init; }
}

/// <summary>
/// What the surface shows at the composition gate transition (decision D-004).
///
/// Nothing is recorded at that transition, and nothing is inferred from the existence of the
/// dispatch record. The step reads as unmeasured, and the records that DO exist are named so the
/// absence is diagnosable rather than merely stated.
/// </summary>
public sealed record CompositionStepReadModel
{
    /// <summary>Always unmeasured: no gate transition was recorded at composition.</summary>
    public required MeasurementQuantity GateTransitions { get; init; }

    /// <summary>The dispatch record that does exist, named as recorded evidence.</summary>
    public required string DispatchRecordEvidence { get; init; }

    /// <summary>The audit entry that does exist, named as recorded evidence.</summary>
    public required string AuditEntryEvidence { get; init; }

    public required string Statement { get; init; }
}

/// <summary>Every production-path stage outcome for one item version, plus every refusal.</summary>
public sealed record StageOutcomeSurface
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }

    /// <summary>One entry per member of the closed twelve-stage set, whatever was recorded.</summary>
    public required IReadOnlyList<StageOutcomeReadModel> Stages { get; init; }

    public required CompositionStepReadModel CompositionStep { get; init; }

    public IReadOnlyList<RefusalReadModel> Refusals { get; init; } = [];
}

/// <summary>
/// One audited subject's supply position. A subject whose clip count was never established shows
/// as unmeasured and carries no number in its place.
/// </summary>
public sealed record SupplyAuditReadModel
{
    /// <summary>The subject as requested. The grouping key is the recorded term, never a channel name.</summary>
    public required string Subject { get; init; }

    public required string Library { get; init; }
    public required TermFidelity Fidelity { get; init; }
    public required MeasurementQuantity ClipCount { get; init; }

    /// <summary>What would obtain a count, where none was obtained.</summary>
    public required string WhatWouldObtainIt { get; init; }

    public required DateTimeOffset AuditedAt { get; init; }
}

/// <summary>
/// One compliance determination's outcome. A determination with no recorded outcome reads as
/// unmeasured rather than as a negative outcome, and no outcome is inferred from another's.
/// </summary>
public sealed record DeterminationOutcomeReadModel
{
    public required ComplianceDetermination Determination { get; init; }

    /// <summary>The recorded outcome, or null where none was recorded.</summary>
    public DeterminationOutcome? RecordedOutcome { get; init; }

    public required MeasurementQuantity RecordedResolutions { get; init; }

    public required string Statement { get; init; }
}

/// <summary>
/// One recorded operation's tier pair, as recorded. The served value is whatever the admitting
/// route stated, and null is the explicit absence marker rather than an unset field.
/// </summary>
public sealed record ServedTierRecord
{
    public required OperationId Operation { get; init; }
    public required DateOnly Period { get; init; }
    public ReasoningTier? Requested { get; init; }
    public ReasoningTier? Served { get; init; }

    public bool CarriesTierEvidence => Requested is not null && Served is not null;
}

/// <summary>One served-tier record as the surface reports it, with its caveat attached.</summary>
public sealed record ServedTierReadModel
{
    public required OperationId Operation { get; init; }
    public ReasoningTier? Requested { get; init; }
    public ReasoningTier? Served { get; init; }

    /// <summary>What the served position is: the tier the route stated, or the absence marker.</summary>
    public required string ServedStatement { get; init; }

    public required SingleRecordCaveat Caveat { get; init; }
}

/// <summary>
/// A reasoning-tier ratio over a period (decision D-007). A period holding fewer than two
/// served-tier records yields the unmeasured case with that as its stated reason, and the caveat
/// travels with the output either way.
/// </summary>
public sealed record TierRatioReadModel
{
    public TierRatioReadModel(DateOnly period, MeasurementQuantity ratio, SingleRecordCaveat caveat, string assumptionLabel)
    {
        ArgumentNullException.ThrowIfNull(ratio);
        ArgumentNullException.ThrowIfNull(caveat);

        if (string.IsNullOrWhiteSpace(assumptionLabel))
        {
            throw new ArgumentException(
                "A split-derived figure states that the split it rests on is assumed rather than measured.",
                nameof(assumptionLabel));
        }

        Period = period;
        Ratio = ratio;
        Caveat = caveat;
        AssumptionLabel = assumptionLabel;
    }

    public DateOnly Period { get; }
    public MeasurementQuantity Ratio { get; }
    public SingleRecordCaveat Caveat { get; }

    /// <summary>States that the split is assumed. Required; no output presents the split as measured.</summary>
    public string AssumptionLabel { get; }
}

/// <summary>
/// One of the six revenue-derived figures, lit. An instance exists only where an observed revenue
/// parameter was supplied to the admission function, and it carries the recorded source alongside
/// the figure so a reader sees what lit it.
/// </summary>
public sealed record RevenueDerivedFigureReadModel
{
    public required RevenueDerivedFigure Figure { get; init; }
    public required MeasurementQuantity Quantity { get; init; }

    /// <summary>The source observation the admitting parameter came from.</summary>
    public required string RecordedSource { get; init; }

    public required DateOnly SourceObservedOn { get; init; }
}
