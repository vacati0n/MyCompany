using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;

namespace MediaCompany.Deterministic.Analytics;

/// <summary>
/// The analytics composers (decision D-002).
///
/// They are declared in the rule-determined assembly and add NO project reference to it, so the
/// delivered reference-closure assertions cover them unchanged: the assembly reaches the domain
/// and the ports and nothing else, which means a reasoning capability is not merely forbidden
/// here — the types are not referenceable, so a call to one does not compile.
///
/// Every composer is a pure function over records the four completed delivery waves already
/// write. None creates a record, and an absent record resolves to the unmeasured case rather than
/// being filled in.
/// </summary>
public static class AnalyticsComposers
{
    /// <summary>
    /// How the unit prices behind a cost figure are dated. Authored here, and true of every row by
    /// construction: each recorded operation stores the price row it applied together with the
    /// unit prices that row carried at the moment of the write, so the figure stays re-derivable
    /// after a price has been superseded.
    /// </summary>
    public const string PriceBasis =
        "each operation carries the price row applied and the unit prices that row held at the moment the "
        + "operation was metered, so every figure is stated against the prices in force at metering time";

    /// <summary>
    /// States that the reasoning-tier split a figure rests on is assumed rather than measured.
    /// </summary>
    public const string SplitAssumption =
        "the reasoning-tier split this figure rests on is an assumption carried from the cost model; "
        + "it has not been measured, and nothing here presents it as measured";

    // -----------------------------------------------------------------------
    // Cost (plan task T-006)
    // -----------------------------------------------------------------------

    /// <summary>
    /// One item's cost. The operation count decides the measurement state: a period or an item
    /// holding no recorded operation is UNMEASURED, never a zero amount, which is the concrete
    /// defect the currency-returning member cannot express.
    /// </summary>
    public static CostReadModel Cost(ItemSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return new CostReadModel
        {
            Item = summary.Item,
            Cost = FromOperations(
                summary.Operations,
                summary.Total,
                $"no operation is recorded for item {summary.Item}"),
            Operations = MeasurementQuantity.Count(summary.Operations, "operations"),
            PriceBasis = PriceBasis,
            ContainsEstimates = summary.ContainsEstimates,
        };
    }

    /// <summary>One calendar period's cost, on the same rule.</summary>
    public static CostReadModel Cost(PeriodSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return new CostReadModel
        {
            Period = summary.Period,
            Cost = FromOperations(
                summary.Operations,
                summary.Total,
                $"no operation is recorded in period {summary.Period:yyyy-MM}"),
            Operations = MeasurementQuantity.Count(summary.Operations, "operations"),
            PriceBasis = PriceBasis,
            ContainsEstimates = summary.ContainsEstimates,
        };
    }

