using System.Globalization;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Routing;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Management;
using MediaCompany.Domain.Publication;

namespace MediaCompany.Deterministic.Analytics;

/// <summary>
/// The five report composers (the AI-management change, decision D-004 of its design), a member of the
/// zero-AI-cost set under the name <see cref="DeterministicTaskRegistry.ReportComposition"/>.
///
/// PURE FUNCTIONS OF ONE SNAPSHOT. Each takes the company record reader's one snapshot as values and returns
/// lines; none holds a port, a clock, a configuration value or a capability reference, and none writes. Every
/// line carries the snapshot's one datastore instant. Where a delivered reading exists the line's case is the
/// one its delivered composer decides over the snapshot's rows — the operation partition, the budgets with the
/// ceiling, throughput coverage under the stored horizon, the approval listing, the benchmark record and the
/// tier distribution — and no second site decides it. A figure no source records reads unmeasured naming the
/// absent source; a recorded amount reads recorded naming where; nothing is invented.
/// </summary>
public static class ManagementComposers
{
    public const string TaskName = DeterministicTaskRegistry.ReportComposition;

    /// <summary>Every report of one snapshot, the rule outcomes over them and the register as read.</summary>
    public static ManagementReportSet Compose(CompanySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var lines = new List<ReportLine>();
        lines.AddRange(Coo(snapshot));
        lines.AddRange(Cto(snapshot));
        lines.AddRange(Cfo(snapshot));
        lines.AddRange(ChannelPerformance(snapshot));
        lines.AddRange(Risk(snapshot));

        // THE DEMONSTRATION RULE (the production change, decision D-005 of its design): in a store designated
        // demonstration every observed figure — spend, utilisation, counts and durations alike, all booked or recorded
        // by fake providers — becomes a demonstration figure BEFORE any rule reads it, so no rule, total, brief line or
        // tile can read a fake operation's zero as observed. Unmeasured and recorded figures keep their case.
        if (snapshot.Designation == MediaCompany.Domain.Production.StoreDesignation.Demonstration)
        {
            lines = lines.Select(Demonstrated).ToList();
        }

        var set = new ManagementReportSet
        {
            Instant = snapshot.Instant,
            StoredHorizon = snapshot.StoredHorizon,
            PeriodStart = snapshot.PeriodStart,
            PeriodEnd = snapshot.PeriodEnd,
            Week = snapshot.Week.ToString(),
            Final = WeekIsFinal(snapshot),
            FinalityStatement = FinalityStatement(snapshot),
            Lines = lines,
            Outcomes = [],
            Register = snapshot.RegisterEntries,
        };

        var outcomes = RuleCatalogue.Evaluate(set, snapshot);

        // A rule that issued over a figure of a demonstration store issues nothing: it abstains, naming the designation,
        // whatever the figure it read was computed from (the production change, decision D-005 of its design).
        if (snapshot.Designation == MediaCompany.Domain.Production.StoreDesignation.Demonstration)
        {
            outcomes = outcomes.Select(AbstainOverDemonstration).ToArray();
        }

        return set with { Outcomes = outcomes };
    }

    /// <summary>An issued outcome of a demonstration store, as the abstention it must be.</summary>
    private static RuleOutcome AbstainOverDemonstration(RuleOutcome outcome) => outcome switch
    {
        RuleOutcome.Issued issued => new RuleOutcome.Abstained
        {
            Rule = issued.Rule,
            Reading = issued.Reading,
            Instant = issued.Instant,
            Informs = issued.Informs,
            Reason = "the reading is a demonstration figure of a store designated demonstration, where fake providers run, so the "
                + "rule abstains and issues nothing: "
                + (issued.Figure is LineFigure.Measured measured ? LineFigure.Demonstrated(measured.Quantity) : issued.Figure).Describe(),
        },
        _ => outcome,
    };

    // -----------------------------------------------------------------------
    // Finality, read and never made (decisions D-001 and D-002)
    // -----------------------------------------------------------------------

    /// <summary>A week is final where the record horizon as stored is at or after its end.</summary>
    public static bool WeekIsFinal(CompanySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.StoredHorizon >= snapshot.PeriodEnd;
    }

    /// <summary>The week's finality and every touched month's, as one statement for the report header.</summary>
    public static string FinalityStatement(CompanySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var week = WeekIsFinal(snapshot)
            ? $"week {snapshot.Week} is final: the record horizon as stored, {snapshot.StoredHorizon:O}, is at or after the "
              + $"week's end, {snapshot.PeriodEnd:O}, so nothing more can be recorded inside it and every line re-reads identical"
            : $"week {snapshot.Week} is NOT FINAL: the record horizon as stored, {snapshot.StoredHorizon:O}, is before the "
              + $"week's end, {snapshot.PeriodEnd:O}, so its figures can still change; it becomes final once the delivered "
              + "measurable-now report command, whose closure raises the horizon, has run after the week ended. This read "
              + "never closes, writes or waits";

        var months = snapshot.Months.Select(m => MonthIsFinal(m.Closure)
            ? $"booking month {m.Closure.Month:yyyy-MM} is final: the horizon as stored is at or after its end"
            : $"booking month {m.Closure.Month:yyyy-MM} is not final: the horizon as stored is before its end, so its "
              + "month-to-date figures can still change");

        return string.Join("; ", new[] { week }.Concat(months));
    }

    /// <summary>A month is final by the delivered finality rule, against the horizon as stored.</summary>
    public static bool MonthIsFinal(MonthClosure closure) => ChannelAnalyticsComposers.Finality(closure).IsFinal;

    private static string WeekTag(CompanySnapshot s) => $"week {s.Week} {(WeekIsFinal(s) ? "final" : "not final")}";

    private static string MonthTag(MonthClosure closure) =>
        $"month {closure.Month:yyyy-MM} {(MonthIsFinal(closure) ? "final" : "not final, month to date")}";

    private static string RecordedTag() => "recorded, the same at every instant";

