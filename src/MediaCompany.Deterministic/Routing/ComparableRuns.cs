using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;

namespace MediaCompany.Deterministic.Routing;

/// <summary>
/// The ONE definition of a comparable run and of a task's count of them (the AI-management change, decision
/// D-005 of its design), a member of the zero-AI-cost set under the name
/// <see cref="DeterministicTaskRegistry.ComparableRunCount"/>.
///
/// One comparable run of a task class on a route is one benchmark observation of that task class on that
/// route whose quality is OBSERVED — an observed zero included — in the floor's rating unit, and whose
/// referenced operation's cost is STATED, an observed zero included. An unmeasured quality or an unstated
/// cost is not a comparable run. A capability class and task class pair counts the LEAST such count over the
/// admitted routes of the capability class; a pair whose routes hold none counts an observed zero.
///
/// The same pure count keys the evidence selection's owner's ten and the CTO report's qualitative-review
/// label, so the report and the router cannot disagree on a task's standing. The ten is the owner's recorded
/// figure, never an observation.
///
/// ONE COUNT, ONE FUNCTION, ONE CANDIDATE SET (the AI-management change, correction cycle). The count is
/// <see cref="OfCandidates"/>: the least count over the COUNTED CANDIDATES of one reasoning-tier class — the
/// provider routes of the class that are available (<see cref="IsAvailable"/>, the resolution function's own
/// availability step, which delegates here). A non-AI substitute or a hold-and-escalate route can hold no
/// observation and is never counted; a disabled or non-serving route never reaches selection and is never
/// counted; a route of another tier class is counted in its own class. Selection counts the class it actually
/// compares; the CTO report, which knows no request, counts every class.
/// </summary>
public static class ComparableRuns
{
    public const string TaskName = DeterministicTaskRegistry.ComparableRunCount;

    /// <summary>The unit a count of comparable runs is stated in.</summary>
    public const string Unit = "comparable runs";

    /// <summary>Whether one observation is a comparable run.</summary>
    public static bool IsComparable(BenchmarkObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        var qualityObserved = observation.Quality switch
        {
            MeasurementQuantity.ObservedValue value => value.Unit == BenchmarkObservation.QualityRatingUnit,
            MeasurementQuantity.ObservedZero zero => zero.Unit == BenchmarkObservation.QualityRatingUnit,
            _ => false,
        };

        return qualityObserved && observation.Cost is MeasurementQuantity.ObservedValue or MeasurementQuantity.ObservedZero;
    }

    /// <summary>One route's count of comparable runs of one task class, an observed count, zero included.</summary>
    public static MeasurementQuantity Of(RouteId route, TaskClass taskClass, IReadOnlyList<BenchmarkObservation> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return MeasurementQuantity.Count(
            evidence.LongCount(o => o.Route.Equals(route) && o.TaskClass == taskClass && IsComparable(o)), Unit);
    }

    /// <summary>
    /// The count of a pair: the least count over the given routes, the admitted routes of one capability class.
    /// With no route there is nothing to run the task on, and the count is an observed zero.
    /// </summary>
    public static MeasurementQuantity LeastOver(
        IReadOnlyList<RouteId> routes,
        TaskClass taskClass,
        IReadOnlyList<BenchmarkObservation> evidence)
    {
        ArgumentNullException.ThrowIfNull(routes);
        ArgumentNullException.ThrowIfNull(evidence);

        var least = routes.Count == 0
            ? 0L
            : routes.Min(route => evidence.LongCount(o => o.Route.Equals(route) && o.TaskClass == taskClass && IsComparable(o)));
        return MeasurementQuantity.Count(least, Unit);
    }

    /// <summary>
    /// The resolution function's availability step, defined once: a provider route only where its account is
    /// active, and any route only where its recorded state is serving; an unrecorded route is not available.
    /// </summary>
    public static bool IsAvailable(
        Route route,
        IReadOnlyDictionary<RouteId, RouteAvailability> availability,
        IReadOnlyDictionary<ProviderAccountId, ProviderAccountStatus> accounts)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(availability);
        ArgumentNullException.ThrowIfNull(accounts);

        if (route.Target is RouteTarget.ProviderRoute provider
            && (!accounts.TryGetValue(provider.ProviderAccount, out var status) || status != ProviderAccountStatus.Active))
        {
            return false;
        }

        return availability.TryGetValue(route.Id, out var state) && state.State == AvailabilityState.Serving;
    }

    /// <summary>The counted candidates of a candidate set: its provider routes, the only routes that can hold an observation.</summary>
    public static IReadOnlyList<RouteId> CountedCandidates(IEnumerable<Route> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return candidates.Where(r => r.Target is RouteTarget.ProviderRoute).Select(r => r.Id).ToArray();
    }

    /// <summary>
    /// THE count: the least count of comparable runs over the counted candidates of one candidate set. A set with
    /// no provider route counts an observed zero, so nothing is ranked on evidence there.
    /// </summary>
    public static MeasurementQuantity OfCandidates(IEnumerable<Route> candidates, TaskClass taskClass, IReadOnlyList<BenchmarkObservation> evidence) =>
        LeastOver(CountedCandidates(candidates), taskClass, evidence);

    /// <summary>
    /// The reasoning-tier classes of a capability as the report counts them: its available routes, grouped by
    /// the reasoning tier each states (none being its own class), in tier order with the untiered class last.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<Route>> TierClasses(
        IEnumerable<Route> routes,
        CapabilityClass capability,
        IReadOnlyDictionary<RouteId, RouteAvailability> availability,
        IReadOnlyDictionary<ProviderAccountId, ProviderAccountStatus> accounts) =>
        routes
            .Where(r => r.Capability == capability && IsAvailable(r, availability, accounts))
            .GroupBy(r => r.StatedReasoningTier)
            .OrderBy(g => g.Key is null ? int.MaxValue : (int)g.Key.Value)
            .Select(g => (IReadOnlyList<Route>)g.OrderBy(r => (int)r.Tier).ThenBy(r => r.Id.Value).ToArray())
            .ToArray();

    /// <summary>Whether a count reaches the owner's ten. An unmeasured count never does.</summary>
    public static bool ReachesOwnersTen(MeasurementQuantity count) =>
        count is MeasurementQuantity.ObservedValue observed
        && observed.Amount >= RecordedProgrammeFacts.ComparableRunsThreshold;

    /// <summary>A count stated against the owner's ten, as every selection and every CTO line states it.</summary>
    public static string AgainstOwnersTen(MeasurementQuantity count) =>
        $"{count.Describe()} against the owner's {RecordedProgrammeFacts.ComparableRunsPerTask.Describe()}";
}
