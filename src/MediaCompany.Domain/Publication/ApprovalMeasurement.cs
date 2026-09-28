namespace MediaCompany.Domain.Publication;

/// <summary>
/// How an approval measurement resolved (module M-027, decision D-006).
///
/// <see cref="Unmeasured"/> is the resolution for an approval missing any one of the three
/// quantities. It is NOT zero. An approval whose queue mark was never taken has an UNKNOWN queue
/// time, and recording that as zero would understate the cost of per-publication approval and
/// argue for relaxing it too early — which is the one use the recorded set must not support.
/// </summary>
public enum ApprovalMeasurementResolution
{
    /// <summary>All three quantities are derivable. The only resolution that yields figures.</summary>
    Measured = 1,

    /// <summary>At least one quantity is not derivable. The figures are absent, not zero.</summary>
    Unmeasured = 2,
}

/// <summary>
/// The mandatory label naming what a recorded approval set is.
///
/// The label is carried ON THE RECORD rather than added at presentation. A figure that travels
/// without its class limits eventually gets read as the real approval series, and by then the
/// caveat is in a document nobody opened. Carrying it here makes the limits inseparable from the
/// number.
/// </summary>
public sealed record ApprovalExerciseLabel
{
    public ApprovalExerciseLabel(string exerciseName, int setSize, string classLimits)
    {
        if (string.IsNullOrWhiteSpace(exerciseName))
        {
            throw new ArgumentException("An exercise label names the exercise.", nameof(exerciseName));
        }

        if (string.IsNullOrWhiteSpace(classLimits))
        {
            throw new ArgumentException(
                "An exercise label states its class limits; a figure without them is read as the real series.",
                nameof(classLimits));
        }

        if (setSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(setSize), "A set size is not negative.");
        }

        ExerciseName = exerciseName;
        SetSize = setSize;
        ClassLimits = classLimits;
    }

    public string ExerciseName { get; }

    /// <summary>The size of the recorded set, carried so a set of one cannot be presented as a series.</summary>
    public int SetSize { get; }

    /// <summary>What this set does and does not establish.</summary>
    public string ClassLimits { get; }
}

/// <summary>
/// How one approval-effort component resolved at the derivation site. A component is either
/// derived from its own recorded marks, or it is not, and then the result says which mark is
/// absent or which marks are inconsistent. Each component resolves from its own marks alone, so
/// a missing mark makes only the component derived from it undeterminable.
/// </summary>
public enum ApprovalComponentState
{
    /// <summary>The interval was derived from the component's own recorded marks.</summary>
    Derived = 1,

    /// <summary>A mark the component is derived from was not recorded.</summary>
    MarkAbsent = 2,

    /// <summary>The marks were recorded and are inconsistent with one another, so no interval exists.</summary>
    MarksInconsistent = 3,
}

/// <summary>
/// One component's result: a derived interval, or a statement naming the mark that is absent or
/// the marks that are inconsistent. The interval is present exactly when the component was
/// derived; a component that was not derived carries no interval to be read as zero.
/// </summary>
public sealed record ApprovalComponentResult
{
    private ApprovalComponentResult(ApprovalComponentState state, TimeSpan? interval, string statement)
    {
        State = state;
        Interval = interval;
        Statement = statement;
    }

    public ApprovalComponentState State { get; }

    /// <summary>The derived interval. Null unless <see cref="State"/> is derived.</summary>
    public TimeSpan? Interval { get; }

    /// <summary>The mark that is absent or the marks that are inconsistent. Empty when derived.</summary>
    public string Statement { get; }

    internal static ApprovalComponentResult Derived(TimeSpan interval) =>
        interval < TimeSpan.Zero
            ? throw new ArgumentOutOfRangeException(nameof(interval), "A derived interval is not negative.")
            : new ApprovalComponentResult(ApprovalComponentState.Derived, interval, string.Empty);

    internal static ApprovalComponentResult Absent(string statement) =>
        new(ApprovalComponentState.MarkAbsent, null, statement);

    internal static ApprovalComponentResult Inconsistent(string statement) =>
        new(ApprovalComponentState.MarksInconsistent, null, statement);
}

