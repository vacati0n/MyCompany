using System.Globalization;
using System.Text;
using MediaCompany.Application.Ports;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Management;

namespace MediaCompany.Deterministic.Analytics;

/// <summary>
/// The ONE rendering of every management surface (the AI-management change, decision D-003 of its design): a
/// report line, a rule outcome and a register entry each render one way, and the reports, the brief and the
/// dashboard all render through here, so a tile cannot render a line differently from its report.
///
/// A line renders as its label, its figure in the figure's own delivered rendering, its case word, the reading
/// and composer it rests on, the report instant and its finality. An unmeasured figure prints no amount, unit or
/// zero; an observed zero prints the zero with its unit and the word observed; a recorded amount names where it
/// was recorded and is never called observed. Recorded text is printed verbatim and labelled as recorded.
/// </summary>
public static class ManagementRendering
{
    public static string Render(ReportLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var text = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"{line.Label}: {line.Figure.Describe()} [{LineFigure.CaseWord(line.Figure.Case)}]")
            .Append(CultureInfo.InvariantCulture, $" | rests on: {line.RestsOn}")
            .Append(CultureInfo.InvariantCulture, $" | as of {line.Instant:O}, {line.Finality}");

        if (line.RecordedText is { } recorded)
        {
            text.Append(CultureInfo.InvariantCulture, $" | recorded text, as recorded{(line.RecordedAt is { } at ? $" at {at:O}" : string.Empty)}: \"{recorded}\"");
        }

        if (line.Informs is { } informs)
        {
            text.Append(CultureInfo.InvariantCulture, $" | informs: {informs}");
        }

        return text.ToString();
    }

    public static string Render(RuleOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        return outcome switch
        {
            RuleOutcome.Issued issued => RenderIssued(issued),
            RuleOutcome.Abstained abstained =>
                $"[rule abstains] {abstained.Rule}: {abstained.Reason} | reading: {abstained.Reading} | as of {abstained.Instant:O}"
                + $" | informs: {abstained.Informs}",
            _ => throw new InvalidOperationException("Unreachable: a rule outcome has two shapes."),
        };
    }

    private static string RenderIssued(RuleOutcome.Issued issued)
    {
        var text = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"[rule output, not a judgement] {issued.Rule}: {issued.Statement}")
            .Append(CultureInfo.InvariantCulture, $" | reading: {issued.Reading} = {issued.Figure.Describe()} [{LineFigure.CaseWord(issued.Figure.Case)}]")
            .Append(CultureInfo.InvariantCulture, $" | anchor: {(issued.Anchor is { } anchor ? anchor.Describe() + "; " : string.Empty)}{issued.AnchorStatement}")
            .Append(CultureInfo.InvariantCulture, $" | as of {issued.Instant:O} | informs: {issued.Informs}");

        if (issued.Cto is { } cto)
        {
            text.Append(CultureInfo.InvariantCulture, $" | current approach: {cto.CurrentApproach}")
                .Append(CultureInfo.InvariantCulture, $" | problem: {cto.Problem}")
                .Append(CultureInfo.InvariantCulture, $" | alternative: {cto.Alternative}")
                .Append(CultureInfo.InvariantCulture, $" | monthly cost difference: {cto.MonthlyCostDifference.Describe()}")
                .Append(CultureInfo.InvariantCulture, $" | capacity difference: {cto.CapacityDifference.Describe()}")
                .Append(CultureInfo.InvariantCulture, $" | quality impact: {cto.QualityImpact.Describe()}")
                .Append(CultureInfo.InvariantCulture, $" | implementation cost: {cto.ImplementationCost.Describe()}")
                .Append(CultureInfo.InvariantCulture, $" | operational risk: {cto.OperationalRisk}")
                .Append(CultureInfo.InvariantCulture, $" | recommendation: {cto.Recommendation}")
                .Append(CultureInfo.InvariantCulture, $" | CEO decision required: {cto.DecisionRequired}");
        }

        return text.ToString();
    }

    public static string Render(RegisterEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var standing = entry.Status == RegisterEntryStatus.Decided
            ? $"decided by {entry.DecidedBy} on {entry.DecidedOn:yyyy-MM-dd}"
            : "open";
        var supersedes = entry.Supersedes is { } replaced ? $", superseding {replaced}" : string.Empty;
        var interim = entry.InterimRuling is { } ruling ? $" | interim ruling standing: {ruling}" : string.Empty;

        return $"{entry.Identifier} [{entry.Kind}, {standing}{supersedes}] owner {entry.Owner}, recorded {entry.RecordedOn:yyyy-MM-dd}, "
            + $"entered {entry.EnteredAt:O}: \"{entry.Statement}\"{interim}";
    }

    public static string Render(BriefItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item switch
        {
            BriefItem.LineItem line => Render(line.Line),
            BriefItem.RuleItem rule => Render(rule.Outcome),
            BriefItem.DecisionItem decision => Render(decision.Entry),
            _ => throw new InvalidOperationException("Unreachable: a brief item has three shapes."),
        };
    }

    /// <summary>The header every surface prints: the one instant, the stored horizon, the period and its finality.</summary>
    public static IReadOnlyList<string> Header(DateTimeOffset instant, DateTimeOffset horizon, string week, DateTimeOffset start, DateTimeOffset end, string finality) =>
    [
        $"Report instant, the datastore's clock: {instant:O}",
        $"Record horizon, as stored: {horizon:O}",
        $"Period: the UTC week {week}, from {start:O} inclusive to {end:O} exclusive (Monday 00:00 UTC is 07:00 Monday in Vietnam)",
        $"Finality: {finality}",
        "Read in one read-only snapshot; nothing here approves, sends back, configures, records, publishes or calls a model.",
    ];
}

