using MediaCompany.Application.Ports;
using MediaCompany.Application.Production;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Production;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Deterministic.Reporting;
using MediaCompany.Deterministic.Rights;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Configuration;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Rights;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The multi-channel capability over in-memory doubles: the channel profile, the gate path through
/// the rights check, the copyright-check stage handler, and the one composing site of every
/// per-channel case. Every channel, value and count here is a DEMONSTRATION PARAMETER; no channel
/// value, budget amount or condition is recorded anywhere but in the doubles of one test.
/// </summary>
public sealed class MultiChannelSurfaceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-08T12:00:00Z");
    private static readonly DateOnly Month = new(2026, 10, 1);
    private static readonly CompanyId Company = CompanyId.New();
    private static readonly ChannelId ChannelA = ChannelId.New();
    private static readonly ChannelId ChannelB = ChannelId.New();

    // -----------------------------------------------------------------------
    // The channel key set and the channel profile
    // -----------------------------------------------------------------------

    [Fact]
    public void AChannelScopeNamesExactlyOneChannel()
    {
        var scope = ChannelConfigurationKeys.ScopeFor(ChannelA);

        Assert.Equal($"channel:{ChannelA.Value:D}", scope);
        Assert.True(ChannelConfigurationKeys.IsChannelScope(scope));
        Assert.False(ChannelConfigurationKeys.IsChannelScope("channel:"));
        Assert.False(ChannelConfigurationKeys.IsChannelScope("channel:not-a-channel"));
        Assert.False(ChannelConfigurationKeys.IsChannelScope("company"));
    }

    /// <summary>
    /// Each attribute recorded for one channel reads back for that channel and changes no value read
    /// for the other; an attribute never recorded reads NOT RECORDED, naming the key and the scope,
    /// with no default in its place; language and library registration come from their registers;
    /// and every channel's risk profile states that the channels share one payee.
    /// </summary>
    [Fact]
    public async Task EachChannelsProfileReadsItsOwnRecordedValuesAndNotRecordedWhereNothingWas()
    {
        var configuration = new InMemoryConfiguration();
        configuration.Record(ChannelConfigurationKeys.Audience, ChannelA, "fixture audience of channel A", 1);
        configuration.Record(ChannelConfigurationKeys.Audience, ChannelB, "fixture audience of channel B", 1);
        configuration.Record(ChannelConfigurationKeys.Audience, ChannelA, "fixture audience of channel A, revised", 2);

        var service = new ChannelProfileService(
            new Registers(new Channel(ChannelA, Company, "video-platform", "en", true), new Channel(ChannelB, Company, "video-platform", "vi", false)),
            new Rights([], [new LibraryRegistration(ChannelA, "library-one", new DateOnly(2026, 9, 1))]),
            configuration,
            new FixedClock(Now));

        var profiles = await service.ProfilesAsync(CancellationToken.None);
        var a = Assert.Single(profiles, p => p.Channel.Equals(ChannelA));
        var b = Assert.Single(profiles, p => p.Channel.Equals(ChannelB));

        var audienceA = Assert.IsType<ChannelAttribute.Recorded>(a.Audience);
        Assert.Equal("fixture audience of channel A, revised", audienceA.Value);
        Assert.Equal(2, audienceA.Version);
        Assert.Equal("fixture audience of channel B", Assert.IsType<ChannelAttribute.Recorded>(b.Audience).Value);

        var brand = Assert.IsType<ChannelAttribute.NotRecorded>(a.Brand);
        Assert.Equal(ChannelConfigurationKeys.Brand, brand.Key);
        Assert.Equal(ChannelConfigurationKeys.ScopeFor(ChannelA), brand.Scope);
        Assert.Contains("not recorded", brand.Describe(), StringComparison.Ordinal);
        Assert.All(a.ConfiguredAttributes.Skip(1), attribute => Assert.IsType<ChannelAttribute.NotRecorded>(attribute));

        Assert.Equal("en", a.Language);
        Assert.Equal("vi", b.Language);
        Assert.Single(a.LibraryRegistrations);
        Assert.Empty(b.LibraryRegistrations);

        foreach (var profile in profiles)
        {
            Assert.Contains("single payee", profile.Risk.SharedPayeeStatement, StringComparison.Ordinal);
            Assert.Contains("2 channel(s)", profile.Risk.SharedPayeeStatement, StringComparison.Ordinal);
            Assert.Contains(Company.ToString(), profile.Risk.SharedPayeeStatement, StringComparison.Ordinal);
        }

        Assert.Null(await service.ProfileAsync(ChannelId.New(), CancellationToken.None));
    }

    // -----------------------------------------------------------------------
    // The gate path through the rights check
    // -----------------------------------------------------------------------

    /// <summary>
    /// A new item in Draft is submitted for the rights check and then presented for owner approval
    /// on recorded state, each step audited; a presentation from Draft is refused by name and writes
    /// nothing but its refusal entry; and the submission from any state but Draft is refused.
    /// </summary>
    [Fact]
    public async Task ADraftItemReachesOwnerApprovalOnlyThroughTheRightsCheck()
    {
        var item = ItemId.New();
        var version = new ItemVersion(1);
        var work = new InMemoryWork();
        var ledger = new LedgerOverWork(work);
        var gate = Gate(work, ledger, [CompleteAsset(item)], item);

        var early = Assert.IsType<GateStepOutcome.Refused>(await gate.PresentForOwnerApprovalAsync(item, version, CancellationToken.None));
        Assert.Equal(GateState.Draft, early.RecordedState);
        Assert.Equal(GateStepRefusal.NotAwaitingRightsCheck, early.Reason);
        Assert.Empty(work.Transitions);
        Assert.Single(work.AuditEntries, e => e.Action == PublicationGateService.StepRefusedAction);

        var submitted = Assert.IsType<GateStepOutcome.Moved>(
            await gate.SubmitForRightsCheckAsync(item, version, WorkforceRole.Producer, CancellationToken.None));
        Assert.Equal((GateState.Draft, GateState.AwaitingRightsCheck), (submitted.From, submitted.To));

        var again = Assert.IsType<GateStepOutcome.Refused>(
            await gate.SubmitForRightsCheckAsync(item, version, WorkforceRole.Producer, CancellationToken.None));
        Assert.Equal(GateStepRefusal.NotSubmittableFromRecordedState, again.Reason);

        var presented = Assert.IsType<GateStepOutcome.Moved>(await gate.PresentForOwnerApprovalAsync(item, version, CancellationToken.None));
        Assert.Equal(GateState.AwaitingOwnerApproval, presented.To);

        Assert.Equal(
            new[] { (GateState.Draft, GateState.AwaitingRightsCheck), (GateState.AwaitingRightsCheck, GateState.AwaitingOwnerApproval) },
            work.Transitions.ToArray());
        Assert.Contains(work.AuditEntries, e => e.Action == PublicationGateService.SubmittedAction);
        Assert.Contains(work.AuditEntries, e => e.Action == PublicationGateService.PresentedAction);

        // The transition table itself is unchanged: no edge from Draft bypasses the rights check.
        Assert.Equal(new[] { GateState.AwaitingRightsCheck, GateState.Withdrawn }, GateTransitionTable.From(GateState.Draft).Order().ToArray());

        // A role without the presentation action cannot submit at all.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gate.SubmitForRightsCheckAsync(ItemId.New(), version, WorkforceRole.Accountant, CancellationToken.None));
    }

    /// <summary>
    /// An item with NO recorded asset decision is not presentable: the absence folds to not satisfied
    /// and is named. An item whose asset lacks its basis is refused naming what is missing.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task APresentationWithoutAReleasableRecordedRightsCheckIsRefusedByName(bool withIncompleteAsset)
    {
        var item = ItemId.New();
        var version = new ItemVersion(1);
        var work = new InMemoryWork().WithRecordedGateState(item, version, GateState.AwaitingRightsCheck);
        var assets = withIncompleteAsset ? new[] { CompleteAsset(item) with { ProofOfLicenceReference = null } } : [];
        var gate = Gate(work, new LedgerOverWork(work), assets, item);

        var refused = Assert.IsType<GateStepOutcome.Refused>(await gate.PresentForOwnerApprovalAsync(item, version, CancellationToken.None));

        Assert.Equal(GateStepRefusal.RightsNotReleasable, refused.Reason);
        Assert.Contains(withIncompleteAsset ? "proof of licence" : "no asset decision is recorded", refused.Detail, StringComparison.Ordinal);
        Assert.Empty(work.Transitions);
    }

    /// <summary>
    /// A transition starting from a state the record does not hold is refused by the writer, and the
    /// gate service names it as a changed recorded state rather than surfacing an error.
    /// </summary>
    [Fact]
    public async Task AStaleRecordedStateIsRefusedAndNamed()
    {
        var item = ItemId.New();
        var version = new ItemVersion(1);
        var work = new InMemoryWork();

        // The ledger reads awaiting rights check while the record holds Draft: another writer's view.
        var gate = Gate(work, new StateLedger(GateState.AwaitingRightsCheck), [CompleteAsset(item)], item);

        var refused = Assert.IsType<GateStepOutcome.Refused>(await gate.PresentForOwnerApprovalAsync(item, version, CancellationToken.None));

        Assert.Equal(GateStepRefusal.RecordedStateChanged, refused.Reason);
        Assert.Empty(work.Transitions);
    }

    /// <summary>Publish evaluation refuses a channel other than the item's recorded one, naming both.</summary>
    [Fact]
    public async Task PublishEvaluationRefusesAChannelOtherThanTheItemsRecordedChannel()
    {
        var item = ItemId.New();
        var work = new InMemoryWork();
        var gate = Gate(work, new StateLedger(GateState.Approved), [], item);

        var refused = Assert.IsType<GateVerdict.Refused>(await gate.EvaluatePublishAsync(
            item, new ItemVersion(1), ChannelB, WorkforceRole.Publisher, CancellationToken.None));

        Assert.Equal(GateRefusal.ChannelNotTheItemsRecordedChannel, refused.Reason);
        Assert.Contains(ChannelA.ToString(), refused.Detail, StringComparison.Ordinal);
        Assert.Empty(work.Blocks);
        Assert.Single(work.AuditEntries);
    }

    /// <summary>
    /// The copyright-check handler reads the RECORDED channel's registrations and the recorded assets,
    /// performs no metered operation, and returns evidence of the recorded rights check's verdict.
    /// </summary>
    [Fact]
    public async Task TheCopyrightCheckHandlerReturnsTheRecordedRightsVerdictAsStageEvidence()
    {
        var item = ItemId.New();
        var version = new ItemVersion(1);
        var dossiers = new OpenedDossier(item, version, ChannelA);
        var subject = new DeclaredSubject { Subject = "fixture subject", Pillar = "fixture", Format = "fixture" };

        var releasable = await new CopyrightCheckStageHandler(
                dossiers, new Rights([CompleteAsset(item)], [new LibraryRegistration(ChannelA, "library-one", new DateOnly(2026, 9, 1))]), new FixedClock(Now))
            .HandleAsync(item, version, subject, CancellationToken.None);
        Assert.Equal(ProductionStage.CopyrightCheck, releasable.Stage);
        Assert.Equal(StageOutcome.Succeeded, releasable.Evidence.Outcome);
        Assert.Contains(ChannelA.ToString(), releasable.Evidence.Summary, StringComparison.Ordinal);

        var unregistered = await new CopyrightCheckStageHandler(
                dossiers, new Rights([CompleteAsset(item)], [new LibraryRegistration(ChannelB, "library-one", new DateOnly(2026, 9, 1))]), new FixedClock(Now))
            .HandleAsync(item, version, subject, CancellationToken.None);
        Assert.Equal(StageOutcome.Failed, unregistered.Evidence.Outcome);
        Assert.Contains("not registered", unregistered.Evidence.Summary, StringComparison.Ordinal);

        var none = await new CopyrightCheckStageHandler(dossiers, new Rights([], []), new FixedClock(Now))
            .HandleAsync(item, version, subject, CancellationToken.None);
        Assert.Equal(StageOutcome.Failed, none.Evidence.Outcome);
        Assert.Contains(RecordedRightsCheck.NoAssetDecisionRecorded, none.Evidence.Summary, StringComparison.Ordinal);

        // The handler's whole dependency set: recorded state and a clock. No operation recorder, no
        // capability, no configuration.
        Assert.All(
            typeof(CopyrightCheckStageHandler).GetConstructors().Single().GetParameters(),
            p => Assert.Contains(p.ParameterType, new[] { typeof(IItemDossierReader), typeof(IAssetLedger), typeof(IClock) }));
        Assert.All(CopyrightCheckStageHandler.TaskNames, name => Assert.True(DeterministicTaskRegistry.Contains(name)));
    }

    // -----------------------------------------------------------------------
    // The one composing site of every per-channel case
    // -----------------------------------------------------------------------

    /// <summary>A month is final only when the horizon is at or after the next month's first instant.</summary>
    [Fact]
    public void AMonthIsFinalOnlyOnceTheHorizonHasPassedItsEnd()
    {
        var open = ChannelAnalyticsComposers.Finality(new MonthClosure(Month, true, DateTimeOffset.Parse("2026-10-31T23:59:59Z"), null));
        Assert.False(open.IsFinal);
        Assert.StartsWith("not yet final", open.Statement, StringComparison.Ordinal);
        Assert.Contains("may still be booked", open.Statement, StringComparison.Ordinal);

        var closed = ChannelAnalyticsComposers.Finality(new MonthClosure(Month, false, DateTimeOffset.Parse("2026-11-01T00:00:00Z"), null));
        Assert.True(closed.IsFinal);
        Assert.StartsWith("final", closed.Statement, StringComparison.Ordinal);
        Assert.Contains("as stored", closed.Statement, StringComparison.Ordinal);
    }

    /// <summary>
    /// SIDE BY SIDE: a registered channel with no operation in an observed month reads its count as
    /// an OBSERVED ZERO and its cost unmeasured under the delivered rule; the same channel in a month
    /// the record was not yet observing reads unmeasured naming the record's start; a channel the rows
    /// name and the register does not hold is its own partition; a channel nobody recorded reads
    /// unmeasured naming the register; and the company figure is the datastore's, never a sum here.
    /// </summary>
    [Fact]
    public void ARegisteredEmptyChannelAnUnobservedMonthAndAnUnregisteredChannelReadApart()
    {
        var stray = ChannelId.New();
        var totalsA = new OperationTotals(2, new Money(0.021m), false, 1, Money.Zero(),
            new Dictionary<CapabilityClass, Money> { [CapabilityClass.EditorialReasoning] = new(0.021m) }, 2, 2, 1);
        var totalsStray = new OperationTotals(1, new Money(0.0105m), true, 0, Money.Zero(), new Dictionary<CapabilityClass, Money>(), 0, 0, 0);
        var company = new OperationTotals(3, new Money(0.0315m), true, 1, Money.Zero(), new Dictionary<CapabilityClass, Money>(), 2, 2, 1);
        var register = new[] { new RegisteredChannel(ChannelA, Company), new RegisteredChannel(ChannelB, Company) };
        var rows = new[]
        {
            new OperationPartitionRow(ChannelA, true, totalsA),
            new OperationPartitionRow(ChannelB, true, StubPartitions.Empty),
            new OperationPartitionRow(stray, false, totalsStray),
        };

        var observed = ChannelAnalyticsComposers.Operations(new OperationPartitionSummary(
            new MonthClosure(Month, true, Now, DateTimeOffset.Parse("2026-10-02T00:00:00Z")), register, rows, company, new Money(-77.3785m)));

        var b = ChannelAnalyticsComposers.ForChannel(observed, ChannelB);
        Assert.IsType<MeasurementQuantity.ObservedZero>(b.Operations);
        var bCost = Assert.IsType<MeasurementQuantity.Unmeasured>(b.Cost);
        Assert.Contains($"channel {ChannelB}", bCost.Detail, StringComparison.Ordinal);
        Assert.IsType<MeasurementQuantity.Unmeasured>(b.VarianceAgainstEnvelope);

        var a = ChannelAnalyticsComposers.ForChannel(observed, ChannelA);
        Assert.Equal(0.021m, Assert.IsType<MeasurementQuantity.ObservedValue>(a.Cost).Amount);
        Assert.Equal(50m, Assert.IsType<MeasurementQuantity.ObservedValue>(a.TierRatio).Amount);

        var strayReading = ChannelAnalyticsComposers.ForChannel(observed, stray);
        Assert.Equal(ChannelPartitionStanding.NotInRegister, strayReading.Standing);
        Assert.IsType<MeasurementQuantity.ObservedValue>(strayReading.Cost);

        var nobody = ChannelAnalyticsComposers.ForChannel(observed, ChannelId.New());
        Assert.Contains("channel register holds no such channel", Assert.IsType<MeasurementQuantity.Unmeasured>(nobody.Operations).Detail, StringComparison.Ordinal);

        Assert.Equal(0.0315m, Assert.IsType<MeasurementQuantity.ObservedValue>(observed.Company.Cost).Amount);
        Assert.Equal(-77.3785m, Assert.IsType<MeasurementQuantity.ObservedValue>(observed.Company.VarianceAgainstEnvelope).Amount);
        Assert.False(string.IsNullOrWhiteSpace(observed.Statement));
        Assert.Contains("dark for every channel", observed.RevenueStatement, StringComparison.Ordinal);

        var before = ChannelAnalyticsComposers.Operations(new OperationPartitionSummary(
            new MonthClosure(new DateOnly(2026, 9, 1), true, Now, DateTimeOffset.Parse("2026-10-02T00:00:00Z")),
            register, [new OperationPartitionRow(ChannelB, true, StubPartitions.Empty)], StubPartitions.Empty, new Money(-77.41m)));

        var bBefore = ChannelAnalyticsComposers.ForChannel(before, ChannelB);
        var unobserved = Assert.IsType<MeasurementQuantity.Unmeasured>(bBefore.Operations);
        Assert.Contains("not yet observing", unobserved.Detail, StringComparison.Ordinal);
        Assert.NotEqual(b.Operations.Describe(), bBefore.Operations.Describe());
    }

    /// <summary>
    /// Budget utilisation: a channel with no recorded budget reads unmeasured naming the missing
    /// budget, at every threshold; a channel with one reads its utilisation and each threshold as
    /// reached or an observed zero; and the company reading carries its coverage statement and the
    /// standing commitment as its own unmeasured quantity.
    /// </summary>
    [Fact]
    public void BudgetUtilisationReadsPerChannelAndForTheCeilingInItsCases()
    {
        var reading = ChannelAnalyticsComposers.Budgets(new BudgetPartitionSummary(
            new MonthClosure(Month, true, Now, Now.AddDays(-7)),
            [
                new ChannelBudgetRow(ChannelA, 2, new Money(0.0149m), new Money(0.02m), 74.5m, new Dictionary<int, long> { [50] = 1 }),
                new ChannelBudgetRow(ChannelB, 0, Money.Zero(), null, null, new Dictionary<int, long>()),
            ],
            2, new Money(0.0149m), 0.0192m, ApprovedEnvelope.MonthlyTotal));

        var a = Assert.Single(reading.Channels, c => c.Channel.Equals(ChannelA));
        Assert.Equal(74.5m, Assert.IsType<MeasurementQuantity.ObservedValue>(a.Utilisation).Amount);
        Assert.IsType<MeasurementQuantity.ObservedValue>(a.Thresholds.Single(t => t.Threshold == BudgetThreshold.Fifty).Reached);
        Assert.IsType<MeasurementQuantity.ObservedZero>(a.Thresholds.Single(t => t.Threshold == BudgetThreshold.SeventyFive).Reached);
        Assert.IsType<MeasurementQuantity.ObservedValue>(a.Thresholds.Single(t => t.Threshold == BudgetThreshold.Fifty).AlertsRecorded);
        Assert.IsType<MeasurementQuantity.ObservedZero>(a.Thresholds.Single(t => t.Threshold == BudgetThreshold.SeventyFive).AlertsRecorded);

        var b = Assert.Single(reading.Channels, c => c.Channel.Equals(ChannelB));
        Assert.Contains("no budget is recorded", Assert.IsType<MeasurementQuantity.Unmeasured>(b.BudgetAmount).Detail, StringComparison.Ordinal);
        Assert.IsType<MeasurementQuantity.Unmeasured>(b.Utilisation);
        Assert.All(b.Thresholds, t => Assert.IsType<MeasurementQuantity.Unmeasured>(t.Reached));

        Assert.Equal(77.41m, Assert.IsType<MeasurementQuantity.ObservedValue>(reading.Company.Ceiling).Amount);
        Assert.IsType<MeasurementQuantity.ObservedValue>(reading.Company.Utilisation);
        Assert.All(reading.Company.Thresholds, t => Assert.IsType<MeasurementQuantity.ObservedZero>(t.Reached));
        Assert.All(reading.Company.Thresholds, t => Assert.IsType<MeasurementQuantity.Unmeasured>(t.AlertsRecorded));
        Assert.Contains("standing commitment", Assert.IsType<MeasurementQuantity.Unmeasured>(reading.Company.StandingCommitment).Detail, StringComparison.Ordinal);
        Assert.Contains("metered operations the operation record carries", reading.Company.CoverageStatement, StringComparison.Ordinal);
        Assert.False(reading.Finality.IsFinal);
    }

    /// <summary>
    /// The approval listing holds exactly the channel's awaiting item versions, and a channel with no
    /// recorded approval in the month reads ONE entry whose three components are unmeasured, never
    /// zero minutes; no aggregate of minutes is formed.
    /// </summary>
    [Fact]
    public void TheApprovalListingAndWorkloadReadPerChannel()
    {
        var awaiting = new[]
        {
            new AwaitingApproval(ChannelA, ItemId.New(), new ItemVersion(1), Now),
            new AwaitingApproval(ChannelB, ItemId.New(), new ItemVersion(1), Now),
        };

        var listing = ChannelAnalyticsComposers.Listing(ChannelA, true, awaiting);
        Assert.Equal(awaiting[0].Item, Assert.Single(listing.Entries).Item);
        Assert.IsType<MeasurementQuantity.ObservedValue>(listing.Awaiting);
        Assert.IsType<MeasurementQuantity.ObservedZero>(ChannelAnalyticsComposers.Listing(ChannelA, true, []).Awaiting);

        var none = ChannelAnalyticsComposers.Workload(ChannelB, true, Month, []);
        Assert.IsType<MeasurementQuantity.ObservedZero>(none.Approvals);
        Assert.IsType<MeasurementQuantity.ObservedZero>(none.ChangeRequests);
        var entry = Assert.Single(none.Efforts);
        Assert.Null(entry.Item);
        Assert.All(new[] { entry.Review, entry.Queue, entry.Rework }, q => Assert.IsType<MeasurementQuantity.Unmeasured>(q));

        var item = ItemId.New();
        var sentBack = new Approval(item, new ItemVersion(1), GatePredicates.OwnerApprovalGate, WorkforceRole.Owner,
            ApprovalVerdict.SentBack, "fixture send-back", Now.AddMinutes(-40), Now.AddMinutes(-10));
        var some = ChannelAnalyticsComposers.Workload(ChannelA, true, Month, [new DecidedApproval(sentBack, null)]);
        Assert.IsType<MeasurementQuantity.ObservedValue>(some.Approvals);
        Assert.IsType<MeasurementQuantity.ObservedValue>(some.ChangeRequests);
        var effort = Assert.Single(some.Efforts);
        Assert.Equal(30m, Assert.IsType<MeasurementQuantity.ObservedValue>(effort.Review).Amount);
        Assert.IsType<MeasurementQuantity.Unmeasured>(effort.Queue);
        Assert.IsType<MeasurementQuantity.ObservedZero>(effort.Rework);
        Assert.DoesNotContain("threshold:", some.Statement, StringComparison.Ordinal);
    }

    /// <summary>The deferred cycle time states the two-clock limit of the stage rows it would read.</summary>
    [Fact]
    public void TheDeferredCycleTimeStatesTheTwoClockLimit()
    {
        var cycle = MeasureCatalogue.Deferred.Single(d => d.Name == "cycle-time");
        Assert.Contains(MeasureCatalogue.CycleTimeClockLimit, cycle.Display, StringComparison.Ordinal);
        Assert.Contains("different clocks", cycle.Display, StringComparison.Ordinal);
        Assert.Contains("not yet available", cycle.Display, StringComparison.Ordinal);
    }

    /// <summary>The declared lifecycle subject names the unit the channel partition reads back out of it.</summary>
    [Fact]
    public void TheDeclaredUnitSubjectIsReadBackByItsPattern()
    {
        var job = JobId.New();
        var subject = LifecycleActions.UnitSubject(job, ItemId.New());
        Assert.Matches(LifecycleActions.UnitSubjectPattern, subject);

        Assert.Equal(job.Value.ToString("D"), System.Text.RegularExpressions.Regex.Match(subject, LifecycleActions.UnitSubjectPattern).Groups[1].Value);
        Assert.DoesNotMatch(LifecycleActions.UnitSubjectPattern, "item:1 version:1");
    }

    /// <summary>
    /// Per-channel lines on the measurable-now report, each carrying whether its month can still
    /// change, and the channel lines of the cost figure beside the company's.
    /// </summary>
    [Fact]
    public async Task TheReportPrintsPerChannelLinesWithTheMonthsFinality()
    {
        var partitions = new StubPartitions
        {
            Horizon = Now,
            EarliestEntry = Now.AddDays(-7),
            Register = [new RegisteredChannel(ChannelA, Company)],
            OperationRows = [new OperationPartitionRow(ChannelA, true, StubPartitions.Empty)],
        };

        var lines = await new ReportingService(new NoCosts(), partitions, new FixedClock(Now)).MeasurableNowAsync(Month, CancellationToken.None);

        var channelLine = Assert.Single(lines, l => l.Name == $"monthly-cost-total:channel:{ChannelA}");
        Assert.IsType<MeasurementQuantity.Unmeasured>(channelLine.Quantity);
        Assert.All(lines, l => Assert.Contains("not yet final", l.Source, StringComparison.Ordinal));
        Assert.False(ReportingService.ReportedSetContainsRevenueMeasure(lines));
    }

    // -----------------------------------------------------------------------
    // The entry point's unclaimed rest
    // -----------------------------------------------------------------------

    /// <summary>
    /// ANOTHER WORKER CLAIMS THE DRIVE'S UNIT between two positions. The drive comes to rest
    /// unclaimed at the position it was due to run, the resting unit reads as claimed by the other
    /// worker and not as waiting, and a second drive starts a NEW unit rather than resuming it.
    /// </summary>
    [Fact]
    public async Task ADriveWhoseUnitAnotherWorkerClaimsRestsUnclaimedAndASecondDriveStartsANewUnit()
    {
        var work = new InMemoryWork();
        var clock = new FixedClock(Now);
        var lifecycle = new WorkLifecycleService(work, clock);
        var stolen = false;

        work.AfterCommit = () =>
        {
            var ready = work.Jobs.Values.FirstOrDefault(j =>
                j.Position == LifecyclePosition.PublishingTimingComputation && j.ClaimState == ClaimState.Ready);
            if (!stolen && ready is not null)
            {
                stolen = true;
                lifecycle.ClaimAsync("another-worker", TimeSpan.FromMinutes(5), CancellationToken.None).GetAwaiter().GetResult();
            }
        };

        var item = ItemId.New();
        var sequence = new PublishingSequenceService(
            lifecycle,
            new PublicationGateService(new StateLedger(GateState.Draft), new Rights([], []), new RecordedItems(ChannelA), work, clock),
            new PublicationDispatchService(work, clock),
            clock);

        var first = await sequence.DriveAsync(Request(item), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.NotClaimable, first.Rest);
        Assert.Equal(LifecyclePosition.PublishingTimingComputation, first.RestingPosition);
        Assert.Equal(new[] { LifecyclePosition.Queued, LifecyclePosition.PublishingSurfaceProduction }, first.PositionsDriven.ToArray());

        var resting = work.Jobs[first.Unit];
        Assert.Equal(ClaimState.Claimed, resting.ClaimState);
        Assert.Equal("another-worker", resting.ClaimedBy);
        Assert.DoesNotContain(work.Jobs.Values, j => j.ClaimState == ClaimState.Ready);
        Assert.Equal(3, work.AuditEntries.Count(e => e.Action == LifecycleActions.Claimed && e.Subject.StartsWith($"job:{first.Unit}", StringComparison.Ordinal)));

        var second = await sequence.DriveAsync(Request(item), CancellationToken.None);

        Assert.NotEqual(first.Unit, second.Unit);
        Assert.Equal(ClaimState.Claimed, work.Jobs[first.Unit].ClaimState);
        Assert.Equal(PublishingSequenceRest.RefusedAtGateEvaluation, second.Rest);
    }

    // -----------------------------------------------------------------------
    // Doubles
    // -----------------------------------------------------------------------

    private static PublicationGateService Gate(InMemoryWork work, IGateLedger ledger, IReadOnlyList<Asset> assets, ItemId item) =>
        new(ledger, new Rights(assets, [new LibraryRegistration(ChannelA, "library-one", new DateOnly(2026, 9, 1))]),
            new RecordedItems().Record(item, ChannelA), work, new FixedClock(Now));

    private static Asset CompleteAsset(ItemId item) => new()
    {
        Id = AssetId.New(),
        Item = item,
        Library = "library-one",
        Source = "fixture catalogue",
        Creator = "fixture creator",
        LicenceType = LicenceType.RoyaltyFreeStock,
        LicenceReference = "fixture-licence",
        CommercialUsePermitted = true,
        ModificationPermitted = true,
        AttributionRequirement = "none",
        PlatformRestrictions = "none",
        ProofOfLicenceReference = "fixture-proof",
        AssessedRisk = AssessedRisk.Low,
        VerifiedBy = WorkforceRole.Copyright,
        VerifiedOn = new DateOnly(2026, 9, 20),
    };

    private static PublishingSequenceRequest Request(ItemId item) => new()
    {
        Item = item,
        Version = new ItemVersion(1),
        Channel = ChannelA,
        Subject = PublishingFixture.Subject,
        Destination = PublishingFixture.Destination,
        TreatmentConditions = Enum.GetValues<TreatmentCondition>(),
        SurfaceDraft = PublishingFixture.Draft(),
        Settings = PublishingFixture.Settings,
        TimingPolicy = new PublicationTiming.TimingPolicy { LeadDays = 3, PreferredTimeOfDay = new TimeOnly(14, 0) },
    };

    /// <summary>A gate ledger that reads the current state from what the in-memory work recorded.</summary>
    private sealed class LedgerOverWork(InMemoryWork work) : IGateLedger
    {
        public Task<IReadOnlyList<Block>> OpenBlocksAsync(ItemId item, CancellationToken ct) => Task.FromResult<IReadOnlyList<Block>>([]);

        public Task<IReadOnlyList<Approval>> ApprovalsAsync(ItemId item, ItemVersion version, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Approval>>([]);

        public Task<GateState> CurrentStateAsync(ItemId item, ItemVersion version, CancellationToken ct) =>
            Task.FromResult(work.GateStates.TryGetValue((item, version), out var state) ? state : GateState.Draft);

        public Task<FirstPublicationConditionRegister> FirstPublicationConditionsAsync(ChannelId channel, CancellationToken ct) =>
            Task.FromResult(FirstPublicationConditionRegister.Empty);
    }

    /// <summary>A gate ledger reading one fixed state.</summary>
    private sealed class StateLedger(GateState state) : IGateLedger
    {
        public Task<IReadOnlyList<Block>> OpenBlocksAsync(ItemId item, CancellationToken ct) => Task.FromResult<IReadOnlyList<Block>>([]);

        public Task<IReadOnlyList<Approval>> ApprovalsAsync(ItemId item, ItemVersion version, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Approval>>([]);

        public Task<GateState> CurrentStateAsync(ItemId item, ItemVersion version, CancellationToken ct) => Task.FromResult(state);

        public Task<FirstPublicationConditionRegister> FirstPublicationConditionsAsync(ChannelId channel, CancellationToken ct) =>
            Task.FromResult(FirstPublicationConditionRegister.Empty);
    }

    private sealed class Rights(IReadOnlyList<Asset> assets, IReadOnlyList<LibraryRegistration> registrations) : IAssetLedger
    {
        public Task<IReadOnlyList<Asset>> ForItemAsync(ItemId item, CancellationToken ct) => Task.FromResult(assets);

        public Task<IReadOnlyList<LibraryRegistration>> RegistrationsAsync(ChannelId channel, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<LibraryRegistration>>(registrations.Where(r => r.Channel.Equals(channel)).ToArray());

        public Task RecordAsync(Asset asset, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class Registers(params Channel[] channels) : IOperatingRegisters
    {
        public Task<IReadOnlyList<ProviderAccount>> ProviderAccountsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<ProviderAccount>>([]);

        public Task<IReadOnlyList<Model>> ModelsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Model>>([]);

        public Task<IReadOnlyList<ModelPrice>> PricesInForceAsync(DateTimeOffset asOf, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ModelPrice>>([]);

        public Task<IReadOnlyList<Channel>> ChannelsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Channel>>(channels);

        public Task<IReadOnlyList<Department>> DepartmentsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Department>>([]);

        public Task<IReadOnlyList<WorkforceAgent>> AgentsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<WorkforceAgent>>([]);
    }

    /// <summary>The temporal configuration register in memory: every version kept, the latest in force.</summary>
    private sealed class InMemoryConfiguration : IConfigurationStore
    {
        private readonly List<ConfigurationVersion> _rows = [];

        public void Record(string key, ChannelId channel, string value, int version) =>
            _rows.Add(new ConfigurationVersion
            {
                Key = key,
                Scope = ChannelConfigurationKeys.ScopeFor(channel),
                Value = value,
                Version = version,
                ChangedBy = "fixture",
                Reason = "demonstration fixture value",
                ValidFrom = Now.AddDays(-10 + version),
            });

        public Task<ConfigurationVersion?> InForceAsync(string key, string scope, DateTimeOffset asOf, CancellationToken ct) =>
            Task.FromResult(_rows
                .Where(r => r.Key == key && r.Scope == scope && r.ValidFrom <= asOf)
                .OrderByDescending(r => r.ValidFrom)
                .FirstOrDefault());

        public Task<IReadOnlyList<ConfigurationVersion>> HistoryAsync(string key, string scope, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ConfigurationVersion>>(_rows.Where(r => r.Key == key && r.Scope == scope).ToArray());

        public Task SupersedeAsync(ConfigurationVersion next, CancellationToken ct) => throw new NotSupportedException();
    }

    /// <summary>One opened dossier, recorded against a channel.</summary>
    private sealed class OpenedDossier(ItemId item, ItemVersion version, ChannelId channel) : IItemDossierReader
    {
        public Task<RecordedDossier?> DossierAsync(ItemId i, ItemVersion v, CancellationToken ct) =>
            Task.FromResult<RecordedDossier?>(i.Equals(item) && v.Equals(version)
                ? new RecordedDossier(new ItemDossier { Item = item, Version = version }, channel, Now)
                : null);

        public Task<IReadOnlyList<StageEvidence>?> StageEvidenceAsync(ItemId i, ItemVersion v, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<StageEvidence>?>([]);

        public Task<IReadOnlyList<SupplyAuditEntry>?> SupplyAuditAsync(ItemId i, ItemVersion v, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SupplyAuditEntry>?>([]);

        public Task<IReadOnlyList<DeterminationResolution>?> DeterminationsAsync(ItemId i, ItemVersion v, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<DeterminationResolution>?>([]);
    }

    private sealed class NoCosts : ICostRollupReader
    {
        public Task<Money> CostForItemAsync(ItemId item, CancellationToken ct) => Task.FromResult(Money.Zero());

        public Task<Money> CostForPeriodAsync(DateOnly period, CancellationToken ct) => Task.FromResult(Money.Zero());

        public Task<PeriodSummary> PeriodSummaryAsync(DateOnly period, CancellationToken ct) =>
            Task.FromResult(new PeriodSummary(period, Money.Zero(), new Money(77.41m), new Money(34.42m), new Money(42.99m),
                new Money(-77.41m), false, 0));

        public Task<IReadOnlyDictionary<CapabilityClass, Money>> CostByCapabilityAsync(DateOnly period, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<CapabilityClass, Money>>(new Dictionary<CapabilityClass, Money>());

        public Task<Money> CostForDeterministicSetAsync(DateOnly period, CancellationToken ct) => Task.FromResult(Money.Zero());

        public Task<MeasurementQuantity> ItemCostQuantityAsync(ItemId item, CancellationToken ct) =>
            Task.FromResult(MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, "none"));

        public Task<MeasurementQuantity> PeriodCostQuantityAsync(DateOnly period, CancellationToken ct) =>
            Task.FromResult(MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, "none"));

        public Task<ItemSummary> ItemSummaryAsync(ItemId item, CancellationToken ct) =>
            Task.FromResult(new ItemSummary(item, Money.Zero(), false, 0));
    }
}