/// <summary>
/// One approval's three separated quantities (module M-027, constraint C-012 of the design).
///
/// Review, queue and rework are three DERIVED quantities over recorded marks, never entered. The
/// derivation is the whole reason the marks exist: a single elapsed figure silently includes queue
/// time, and a threshold derived from it would relax per-publication approval on the wrong number.
///
/// Two readings of the same marks are carried, both computed HERE, at the one derivation site. The
/// whole-row members keep their delivered meaning exactly — a row missing any one quantity
/// resolves unmeasured and carries no interval at all — so the approval surface exercise and the
/// datastore view read unchanged. The three component results resolve each quantity from its own
/// marks alone, so a missing queued mark makes the queue component undeterminable and leaves the
/// review and rework components as derived.
/// </summary>
public sealed record ApprovalMeasurement
{
    private ApprovalMeasurement(
        ItemId item,
        ItemVersion version,
        ApprovalMeasurementResolution resolution,
        TimeSpan? reviewTime,
        TimeSpan? queueTime,
        TimeSpan? reworkTime,
        string unmeasuredReason,
        ApprovalExerciseLabel label,
        ApprovalComponentResult reviewComponent,
        ApprovalComponentResult queueComponent,
        ApprovalComponentResult reworkComponent)
    {
        Item = item;
        Version = version;
        Resolution = resolution;
        ReviewTime = reviewTime;
        QueueTime = queueTime;
        ReworkTime = reworkTime;
        UnmeasuredReason = unmeasuredReason;
        Label = label;
        ReviewComponent = reviewComponent;
        QueueComponent = queueComponent;
        ReworkComponent = reworkComponent;
    }

    public ItemId Item { get; }
    public ItemVersion Version { get; }
    public ApprovalMeasurementResolution Resolution { get; }

    /// <summary>The interval the owner spent reviewing. Null when unmeasured.</summary>
    public TimeSpan? ReviewTime { get; }

    /// <summary>
    /// The interval the item waited before review began. Null when unmeasured. A genuine zero
    /// wait is recorded as zero, which is a different value from null and means a different thing.
    /// </summary>
    public TimeSpan? QueueTime { get; }

    /// <summary>The interval spent on rework between a send-back and the re-presentation. Null when unmeasured.</summary>
    public TimeSpan? ReworkTime { get; }

    /// <summary>Which quantity was missing, when the resolution is unmeasured. Empty otherwise.</summary>
    public string UnmeasuredReason { get; }

    /// <summary>The mandatory exercise label. Every measurement carries one.</summary>
    public ApprovalExerciseLabel Label { get; }

    /// <summary>
    /// Review, from the presentation and decision instants alone. Both marks are mandatory on the
    /// approval type and in the table, so this component is derived on every approval that exists:
    /// a review mark missing is a construction the type refuses, not a component that resolves.
    /// </summary>
    public ApprovalComponentResult ReviewComponent { get; }

    /// <summary>Queue, from the queued mark and the presentation alone.</summary>
    public ApprovalComponentResult QueueComponent { get; }

    /// <summary>Rework, from the rework link, the predecessor's decision and the presentation alone.</summary>
    public ApprovalComponentResult ReworkComponent { get; }

