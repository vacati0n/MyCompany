using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Management;

namespace MediaCompany.Domain.Analytics;

// ---------------------------------------------------------------------------
// The AI-management change: the report line, the report set, the recommendation and its abstention, the
// CEO brief and the dashboard tile, and the controller's recorded decisions read back (module M-012 of its
// design).
//
// Every figure on every type here is a LineFigure: one case of the closed three-case measurement union, or
// a recorded amount in one of its three shapes. No member is a bare number, and every string member is an
// authored label the build-time membership assertion names, or recorded text printed verbatim and labelled
// as recorded.
// ---------------------------------------------------------------------------

/// <summary>
/// The case a figure is in, as every surface names it. Five words for four cases and the absent recorded
/// amount: an observed value, an observed zero, an unmeasured reading, a recorded amount, and a recorded
/// amount nobody recorded. The last two are never observations; the middle one carries no value.
/// </summary>
public enum FigureCase
{
    Observed = 1,
    ObservedZero = 2,
    Unmeasured = 3,
    Recorded = 4,
    NotRecorded = 5,

    /// <summary>
    /// A figure read from a store designated DEMONSTRATION (the production change, decision D-005 of its design): what fake
    /// providers booked there. Never an observation of the company's work or spend, never counted, and rendered apart
    /// from observed, observed zero and unmeasured.
    /// </summary>
    Demonstration = 6,
}

/// <summary>
/// The figure of one report line: a measurement quantity, or a recorded amount. CLOSED at two shapes by a
/// private constructor, so a line cannot carry a figure that is neither, and the measurement union stays at
/// three cases with the recorded amount outside it (decision D-003).
/// </summary>
public abstract record LineFigure
{
    private LineFigure()
    {
    }

    /// <summary>A measurement quantity, in the case its composer decided.</summary>
    public sealed record Measured : LineFigure
    {
        internal Measured(MeasurementQuantity quantity) => Quantity = quantity;

        public MeasurementQuantity Quantity { get; }
    }

    /// <summary>A recorded amount: recorded somewhere, never observed.</summary>
    public sealed record Recorded : LineFigure
    {
        internal Recorded(RecordedAmount amount) => Amount = amount;

        public RecordedAmount Amount { get; }
    }

    /// <summary>
    /// A figure of a DEMONSTRATION store (the production change, decision D-005 of its design): the quantity the store
    /// holds, booked by fake providers, carried only so a reader sees what the demonstration did. It carries no value a
    /// rule, a total or a surface may read as an observation.
    /// </summary>
    public sealed record Demonstration : LineFigure
    {
        internal Demonstration(MeasurementQuantity quantity) => Quantity = quantity;

        public MeasurementQuantity Quantity { get; }
    }

    /// <summary>The figure of a demonstration store for a measured quantity.</summary>
    public static LineFigure Demonstrated(MeasurementQuantity quantity) =>
        new Demonstration(quantity ?? throw new ArgumentNullException(nameof(quantity)));

    public static LineFigure Of(MeasurementQuantity quantity) =>
        new Measured(quantity ?? throw new ArgumentNullException(nameof(quantity)));

    public static LineFigure Of(RecordedAmount amount) =>
        new Recorded(amount ?? throw new ArgumentNullException(nameof(amount)));

    /// <summary>The case, decided by the shape and never by a number.</summary>
    public FigureCase Case => this switch
    {
        Measured { Quantity: MeasurementQuantity.ObservedValue } => FigureCase.Observed,
        Measured { Quantity: MeasurementQuantity.ObservedZero } => FigureCase.ObservedZero,
        Measured => FigureCase.Unmeasured,
        Recorded { Amount: RecordedAmount.NotRecorded } => FigureCase.NotRecorded,
        Recorded => FigureCase.Recorded,
        Demonstration => FigureCase.Demonstration,
        _ => throw new InvalidOperationException("Unreachable: a line figure has three shapes."),
    };

    /// <summary>Whether the figure carries a value at all: false for an unmeasured reading and an amount nobody recorded.</summary>
    public bool CarriesAValue => Case is FigureCase.Observed or FigureCase.ObservedZero or FigureCase.Recorded;

