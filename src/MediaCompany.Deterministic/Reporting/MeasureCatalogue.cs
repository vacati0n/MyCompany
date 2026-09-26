namespace MediaCompany.Deterministic.Reporting;

/// <summary>
/// One measure the company may report. A measure is either measurable now, or deferred on a
/// parameter that has not been observed.
/// </summary>
public abstract record Measure
{
    private Measure()
    {
    }

    public abstract string Name { get; }

    /// <summary>A measure computable before any revenue parameter is observed.</summary>
    public sealed record MeasurableNow(string MeasureName, string Unit, string Source) : Measure
    {
        public override string Name => MeasureName;
    }

    /// <summary>
    /// A measure awaiting a parameter. Acceptance criterion AC-026 forbids showing it as zero, a
    /// dash, or any placeholder a reader could mistake for a measurement, so the type carries no
    /// value field at all: there is nothing a renderer could print as a number.
    /// </summary>
    public sealed record Deferred(string MeasureName, string AwaitingParameter) : Measure
    {
        public override string Name => MeasureName;

        public string Display => $"not yet available — awaiting {AwaitingParameter}";
    }
}

/// <summary>
/// The measure set the company reports (module M-016, plan task T-025). The reported set carries
/// no revenue-denominated measure, and each deferred measure names the parameter it waits on.
///
/// Every cost figure in the measurable-now set is an ESTIMATE over unit prices that no first-hand
/// verification has confirmed, which is why <see cref="Measure.MeasurableNow.Source"/> is a
/// required field: the report states where each figure came from.
/// </summary>
public static class MeasureCatalogue
{
    public static IReadOnlyList<Measure> All { get; } =
    [
        new Measure.MeasurableNow("cost-per-item", "USD", "recorded operations, exact decimal aggregation"),
        new Measure.MeasurableNow("monthly-cost-total", "USD", "recorded operations, exact decimal aggregation"),
        new Measure.MeasurableNow("cost-variance-against-envelope", "USD", "recorded operations against the approved envelope"),
        new Measure.MeasurableNow("cost-by-capability-class", "USD", "recorded operations, exact decimal aggregation"),
        new Measure.MeasurableNow("deterministic-set-ai-cost", "USD", "recorded operations attributed to the named deterministic set"),
        new Measure.MeasurableNow("budget-utilization-percent", "percent", "recorded operations against the configured budget"),
        new Measure.MeasurableNow("owner-approval-elapsed-minutes", "minutes", "two recorded timestamps per approval"),
        new Measure.MeasurableNow("owner-send-back-count", "count", "recorded approval set"),
        new Measure.MeasurableNow("capability-error-rate", "percent", "recorded operation outcomes"),
        new Measure.MeasurableNow("held-work-count", "count", "recorded held resolutions"),
        new Measure.MeasurableNow("stage-completion-count", "count", "recorded job stages"),
        new Measure.MeasurableNow("failures-ending-unretried-and-unescalated", "count", "recorded job stages"),
        new Measure.MeasurableNow("assets-without-verified-permission-basis", "count", "asset and rights ledger"),
        new Measure.MeasurableNow("operations-with-incomplete-attribution", "count", "recorded operations"),

        new Measure.Deferred("revenue-per-mille", "an observed RPM from the publishing platform"),
        new Measure.Deferred("profit", "an observed revenue figure"),
        new Measure.Deferred("revenue-per-item", "an observed revenue figure"),
        new Measure.Deferred("return-on-cost", "an observed revenue figure"),
        new Measure.Deferred("watch-hours", "an observed audience figure from the publishing platform"),
        new Measure.Deferred("subscriber-growth-rate", "an observed audience figure from the publishing platform"),
        new Measure.Deferred("partner-programme-threshold-progress", "observed watch hours and subscriber counts"),
    ];

    public static IReadOnlyList<Measure.MeasurableNow> MeasurableNow { get; } =
        All.OfType<Measure.MeasurableNow>().ToArray();

    public static IReadOnlyList<Measure.Deferred> Deferred { get; } =
        All.OfType<Measure.Deferred>().ToArray();
}
