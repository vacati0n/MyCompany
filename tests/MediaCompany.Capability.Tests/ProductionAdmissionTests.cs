using MediaCompany.Application.Ports;
using MediaCompany.Capability.Providers;
using MediaCompany.Credentials;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Capability.Tests;

/// <summary>
/// The per-item cap at the boundary (the production change, decision D-006 of its design), over in-memory ports
/// that keep the one property that matters: nothing a transaction wrote is durable until it commits. Every amount is
/// a demonstration parameter; the cap is the owner's USD 5.95; the adapter doubles reach nothing.
/// </summary>
public sealed class ProductionAdmissionTests
{
    private static readonly ProviderAccountId Account = new("speech-account");

    /// <summary>
    /// THE ROUND TRIP OF THE WORST CASE: the reservation is COMMITTED before the vendor is called; an attempt whose
    /// charge is not known (a timeout after the vendor may have charged) is booked at that worst case, labelled
    /// estimate, never at zero units; and the booking reconciles the reservation under the one identifier.
    /// </summary>
    [Fact]
    public async Task AnAttemptWhoseChargeIsUnknownIsBookedAtItsReservedWorstCaseNeverAtZero()
    {
        var work = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _, cap: 5.95m);
        var reservedAtCall = -1;
        var adapter = new ScriptedAdapter(Account, () =>
        {
            reservedAtCall = work.Admission.CommittedReservations.Count;
            return VendorCalls.Unknown(TimeSpan.FromSeconds(60), "the provider-call bound passed", ProviderFailureSignal.Outage);
        });
        var gateway = ProductionHarness.Gateway(work, adapter);

        var outcome = Assert.IsType<CapabilityOutcome.Completed>(await gateway.ExecuteAsync(
            ProductionHarness.NarrationRequest(new string('a', 1_200)), ProductionHarness.Context(), CancellationToken.None));

