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
/// A reasoning-tier ratio over a period. A period holding fewer than two records that carry both
/// a requested and a served tier yields the unmeasured case with that as its stated reason, and
/// the caveat, the assumption label and the definition travel with the output either way.
/// </summary>
public sealed record TierRatioReadModel
{
    public TierRatioReadModel(
        DateOnly period,
        MeasurementQuantity ratio,
        SingleRecordCaveat caveat,
        string assumptionLabel,
        string definition)
    {
        ArgumentNullException.ThrowIfNull(ratio);
        ArgumentNullException.ThrowIfNull(caveat);

        if (string.IsNullOrWhiteSpace(assumptionLabel))
        {
            throw new ArgumentException(
                "A split-derived figure states that the split it rests on is assumed rather than measured.",
                nameof(assumptionLabel));
        }

        // A CONSTRUCTION INVARIANT of the same shape as the caveat and the assumption label. A
        // ratio presented without saying which ratio it is gets read as the measured tier split,
        // and a definition added at presentation is one the first presentation does not carry.
        if (string.IsNullOrWhiteSpace(definition))
        {
            throw new ArgumentException(
                "A tier ratio states its definition wherever it is presented; without it the figure is read as the measured split.",
                nameof(definition));
        }

        Period = period;
        Ratio = ratio;
        Caveat = caveat;
        AssumptionLabel = assumptionLabel;
        Definition = definition;
    }

    public DateOnly Period { get; }
    public MeasurementQuantity Ratio { get; }
    public SingleRecordCaveat Caveat { get; }

    /// <summary>States that the split is assumed. Required; no output presents the split as measured.</summary>
    public string AssumptionLabel { get; }

    /// <summary>What the ratio is. Required; no output presents the ratio without saying what it is.</summary>
    public string Definition { get; }
}

/// <summary>
/// Throughput and queue quantities for one caller-supplied half-open period of instants.
///
/// Every quantity is one case of the closed three-case union, decided at one composing site by
/// one coverage rule, and the required statement says what the counts establish and what they do
/// not: they establish what the store read recorded over the period, and nothing about a
/// sustainable rate, a required buffer depth or a concurrency figure.
/// </summary>
public sealed record ThroughputReadModel
{
    public ThroughputReadModel(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        MeasurementQuantity waiting,
        MeasurementQuantity claimed,
        MeasurementQuantity retried,
        MeasurementQuantity escalated,
        MeasurementQuantity completed,
        string statement)
    {
        ArgumentNullException.ThrowIfNull(waiting);
        ArgumentNullException.ThrowIfNull(claimed);
        ArgumentNullException.ThrowIfNull(retried);
        ArgumentNullException.ThrowIfNull(escalated);
        ArgumentNullException.ThrowIfNull(completed);

        if (string.IsNullOrWhiteSpace(statement))
        {
            throw new ArgumentException(
                "A throughput reading states what its counts do and do not establish; without it a count is read as a rate.",
                nameof(statement));
        }

        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Waiting = waiting;
        Claimed = claimed;
        Retried = retried;
        Escalated = escalated;
        Completed = completed;
        Statement = statement;
    }

    /// <summary>The period's first instant, included.</summary>
    public DateTimeOffset PeriodStart { get; }

    /// <summary>The period's close, excluded.</summary>
    public DateTimeOffset PeriodEnd { get; }

    /// <summary>Units waiting to be claimed at the period's close.</summary>
    public MeasurementQuantity Waiting { get; }

    /// <summary>Claims made in the period.</summary>
    public MeasurementQuantity Claimed { get; }

    /// <summary>Stage attempts released for retry in the period.</summary>
    public MeasurementQuantity Retried { get; }

    /// <summary>Units escalated to the dead claim state in the period.</summary>
    public MeasurementQuantity Escalated { get; }

    /// <summary>Units reaching the terminal claim state in the period.</summary>
    public MeasurementQuantity Completed { get; }

    /// <summary>What the counts establish and what they do not. Required.</summary>
    public string Statement { get; }
}

/// <summary>
/// The item dossier of one item version as the company recorded it: one count per recorded
/// component.
///
/// Every count is unmeasured naming the register and the item version where no dossier was
/// opened, an observed zero where one was opened and holds no row of that component, and an
/// observed value otherwise. The counts are of RECORDED ROWS; none of them is a clip count, a
/// supply figure or a threshold.
/// </summary>
public sealed record ItemDossierReadModel
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }
    public required MeasurementQuantity Stages { get; init; }
    public required MeasurementQuantity TreatmentVerdicts { get; init; }
    public required MeasurementQuantity AudienceDesignations { get; init; }
    public required MeasurementQuantity Visuals { get; init; }
    public required MeasurementQuantity ClipOriginAssessments { get; init; }
    public required MeasurementQuantity ClaimAttributions { get; init; }
    public required MeasurementQuantity SupplyAuditEntries { get; init; }
    public required MeasurementQuantity DeterminationResolutions { get; init; }
    public required MeasurementQuantity RuntimeRecords { get; init; }
    public required MeasurementQuantity MetadataRecords { get; init; }
    public required MeasurementQuantity OriginalityAssessments { get; init; }

    /// <summary>What the recorded set does and does not establish. Required.</summary>
    public required string Statement { get; init; }
}

/// <summary>
/// The supply audit of one item version as recorded, one entry per recorded audit, with the count
/// of entries in its measurement case. No entry carries a clip count the recorded audit does not
/// establish, and the grouping key is the recorded subject term.
/// </summary>
public sealed record SupplyAuditReading
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }

    /// <summary>How many entries are recorded. Unmeasured where no dossier was opened.</summary>
    public required MeasurementQuantity RecordedEntries { get; init; }

    public required IReadOnlyList<SupplyAuditReadModel> Entries { get; init; }

    public required string Statement { get; init; }
}

/// <summary>
/// The compliance determinations of one item version as recorded: one entry per member of the
/// closed five-member set, with the count of recorded resolutions in its measurement case.
/// </summary>
public sealed record DeterminationReading
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }

    /// <summary>How many resolutions are recorded. Unmeasured where no dossier was opened.</summary>
    public required MeasurementQuantity RecordedResolutions { get; init; }

    public required IReadOnlyList<DeterminationOutcomeReadModel> Determinations { get; init; }

    public required string Statement { get; init; }
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