    /// <summary>
    /// Derives the three quantities from an approval's recorded marks.
    ///
    /// The approval must carry the queued mark for queue time to be derivable, and the rework link
    /// resolves to the prior approval whose decision began the rework interval. A missing mark
    /// yields UNMEASURED naming what was missing, never a zero standing in for an unknown.
    /// </summary>
    public static ApprovalMeasurement Derive(
        Approval approval,
        Approval? reworkPredecessor,
        ApprovalExerciseLabel label)
    {
        ArgumentNullException.ThrowIfNull(approval);
        ArgumentNullException.ThrowIfNull(label);

        // Review: both marks are mandatory and the approval type refuses a decision earlier than
        // its presentation, so the interval is always derivable and never negative.
        var review = ApprovalComponentResult.Derived(approval.DecidedAt - approval.PresentedAt);

        ApprovalComponentResult queue;
        if (approval.QueuedAt is null)
        {
            queue = ApprovalComponentResult.Absent("queue time (no queued mark was recorded)");
        }
        else if (approval.QueuedAt > approval.PresentedAt)
        {
            // Unmeasured, not a clamped zero, and for the same reason the rework case is: an
            // interval whose marks are inverted was not measured, and a zero standing in for it
            // would understate the cost of per-publication approval exactly as a substituted zero
            // queue time would. The constructor and a datastore check both bar this today, so the
            // branch is unreachable by any supported route; it is here because the rule is that a
            // component that was not measured resolves as unmeasured, without exception.
            queue = ApprovalComponentResult.Inconsistent("queue time (the queued mark is later than the presentation)");
        }
        else
        {
            queue = ApprovalComponentResult.Derived(approval.PresentedAt - approval.QueuedAt.Value);
        }

        // Rework time is derivable when the approval declares a predecessor and that predecessor
        // was supplied. A first-pass approval has no rework, which is a measured zero rather than
        // a missing quantity: nothing was reworked, and that is an observation, not an absence.
        ApprovalComponentResult rework;
        if (approval.ReworkOf is null)
        {
            rework = ApprovalComponentResult.Derived(TimeSpan.Zero);
        }
        else if (reworkPredecessor is null)
        {
            rework = ApprovalComponentResult.Absent("rework time (the approval names a predecessor that was not supplied)");
        }
        else if (approval.PresentedAt - reworkPredecessor.DecidedAt < TimeSpan.Zero)
        {
            rework = ApprovalComponentResult.Inconsistent(
                "rework time (the predecessor was decided after this approval was presented)");
        }
        else
        {
            rework = ApprovalComponentResult.Derived(approval.PresentedAt - reworkPredecessor.DecidedAt);
        }

        // The whole-row resolution, from the SAME component results, so the two readings of one
        // approval cannot be computed by different rules. Its meaning is the delivered one: any
        // component not derived makes the row unmeasured and nulls all three intervals, and the
        // missing-quantity statement is the delivered text, in the delivered order.
        var missing = new[] { queue, rework }
            .Where(c => c.State != ApprovalComponentState.Derived)
            .Select(c => c.Statement)
            .ToArray();

        if (missing.Length > 0)
        {
            return new ApprovalMeasurement(
                approval.Item,
                approval.ItemVersion,
                ApprovalMeasurementResolution.Unmeasured,
                null,
                null,
                null,
                string.Join("; ", missing),
                label,
                review,
                queue,
                rework);
        }

        return new ApprovalMeasurement(
            approval.Item,
            approval.ItemVersion,
            ApprovalMeasurementResolution.Measured,
            review.Interval,
            queue.Interval,
            rework.Interval,
            string.Empty,
            label,
            review,
            queue,
            rework);
    }
}

/// <summary>
/// How a set of approval measurements totals. A set containing ANY unmeasured member reports the
/// unmeasured count rather than averaging over the measured remainder, because an average over the
/// subset that happened to be complete is the same overstatement the separation exists to prevent.
/// </summary>
public sealed record ApprovalSurfaceExercise
{
    public ApprovalSurfaceExercise(IReadOnlyList<ApprovalMeasurement> measurements, ApprovalExerciseLabel label)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        ArgumentNullException.ThrowIfNull(label);

        Measurements = measurements;
        Label = label;
    }

    public IReadOnlyList<ApprovalMeasurement> Measurements { get; }
    public ApprovalExerciseLabel Label { get; }

    public int MeasuredCount =>
        Measurements.Count(m => m.Resolution == ApprovalMeasurementResolution.Measured);

    public int UnmeasuredCount =>
        Measurements.Count(m => m.Resolution == ApprovalMeasurementResolution.Unmeasured);

    /// <summary>The three totals across the measured members, or null when none is measured.</summary>
    public (TimeSpan Review, TimeSpan Queue, TimeSpan Rework)? Totals()
    {
        var measured = Measurements
            .Where(m => m.Resolution == ApprovalMeasurementResolution.Measured)
            .ToArray();

        if (measured.Length == 0)
        {
            return null;
        }

        return (
            measured.Aggregate(TimeSpan.Zero, (a, m) => a + m.ReviewTime!.Value),
            measured.Aggregate(TimeSpan.Zero, (a, m) => a + m.QueueTime!.Value),
            measured.Aggregate(TimeSpan.Zero, (a, m) => a + m.ReworkTime!.Value));
    }
}