        Assert.Equal(1, reservedAtCall);
        var reservation = Assert.Single(work.Admission.CommittedReservations);
        Assert.Equal(outcome.Operation.Id, reservation.Operation);
        Assert.Equal(1_200, reservation.WorstCaseUnits.CharacterUnits);
        Assert.Equal(1_200 * ProductionHarness.CharacterPrice, reservation.WorstCaseAmount.Amount);
        Assert.Equal(OperationOutcome.Failed, outcome.Operation.Outcome);
        Assert.Equal(1_200, outcome.Operation.Units.CharacterUnits);
        Assert.Equal(CostBasis.Estimate, outcome.Operation.CostBasis);
        Assert.Contains("booked at the admitted worst case", outcome.Operation.FailureReason, StringComparison.Ordinal);
        Assert.Null(outcome.Content);
    }

    /// <summary>
    /// ADMISSION REFUSES A CALL WHOSE WORST CASE WOULD PASS THE CAP, BEFORE IT IS MADE: with USD 5.90 counted
    /// against a USD 5.95 cap, a call whose worst case is USD 0.06 is refused under the item-cap reason naming the
    /// cap, the counted total and the worst case; the vendor is not called and nothing is reserved.
    /// </summary>
    [Fact]
    public async Task ACallWhoseWorstCasePassesTheCapIsRefusedBeforeItIsMade()
    {
        var work = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _, cap: 5.95m, booked: 5.90m);
        var adapter = new ScriptedAdapter(Account, ProductionHarness.Succeeded);
        var gateway = ProductionHarness.Gateway(work, adapter);

        var refused = Assert.IsType<CapabilityOutcome.Refused>(await gateway.ExecuteAsync(
            ProductionHarness.NarrationRequest(new string('a', 4_000)), ProductionHarness.Context(), CancellationToken.None));

        Assert.Equal(RefusalReason.ItemCapExceeded, refused.Reason);
        Assert.Contains("5.95", refused.Detail, StringComparison.Ordinal);
        Assert.Contains("counted total 5.90", refused.Detail, StringComparison.Ordinal);
        Assert.Equal(0, adapter.Calls);
        Assert.Empty(work.Admission.CommittedReservations);
    }

    /// <summary>
    /// With stub prices binding mid-production, the counted total NEVER EXCEEDS the cap: calls are admitted while
    /// their worst case fits, every attempt — the timed-out ones included — counts, and the call that would pass the
    /// cap is not made.
    /// </summary>
    [Fact]
    public async Task WithPricesBindingMidProductionTheCountedTotalNeverPassesTheCap()
    {
        var work = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _, cap: 0.10m);
        var calls = 0;
        var adapter = new ScriptedAdapter(Account, () => ++calls % 2 == 0
            ? VendorCalls.Unknown(TimeSpan.FromSeconds(1), "timed out", ProviderFailureSignal.Outage)
            : ProductionHarness.Succeeded());
        var gateway = ProductionHarness.Gateway(work, adapter);

        var outcomes = new List<CapabilityOutcome>();
        for (var i = 0; i < 6; i++)
        {
            outcomes.Add(await gateway.ExecuteAsync(ProductionHarness.NarrationRequest(new string('a', 1_500)), ProductionHarness.Context(), CancellationToken.None));
        }

        // 1,500 characters at the stub price is USD 0.0225 each: four fit under USD 0.10, the fifth would pass it.
        Assert.Equal(4, adapter.Calls);
        Assert.Equal(4, work.Admission.CommittedReservations.Count);
        Assert.All(outcomes.Skip(4), o => Assert.Equal(RefusalReason.ItemCapExceeded, Assert.IsType<CapabilityOutcome.Refused>(o).Reason));
        var counted = work.Admission.CommittedReservations.Sum(r => r.WorstCaseAmount.Amount);
        Assert.True(counted <= 0.10m, $"the counted total {counted} passed the cap");
    }

    /// <summary>
    /// A RESERVATION COMMITTED AND THE BOOKING NEVER WRITTEN keeps counting at its worst case: after an attempt whose
    /// recording was lost and could not be recovered, the next admission of the item counts the open reservation, and
    /// refuses a call that would pass the cap with it.
    /// </summary>
    [Fact]
    public async Task AReservationWhoseBookingWasNeverWrittenKeepsCountingAgainstTheCap()
    {
        var work = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _, cap: 0.05m);
        var adapter = new ScriptedAdapter(Account, ProductionHarness.Succeeded);
        var gateway = ProductionHarness.Gateway(work, adapter);

        // The admission transaction is lost after the call, and the fresh recording fails as well.
        work.Admission.LoseNextOperationWrite = true;
        work.FailNextCommit = false;
        var failingRecovery = new FailingRecoveryWork(work);
        var lostGateway = ProductionHarness.Gateway(failingRecovery, adapter);
        await Assert.ThrowsAsync<IncurredAttemptNotRecordedException>(() => lostGateway.ExecuteAsync(
            ProductionHarness.NarrationRequest(new string('a', 2_000)), ProductionHarness.Context(), CancellationToken.None));

        Assert.Single(work.Admission.CommittedReservations);
        Assert.Empty(work.Operations);

        // USD 0.03 is reserved and never booked; a second call worth USD 0.03 would take the count past USD 0.05.
        var refused = Assert.IsType<CapabilityOutcome.Refused>(await gateway.ExecuteAsync(
            ProductionHarness.NarrationRequest(new string('b', 2_000)), ProductionHarness.Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.ItemCapExceeded, refused.Reason);
        Assert.Contains("1 open reservation(s)", refused.Detail, StringComparison.Ordinal);
        Assert.Equal(1, adapter.Calls);
    }

    /// <summary>A reservation that cannot be committed ends the attempt with NO CALL.</summary>
    [Fact]
    public async Task AReservationThatCannotBeCommittedEndsTheAttemptWithNoCall()
    {
        var work = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _, cap: 5.95m);
        work.Admission.FailNextReservation = true;
        var adapter = new ScriptedAdapter(Account, ProductionHarness.Succeeded);

        var refused = Assert.IsType<CapabilityOutcome.Refused>(await ProductionHarness.Gateway(work, adapter).ExecuteAsync(
            ProductionHarness.NarrationRequest("text"), ProductionHarness.Context(), CancellationToken.None));

        Assert.Equal(0, adapter.Calls);
        Assert.Contains("no call was made", refused.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// A CHANNEL WITH NO BUDGET AMOUNT is admitted, for an item with a recorded cap, against the company ceiling and
    /// the cap alone: the channel reading demands no action and RECORDS NO AMOUNT. Uncapped, it is refused as
    /// delivered.
    /// </summary>
    [Fact]
    public async Task AChannelWithNoBudgetAmountIsGovernedByTheCeilingAndTheCapAlone()
    {
        var capped = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _, cap: 5.95m);
        capped.Admission.ChannelBudget = null;
        var adapter = new ScriptedAdapter(Account, ProductionHarness.Succeeded);

        var admitted = Assert.IsType<CapabilityOutcome.Completed>(await ProductionHarness.Gateway(capped, adapter).ExecuteAsync(
            ProductionHarness.NarrationRequest("text"), ProductionHarness.Context(), CancellationToken.None));

        var channel = Assert.Single(capped.Decisions).Controller.Readings.Single(r => r.Scope == GoverningScope.Channel);
        Assert.Equal(ControllerAction.None, channel.Action);
        Assert.IsType<RecordedAmount.NotRecorded>(channel.Amount);
        Assert.Equal(OperationOutcome.Succeeded, admitted.Operation.Outcome);

        var uncapped = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _);
        uncapped.Admission.ChannelBudget = null;
        var refused = Assert.IsType<CapabilityOutcome.Refused>(await ProductionHarness.Gateway(uncapped, adapter).ExecuteAsync(
            ProductionHarness.NarrationRequest("text"), ProductionHarness.Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.BudgetAmountNotRecorded, refused.Reason);
        Assert.Equal(1, adapter.Calls);
    }

    /// <summary>A call the controller DEFERS is not made, though it fits the remaining cap.</summary>
    [Fact]
    public async Task ACallTheControllerDefersIsNotMadeThoughItFitsTheCap()
    {
        var work = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _, cap: 5.95m);
        work.Admission.CompanySpend = new ScopeSpend(1, 0, new Money(32m));
        var adapter = new ScriptedAdapter(Account, ProductionHarness.Succeeded);

        var held = Assert.IsType<CapabilityOutcome.Held>(await ProductionHarness.Gateway(work, adapter).ExecuteAsync(
            ProductionHarness.NarrationRequest("text"), ProductionHarness.Context(), CancellationToken.None));

        Assert.Equal(RefusalReason.DeferredAtThreshold, held.Reason);
        Assert.Equal(0, adapter.Calls);
    }

    /// <summary>
    /// A COMPANY-LEVEL ACCOUNT's credential is issued with NO CHANNEL, so its variable name is the GLOBAL form; a
    /// channel-scoped account keeps the delivered channel form.
    /// </summary>
    [Fact]
    public async Task ACompanyLevelAccountIsIssuedUnderTheGlobalNameAndItsRecordedScheme()
    {
        var work = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out var registers, cap: 5.95m);
        registers.Accounts[0] = registers.Accounts[0] with { Scope = CredentialScope.Company, Scheme = AuthenticationScheme.KeyHeader };
        var broker = new RecordingBroker();
        var gateway = new CapabilityGateway(work, broker, [new ScriptedAdapter(Account, ProductionHarness.Succeeded)], TimeSpan.FromSeconds(60));

        await gateway.ExecuteAsync(ProductionHarness.NarrationRequest("text"), ProductionHarness.Context(), CancellationToken.None);

        var request = Assert.Single(broker.Requests);
        Assert.Null(request.Channel);
        Assert.Equal(AuthenticationScheme.KeyHeader, request.Scheme);
        Assert.Equal("MEDIACOMPANY_SECRET_SPEECH_ACCOUNT__GLOBAL", CredentialBrokerFactory.VariableName(Account, request.Channel));
    }

    /// <summary>
    /// A RECOVERED attempt is marked recovered, so a production makes no further call after it; its content, where
    /// the vendor produced some, still comes back with it.
    /// </summary>
    [Fact]
    public async Task ARecoveredAttemptIsMarkedRecovered()
    {
        var work = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _, cap: 5.95m);
        work.Admission.LoseNextOperationWrite = true;

        var recovered = Assert.IsType<CapabilityOutcome.Completed>(await ProductionHarness.Gateway(work, new ScriptedAdapter(Account, ProductionHarness.Succeeded))
            .ExecuteAsync(ProductionHarness.NarrationRequest("text"), ProductionHarness.Context(), CancellationToken.None));

        Assert.True(recovered.Recovered);
        Assert.NotNull(recovered.Content);
        Assert.Equal(recovered.Operation.Id, Assert.Single(work.Admission.CommittedReservations).Operation);
    }

    /// <summary>A completed outcome's content is the adapter's buffer: its length and hash are the received bytes'.</summary>
    [Fact]
    public async Task ACompletedOutcomeCarriesTheReceivedBytesAndEveryOtherOutcomeCarriesNone()
    {
        var work = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _, cap: 5.95m);
        var completed = Assert.IsType<CapabilityOutcome.Completed>(await ProductionHarness.Gateway(work, new ScriptedAdapter(Account, ProductionHarness.Succeeded))
            .ExecuteAsync(ProductionHarness.NarrationRequest("text"), ProductionHarness.Context(), CancellationToken.None));

        Assert.Equal(ProductionHarness.ContentBytes, completed.Content!.Bytes.ToArray());
        Assert.Equal(ProductionHarness.ContentBytes.Length, completed.Content.Length);
        Assert.Equal(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(ProductionHarness.ContentBytes)), completed.Content.Sha256);

        var failed = Assert.IsType<CapabilityOutcome.Completed>(await ProductionHarness.Gateway(work, new ScriptedAdapter(Account,
            () => VendorCalls.Unknown(TimeSpan.Zero, "failed", ProviderFailureSignal.Transient) with { Content = new ProducedContent(ContentKind.Audio, "audio/wav", new byte[] { 1 }) }))
            .ExecuteAsync(ProductionHarness.NarrationRequest("text"), ProductionHarness.Context(), CancellationToken.None));
        Assert.Null(failed.Content);

        // Refused, held, deferred and substituted outcomes have no content member at all, by construction.
        Assert.Null(typeof(CapabilityOutcome.Refused).GetProperty("Content"));
        Assert.Null(typeof(CapabilityOutcome.Held).GetProperty("Content"));
        Assert.Null(typeof(CapabilityOutcome.Substituted).GetProperty("Content"));
    }

    // -----------------------------------------------------------------------

    /// <summary>An adapter double scripted per call; it reaches nothing.</summary>
    internal sealed class ScriptedAdapter(ProviderAccountId account, Func<ProviderAttempt> answer) : IProviderAdapter
    {
        public int Calls { get; private set; }

        public ProviderAccountId ProviderAccount { get; } = account;

        public Task<ProviderAttempt> InvokeAsync(RouteTarget.ProviderRoute route, CapabilityRequest request, ScopedHandle handle, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(answer());
        }
    }

    /// <summary>A broker double recording what it is asked for; it holds no secret.</summary>
    private sealed class RecordingBroker : ICredentialBroker
    {
        public List<CredentialRequest> Requests { get; } = [];

        public Task<CredentialOutcome> IssueAsync(CredentialRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult<CredentialOutcome>(new CredentialOutcome.Issued(new ScopedHandle(
                Guid.NewGuid(), request.CredentialClass, request.ProviderAccount, request.Channel, request.Capability, request.Job,
                request.Stage, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), 1)));
        }
    }

    /// <summary>The same in-memory store, whose fresh recording transaction always fails: an unrecoverable recording.</summary>
    private sealed class FailingRecoveryWork(FakeUnitOfWork inner) : IUnitOfWork
    {
        public Task<IWorkTransaction> BeginAsync(CancellationToken cancellationToken) => inner.BeginAsync(cancellationToken);

        public Task<IWorkTransaction> BeginAsync(TimeSpan commandTimeout, CancellationToken cancellationToken) =>
            throw new TimeoutException("induced: the recording transaction could not be opened");
    }
}