    /// <summary>The figure's own delivered rendering, unchanged.</summary>
    public string Describe() => this switch
    {
        Measured measured => measured.Quantity.Describe(),
        Recorded recorded => recorded.Amount.Describe(),
        Demonstration demonstration => "a DEMONSTRATION figure of " + demonstration.Quantity switch
        {
            MeasurementQuantity.ObservedValue value => $"{value.Amount} {value.Unit}",
            MeasurementQuantity.ObservedZero zero => $"0 {zero.Unit}",
            var other => other.Describe(),
        } + ", read from a store designated demonstration, where fake providers run; never an observation of the company's work or spend",
        _ => throw new InvalidOperationException("Unreachable: a line figure has three shapes."),
    };

    /// <summary>The word every surface prints for a case. Six words, all different.</summary>
    public static string CaseWord(FigureCase figureCase) => figureCase switch
    {
        FigureCase.Observed => "observed",
        FigureCase.ObservedZero => "observed zero",
        FigureCase.Unmeasured => "unmeasured",
        FigureCase.Recorded => "recorded",
        FigureCase.NotRecorded => "not recorded",
        FigureCase.Demonstration => "demonstration",
        _ => throw new ArgumentOutOfRangeException(nameof(figureCase)),
    };
}

/// <summary>Which report a line belongs to.</summary>
public enum ReportKind
{
    Coo = 1,
    Cto = 2,
    Cfo = 3,
    ChannelPerformance = 4,
    Risk = 5,
}

/// <summary>
/// What a line reads, so the rules, the brief and a reader can find it without matching its label. A key can
/// recur: one line per channel, month, task pair, decision or held request.
/// </summary>
public enum ReportLineKey
{
    UnitsCompleted = 1,
    ItemsPublished = 2,
    ItemsPlanned = 3,
    BufferDepth = 4,
    AwaitingApproval = 5,
    RightsCheckSendBackRate = 6,
    OwnerSendBackRate = 7,
    RejectionRate = 8,
    RevisionRate = 9,
    PolicyRejectionRate = 10,
    SendBackReason = 11,
    UnitsWaiting = 12,
    QueuePerStage = 13,
    UnitsEscalated = 14,
    StageRetries = 15,
    OperationsFailed = 16,
    HeldOperations = 17,
    HeldRequest = 18,
    ChannelAwaitingApproval = 19,
    ChannelPublished = 20,
    WeekCost = 21,
    CeilingUtilisation = 22,
    OwnersTen = 23,
    TaskComparableRuns = 24,
    RouteQuality = 25,
    RouteCost = 26,
    ControllerDecisions = 27,
    ControllerDecision = 28,
    ControllerReading = 29,
    TierOperations = 30,
    TierCost = 31,
    ReverificationRechecked = 32,
    ReverificationChanged = 33,
    ReverificationOverdue = 34,
    CompanySpend = 35,
    Ceiling = 36,
    Envelope = 37,
    EnvelopeUtilisation = 38,
    StandingCharge = 39,
    StandingCommitment = 40,
    ChannelSpend = 41,
    ChannelBudget = 42,
    ChannelUtilisation = 43,
    RevenueDerived = 44,
    Audience = 45,
    ChannelWeekCost = 46,
    Conditions = 47,
    ProgrammeThreshold = 48,
    ProgrammeProgress = 49,
    DaysToThresholdChange = 50,
    CopyrightSendBacks = 51,
    CopyrightClaims = 52,
    PolicySendBacks = 53,
    Monetisation = 54,
    ProviderFailures = 55,
    HeldWork = 56,
    NextDatedChange = 57,
    DaysToNextDatedChange = 58,
    EscalatedDeferrals = 59,
    ControllerActions = 60,
    RuleOutputsNotShown = 61,
    ChannelAlerts = 62,

    // The production change (decision D-017 of its design): the produced item's lines.
    ProducedItemStages = 63,
    ProducedItemOperations = 64,
    ProducedItemCap = 65,
    ProducedItemRuntime = 66,
}

