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
/// One approval's three separated quantities (module M-027, constraint C-012 of the design).
///
/// Review, queue and rework are three DERIVED quantities over recorded marks, never entered. The
/// derivation is the whole reason the marks exist: a single elapsed figure silently includes queue
/// time, and a threshold derived from it would relax per-publication approval on the wrong number.
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
        ApprovalExerciseLabel label)
    {
        Item = item;
        Version = version;
        Resolution = resolution;
        ReviewTime = reviewTime;
        QueueTime = queueTime;
        ReworkTime = reworkTime;
        UnmeasuredReason = unmeasuredReason;
        Label = label;
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

        var missing = new List<string>();

        if (approval.QueuedAt is null)
        {
            missing.Add("queue time (no queued mark was recorded)");
        }

        // Rework time is derivable when the approval declares a predecessor and that predecessor
        // was supplied. A first-pass approval has no rework, which is a measured zero rather than
        // a missing quantity: nothing was reworked, and that is an observation, not an absence.
        TimeSpan? rework = null;
        if (approval.ReworkOf is null)
        {
            rework = TimeSpan.Zero;
        }
        else if (reworkPredecessor is null)
        {
            missing.Add("rework time (the approval names a predecessor that was not supplied)");
        }
        else
        {
            rework = approval.PresentedAt - reworkPredecessor.DecidedAt;
            if (rework < TimeSpan.Zero)
            {
                missing.Add("rework time (the predecessor was decided after this approval was presented)");
                rework = null;
            }
        }

        if (missing.Count > 0)
        {
            return new ApprovalMeasurement(
                approval.Item,
                approval.ItemVersion,
                ApprovalMeasurementResolution.Unmeasured,
                null,
                null,
                null,
                string.Join("; ", missing),
                label);
        }

        var queue = approval.PresentedAt - approval.QueuedAt!.Value;
        var review = approval.DecidedAt - approval.PresentedAt;

        return new ApprovalMeasurement(
            approval.Item,
            approval.ItemVersion,
            ApprovalMeasurementResolution.Measured,
            review,
            queue < TimeSpan.Zero ? TimeSpan.Zero : queue,
            rework,
            string.Empty,
            label);
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