/// <summary>The in-memory composition the production tests share: one narration route, its price, and an optional cap.</summary>
internal static class ProductionHarness
{
    /// <summary>A stub price per character, a demonstration parameter.</summary>
    internal const decimal CharacterPrice = 0.000015m;

    internal static readonly byte[] ContentBytes = FakeSpeechAudio.Wave("harness");

    internal static readonly DateTimeOffset Reserved = DateTimeOffset.Parse("2026-10-20T12:00:00Z");

    internal static FakeUnitOfWork Work(ProviderAccountId account, string model, CapabilityClass capability, out FakeRegisters registers, decimal? cap = null, decimal booked = 0m)
    {
        var routes = new FakeRouteRegistry();
        var availability = new FakeAvailabilityLedger();
        registers = new FakeRegisters();
        var work = new FakeUnitOfWork();
        work.Admission.Instant = Reserved;
        work.Admission.Headroom = new Money(100m);
        var modelId = new ModelId(model);
        work.Admission.Prices.Add(new ModelPrice(
            ModelPriceId.New(), modelId, PriceUnitKind.CharacterUnit, CharacterPrice, "USD", "ESTIMATE, a demonstration parameter",
            new DateOnly(2026, 10, 1), Reserved.AddDays(-10), null));
        work.Admission.BilledKinds[modelId] = [PriceUnitKind.CharacterUnit];
        registers.Accounts.Add(new ProviderAccount(account, "vendor", "pay-per-use terms", new DateOnly(2026, 10, 9), ProviderAccountStatus.Active));

        var route = new Route(
            RouteId.New(), capability, RouteTier.Primary, new RouteTarget.ProviderRoute(account, modelId),
            new QualityRating(0), new ContextCapacity(0), "pay-per-use terms", new DateOnly(2026, 10, 9))
        {
            TermsPositions = new RouteTermsPositions(AutomatedAccessPosition.Permits, CustomerContentPosition.NoLicenceTaken, "fixture terms", new DateOnly(2026, 10, 9)),
        };
        routes.Routes.Add(route);
        availability.States[route.Id] = RouteAvailability.Serving(route.Id, Reserved.AddDays(-1));

        if (cap is { } amount)
        {
            work.Admission.ItemCap = new ItemCapReading
            {
                Cap = new Money(amount),
                Source = "the owner's cap, a demonstration parameter",
                Booked = new Money(booked),
                UnstatedOperations = 0,
                OpenReservations = Money.Zero(),
                OpenReservationCount = 0,
            };
        }

        work.Admission.Booked = () => work.Operations.Select(o => o.Id).ToArray();
        return work.Bind(routes, availability, registers);
    }

