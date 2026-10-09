using MediaCompany.Application.Ports;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Deterministic.Analytics;

/// <summary>
/// The AI-economics composers (decisions D-002 and D-006 of its design): the ONE composing site at which
/// every case of the benchmark readings and the tier distribution is decided.
///
/// They sit in the rule-determined assembly with the delivered composers and add no reference, so a
/// reasoning capability is not referenceable from here. Each is a pure function over a boundary summary
/// the datastore produced; none creates a record and none performs money arithmetic. A quantity the
/// record cannot state resolves to the unmeasured case, naming what was looked for, and is never filled
/// in, and an observed zero is its own case.
/// </summary>
public static class EconomicsComposers
{
    /// <summary>What the benchmark record reading is and what it does not establish.</summary>
    public const string BenchmarkStatement =
        "every route the route register holds, for every task class of the closed set, read from the benchmark record in "
        + "one snapshot: each route's minimum observed quality, maximum observed cost and maximum observed latency for the "
        + "task class, with how many observations stand behind them, unmeasured wherever the record holds no observation "
        + "of the pair or any of its observations is unmeasured in that quantity; no minimum observation count is applied, "
        + "a configured route rating is never presented as an observed quality, and an observation read from a "
        + "demonstration store is a demonstration fixture";

    /// <summary>The split the cost model assumes, labelled an assumption. Recorded figures, never measured here.</summary>
    public const string AssumedSplit =
        "assumed, not measured: the cost model's per-item token ledger assumes 87,000 input and 13,000 output units at the "
        + "L1 and L2 complexity levels and 290,000 input and 20,000 output units at the L3 and L4 levels; that split is an "
        + "assumption carried from the cost model, and this reading neither measures, confirms nor refutes it";

    /// <summary>That no relation of complexity levels to reasoning tiers is recorded.</summary>
    public const string RelationStatement =
        "no record, configuration or document relates the five complexity levels to the three reasoning tiers, so this "
        + "reading is per served tier and decides no split; the relation is the owner's to record";

    // -----------------------------------------------------------------------
    // The benchmark record (decision D-002)
    // -----------------------------------------------------------------------

    /// <summary>Every route the register holds, for every task class of the closed set.</summary>
    public static BenchmarkRecordReading Benchmark(BenchmarkRecordSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var readings = new List<BenchmarkReading>();
        foreach (var route in summary.Routes.OrderBy(r => r.Value))
        {
            foreach (var task in Enum.GetValues<TaskClass>())
            {
                var observations = summary.Observations
                    .Where(o => o.Route.Equals(route) && o.TaskClass == task)
                    .OrderBy(o => o.Id.Value)
                    .ToArray();

                var lookedFor = $"route {route} for task class {task}";

                readings.Add(new BenchmarkReading
                {
                    Route = route,
                    TaskClass = task,
                    Quality = Extreme(observations.Select(o => (o.Id, o.Quality)).ToArray(), takeMinimum: true, $"the quality of {lookedFor}"),
                    Cost = Extreme(observations.Select(o => (o.Id, o.Cost)).ToArray(), takeMinimum: false, $"the cost of {lookedFor}"),
                    Latency = Extreme(observations.Select(o => (o.Id, o.Latency)).ToArray(), takeMinimum: false, $"the latency of {lookedFor}"),
                    Observations = MeasurementQuantity.Count(observations.Length, "observations"),
                });
            }
        }

        return new BenchmarkRecordReading { Readings = readings, Statement = BenchmarkStatement };
    }

