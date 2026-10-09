using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Accounting;
using MediaCompany.Deterministic.Routing;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Management;

namespace MediaCompany.Deterministic.Analytics;

/// <summary>
/// The CLOSED CATALOGUE of recommendation rules (the AI-management change, decision D-009 of its design), a
/// member of the zero-AI-cost set under the name <see cref="DeterministicTaskRegistry.RuleEvaluation"/>.
///
/// Seven fixed rules, evaluated once per read in catalogue order, each over the composed lines of that read and
/// the recorded rows they rest on. A rule keys on a RECORDED ANCHOR — the owner's decided ceiling, the
/// controller's fixed thresholds, the owner's ten, the recorded end of the re-verification obligation, a
/// request's own recorded hold timeout, an open register entry — or on MEASUREMENT CASE ALONE; no rule keys on a
/// clean-record, approval-workload, sizing or any other threshold nobody recorded. A rule whose reading is
/// unmeasured ABSTAINS, naming the reading, and issues nothing. A rule whose reading is measured and reaches no
/// anchor issues nothing. The same read evaluated twice yields the same outcomes in the same order.
/// </summary>
public static class RuleCatalogue
{
    public const string TaskName = DeterministicTaskRegistry.RuleEvaluation;

    /// <summary>Every rule over one read, in catalogue order.</summary>
    public static IReadOnlyList<RuleOutcome> Evaluate(ManagementReportSet reports, CompanySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(snapshot);

        var outcomes = new List<RuleOutcome>();
        outcomes.AddRange(CompanyCeiling(reports));
        outcomes.AddRange(UnbudgetedChannel(reports));
        outcomes.AddRange(QualitativeReview(reports, snapshot));
        outcomes.AddRange(ControllerActions(reports, snapshot));
        outcomes.AddRange(Reverification(reports, snapshot));
        outcomes.AddRange(EscalatedDeferral(reports, snapshot));
        outcomes.AddRange(AwaitingApproval(reports));
        return outcomes;
    }

