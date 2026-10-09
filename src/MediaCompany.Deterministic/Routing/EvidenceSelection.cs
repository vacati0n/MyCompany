using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Deterministic.Routing;

/// <summary>
/// What one selection chose and what it rested on. <see cref="Selected"/> is null where the evidence
/// basis applied and no candidate met the floor on observation, which the resolution function holds
/// with the floor reason rather than serving anything below the floor.
/// </summary>
public sealed record SelectionOutcome(Route? Selected, SelectionRecord Record);

/// <summary>
/// Evidence-ready selection (the AI-economics change, decision D-003 of its design), a member of the
/// zero-AI-cost set under the name <see cref="DeterministicTaskRegistry.EvidenceSelection"/>.
///
/// A PURE RANKING over candidates the delivered resolution steps already admitted, within one tier
/// class, and over an evidence table read in one statement. It REFUSES TO RANK ON AN UNMEASURED
/// QUANTITY: the evidence basis applies only where every candidate has at least one observation for the
/// request's task class, none of a candidate's observations is unmeasured in quality or cost, every
/// quality is in the floor's rating unit and every cost is in the request ceiling's currency. Each
/// candidate's figures are then its MINIMUM observed quality and its MAXIMUM observed cost; an observed
/// zero counts as observed and ranks as zero. The selection is the cheapest candidate meeting the floor,
/// ties broken by route tier then identifier, and where no candidate meets the floor on observation
/// nothing is selected. Otherwise the selection is the delivered route-tier ordering EXACTLY, labelled
/// configured, naming each unmeasured quantity, unit mismatch or absent task class.
///
/// THE OWNER'S TEN (the AI-management change, decision D-005 of its design, on the owner's decision of
/// 2026-10-09): the evidence basis applies only where EVERY candidate also holds at least ten comparable runs
/// of the task class, counted by <see cref="ComparableRuns"/>, the one count the CTO report's label keys on.
/// Below ten for any candidate the selection is the configured ordering, labelled, and every basis states
/// every candidate's count against the ten. Latency is read and recorded but not ranked. A configured rating is never presented as an observed quality: the
/// two are separate fields. The route register is never written.
/// </summary>
public static class EvidenceSelection
{
    public const string TaskName = DeterministicTaskRegistry.EvidenceSelection;

    /// <summary>
    /// Selects among <paramref name="candidates"/>, the routes of the best eligible tier class.
    /// </summary>
    public static SelectionOutcome Select(
        IReadOnlyList<Route> candidates,
        TaskClass? taskClass,
        QualityRating floor,
        string currency,
        IReadOnlyList<BenchmarkObservation> evidence)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        var configured = candidates.OrderBy(r => (int)r.Tier).ThenBy(r => r.Id.Value).ToArray();
        if (configured.Length == 0)
        {
            throw new ArgumentException("A selection is made among at least one candidate.", nameof(candidates));
        }

        var figures = configured
            .Select(route => Figures(route, taskClass, currency, evidence))
            .ToArray();

        // THE COUNTED CANDIDATES (the AI-management change, correction cycle): the provider routes of the class,
        // the only routes that can hold an observation. A non-AI substitute or a hold-and-escalate candidate is
        // excluded from the evidence comparison and from the count, exactly as the CTO report excludes it, and
        // keeps its configured position after the ranked providers.
        var counted = ComparableRuns.CountedCandidates(configured).ToHashSet();
        var compared = figures.Where(f => counted.Contains(f.Route.Id)).ToArray();
        var excluded = figures.Where(f => !counted.Contains(f.Route.Id)).ToArray();
        var gaps = compared.SelectMany(f => f.Gaps).ToArray();

        // Every counted candidate's count of comparable runs against the owner's ten, stated in every basis, and
        // THE count of the class, the one function the CTO report uses.
        var counts = taskClass is { } named
            ? compared.Select(f => (Route: f.Route.Id, Count: ComparableRuns.Of(f.Route.Id, named, evidence))).ToArray()
            : [];
        var classCount = taskClass is { } task ? ComparableRuns.OfCandidates(configured, task, evidence) : null;
        var belowTen = classCount is not null && !ComparableRuns.ReachesOwnersTen(classCount);
        var countStatement = classCount is null
            ? "no comparable run is counted, because no task class is named"
            : $"comparable runs of task class {taskClass}, least over the {compared.Length} provider candidates: "
              + ComparableRuns.AgainstOwnersTen(classCount)
              + (counts.Length == 0 ? string.Empty : " (" + string.Join(
                  "; ", counts.Select(c => $"route {c.Route} {ComparableRuns.AgainstOwnersTen(c.Count)}")) + ")")
              + (excluded.Length == 0 ? string.Empty : $"; {excluded.Length} candidates reaching no provider are not counted");

