using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Routing;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Management;
using MediaCompany.Domain.Registry;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The AI-management capability as pure functions over one snapshot: the comparable-run count and the owner's
/// ten in selection and in the CTO label, the four-case rendering, the five reports, the closed rule catalogue,
/// the brief and the dashboard.
///
/// Every snapshot below is a DEMONSTRATION FIXTURE built in memory: its spend, budgets, decisions, held requests,
/// register entries, routes and observations exist only in the test that builds them and are none of them an
/// observation of the company's work.
/// </summary>
public sealed class ManagementCompositionTests
{
    // -----------------------------------------------------------------------
    // The owner's ten: one count for the router and the CTO report
    // -----------------------------------------------------------------------

    private static BenchmarkObservation Run(RouteId route, decimal quality = 90m, decimal cost = 0.01m, TaskClass task = TaskClass.ScriptPass) => new()
    {
        Id = BenchmarkObservationId.New(),
        Entry = CorpusEntryId.New(),
        TaskClass = task,
        Route = route,
        Model = Fixture.PrimaryModel,
        Operation = OperationId.New(),
        Quality = MeasurementQuantity.Observed(quality, BenchmarkObservation.QualityRatingUnit),
        Cost = MeasurementQuantity.Observed(cost, "USD"),
        Latency = MeasurementQuantity.Observed(1_000m, BenchmarkObservation.LatencyUnit),
        ObservedAt = Fixture.Now.AddDays(-1),
        Period = Fixture.Today,
    };

    private static BenchmarkObservation[] Runs(RouteId route, int count, decimal quality = 90m, decimal cost = 0.01m) =>
        Enumerable.Range(0, count).Select(_ => Run(route, quality, cost)).ToArray();

    /// <summary>
    /// At NINE comparable runs of any candidate the selection is the configured ordering, labelled configured,
    /// stating every candidate's count against the owner's ten; at TEN for every candidate it ranks on evidence and
    /// states the count. No evidence ranking occurs below ten.
    /// </summary>
    [Fact]
    public void SelectionKeepsTheConfiguredOrderingBelowTheOwnersTenAndRanksOnEvidenceAtTen()
    {
        var primary = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var secondary = Fixture.Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 85);

        // Ten for the primary and nine for the secondary: configured, both counts stated.
        var nine = Runs(primary.Id, 10, cost: 0.02m).Concat(Runs(secondary.Id, 9, cost: 0.01m)).ToArray();
        var configured = EvidenceSelection.Select([primary, secondary], TaskClass.ScriptPass, new QualityRating(80), "USD", nine);
        Assert.Equal(SelectionBasis.Configured, configured.Record.Basis);
        Assert.Equal(primary.Id, configured.Selected!.Id);
        Assert.Contains($"route {secondary.Id} 9 comparable runs against the owner's recorded 10 comparable runs per task", configured.Record.Statement, StringComparison.Ordinal);
        Assert.Contains($"route {primary.Id} 10 comparable runs against the owner's", configured.Record.Statement, StringComparison.Ordinal);
        Assert.Contains("fewer comparable runs than the owner's ten", configured.Record.Statement, StringComparison.Ordinal);
        Assert.Empty(configured.Record.ObservationsRankedOn);

        // Ten for both: evidence, the cheaper secondary taken, the count stated.
        var ten = nine.Append(Run(secondary.Id, cost: 0.01m)).ToArray();
        var evidence = EvidenceSelection.Select([primary, secondary], TaskClass.ScriptPass, new QualityRating(80), "USD", ten);
        Assert.Equal(SelectionBasis.Evidence, evidence.Record.Basis);
        Assert.Equal(secondary.Id, evidence.Selected!.Id);
        Assert.Contains("every candidate holds at least the owner's ten comparable runs", evidence.Record.Statement, StringComparison.Ordinal);
        Assert.Contains($"route {secondary.Id} 10 comparable runs", evidence.Record.Statement, StringComparison.Ordinal);

