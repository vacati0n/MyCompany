using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;

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

    /// <summary>Whether a count reaches the owner's ten. An unmeasured count never does.</summary>
    public static bool ReachesOwnersTen(MeasurementQuantity count) =>
        count is MeasurementQuantity.ObservedValue observed
        && observed.Amount >= RecordedProgrammeFacts.ComparableRunsThreshold;

    /// <summary>A count stated against the owner's ten, as every selection and every CTO line states it.</summary>
    public static string AgainstOwnersTen(MeasurementQuantity count) =>
        $"{count.Describe()} against the owner's {RecordedProgrammeFacts.ComparableRunsPerTask.Describe()}";
}
