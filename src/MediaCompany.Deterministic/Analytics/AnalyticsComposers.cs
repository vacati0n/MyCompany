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
    /// One approval's effort as three separate quantities, each mapped from ITS OWN component
    /// result at the delivered derivation site.
    ///
    /// The mapping no longer reads the whole-row resolution, which made every component unmeasured
    /// whenever any one mark was missing: a review interval that was derived is now reported as
    /// derived whatever happened to the queue mark. A derived zero is an observed zero, a derived
    /// non-zero interval an observed value in minutes, an absent mark unmeasured because no
    /// observation exists, and inconsistent marks unmeasured because the source cannot state one,
    /// each naming the mark. No threshold, target or pass line is stated against any of the three,
    /// here or anywhere the read model travels, and no combined figure is formed.
    /// </summary>
    public static ApprovalEffortReadModel ApprovalEffort(ApprovalMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        return new ApprovalEffortReadModel
        {
            Item = measurement.Item,
            Version = measurement.Version,
            Review = Component(measurement.ReviewComponent, "review"),
            Queue = Component(measurement.QueueComponent, "queue"),
            Rework = Component(measurement.ReworkComponent, "rework"),
            Label = measurement.Label,
        };
    }

    private static MeasurementQuantity Component(ApprovalComponentResult result, string component) =>
        result.State switch
        {
            ApprovalComponentState.Derived when result.Interval is { } interval =>
                MeasurementQuantity.Observed((decimal)interval.TotalMinutes, "minutes"),
            ApprovalComponentState.MarksInconsistent => MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"{component} time is not derivable from the recorded marks: {result.Statement}"),
            ApprovalComponentState.MarkAbsent => MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"{component} time is not derivable from the recorded marks: {result.Statement}"),
            _ => throw new InvalidOperationException(
                "Unreachable: a derived component carries its interval, and the component state set has three members."),
        };

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

        // The caveat counts SERVED-TIER records, so it counts the records that carry a served tier
        // and not every record in the period. Counting the period made one period report two
        // different counts across two outputs carrying the same caveat, and a reader comparing them
        // could not tell which count the caveat answered for.
        var caveat = SingleRecordCaveat.For(ServedTierCount(records));

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
    /// What the tier ratio is. Authored here, once, and carried by every ratio as a required
    /// construction argument, so no output presents the ratio without saying which ratio it is.
    /// </summary>
    public const string TierRatioDefinition =
        "the agreement share: of the operations recorded in the period whose record carries both a requested "
        + "and a served reasoning tier, the share in percent whose served tier equals its requested tier; it is "
        + "computed from the recorded pairs alone, needs no assumed split and no ordering of tiers, and it is "
        + "not the reasoning-tier split the cost model assumes";

    /// <summary>
    /// The agreement share over the records that carry BOTH a requested and a served tier.
    ///
    /// The delivered meaning is kept and its denominator corrected: a record carrying a served
    /// tier and no requested tier has nothing to agree or disagree with, so it no longer counts as
    /// a mismatch. A period holding fewer than two records that carry both tiers yields the
    /// UNMEASURED case with that as its stated reason, which also guarantees at least two
    /// served-tier records behind any ratio presented. The caveat, the split-assumption label and
    /// the definition are construction arguments, so no output of this function exists without
    /// them.
    /// </summary>
    public static TierRatioReadModel TierRatio(DateOnly period, IReadOnlyList<ServedTierRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var carryingBoth = records.Where(r => r.CarriesTierEvidence).ToArray();
        var caveat = SingleRecordCaveat.For(ServedTierCount(records));
        var agreeing = carryingBoth.Count(r => r.Requested == r.Served);

        return new TierRatioReadModel(
            period,
            TierRatioFromCounts(period, carryingBoth.Length, agreeing, "period"),
            caveat,
            SplitAssumption,
            TierRatioDefinition);
    }

    /// <summary>
    /// The agreement share's ONE rule, over counts: unmeasured where fewer than two records carry
    /// both tiers, otherwise the share in percent. The delivered reading over a record list and the
    /// channel partition over the datastore's counts both decide here, so the two cannot drift.
    /// </summary>
    public static MeasurementQuantity TierRatioFromCounts(DateOnly period, long carryingBoth, long agreeing, string scope) =>
        carryingBoth < 2
            ? MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"fewer than two served-tier records that also carry a requested tier exist in {scope} {period:yyyy-MM} "
                + $"({carryingBoth} recorded), so no tier ratio can be read from it")
            : MeasurementQuantity.Observed(decimal.Divide(agreeing * 100, carryingBoth), "percent");

    // -----------------------------------------------------------------------
    // The six revenue-derived figures (plan task T-007)
    // -----------------------------------------------------------------------

    /// <summary>
    /// How many of a record set carry a served tier. One definition, used by every output that
    /// carries the caveat, so one period reports one count.
    /// </summary>
    public static MeasurementQuantity ServedTierCount(IReadOnlyList<ServedTierRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return MeasurementQuantity.Count(records.Count(r => r.Served is not null), "served-tier records");
    }

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

    // -----------------------------------------------------------------------
    // Throughput and queue quantities for a period
    // -----------------------------------------------------------------------

    /// <summary>
    /// What a throughput reading establishes and what it does not. Authored here and carried by
    /// every reading as a required construction argument.
    /// </summary>
    public const string ThroughputStatement =
        "these counts establish what the record store that was read held for the period and nothing more: "
        + "no sustainable rate, no required buffer depth and no concurrency figure is established by them, and a "
        + "count read from a demonstration store is a demonstration parameter rather than an observation of the "
        + "company's own work";

    /// <summary>
    /// The ONE composing site at which every throughput quantity's case is decided.
    ///
    /// The coverage rule: a period lying wholly after the append-only record's earliest entry and
    /// already elapsed when the datastore read it is one over which the count was taken, so a
    /// count of none there is an OBSERVED ZERO. Any other period is UNMEASURED because no
    /// observation exists, naming the coverage bounds, and no count is presented for it at all. A
    /// reconstructed waiting count below zero means the record is incomplete for the queue, and is
    /// unmeasured because the source cannot state one rather than clamped to zero.
    /// </summary>
    public static ThroughputReadModel Throughput(ThroughputSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        if (summary.PeriodEnd <= summary.PeriodStart)
        {
            throw new ArgumentException("A period closes after it starts.", nameof(summary));
        }

        // The read never found a quiet instant: audited transitions were in flight at every
        // attempt to close the record, so no count it could give would be final.
        if (!summary.Quiet || summary.Horizon is not { } horizon)
        {
            return Unmeasured(
                summary,
                UnmeasuredReason.SourceCannotStateOne,
                "audited transitions were in flight at every bounded attempt to close the append-only record, "
                + "so no count over the period would be final");
        }

        // OBSERVED only where the period starts no earlier than the record's first entry and ends no
        // later than the horizon this read set, below which no entry can commit any more.
        var covered = summary.EarliestEntry is { } earliest
            && summary.PeriodStart >= earliest
            && summary.PeriodEnd <= horizon;

        if (!covered)
        {
            return Unmeasured(summary, UnmeasuredReason.NoObservationExists, CoverageDetail(summary, horizon));
        }

        // Waiting at the close: every arrival into the ready queue up to the close, less every
        // claim that took a unit out of it. Arrivals are enqueues, non-final successes and
        // retries; escalation and completion leave from the claimed state, not the ready one.
        var arrivals = summary.EnqueuedToClose + summary.StageSucceededToClose + summary.RetriedToClose;
        var waiting = arrivals - summary.ClaimedToClose;

        var waitingQuantity = waiting < 0
            ? MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"the append-only record up to {summary.PeriodEnd:O} holds more claims ({summary.ClaimedToClose}) than "
                + $"arrivals into the ready queue ({arrivals}), so the units waiting at the close cannot be reconstructed "
                + "from it; a claim state was changed by something other than the lifecycle and dispatch services")
            : MeasurementQuantity.Count(waiting, "units waiting");

        return new ThroughputReadModel(
            summary.PeriodStart,
            summary.PeriodEnd,
            waitingQuantity,
            MeasurementQuantity.Count(summary.ClaimedInPeriod, "claims"),
            MeasurementQuantity.Count(summary.RetriedInPeriod, "retried stage attempts"),
            MeasurementQuantity.Count(summary.EscalatedInPeriod, "escalated units"),
            MeasurementQuantity.Count(summary.CompletedInPeriod, "completed units"),
            ThroughputStatement);
    }

    private static ThroughputReadModel Unmeasured(ThroughputSummary summary, UnmeasuredReason reason, string detail) =>
        new(
            summary.PeriodStart,
            summary.PeriodEnd,
            MeasurementQuantity.NotMeasured(reason, "units waiting at the close: " + detail),
            MeasurementQuantity.NotMeasured(reason, "claims: " + detail),
            MeasurementQuantity.NotMeasured(reason, "retried stage attempts: " + detail),
            MeasurementQuantity.NotMeasured(reason, "escalated units: " + detail),
            MeasurementQuantity.NotMeasured(reason, "completed units: " + detail),
            ThroughputStatement);

    private static string CoverageDetail(ThroughputSummary summary, DateTimeOffset horizon)
    {
        var period = $"the period from {summary.PeriodStart:O} to {summary.PeriodEnd:O}";

        if (summary.EarliestEntry is not { } earliest)
        {
            return $"the append-only record holds no entry, so no count was taken over {period}";
        }

        if (summary.PeriodStart < earliest)
        {
            return $"{period} begins before the append-only record's earliest entry at {earliest:O}, "
                + "so no count was taken over all of it";
        }

        return $"{period} ends after the record horizon this read set at {horizon:O}, "
            + "so entries stamped inside it could still commit and its count is not final";
    }

    // -----------------------------------------------------------------------
    // The four record sources, read back from the item dossier register
    // -----------------------------------------------------------------------

    /// <summary>What a dossier reading establishes and what it does not.</summary>
    public const string DossierStatement =
        "these counts are of rows the item dossier register holds for this item version, recorded through the "
        + "dossier recorder, as recorded at the read; the dossier accumulates, so they claim no permanence; they "
        + "establish what was recorded and nothing about what was produced, and none of them is a clip count, a "
        + "supply figure or a threshold";

    /// <summary>What a supply-audit reading establishes and what it does not.</summary>
    public const string SupplyAuditStatement =
        "one entry per recorded audit, grouped by the recorded subject term; a clip count appears only where the "
        + "recorded audit establishes one, and an entry whose count was never established carries no number";

    /// <summary>What a determination reading establishes and what it does not.</summary>
    public const string DeterminationStatement =
        "one entry per member of the closed determination set; a determination with no recorded outcome reads "
        + "unmeasured, and no outcome is inferred from another determination's outcome";

    private static string NoDossier(ItemId item, ItemVersion version, string lookedFor) =>
        $"{lookedFor} were looked for in the item dossier register for item {item} version {version}, "
        + "and no dossier is opened there for that version";

    /// <summary>
    /// A count of recorded rows for one source: unmeasured naming the register and the version
    /// where no dossier was opened, and an observed count (zero included) where one was.
    /// </summary>
    private static MeasurementQuantity RecordedRows(
        bool opened,
        long rows,
        ItemId item,
        ItemVersion version,
        string unit) =>
        opened
            ? MeasurementQuantity.Count(rows, unit)
            : MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, NoDossier(item, version, unit));

    /// <summary>
    /// The item dossier of one item version, one count per component. With no dossier opened every
    /// count is unmeasured naming the register and the version; with one opened, a component with
    /// no row is an observed zero and any other an observed value.
    /// </summary>
    public static ItemDossierReadModel DossierCounts(ItemId item, ItemVersion version, RecordedDossier? recorded)
    {
        var opened = recorded is not null;
        var dossier = recorded?.Dossier;

        MeasurementQuantity Rows(Func<ItemDossier, long> count, string unit) =>
            RecordedRows(opened, dossier is null ? 0 : count(dossier), item, version, unit);

        return new ItemDossierReadModel
        {
            Item = item,
            Version = version,
            Stages = Rows(d => d.Stages.Count, "stage outcomes"),
            TreatmentVerdicts = Rows(d => d.Treatment.Count, "treatment verdicts"),
            AudienceDesignations = Rows(d => d.Audience is null ? 0 : 1, "audience designations"),
            Visuals = Rows(d => d.Visuals.Count, "visual provenance records"),
            ClipOriginAssessments = Rows(d => d.ClipOrigins.Count, "clip origin assessments"),
            ClaimAttributions = Rows(d => d.Claims.Count, "claim attributions"),
            SupplyAuditEntries = Rows(d => d.SupplyAudit.Count, "supply audit entries"),
            DeterminationResolutions = Rows(d => d.Determinations.Count, "determination resolutions"),
            RuntimeRecords = Rows(d => d.Runtime is null ? 0 : 1, "runtime records"),
            MetadataRecords = Rows(d => d.Metadata is null ? 0 : 1, "metadata records"),
            OriginalityAssessments = Rows(d => d.Originality is null ? 0 : 1, "originality assessments"),
            Statement = DossierStatement,
        };
    }

    /// <summary>
    /// The stage-outcome surface where NO dossier is opened for the item version: every stage of
    /// the closed set unmeasured, naming the register and the version, and NO refusal listed,
    /// because no predicate is evaluated over a dossier that does not exist.
    /// </summary>
    public static StageOutcomeSurface StageOutcomesUnrecorded(ItemId item, ItemVersion version) => new()
    {
        Item = item,
        Version = version,
        Stages = ProductionStageSet.All.Select(stage => new StageOutcomeReadModel
        {
            Stage = stage,
            RecordedOutcome = null,
            RecordedOutcomes = MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                NoDossier(item, version, $"outcomes of stage {stage}")),
            Statement = $"stage {stage} has no recorded outcome: no dossier is opened for this item version",
        }).ToArray(),
        CompositionStep = CompositionStep(),
        Refusals = [],
    };

    /// <summary>
    /// The supply audit of one item version as recorded. With no dossier opened the entry count is
    /// unmeasured naming the register and the version, and no entry is listed.
    /// </summary>
    public static SupplyAuditReading SupplyAuditRecorded(
        ItemId item,
        ItemVersion version,
        IReadOnlyList<SupplyAuditEntry>? entries) => new()
    {
        Item = item,
        Version = version,
        RecordedEntries = RecordedRows(entries is not null, entries?.Count ?? 0, item, version, "supply audit entries"),
        Entries = entries is null ? [] : SupplyAudit(entries),
        Statement = SupplyAuditStatement,
    };

    /// <summary>
    /// The determinations of one item version as recorded, over the closed set. With no dossier
    /// opened the resolution count is unmeasured naming the register and the version.
    /// </summary>
    public static DeterminationReading DeterminationsRecorded(
        ItemId item,
        ItemVersion version,
        IReadOnlyList<DeterminationResolution>? resolutions) => new()
    {
        Item = item,
        Version = version,
        RecordedResolutions = RecordedRows(
            resolutions is not null, resolutions?.Count ?? 0, item, version, "determination resolutions"),
        Determinations = Determinations(resolutions ?? []),
        Statement = DeterminationStatement,
    };
}