    /// <summary>The observations booked into one month, with the month's finality from the same read.</summary>
    public static BenchmarkMonthReading BenchmarkMonth(MonthReading<IReadOnlyList<BenchmarkObservation>> reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var month = reading.Closure.Month;

        return new BenchmarkMonthReading
        {
            Month = month,
            Finality = ChannelAnalyticsComposers.Finality(reading.Closure),
            Observations = reading.Value,
            Count = reading.Value.Count == 0
                ? MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.NoObservationExists,
                    $"no benchmark observation is recorded in the benchmark record for {month:yyyy-MM}")
                : MeasurementQuantity.Count(reading.Value.Count, "observations"),
        };
    }

    /// <summary>
    /// The minimum or maximum of one quantity over observations, ONLY where every one is observed and in one
    /// unit; otherwise unmeasured, naming what was looked for, the unmeasured observation or the units found.
    /// </summary>
    private static MeasurementQuantity Extreme(
        IReadOnlyList<(BenchmarkObservationId Id, MeasurementQuantity Quantity)> values,
        bool takeMinimum,
        string what)
    {
        if (values.Count == 0)
        {
            return MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"no observation of {what} exists in the benchmark record");
        }

        var unmeasured = values.FirstOrDefault(v => v.Quantity is MeasurementQuantity.Unmeasured);
        if (unmeasured.Quantity is MeasurementQuantity.Unmeasured gap)
        {
            return MeasurementQuantity.NotMeasured(
                gap.Reason,
                $"{what} cannot be stated, because observation {unmeasured.Id} is unmeasured in it: {gap.Detail}");
        }

        var units = values.Select(v => v.Quantity switch
            {
                MeasurementQuantity.ObservedValue observed => observed.Unit,
                MeasurementQuantity.ObservedZero zero => zero.Unit,
                _ => throw new InvalidOperationException("Unreachable: an unmeasured observation was handled above."),
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (units.Length != 1)
        {
            return MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"{what} cannot be stated as one figure, because its observations are in {string.Join(" and ", units)}");
        }

        var amounts = values.Select(v => v.Quantity is MeasurementQuantity.ObservedValue observed ? observed.Amount : 0m).ToArray();
        return MeasurementQuantity.Observed(takeMinimum ? amounts.Min() : amounts.Max(), units[0]);
    }

    // -----------------------------------------------------------------------
    // The tier distribution (decision D-006)
    // -----------------------------------------------------------------------

    /// <summary>
    /// A month's tier distribution: per stated tier and untiered, and the period total, every part in its
    /// case, with the assumed split labelled, the count of operations carrying a served tier, the statement
    /// that no level-to-tier relation is recorded, and the month's finality from the same read.
    /// </summary>
    public static TierDistributionReading TierDistribution(TierDistributionSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var month = summary.Closure.Month;
        var noOperation = summary.Total.Operations == 0;

        TierDistributionPart Part(ReasoningTier? tier, TierPartTotals? totals, string label)
        {
            if (noOperation)
            {
                var none = MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.NoObservationExists,
                    $"no operation is recorded in the operation record for {month:yyyy-MM}, so {label} was not observed");
                return new TierDistributionPart
                {
                    Tier = tier,
                    Operations = none,
                    InputUnits = none,
                    OutputUnits = none,
                    CachedUnits = none,
                    Cost = none,
                };
            }

            var part = totals ?? new TierPartTotals(tier, 0, 0, 0, 0, Domain.Accounting.Money.Zero(), 0);

            return new TierDistributionPart
            {
                Tier = tier,
                Operations = MeasurementQuantity.Count(part.Operations, "operations"),
                InputUnits = MeasurementQuantity.Count(part.InputUnits, "input units"),
                OutputUnits = MeasurementQuantity.Count(part.OutputUnits, "output units"),
                CachedUnits = MeasurementQuantity.Count(part.CachedUnits, "cached units"),
                Cost = part.UnstatedCosts > 0
                    ? MeasurementQuantity.NotMeasured(
                        UnmeasuredReason.SourceCannotStateOne,
                        $"{part.UnstatedCosts} of the {part.Operations} operations of {label} in {month:yyyy-MM} carry a cost "
                        + "that is not stated, so its cost cannot be stated")
                    : MeasurementQuantity.Observed(part.Cost.Amount, part.Cost.Currency),
            };
        }

        var parts = new List<TierDistributionPart>();
        foreach (var tier in Enum.GetValues<ReasoningTier>())
        {
            parts.Add(Part(tier, summary.Parts.FirstOrDefault(p => p.Tier == tier), $"the operations served at {tier}"));
        }

        parts.Add(Part(null, summary.Parts.FirstOrDefault(p => p.Tier is null), "the operations whose route stated no tier"));

        return new TierDistributionReading
        {
            Month = month,
            Finality = ChannelAnalyticsComposers.Finality(summary.Closure),
            Parts = parts,
            Total = Part(null, summary.Total, "the period total"),
            TierEvidence = noOperation
                ? MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.NoObservationExists,
                    $"no operation is recorded in the operation record for {month:yyyy-MM}, so no served-tier record exists")
                : MeasurementQuantity.Count(summary.ServedTierRecords, "operations carrying a served tier"),
            AssumedSplit = AssumedSplit,
            RelationStatement = RelationStatement,
        };
    }
}