        // The route register is never written: the candidates are the routes passed in, unchanged.
        Assert.Equal(new QualityRating(85), secondary.RatedQuality);
    }

    /// <summary>
    /// A comparable run is an observation whose quality is observed in the rating unit and whose cost is stated; an
    /// unmeasured quality, a quality in another unit or an unstated cost is not one; a pair counts the least over
    /// its routes, and a pair with no route counts an observed zero.
    /// </summary>
    [Fact]
    public void AComparableRunIsObservedInQualityAndStatedInCostAndAPairCountsItsLeastRoute()
    {
        var route = RouteId.New();
        var other = RouteId.New();
        var gap = MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne, "fixture: not stated");
        var evidence = new[]
        {
            Run(route),
            Run(route) with { Quality = MeasurementQuantity.Zero(BenchmarkObservation.QualityRatingUnit) },
            Run(route) with { Quality = gap },
            Run(route) with { Cost = gap },
            Run(route) with { Quality = MeasurementQuantity.Observed(4m, "stars") },
            Run(other),
        };

        Assert.Equal("2 comparable runs", ComparableRuns.Of(route, TaskClass.ScriptPass, evidence).Describe());
        Assert.Equal("1 comparable runs", ComparableRuns.LeastOver([route, other], TaskClass.ScriptPass, evidence).Describe());
        Assert.IsType<MeasurementQuantity.ObservedZero>(ComparableRuns.LeastOver([], TaskClass.ScriptPass, evidence));
        Assert.IsType<MeasurementQuantity.ObservedZero>(ComparableRuns.Of(route, TaskClass.FactCheck, evidence));
        Assert.False(ComparableRuns.ReachesOwnersTen(MeasurementQuantity.Count(9, ComparableRuns.Unit)));
        Assert.True(ComparableRuns.ReachesOwnersTen(MeasurementQuantity.Count(10, ComparableRuns.Unit)));
    }

    /// <summary>
    /// The CTO report labels a task pair qualitative below the owner's ten and never at ten, with the SAME count the
    /// router uses, and the qualitative-review rule fires below ten and is silent at ten.
    /// </summary>
    [Fact]
    public void TheCtoLabelAndTheRouterAgreeAtNineAndAtTen()
    {
        var route = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var others = Enum.GetValues<TaskClass>().Where(t => t != TaskClass.ScriptPass)
            .SelectMany(t => Enumerable.Range(0, 10).Select(_ => Run(route.Id, task: t))).ToArray();

        foreach (var (runs, qualitative) in new[] { (9, true), (10, false) })
        {
            var observations = Runs(route.Id, runs).Concat(others).ToArray();
            var reports = ManagementComposers.Compose(SnapshotFixture.Build(routes: [route], observations: observations));
            var line = Assert.Single(reports.Lines, l => l.Key == ReportLineKey.TaskComparableRuns && l.Label.Contains($"/ {TaskClass.ScriptPass}:", StringComparison.Ordinal));

            var routerCount = ComparableRuns.Of(route.Id, TaskClass.ScriptPass, observations);
            Assert.Equal(routerCount, Assert.IsType<LineFigure.Measured>(line.Figure).Quantity);
            Assert.Equal(qualitative, line.Label.Contains("QUALITATIVE REVIEW", StringComparison.Ordinal));
            Assert.Equal(qualitative, reports.Outcomes.Any(o => o.Rule == RecommendationRule.CtoQualitativeReview));
        }
    }

    // -----------------------------------------------------------------------
    // The four cases, rendered apart on every surface
    // -----------------------------------------------------------------------

    /// <summary>
    /// An observed value, an observed zero, an unmeasured reading and a recorded amount render apart: the
    /// unmeasured rendering carries no amount, unit or zero, the observed zero carries its unit and the word
    /// observed, and a recorded amount names where it was recorded and is never called observed.
    /// </summary>
    [Fact]
    public void TheFourCasesRenderApartAndAnUnmeasuredLineCarriesNoAmount()
    {
        var reports = ManagementComposers.Compose(SnapshotFixture.Build(operations: 0, awaiting: []));

        var zero = Assert.Single(reports.Lines, l => l.Key == ReportLineKey.AwaitingApproval);
        var unmeasured = Assert.Single(reports.Lines, l => l.Report == ReportKind.Cfo && l.Key == ReportLineKey.CeilingUtilisation);
        var recorded = Assert.Single(reports.Lines, l => l.Key == ReportLineKey.Ceiling);
        var notRecorded = Assert.Single(reports.Lines, l => l.Key == ReportLineKey.ItemsPlanned);

        Assert.Equal(FigureCase.ObservedZero, zero.Figure.Case);
        Assert.Equal(FigureCase.Unmeasured, unmeasured.Figure.Case);
        Assert.Equal(FigureCase.Recorded, recorded.Figure.Case);
        Assert.Equal(FigureCase.NotRecorded, notRecorded.Figure.Case);

        var zeroText = ManagementRendering.Render(zero);
        var unmeasuredText = ManagementRendering.Render(unmeasured);
        var recordedText = ManagementRendering.Render(recorded);

        Assert.Contains("observed zero item versions awaiting owner approval [observed zero]", zeroText, StringComparison.Ordinal);
        Assert.Contains("[unmeasured]", unmeasuredText, StringComparison.Ordinal);
        Assert.DoesNotContain("observed zero", unmeasuredText, StringComparison.Ordinal);
        Assert.DoesNotContain("percent", unmeasuredText.Split(" | ")[0], StringComparison.Ordinal);
        Assert.StartsWith(
            $"{unmeasured.Label}: {MeasurementQuantity.UnmeasuredPrefix(UnmeasuredReason.NoObservationExists)}",
            unmeasuredText,
            StringComparison.Ordinal);
        Assert.Contains("recorded 34.42 USD in the owner's decision of 2026-10-09", recordedText, StringComparison.Ordinal);
        Assert.Contains("[recorded]", recordedText, StringComparison.Ordinal);
        Assert.DoesNotContain("[observed]", recordedText, StringComparison.Ordinal);
        Assert.NotEqual(zeroText.Split(": ", 2)[1].Split(" | ")[0], unmeasuredText.Split(": ", 2)[1].Split(" | ")[0]);
    }

    /// <summary>A recorded figure that is not money is positive, names its unit and source, and renders as recorded.</summary>
    [Fact]
    public void ARecordedFigureIsPositiveNamesItsSourceAndRendersAsRecorded()
    {
        Assert.Equal(
            "recorded 10 comparable runs per task in " + RecordedProgrammeFacts.ComparableRunsRecordedIn,
            RecordedProgrammeFacts.ComparableRunsPerTask.Describe());
        Assert.StartsWith("recorded the doubling of the entry thresholds for a new entrant 2027-02-01 in MASTER-PLAN.md",
            RecordedProgrammeFacts.DatedChanges[2].Describe(), StringComparison.Ordinal);
        Assert.Throws<ArgumentOutOfRangeException>(() => RecordedAmount.Figure(0m, "runs", "a fixture source"));
        Assert.Throws<ArgumentException>(() => RecordedAmount.Figure(10m, "runs", " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => RecordedProgrammeFacts.ControllerThreshold(60));

        // Each dated change names the master plan section that states it (correction cycle).
        Assert.Contains("MASTER-PLAN.md section 3.3", RecordedProgrammeFacts.DatedChanges[0].Describe(), StringComparison.Ordinal);
        Assert.Contains("2026-09-24", RecordedProgrammeFacts.DatedChanges[0].Describe(), StringComparison.Ordinal);
        Assert.Contains("sections 2 and 5", RecordedProgrammeFacts.DatedChanges[1].Describe(), StringComparison.Ordinal);
    }

    /// <summary>
    /// ONE COUNT OVER ONE CANDIDATE SET (correction cycle). A capability holds a provider route with ten comparable
    /// runs and, in the same reasoning-tier class, a non-AI substitute and a disabled provider route that hold none,
    /// and in another tier class a provider route with three: the label of the first class is not qualitative and
    /// the router ranks a request of that class on evidence; the label of the other class is qualitative and the
    /// router keeps the configured ordering for it. The excluded routes change neither.
    /// </summary>
    [Fact]
    public void TheLabelAndTheRouterCountTheSameCandidatesWhereAClassHoldsExcludedRoutes()
    {
        Route Provider(RouteTier tier, ProviderAccountId account, ModelId model, ReasoningTier stated) =>
            new(RouteId.New(), CapabilityClass.EditorialReasoning, tier, new RouteTarget.ProviderRoute(account, model),
                new QualityRating(90), new ContextCapacity(100_000), "commercial terms, paid tier", Fixture.Today.AddDays(-10), stated);

        var counted = Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, ReasoningTier.Standard);
        var substitute = new Route(RouteId.New(), CapabilityClass.EditorialReasoning, RouteTier.Secondary,
            new RouteTarget.NonAiSubstitute("metadata-template-population"), new QualityRating(80), new ContextCapacity(int.MaxValue),
            "non-AI substitute", Fixture.Today.AddDays(-10), ReasoningTier.Standard);
        var disabled = Provider(RouteTier.Emergency, Fixture.EmergencyAccount, Fixture.EmergencyModel, ReasoningTier.Standard);
        var otherTier = Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, ReasoningTier.Deep);
        Route[] routes = [counted, substitute, disabled, otherTier];

        var observations = Enum.GetValues<TaskClass>()
            .SelectMany(task => Enumerable.Range(0, 10).Select(_ => Run(counted.Id, task: task)))
            .Concat(Enumerable.Range(0, 3).Select(_ => Run(otherTier.Id)))
            .ToArray();
        var accounts = new Dictionary<ProviderAccountId, ProviderAccountStatus>(Fixture.AllActive())
        {
            [Fixture.EmergencyAccount] = ProviderAccountStatus.Disabled,
        };

        var snapshot = SnapshotFixture.Build(routes: routes, observations: observations) with { AccountStatus = accounts };
        var reports = ManagementComposers.Compose(snapshot);

        var standard = Assert.Single(reports.Lines, l => l.Key == ReportLineKey.TaskComparableRuns
            && l.Label.Contains("routes stating Standard / ScriptPass:", StringComparison.Ordinal));
        var deep = Assert.Single(reports.Lines, l => l.Key == ReportLineKey.TaskComparableRuns
            && l.Label.Contains("routes stating Deep / ScriptPass:", StringComparison.Ordinal));
        Assert.Equal("10 comparable runs", standard.Figure.Describe());
        Assert.DoesNotContain("QUALITATIVE", standard.Label, StringComparison.Ordinal);
        Assert.Equal("3 comparable runs", deep.Figure.Describe());
        Assert.Contains("QUALITATIVE", deep.Label, StringComparison.Ordinal);

        // The router, over the same inputs: a Standard request ranks on evidence with the same count.
        var inputs = Fixture.Inputs(routes, accounts: accounts) with { Evidence = observations };
        var standardRecord = RouteResolver.ResolveWithRecord(Fixture.Request(floor: 60) with { TaskClass = TaskClass.ScriptPass }, inputs);
        Assert.Equal(SelectionBasis.Evidence, standardRecord.Selection.Basis);
        Assert.Equal(counted.Id, Assert.IsType<CapabilityResolution.Resolved>(standardRecord.Resolution).Route.Id);
        Assert.Equal(
            Assert.IsType<LineFigure.Measured>(standard.Figure).Quantity,
            ComparableRuns.OfCandidates([counted, substitute], TaskClass.ScriptPass, observations));

        // A Deep request compares the Deep class and keeps the configured ordering, as the label says.
        var deepRequest = new CapabilityRequest(
            CapabilityClass.EditorialReasoning, ReasoningTier.Deep, new QualityRating(60), new ContextCapacity(8_000), new Money(1.00m),
            Criticality.Routine, Fixture.Attribution(), new EstimatedUnits(1_000, 500, 0), TimeSpan.FromHours(4), ReducedFloorPolicy.Forbidden)
        {
            TaskClass = TaskClass.ScriptPass,
        };
        var deepRecord = RouteResolver.ResolveWithRecord(deepRequest, inputs);
        Assert.Equal(SelectionBasis.Configured, deepRecord.Selection.Basis);
        Assert.Contains("fewer comparable runs than the owner's ten", deepRecord.Selection.Statement, StringComparison.Ordinal);

        // The qualitative-review rule names the Deep class and not the Standard one.
        var rule = Assert.IsType<RuleOutcome.Issued>(Assert.Single(reports.Outcomes, o => o.Rule == RecommendationRule.CtoQualitativeReview));
        Assert.Contains("routes stating Deep / ScriptPass", rule.Statement, StringComparison.Ordinal);
        Assert.DoesNotContain("routes stating Standard / ScriptPass", rule.Statement, StringComparison.Ordinal);
    }

    /// <summary>
    /// A ROUTE REACHING NO PROVIDER KEEPS ITS EXACT CONFIGURED POSITION (the tech lead's ruling, second correction
    /// cycle). A hold-and-escalate route configured ahead of every provider of its class is still selected first when
    /// every provider holds ten comparable runs; evidence swaps the two providers only within the positions providers
    /// hold; below ten the configured ordering applies unchanged.
    /// </summary>
    [Fact]
    public void ANonProviderRouteKeepsItsConfiguredPositionAndEvidenceReordersOnlyProviderPositions()
    {
        var hold = Fixture.HoldAndEscalate(RouteTier.Primary);
        var dearer = Fixture.Provider(RouteTier.Secondary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var cheaper = Fixture.Provider(RouteTier.Emergency, Fixture.SecondaryAccount, Fixture.SecondaryModel, 85);
        var ten = Runs(dearer.Id, 10, cost: 0.02m).Concat(Runs(cheaper.Id, 10, cost: 0.01m)).ToArray();

        var outcome = EvidenceSelection.Select([hold, dearer, cheaper], TaskClass.ScriptPass, new QualityRating(80), "USD", ten);
        Assert.Equal(SelectionBasis.Evidence, outcome.Record.Basis);
        Assert.Equal(hold.Id, outcome.Selected!.Id);
        Assert.Equal(new[] { hold.Id, cheaper.Id, dearer.Id }, outcome.Record.Candidates.Select(c => c.Route));

        // The non-provider route between the providers keeps its middle position; the providers swap around it.
        var middle = Fixture.HoldAndEscalate(RouteTier.Secondary);
        var first = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var last = Fixture.Provider(RouteTier.Emergency, Fixture.SecondaryAccount, Fixture.SecondaryModel, 85);
        var swapped = EvidenceSelection.Select([first, middle, last], TaskClass.ScriptPass, new QualityRating(80), "USD",
            Runs(first.Id, 10, cost: 0.02m).Concat(Runs(last.Id, 10, cost: 0.01m)).ToArray());
        Assert.Equal(new[] { last.Id, middle.Id, first.Id }, swapped.Record.Candidates.Select(c => c.Route));
        Assert.Equal(last.Id, swapped.Selected!.Id);

        // Below ten: the configured ordering exactly.
        var nine = Runs(dearer.Id, 10, cost: 0.02m).Concat(Runs(cheaper.Id, 9, cost: 0.01m)).ToArray();
        var configured = EvidenceSelection.Select([hold, dearer, cheaper], TaskClass.ScriptPass, new QualityRating(80), "USD", nine);
        Assert.Equal(SelectionBasis.Configured, configured.Record.Basis);
        Assert.Equal(hold.Id, configured.Selected!.Id);
        Assert.Equal(new[] { hold.Id, dearer.Id, cheaper.Id }, configured.Record.Candidates.Select(c => c.Route));
    }

    /// <summary>
    /// THE BRIEF IS BOUNDED (correction cycle): held requests, escalated deferrals and controller actions appear as
    /// counts in their measurement case with the single most urgent item per kind, and the brief's size is the same
    /// with two of each as with fifty; every held request and decision stays listed in the reports.
    /// </summary>
    [Fact]
    public void TheBriefDoesNotGrowWithHeldRequestsOrControllerActions()
    {
        CeoBrief Brief(int rows, out ManagementReportSet reports)
        {
            var held = Enumerable.Range(0, rows).Select(n => new HeldOutcomeRecord
            {
                Operation = OperationId.New(),
                Channel = SnapshotFixture.ChannelOne,
                Item = ItemId.New(),
                HeldAt = SnapshotFixture.Instant.AddHours(-10),
                Reason = RefusalReason.DeferredAtThreshold,
                EscalatesAt = SnapshotFixture.Instant.AddHours(-9).AddMinutes(n),
                HoldTimeout = TimeSpan.FromHours(1),
                EscalatesToOwner = false,
                RecordedFailureReason = "fixture",
            }).ToArray();
            var decisions = Enumerable.Range(0, rows).Select(n => new RecordedControllerDecision
            {
                Operation = OperationId.New(),
                Channel = SnapshotFixture.ChannelOne,
                DecidedAt = SnapshotFixture.Instant.AddMinutes(-n - 1),
                BookingMonth = SnapshotFixture.October,
                Action = n == rows - 1 ? ControllerAction.Refuse : ControllerAction.Defer,
                Basis = SelectionBasis.Configured,
                BasisStatement = "fixture",
                TierStatement = "fixture",
                CompanyBasisStatement = "fixture",
                ReservationStatement = "fixture",
                ObservationsRankedOn = MeasurementQuantity.Count(0, "observations ranked on"),
                Readings =
                [
                    new RecordedDecisionReading
                    {
                        Scope = GoverningScope.Channel,
                        Amount = RecordedAmount.Of(new Money(1m), "the budget register"),
                        BookedSpend = MeasurementQuantity.Observed(0.95m, "USD"),
                        Utilisation = MeasurementQuantity.Observed(95m, "percent"),
                        Threshold = BudgetThreshold.Ninety,
                        Action = n == rows - 1 ? ControllerAction.Refuse : ControllerAction.Defer,
                    },
                ],
                Candidates = [],
            }).ToArray();

            reports = ManagementComposers.Compose(SnapshotFixture.Build(held: held, decisions: decisions));
            return BriefComposer.Compose(reports);
        }

        var two = Brief(2, out var twoReports);
        var fifty = Brief(50, out var fiftyReports);

        Assert.Equal(two.Sections.Sum(s => s.Items.Count), fifty.Sections.Sum(s => s.Items.Count));
        Assert.Equal(50, fiftyReports.Lines.Count(l => l.Key == ReportLineKey.HeldRequest));
        Assert.Equal(50, fiftyReports.Outcomes.Count(o => o.Rule == RecommendationRule.CooEscalatedDeferral));

        var items = fifty.Sections.SelectMany(s => s.Items).ToArray();
        var lines = items.OfType<BriefItem.LineItem>().Select(i => i.Line).ToArray();
        Assert.Equal("50 held operations", Assert.Single(lines, l => l.Key == ReportLineKey.HeldOperations).Figure.Describe());
        Assert.Equal("50 escalated deferrals", Assert.Single(lines, l => l.Key == ReportLineKey.EscalatedDeferrals).Figure.Describe());
        Assert.Single(lines, l => l.Key == ReportLineKey.HeldRequest);
        Assert.Contains("49 further rule outcomes", Assert.Single(lines, l => l.Key == ReportLineKey.RuleOutputsNotShown
            && l.Label.StartsWith(nameof(RecommendationRule.CooEscalatedDeferral), StringComparison.Ordinal)).Figure.Describe(), StringComparison.Ordinal);

        // The one shown of each kind is the most urgent: the earliest escalation and the most severe action.
        var shownDeferral = Assert.IsType<RuleOutcome.Issued>(Assert.Single(items.OfType<BriefItem.RuleItem>(),
            i => i.Outcome.Rule == RecommendationRule.CooEscalatedDeferral).Outcome);
        Assert.Equal(fiftyReports.Outcomes.First(o => o.Rule == RecommendationRule.CooEscalatedDeferral), shownDeferral);
        var shownAction = Assert.IsType<RuleOutcome.Issued>(Assert.Single(items.OfType<BriefItem.RuleItem>(),
            i => i.Outcome.Rule == RecommendationRule.CtoControllerAction).Outcome);
        Assert.Contains("refuses metered work", shownAction.Statement, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // One instant, finality read and never made
    // -----------------------------------------------------------------------

    /// <summary>
    /// Every line, every rule outcome and every tile carries the snapshot's one instant; a week whose stored horizon
    /// is before its end reads not final naming the horizon and what makes it final, and one at or after reads final.
    /// </summary>
    [Fact]
    public void EveryLineCarriesTheOneInstantAndFinalityIsReadFromTheStoredHorizon()
    {
        var notFinal = ManagementReportService.Compose(SnapshotFixture.Build(operations: 2, cost: 10m, awaiting: [Awaiting()]));
        Assert.All(notFinal.Reports.Lines, l => Assert.Equal(SnapshotFixture.Instant, l.Instant));
        Assert.All(notFinal.Reports.Outcomes, o => Assert.Equal(SnapshotFixture.Instant, o.Instant));
        Assert.All(notFinal.Tiles, t => Assert.Equal(SnapshotFixture.Instant, t.Instant));
        Assert.False(notFinal.Reports.Final);
        Assert.Contains("week 2026-W41 is NOT FINAL", notFinal.Reports.FinalityStatement, StringComparison.Ordinal);
        Assert.Contains("measurable-now report command", notFinal.Reports.FinalityStatement, StringComparison.Ordinal);
        Assert.Contains(SnapshotFixture.Instant.AddHours(-1).ToString("O"), notFinal.Reports.FinalityStatement, StringComparison.Ordinal);

        var final = ManagementComposers.Compose(SnapshotFixture.Build(horizon: SnapshotFixture.WeekEnd.AddSeconds(1)));
        Assert.True(final.Final);
        Assert.Contains("week 2026-W41 is final", final.FinalityStatement, StringComparison.Ordinal);
    }

    private static decimal Amount(ReportLine line) =>
        Assert.IsType<MeasurementQuantity.ObservedValue>(Assert.IsType<LineFigure.Measured>(line.Figure).Quantity).Amount;

    private static AwaitingApproval Awaiting() =>
        new(SnapshotFixture.ChannelOne, ItemId.New(), new ItemVersion(1), SnapshotFixture.Instant.AddDays(-1));

    // -----------------------------------------------------------------------
    // The reports
    // -----------------------------------------------------------------------

    /// <summary>
    /// The COO report: planned items not recorded, the committed rate never shown as planned, buffer depth
    /// unmeasured, a rate unmeasured over no denominator and observed over one, every recorded send-back reason
    /// listed verbatim, and every held request with its hold age and escalation state, none re-admitted.
    /// </summary>
    [Fact]
    public void TheCooReportStatesEachLineInItsCaseAndListsEveryHeldRequest()
    {
        var past = new HeldOutcomeRecord
        {
            Operation = OperationId.New(),
            Channel = SnapshotFixture.ChannelOne,
            Item = ItemId.New(),
            HeldAt = SnapshotFixture.Instant.AddHours(-6),
            Reason = RefusalReason.DeferredAtThreshold,
            EscalatesAt = SnapshotFixture.Instant.AddHours(-2),
            HoldTimeout = TimeSpan.FromHours(4),
            EscalatesToOwner = false,
            RecordedFailureReason = "held at floor 60: DeferredAtThreshold",
        };
        var old = new HeldOutcomeRecord
        {
            Operation = OperationId.New(),
            Channel = SnapshotFixture.ChannelOne,
            Item = ItemId.New(),
            HeldAt = SnapshotFixture.Instant.AddDays(-2),
            RecordedFailureReason = "held at floor 60: MeteredAdmissionInProgress - recorded before the held-outcome record",
        };

        var snapshot = SnapshotFixture.Build(held: [past, old]) with
        {
            AuditActions =
            [
                new AuditActionCount(PublicationGateService.PresentedAction, SnapshotFixture.ChannelOne, 4),
                new AuditActionCount(PublicationGateService.OwnerSentBackAction, SnapshotFixture.ChannelOne, 1),
            ],
            SendBacks = [new RecordedSendBack(PublicationGateService.OwnerSentBackAction, SnapshotFixture.ChannelOne, "fixture reason: the hook is weak", SnapshotFixture.Instant.AddHours(-3))],
        };
        var lines = ManagementComposers.Coo(snapshot);

        Assert.Equal(FigureCase.NotRecorded, Assert.Single(lines, l => l.Key == ReportLineKey.ItemsPlanned).Figure.Case);
        Assert.Contains("committed rate", Assert.Single(lines, l => l.Key == ReportLineKey.ItemsPlanned).Figure.Describe(), StringComparison.Ordinal);
        Assert.Equal(FigureCase.Unmeasured, Assert.Single(lines, l => l.Key == ReportLineKey.BufferDepth).Figure.Case);
        Assert.Equal(25m, Amount(Assert.Single(lines, l => l.Key == ReportLineKey.OwnerSendBackRate)));
        Assert.Equal(FigureCase.Unmeasured, Assert.Single(lines, l => l.Key == ReportLineKey.RightsCheckSendBackRate).Figure.Case);
        Assert.Equal("fixture reason: the hook is weak", Assert.Single(lines, l => l.Key == ReportLineKey.SendBackReason).RecordedText);

        var heldLines = lines.Where(l => l.Key == ReportLineKey.HeldRequest).ToArray();
        Assert.Equal(2, heldLines.Length);
        var pastLine = Assert.Single(heldLines, l => l.Label.Contains(past.Operation.ToString(), StringComparison.Ordinal));
        Assert.Equal("360 minutes held", pastLine.Figure.Describe());
        Assert.Contains("reason DeferredAtThreshold; escalation PAST", pastLine.RecordedText, StringComparison.Ordinal);
        Assert.Contains("not re-admitted", pastLine.RecordedText, StringComparison.Ordinal);
        var oldLine = Assert.Single(heldLines, l => l.Label.Contains(old.Operation.ToString(), StringComparison.Ordinal));
        Assert.Contains("recorded before the held-outcome record existed", oldLine.RecordedText, StringComparison.Ordinal);
        Assert.Equal("2 held operations", Assert.Single(lines, l => l.Key == ReportLineKey.HeldOperations).Figure.Describe());
    }

    /// <summary>
    /// The CFO summary: spend against the owner's decided ceiling and the envelope, the standing charge recorded
    /// separately and never netted, an unbudgeted channel's utilisation unmeasured naming the missing amount and a
    /// budgeted one in its case, every revenue-derived figure unmeasured and never a zero amount, and no output
    /// mentioning the allotment calls it interim.
    /// </summary>
    [Fact]
    public void TheCfoSummaryReadsTheDecidedCeilingAndNeverCallsItInterim()
    {
        var unbudgeted = ManagementComposers.Cfo(SnapshotFixture.Build(operations: 3, cost: 17.21m));
        Assert.Equal(50m, Amount(Assert.Single(unbudgeted, l => l.Key == ReportLineKey.CeilingUtilisation)));
        Assert.Equal(FigureCase.Recorded, Assert.Single(unbudgeted, l => l.Key == ReportLineKey.StandingCharge).Figure.Case);
        Assert.Contains("42.99 USD", Assert.Single(unbudgeted, l => l.Key == ReportLineKey.StandingCharge).Figure.Describe(), StringComparison.Ordinal);
        Assert.Equal(FigureCase.Unmeasured, Assert.Single(unbudgeted, l => l.Key == ReportLineKey.StandingCommitment).Figure.Case);
        Assert.Equal(FigureCase.NotRecorded, Assert.Single(unbudgeted, l => l.Key == ReportLineKey.ChannelBudget).Figure.Case);
        Assert.Contains("no budget is recorded", Assert.Single(unbudgeted, l => l.Key == ReportLineKey.ChannelUtilisation).Figure.Describe(), StringComparison.Ordinal);
        Assert.All(unbudgeted.Where(l => l.Key == ReportLineKey.RevenueDerived), l =>
        {
            Assert.Equal(FigureCase.Unmeasured, l.Figure.Case);
            Assert.Contains("not yet earnable", l.Figure.Describe(), StringComparison.Ordinal);
        });

        var budgeted = ManagementComposers.Cfo(SnapshotFixture.Build(operations: 3, cost: 17.21m, channelBudget: 20m));
        Assert.Equal(86.05m, Amount(Assert.Single(budgeted, l => l.Key == ReportLineKey.ChannelUtilisation)));

        var all = ManagementReportService.Compose(SnapshotFixture.Build(operations: 3, cost: 17.21m));
        var texts = all.Reports.Lines.Select(ManagementRendering.Render)
            .Concat(all.Reports.Outcomes.Select(ManagementRendering.Render))
            .Concat(all.Tiles.Select(t => t.Rendering));
        Assert.All(texts.Where(t => t.Contains("34.42", StringComparison.Ordinal)), t =>
            Assert.DoesNotContain("interim", t, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Channel performance: exactly one line set per channel the register holds and no other channel, audience
    /// unmeasured, progress unmeasured against the recorded thresholds, and the days to 2027-02-01 counted on the
    /// datastore's date. The risk summary names the next recorded dated change, counted the same way.
    /// </summary>
    [Fact]
    public void ChannelPerformanceAndRiskCountOnTheDatastoresDate()
    {
        var snapshot = SnapshotFixture.Build();
        var channel = ManagementComposers.ChannelPerformance(snapshot);

        Assert.All(channel, l => Assert.Contains(SnapshotFixture.ChannelOne.ToString(), l.Label, StringComparison.Ordinal));
        Assert.All(channel.Where(l => l.Key == ReportLineKey.Audience), l => Assert.Equal(FigureCase.Unmeasured, l.Figure.Case));
        Assert.Equal(FigureCase.Unmeasured, Assert.Single(channel, l => l.Key == ReportLineKey.ProgrammeProgress).Figure.Case);
        Assert.All(channel.Where(l => l.Key == ReportLineKey.ProgrammeThreshold), l => Assert.Equal(FigureCase.Recorded, l.Figure.Case));
        Assert.Equal("116 days, counted on the datastore's date", Assert.Single(channel, l => l.Key == ReportLineKey.DaysToThresholdChange).Figure.Describe());
        Assert.Equal("3 of the three coded conditions not satisfied", Assert.Single(channel, l => l.Key == ReportLineKey.Conditions).Figure.Describe());

        var risk = ManagementComposers.Risk(snapshot);
        Assert.Contains("2027-01-31", Assert.Single(risk, l => l.Key == ReportLineKey.NextDatedChange).Figure.Describe(), StringComparison.Ordinal);
        Assert.Equal("115 days, counted on the datastore's date", Assert.Single(risk, l => l.Key == ReportLineKey.DaysToNextDatedChange).Figure.Describe());
        Assert.Contains("NO observation is recorded", Assert.Single(risk, l => l.Key == ReportLineKey.Conditions).RecordedText, StringComparison.Ordinal);

        // A process clock is read nowhere: a snapshot whose datastore date is a day later counts one day fewer.
        var nextDay = ManagementComposers.ChannelPerformance(snapshot with { DatastoreDate = snapshot.DatastoreDate.AddDays(1) });
        Assert.Equal("115 days, counted on the datastore's date", Assert.Single(nextDay, l => l.Key == ReportLineKey.DaysToThresholdChange).Figure.Describe());
    }

    /// <summary>
    /// The CTO re-verification line: unmeasured naming the missing record where no statement is recorded, counted
    /// where one is, overdue unmeasured naming the missing cadence either way, and absent for a week beginning on or
    /// after 2027-02-01.
    /// </summary>
    [Fact]
    public void TheReverificationLineIsUnmeasuredUntilARecordAndACadenceExist()
    {
        var none = ManagementComposers.Reverification(SnapshotFixture.Build());
        Assert.Contains("no platform-policy re-verification result is recorded", Assert.Single(none, l => l.Key == ReportLineKey.ReverificationRechecked).Figure.Describe(), StringComparison.Ordinal);
        Assert.Contains("no re-verification cadence is confirmed by the owner", Assert.Single(none, l => l.Key == ReportLineKey.ReverificationOverdue).Figure.Describe(), StringComparison.Ordinal);

        var statement = new PolicyStatementRecord
        {
            Identifier = "fixture-statement",
            Platform = "video-platform",
            Label = "a fixture statement",
            SourceReference = "a fixture source",
            RegisteredAt = SnapshotFixture.Instant.AddDays(-30),
        };
        var result = new ReverificationResult
        {
            Id = Guid.NewGuid(),
            Statement = "fixture-statement",
            VerifiedOn = new DateOnly(2026, 10, 6),
            Changed = true,
            RecordedAt = SnapshotFixture.Instant.AddDays(-1),
        };

        var recorded = ManagementComposers.Reverification(SnapshotFixture.Build(statements: [statement], results: [result]));
        Assert.Equal("1 statements re-checked", Assert.Single(recorded, l => l.Key == ReportLineKey.ReverificationRechecked).Figure.Describe());
        Assert.Equal(FigureCase.Unmeasured, Assert.Single(recorded, l => l.Key == ReportLineKey.ReverificationOverdue).Figure.Case);

        // A registered statement with NO result ever recorded is a reading nobody took: unmeasured, never an observed
        // zero (Design Gate ruling on the re-verification record, correction cycle).
        var unchecked_ = ManagementComposers.Reverification(SnapshotFixture.Build(statements: [statement]));
        var neverRechecked = Assert.Single(unchecked_, l => l.Key == ReportLineKey.ReverificationRechecked);
        Assert.Equal(FigureCase.Unmeasured, neverRechecked.Figure.Case);
        Assert.Contains("no platform-policy re-verification result is recorded", neverRechecked.Figure.Describe(), StringComparison.Ordinal);

        // With a result recorded in an earlier week, a week with none re-checked is an observed zero.
        var earlier = result with { RecordedAt = SnapshotFixture.WeekStart.AddDays(-3) };
        var quietWeek = ManagementComposers.Reverification(SnapshotFixture.Build(statements: [statement], results: [earlier]));
        Assert.Equal(FigureCase.ObservedZero, Assert.Single(quietWeek, l => l.Key == ReportLineKey.ReverificationRechecked).Figure.Case);

        var after = SnapshotFixture.Build() with
        {
            PeriodStart = new DateTimeOffset(2027, 2, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2027, 2, 8, 0, 0, 0, TimeSpan.Zero),
        };
        Assert.Empty(ManagementComposers.Reverification(after));
    }

    // -----------------------------------------------------------------------
    // The closed rule catalogue
    // -----------------------------------------------------------------------

    /// <summary>
    /// CFO rule 1 fires at a reached threshold of the decided ceiling naming the controller action and the recorded
    /// anchor, is silent below 50 percent, and ABSTAINS over an unmeasured utilisation, naming the reading.
    /// </summary>
    [Fact]
    public void TheCompanyCeilingRuleFiresAtAThresholdAndAbstainsOverAnUnmeasuredReading()
    {
        var at90 = ManagementComposers.Compose(SnapshotFixture.Build(operations: 2, cost: 31m)).Outcomes;
        var issued = Assert.IsType<RuleOutcome.Issued>(Assert.Single(at90, o => o.Rule == RecommendationRule.CfoCompanyCeiling));
        Assert.Equal(FigureCase.Observed, issued.Figure.Case);
        Assert.Equal("recorded 90 percent of the governing amount in " + RecordedProgrammeFacts.ControllerThresholdsRecordedIn, issued.Anchor!.Describe());
        Assert.Contains("defers metered work", issued.Statement, StringComparison.Ordinal);

        var below = ManagementComposers.Compose(SnapshotFixture.Build(operations: 2, cost: 10m)).Outcomes;
        Assert.DoesNotContain(below, o => o.Rule == RecommendationRule.CfoCompanyCeiling);

        var unmeasured = ManagementComposers.Compose(SnapshotFixture.Build(operations: 0)).Outcomes;
        var abstained = Assert.IsType<RuleOutcome.Abstained>(Assert.Single(unmeasured, o => o.Rule == RecommendationRule.CfoCompanyCeiling));
        Assert.Contains("utilisation of the owner's decided ceiling", abstained.Reading, StringComparison.Ordinal);
        Assert.Contains("the reading is unmeasured, so the rule abstains", abstained.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// CFO rule 2 fires for a channel whose budget amount is not recorded, keyed on the open register entry and its
    /// interim ruling; it abstains once that entry is superseded, and is silent for a budgeted channel.
    /// </summary>
    [Fact]
    public void TheUnbudgetedChannelRuleKeysOnTheOpenRegisterEntry()
    {
        var open = ManagementComposers.Compose(SnapshotFixture.Build()).Outcomes;
        var issued = Assert.IsType<RuleOutcome.Issued>(Assert.Single(open, o => o.Rule == RecommendationRule.CfoUnbudgetedChannel));
        Assert.Equal(FigureCase.NotRecorded, issued.Figure.Case);
        Assert.Contains("REG-004", issued.AnchorStatement, StringComparison.Ordinal);
        Assert.Null(issued.Anchor);

        // A re-worded question recorded as a superseding OPEN entry keeps the lineage open: the rule cites the
        // current entry, never the transcribed identifier (correction cycle).
        var reworded = SnapshotFixture.Register()
            .Append(SnapshotFixture.Entry("REG-098", RegisterEntryStatus.Open, "fixture: the question re-worded", "still refused", supersedes: "REG-004"))
            .ToArray();
        var current = Assert.IsType<RuleOutcome.Issued>(Assert.Single(
            ManagementComposers.Compose(SnapshotFixture.Build(register: reworded)).Outcomes, o => o.Rule == RecommendationRule.CfoUnbudgetedChannel));
        Assert.Contains("REG-098", current.AnchorStatement, StringComparison.Ordinal);
        Assert.Contains("still refused", current.AnchorStatement, StringComparison.Ordinal);

        // A decided entry at the end of the lineage closes it, and the rule abstains naming it.
        var superseded = reworded
            .Append(SnapshotFixture.Entry("REG-099", RegisterEntryStatus.Decided, "fixture: an answer superseding REG-098", supersedes: "REG-098"))
            .ToArray();
        var abstained = Assert.IsType<RuleOutcome.Abstained>(Assert.Single(
            ManagementComposers.Compose(SnapshotFixture.Build(register: superseded)).Outcomes, o => o.Rule == RecommendationRule.CfoUnbudgetedChannel));
        Assert.Contains("REG-099", abstained.Reason, StringComparison.Ordinal);

        var budgeted = ManagementComposers.Compose(SnapshotFixture.Build(channelBudget: 5m)).Outcomes;
        Assert.DoesNotContain(budgeted, o => o.Rule == RecommendationRule.CfoUnbudgetedChannel);
    }

    /// <summary>
    /// CTO rule 2 fires on a recorded decision at defer, keyed on its fixed threshold, with the ten CTO fields; it
    /// abstains over a decision whose deciding reading is unmeasured.
    /// </summary>
    [Fact]
    public void TheControllerActionRuleKeysOnTheRecordedThresholdAndAbstainsOverAnUnmeasuredReading()
    {
        RecordedControllerDecision Decision(MeasurementQuantity utilisation, BudgetThreshold? threshold, ControllerAction action, RefusalReason? reason) => new()
        {
            Operation = OperationId.New(),
            Channel = SnapshotFixture.ChannelOne,
            DecidedAt = SnapshotFixture.Instant.AddHours(-1),
            BookingMonth = SnapshotFixture.October,
            Action = action,
            Basis = SelectionBasis.Configured,
            BasisStatement = "fixture: configured",
            TierStatement = "fixture: no route served",
            CompanyBasisStatement = "fixture: as recorded",
            ReservationStatement = "fixture: held",
            ObservationsRankedOn = MeasurementQuantity.Count(0, "observations ranked on"),
            Readings =
            [
                new RecordedDecisionReading
                {
                    Scope = GoverningScope.Channel,
                    Amount = RecordedAmount.Of(new Money(1m), "the budget register"),
                    BookedSpend = MeasurementQuantity.Observed(0.92m, "USD"),
                    Utilisation = utilisation,
                    Threshold = threshold,
                    Action = action,
                    Reason = reason,
                },
            ],
            Candidates = [],
        };

        var deferred = Decision(MeasurementQuantity.Observed(92m, "percent"), BudgetThreshold.Ninety, ControllerAction.Defer, RefusalReason.DeferredAtThreshold);
        var outcome = Assert.IsType<RuleOutcome.Issued>(Assert.Single(
            ManagementComposers.Compose(SnapshotFixture.Build(decisions: [deferred])).Outcomes, o => o.Rule == RecommendationRule.CtoControllerAction));
        Assert.Equal("recorded 90 percent of the governing amount in " + RecordedProgrammeFacts.ControllerThresholdsRecordedIn, outcome.Anchor!.Describe());
        Assert.NotNull(outcome.Cto);
        Assert.IsType<MeasurementQuantity.Unmeasured>(outcome.Cto!.MonthlyCostDifference);
        Assert.Contains("CEO decision required", ManagementRendering.Render(outcome), StringComparison.Ordinal);

        var unmeasured = Decision(
            MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne, "fixture: spend not stated"), null, ControllerAction.Refuse, RefusalReason.SpendUnmeasured);
        Assert.IsType<RuleOutcome.Abstained>(Assert.Single(
            ManagementComposers.Compose(SnapshotFixture.Build(decisions: [unmeasured])).Outcomes, o => o.Rule == RecommendationRule.CtoControllerAction));
    }

    /// <summary>CTO rule 3 abstains while no statement is recorded, and fires for a recorded statement with no result this week.</summary>
    [Fact]
    public void TheReverificationRuleAbstainsWithoutARecordAndFiresForAnUncheckedStatement()
    {
        Assert.IsType<RuleOutcome.Abstained>(Assert.Single(
            ManagementComposers.Compose(SnapshotFixture.Build()).Outcomes, o => o.Rule == RecommendationRule.CtoReverification));

        var statement = new PolicyStatementRecord
        {
            Identifier = "fixture-statement",
            Platform = "video-platform",
            Label = "a fixture statement",
            SourceReference = "a fixture source",
            RegisteredAt = SnapshotFixture.Instant.AddDays(-30),
        };
        // A statement with no result ever recorded: the reading is unmeasured, so the rule still abstains.
        Assert.IsType<RuleOutcome.Abstained>(Assert.Single(
            ManagementComposers.Compose(SnapshotFixture.Build(statements: [statement])).Outcomes, o => o.Rule == RecommendationRule.CtoReverification));

        var earlier = new ReverificationResult
        {
            Id = Guid.NewGuid(),
            Statement = "fixture-statement",
            VerifiedOn = new DateOnly(2026, 9, 30),
            Changed = false,
            RecordedAt = SnapshotFixture.WeekStart.AddDays(-3),
        };
        var issued = Assert.IsType<RuleOutcome.Issued>(Assert.Single(
            ManagementComposers.Compose(SnapshotFixture.Build(statements: [statement], results: [earlier])).Outcomes,
            o => o.Rule == RecommendationRule.CtoReverification));
        Assert.Contains("never that the statement is overdue", issued.AnchorStatement, StringComparison.Ordinal);
    }

    /// <summary>
    /// COO rule 1 fires for a deferral past its own recorded escalation instant, keyed on the request's recorded hold
    /// timeout and the open re-admission entry; it is silent before the instant and abstains where no held-outcome row
    /// records the instant. COO rule 2 fires on an observed non-zero awaiting-approval count and is silent at zero.
    /// </summary>
    [Fact]
    public void TheCooRulesKeyOnRecordedEscalationAndOnMeasurementCase()
    {
        HeldOutcomeRecord Held(DateTimeOffset escalates, bool row = true) => new()
        {
            Operation = OperationId.New(),
            Channel = SnapshotFixture.ChannelOne,
            Item = ItemId.New(),
            HeldAt = SnapshotFixture.Instant.AddHours(-5),
            Reason = row ? RefusalReason.MeteredAdmissionInProgress : null,
            EscalatesAt = row ? escalates : null,
            HoldTimeout = row ? TimeSpan.FromHours(4) : null,
            EscalatesToOwner = row ? false : null,
            RecordedFailureReason = "fixture",
        };

        var past = ManagementComposers.Compose(SnapshotFixture.Build(held: [Held(SnapshotFixture.Instant.AddHours(-1))])).Outcomes;
        var issued = Assert.IsType<RuleOutcome.Issued>(Assert.Single(past, o => o.Rule == RecommendationRule.CooEscalatedDeferral));
        Assert.Equal("recorded 14400 seconds, the request's recorded hold timeout in the held-outcome record of operation", issued.Anchor!.Describe()[..^37]);
        Assert.Contains("REG-008", issued.AnchorStatement, StringComparison.Ordinal);
        Assert.Contains("not an owner threshold", issued.AnchorStatement, StringComparison.Ordinal);

        var future = ManagementComposers.Compose(SnapshotFixture.Build(held: [Held(SnapshotFixture.Instant.AddHours(1))])).Outcomes;
        Assert.DoesNotContain(future, o => o.Rule == RecommendationRule.CooEscalatedDeferral);

        var unrecorded = ManagementComposers.Compose(SnapshotFixture.Build(held: [Held(default, row: false)])).Outcomes;
        Assert.IsType<RuleOutcome.Abstained>(Assert.Single(unrecorded, o => o.Rule == RecommendationRule.CooEscalatedDeferral));

        var awaiting = ManagementComposers.Compose(SnapshotFixture.Build(awaiting: [Awaiting(), Awaiting()])).Outcomes;
        var approval = Assert.IsType<RuleOutcome.Issued>(Assert.Single(awaiting, o => o.Rule == RecommendationRule.CooAwaitingApproval));
        Assert.Null(approval.Anchor);
        Assert.Contains("rests on measurement case alone", approval.AnchorStatement, StringComparison.Ordinal);
        Assert.DoesNotContain(ManagementComposers.Compose(SnapshotFixture.Build()).Outcomes, o => o.Rule == RecommendationRule.CooAwaitingApproval);
    }

    /// <summary>Two evaluations over one read yield identical outcomes in identical, catalogue order.</summary>
    [Fact]
    public void TwoEvaluationsOverOneReadAreIdenticalAndInCatalogueOrder()
    {
        var snapshot = SnapshotFixture.Build(operations: 2, cost: 31m, awaiting: [Awaiting()]);
        var first = ManagementComposers.Compose(snapshot);
        var second = ManagementComposers.Compose(snapshot);

        Assert.Equal(first.Outcomes.Select(ManagementRendering.Render), second.Outcomes.Select(ManagementRendering.Render));
        Assert.Equal(first.Outcomes.Select(o => (int)o.Rule).Order(), first.Outcomes.Select(o => (int)o.Rule));
        Assert.Equal(RuleCatalogue.Evaluate(first, snapshot).Select(ManagementRendering.Render), first.Outcomes.Select(ManagementRendering.Render));
    }

    // -----------------------------------------------------------------------
    // The brief and the dashboard
    // -----------------------------------------------------------------------

    /// <summary>
    /// The brief carries its seven sections in order; every item names the decision it informs; the decisions-required
    /// section is exactly the open register entries of the same read — decided and superseded entries excluded.
    /// </summary>
    [Fact]
    public void TheBriefCarriesSevenSectionsInOrderAndExactlyTheOpenEntries()
    {
        var register = SnapshotFixture.Register()
            .Append(SnapshotFixture.Entry("REG-020", RegisterEntryStatus.Open, "fixture: a question later superseded"))
            .Append(SnapshotFixture.Entry("REG-021", RegisterEntryStatus.Open, "fixture: its replacement", supersedes: "REG-020"))
            .ToArray();
        var read = ManagementReportService.Compose(SnapshotFixture.Build(operations: 2, cost: 31m, awaiting: [Awaiting()], register: register));

        Assert.Equal(Enum.GetValues<BriefSection>(), read.Brief.Sections.Select(s => s.Section));
        Assert.All(read.Brief.Sections.SelectMany(s => s.Items), item =>
        {
            switch (item)
            {
                case BriefItem.LineItem line:
                    Assert.False(string.IsNullOrWhiteSpace(line.Line.Informs));
                    break;
                case BriefItem.RuleItem rule:
                    Assert.False(string.IsNullOrWhiteSpace(rule.Outcome.Informs));
                    break;
            }
        });

        var decisions = read.Brief.Sections.Single(s => s.Section == BriefSection.DecisionsRequired).Items
            .Cast<BriefItem.DecisionItem>().Select(d => d.Entry.Identifier).ToArray();
        Assert.Equal(new[] { "REG-004", "REG-008", "REG-021" }, decisions);
        Assert.Contains(read.Brief.Sections.Single(s => s.Section == BriefSection.Company).Items,
            i => i is BriefItem.RuleItem { Outcome.Rule: RecommendationRule.CfoCompanyCeiling });
    }

    /// <summary>
    /// Every dashboard tile is its brief item's own rendering from the same read with the same instant and case; the
    /// awaiting-approval tile equals the approval view's count; and no tile carries a connection string, a handle or
    /// a credential.
    /// </summary>
    [Fact]
    public void EveryTileIsItsBriefLineFromTheSameReadAndCarriesNoSecret()
    {
        var read = ManagementReportService.Compose(SnapshotFixture.Build(operations: 2, cost: 31m, awaiting: [Awaiting(), Awaiting()]));
        var items = read.Brief.Sections.SelectMany(s => s.Items).ToArray();

        Assert.Equal(items.Length, read.Tiles.Count);
        Assert.Equal(items.Select(ManagementRendering.Render), read.Tiles.Select(t => t.Rendering));
        Assert.All(read.Tiles, t => Assert.Equal(read.Reports.Instant, t.Instant));

        var awaiting = Assert.Single(read.Tiles, t => t.Rendering.StartsWith("item versions awaiting owner approval:", StringComparison.Ordinal));
        Assert.Contains("2 item versions awaiting owner approval [observed]", awaiting.Rendering, StringComparison.Ordinal);
        Assert.Equal(FigureCase.Observed, awaiting.Case);

        foreach (var forbidden in new[] { @"\bPassword\b", @"Host=", @"\bUsername\b", @"\bhandles?\b", @"\bsecrets?\b", @"\bcredentials?\b" })
        {
            Assert.All(read.Tiles, t => Assert.DoesNotMatch(new System.Text.RegularExpressions.Regex(forbidden, System.Text.RegularExpressions.RegexOptions.IgnoreCase), t.Rendering));
        }
    }
}