/// <summary>
/// The CEO brief (the AI-management change, decision D-011 of its design), a member of the zero-AI-cost set under
/// the name <see cref="DeterministicTaskRegistry.BriefComposition"/>: seven sections in the order company,
/// channel performance, production, risk, CTO recommendations, COO recommendations, decisions required. Each item
/// is a report line or a rule outcome of the same read, naming the decision it informs; a line informing none is
/// not shown; the decisions-required section is exactly the open register entries of the same read.
/// </summary>
public static class BriefComposer
{
    public const string TaskName = DeterministicTaskRegistry.BriefComposition;

    public static CeoBrief Compose(ManagementReportSet reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        // THE BOUND (correction cycle): the brief's size does not grow with the rows behind it. Of the held requests
        // only the most urgent is shown beside the counts of held operations and escalated deferrals; of each rule's
        // outcomes at most the first output and the first abstention are shown, each rule ordering its outcomes by
        // urgency or severity, and a further count names how many more stand in the report holding the full list. The
        // bound is a presentation choice, not a business quantity.
        var firstHeld = reports.Lines.FirstOrDefault(l => l.Key == ReportLineKey.HeldRequest);

        IReadOnlyList<BriefItem> Lines(Func<ReportLine, bool> where) =>
            reports.Lines
                .Where(l => l.Informs is not null && where(l))
                .Where(l => l.Key != ReportLineKey.HeldRequest || ReferenceEquals(l, firstHeld))
                .Select(l => (BriefItem)new BriefItem.LineItem(l))
                .ToArray();

        IReadOnlyList<BriefItem> Rules(params RecommendationRule[] rules)
        {
            var items = new List<BriefItem>();
            foreach (var rule in rules)
            {
                var outcomes = reports.Outcomes.Where(o => o.Rule == rule).ToArray();
                var shown = new[]
                    {
                        outcomes.FirstOrDefault(o => o is RuleOutcome.Issued),
                        outcomes.FirstOrDefault(o => o is RuleOutcome.Abstained),
                    }
                    .Where(o => o is not null)
                    .Cast<RuleOutcome>()
                    .OrderBy(o => Array.IndexOf(outcomes, o))
                    .ToArray();
                items.AddRange(shown.Select(o => (BriefItem)new BriefItem.RuleItem(o)));

                if (outcomes.Length > shown.Length)
                {
                    items.Add(new BriefItem.LineItem(NotShown(reports, rule, outcomes.Length - shown.Length)));
                }
            }

            return items;
        }

        var company = Lines(l => l.Report == ReportKind.Cfo || (l.Report == ReportKind.Coo && l.Key == ReportLineKey.ItemsPublished))
            .Concat(Rules(RecommendationRule.CfoCompanyCeiling, RecommendationRule.CfoUnbudgetedChannel))
            .ToArray();

        var production = Lines(l => l.Report == ReportKind.Coo && l.Key != ReportLineKey.ItemsPublished);

        var decisions = reports.Register
            .Where(e => RuleCatalogue.IsOpen(e, reports.Register))
            .Select(e => (BriefItem)new BriefItem.DecisionItem(e))
            .ToArray();

        return new CeoBrief
        {
            Instant = reports.Instant,
            StoredHorizon = reports.StoredHorizon,
            PeriodStart = reports.PeriodStart,
            PeriodEnd = reports.PeriodEnd,
            Week = reports.Week,
            Final = reports.Final,
            FinalityStatement = reports.FinalityStatement,
            Sections =
            [
                new BriefSectionReading { Section = BriefSection.Company, Items = company },
                new BriefSectionReading { Section = BriefSection.ChannelPerformance, Items = Lines(l => l.Report == ReportKind.ChannelPerformance) },
                new BriefSectionReading { Section = BriefSection.Production, Items = production },
                new BriefSectionReading { Section = BriefSection.Risk, Items = Lines(l => l.Report == ReportKind.Risk) },
                new BriefSectionReading
                {
                    Section = BriefSection.CtoRecommendations,
                    Items = Lines(l => l.Report == ReportKind.Cto && l.Key == ReportLineKey.ControllerActions)
                        .Concat(Rules(RecommendationRule.CtoQualitativeReview, RecommendationRule.CtoControllerAction, RecommendationRule.CtoReverification))
                        .ToArray(),
                },
                new BriefSectionReading
                {
                    Section = BriefSection.CooRecommendations,
                    Items = Rules(RecommendationRule.CooEscalatedDeferral, RecommendationRule.CooAwaitingApproval),
                },
                new BriefSectionReading { Section = BriefSection.DecisionsRequired, Items = decisions },
            ],
        };
    }

