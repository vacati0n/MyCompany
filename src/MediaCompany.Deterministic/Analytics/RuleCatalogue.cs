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
        outcomes.AddRange(UnbudgetedChannel(reports, snapshot));
        outcomes.AddRange(QualitativeReview(reports, snapshot));
        outcomes.AddRange(ControllerActions(reports, snapshot));
        outcomes.AddRange(Reverification(reports, snapshot));
        outcomes.AddRange(EscalatedDeferral(reports, snapshot));
        outcomes.AddRange(AwaitingApproval(reports));
        return outcomes;
    }

    /// <summary>
    /// The current entry of a question's lineage (correction cycle): starting from the entry the transcription
    /// recorded, follow each later entry that supersedes the current one until none does. A re-worded question
    /// recorded as a superseding open entry is the current entry, and a decided one closes the lineage.
    /// </summary>
    public static RegisterEntry? Current(string lineageRoot, IReadOnlyList<RegisterEntry> register)
    {
        ArgumentNullException.ThrowIfNull(register);

        var current = register.FirstOrDefault(e => e.Identifier == lineageRoot);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (current is not null && seen.Add(current.Identifier))
        {
            var next = register.FirstOrDefault(e => e.Supersedes == current.Identifier);
            if (next is null)
            {
                return current;
            }

            current = next;
        }

        return current;
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

    /// <summary>
    /// The unbudgeted-channel rule. While the register's question on an unbudgeted channel is open, it issues for every
    /// channel with no budget amount, anchored on the open entry, as delivered. Once the question is decided (the
    /// owner's answer that the company ceiling and an item's recorded cap govern a channel with no budget amount), the
    /// rule KEEPS ISSUING for a channel whose metered work admission would still refuse — a channel with an item that
    /// carries no recorded cap, or with no item at all — anchored on the decided entry; it abstains only for a channel
    /// whose every item carries a recorded cap, where admission now admits (the ruling on the review's question).
    /// </summary>
    private static IEnumerable<RuleOutcome> UnbudgetedChannel(ManagementReportSet reports, CompanySnapshot snapshot)
    {
        var informs = "what each channel's budget amount is, and whether an unbudgeted channel may rely on the company ceiling (owner)";

        // The current entry of the transcribed question's lineage, never the transcribed identifier itself.
        var entry = Current("REG-004", reports.Register);

        foreach (var line in reports.Lines.Where(l =>
                     l.Report == ReportKind.Cfo && l.Key == ReportLineKey.ChannelBudget && l.Figure.Case == FigureCase.NotRecorded))
        {
            if (entry is not null && !IsOpen(entry, reports.Register) && entry.Status == RegisterEntryStatus.Decided
                && snapshot.ItemCapCoverage is { } coverage)
            {
                var covered = coverage.FirstOrDefault(c => line.Label.StartsWith($"channel {c.Channel}: ", StringComparison.Ordinal));
                var items = covered?.Items ?? 0;
                var capped = covered?.CappedItems ?? 0;
                if (items > 0 && capped == items)
                {
                    yield return new RuleOutcome.Abstained
                    {
                        Rule = RecommendationRule.CfoUnbudgetedChannel,
                        Reading = line.Label,
                        Instant = reports.Instant,
                        Informs = informs,
                        Reason = $"the question is decided ({entry.Identifier}), and every one of the channel's {items} item(s) carries a recorded cap, "
                            + "so admission admits its metered work under the company ceiling and the item's cap; the rule has nothing to issue",
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
                    AnchorStatement = $"the decided register entry {entry.Identifier} ({entry.Statement}) and the Design Gate's scope: the company "
                        + "ceiling and an item's recorded cap govern a channel with no budget amount, so an item with no recorded cap is still refused",
                    Statement = (items == 0
                            ? "the channel has no item, so any item it takes on has no recorded cap"
                            : $"{items - capped} of the channel's {items} item(s) carry no recorded cap")
                        + ", and no budget amount is recorded for the channel in this booking month, so that metered work is refused "
                        + "under its own reason; record the item's cap or the channel's amount",
                };
                continue;
            }

            if (entry is null || !IsOpen(entry, reports.Register))
            {
                yield return new RuleOutcome.Abstained
                {
                    Rule = RecommendationRule.CfoUnbudgetedChannel,
                    Reading = line.Label,
                    Instant = reports.Instant,
                    Informs = informs,
                    Reason = "the current entry of the register's question on an unbudgeted channel is not open"
                        + (entry is null ? string.Empty : $" ({entry.Identifier}, {entry.Status})")
                        + ", so the rule has no recorded anchor",
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
            // The same count over the same candidate sets the report states, ComparableRuns.OfCandidates per
            // reasoning-tier class of the capability's available routes (correction cycle).
            var counts = ComparableRuns.TierClasses(snapshot.Routes, capability, snapshot.Availability, snapshot.AccountStatus)
                .SelectMany(tierClass => Enum.GetValues<TaskClass>().Select(task => (
                    Name: $"{ManagementComposers.TierClassName(tierClass[0].StatedReasoningTier)} / {task}",
                    Count: ComparableRuns.OfCandidates(tierClass, task, snapshot.Benchmark.Observations))))
                .ToArray();
            var below = counts.Where(c => !ComparableRuns.ReachesOwnersTen(c.Count)).Select(c => c.Name).ToArray();
            if (below.Length == 0)
            {
                continue;
            }

            var least = counts.Where(c => below.Contains(c.Name)).Select(c => c.Count).First();
            yield return new RuleOutcome.Issued
            {
                Rule = RecommendationRule.CtoQualitativeReview,
                Reading = $"{capability}: comparable runs of {below[0]}, the first class and task below the owner's ten",
                Instant = reports.Instant,
                Informs = informs,
                Figure = LineFigure.Of(least),
                Anchor = RecordedProgrammeFacts.ComparableRunsPerTask,
                AnchorStatement = "the owner's ten comparable runs per task, the one count the router and this report share",
                Statement = $"{below.Length} of the {counts.Length} tier-class and task pairs of {capability} hold fewer comparable runs "
                    + $"than the owner's ten ({string.Join(", ", below)}); the router keeps the labelled configured ordering and the "
                    + "CTO report keeps the qualitative-review label for them",
                Cto = Cto(
                    $"the labelled configured ordering for {capability}",
                    $"{below.Length} tier-class and task pairs are below the owner's ten comparable runs, so no evidence ranking may replace the configured ordering",
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

        // The most severe first, so the brief's one shown output is the most severe action.
        foreach (var decision in ManagementComposers.Severity(snapshot.Decisions).Where(d => d.Action >= ControllerAction.Downgrade))
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
        var entry = Current("REG-008", reports.Register);

        // The earliest recorded escalation first, so the brief's one shown output is the most urgent.
        foreach (var held in ManagementComposers.Urgency(snapshot.Held))
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
