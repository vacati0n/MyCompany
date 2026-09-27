using MediaCompany.Domain.Publication;

namespace MediaCompany.Deterministic.Publication;

/// <summary>
/// The publication timing calculator (module M-026), a use of the delivered
/// <see cref="DeterministicTaskRegistry.ScheduleArithmetic"/> member rather than a second
/// implementation of it.
///
/// A planned time is COMPUTED AND HELD and causes nothing. There is no trigger contract in this
/// file and no due-time reader anywhere in the build: nothing selects rows by planned time, so an
/// elapsed planned time has nothing to fire. Dispatch is initiated only by an explicit recorded
/// command, and a held time is safe precisely because nothing reads it.
///
/// This is why the calculator exists before anything holds a time rather than after: adding it
/// later would leave an interval in which a trigger could be introduced unnoticed.
/// </summary>
public static class PublicationTiming
{
    /// <summary>The rule-determined step this type realizes, discovered by the build-time check.</summary>
    public static readonly IReadOnlyList<string> TaskNames =
    [
        DeterministicTaskRegistry.PublicationTimingArithmetic,
    ];

    /// <summary>
    /// The timing policy as a VALUE, so re-pointing the cadence is a configuration change and not
    /// a rebuild. It commits no publication date and no publication pattern: it describes how a
    /// candidate instant is computed, not when anything will happen.
    /// </summary>
    public sealed record TimingPolicy
    {
        public required int LeadDays { get; init; }
        public required TimeOnly PreferredTimeOfDay { get; init; }

        /// <summary>The days of the week a candidate may fall on. Empty means any day.</summary>
        public IReadOnlyList<DayOfWeek> PermittedDays { get; init; } = [];
    }

    /// <summary>The computed time with the arithmetic that produced it, so the result is re-derivable.</summary>
    public sealed record PlannedPublication
    {
        public required ItemId Item { get; init; }
        public required ItemVersion Version { get; init; }
        public required DateTimeOffset PlannedAt { get; init; }
        public required string Computation { get; init; }
    }

    /// <summary>
    /// Computes a candidate publication instant. Pure arithmetic over the supplied instant and the
    /// supplied policy: no clock is read inside, so the same inputs always give the same answer.
    /// </summary>
    public static PlannedPublication Compute(
        ItemId item,
        ItemVersion version,
        DateTimeOffset from,
        TimingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (policy.LeadDays < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy), policy.LeadDays, "A lead time is not negative.");
        }

        var candidate = new DateTimeOffset(
            from.UtcDateTime.Date.AddDays(policy.LeadDays),
            TimeSpan.Zero)
            .Add(policy.PreferredTimeOfDay.ToTimeSpan());

        var shifted = 0;
        if (policy.PermittedDays.Count > 0)
        {
            // Advance to the next permitted day. Bounded by seven, so the loop terminates whatever
            // the permitted set contains, and a set that permits nothing is refused rather than
            // spun on.
            while (shifted < 7 && !policy.PermittedDays.Contains(candidate.UtcDateTime.DayOfWeek))
            {
                candidate = candidate.AddDays(1);
                shifted++;
            }

            if (!policy.PermittedDays.Contains(candidate.UtcDateTime.DayOfWeek))
            {
                throw new ArgumentException(
                    "The timing policy permits no day of the week, so no candidate instant exists.",
                    nameof(policy));
            }
        }

        return new PlannedPublication
        {
            Item = item,
            Version = version,
            PlannedAt = candidate,
            Computation =
                $"from {from:O} plus {policy.LeadDays} lead day(s) at {policy.PreferredTimeOfDay:HH:mm} UTC"
                + (shifted > 0 ? $", advanced {shifted} day(s) to a permitted day" : string.Empty)
                + "; held as an attribute of the descriptor and read by nothing",
        };
    }
}