/// <summary>
/// One line of one report: its section, its label, its figure in its case, the reading and composer it
/// rests on, the report's one datastore instant, the finality of what it reads, and, where the line prints
/// something the company recorded in words, that recorded text verbatim with the instant it was recorded.
/// <see cref="Informs"/> names the owner decision the line informs; a line informing none is never shown in
/// the brief.
/// </summary>
public sealed record ReportLine
{
    public required ReportKind Report { get; init; }
    public required ReportLineKey Key { get; init; }
    public required string Heading { get; init; }
    public required string Label { get; init; }
    public required LineFigure Figure { get; init; }
    public required string RestsOn { get; init; }
    public required DateTimeOffset Instant { get; init; }
    public required string Finality { get; init; }
    public string? RecordedText { get; init; }
    public DateTimeOffset? RecordedAt { get; init; }
    public string? Informs { get; init; }
}

/// <summary>The closed catalogue of recommendation rules, in catalogue order (decision D-009).</summary>
public enum RecommendationRule
{
    CfoCompanyCeiling = 1,
    CfoUnbudgetedChannel = 2,
    CtoQualitativeReview = 3,
    CtoControllerAction = 4,
    CtoReverification = 5,
    CooEscalatedDeferral = 6,
    CooAwaitingApproval = 7,
}

/// <summary>
/// The ten fields master plan section 33 requires of a CTO recommendation. A quantity nobody can state is
/// unmeasured, naming why; no field is invented.
/// </summary>
public sealed record CtoRecommendation
{
    public required string CurrentApproach { get; init; }
    public required string Problem { get; init; }
    public required string Alternative { get; init; }
    public required MeasurementQuantity MonthlyCostDifference { get; init; }
    public required MeasurementQuantity CapacityDifference { get; init; }
    public required MeasurementQuantity QualityImpact { get; init; }
    public required MeasurementQuantity ImplementationCost { get; init; }
    public required string OperationalRisk { get; init; }
    public required string Recommendation { get; init; }
    public required string DecisionRequired { get; init; }
}

/// <summary>
/// What one rule produced over one read: a recommendation, or an abstention. CLOSED at two shapes. Both
/// name the rule and the reading; a recommendation names the reading's case and figure and its recorded
/// anchor, and is labelled a rule output; an abstention names the reading it could not state.
/// </summary>
public abstract record RuleOutcome
{
    private RuleOutcome()
    {
    }

    public required RecommendationRule Rule { get; init; }

    /// <summary>The reading the rule rests on.</summary>
    public required string Reading { get; init; }

    /// <summary>The report's one datastore instant.</summary>
    public required DateTimeOffset Instant { get; init; }

    /// <summary>The owner decision the outcome informs; an abstention informs it by naming what cannot be stated.</summary>
    public required string Informs { get; init; }

    /// <summary>A recommendation: a rule output, never a judgement.</summary>
    public sealed record Issued : RuleOutcome
    {
        /// <summary>The reading's figure, in its case.</summary>
        public required LineFigure Figure { get; init; }

        /// <summary>The recorded figure the rule keys on, or null for a rule resting on measurement case alone.</summary>
        public RecordedAmount? Anchor { get; init; }

        /// <summary>What the anchor is, or that the rule rests on measurement case alone.</summary>
        public required string AnchorStatement { get; init; }

        /// <summary>What the rule output says.</summary>
        public required string Statement { get; init; }

        /// <summary>The ten fields, on a CTO rule output only.</summary>
        public CtoRecommendation? Cto { get; init; }
    }

    /// <summary>An abstention: the rule's reading is unmeasured or not recorded, so nothing is issued.</summary>
    public sealed record Abstained : RuleOutcome
    {
        public required string Reason { get; init; }
    }
}

/// <summary>
/// Every report of one read: the five reports' lines, the rule outcomes over them and the register as read,
/// all carrying the one datastore instant, with the period, the stored horizon and the period's finality.
/// </summary>
public sealed record ManagementReportSet
{
    public required DateTimeOffset Instant { get; init; }
    public required DateTimeOffset StoredHorizon { get; init; }
    public required DateTimeOffset PeriodStart { get; init; }
    public required DateTimeOffset PeriodEnd { get; init; }
    public required string Week { get; init; }
    public required bool Final { get; init; }
    public required string FinalityStatement { get; init; }
    public required IReadOnlyList<ReportLine> Lines { get; init; }
    public required IReadOnlyList<RuleOutcome> Outcomes { get; init; }
    public required IReadOnlyList<RegisterEntry> Register { get; init; }
}