    /// <summary>Whether a register entry is open: recorded open and superseded by no later entry.</summary>
    public static bool IsOpen(RegisterEntry entry, IReadOnlyList<RegisterEntry> register)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(register);
        return entry.Status == RegisterEntryStatus.Open && !register.Any(e => e.Supersedes == entry.Identifier);
    }

    private static RuleOutcome.Abstained Abstain(RecommendationRule rule, ReportLine line, ManagementReportSet reports, string informs) => new()
    {
        Rule = rule,
        Reading = line.Label,
        Instant = reports.Instant,
        Informs = informs,
        Reason = $"the reading is {LineFigure.CaseWord(line.Figure.Case)}, so the rule abstains and issues nothing: {line.Figure.Describe()}",
    };

    private static MeasurementQuantity NotStated(string what) =>
        MeasurementQuantity.NotMeasured(
            UnmeasuredReason.SourceCannotStateOne,
            $"{what} is not stated: the rule compares no prices and no capacities, and no metered call is authorised to "
            + "observe one (the owner's decision of 2026-10-09)");

    private static CtoRecommendation Cto(string current, string problem, string alternative, string risk, string recommendation, string decision) => new()
    {
        CurrentApproach = current,
        Problem = problem,
        Alternative = alternative,
        MonthlyCostDifference = NotStated("the monthly cost difference"),
        CapacityDifference = NotStated("the capacity difference"),
        QualityImpact = NotStated("the quality impact"),
        ImplementationCost = NotStated("the implementation cost"),
        OperationalRisk = risk,
        Recommendation = recommendation,
        DecisionRequired = decision,
    };

    private static string ActionText(ControllerAction action) => action switch
    {
        ControllerAction.AlertOnly => "raises the delivered alert and nothing else",
        ControllerAction.Downgrade => "downgrades the reasoning tier of metered work",
        ControllerAction.Defer => "defers metered work and holds it",
        ControllerAction.Refuse => "refuses metered work",
        _ => "takes no action",
    };

    // -----------------------------------------------------------------------
    // CFO rule 1: the company ceiling
    // -----------------------------------------------------------------------

    private static IEnumerable<RuleOutcome> CompanyCeiling(ManagementReportSet reports)
    {
        var informs = "whether to change the decided ceiling or the month's metered work (owner)";

        foreach (var line in reports.Lines.Where(l => l.Report == ReportKind.Cfo && l.Key == ReportLineKey.CeilingUtilisation))
        {
            var percent = line.Figure switch
            {
                LineFigure.Measured { Quantity: MeasurementQuantity.ObservedValue observed } => observed.Amount,
                LineFigure.Measured { Quantity: MeasurementQuantity.ObservedZero } => 0m,
                _ => (decimal?)null,
            };

            if (percent is not { } observedPercent)
            {
                yield return Abstain(RecommendationRule.CfoCompanyCeiling, line, reports, informs);
                continue;
            }

            if (BudgetThresholds.HighestReached(observedPercent) is not { } threshold)
            {
                continue;
            }

            yield return new RuleOutcome.Issued
            {
                Rule = RecommendationRule.CfoCompanyCeiling,
                Reading = line.Label,
                Instant = reports.Instant,
                Informs = informs,
                Figure = line.Figure,
                Anchor = RecordedProgrammeFacts.ControllerThreshold(threshold),
                AnchorStatement = $"the cost controller's fixed {threshold} percent threshold, against the owner's decided ceiling "
                    + $"({RecordedProgrammeFacts.CompanyCeiling.Describe()})",
                Statement = $"company booked metered spend has reached the {threshold} percent threshold of the owner's decided "
                    + $"ceiling; at that threshold the cost controller {ActionText(CostController.ActionFor(threshold))}",
            };
        }
    }

    // -----------------------------------------------------------------------
    // CFO rule 2: an unbudgeted channel
    // -----------------------------------------------------------------------

    private static IEnumerable<RuleOutcome> UnbudgetedChannel(ManagementReportSet reports)
    {
        var informs = "what each channel's budget amount is, and whether an unbudgeted channel may rely on the company ceiling (owner)";
        var entry = reports.Register.FirstOrDefault(e => e.Identifier == "REG-004");

        foreach (var line in reports.Lines.Where(l =>
                     l.Report == ReportKind.Cfo && l.Key == ReportLineKey.ChannelBudget && l.Figure.Case == FigureCase.NotRecorded))
        {
            if (entry is null || !IsOpen(entry, reports.Register))
            {
                yield return new RuleOutcome.Abstained
                {
                    Rule = RecommendationRule.CfoUnbudgetedChannel,
                    Reading = line.Label,
                    Instant = reports.Instant,
                    Informs = informs,
                    Reason = "the register holds no open entry REG-004 on an unbudgeted channel, so the rule has no recorded anchor",
                };
                continue;
            }

            yield return new RuleOutcome.Issued
            {
                Rule = RecommendationRule.CfoUnbudgetedChannel,
                Reading = line.Label,
                Instant = reports.Instant,
                Informs = informs,
                Figure = line.Figure,
                AnchorStatement = $"the open register entry {entry.Identifier} and its interim ruling: {entry.InterimRuling}",
                Statement = "no budget amount is recorded for the channel in this booking month, so its metered work is refused "
                    + "under its own reason; record the amount, or rule on the open entry",
            };
        }
    }

    // -----------------------------------------------------------------------
    // CTO rule 1: qualitative review below the owner's ten
    // -----------------------------------------------------------------------

    private static IEnumerable<RuleOutcome> QualitativeReview(ManagementReportSet reports, CompanySnapshot snapshot)
    {
        var informs = "whether and when to authorise benchmark runs (owner)";

        foreach (var capability in snapshot.Routes.Select(r => r.Capability).Distinct().OrderBy(c => c))
        {
            var routes = snapshot.Routes.Where(r => r.Capability == capability).Select(r => r.Id).ToArray();
            var counts = Enum.GetValues<TaskClass>()
                .Select(task => (Task: task, Count: ComparableRuns.LeastOver(routes, task, snapshot.Benchmark.Observations)))
                .ToArray();
            var below = counts.Where(c => !ComparableRuns.ReachesOwnersTen(c.Count)).Select(c => c.Task).ToArray();
            if (below.Length == 0)
            {
                continue;
            }

            var least = counts.Where(c => below.Contains(c.Task)).Select(c => c.Count).First();
            yield return new RuleOutcome.Issued
            {
                Rule = RecommendationRule.CtoQualitativeReview,
                Reading = $"{capability}: comparable runs of task class {below[0]}, the first below the owner's ten",
                Instant = reports.Instant,
                Informs = informs,
                Figure = LineFigure.Of(least),
                Anchor = RecordedProgrammeFacts.ComparableRunsPerTask,
                AnchorStatement = "the owner's ten comparable runs per task, the one count the router and this report share",
                Statement = $"{below.Length} of the {counts.Length} task classes of {capability} hold fewer comparable runs than the owner's "
                    + $"ten ({string.Join(", ", below)}); the router keeps the labelled configured ordering and the CTO report keeps "
                    + "the qualitative-review label for them",
                Cto = Cto(
                    $"the labelled configured ordering for {capability}",
                    $"{below.Length} task classes are below the owner's ten comparable runs, so no evidence ranking may replace the configured ordering",
                    "record comparable benchmark runs until every task class holds ten",
                    "none added by keeping the configured ordering; it is labelled configured on every decision",
                    "keep the labelled configured ordering and the qualitative-review label until each task class holds ten comparable runs",
                    "whether and when to authorise metered benchmark runs (owner)"),
            };
        }
    }

    // -----------------------------------------------------------------------
    // CTO rule 2: a controller action of the week
    // -----------------------------------------------------------------------

    private static IEnumerable<RuleOutcome> ControllerActions(ManagementReportSet reports, CompanySnapshot snapshot)
    {
        var informs = "whether the decided ceiling or a channel budget needs changing (owner)";

        foreach (var decision in snapshot.Decisions.Where(d => d.Action >= ControllerAction.Downgrade))
        {
            var deciding = decision.Readings.Where(r => r.Action == decision.Action).OrderBy(r => r.Scope).FirstOrDefault();
            var label = $"decision for operation {decision.Operation}: the deciding reading's utilisation";
            var figure = deciding is null
                ? LineFigure.Of(MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.NoObservationExists, $"no governing reading of decision {decision.Operation} is recorded"))
                : LineFigure.Of(deciding.Utilisation);

            if (!figure.CarriesAValue)
            {
                yield return new RuleOutcome.Abstained
                {
                    Rule = RecommendationRule.CtoControllerAction,
                    Reading = label,
                    Instant = reports.Instant,
                    Informs = informs,
                    Reason = $"the reading is {LineFigure.CaseWord(figure.Case)}, so the rule abstains and issues nothing: {figure.Describe()}; "
                        + $"the decision itself, action {decision.Action}{(deciding?.Reason is { } r ? $" for reason {r}" : string.Empty)}, is listed as recorded in the CTO report",
                };
                continue;
            }

            var threshold = deciding!.Threshold;
            yield return new RuleOutcome.Issued
            {
                Rule = RecommendationRule.CtoControllerAction,
                Reading = label,
                Instant = reports.Instant,
                Informs = informs,
                Figure = figure,
                Anchor = threshold is { } t ? RecordedProgrammeFacts.ControllerThreshold((int)t) : null,
                AnchorStatement = threshold is { } th
                    ? $"the cost controller's fixed {(int)th} percent threshold, which demands {decision.Action}"
                    : $"the recorded refusal reason {deciding.Reason}, which demands {decision.Action} without a threshold",
                Statement = $"the cost controller {ActionText(decision.Action)} for operation {decision.Operation} of channel "
                    + $"{decision.Channel} in booking month {decision.BookingMonth:yyyy-MM}, on its {deciding.Scope} reading",
                Cto = Cto(
                    $"metered work governed by the cost controller against {deciding.Amount.Describe()}",
                    $"the {deciding.Scope} reading demanded {decision.Action}",
                    "raise the governing amount, or keep the controller's action",
                    "metered work stays restricted while the reading stands",
                    "review the governing amount against the recorded spend; the controller's mapping is unchanged",
                    "whether the decided ceiling or the channel's budget amount changes (owner)"),
            };
        }
    }

    // -----------------------------------------------------------------------
    // CTO rule 3: re-verification of a recorded policy statement
    // -----------------------------------------------------------------------

    private static IEnumerable<RuleOutcome> Reverification(ManagementReportSet reports, CompanySnapshot snapshot)
    {
        var informs = "who records platform-policy re-verification results and at what cadence (owner)";
        var line = reports.Lines.FirstOrDefault(l => l.Key == ReportLineKey.ReverificationRechecked && l.Report == ReportKind.Cto);
        if (line is null)
        {
            yield break;
        }

        if (!line.Figure.CarriesAValue)
        {
            yield return Abstain(RecommendationRule.CtoReverification, line, reports, informs);
            yield break;
        }

        foreach (var statement in snapshot.PolicyStatements)
        {
            var rechecked = snapshot.Reverifications.Any(r =>
                r.Statement == statement.Identifier && r.RecordedAt >= snapshot.PeriodStart && r.RecordedAt < snapshot.PeriodEnd);
            if (rechecked)
            {
                continue;
            }

            yield return new RuleOutcome.Issued
            {
                Rule = RecommendationRule.CtoReverification,
                Reading = line.Label,
                Instant = reports.Instant,
                Informs = informs,
                Figure = line.Figure,
                Anchor = RecordedProgrammeFacts.ReverificationUntil,
                AnchorStatement = "the recorded end of the re-verification obligation; no cadence is confirmed, so the output says only "
                    + "that no result is recorded this week, never that the statement is overdue",
                Statement = $"platform-policy statement {statement.Identifier} ({statement.Platform}: {statement.Label}) has no "
                    + "re-verification result recorded this week",
                Cto = Cto(
                    $"statement {statement.Identifier} as last recorded",
                    "no re-verification result is recorded for it this week",
                    "re-verify it against its source and record the result",
                    "a changed policy would reach the compliance gate pack late",
                    "re-verify the statement and record the result",
                    "who records re-verification results and at what cadence (owner)"),
            };
        }
    }

    // -----------------------------------------------------------------------
    // COO rule 1: a deferred request past its recorded escalation instant
    // -----------------------------------------------------------------------

    private static IEnumerable<RuleOutcome> EscalatedDeferral(ManagementReportSet reports, CompanySnapshot snapshot)
    {
        var informs = "whether deferred requests are re-admitted, and how (owner)";
        var entry = reports.Register.FirstOrDefault(e => e.Identifier == "REG-008");

        foreach (var held in snapshot.Held)
        {
            var line = reports.Lines.First(l => l.Key == ReportLineKey.HeldRequest && l.Label.Contains(held.Operation.ToString(), StringComparison.Ordinal));

            if (!held.HasHeldOutcomeRow)
            {
                yield return new RuleOutcome.Abstained
                {
                    Rule = RecommendationRule.CooEscalatedDeferral,
                    Reading = line.Label,
                    Instant = reports.Instant,
                    Informs = informs,
                    Reason = "the held operation was recorded before the held-outcome record existed, so its escalation instant is "
                        + "not recorded and the rule abstains",
                };
                continue;
            }

            if (!held.IsDeferral || held.EscalatesAt is not { } escalates || reports.Instant < escalates)
            {
                continue;
            }

            yield return new RuleOutcome.Issued
            {
                Rule = RecommendationRule.CooEscalatedDeferral,
                Reading = line.Label,
                Instant = reports.Instant,
                Informs = informs,
                Figure = line.Figure,
                Anchor = held.HoldTimeout is { TotalSeconds: > 0 } timeout
                    ? RecordedAmount.Figure(
                        (decimal)timeout.TotalSeconds, "seconds, the request's recorded hold timeout",
                        $"the held-outcome record of operation {held.Operation}")
                    : null,
                AnchorStatement = $"the request's own recorded escalation instant, {escalates:O}, set from its recorded hold timeout "
                    + "(the request's recorded value, not an owner threshold)"
                    + (entry is not null && IsOpen(entry, reports.Register)
                        ? $", and the open register entry {entry.Identifier} on re-admission"
                        : string.Empty),
                Statement = $"deferred metered request {held.Operation} of channel {held.Channel} ({held.Reason}) has passed its recorded "
                    + "escalation instant and is not re-admitted; it waits on the owner's ruling on re-admission",
            };
        }
    }

    // -----------------------------------------------------------------------
    // COO rule 2: work awaiting the owner's approval
    // -----------------------------------------------------------------------

    private static IEnumerable<RuleOutcome> AwaitingApproval(ManagementReportSet reports)
    {
        var informs = "which items to approve or send back (owner)";
        var line = reports.Lines.FirstOrDefault(l => l.Report == ReportKind.Coo && l.Key == ReportLineKey.AwaitingApproval);
        if (line is null)
        {
            yield break;
        }

        switch (line.Figure.Case)
        {
            case FigureCase.Observed:
                yield return new RuleOutcome.Issued
                {
                    Rule = RecommendationRule.CooAwaitingApproval,
                    Reading = line.Label,
                    Instant = reports.Instant,
                    Informs = informs,
                    Figure = line.Figure,
                    AnchorStatement = "rests on measurement case alone: owner approval is a transition-table state, and no "
                        + "approval-workload threshold is recorded",
                    Statement = $"{line.Figure.Describe()} wait on the owner; only the owner approves or sends back, and nothing on "
                        + "this report does either",
                };
                break;

            case FigureCase.Unmeasured:
            case FigureCase.NotRecorded:
                yield return Abstain(RecommendationRule.CooAwaitingApproval, line, reports, informs);
                break;
        }
    }
}