        if (taskClass is null || gaps.Length > 0 || belowTen)
        {
            var reasons = new List<string>();
            if (taskClass is null)
            {
                reasons.Add("the request names no task class, so there is nothing to compare evidence by");
            }

            reasons.AddRange(gaps);

            if (belowTen)
            {
                reasons.Add(
                    compared.Length == 0
                        ? "no candidate reaches a provider, so there is no evidence to rank on"
                        : "the candidates hold fewer comparable runs than the owner's ten, so no evidence ranking replaces "
                          + "the configured ordering");
            }

            var why = string.Join("; ", reasons);

            return new SelectionOutcome(
                configured[0],
                new SelectionRecord
                {
                    Basis = SelectionBasis.Configured,
                    TaskClass = taskClass,
                    Candidates = figures.Select(f => f.Candidate).ToArray(),
                    ObservationsRankedOn = [],
                    ObservationCount = MeasurementQuantity.Count(0, "observations ranked on"),
                    Statement = "the configured ordering, taken exactly: route tier order, ties by route identifier; "
                        + $"evidence was not ranked on, because {why}; {countStatement}; a configured route rating is a "
                        + "rating and is never presented as an observed quality",
                });
        }

        // Every compared quantity of every counted candidate is observed in one unit: rank on evidence. EVIDENCE
        // REORDERS THE PROVIDERS ONLY AMONG THE POSITIONS PROVIDERS ALREADY HOLD in the configured ordering (the
        // tech lead's ruling on the second correction cycle): a candidate reaching no provider keeps its exact
        // configured position, is never moved after the ranked providers and is never skipped for a metered one.
        var meeting = compared.Where(f => f.MinimumQuality >= floor.Value).ToArray();
        var providerOrder = new Queue<CandidateFigures>(meeting
            .OrderBy(f => f.MaximumCost)
            .ThenBy(f => (int)f.Route.Tier)
            .ThenBy(f => f.Route.Id.Value)
            .Concat(compared.Where(f => f.MinimumQuality < floor.Value)));
        var ranked = figures
            .Select(f => counted.Contains(f.Route.Id) ? providerOrder.Dequeue() : f)
            .ToArray();

        // The first position that may be served: a candidate reaching no provider, which met the floor at the
        // resolution's floor step, or a provider meeting the floor on observation; with none, nothing is served
        // below the floor.
        var served = ranked.FirstOrDefault(f => !counted.Contains(f.Route.Id) || f.MinimumQuality >= floor.Value);

        var rankedOn = compared.SelectMany(f => f.Observations).Select(o => o.Id).ToArray();

        var statement = meeting.Length == 0
            ? $"ranked on evidence: {rankedOn.Length} observations of task class {taskClass} across {compared.Length} "
              + $"candidates; no candidate's minimum observed quality meets the floor of {floor.Value} on the "
              + $"{BenchmarkObservation.QualityRatingUnit}, so {(excluded.Length > 0 ? "the first candidate reaching no provider is taken in its configured position" : "none is selected")} and nothing is served below the floor; "
              + $"every candidate holds at least the owner's ten comparable runs: {countStatement}"
            : $"ranked on evidence: {rankedOn.Length} observations of task class {taskClass} across {compared.Length} "
              + $"candidates, each candidate's minimum observed quality against the floor of {floor.Value} on the "
              + $"{BenchmarkObservation.QualityRatingUnit} and its maximum observed cost in {currency}; the cheapest "
              + "candidate meeting the floor fills the first provider position, ties by route tier then identifier, and "
              + "every candidate reaching no provider keeps its configured position; every candidate holds at "
              + $"least the owner's ten comparable runs: {countStatement}";

