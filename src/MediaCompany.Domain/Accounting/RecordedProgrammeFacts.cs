namespace MediaCompany.Domain.Accounting;

/// <summary>
/// The figures the management reports and rules are anchored on, each RECORDED somewhere a reader can
/// check and each naming where (the AI-management change, decision D-008 of its design and the owner's
/// decisions of 2026-10-09).
///
/// Every member is a <see cref="RecordedAmount"/>: a recorded amount or a recorded figure, never an
/// observation. Nothing here is a threshold, cadence, budget amount, sizing figure or relation nobody
/// recorded: the owner's ten is the owner's decision; the ceiling, the envelope and its standing line are the
/// owner-approved figures the accounting domain already carries; the controller thresholds are the delivered
/// fixed mapping; and the programme thresholds and dated platform changes are the master plan's, cited by
/// section. They are code constants, identical at every instant, so a report's one read needs nothing from
/// the datastore to state them.
/// </summary>
public static class RecordedProgrammeFacts
{
    /// <summary>Where the owner's ten comparable runs is recorded.</summary>
    public const string ComparableRunsRecordedIn =
        "the owner's decision of 2026-10-09 (CEO-D-701 in research/ceo-decision-record.md), the figure master plan section 16 states";

    /// <summary>Where the decided company ceiling is recorded.</summary>
    public const string CeilingRecordedIn =
        "the owner's decision of 2026-10-09 (CEO-D-702 in research/ceo-decision-record.md): the metered line of the owner-approved monthly envelope, the owner's decided company ceiling";

    /// <summary>Where the envelope is recorded.</summary>
    public const string EnvelopeRecordedIn = "the owner-approved monthly envelope, a code constant of the accounting domain";

    /// <summary>Where the standing charge is recorded, and what is not.</summary>
    public const string StandingRecordedIn =
        "the standing line of the owner-approved monthly envelope, a code constant; no record of a standing commitment exists, so it has no recorded home";

    /// <summary>Where the controller thresholds are recorded.</summary>
    public const string ControllerThresholdsRecordedIn =
        "the cost controller's fixed threshold mapping, code (50 alert only, 75 downgrade, 90 defer, 100 refuse)";

    /// <summary>Where the programme thresholds and dated changes are recorded.</summary>
    public const string ProgrammeRecordedIn = "MASTER-PLAN.md sections 2 and 5, recorded from the platform policy dossier verified 2026-09-18";

    /// <summary>Where the re-verification wording is recorded.</summary>
    public const string ReverificationRecordedIn = "MASTER-PLAN.md section 3.3 and section 33";

    /// <summary>
    /// The owner's ten, as the integer the selection and the CTO label compare a count against. The one
    /// definition of the figure; <see cref="ComparableRunsPerTask"/> is its recorded rendering.
    /// </summary>
    public const int ComparableRunsThreshold = 10;

    /// <summary>The owner's ten comparable runs per task, recorded.</summary>
    public static RecordedAmount ComparableRunsPerTask { get; } =
        RecordedAmount.Figure(ComparableRunsThreshold, "comparable runs per task", ComparableRunsRecordedIn);

    /// <summary>The owner's decided company ceiling: the USD 34.42 metered allotment.</summary>
    public static RecordedAmount CompanyCeiling { get; } = RecordedAmount.Of(ApprovedEnvelope.Metered, CeilingRecordedIn);

    /// <summary>The owner-approved monthly envelope of USD 77.41.</summary>
    public static RecordedAmount Envelope { get; } = RecordedAmount.Of(ApprovedEnvelope.MonthlyTotal, EnvelopeRecordedIn);

    /// <summary>The standing charge of USD 42.99, separate from metered spend and never netted against it.</summary>
    public static RecordedAmount StandingCharge { get; } = RecordedAmount.Of(ApprovedEnvelope.Standing, StandingRecordedIn);

    /// <summary>One of the controller's four fixed thresholds, recorded in percent.</summary>
    public static RecordedAmount ControllerThreshold(int percent) =>
        percent is 50 or 75 or 90 or 100
            ? RecordedAmount.Figure(percent, "percent of the governing amount", ControllerThresholdsRecordedIn)
            : throw new ArgumentOutOfRangeException(nameof(percent), percent, "The threshold set is closed at 50, 75, 90 and 100.");

    /// <summary>The partner programme's subscriber entry threshold.</summary>
    public static RecordedAmount Subscribers { get; } = RecordedAmount.Figure(1_000m, "subscribers", ProgrammeRecordedIn);

    /// <summary>The long-form watch-hour entry threshold before the threshold change.</summary>
    public static RecordedAmount WatchHoursBeforeChange { get; } =
        RecordedAmount.Figure(4_000m, "qualified watch hours in 12 months, before 2027-02-01", ProgrammeRecordedIn);

    /// <summary>The short-form view entry threshold before the threshold change.</summary>
    public static RecordedAmount ShortViewsBeforeChange { get; } =
        RecordedAmount.Figure(10_000_000m, "qualified short-form views in 90 days, before 2027-02-01", ProgrammeRecordedIn);

    /// <summary>The long-form watch-hour entry threshold for a new entrant from the threshold change.</summary>
    public static RecordedAmount WatchHoursFromChange { get; } =
        RecordedAmount.Figure(8_000m, "qualified watch hours in 365 days, for a new entrant from 2027-02-01", ProgrammeRecordedIn);

    /// <summary>The short-form view entry threshold for a new entrant from the threshold change.</summary>
    public static RecordedAmount ShortViewsFromChange { get; } =
        RecordedAmount.Figure(20_000_000m, "qualified short-form views in 90 days, for a new entrant from 2027-02-01", ProgrammeRecordedIn);

    /// <summary>The date the entry thresholds double for a new entrant.</summary>
    public static DateOnly ThresholdChangeDate { get; } = new(2027, 2, 1);

    /// <summary>The dated platform changes the master plan records, in date order.</summary>
    public static IReadOnlyList<RecordedAmount> DatedChanges { get; } =
    [
        RecordedAmount.OnDate(new DateOnly(2026, 9, 24), "the platform guidelines change", ProgrammeRecordedIn),
        RecordedAmount.OnDate(new DateOnly(2027, 1, 31), "the deadline for accepting the updated programme terms", ProgrammeRecordedIn),
        RecordedAmount.OnDate(ThresholdChangeDate, "the doubling of the entry thresholds for a new entrant", ProgrammeRecordedIn),
    ];

    /// <summary>
    /// The date the master plan's monthly re-verification wording runs until. The wording is a recommendation,
    /// not an owner-confirmed cadence, so nothing infers an overdue statement from it.
    /// </summary>
    public static RecordedAmount ReverificationUntil { get; } =
        RecordedAmount.OnDate(ThresholdChangeDate, "the end of the platform-policy re-verification obligation", ReverificationRecordedIn);
}