    /// <summary>
    /// The produced item's lines (the production change, decision D-017 of its design): its stage outcomes, its
    /// operations and booked cost, its cap with the counted total, and its runtime measured from the rendered file.
    /// EVERY FIGURE OF A DEMONSTRATION STORE IS LABELLED DEMONSTRATION and is never an observation: it reads
    /// unmeasured, naming the designation, and adds nothing to observed spend. With no production the line reads
    /// not recorded, naming the register; where the snapshot did not read the register, nothing is composed.
    /// </summary>
    private static IEnumerable<ReportLine> ProducedItemLines(
        CompanySnapshot s,
        Func<ReportLineKey, string, string, LineFigure, string, string, string?, string?, DateTimeOffset?, ReportLine> line)
    {
        const string Heading = "Produced item";
        const string Informs = "the owner's go for the one metered production run (owner)";
        const string RestsOn = "the production version register, the item dossier, the operation record, the item cap register and the "
            + "admission reservations, in the one snapshot; ManagementComposers.ProducedItemLines";

        if (s.ProducedItems is null)
        {
            yield break;
        }

        if (s.ProducedItems.Count == 0)
        {
            yield return line(ReportLineKey.ProducedItemStages, Heading, "produced items",
                LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists,
                    "no production is recorded in the production version register")),
                RestsOn, "as of the snapshot", Informs, null, null);
            yield break;
        }

        foreach (var item in s.ProducedItems)
        {
            var demonstration = !string.Equals(item.Designation, "Company", StringComparison.Ordinal);
            var tag = $"item {item.Item} version {item.Version} ({item.Mode} mode, {item.Designation} store)";

            // A demonstration store's figures become demonstration figures by the one rule applied to every line.
            MeasurementQuantity Figure(MeasurementQuantity measured, string what) => measured;

            var stages = string.Join("; ", item.Stages.Select(st => $"{st.Stage} {st.Outcome}"));
            yield return line(ReportLineKey.ProducedItemStages, Heading, $"stage outcomes of {tag}",
                LineFigure.Of(Figure(MeasurementQuantity.Count(item.Stages.Count, "stage outcomes"), $"{item.Stages.Count} stage outcomes recorded")),
                RestsOn, "as of the snapshot", Informs, stages, item.OpenedAt);

            var booked = item.UnstatedOperations > 0
                ? MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne,
                    $"{item.UnstatedOperations} of the {item.Operations} operations carry a cost that is not stated")
                : MeasurementQuantity.Observed(item.Booked.Amount, item.Booked.Currency);
            yield return line(ReportLineKey.ProducedItemOperations, Heading, $"operations and booked cost of {tag}",
                LineFigure.Of(Figure(booked, $"{item.Operations} operations booked {item.Booked}")),
                RestsOn, "as of the snapshot", Informs, $"{item.Operations} operations", item.OpenedAt);

            if (item.Cap is { } cap)
            {
                var counted = item.Booked + item.OpenReservations;
                yield return line(ReportLineKey.ProducedItemCap, Heading, $"cap of item {item.Item}",
                    LineFigure.Of(RecordedAmount.Of(cap, item.CapSource ?? "the item cap register")),
                    RestsOn, "as of the snapshot", Informs,
                    $"counted total {counted} (booked {item.Booked} plus {item.OpenReservationCount} open reservation(s) at their worst case "
                    + $"{item.OpenReservations}); remaining {new Money(cap.Amount - counted.Amount, cap.Currency)}"
                    + (demonstration ? "; DEMONSTRATION figures, never observed" : string.Empty),
                    item.OpenedAt);
            }

            yield return line(ReportLineKey.ProducedItemRuntime, Heading, $"runtime of {tag}, measured from the rendered file",
                LineFigure.Of(item.MeasuredRuntime is { } runtime
                    ? Figure(MeasurementQuantity.Observed(Math.Round((decimal)runtime.TotalSeconds, 3), "seconds"), $"a runtime of {runtime.TotalSeconds:0.000} s measured from the file")
                    : MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, "no rendered file is recorded for the production")),
                RestsOn, "as of the snapshot", Informs, item.RenderedPath, item.OpenedAt);
        }
    }

    /// <summary>A line of a demonstration store: its observed figure becomes a demonstration figure; nothing else changes.</summary>
    private static ReportLine Demonstrated(ReportLine line) =>
        line.Figure is LineFigure.Measured { Quantity: MeasurementQuantity.ObservedValue or MeasurementQuantity.ObservedZero } measured
            ? line with { Figure = LineFigure.Demonstrated(measured.Quantity) }
            : line;

    private static ReportLine Line(
        ReportKind report,
        ReportLineKey key,
        string heading,
        string label,
        LineFigure figure,
        string restsOn,
        CompanySnapshot snapshot,
        string finality,
        string? informs = null,
        string? recordedText = null,
        DateTimeOffset? recordedAt = null) => new()
        {
            Report = report,
            Key = key,
            Heading = heading,
            Label = label,
            Figure = figure,
            RestsOn = restsOn,
            Instant = snapshot.Instant,
            Finality = finality,
            Informs = informs,
            RecordedText = recordedText,
            RecordedAt = recordedAt,
        };

    // -----------------------------------------------------------------------
    // Counts over the append-only record's actions in the week
    // -----------------------------------------------------------------------

    /// <summary>Whether the append-only record was observing during the week: it holds an entry before the week's end.</summary>
    private static bool Observing(CompanySnapshot s) => s.EarliestEntry is { } earliest && earliest < s.PeriodEnd;

    private static MeasurementQuantity NotObserving(CompanySnapshot s, string what) =>
        MeasurementQuantity.NotMeasured(
            UnmeasuredReason.NoObservationExists,
            s.EarliestEntry is { } earliest
                ? $"no observation of {what} exists for week {s.Week}: the append-only record's first entry, at {earliest:O}, "
                  + "is not before the week's end, so the record was not yet observing"
                : $"no observation of {what} exists for week {s.Week}: the append-only record holds no entry");

    /// <summary>
    /// The count of one action in the week, for one channel or for the company: an observed count, zero included,
    /// as recorded at the read, wherever the record was observing during the week.
    /// </summary>
    private static MeasurementQuantity ActionCount(CompanySnapshot s, string action, ChannelId? channel, string unit) =>
        Observing(s)
            ? MeasurementQuantity.Count(
                s.AuditActions.Where(a => a.Action == action && (channel is null || Equals(a.Channel, channel))).Sum(a => a.Count),
                unit)
            : NotObserving(s, unit);

    /// <summary>
    /// A rate: the count of one named action over the count of items reaching its step, in percent, observed zero
    /// included; unmeasured where the record was not observing or no item reached the step.
    /// </summary>
    private static MeasurementQuantity Rate(CompanySnapshot s, string numerator, string denominator, string what, string step)
    {
        if (!Observing(s))
        {
            return NotObserving(s, what);
        }

        var reached = s.AuditActions.Where(a => a.Action == denominator).Sum(a => a.Count);
        if (reached == 0)
        {
            return MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"no item reached {step} inside week {s.Week}, so no {what} is formed; the denominator is not zero items "
                + "observed failing, it is no item observed at all");
        }

        var count = s.AuditActions.Where(a => a.Action == numerator).Sum(a => a.Count);
        return MeasurementQuantity.Observed(Math.Round(count * 100m / reached, 2), "percent");
    }

    // -----------------------------------------------------------------------
    // The COO weekly report (master plan section 32)
    // -----------------------------------------------------------------------

    public static IReadOnlyList<ReportLine> Coo(CompanySnapshot s)
    {
        ArgumentNullException.ThrowIfNull(s);

        var lines = new List<ReportLine>();
        var week = WeekTag(s);
        var throughput = ChannelAnalyticsComposers.Throughput(s.Throughput).Company.Throughput;
        var throughputRestsOn = "the throughput partition of the append-only record, under the delivered coverage rule with the "
            + "record horizon as stored; ChannelAnalyticsComposers.Throughput";

        ReportLine L(ReportLineKey key, string heading, string label, LineFigure figure, string restsOn, string finality, string? informs = null,
            string? text = null, DateTimeOffset? at = null) =>
            Line(ReportKind.Coo, key, heading, label, figure, restsOn, s, finality, informs, text, at);

        // The produced item (the production change, decision D-017 of its design), from the same snapshot.
        lines.AddRange(ProducedItemLines(s, L));

        // Production.
        lines.Add(L(ReportLineKey.UnitsCompleted, "Production", "units completed in the week", LineFigure.Of(throughput.Completed),
            throughputRestsOn, week, "whether production is keeping pace (owner)"));
        lines.Add(L(ReportLineKey.ItemsPublished, "Production", "items published (latest recorded gate state)",
            LineFigure.Of(MeasurementQuantity.Count(s.Published.Sum(p => p.Count), "items published")),
            "the gate-transition record, latest state per item version by the datastore's recorded order; a count, nothing publishes",
            "as of the snapshot", "whether production is keeping pace (owner)"));
        lines.Add(L(ReportLineKey.ItemsPlanned, "Production", "items planned",
            LineFigure.Of(RecordedAmount.Missing(
                "no production plan is recorded in any register; the committed rate of three items a week is a programme figure "
                + "and is shown as neither planned, produced nor published")),
            "the registers of the record store, searched for a production plan", RecordedTag(), "whether to record a production plan (owner)"));
        lines.Add(L(ReportLineKey.BufferDepth, "Production", "buffer depth",
            LineFigure.Of(MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                "no buffer record exists: nothing records items produced and held ready for publication, so no depth or days of "
                + "cover is stated")),
            "the record store, searched for a buffer record", week));
        lines.Add(L(ReportLineKey.AwaitingApproval, "Production", "item versions awaiting owner approval",
            LineFigure.Of(MeasurementQuantity.Count(s.Awaiting.Count, "item versions awaiting owner approval")),
            "the awaiting-approval listing, a view over the recorded gate state", "as of the snapshot",
            "which items to approve or send back (owner)"));

        // Quality.
        lines.Add(L(ReportLineKey.RightsCheckSendBackRate, "Quality", "rights-check send-back rate (copyright)",
            LineFigure.Of(Rate(s, PublicationGateService.SentBackFromRightsCheckAction, PublicationGateService.SubmittedAction,
                "rights-check send-back rate", "the rights check")),
            "audit actions sent back from the rights check over submissions for the rights check, inside the week", week));
        lines.Add(L(ReportLineKey.OwnerSendBackRate, "Quality", "owner send-back rate",
            LineFigure.Of(Rate(s, PublicationGateService.OwnerSentBackAction, PublicationGateService.PresentedAction,
                "owner send-back rate", "presentation for owner approval")),
            "audit actions of owner send-backs over presentations for owner approval, inside the week", week));
        lines.Add(L(ReportLineKey.RejectionRate, "Quality", "rejection rate",
            LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne,
                "the closed verdict set records approval and send-back only, so no source records a rejection")),
            "the closed approval verdict set", week));
        lines.Add(L(ReportLineKey.RevisionRate, "Quality", "revision rate",
            LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists,
                "no record of a revision exists; a send-back is counted on its own line")),
            "the record store, searched for a revision record", week));
        lines.Add(L(ReportLineKey.PolicyRejectionRate, "Quality", "policy rejection rate",
            LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne,
                "no audit action records a policy-check outcome, and a send-back records no ground, so no policy rejection is counted")),
            "the audit actions of the gate service", week));
        foreach (var sendBack in s.SendBacks)
        {
            lines.Add(L(ReportLineKey.SendBackReason, "Quality",
                $"send-back reason ({sendBack.Action}{(sendBack.Channel is { } c ? $", channel {c}" : string.Empty)})",
                LineFigure.Of(MeasurementQuantity.Count(1, "send-back recorded")),
                "the append-only record's send-back entry, its reason printed as recorded", week,
                text: sendBack.Reason, at: sendBack.RecordedAt));
        }

        // Operations.
        lines.Add(L(ReportLineKey.UnitsWaiting, "Operations", "units waiting at the week's close", LineFigure.Of(throughput.Waiting),
            throughputRestsOn, week));
        lines.Add(L(ReportLineKey.QueuePerStage, "Operations", "queue length per stage",
            LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne,
                "the queue table holds only each unit's current claim state, overwritten on every claim, release and advance, so "
                + "no per-stage length at the week's close can be read from it; the units waiting are read from the append-only record")),
            "the job queue table, which carries no recorded instant of its state", week));
        lines.Add(L(ReportLineKey.UnitsEscalated, "Operations", "units escalated to the dead claim state (unhandled failures)",
            LineFigure.Of(throughput.Escalated), throughputRestsOn, week, "which failures to resolve (operations)"));
        lines.Add(L(ReportLineKey.StageRetries, "Operations", "stage attempts retried", LineFigure.Of(throughput.Retried),
            throughputRestsOn, week));
        lines.Add(L(ReportLineKey.OperationsFailed, "Operations", "operations failed in the week",
            LineFigure.Of(MeasurementQuantity.Count(s.FailedOperations.Sum(f => f.Count), "operations failed")),
            "the operation record, outcome failed, booked inside the week", week));
        lines.Add(L(ReportLineKey.HeldOperations, "Operations", "held operations, none re-admitted (the full list is in the COO report)",
            LineFigure.Of(MeasurementQuantity.Count(s.Held.Count, "held operations")),
            "the operation record, outcome held, booked at or before the instant, with the held-outcome record", "as of the snapshot",
            "whether deferred requests are re-admitted (owner)"));
        lines.Add(L(ReportLineKey.EscalatedDeferrals, "Operations",
            "deferred requests past their recorded escalation instant (the full list is in the COO report)",
            LineFigure.Of(MeasurementQuantity.Count(
                s.Held.LongCount(h => h.IsDeferral && h.EscalatesAt is { } at && s.Instant >= at), "escalated deferrals")),
            "the held-outcome record's escalation instants against the report instant; a held operation recorded before the "
            + "record holds no instant and is counted under held operations only",
            "as of the snapshot", "whether deferred requests are re-admitted (owner)"));

        // The most urgent first: the earliest recorded escalation, then the held operations with none recorded.
        foreach (var held in Urgency(s.Held))
        {
            lines.Add(L(ReportLineKey.HeldRequest, "Operations",
                $"{(held.IsDeferral ? "deferred metered request" : "held request")}, operation {held.Operation}, channel {held.Channel}: hold age",
                LineFigure.Of(HoldAge(s, held)),
                "the held-outcome record and the operation's booked instant; the hold age is the report instant less the booked instant",
                "as of the snapshot", "whether deferred requests are re-admitted (owner)",
                HeldText(s, held), held.HeldAt));
        }

        // Channels.
        foreach (var channel in s.Register)
        {
            var listing = ChannelAnalyticsComposers.Listing(channel.Channel, inRegister: true, s.Awaiting);
            lines.Add(L(ReportLineKey.ChannelAwaitingApproval, "Channels", $"channel {channel.Channel}: awaiting owner approval",
                LineFigure.Of(listing.Awaiting), "the per-channel approval listing; ChannelAnalyticsComposers.Listing", "as of the snapshot",
                "which items to approve or send back (owner)"));
            lines.Add(L(ReportLineKey.ChannelPublished, "Channels", $"channel {channel.Channel}: items published",
                LineFigure.Of(MeasurementQuantity.Count(s.Published.Where(p => p.Channel.Equals(channel.Channel)).Sum(p => p.Count), "items published")),
                "the gate-transition record, latest state per item version", "as of the snapshot"));
        }

        // Finance.
        lines.Add(L(ReportLineKey.WeekCost, "Finance", "production cost booked in the week", LineFigure.Of(WeekCost(s, null)),
            "the operation record, booked inside the week, aggregated by the datastore; the delivered cost rule", week,
            "whether spend is on track (owner)"));
        foreach (var month in s.Months)
        {
            lines.Add(L(ReportLineKey.CeilingUtilisation, "Finance",
                $"utilisation of the owner's decided ceiling, {month.Closure.Month:yyyy-MM}",
                LineFigure.Of(ChannelAnalyticsComposers.Budgets(month.AgainstCeiling).Company.Utilisation),
                "the company ceiling reading against the owner's decided ceiling; ChannelAnalyticsComposers.Budgets",
                MonthTag(month.Closure)));
        }

        return lines;
    }

    /// <summary>Held requests in order of urgency: the earliest recorded escalation first, those with none after, by held instant.</summary>
    public static IReadOnlyList<HeldOutcomeRecord> Urgency(IReadOnlyList<HeldOutcomeRecord> held)
    {
        ArgumentNullException.ThrowIfNull(held);
        return held
            .OrderBy(h => h.EscalatesAt is null)
            .ThenBy(h => h.EscalatesAt ?? h.HeldAt)
            .ThenBy(h => h.Operation.Value)
            .ToArray();
    }

    /// <summary>Recorded controller decisions in order of severity: the most severe action first, then by instant.</summary>
    public static IReadOnlyList<RecordedControllerDecision> Severity(IReadOnlyList<RecordedControllerDecision> decisions)
    {
        ArgumentNullException.ThrowIfNull(decisions);
        return decisions.OrderByDescending(d => d.Action).ThenBy(d => d.DecidedAt).ThenBy(d => d.Operation.Value).ToArray();
    }

    /// <summary>
    /// The report instant less the held instant, in minutes to the hundredth on the datastore's clock: both instants
    /// are the datastore's, so a process clock moves neither.
    /// </summary>
    private static MeasurementQuantity HoldAge(CompanySnapshot s, HeldOutcomeRecord held) =>
        MeasurementQuantity.Observed(Math.Round((s.Instant - held.HeldAt).Ticks / (decimal)TimeSpan.TicksPerMinute, 2), "minutes held");

    private static string HeldText(CompanySnapshot s, HeldOutcomeRecord held)
    {
        if (!held.HasHeldOutcomeRow)
        {
            return "recorded before the held-outcome record existed: its reason, escalation instant and escalation state are not "
                + $"recorded; the operation row records: {held.RecordedFailureReason}; not re-admitted";
        }

        var escalation = held.EscalatesAt is { } at
            ? s.Instant >= at
                ? $"escalation PAST: it escalated at {at:O}"
                : $"escalation not yet reached: it escalates at {at:O}"
            : "escalation instant not recorded";

        return $"reason {held.Reason}; {escalation}; escalates to the owner: {(held.EscalatesToOwner == true ? "yes" : "no")}; "
            + $"hold timeout {held.HoldTimeout?.ToString("c", CultureInfo.InvariantCulture)} (the request's recorded hold timeout); "
            + "not re-admitted";
    }

    /// <summary>The cost booked inside the week for one channel or the company, by the delivered cost rule.</summary>
    private static MeasurementQuantity WeekCost(CompanySnapshot s, ChannelId? channel)
    {
        var totals = channel is { } id
            ? s.WeekOperations.Channels.FirstOrDefault(c => c.Channel.Equals(id))?.Totals
            : s.WeekOperations.Company;
        var scope = channel is { } named ? $"channel {named} in week {s.Week}" : $"the company in week {s.Week}";

        return totals is null
            ? MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, $"no operation is recorded for {scope}")
            : AnalyticsComposers.FromOperations(totals.Operations, totals.UnstatedCosts, totals.Cost, $"no operation is recorded for {scope}", scope);
    }

    // -----------------------------------------------------------------------
    // The CTO weekly report (master plan section 33)
    // -----------------------------------------------------------------------

    public static IReadOnlyList<ReportLine> Cto(CompanySnapshot s)
    {
        ArgumentNullException.ThrowIfNull(s);

        var lines = new List<ReportLine>();
        var week = WeekTag(s);
        var benchmark = EconomicsComposers.Benchmark(s.Benchmark);

        ReportLine L(ReportLineKey key, string heading, string label, LineFigure figure, string restsOn, string finality, string? informs = null,
            string? text = null, DateTimeOffset? at = null) =>
            Line(ReportKind.Cto, key, heading, label, figure, restsOn, s, finality, informs, text, at);

        // Cost and quality per task, with the comparable-run count against the owner's ten.
        lines.Add(L(ReportLineKey.OwnersTen, "Cost and quality per task", "the owner's comparable-run threshold",
            LineFigure.Of(RecordedProgrammeFacts.ComparableRunsPerTask), "the owner's decision of 2026-10-09", RecordedTag()));

        // ONE COUNT OVER ONE CANDIDATE SET (correction cycle): per capability, per reasoning-tier class of its
        // available routes, the count selection uses over the class it compares, ComparableRuns.OfCandidates; a
        // report knows no request, so it states every class.
        foreach (var capability in s.Routes.Select(r => r.Capability).Distinct().OrderBy(c => c))
        {
            foreach (var tierClass in ComparableRuns.TierClasses(s.Routes, capability, s.Availability, s.AccountStatus))
            {
                var tier = TierClassName(tierClass[0].StatedReasoningTier);
                var routes = tierClass.Select(r => r.Id).ToArray();
                foreach (var task in Enum.GetValues<TaskClass>())
                {
                    var count = ComparableRuns.OfCandidates(tierClass, task, s.Benchmark.Observations);
                    var label = ComparableRuns.ReachesOwnersTen(count)
                        ? "at or above the owner's ten: the router may rank on evidence where its all-observed rule holds"
                        : "QUALITATIVE REVIEW: below the owner's ten, the router keeps the labelled configured ordering";
                    lines.Add(L(ReportLineKey.TaskComparableRuns, "Cost and quality per task",
                        $"{capability} / {tier} / {task}: comparable runs, least over the class's available provider routes ({label})",
                        LineFigure.Of(count),
                        "the benchmark record at or before the instant; ComparableRuns.OfCandidates, the count selection uses over the class it compares",
                        "as of the snapshot", "whether to authorise benchmark runs (owner)"));
                }
            }

            var capabilityRoutes = s.Routes.Where(r => r.Capability == capability).Select(r => r.Id).ToArray();
            foreach (var task in Enum.GetValues<TaskClass>())
            {
                foreach (var reading in benchmark.Readings.Where(r => capabilityRoutes.Contains(r.Route) && r.TaskClass == task))
                {
                    lines.Add(L(ReportLineKey.RouteQuality, "Cost and quality per task", $"{capability} / {task}: route {reading.Route} quality",
                        LineFigure.Of(reading.Quality), "the benchmark record; EconomicsComposers.Benchmark", "as of the snapshot"));
                    lines.Add(L(ReportLineKey.RouteCost, "Cost and quality per task", $"{capability} / {task}: route {reading.Route} cost",
                        LineFigure.Of(reading.Cost), "the benchmark record; EconomicsComposers.Benchmark", "as of the snapshot"));
                }
            }
        }

        // The controller's recorded decisions of the week, exactly as recorded.
        lines.Add(L(ReportLineKey.ControllerDecisions, "Controller decisions", "controller decisions booked inside the week",
            LineFigure.Of(MeasurementQuantity.Count(s.Decisions.Count, "decisions")),
            "the admission decision record, read inside the snapshot", week, "whether the ceiling or a budget needs changing (owner)"));
        lines.Add(L(ReportLineKey.ControllerActions, "Controller decisions",
            "controller downgrades, deferrals and refusals booked inside the week (the full list is in the CTO report)",
            LineFigure.Of(MeasurementQuantity.Count(s.Decisions.LongCount(d => d.Action >= ControllerAction.Downgrade), "controller actions")),
            "the admission decision record, read inside the snapshot", week, "whether the ceiling or a budget needs changing (owner)"));
        foreach (var decision in Severity(s.Decisions))
        {
            lines.Add(L(ReportLineKey.ControllerDecision, "Controller decisions",
                $"decision for operation {decision.Operation}, channel {decision.Channel}, booking month {decision.BookingMonth:yyyy-MM}: "
                + $"action {decision.Action}, selection basis {decision.Basis}{(decision.TaskClass is { } t ? $" for task class {t}" : string.Empty)}",
                LineFigure.Of(decision.ObservationsRankedOn),
                "the admission decision record, as recorded", week,
                text: $"selection statement as recorded: {decision.BasisStatement} | tier statement as recorded: {decision.TierStatement} "
                    + $"| reservation statement as recorded: {decision.ReservationStatement} | company basis statement as recorded "
                    + $"at that time: {decision.CompanyBasisStatement}",
                at: decision.DecidedAt));

            foreach (var reading in decision.Readings)
            {
                lines.Add(L(ReportLineKey.ControllerReading, "Controller decisions",
                    $"decision for operation {decision.Operation}: {reading.Scope} reading utilisation, amount {reading.Amount.Describe()}, "
                    + $"booked spend {reading.BookedSpend.Describe()}, threshold {(reading.Threshold is { } th ? ((int)th).ToString(CultureInfo.InvariantCulture) : "none reached")}, "
                    + $"action {reading.Action}{(reading.Reason is { } r ? $", reason {r}" : string.Empty)}",
                    LineFigure.Of(reading.Utilisation), "the admission decision readings, as recorded", week, at: decision.DecidedAt));
            }

            foreach (var candidate in decision.Candidates)
            {
                lines.Add(L(ReportLineKey.ControllerReading, "Controller decisions",
                    $"decision for operation {decision.Operation}: candidate route {candidate.Route}, configured rating "
                    + $"{candidate.ConfiguredRating.Value} (a rating, never an observed quality), observed quality "
                    + $"{candidate.ObservedQuality.Describe()}, observed cost {candidate.ObservedCost.Describe()}",
                    LineFigure.Of(candidate.Observations), "the admission decision candidates, as recorded", week, at: decision.DecidedAt));
            }
        }

        // The tier distribution of every touched month.
        foreach (var month in s.Months)
        {
            var distribution = EconomicsComposers.TierDistribution(month.Tiers);
            foreach (var part in distribution.Parts.Append(distribution.Total))
            {
                var name = ReferenceEquals(part, distribution.Total) ? "all tiers" : part.Tier?.ToString() ?? "no stated tier";
                lines.Add(L(ReportLineKey.TierOperations, "Tier distribution", $"{month.Closure.Month:yyyy-MM} {name}: operations",
                    LineFigure.Of(part.Operations), "the tier distribution; EconomicsComposers.TierDistribution", MonthTag(month.Closure)));
                lines.Add(L(ReportLineKey.TierCost, "Tier distribution", $"{month.Closure.Month:yyyy-MM} {name}: cost",
                    LineFigure.Of(part.Cost), "the tier distribution; EconomicsComposers.TierDistribution", MonthTag(month.Closure)));
            }
        }

        // The platform-policy re-verification status, for every week beginning before the threshold change.
        lines.AddRange(Reverification(s));

        return lines;
    }

    /// <summary>The name of a reasoning-tier class: the tier the routes state, or none.</summary>
    public static string TierClassName(ReasoningTier? stated) =>
        stated is { } tier ? $"routes stating {tier}" : "routes stating no tier";

    /// <summary>
    /// The re-verification line (decision D-007): for a week beginning before 2027-02-01, the statements re-checked
    /// and changed from the results recorded inside the week, unmeasured naming the missing record where no
    /// statement or result is recorded, and overdue unmeasured naming the missing cadence until the owner confirms one.
    /// </summary>
    public static IReadOnlyList<ReportLine> Reverification(CompanySnapshot s)
    {
        ArgumentNullException.ThrowIfNull(s);

        var thresholdChange = new DateTimeOffset(RecordedProgrammeFacts.ThresholdChangeDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        if (s.PeriodStart >= thresholdChange)
        {
            return [];
        }

        var week = WeekTag(s);
        var inWeek = s.Reverifications.Where(r => r.RecordedAt >= s.PeriodStart && r.RecordedAt < s.PeriodEnd).ToArray();
        var restsOn = "the platform-policy statement register and the re-verification result record, at or before the instant";

        // Unmeasured until at least one re-verification RESULT is recorded (Design Gate ruling on the re-verification
        // record): a registered statement with no result ever recorded is a reading nobody took, never an observed zero.
        MeasurementQuantity Counted(int count, string unit) => s.Reverifications.Count == 0
            ? MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                "no platform-policy re-verification result is recorded in the result record, so no re-verification count can be "
                + "stated, whether or not statements are registered; who records results is the owner's to direct")
            : MeasurementQuantity.Count(count, unit);

        var lines = new List<ReportLine>
        {
            Line(ReportKind.Cto, ReportLineKey.ReverificationRechecked, "Platform-policy re-verification", "statements re-checked in the week",
                LineFigure.Of(Counted(inWeek.Select(r => r.Statement).Distinct().Count(), "statements re-checked")), restsOn, s, week,
                "who records re-verification results and at what cadence (owner)"),
            Line(ReportKind.Cto, ReportLineKey.ReverificationChanged, "Platform-policy re-verification", "statements changed in the week",
                LineFigure.Of(Counted(inWeek.Where(r => r.Changed).Select(r => r.Statement).Distinct().Count(), "statements changed")), restsOn, s, week,
                "who records re-verification results and at what cadence (owner)"),
            Line(ReportKind.Cto, ReportLineKey.ReverificationOverdue, "Platform-policy re-verification", "statements overdue",
                LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne,
                    "no re-verification cadence is confirmed by the owner; the master plan's monthly wording is a recommendation, "
                    + "and no overdue statement is inferred from it")),
                restsOn, s, week, "who records re-verification results and at what cadence (owner)"),
        };

        foreach (var changed in inWeek.Where(r => r.Changed))
        {
            lines.Add(Line(ReportKind.Cto, ReportLineKey.ReverificationChanged, "Platform-policy re-verification",
                $"statement {changed.Statement} changed, verified {changed.VerifiedOn:yyyy-MM-dd}",
                LineFigure.Of(MeasurementQuantity.Count(1, "changed statement")), restsOn, s, week, recordedAt: changed.RecordedAt));
        }

        return lines;
    }

    // -----------------------------------------------------------------------
    // The CFO summary (master plan section 10), on the owner's decided ceiling
    // -----------------------------------------------------------------------

    public static IReadOnlyList<ReportLine> Cfo(CompanySnapshot s)
    {
        ArgumentNullException.ThrowIfNull(s);

        var lines = new List<ReportLine>();
        var spend = "whether to change the decided ceiling or the month's metered work (owner)";

        ReportLine L(ReportLineKey key, string heading, string label, LineFigure figure, string restsOn, string finality, string? informs = null) =>
            Line(ReportKind.Cfo, key, heading, label, figure, restsOn, s, finality, informs);

        lines.Add(L(ReportLineKey.Ceiling, "Company spend", "the owner's decided company ceiling",
            LineFigure.Of(RecordedProgrammeFacts.CompanyCeiling), "the owner's decision of 2026-10-09", RecordedTag(), spend));
        lines.Add(L(ReportLineKey.Envelope, "Company spend", "the approved monthly envelope",
            LineFigure.Of(RecordedProgrammeFacts.Envelope), "the owner-approved envelope", RecordedTag()));
        lines.Add(L(ReportLineKey.StandingCharge, "Company spend", "the standing charge, shown separately and never netted",
            LineFigure.Of(RecordedProgrammeFacts.StandingCharge), "the standing line of the owner-approved envelope", RecordedTag(),
            "where the standing charge is recorded (owner)"));

        foreach (var month in s.Months)
        {
            var tag = MonthTag(month.Closure);
            var ceiling = ChannelAnalyticsComposers.Budgets(month.AgainstCeiling);
            var envelope = ChannelAnalyticsComposers.Budgets(month.AgainstEnvelope);
            var name = month.Closure.Month.ToString("yyyy-MM", CultureInfo.InvariantCulture);

            lines.Add(L(ReportLineKey.CompanySpend, "Company spend", $"company booked metered spend, {name}",
                LineFigure.Of(ceiling.Company.CompanyCost), "the company ceiling reading; ChannelAnalyticsComposers.Budgets", tag, spend));
            lines.Add(L(ReportLineKey.CeilingUtilisation, "Company spend", $"utilisation of the owner's decided ceiling, {name}",
                LineFigure.Of(ceiling.Company.Utilisation), "the company ceiling reading against the decided ceiling; ChannelAnalyticsComposers.Budgets",
                tag, spend));
            lines.Add(L(ReportLineKey.EnvelopeUtilisation, "Company spend", $"utilisation of the approved envelope, {name}",
                LineFigure.Of(envelope.Company.Utilisation), "the company ceiling reading against the envelope; ChannelAnalyticsComposers.Budgets", tag));
            lines.Add(L(ReportLineKey.StandingCommitment, "Company spend", $"standing commitment incurred, {name}",
                LineFigure.Of(ceiling.Company.StandingCommitment), "the record store, searched for a standing commitment", tag,
                "where the standing charge is recorded (owner)"));

            foreach (var channel in ceiling.Channels)
            {
                lines.Add(L(ReportLineKey.ChannelSpend, "Channel spend", $"channel {channel.Channel}: booked spend, {name}",
                    LineFigure.Of(channel.Utilised), "the channel budget reading; ChannelAnalyticsComposers.Budgets", tag));
                lines.Add(L(ReportLineKey.ChannelBudget, "Channel spend", $"channel {channel.Channel}: budget amount, {name}",
                    LineFigure.Of(channel.BudgetAmount), "the budget register", tag, "what each channel's budget amount is (owner)"));
                lines.Add(L(ReportLineKey.ChannelUtilisation, "Channel spend", $"channel {channel.Channel}: utilisation of its budget, {name}",
                    LineFigure.Of(channel.Utilisation), "the channel budget reading; ChannelAnalyticsComposers.Budgets", tag,
                    "what each channel's budget amount is (owner)"));
                lines.Add(L(ReportLineKey.ChannelAlerts, "Channel spend", $"channel {channel.Channel}: budget alerts recorded, {name}",
                    LineFigure.Of(Alerts(channel)), "the budget alerts the delivered tracking raised, at or before the instant; "
                    + "ChannelAnalyticsComposers.Budgets", tag));
            }
        }

        // Revenue and every revenue-derived figure: lit only by an admitted observed parameter, never a zero amount.
        var visible = AnalyticsComposers.Visible(s.RevenueParameters);
        foreach (var figure in AnalyticsComposers.RevenueDerived)
        {
            var lit = visible.FirstOrDefault(v => v.Figure == figure);
            lines.Add(L(ReportLineKey.RevenueDerived, "Revenue", figure.ToString(),
                LineFigure.Of(lit?.Quantity ?? MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.NoObservationExists,
                    $"{figure} is not yet earnable: platform advertising is earnable only after partner-programme entry, and the revenue "
                    + "register holds no admitted observed parameter")),
                "the revenue parameter register; AnalyticsComposers.Visible", "as of the snapshot",
                figure == RevenueDerivedFigure.Revenue ? "whether the channel is on course for programme entry, the only route to advertising revenue (owner)" : null));
        }

        return lines;
    }

    /// <summary>
    /// The alerts recorded for one channel budget: the sum of the delivered per-threshold alert counts where every one
    /// is observed, and the first unmeasured one otherwise (no budget recorded, so no alert can be).
    /// </summary>
    private static MeasurementQuantity Alerts(ChannelBudgetReading channel)
    {
        var unmeasured = channel.Thresholds.Select(t => t.AlertsRecorded).OfType<MeasurementQuantity.Unmeasured>().FirstOrDefault();
        if (unmeasured is not null)
        {
            return unmeasured;
        }

        var sum = channel.Thresholds.Sum(t => t.AlertsRecorded is MeasurementQuantity.ObservedValue v ? v.Amount : 0m);
        return MeasurementQuantity.Count((long)sum, "alerts recorded");
    }

    // -----------------------------------------------------------------------
    // Channel performance (master plan sections 31 and 34)
    // -----------------------------------------------------------------------

    public static IReadOnlyList<ReportLine> ChannelPerformance(CompanySnapshot s)
    {
        ArgumentNullException.ThrowIfNull(s);

        var lines = new List<ReportLine>();
        var week = WeekTag(s);
        var thresholdInForce = s.DatastoreDate < RecordedProgrammeFacts.ThresholdChangeDate
            ? RecordedProgrammeFacts.WatchHoursBeforeChange
            : RecordedProgrammeFacts.WatchHoursFromChange;
        var days = RecordedProgrammeFacts.ThresholdChangeDate.DayNumber - s.DatastoreDate.DayNumber;

        ReportLine L(ReportLineKey key, string label, LineFigure figure, string restsOn, string finality, string? informs = null,
            string? text = null) =>
            Line(ReportKind.ChannelPerformance, key, "Channel performance", label, figure, restsOn, s, finality, informs, text);

        // Exactly the channels the register holds: none is created, configured or routed to here.
        foreach (var channel in s.Register)
        {
            var id = channel.Channel;
            foreach (var audience in new[] { "subscribers", "qualified watch hours", "qualified short-form views" })
            {
                lines.Add(L(ReportLineKey.Audience, $"channel {id}: {audience}",
                    LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists,
                        $"no source records the channel's {audience}: nothing is published and no platform analytics source is connected")),
                    "the record store, searched for an audience source", week, "whether the channel is on course for the programme threshold (owner)"));
            }

            lines.Add(L(ReportLineKey.ChannelWeekCost, $"channel {id}: cost booked in the week", LineFigure.Of(WeekCost(s, id)),
                "the operation record, booked inside the week; the delivered cost rule", week, "whether spend is on track (owner)"));

            var conditions = s.Conditions.FirstOrDefault(c => c.Channel.Equals(id))?.Register ?? FirstPublicationConditionRegister.Empty;
            lines.Add(L(ReportLineKey.Conditions, $"channel {id}: first-publication conditions not satisfied",
                LineFigure.Of(MeasurementQuantity.Count(conditions.Unsatisfied().Count, "of the three coded conditions not satisfied")),
                "the first-publication condition register, the payee read at company level", "as of the snapshot",
                "which owner obligations block first publication (owner)", ConditionsText(conditions)));

            lines.Add(L(ReportLineKey.ProgrammeThreshold, $"channel {id}: subscriber threshold of the partner programme",
                LineFigure.Of(RecordedProgrammeFacts.Subscribers), "the master plan's recorded programme thresholds", RecordedTag()));
            lines.Add(L(ReportLineKey.ProgrammeThreshold, $"channel {id}: watch-hour threshold in force on the datastore's date",
                LineFigure.Of(thresholdInForce), "the master plan's recorded programme thresholds", RecordedTag()));
            lines.Add(L(ReportLineKey.ProgrammeProgress, $"channel {id}: progress to the programme threshold",
                LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists,
                    "progress cannot be stated: the channel's subscribers and qualified watch hours are unmeasured")),
                "the audience lines above against the recorded thresholds", week, "whether the channel is on course for the programme threshold (owner)"));
            lines.Add(L(ReportLineKey.DaysToThresholdChange, $"channel {id}: days to {RecordedProgrammeFacts.ThresholdChangeDate:yyyy-MM-dd}",
                LineFigure.Of(MeasurementQuantity.Count(Math.Max(0, days), "days, counted on the datastore's date")),
                $"the datastore's date, {s.DatastoreDate:yyyy-MM-dd}, against the recorded threshold change date", "as of the snapshot",
                "whether the channel is on course for the programme threshold (owner)"));
        }

        return lines;
    }

    private static string ConditionsText(FirstPublicationConditionRegister register) =>
        string.Join(" ", register.Evaluate().Select(standing => standing.Describe()))
        + " The payee and payment account are company-level, shared by every channel; no condition is discharged by this report.";

    // -----------------------------------------------------------------------
    // The risk summary (master plan section 34)
    // -----------------------------------------------------------------------

    public static IReadOnlyList<ReportLine> Risk(CompanySnapshot s)
    {
        ArgumentNullException.ThrowIfNull(s);

        var lines = new List<ReportLine>();
        var week = WeekTag(s);

        ReportLine L(ReportLineKey key, string label, LineFigure figure, string restsOn, string finality, string? informs = null, string? text = null) =>
            Line(ReportKind.Risk, key, "Risk", label, figure, restsOn, s, finality, informs, text);

        lines.Add(L(ReportLineKey.CopyrightSendBacks, "copyright: send-backs from the rights check in the week",
            LineFigure.Of(ActionCount(s, PublicationGateService.SentBackFromRightsCheckAction, null, "rights-check send-backs")),
            "the append-only record's rights-check send-back entries inside the week", week, "which copyright risks need action (owner)"));
        lines.Add(L(ReportLineKey.CopyrightClaims, "copyright: claims and strikes",
            LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists,
                "no source records a claim, an incident or a strike: nothing is published and no platform source is connected")),
            "the record store, searched for a claim or strike record", week));
        lines.Add(L(ReportLineKey.PolicySendBacks, "policy: send-backs on policy grounds",
            LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne,
                "a send-back records no ground, so none is counted as on policy grounds; the recorded reasons are listed in the COO report")),
            "the append-only record's send-back entries", week));
        lines.Add(L(ReportLineKey.Monetisation, "monetisation: pre-threshold",
            LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists,
                "pre-threshold: advertising revenue is not yet earnable before partner-programme entry, and the channel's progress "
                + "to the threshold is unmeasured")),
            "the channel performance lines against the recorded programme thresholds", week, "whether the channel is on course for the programme threshold (owner)"));
        lines.Add(L(ReportLineKey.ProviderFailures, "technology: operations failed in the week",
            LineFigure.Of(MeasurementQuantity.Count(s.FailedOperations.Sum(f => f.Count), "operations failed")),
            "the operation record, outcome failed, booked inside the week", week, "which technology failures need action (owner)"));
        lines.Add(L(ReportLineKey.HeldWork, "technology: held work, none re-admitted",
            LineFigure.Of(MeasurementQuantity.Count(s.Held.Count, "held operations")),
            "the operation record and the held-outcome record", "as of the snapshot", "whether deferred requests are re-admitted (owner)"));

        var next = RecordedProgrammeFacts.DatedChanges
            .OfType<RecordedAmount.RecordedFigure>()
            .FirstOrDefault(change => change.Date is { } date && date >= s.DatastoreDate);
        if (next is { Date: { } nextDate })
        {
            lines.Add(L(ReportLineKey.NextDatedChange, "dated: the next recorded platform change", LineFigure.Of(next),
                "the master plan's recorded dated changes, the first on or after the datastore's date", RecordedTag(),
                "which dated obligation to prepare for (owner)"));
            lines.Add(L(ReportLineKey.DaysToNextDatedChange, "dated: days to the next recorded platform change",
                LineFigure.Of(MeasurementQuantity.Count(nextDate.DayNumber - s.DatastoreDate.DayNumber, "days, counted on the datastore's date")),
                $"the datastore's date, {s.DatastoreDate:yyyy-MM-dd}, against the recorded date", "as of the snapshot",
                "which dated obligation to prepare for (owner)"));
        }
        else
        {
            lines.Add(L(ReportLineKey.NextDatedChange, "dated: the next recorded platform change",
                LineFigure.Of(RecordedAmount.Missing("no recorded dated change falls on or after the datastore's date")),
                "the master plan's recorded dated changes", RecordedTag()));
        }

        foreach (var channel in s.Register)
        {
            var conditions = s.Conditions.FirstOrDefault(c => c.Channel.Equals(channel.Channel))?.Register ?? FirstPublicationConditionRegister.Empty;
            lines.Add(L(ReportLineKey.Conditions, $"channel {channel.Channel}: coded first-publication conditions not satisfied",
                LineFigure.Of(MeasurementQuantity.Count(conditions.Unsatisfied().Count, "of the three coded conditions not satisfied")),
                "the first-publication condition register, the payee read at company level", "as of the snapshot",
                "which owner obligations block first publication (owner)", ConditionsText(conditions)));
        }

        return lines;
    }
}