    internal static CapabilityGateway Gateway(IUnitOfWork work, IProviderAdapter adapter) =>
        new(work, new OpenBroker(), [adapter], TimeSpan.FromSeconds(60));

    internal static ProviderAttempt Succeeded() =>
        new(true, UnitCounts.None, CostBasis.Estimate, TimeSpan.FromMilliseconds(5), null, null)
        {
            Content = new ProducedContent(ContentKind.Audio, "audio/wav", ContentBytes),
            ChargeKnown = false,
        };

    internal static CapabilityRequest NarrationRequest(string text) => new(
        CapabilityClass.Narration, ReasoningTier.Light, new QualityRating(0), new ContextCapacity(0), new Money(5.95m), Criticality.Routine,
        new Attribution(Item, Channel, DepartmentId.New(), AgentId.New()), new EstimatedUnits(0, 0, 0) { CharacterUnits = text.Length },
        TimeSpan.Zero, ReducedFloorPolicy.Forbidden)
    {
        Payload = new CapabilityPayload.Narration(text, "voice", "wav"),
    };

    internal static CapabilityInvocationContext Context() => new()
    {
        Run = RunId.New(),
        Job = JobId.New(),
        Stage = LifecyclePosition.Assembly,
        Actor = WorkforceRole.Producer,
        Attempt = 1,
    };

    private static readonly ItemId Item = ItemId.New();
    private static readonly ChannelId Channel = ChannelId.New();

    /// <summary>A broker double that issues a handle and holds no secret.</summary>
    private sealed class OpenBroker : ICredentialBroker
    {
        public Task<CredentialOutcome> IssueAsync(CredentialRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<CredentialOutcome>(new CredentialOutcome.Issued(new ScopedHandle(
                Guid.NewGuid(), request.CredentialClass, request.ProviderAccount, request.Channel, request.Capability, request.Job,
                request.Stage, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), 1)));
    }
}