/// <summary>The seven sections of the CEO brief, in their order (decision D-011).</summary>
public enum BriefSection
{
    Company = 1,
    ChannelPerformance = 2,
    Production = 3,
    Risk = 4,
    CtoRecommendations = 5,
    CooRecommendations = 6,
    DecisionsRequired = 7,
}

/// <summary>One item of the brief: a report line, a rule outcome, or an open register entry. CLOSED.</summary>
public abstract record BriefItem
{
    private BriefItem()
    {
    }

    public sealed record LineItem(ReportLine Line) : BriefItem;

    public sealed record RuleItem(RuleOutcome Outcome) : BriefItem;

    public sealed record DecisionItem(RegisterEntry Entry) : BriefItem;
}

/// <summary>One section of the brief and its items, in order.</summary>
public sealed record BriefSectionReading
{
    public required BriefSection Section { get; init; }
    public required IReadOnlyList<BriefItem> Items { get; init; }
}

/// <summary>The CEO brief: seven sections in order, from one read, with its header.</summary>
public sealed record CeoBrief
{
    public required DateTimeOffset Instant { get; init; }
    public required DateTimeOffset StoredHorizon { get; init; }
    public required DateTimeOffset PeriodStart { get; init; }
    public required DateTimeOffset PeriodEnd { get; init; }
    public required string Week { get; init; }
    public required bool Final { get; init; }
    public required string FinalityStatement { get; init; }
    public required IReadOnlyList<BriefSectionReading> Sections { get; init; }
}

/// <summary>One dashboard tile: one brief item's own rendering, from the same read, with its instant.</summary>
public sealed record DashboardTile
{
    public required BriefSection Section { get; init; }
    public required string Rendering { get; init; }
    public required DateTimeOffset Instant { get; init; }

    /// <summary>The case of the item's figure, or null for an abstention and a register entry, which carry none.</summary>
    public FigureCase? Case { get; init; }
}

/// <summary>
/// One controller decision read back from the admission decision record, exactly as recorded (decision
/// D-004 of the AI-management design): its operation and channel, its datastore instant and booking month,
/// its action, the selection basis and statement, and every statement it stored, verbatim.
/// </summary>
public sealed record RecordedControllerDecision
{
    public required OperationId Operation { get; init; }
    public required ChannelId Channel { get; init; }
    public required DateTimeOffset DecidedAt { get; init; }
    public required DateOnly BookingMonth { get; init; }
    public required ControllerAction Action { get; init; }
    public required SelectionBasis Basis { get; init; }
    public required string BasisStatement { get; init; }
    public TaskClass? TaskClass { get; init; }
    public required string TierStatement { get; init; }
    public required string CompanyBasisStatement { get; init; }
    public required string ReservationStatement { get; init; }
    public required MeasurementQuantity ObservationsRankedOn { get; init; }
    public required IReadOnlyList<RecordedDecisionReading> Readings { get; init; }
    public required IReadOnlyList<RecordedDecisionCandidate> Candidates { get; init; }
}

/// <summary>One governing reading of a recorded decision, read back in the cases it was written in.</summary>
public sealed record RecordedDecisionReading
{
    public required GoverningScope Scope { get; init; }
    public required RecordedAmount Amount { get; init; }
    public required MeasurementQuantity BookedSpend { get; init; }
    public required MeasurementQuantity Utilisation { get; init; }
    public BudgetThreshold? Threshold { get; init; }
    public required ControllerAction Action { get; init; }
    public RefusalReason? Reason { get; init; }
}

/// <summary>One candidate of a recorded decision, in its recorded position, read back in its cases.</summary>
public sealed record RecordedDecisionCandidate
{
    public required RouteId Route { get; init; }
    public required QualityRating ConfiguredRating { get; init; }
    public required MeasurementQuantity ObservedQuality { get; init; }
    public required MeasurementQuantity ObservedCost { get; init; }
    public required MeasurementQuantity Observations { get; init; }
}