        return new SelectionOutcome(
            served?.Route,
            new SelectionRecord
            {
                Basis = SelectionBasis.Evidence,
                TaskClass = taskClass,
                Candidates = ranked.Select(f => f.Candidate).ToArray(),
                ObservationsRankedOn = rankedOn,
                ObservationCount = MeasurementQuantity.Count(rankedOn.Length, "observations ranked on"),
                Statement = statement,
            });
    }

    /// <summary>
    /// The record of a resolution that ended before any candidate was ranked: configured, no candidate,
    /// nothing ranked on, and the step that ended it named.
    /// </summary>
    public static SelectionRecord NotRanked(TaskClass? taskClass, string why)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(why);

        return new SelectionRecord
        {
            Basis = SelectionBasis.Configured,
            TaskClass = taskClass,
            Candidates = [],
            ObservationsRankedOn = [],
            ObservationCount = MeasurementQuantity.Count(0, "observations ranked on"),
            Statement = $"no candidate was ranked: {why}",
        };
    }

    /// <summary>One candidate's figures and every reason its evidence cannot be ranked on.</summary>
    private sealed record CandidateFigures(
        Route Route,
        IReadOnlyList<BenchmarkObservation> Observations,
        SelectionCandidate Candidate,
        IReadOnlyList<string> Gaps,
        decimal MinimumQuality,
        decimal MaximumCost);

    private static CandidateFigures Figures(
        Route route,
        TaskClass? taskClass,
        string currency,
        IReadOnlyList<BenchmarkObservation> evidence)
    {
        var observations = taskClass is { } task
            ? evidence.Where(o => o.Route.Equals(route.Id) && o.TaskClass == task).OrderBy(o => o.Id.Value).ToArray()
            : [];

        var gaps = new List<string>();
        var label = $"route {route.Id}";
        var lookedFor = taskClass is { } named ? $"for task class {named}" : "with no task class named";

        MeasurementQuantity quality;
        MeasurementQuantity cost;
        decimal minimumQuality = 0m;
        decimal maximumCost = 0m;

        if (observations.Length == 0)
        {
            var detail = $"no observation of {label} {lookedFor} exists in the benchmark record";
            quality = MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, $"the quality: {detail}");
            cost = MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, $"the cost: {detail}");
            if (taskClass is not null)
            {
                gaps.Add($"{label} has no observation {lookedFor}");
            }
        }
        else
        {
            (quality, minimumQuality) = Extreme(
                observations.Select(o => (o.Id, o.Quality)).ToArray(),
                BenchmarkObservation.QualityRatingUnit,
                takeMinimum: true,
                $"the quality of {label} {lookedFor}",
                gaps);
            (cost, maximumCost) = Extreme(
                observations.Select(o => (o.Id, o.Cost)).ToArray(),
                currency,
                takeMinimum: false,
                $"the cost of {label} {lookedFor}",
                gaps);
        }

        return new CandidateFigures(
            route,
            observations,
            new SelectionCandidate
            {
                Route = route.Id,
                Tier = route.Tier,
                ConfiguredRating = route.RatedQuality,
                ObservedQuality = quality,
                ObservedCost = cost,
                Observations = MeasurementQuantity.Count(observations.Length, "observations"),
            },
            gaps,
            minimumQuality,
            maximumCost);
    }

    /// <summary>
    /// The minimum or maximum of one quantity over a candidate's observations, ONLY where every one is
    /// observed in the required unit; otherwise unmeasured, naming the observation or the unit found, and
    /// a gap is recorded so the candidate cannot be ranked on it.
    /// </summary>
    private static (MeasurementQuantity Figure, decimal Value) Extreme(
        IReadOnlyList<(BenchmarkObservationId Id, MeasurementQuantity Quantity)> values,
        string requiredUnit,
        bool takeMinimum,
        string what,
        List<string> gaps)
    {
        var amounts = new List<decimal>();

        foreach (var (id, quantity) in values)
        {
            switch (quantity)
            {
                case MeasurementQuantity.Unmeasured unmeasured:
                    gaps.Add($"{what} is unmeasured in observation {id}");
                    return (MeasurementQuantity.NotMeasured(
                        unmeasured.Reason,
                        $"{what} cannot be stated, because observation {id} is unmeasured: {unmeasured.Detail}"), 0m);

                case MeasurementQuantity.ObservedValue observed when observed.Unit != requiredUnit:
                    gaps.Add($"{what} is observed in {observed.Unit}, not in {requiredUnit}, in observation {id}");
                    return (MeasurementQuantity.NotMeasured(
                        UnmeasuredReason.SourceCannotStateOne,
                        $"{what} cannot be compared, because observation {id} is in {observed.Unit}, not in {requiredUnit}"), 0m);

                case MeasurementQuantity.ObservedZero zero when zero.Unit != requiredUnit:
                    gaps.Add($"{what} is observed in {zero.Unit}, not in {requiredUnit}, in observation {id}");
                    return (MeasurementQuantity.NotMeasured(
                        UnmeasuredReason.SourceCannotStateOne,
                        $"{what} cannot be compared, because observation {id} is in {zero.Unit}, not in {requiredUnit}"), 0m);

                case MeasurementQuantity.ObservedValue observed:
                    amounts.Add(observed.Amount);
                    break;

                case MeasurementQuantity.ObservedZero:
                    amounts.Add(0m);
                    break;

                default:
                    throw new InvalidOperationException("Unreachable: the measurement union has three cases.");
            }
        }

        var extreme = takeMinimum ? amounts.Min() : amounts.Max();
        return (MeasurementQuantity.Observed(extreme, requiredUnit), extreme);
    }
}