    /// <summary>
    /// The rule both cost figures share. With no recorded operation behind it there is nothing to
    /// aggregate and the figure is unmeasured; with records behind it the aggregation the
    /// datastore computed is reported as observed, and an aggregation of zero is an observed zero.
    /// </summary>
    public static MeasurementQuantity FromOperations(long operations, Domain.Accounting.Money total, string absenceDetail) =>
        operations == 0
            ? MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, absenceDetail)
            : MeasurementQuantity.Observed(total.Amount, total.Currency);

    // -----------------------------------------------------------------------
    // Approval effort (plan task T-008)
    // -----------------------------------------------------------------------

    /// <summary>
    /// One approval's effort as three separate quantities, mapped from the delivered measurement.
    ///
    /// The delivered derivation is reused as it stands: it already separates review, queue and
    /// rework, already treats a first-pass approval's absent rework as an observed zero, and
    /// already names which quantity was missing. No threshold, target or pass line is stated
    /// against any of the three, here or anywhere the read model travels.
    /// </summary>
    public static ApprovalEffortReadModel ApprovalEffort(ApprovalMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        var unmeasured = measurement.Resolution == ApprovalMeasurementResolution.Unmeasured;

        return new ApprovalEffortReadModel
        {
            Item = measurement.Item,
            Version = measurement.Version,
            Review = Interval(measurement.ReviewTime, unmeasured, "review", measurement.UnmeasuredReason),
            Queue = Interval(measurement.QueueTime, unmeasured, "queue", measurement.UnmeasuredReason),
            Rework = Interval(measurement.ReworkTime, unmeasured, "rework", measurement.UnmeasuredReason),
            Label = measurement.Label,
        };
    }

    private static MeasurementQuantity Interval(TimeSpan? interval, bool unmeasured, string component, string reason) =>
        interval is { } value && !unmeasured
            ? MeasurementQuantity.Observed((decimal)value.TotalMinutes, "minutes")
            : MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"{component} time is not derivable from the recorded marks: "
                + (string.IsNullOrWhiteSpace(reason) ? "no mark was recorded" : reason));

    // -----------------------------------------------------------------------
    // Production-path stage outcomes and refusals (plan tasks T-009, and D-004)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Every stage of the closed twelve-stage path, whatever was recorded.
    ///
    /// The closed set drives the result rather than the recorded outcomes, so a stage with nothing
    /// recorded produces an entry that reads unmeasured rather than producing no entry at all.
    /// Nothing is inferred from a later stage's outcome.
    /// </summary>
    public static StageOutcomeSurface StageOutcomes(
        ItemDossier dossier,
        PublishReadyVerdict verdict,
        FirstPublicationConditionRegister conditions)
    {
        ArgumentNullException.ThrowIfNull(dossier);
        ArgumentNullException.ThrowIfNull(verdict);
        ArgumentNullException.ThrowIfNull(conditions);

        var stages = new List<StageOutcomeReadModel>(ProductionStageSet.Count);

        foreach (var stage in ProductionStageSet.All)
        {
            var evidence = dossier.StageFor(stage);

            stages.Add(new StageOutcomeReadModel
            {
                Stage = stage,
                RecordedOutcome = evidence?.Outcome,
                RecordedOutcomes = evidence is null
                    ? MeasurementQuantity.NotMeasured(
                        UnmeasuredReason.NoObservationExists,
                        $"no outcome is recorded for stage {stage}; nothing is inferred from any later stage")
                    : MeasurementQuantity.Count(1, "recorded outcomes"),
                Statement = evidence is null
                    ? $"stage {stage} has no recorded outcome"
                    : $"stage {stage} recorded outcome {evidence.Outcome} at {evidence.RecordedAt:O}",
            });
        }

        return new StageOutcomeSurface
        {
            Item = dossier.Item,
            Version = dossier.Version,
            Stages = stages,
            CompositionStep = CompositionStep(),
            Refusals = Refusals(dossier, verdict, conditions),
        };
    }

    /// <summary>
    /// The composition step (decision D-004). Nothing was recorded at that gate transition and
    /// nothing is recorded at it by this change, so the step reads unmeasured and names the
    /// dispatch record and the audit entry that DO exist. No gate state is inferred from the
    /// existence of a dispatch record, and the caller-supplied transition parameter is untouched.
    /// </summary>
    public static CompositionStepReadModel CompositionStep() => new()
    {
        GateTransitions = MeasurementQuantity.NotMeasured(
            UnmeasuredReason.NoObservationExists,
            "no gate transition was recorded at composition"),
        DispatchRecordEvidence =
            "the dispatch record written by the dispatch service in the same transaction as the composition",
        AuditEntryEvidence =
            "the audit entry written by the dispatch service in that same transaction",
        Statement =
            "nothing is recorded at the composition gate transition, so the step is unmeasured; the dispatch "
            + "record and the audit entry that do exist are named as the recorded evidence, and no gate state "
            + "is inferred from either",
    };

    /// <summary>
    /// Every refusal, each answerable for the precondition that refused and the recorded state it
    /// refused from.
    ///
    /// The three coded conditions of first publication contribute their standings, where UNKNOWN
    /// and ABSENT both refuse but stay visible as themselves rather than folding into a satisfied
    /// answer or into one another.
    /// </summary>
    public static IReadOnlyList<RefusalReadModel> Refusals(
        ItemDossier dossier,
        PublishReadyVerdict verdict,
        FirstPublicationConditionRegister conditions)
    {
        ArgumentNullException.ThrowIfNull(dossier);
        ArgumentNullException.ThrowIfNull(verdict);
        ArgumentNullException.ThrowIfNull(conditions);

        var refusals = verdict.Refusals
            .Select(refusal => new RefusalReadModel
            {
                Precondition = refusal.Refusal.ToString(),
                RecordedState = RecordedStateFor(refusal, dossier),
                Detail = refusal.Detail,
            })
            .ToList();

        refusals.AddRange(conditions.Unsatisfied().Select(standing => new RefusalReadModel
        {
            Precondition = standing.Condition.ToString(),
            RecordedState = standing.Resolution.ToString(),
            Detail = standing.Describe(),
        }));

        return refusals;
    }

    /// <summary>
    /// The recorded state a refusal refused from, read back from the dossier where the dossier
    /// holds it. Where the delivered predicate's own detail is the statement of that state, the
    /// state is reported as recorded-in-the-dossier and the detail carries it verbatim.
    /// </summary>
    private static string RecordedStateFor(PublishReadyRefusalDetail refusal, ItemDossier dossier) =>
        refusal.Refusal switch
        {
            PublishReadyRefusal.RuntimeNotRecorded => "absent",
            PublishReadyRefusal.RuntimeBelowFloor =>
                dossier.Runtime is { } runtime ? runtime.ToString() : "absent",
            PublishReadyRefusal.DeterminationUnresolved => "recorded in the item dossier",
            PublishReadyRefusal.StageOutcomeMissing => "recorded in the item dossier",
            _ => "recorded in the item dossier",
        };

    // -----------------------------------------------------------------------
    // Supply audit (plan task T-010)
    // -----------------------------------------------------------------------

    /// <summary>
    /// One entry per audited subject. A subject whose clip count was never established reads as
    /// unmeasured and carries no number in its place; no count appears that the recorded audit
    /// does not establish.
    ///
    /// The grouping key is the recorded subject term, so no figure, grouping or label here assumes
    /// the current subject is permanent.
    /// </summary>
    public static IReadOnlyList<SupplyAuditReadModel> SupplyAudit(IReadOnlyList<SupplyAuditEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return entries.Select(entry => new SupplyAuditReadModel
        {
            Subject = entry.RequestedTerm,
            Library = entry.Library,
            Fidelity = entry.Fidelity,
            ClipCount = ClipCount(entry),
            WhatWouldObtainIt = entry.HasAdmissibleCount
                ? "not applicable: the audit carries an admissible count"
                : entry.WhatWouldObtainIt ?? "not recorded",
            AuditedAt = entry.AuditedAt,
        }).ToArray();
    }

    private static MeasurementQuantity ClipCount(SupplyAuditEntry entry)
    {
        if (entry.HasAdmissibleCount)
        {
            return MeasurementQuantity.Observed(entry.Count!.Value, "clips");
        }

        // A literal answer of "none" IS a count, of zero. Every other unobtained reason is an
        // absence, and the two are reported as different facts.
        if (entry.UnobtainedReason == CountUnobtainedReason.LibraryReportsNone
            && entry.Fidelity == TermFidelity.LiteralSingleToken)
        {
            return MeasurementQuantity.Zero("clips");
        }

        return entry.UnobtainedReason switch
        {
            CountUnobtainedReason.TermNotAnsweredLiterally => MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"the library answered '{entry.TermAnswered ?? "a different term"}' rather than '{entry.RequestedTerm}', "
                + "so its reported total is not a count"),
            CountUnobtainedReason.RequiresPerClipConfirmation => MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"supply for '{entry.RequestedTerm}' can be established only by confirming clips by eye"),
            CountUnobtainedReason.LibrarySurfaceUnreachable => MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"the library's search surface was not reachable for '{entry.RequestedTerm}'"),
            _ => MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"no clip count was established for '{entry.RequestedTerm}'"),
        };
    }

    // -----------------------------------------------------------------------
    // Compliance determinations (plan task T-011)
    // -----------------------------------------------------------------------

    /// <summary>
    /// One entry per member of the closed five-member determination set. A determination with no
    /// recorded outcome reads as unmeasured rather than as a negative outcome, and no outcome is
    /// inferred from another determination's outcome.
    /// </summary>
    public static IReadOnlyList<DeterminationOutcomeReadModel> Determinations(
        IReadOnlyList<DeterminationResolution> resolutions)
    {
        ArgumentNullException.ThrowIfNull(resolutions);

        return Enum.GetValues<ComplianceDetermination>().Select(determination =>
        {
            var resolution = resolutions.FirstOrDefault(r => r.Determination == determination);

            return new DeterminationOutcomeReadModel
            {
                Determination = determination,
                RecordedOutcome = resolution?.Outcome,
                RecordedResolutions = resolution is null
                    ? MeasurementQuantity.NotMeasured(
                        UnmeasuredReason.NoObservationExists,
                        $"no outcome is recorded for determination {determination}; "
                        + "nothing is inferred from any other determination")
                    : MeasurementQuantity.Count(1, "recorded resolutions"),
                Statement = resolution is null
                    ? $"determination {determination} has no recorded outcome"
                    : $"determination {determination} resolved {resolution.Outcome} at {resolution.ResolvedAt:O}",
            };
        }).ToArray();
    }

    // -----------------------------------------------------------------------
    // Served reasoning tier (plan tasks T-012, T-014)
    // -----------------------------------------------------------------------

    /// <summary>
    /// The served-tier records of a period, each carrying the single-record caveat as a
    /// construction argument. A record whose admitting route stated no tier carries the explicit
    /// absence marker and no tier value in its place.
    /// </summary>
    public static IReadOnlyList<ServedTierReadModel> ServedTiers(IReadOnlyList<ServedTierRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var caveat = SingleRecordCaveat.For(MeasurementQuantity.Count(records.Count, "served-tier records"));

        return records.Select(record => new ServedTierReadModel
        {
            Operation = record.Operation,
            Requested = record.Requested,
            Served = record.Served,
            ServedStatement = record.Served is { } served
                ? $"the admitted route stated {served}"
                : "absent: the admitted route stated no reasoning tier, and no value stands in its place",
            Caveat = caveat,
        }).ToArray();
    }

    /// <summary>
    /// The share of served-tier records in which the admitted route served the tier the request
    /// asked for (decision D-007).
    ///
    /// A period holding fewer than two served-tier records yields the UNMEASURED case with that as
    /// its stated reason: one record establishes the mechanism and settles no ratio. The caveat and
    /// the split-assumption label are construction arguments, so no output of this function exists
    /// without them.
    /// </summary>
    public static TierRatioReadModel TierRatio(DateOnly period, IReadOnlyList<ServedTierRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var served = records.Where(r => r.Served is not null).ToArray();
        var caveat = SingleRecordCaveat.For(MeasurementQuantity.Count(served.Length, "served-tier records"));

        if (served.Length < 2)
        {
            return new TierRatioReadModel(
                period,
                MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.NoObservationExists,
                    $"fewer than two served-tier records exist in period {period:yyyy-MM} "
                    + $"({served.Length} recorded), so no tier ratio can be read from it"),
                caveat,
                SplitAssumption);
        }

        var matching = served.Count(r => r.Requested is not null && r.Requested == r.Served);

        return new TierRatioReadModel(
            period,
            MeasurementQuantity.Observed(decimal.Divide(matching * 100, served.Length), "percent"),
            caveat,
            SplitAssumption);
    }

    // -----------------------------------------------------------------------
    // The six revenue-derived figures (plan task T-007)
    // -----------------------------------------------------------------------

    /// <summary>The closed set of six. Present in the surface, and visible in no view while unlit.</summary>
    public static IReadOnlyList<RevenueDerivedFigure> RevenueDerived { get; } =
        Enum.GetValues<RevenueDerivedFigure>();

    /// <summary>
    /// The ONLY path to a revenue-derived figure.
    ///
    /// The observed parameter is a required constructed argument, and the only way to construct
    /// one is from a register row carrying the source observation it came from and that
    /// observation's date. With nothing recorded there is no instance to pass, so this function is
    /// not callable and the six are unlit by default rather than filtered out of a view. No
    /// assumed, modelled, default or placeholder value is expressible for any of them.
    /// </summary>
    public static RevenueDerivedFigureReadModel Admit(
        RevenueDerivedFigure figure,
        ObservedRevenueParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return new RevenueDerivedFigureReadModel
        {
            Figure = figure,
            Quantity = figure == RevenueDerivedFigure.Revenue
                ? parameter.Observation
                : MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.SourceCannotStateOne,
                    $"{figure} is derived from the observed revenue parameter and recorded cost by an "
                    + "aggregation that belongs in the datastore's exact decimal type; the surface performs "
                    + "no money arithmetic, and no such aggregation is delivered over the register"),
            RecordedSource = parameter.SourceObservation,
            SourceObservedOn = parameter.ObservedOn,
        };
    }

    /// <summary>
    /// The lit figures, given whatever the register holds. Empty while no recorded row admits,
    /// which is every state this change delivers: the register is created empty.
    /// </summary>
    public static IReadOnlyList<RevenueDerivedFigureReadModel> Visible(
        IReadOnlyList<RevenueParameterRecord> recorded)
    {
        ArgumentNullException.ThrowIfNull(recorded);

        var parameter = recorded
            .Select(ObservedRevenueParameter.Admit)
            .FirstOrDefault(admitted => admitted is not null);

        return parameter is null
            ? []
            : RevenueDerived.Select(figure => Admit(figure, parameter)).ToArray();
    }
}
