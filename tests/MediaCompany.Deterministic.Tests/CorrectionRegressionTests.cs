using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Deterministic.Resilience;
using MediaCompany.Deterministic.Routing;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Rights;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The demonstrations the review asked for, each one failing on the code as it stood before the
/// correction it covers. A correction with no check that would have caught the defect leaves the
/// next regression to be found the same way the first one was.
/// </summary>
public sealed class LifecycleTerminationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");

    private static readonly WorkflowDefinition Workflow = new(
        "single-item",
        [LifecyclePosition.Research, LifecyclePosition.Script, LifecyclePosition.Assembly],
        FailurePolicy.Default);

    private static Job NewJob() => new()
    {
        Id = JobId.New(),
        Item = ItemId.New(),
        Channel = ChannelId.New(),
        Workflow = Workflow.Name,
        Position = LifecyclePosition.Research,
        ClaimState = ClaimState.Ready,
        AvailableAt = Now,
    };

    /// <summary>
    /// A unit that finishes the last position its workflow declares reaches a terminal claim
    /// state and is not claimed again. Before the correction it was released back to Ready and
    /// re-claimed indefinitely.
    /// </summary>
    [Fact]
    public async Task ACompletedUnitReachesATerminalClaimStateAndIsNotClaimedAgain()
    {
        var work = new InMemoryWork();
        var clock = new FixedClock(Now);
        var service = new WorkLifecycleService(work, clock);
        var job = NewJob();

        await service.EnqueueAsync(job, CancellationToken.None);

        var current = job;
        foreach (var position in Workflow.Stages)
        {
            await service.RecordStageOutcomeAsync(
                current, Workflow, position, clock.UtcNow, succeeded: true,
                failureClass: null, attemptsSoFar: 1, Workflow.FailurePolicy,
                TimeSpan.Zero, TimeSpan.FromHours(1), failureReason: null, CancellationToken.None);

            current = work.Jobs[job.Id];
            clock.Advance(TimeSpan.FromMinutes(5));
        }

        var finished = work.Jobs[job.Id];
        Assert.Equal(ClaimState.Done, finished.ClaimState);
        Assert.Equal(LifecyclePosition.Completed, finished.Position);

        // Nothing is claimable once the unit is done.
        await using var transaction = await work.BeginAsync(CancellationToken.None);
        Assert.Null(await transaction.Jobs.ClaimNextAsync("worker", TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    /// <summary>
    /// The stage history of a completed unit resolves to every position it passed, each recorded
    /// as succeeded exactly once (acceptance criterion AC-022).
    /// </summary>
    [Fact]
    public async Task TheStageHistoryResolvesToEachPositionExactlyOnce()
    {
        var work = new InMemoryWork();
        var clock = new FixedClock(Now);
        var service = new WorkLifecycleService(work, clock);
        var job = NewJob();

        await service.EnqueueAsync(job, CancellationToken.None);

        var current = job;
        foreach (var position in Workflow.Stages)
        {
            await service.RecordStageOutcomeAsync(
                current, Workflow, position, clock.UtcNow, succeeded: true,
                failureClass: null, attemptsSoFar: 1, Workflow.FailurePolicy,
                TimeSpan.Zero, TimeSpan.FromHours(1), failureReason: null, CancellationToken.None);

            current = work.Jobs[job.Id];
            clock.Advance(TimeSpan.FromMinutes(5));
        }

        var succeeded = work.Stages
            .Where(s => s.Outcome == StageOutcome.Succeeded)
            .Select(s => s.Position)
            .ToArray();

        Assert.Equal(Workflow.Stages, succeeded);
    }

    /// <summary>
    /// A re-resolution waits the declared backoff before the unit is claimable again, so an
    /// unavailable route cannot be re-resolved in a tight loop.
    /// </summary>
    [Fact]
    public async Task AReResolutionWaitsTheDeclaredBackoff()
    {
        var work = new InMemoryWork();
        var clock = new FixedClock(Now);
        var service = new WorkLifecycleService(work, clock);
        var job = NewJob();

        await service.EnqueueAsync(job, CancellationToken.None);

        var disposition = await service.RecordStageOutcomeAsync(
            job, Workflow, LifecyclePosition.Research, clock.UtcNow, succeeded: false,
            FailureClass.Outage, attemptsSoFar: 1, Workflow.FailurePolicy,
            TimeSpan.Zero, TimeSpan.FromHours(1), "the route's circuit opened", CancellationToken.None);

        Assert.IsType<FailureDisposition.ReResolve>(disposition);
        Assert.True(work.Jobs[job.Id].AvailableAt > clock.UtcNow);
    }

    /// <summary>An escalated unit is dead rather than ready, so it is not claimed again either.</summary>
    [Fact]
    public async Task AnEscalatedUnitIsNotClaimedAgain()
    {
        var work = new InMemoryWork();
        var clock = new FixedClock(Now);
        var service = new WorkLifecycleService(work, clock);
        var job = NewJob();

        await service.EnqueueAsync(job, CancellationToken.None);

        var disposition = await service.RecordStageOutcomeAsync(
            job, Workflow, LifecyclePosition.Research, clock.UtcNow, succeeded: false,
            FailureClass.Refusal, attemptsSoFar: 1, Workflow.FailurePolicy,
            TimeSpan.Zero, TimeSpan.FromHours(1), "a refusal never retries", CancellationToken.None);

        Assert.IsType<FailureDisposition.Escalate>(disposition);
        Assert.Equal(ClaimState.Dead, work.Jobs[job.Id].ClaimState);
        Assert.Contains(work.Stages, s => s.Outcome == StageOutcome.Escalated && s.Escalated);
    }
}

/// <summary>
/// The currency demonstration. The single egress must never end a request with an unhandled
/// exception, because a request that throws has no recorded reason.
/// </summary>
public sealed class CurrencyMismatchTests
{
    /// <summary>
    /// A route priced in a currency the request's ceiling is not resolves to a held state with a
    /// recorded reason. Before the correction the comparison raised.
    /// </summary>
    [Fact]
    public void ARoutePricedInAnotherCurrencyIsHeldWithARecordedReason()
    {
        var route = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var prices = new Dictionary<ModelId, UnitPrices>
        {
            [Fixture.PrimaryModel] = new(0.000_003m, 0.000_015m, 0m, "EUR"),
        };

        var resolution = RouteResolver.Resolve(
            Fixture.Request(floor: 70),
            Fixture.Inputs([route], prices: prices));

        var held = Assert.IsType<CapabilityResolution.Held>(resolution);
        Assert.Equal(RefusalReason.CurrencyMismatch, held.Reason);
    }

    /// <summary>
    /// Where one route is comparable and another is not, the comparable one is still resolved to.
    /// A mismatch removes a route; it does not fail the request.
    /// </summary>
    [Fact]
    public void AComparableRouteIsStillResolvedToWhenAnotherIsNot()
    {
        var primary = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var secondary = Fixture.Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 85);

        var prices = new Dictionary<ModelId, UnitPrices>
        {
            [Fixture.PrimaryModel] = new(0.000_003m, 0.000_015m, 0m, "EUR"),
            [Fixture.SecondaryModel] = new(0.000_001m, 0.000_005m, 0m, "USD"),
        };

        var resolved = Assert.IsType<CapabilityResolution.Resolved>(
            RouteResolver.Resolve(Fixture.Request(floor: 70), Fixture.Inputs([primary, secondary], prices: prices)));

        Assert.Equal(secondary.Id, resolved.Route.Id);
    }

    /// <summary>
    /// A held resolution carries its escalation instant when it is returned; no caller has to
    /// stamp it afterwards.
    /// </summary>
    [Fact]
    public void AHeldResolutionCarriesItsEscalationInstant()
    {
        var held = Assert.IsType<CapabilityResolution.Held>(
            RouteResolver.Resolve(Fixture.Request(floor: 99), Fixture.Inputs([])));

        Assert.Equal(Fixture.Now + TimeSpan.FromHours(4), held.EscalatesAt);
        Assert.NotEqual(DateTimeOffset.MinValue, held.EscalatesAt);
    }

    /// <summary>
    /// The forbidden-source screen applies to the routes the request could reach. A forbidden
    /// route admitted for another capability does not refuse an unrelated request.
    /// </summary>
    [Fact]
    public void AForbiddenRouteForAnotherCapabilityDoesNotRefuseThisRequest()
    {
        var wanted = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var otherCapability = new Route(
            RouteId.New(),
            CapabilityClass.Narration,
            RouteTier.Primary,
            new RouteTarget.ProviderRoute(new ProviderAccountId("consumer-chat-subscription"), new ModelId("x")),
            new QualityRating(80),
            new ContextCapacity(1_000),
            "consumer subscription",
            Fixture.Today);

        var register = new[]
        {
            new ForbiddenSource(
                ForbiddenSourceKind.ConsumerChatSubscription,
                "consumer-chat-subscription",
                "prohibited by the provider's terms",
                "platform-policy-dossier"),
        };

        var resolved = Assert.IsType<CapabilityResolution.Resolved>(
            RouteResolver.Resolve(
                Fixture.Request(floor: 70),
                Fixture.Inputs([wanted, otherCapability], forbidden: register)));

        Assert.Equal(wanted.Id, resolved.Route.Id);
    }
}

/// <summary>
/// The gate demonstration the review asked for: an unmet rights precondition places a standing
/// block naming the asset and what is missing (acceptance criterion AC-006).
/// </summary>
public sealed class GateBlockPlacementTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
    private static readonly ItemId Item = ItemId.New();
    private static readonly ChannelId Channel = ChannelId.New();
    private static readonly ItemVersion Version = new(1);

    [Fact]
    public async Task AnUnmetRightsPreconditionPlacesABlockNamingTheAssetAndWhatIsMissing()
    {
        var incomplete = new Asset
        {
            Id = AssetId.New(),
            Item = Item,
            Library = "stock-library-a",
            Source = "stock-library-a catalogue",
            Creator = "A. Photographer",
            LicenceType = LicenceType.RoyaltyFreeStock,
            LicenceReference = null,
            CommercialUsePermitted = true,
            ModificationPermitted = true,
            AttributionRequirement = "none",
            PlatformRestrictions = "none",
            ProofOfLicenceReference = null,
            AssessedRisk = AssessedRisk.Low,
            VerifiedBy = WorkforceRole.Copyright,
            VerifiedOn = new DateOnly(2026, 9, 20),
        };

        var work = new InMemoryWork();
        var service = new PublicationGateService(
            new StubGateLedger(GateState.Approved),
            new StubAssetLedger([incomplete], [new LibraryRegistration(Channel, "stock-library-a", new DateOnly(2026, 9, 1))]),
            new RecordedItems().Record(Item, Channel),
            work,
            new FixedClock(Now));

        var verdict = await service.EvaluatePublishAsync(
            Item, Version, Channel, WorkforceRole.Publisher, CancellationToken.None);

        var refused = Assert.IsType<GateVerdict.Refused>(verdict);
        Assert.Equal(GateRefusal.RightsPreconditionUnmet, refused.Reason);

        var block = Assert.Single(work.Blocks);
        Assert.Equal(incomplete.Id, block.Asset);
        Assert.Equal(WorkforceRole.Copyright, block.PlacedBy);
        Assert.Contains("licence reference", block.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("proof of licence", block.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.True(block.Open);
    }

    /// <summary>
    /// The owner verdict, its gate transition and the entry that records them become durable
    /// together. Before the correction the approval was written on its own connection.
    /// </summary>
    [Fact]
    public async Task AnOwnerVerdictItsTransitionAndItsEntryCommitTogether()
    {
        // The verdict is written from the state the record holds, which a demonstration records first:
        // the double refuses a from-state the record does not hold, exactly as the record store does.
        var work = new InMemoryWork().WithRecordedGateState(Item, Version, GateState.AwaitingOwnerApproval);
        var clock = new FixedClock(Now);
        var service = new PublicationGateService(
            new StubGateLedger(GateState.AwaitingOwnerApproval),
            new StubAssetLedger([], []),
            new RecordedItems().Record(Item, Channel),
            work,
            clock);

        var presented = Now.AddMinutes(-31);
        clock.UtcNow = Now;

        var approval = await service.RecordOwnerVerdictAsync(
            Item, Version, ApprovalVerdict.Approved, "reads well", presented, CancellationToken.None);

        Assert.Equal(31, approval.ElapsedMinutes);
        Assert.Equal(1, work.Commits);
        Assert.Single(work.Approvals);
        Assert.Contains(work.Transitions, t => t == (GateState.AwaitingOwnerApproval, GateState.Approved));
        Assert.Contains(work.AuditEntries, e => e.Action == "gate.owner-approved");
    }

    private sealed class StubGateLedger(GateState state) : IGateLedger
    {
        public Task<IReadOnlyList<Block>> OpenBlocksAsync(ItemId item, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Block>>([]);

        public Task<IReadOnlyList<Approval>> ApprovalsAsync(ItemId item, ItemVersion version, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Approval>>([
                new Approval(item, version, GatePredicates.OwnerApprovalGate, WorkforceRole.Owner,
                    ApprovalVerdict.Approved, "approved", Now.AddMinutes(-20), Now.AddMinutes(-5)),
            ]);

        public Task<GateState> CurrentStateAsync(ItemId item, ItemVersion version, CancellationToken ct) =>
            Task.FromResult(state);

        // The empty register. All three conditions of first publication resolve ABSENT and refuse,
        // which is the real recorded position: none of the three is discharged.
        public Task<FirstPublicationConditionRegister> FirstPublicationConditionsAsync(
            ChannelId channel, CancellationToken ct) =>
            Task.FromResult(FirstPublicationConditionRegister.Empty);
    }

    private sealed class StubAssetLedger(IReadOnlyList<Asset> assets, IReadOnlyList<LibraryRegistration> registrations)
        : IAssetLedger
    {
        public Task<IReadOnlyList<Asset>> ForItemAsync(ItemId item, CancellationToken ct) =>
            Task.FromResult(assets);

        public Task<IReadOnlyList<LibraryRegistration>> RegistrationsAsync(ChannelId channel, CancellationToken ct) =>
            Task.FromResult(registrations);

        public Task RecordAsync(Asset asset, CancellationToken ct) => Task.CompletedTask;
    }
}