    /// <summary>The brief's count of a rule's further outcomes, naming the report that holds them all.</summary>
    private static ReportLine NotShown(ManagementReportSet reports, RecommendationRule rule, int further)
    {
        var (report, name) = rule switch
        {
            RecommendationRule.CfoCompanyCeiling or RecommendationRule.CfoUnbudgetedChannel => (ReportKind.Cfo, "CFO"),
            RecommendationRule.CooEscalatedDeferral or RecommendationRule.CooAwaitingApproval => (ReportKind.Coo, "COO"),
            _ => (ReportKind.Cto, "CTO"),
        };

        return new ReportLine
        {
            Report = report,
            Key = ReportLineKey.RuleOutputsNotShown,
            Heading = "Brief",
            Label = $"{rule}: further outcomes not shown in the brief (the full list is in the {name} report)",
            Figure = LineFigure.Of(MeasurementQuantity.Count(further, "further rule outcomes")),
            RestsOn = "the rule catalogue's outcomes over the same read",
            Instant = reports.Instant,
            Finality = "as of the snapshot",
            Informs = reports.Outcomes.First(o => o.Rule == rule).Informs,
        };
    }
}

/// <summary>
/// The read-only dashboard (the AI-management change, decision D-012 of its design), a member of the zero-AI-cost
/// set under the name <see cref="DeterministicTaskRegistry.DashboardComposition"/>: one tile per brief item, each
/// tile being that item's own rendering from the same composed read, with its instant and case. It offers no
/// action, writes nothing and opens nothing; it shows composed lines only, never a connection string, a handle or
/// a credential.
/// </summary>
public static class DashboardComposer
{
    public const string TaskName = DeterministicTaskRegistry.DashboardComposition;

    public static IReadOnlyList<DashboardTile> Tiles(CeoBrief brief)
    {
        ArgumentNullException.ThrowIfNull(brief);

        return brief.Sections
            .SelectMany(section => section.Items.Select(item => new DashboardTile
            {
                Section = section.Section,
                Rendering = ManagementRendering.Render(item),
                Instant = brief.Instant,
                Case = item switch
                {
                    BriefItem.LineItem line => line.Line.Figure.Case,
                    BriefItem.RuleItem { Outcome: RuleOutcome.Issued issued } => issued.Figure.Case,
                    _ => null,
                },
            }))
            .ToArray();
    }
}

/// <summary>What one read composed: the report set, the brief and the dashboard tiles, all from one snapshot.</summary>
public sealed record ManagementRead(ManagementReportSet Reports, CeoBrief Brief, IReadOnlyList<DashboardTile> Tiles);

/// <summary>
/// The management surface's one entry (the AI-management change): ONE read of the company's records through the
/// company record reader, composed into the reports, the brief and the dashboard tiles. It takes the reader port
/// and nothing else: no clock, no configuration, no writer, no capability, so it can do nothing but read once and
/// compose. Nothing here is scheduled or sent anywhere; it runs when the operator asks.
/// </summary>
public sealed class ManagementReportService
{
    private readonly ICompanyRecordReader _records;

    public ManagementReportService(ICompanyRecordReader records) => _records = records;

    public async Task<ManagementRead> ReadAsync(ReportWeek? week, CancellationToken cancellationToken)
    {
        var snapshot = await _records.ReadAsync(week, cancellationToken).ConfigureAwait(false);
        return Compose(snapshot);
    }

    /// <summary>The pure composition of one snapshot.</summary>
    public static ManagementRead Compose(CompanySnapshot snapshot)
    {
        var reports = ManagementComposers.Compose(snapshot);
        var brief = BriefComposer.Compose(reports);
        return new ManagementRead(reports, brief, DashboardComposer.Tiles(brief));
    }
}
