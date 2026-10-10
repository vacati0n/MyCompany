using MediaCompany.Application.Ports;
using MediaCompany.Application.Production;
using MediaCompany.Deterministic.Production;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Deterministic.Record;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Configuration;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The multi-channel capability against the record store, over TWO FIXTURE CHANNELS of one fixture
/// company.
///
/// EVERY DEMONSTRATION RUNS AGAINST A THROWAWAY STORE THAT IT DROPS AND RECREATES, in the separate
/// demonstration database. Every company, channel, configuration value, budget, asset, library
/// registration, condition, approval and operation below is a DEMONSTRATION FIXTURE that exists only
/// there; nothing in production code, configuration, seed data or migration records any of them, and
/// every unit count, amount and percentage is a demonstration parameter, not an observation of the
/// company's work.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class MultiChannelIntegrationTests : IAsyncLifetime
{
    private NpgsqlDataSource? _dataSource;
    private readonly SteppingClock _clock = SteppingClock.HoursAgo(6);

    private NpgsqlDataSource Source => _dataSource
        ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    private static readonly CompanyId Company = CompanyId.New();
    private static readonly ChannelId ChannelA = ChannelId.New();
    private static readonly ChannelId ChannelB = ChannelId.New();
    private static readonly DepartmentId Department = DepartmentId.New();
    private static readonly AgentId Agent = AgentId.New();
    private static readonly ItemId ItemA = ItemId.New();
    private static readonly ItemId ItemB = ItemId.New();
    private static readonly ItemVersion Version = new(1);
    private static readonly ModelId ModelName = new("alpha-reasoning");

    public async Task InitializeAsync()
    {
        if (PostgresIntegrationTests.ConnectionString is null)
        {
            return;
        }

        _dataSource = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString);

        try
        {
            await ThrowawayStore.DropAsync(_dataSource);
            await SchemaInstaller.InstallAsync(_dataSource, CancellationToken.None);
            await SeedAsync();
        }
        catch
        {
            await ThrowawayStore.DropAsync(_dataSource);
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_dataSource is not null)
        {
            try
            {
                await ThrowawayStore.DropAsync(_dataSource);
            }
            finally
            {
                await _dataSource.DisposeAsync();
            }
        }
    }

    // -----------------------------------------------------------------------
    // The sixth schema resource
    // -----------------------------------------------------------------------

    /// <summary>
    /// The sixth resource installs last and records NOTHING but the chain-head row: no channel, no
    /// configuration value, no budget, no condition, no payment-account observation. Applying it a
    /// second time creates no duplicate and fails nothing.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheSixthResourceRecordsNothingButTheChainHeadAndReappliesCleanly()
    {
        // The seventh and eighth resources now install after it (the AI-economics and AI-management changes);
        // re-applying the sixth over a store holding both must still succeed and record nothing.
        // The ninth resource (the production change) now installs last; the sixth is still the sixth.
        Assert.Equal("MediaCompany.Persistence.Schema.006-multi-channel.sql", SchemaInstaller.ResourceNames[^4]);
        Assert.Equal("MediaCompany.Persistence.Schema.006-multi-channel.sql", SchemaInstaller.ResourceNames[5]);

        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.006-multi-channel.sql"));

        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM company_payment_account_observations"));
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM audit_chain_head"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM configuration"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM budgets"));
        Assert.Equal(1L, await ScalarAsync<long>(
            "SELECT COUNT(*) FROM pg_constraint WHERE conname = 'first_publication_conditions_no_new_channel_payment_account' AND NOT convalidated"));
        foreach (var trigger in new[]
                 {
                     "audit_entries_chained_to_head", "agent_costs_booked_under_horizon", "gate_transitions_refuse_unrecorded_from_state",
                 })
        {
            Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM pg_trigger WHERE tgname = @name", c => c.Parameters.AddWithValue("name", trigger)));
        }
    }

    // -----------------------------------------------------------------------
    // Per-channel attributes, and a channel brought into being by register and configuration alone
    // -----------------------------------------------------------------------

    /// <summary>
    /// Each attribute recorded for one channel reads back for that channel through the temporal
    /// register and changes nothing read for the other; a superseded value stays in the history; an
    /// attribute never recorded reads not recorded; and the store refuses every key-and-scope pairing
    /// but a channel key under one channel's scope.
    /// </summary>
    [RequiresPostgresFact]
    public async Task EachChannelsAttributesRoundTripIndependentlyThroughTheTemporalRegister()
    {
        var store = new NpgsqlConfigurationStore(Source);
        var start = DateTimeOffset.UtcNow.AddHours(-3);

        await store.SupersedeAsync(Value(ChannelConfigurationKeys.Audience, ChannelA, "fixture audience A", 1, start), CancellationToken.None);
        await store.SupersedeAsync(Value(ChannelConfigurationKeys.Audience, ChannelB, "fixture audience B", 1, start), CancellationToken.None);
        await store.SupersedeAsync(Value(ChannelConfigurationKeys.Tone, ChannelA, "fixture tone A", 1, start), CancellationToken.None);
        await store.SupersedeAsync(Value(ChannelConfigurationKeys.Audience, ChannelA, "fixture audience A, revised", 2, start.AddHours(1)), CancellationToken.None);

        var profiles = await Profiles().ProfilesAsync(CancellationToken.None);
        var a = profiles.Single(p => p.Channel.Equals(ChannelA));
        var b = profiles.Single(p => p.Channel.Equals(ChannelB));

        Assert.Equal("fixture audience A, revised", Assert.IsType<ChannelAttribute.Recorded>(a.Audience).Value);
        Assert.Equal("fixture audience B", Assert.IsType<ChannelAttribute.Recorded>(b.Audience).Value);
        Assert.Equal("fixture tone A", Assert.IsType<ChannelAttribute.Recorded>(a.Tone).Value);
        Assert.IsType<ChannelAttribute.NotRecorded>(b.Tone);
        Assert.IsType<ChannelAttribute.NotRecorded>(a.Schedule);
        Assert.Equal(2, (await store.HistoryAsync(ChannelConfigurationKeys.Audience, ChannelConfigurationKeys.ScopeFor(ChannelA), CancellationToken.None)).Count);
        Assert.Single(await store.HistoryAsync(ChannelConfigurationKeys.Audience, ChannelConfigurationKeys.ScopeFor(ChannelB), CancellationToken.None));

        // Language from the register row, library registration from its own register.
        Assert.Equal("en", a.Language);
        Assert.Equal("vi", b.Language);
        Assert.Empty(a.LibraryRegistrations);

        // Every pairing but a channel key under one channel's scope is refused.
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SupersedeAsync(
            Value(ConfigurationKeys.BudgetAmount, ChannelA, "a base key under a channel scope", 1, start), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SupersedeAsync(
            Value(ChannelConfigurationKeys.Audience, ChannelA, "a channel key under a non-channel scope", 1, start) with { Scope = "company" },
            CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SupersedeAsync(
            Value("channel.payee", ChannelA, "a key naming the payee", 1, start), CancellationToken.None));

        Assert.Equal(4L, await ScalarAsync<long>("SELECT COUNT(*) FROM configuration"));
    }

    /// <summary>
    /// A fixture channel brought into being by a register row and a configuration row ALONE, with no
    /// source change, is read by every partitioned reading, composes a profile, and stays refused and
    /// unrouted: no unit of work is enqueued for it, and publish evaluation for its item refuses.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AChannelBroughtIntoBeingByRegisterAndConfigurationRowsIsPartitionedRefusedAndUnrouted()
    {
        var third = ChannelId.New();
        var item = ItemId.New();
        await ExecuteAsync(
            """
            INSERT INTO channels (channel_id, company_id, platform, language, registered) VALUES (@channel, @company, 'video-platform', 'en', false);
            INSERT INTO items (item_id, channel_id, item_version, title) VALUES (@item, @channel, 1, 'fixture item of the third channel');
            """,
            c =>
            {
                c.Parameters.AddWithValue("channel", third.Value);
                c.Parameters.AddWithValue("company", Company.Value);
                c.Parameters.AddWithValue("item", item.Value);
            });
        await new NpgsqlConfigurationStore(Source).SupersedeAsync(
            Value(ChannelConfigurationKeys.Schedule, third, "fixture schedule text, read by nothing", 1, DateTimeOffset.UtcNow.AddHours(-1)),
            CancellationToken.None);
        await AppendObservationStartAsync();

        var month = await ChannelTestKit.BookingMonthAsync(Source);
        var analytics = ChannelTestKit.Analytics(Source, _clock);

        Assert.Contains((await analytics.OperationsByChannelAsync(month, CancellationToken.None)).Channels, c => third.Equals(c.Channel));
        Assert.Contains((await analytics.DossiersByChannelAsync(month, CancellationToken.None)).Channels, c => third.Equals(c.Channel));
        Assert.Contains((await analytics.BudgetsByChannelAsync(month, CancellationToken.None)).Channels, c => third.Equals(c.Channel));
        Assert.Contains(await analytics.ApprovalListingsAsync(CancellationToken.None), l => third.Equals(l.Channel));

        var profile = await Profiles().ProfileAsync(third, CancellationToken.None);
        Assert.IsType<ChannelAttribute.Recorded>(profile!.Schedule);
        Assert.Contains("3 channel(s)", profile.Risk.SharedPayeeStatement, StringComparison.Ordinal);

        var verdict = await Gate().EvaluatePublishAsync(item, Version, third, WorkforceRole.Publisher, CancellationToken.None);
        Assert.IsType<GateVerdict.Refused>(verdict);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM jobs WHERE channel_id = @c", c => c.Parameters.AddWithValue("c", third.Value)));
    }

    // -----------------------------------------------------------------------
    // One payee
    // -----------------------------------------------------------------------

    /// <summary>
    /// The payment-account condition reads IDENTICALLY for both channels of the company, before and
    /// after one company-level observation is recorded; a per-channel payment-account row is refused;
    /// and the other two conditions stay each channel's own.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ThePaymentAccountConditionResolvesFromOneCompanyLevelRecordForEveryChannel()
    {
        var ledger = new NpgsqlGateLedger(Source);

        async Task<(ConditionStanding A, ConditionStanding B)> PaymentAsync() => (
            (await ledger.FirstPublicationConditionsAsync(ChannelA, CancellationToken.None)).Evaluate().Single(s => s.Condition == FirstPublicationCondition.PaymentAccount),
            (await ledger.FirstPublicationConditionsAsync(ChannelB, CancellationToken.None)).Evaluate().Single(s => s.Condition == FirstPublicationCondition.PaymentAccount));

        var before = await PaymentAsync();
        Assert.Equal(ConditionResolution.Absent, before.A.Resolution);
        Assert.Equal(before.A, before.B with { });

        await ChannelTestKit.RecordConditionFixtureAsync(Source, ChannelA, FirstPublicationCondition.PaymentAccount, ConditionState.Satisfied);

        var after = await PaymentAsync();
        Assert.Equal(ConditionResolution.Satisfied, after.A.Resolution);
        Assert.Equal(after.A.Resolution, after.B.Resolution);
        Assert.Equal(after.A.Observation, after.B.Observation);

        var refused = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO first_publication_conditions (channel_id, condition, state, evidence, observed_on)
            VALUES (@channel, 'PaymentAccount', 'Satisfied', 'a per-channel payee value', CURRENT_DATE)
            """,
            c => c.Parameters.AddWithValue("channel", ChannelB.Value)));
        Assert.Equal("first_publication_conditions_no_new_channel_payment_account", refused.ConstraintName);

        await ChannelTestKit.RecordConditionFixtureAsync(Source, ChannelA, FirstPublicationCondition.LibraryRegistration, ConditionState.Satisfied);
        Assert.Equal(ConditionResolution.Satisfied, (await ledger.FirstPublicationConditionsAsync(ChannelA, CancellationToken.None))
            .Evaluate().Single(s => s.Condition == FirstPublicationCondition.LibraryRegistration).Resolution);
        Assert.Equal(ConditionResolution.Absent, (await ledger.FirstPublicationConditionsAsync(ChannelB, CancellationToken.None))
            .Evaluate().Single(s => s.Condition == FirstPublicationCondition.LibraryRegistration).Resolution);
    }

    /// <summary>
    /// A per-channel payment-account observation recorded BEFORE the sixth resource is retained and
    /// readable in its table after it, and is no longer read to resolve the condition, which reads
    /// absent until the company-level observation is recorded.
    /// </summary>
    [RequiresPostgresFact]
    public async Task APerChannelPaymentObservationRecordedBeforeTheChangeIsRetainedAndNotRead()
    {
        await ThrowawayStore.DropAsync(Source);
        foreach (var resource in SchemaInstaller.ResourceNames.Take(5))
        {
            await ExecuteAsync(SchemaInstaller.ReadResource(resource));
        }

        await SeedAsync();
        await ExecuteAsync(
            """
            INSERT INTO first_publication_conditions (channel_id, condition, state, evidence, observed_on)
            VALUES (@channel, 'PaymentAccount', 'Satisfied', 'an observation recorded before the change', CURRENT_DATE - 3)
            """,
            c => c.Parameters.AddWithValue("channel", ChannelA.Value));

        // The sixth resource by name, then the seventh and the eighth, which now install after it (the AI-economics
        // and AI-management changes).
        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.006-multi-channel.sql"));
        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.007-ai-economics.sql"));
        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.008-management.sql"));

        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM first_publication_conditions WHERE condition = 'PaymentAccount'"));
        Assert.Equal("an observation recorded before the change", await ScalarAsync<string>(
            "SELECT evidence FROM first_publication_conditions WHERE condition = 'PaymentAccount'"));
        Assert.Equal(ConditionResolution.Absent, (await new NpgsqlGateLedger(Source).FirstPublicationConditionsAsync(ChannelA, CancellationToken.None))
            .Evaluate().Single(s => s.Condition == FirstPublicationCondition.PaymentAccount).Resolution);
    }

    // -----------------------------------------------------------------------
    // The channel partition of every published reading
    // -----------------------------------------------------------------------

    /// <summary>
    /// Over two fixture channels and one channel identifier the register does not hold: every additive
    /// cost figure of the company equals the sum of its channel figures EXACTLY, the unregistered
    /// identifier is its own partition, and SIDE BY SIDE a registered channel with no operation reads
    /// its count as an observed zero in an observed month and unmeasured in a month the record was not
    /// yet observing.
    /// </summary>
    [RequiresPostgresFact]
    public async Task EveryOperationReadingPartitionsByChannelAndTheCompanyFigureIsTheExactSum()
    {
        var stray = ChannelId.New();
        await AppendObservationStartAsync();
        await RecordOperationAsync(ChannelA, ItemA, 1_000, 500);
        await RecordOperationAsync(ChannelA, ItemA, 2_000, 0);
        await RecordOperationAsync(ChannelA, ItemA, 0, 0, deterministic: true);
        var last = await RecordOperationAsync(stray, ItemB, 300, 7);

        var month = new DateOnly(last.OccurredAt.UtcDateTime.Year, last.OccurredAt.UtcDateTime.Month, 1);
        var reading = await ChannelTestKit.Analytics(Source, _clock).OperationsByChannelAsync(month, CancellationToken.None);

        decimal Amount(MeasurementQuantity q) => q switch
        {
            MeasurementQuantity.ObservedValue v => v.Amount,
            MeasurementQuantity.ObservedZero => 0m,
            _ => 0m,
        };

        var company = Assert.IsType<MeasurementQuantity.ObservedValue>(reading.Company.Cost).Amount;
        Assert.Equal(company, reading.Channels.Sum(c => Amount(c.Cost)));
        Assert.Equal(company, await ScalarAsync<decimal>("SELECT SUM(computed_cost) FROM agent_costs"));
        Assert.Equal(4m, Assert.IsType<MeasurementQuantity.ObservedValue>(reading.Company.Operations).Amount);
        Assert.Equal(
            Amount(reading.Company.CostByCapability[CapabilityClass.EditorialReasoning]),
            reading.Channels.Sum(c => c.CostByCapability.TryGetValue(CapabilityClass.EditorialReasoning, out var q) ? Amount(q) : 0m));

        var a = reading.Channels.Single(c => ChannelA.Equals(c.Channel));
        Assert.Equal(3m, Assert.IsType<MeasurementQuantity.ObservedValue>(a.Operations).Amount);
        Assert.IsType<MeasurementQuantity.ObservedZero>(a.DeterministicSetCost);

        var b = reading.Channels.Single(c => ChannelB.Equals(c.Channel));
        Assert.IsType<MeasurementQuantity.ObservedZero>(b.Operations);
        Assert.IsType<MeasurementQuantity.Unmeasured>(b.Cost);

        var strayReading = reading.Channels.Single(c => stray.Equals(c.Channel));
        Assert.Equal(ChannelPartitionStanding.NotInRegister, strayReading.Standing);
        Assert.IsType<MeasurementQuantity.ObservedValue>(strayReading.Cost);

        Assert.False(reading.Finality.IsFinal);
        Assert.StartsWith("not yet final", reading.Finality.Statement, StringComparison.Ordinal);

        var earlier = await ChannelTestKit.Analytics(Source, _clock).OperationsByChannelAsync(month.AddMonths(-1), CancellationToken.None);
        var bEarlier = earlier.Channels.Single(c => ChannelB.Equals(c.Channel));
        Assert.Contains("not yet observing", Assert.IsType<MeasurementQuantity.Unmeasured>(bEarlier.Operations).Detail, StringComparison.Ordinal);
        Assert.True(earlier.Finality.IsFinal);
    }

    /// <summary>
    /// The item dossier register partitions through each item's channel, and the throughput reading
    /// through the work unit each lifecycle entry names, with an entry naming no unit counted as
    /// unattributed; the company counts are the sum of the partitions.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheDossierAndThroughputReadingsPartitionByChannel()
    {
        await AppendObservationStartAsync();
        var recorder = new ItemDossierRecorder(new NpgsqlUnitOfWork(Source, _clock), _clock);
        await recorder.OpenAsync(ItemA, Version, WorkforceRole.Producer, CancellationToken.None);

        var lifecycle = new WorkLifecycleService(new NpgsqlUnitOfWork(Source, _clock), _clock);
        var unitA = Unit(ItemA, ChannelA);
        var unitB = Unit(ItemB, ChannelB);
        await lifecycle.EnqueueAsync(unitA, CancellationToken.None);
        await lifecycle.EnqueueAsync(unitB, CancellationToken.None);
        Assert.NotNull(await lifecycle.ClaimUnitAsync(unitA.Id, "fixture-worker", TimeSpan.FromMinutes(5), CancellationToken.None));
        await AppendAsync(LifecycleActions.Claimed, "an entry naming no recorded unit");

        var month = await ChannelTestKit.BookingMonthAsync(Source);
        var analytics = ChannelTestKit.Analytics(Source, _clock);

        var dossiers = await analytics.DossiersByChannelAsync(month, CancellationToken.None);
        Assert.IsType<MeasurementQuantity.ObservedValue>(dossiers.Channels.Single(c => ChannelA.Equals(c.Channel)).DossiersOpened);
        Assert.IsType<MeasurementQuantity.ObservedZero>(dossiers.Channels.Single(c => ChannelB.Equals(c.Channel)).DossiersOpened);

        var throughput = await analytics.ThroughputByChannelAsync(await EarliestEntryAsync(), await DatastoreNowAsync(), CancellationToken.None);
        decimal Claims(ChannelThroughputReading r) => r.Throughput.Claimed is MeasurementQuantity.ObservedValue v ? v.Amount : 0m;

        var a = throughput.Channels.Single(c => ChannelA.Equals(c.Channel));
        var b = throughput.Channels.Single(c => ChannelB.Equals(c.Channel));
        var unattributed = throughput.Channels.Single(c => c.Standing == ChannelPartitionStanding.Unattributed);
        Assert.Equal(1m, Claims(a));
        Assert.IsType<MeasurementQuantity.ObservedZero>(b.Throughput.Claimed);
        Assert.Equal(1m, Assert.IsType<MeasurementQuantity.ObservedValue>(b.Throughput.Waiting).Amount);
        Assert.IsType<MeasurementQuantity.ObservedZero>(a.Throughput.Waiting);
        Assert.Equal(1m, Claims(unattributed));
        Assert.Equal(throughput.Channels.Sum(Claims), Claims(throughput.Company));
    }

    /// <summary>
    /// Budget utilisation per channel at the four thresholds: a channel with a fixture budget reads its
    /// utilisation, each crossing still raises exactly one alert however often it is evaluated, and a
    /// channel with no recorded budget reads unmeasured naming the missing budget; the company ceiling
    /// covers every channel's metered operations, says so, and reads the standing commitment unmeasured.
    /// </summary>
    [RequiresPostgresFact]
    public async Task BudgetUtilisationReadsPerChannelAndForTheCompanyCeiling()
    {
        var month = await ChannelTestKit.BookingMonthAsync(Source);
        await ExecuteAsync(
            "INSERT INTO budgets (budget_id, scope_kind, scope_id, period, amount, currency) VALUES (gen_random_uuid(), 'Channel', @channel, @period, 0.02, 'USD')",
            c =>
            {
                c.Parameters.AddWithValue("channel", ChannelA.Value);
                c.Parameters.Add("period", NpgsqlDbType.Date).Value = month;
            });

        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        for (var evaluation = 0; evaluation < 2; evaluation++)
        {
            await using var transaction = await unitOfWork.BeginAsync(CancellationToken.None);
            var operation = evaluation == 0
                ? await transaction.Operations.RecordAsync(Draft(ChannelA, ItemA, 1_000, 500), CancellationToken.None)
                : null;
            var booked = operation?.OccurredAt ?? await DatastoreNowAsync();
            await transaction.Budgets.EvaluateAsync(
                new Attribution(ItemA, ChannelA, Department, Agent),
                new DateOnly(booked.UtcDateTime.Year, booked.UtcDateTime.Month, 1), _clock.UtcNow, CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM budget_alerts WHERE threshold = 50"));

        var reading = await ChannelTestKit.Analytics(Source, _clock).BudgetsByChannelAsync(month, CancellationToken.None);

        var a = reading.Channels.Single(c => c.Channel.Equals(ChannelA));
        Assert.Equal(52.5m, Assert.IsType<MeasurementQuantity.ObservedValue>(a.Utilisation).Amount);
        Assert.IsType<MeasurementQuantity.ObservedValue>(a.Thresholds.Single(t => t.Threshold == BudgetThreshold.Fifty).Reached);
        Assert.IsType<MeasurementQuantity.ObservedZero>(a.Thresholds.Single(t => t.Threshold == BudgetThreshold.SeventyFive).Reached);
        Assert.Equal(1m, Assert.IsType<MeasurementQuantity.ObservedValue>(a.Thresholds.Single(t => t.Threshold == BudgetThreshold.Fifty).AlertsRecorded).Amount);

        var b = reading.Channels.Single(c => c.Channel.Equals(ChannelB));
        Assert.Contains("no budget is recorded", Assert.IsType<MeasurementQuantity.Unmeasured>(b.Utilisation).Detail, StringComparison.Ordinal);

        Assert.Equal(0.0105m, Assert.IsType<MeasurementQuantity.ObservedValue>(reading.Company.CompanyCost).Amount);
        // A recorded amount, never an observation (the AI-economics change, decision D-011).
        Assert.Equal(77.41m, Assert.IsType<RecordedAmount.Recorded>(reading.Company.Ceiling).Amount.Amount);
        Assert.Equal(0.02m, Assert.IsType<RecordedAmount.Recorded>(a.BudgetAmount).Amount.Amount);
        Assert.IsType<RecordedAmount.NotRecorded>(b.BudgetAmount);
        Assert.IsType<MeasurementQuantity.ObservedValue>(reading.Company.Utilisation);
        Assert.IsType<MeasurementQuantity.Unmeasured>(reading.Company.StandingCommitment);
        Assert.Contains("metered operations", reading.Company.CoverageStatement, StringComparison.Ordinal);
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM budgets"));
    }

    // -----------------------------------------------------------------------
    // The approval queue and workload
    // -----------------------------------------------------------------------

    /// <summary>
    /// Each channel's listing holds exactly its item versions whose latest recorded state is awaiting
    /// owner approval — none in another state and none of another channel — and the union over the
    /// channels is the company's set with no item twice. A channel with no decided approval reads every
    /// effort component unmeasured; a sent-back verdict is a change request.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheApprovalListingIsAViewOverGateStateAndTheWorkloadReadsPerChannel()
    {
        var a2 = await NewItemAsync(ChannelA);
        var b2 = await NewItemAsync(ChannelB);
        foreach (var (item, channel) in new[] { (ItemA, ChannelA), (a2, ChannelA), (ItemB, ChannelB), (b2, ChannelB) })
        {
            await ChannelTestKit.RecordRightsFixtureAsync(Source, item, channel);
        }

        var gate = Gate();
        await ChannelTestKit.PresentAsync(gate, ItemA, Version);
        await gate.SubmitForRightsCheckAsync(a2, Version, WorkforceRole.Producer, CancellationToken.None);
        var presentedB = await ChannelTestKit.PresentAsync(gate, ItemB, Version);
        await gate.RecordOwnerVerdictAsync(ItemB, Version, ApprovalVerdict.SentBack, "fixture change request", presentedB, CancellationToken.None);
        await ChannelTestKit.PresentAsync(gate, b2, Version);

        var analytics = ChannelTestKit.Analytics(Source, _clock);
        var listings = await analytics.ApprovalListingsAsync(CancellationToken.None);

        Assert.Equal([ItemA], listings.Single(l => l.Channel.Equals(ChannelA)).Entries.Select(e => e.Item));
        Assert.Equal([b2], listings.Single(l => l.Channel.Equals(ChannelB)).Entries.Select(e => e.Item));

        var union = listings.SelectMany(l => l.Entries).Select(e => e.Item).ToArray();
        var company = (await new NpgsqlApprovalQueueReader(Source).AwaitingOwnerApprovalAsync(CancellationToken.None)).Select(e => e.Item).ToArray();
        Assert.Equal(union.OrderBy(i => i.Value), company.OrderBy(i => i.Value));
        Assert.Equal(union.Length, union.Distinct().Count());

        var month = DateOnly.FromDateTime(DateTime.UtcNow);
        month = new DateOnly(month.Year, month.Month, 1);
        var workloadB = await analytics.ApprovalWorkloadAsync(ChannelB, month, CancellationToken.None);
        Assert.Equal(1m, Assert.IsType<MeasurementQuantity.ObservedValue>(workloadB.ChangeRequests).Amount);
        var effort = Assert.Single(workloadB.Efforts);
        Assert.Equal(ItemB, effort.Item);
        Assert.IsType<MeasurementQuantity.Unmeasured>(effort.Queue);

        var workloadA = await analytics.ApprovalWorkloadAsync(ChannelA, month, CancellationToken.None);
        Assert.IsType<MeasurementQuantity.ObservedZero>(workloadA.Approvals);
        var none = Assert.Single(workloadA.Efforts);
        Assert.All(new[] { none.Review, none.Queue, none.Rework }, q => Assert.IsType<MeasurementQuantity.Unmeasured>(q));
    }

    // -----------------------------------------------------------------------
    // Finality over the operation record
    // -----------------------------------------------------------------------

    /// <summary>
    /// A month reading taken while operations can still be booked into the month reads NOT YET FINAL;
    /// once the horizon passes the month's end it reads FINAL, and an operation recorded afterwards is
    /// booked into the open month and leaves the final reading unchanged. Every writer is bound: a
    /// direct insert below the horizon, into another month than its instant's, or an update moving an
    /// operation's month is refused by the datastore.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AMonthReadsFinalOnlyOnceNoOperationCanBeBookedIntoIt()
    {
        await AppendObservationStartAsync();
        var first = await RecordOperationAsync(ChannelA, ItemA, 1_000, 500);
        var month = new DateOnly(first.OccurredAt.UtcDateTime.Year, first.OccurredAt.UtcDateTime.Month, 1);
        var analytics = ChannelTestKit.Analytics(Source, _clock);

        var open = await analytics.PeriodCostAsync(month, CancellationToken.None);
        Assert.False(open.Finality!.IsFinal);

        // The horizon is moved past the month's end, as a read at that instant would move it.
        var next = new DateTimeOffset(month.AddMonths(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        await ExecuteAsync("UPDATE audit_record_horizon SET horizon = @at", c => c.Parameters.AddWithValue("at", next.AddSeconds(1)));

        var final = await analytics.OperationsByChannelAsync(month, CancellationToken.None);
        Assert.True(final.Finality.IsFinal);
        var finalCost = Assert.IsType<MeasurementQuantity.ObservedValue>(final.Company.Cost).Amount;

        var later = await RecordOperationAsync(ChannelA, ItemA, 5_000, 5_000);
        Assert.True(later.OccurredAt >= next);

        var again = await analytics.OperationsByChannelAsync(month, CancellationToken.None);
        Assert.True(again.Finality.IsFinal);
        Assert.Equal(finalCost, Assert.IsType<MeasurementQuantity.ObservedValue>(again.Company.Cost).Amount);
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM agent_costs WHERE period = @p", c => c.Parameters.Add("p", NpgsqlDbType.Date).Value = month.AddMonths(1)));

        var below = await Assert.ThrowsAsync<PostgresException>(() => InsertOperationDirectlyAsync("@horizon - interval '1 hour'", "date_trunc('month', @horizon - interval '1 hour')::date"));
        Assert.Equal("agent_costs_booked_under_horizon", below.ConstraintName);
        Assert.Contains("closed below", below.MessageText, StringComparison.Ordinal);

        var otherMonth = await Assert.ThrowsAsync<PostgresException>(() => InsertOperationDirectlyAsync("@horizon + interval '1 minute'", "@month"));
        Assert.Equal("agent_costs_booked_under_horizon", otherMonth.ConstraintName);

        var moved = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            "UPDATE agent_costs SET period = @p WHERE operation_id = @id",
            c =>
            {
                c.Parameters.Add("p", NpgsqlDbType.Date).Value = month.AddMonths(-1);
                c.Parameters.AddWithValue("id", first.Id.Value);
            }));
        Assert.Equal("agent_costs_booked_under_horizon", moved.ConstraintName);

        async Task InsertOperationDirectlyAsync(string instant, string period) => await ExecuteAsync(
            $"""
            INSERT INTO agent_costs (operation_id, run_id, occurred_at, attempt, item_id, channel_id, department_id, agent_id,
                                     capability_class, period, cost_basis, duration_ms, outcome)
            SELECT gen_random_uuid(), gen_random_uuid(), {instant}, 1, @item, @channel, @department, @agent,
                   'EditorialReasoning', {period}, 'Measurement', 0, 'Succeeded'
            FROM (SELECT horizon AS h FROM audit_record_horizon) x
            """.Replace("@horizon", "x.h"),
            c =>
            {
                c.Parameters.AddWithValue("item", ItemA.Value);
                c.Parameters.AddWithValue("channel", ChannelA.Value);
                c.Parameters.AddWithValue("department", Department.Value);
                c.Parameters.AddWithValue("agent", Agent.Value);
                c.Parameters.Add("month", NpgsqlDbType.Date).Value = month;
            });
    }

    // -----------------------------------------------------------------------
    // The audit chain
    // -----------------------------------------------------------------------

    /// <summary>
    /// EIGHT APPENDERS IN FLIGHT AT ONCE, each holding its transaction open across two appends, with
    /// throughput and month reads running beside them: the chain ends linear — no two entries share a
    /// predecessor, verification finds no broken link, and every appender's entries appear exactly once.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ConcurrentAppendersProduceOneLinearChain()
    {
        const int Appenders = 8;
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        var analytics = ChannelTestKit.Analytics(Source, _clock);
        var month = await ChannelTestKit.BookingMonthAsync(Source);
        var start = new TaskCompletionSource();

        var appenders = Enumerable.Range(0, Appenders).Select(async n =>
        {
            await start.Task;
            await using var transaction = await unitOfWork.BeginAsync(CancellationToken.None);
            await transaction.Audit.AppendAsync(Entry($"appender {n} first"), CancellationToken.None);
            await Task.Delay(20);
            await transaction.Audit.AppendAsync(Entry($"appender {n} second"), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }).ToArray();

        var readers = Enumerable.Range(0, 3).Select(async _ =>
        {
            await start.Task;
            for (var read = 0; read < 5; read++)
            {
                await analytics.ThroughputByChannelAsync(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow, CancellationToken.None);
                await analytics.OperationsByChannelAsync(month, CancellationToken.None);
            }
        }).ToArray();

        start.SetResult();
        await Task.WhenAll(appenders.Concat(readers));

        Assert.Equal(0L, await ScalarAsync<long>(
            "SELECT COUNT(*) FROM (SELECT previous_entry_hash FROM audit_entries GROUP BY previous_entry_hash HAVING COUNT(*) > 1) forks"));
        Assert.Equal(-1, AuditChain.FirstBrokenLink(await EntriesAsync()));
        for (var n = 0; n < Appenders; n++)
        {
            Assert.Equal(2L, await ScalarAsync<long>(
                "SELECT COUNT(*) FROM audit_entries WHERE reason LIKE @r", c => c.Parameters.AddWithValue("r", $"appender {n} %")));
        }
    }

    /// <summary>
    /// A writer BYPASSING the appender and naming a superseded head is refused by the datastore rather
    /// than forking the chain; an entry stamped below the horizon is still refused for that reason first.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AWriterBypassingTheAppenderIsRefusedRatherThanForkingTheChain()
    {
        await AppendAsync("fixture.first", "the first entry");
        await AppendAsync("fixture.second", "the second entry");

        var stale = await Assert.ThrowsAsync<PostgresException>(() => InsertEntryDirectlyAsync("GREATEST(clock_timestamp(), (SELECT horizon FROM audit_record_horizon))", genesis: true));
        Assert.Equal("audit_entries_chained_to_head", stale.ConstraintName);

        var below = await Assert.ThrowsAsync<PostgresException>(() => InsertEntryDirectlyAsync("(SELECT horizon FROM audit_record_horizon) - interval '1 day'", genesis: true));
        Assert.Contains("closed below", below.MessageText, StringComparison.Ordinal);

        Assert.Equal(2L, await ScalarAsync<long>("SELECT COUNT(*) FROM audit_entries"));
        Assert.Equal(-1, AuditChain.FirstBrokenLink(await EntriesAsync()));

        async Task InsertEntryDirectlyAsync(string instant, bool genesis) => await ExecuteAsync(
            $"""
            INSERT INTO audit_entries (entry_id, actor, action, subject, occurred_at, reason, inputs_reference,
                                       outputs_reference, decision, cost_reference, risk, retention_class,
                                       previous_entry_hash, entry_hash)
            VALUES (gen_random_uuid(), 'bypass', 'fixture.bypass', 'none', {instant}, 'a bypassing writer', 'none',
                    'none', 'none', 'none', 'none', 'OperationalRecord', {(genesis ? "''" : "'x'")}, md5(random()::text))
            """);
    }

    // -----------------------------------------------------------------------
    // The gate path, and the dossier recorder's first production caller
    // -----------------------------------------------------------------------

    /// <summary>
    /// A transition claiming a state the record does not hold is refused for EVERY WRITER — direct SQL
    /// and the delivered writer alike — while the delivered bypass check still refuses Published from
    /// anything but Approved for its own reason first.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ATransitionClaimingAStateTheRecordDoesNotHoldIsRefusedForEveryWriter()
    {
        var direct = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO gate_transitions (item_id, item_version, occurred_at, from_state, to_state, reason)
            VALUES (@item, 1, now(), 'AwaitingOwnerApproval', 'Approved', 'a claimed state')
            """,
            c => c.Parameters.AddWithValue("item", ItemA.Value)));
        Assert.Equal("gate_transitions_from_recorded_state", direct.ConstraintName);

        await using (var transaction = await new NpgsqlUnitOfWork(Source, _clock).BeginAsync(CancellationToken.None))
        {
            await Assert.ThrowsAsync<GateStateConflictException>(() => transaction.Gates.RecordTransitionAsync(
                ItemA, Version, GateState.AwaitingRightsCheck, GateState.AwaitingOwnerApproval, "a claimed state", _clock.UtcNow, CancellationToken.None));
        }

        var bypass = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO gate_transitions (item_id, item_version, occurred_at, from_state, to_state, reason)
            VALUES (@item, 1, now(), 'Draft', 'Published', 'a bypass')
            """,
            c => c.Parameters.AddWithValue("item", ItemA.Value)));
        Assert.Contains("gate bypass refused", bypass.MessageText, StringComparison.Ordinal);

        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM gate_transitions"));
    }

    /// <summary>
    /// THE PRODUCTION PATH. A new item in Draft, with a recorded asset decision, is carried by the
    /// rights-check step through awaiting rights check to awaiting owner approval on real recorded
    /// state, each transition audited; the dossier header and the copyright-check outcome are written
    /// through the recorder by production code and read back through the dossier reader; no operation
    /// is metered and no unit of work is enqueued; and once the owner approves, the dispatch the item
    /// reaches is refused by each undischarged first-publication condition, named individually.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ANewItemReachesOwnerApprovalThroughTheRightsCheckOnRealRecordedState()
    {
        await ChannelTestKit.RecordRightsFixtureAsync(Source, ItemA, ChannelA);

        var result = await RightsCheck().RunAsync(ItemA, Version, Subject(), CancellationToken.None);

        Assert.Equal(RightsCheckRest.PresentedForOwnerApproval, result.Rest);
        Assert.Equal(ChannelA, result.Channel);
        Assert.True(result.EvidenceRecordedByThisRun);
        Assert.Equal(
            new[] { "Draft>AwaitingRightsCheck", "AwaitingRightsCheck>AwaitingOwnerApproval" },
            await ColumnAsync("SELECT from_state || '>' || to_state FROM gate_transitions ORDER BY occurred_at"));
        Assert.Equal(1L, await CountActionAsync(PublicationGateService.SubmittedAction));
        Assert.Equal(1L, await CountActionAsync(PublicationGateService.PresentedAction));
        Assert.Equal(1L, await CountActionAsync(ItemDossierRecorder.OpenedAction));
        Assert.Equal(1L, await CountActionAsync(ItemDossierRecorder.RecordedAction));

        var recorded = await new NpgsqlItemDossierReader(Source).DossierAsync(ItemA, Version, CancellationToken.None);
        var stage = recorded!.Dossier.StageFor(ProductionStage.CopyrightCheck);
        Assert.Equal(StageOutcome.Succeeded, stage!.Outcome);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM agent_costs"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM jobs"));

        var gate = Gate();
        var presentedAt = await ScalarAsync<DateTime>("SELECT occurred_at FROM gate_transitions WHERE to_state = 'AwaitingOwnerApproval'");
        await gate.RecordOwnerVerdictAsync(
            ItemA, Version, ApprovalVerdict.Approved, "fixture verdict", new DateTimeOffset(presentedAt, TimeSpan.Zero), CancellationToken.None);

        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        var drive = await new PublishingSequenceService(
                new WorkLifecycleService(unitOfWork, _clock), gate, new PublicationDispatchService(unitOfWork, _clock), _clock)
            .DriveAsync(Request(ItemA, ChannelA), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.RefusedAtGateEvaluation, drive.Rest);
        var detail = await ScalarAsync<string>("SELECT refusal_detail FROM publication_attempts");
        foreach (var condition in Enum.GetNames<FirstPublicationCondition>())
        {
            Assert.Contains($"{condition} is NOT satisfied: NO observation is recorded", detail, StringComparison.Ordinal);
        }

        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM publication_dispatches"));
    }

    /// <summary>
    /// An item with NO recorded asset decision is sent back from the rights check, with the
    /// copyright-check outcome recorded as failed naming the absence; it is never presented.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnItemWithNoRecordedAssetDecisionIsSentBackAndNeverPresented()
    {
        var result = await RightsCheck().RunAsync(ItemB, Version, Subject(), CancellationToken.None);

        Assert.Equal(RightsCheckRest.SentBack, result.Rest);
        Assert.Equal(StageOutcome.Failed, result.Evidence!.Outcome);
        Assert.Contains("no asset decision is recorded", result.Evidence.Summary, StringComparison.Ordinal);
        Assert.Equal(GateState.SentBack, await new NpgsqlGateLedger(Source).CurrentStateAsync(ItemB, Version, CancellationToken.None));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM gate_transitions WHERE to_state = 'AwaitingOwnerApproval'"));
    }

    /// <summary>
    /// An interrupted run RESUMES FROM RECORDED STATE: with the dossier already opened and the version
    /// already awaiting the rights check, the step neither reopens nor resubmits, and presents.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnInterruptedRightsCheckResumesFromWhatWasRecorded()
    {
        await ChannelTestKit.RecordRightsFixtureAsync(Source, ItemA, ChannelA);
        await new ItemDossierRecorder(new NpgsqlUnitOfWork(Source, _clock), _clock).OpenAsync(ItemA, Version, WorkforceRole.Producer, CancellationToken.None);
        await Gate().SubmitForRightsCheckAsync(ItemA, Version, WorkforceRole.Producer, CancellationToken.None);

        var result = await RightsCheck().RunAsync(ItemA, Version, Subject(), CancellationToken.None);

        Assert.Equal(RightsCheckRest.PresentedForOwnerApproval, result.Rest);
        Assert.Single(result.GateSteps);
        Assert.Equal(1L, await CountActionAsync(ItemDossierRecorder.OpenedAction));
        Assert.Equal(1L, await CountActionAsync(PublicationGateService.SubmittedAction));
    }

    // -----------------------------------------------------------------------
    // The entry point's unclaimed rest, on the record store
    // -----------------------------------------------------------------------

    /// <summary>
    /// Another worker claims the drive's unit between two positions. The drive rests UNCLAIMED at the
    /// position it was due to run; in the throughput reading the resting unit counts as claimed and not
    /// as waiting; and a second drive starts a new unit rather than resuming the resting one.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ADriveWhoseUnitAnotherWorkerClaimsRestsUnclaimed()
    {
        await AppendObservationStartAsync();
        var inner = new NpgsqlUnitOfWork(Source, _clock);
        var stealing = new StealingUnitOfWork(inner, new WorkLifecycleService(inner, _clock), LifecyclePosition.PublishingTimingComputation);
        var sequence = new PublishingSequenceService(
            new WorkLifecycleService(stealing, _clock), Gate(), new PublicationDispatchService(stealing, _clock), _clock);

        var first = await sequence.DriveAsync(Request(ItemA, ChannelA), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.NotClaimable, first.Rest);
        Assert.Equal(LifecyclePosition.PublishingTimingComputation, first.RestingPosition);
        Assert.Equal("Claimed", await ScalarAsync<string>("SELECT claim_state FROM jobs WHERE job_id = @j", c => c.Parameters.AddWithValue("j", first.Unit.Value)));
        Assert.Equal("another-worker", await ScalarAsync<string>("SELECT claimed_by FROM jobs WHERE job_id = @j", c => c.Parameters.AddWithValue("j", first.Unit.Value)));

        var reading = await ChannelTestKit.Analytics(Source, _clock)
            .ThroughputByChannelAsync(await EarliestEntryAsync(), await DatastoreNowAsync(), CancellationToken.None);
        var a = reading.Channels.Single(c => ChannelA.Equals(c.Channel));
        Assert.IsType<MeasurementQuantity.ObservedZero>(a.Throughput.Waiting);
        Assert.Equal(3m, Assert.IsType<MeasurementQuantity.ObservedValue>(a.Throughput.Claimed).Amount);

        var second = await sequence.DriveAsync(Request(ItemA, ChannelA), CancellationToken.None);
        Assert.NotEqual(first.Unit, second.Unit);
        Assert.Equal("another-worker", await ScalarAsync<string>("SELECT claimed_by FROM jobs WHERE job_id = @j", c => c.Parameters.AddWithValue("j", first.Unit.Value)));
    }

    // -----------------------------------------------------------------------
    // The correction cycle: datastore order, one-read additivity, concurrency, resume
    // -----------------------------------------------------------------------

    /// <summary>
    /// LATEST IS THE DATASTORE'S ORDER. An owner verdict written with a caller instant EARLIER than
    /// the presentation's still becomes the item's current state, because the datastore positions each
    /// transition under the item hold; and a second, contradictory verdict from the same state is then
    /// refused, so no two transitions leave one state.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ATransitionWithAnEarlierCallerInstantIsStillLatestAndNoStateIsLeftTwice()
    {
        await ChannelTestKit.RecordRightsFixtureAsync(Source, ItemA, ChannelA);
        await ChannelTestKit.PresentAsync(Gate(), ItemA, Version);

        // A process whose clock runs twelve hours behind the presentation's.
        var behind = SteppingClock.HoursAgo(12);
        var lateGate = ChannelTestKit.Gate(Source, new NpgsqlUnitOfWork(Source, behind), behind);
        await lateGate.RecordOwnerVerdictAsync(ItemA, Version, ApprovalVerdict.Approved, "fixture verdict", behind.UtcNow, CancellationToken.None);

        Assert.Equal(GateState.Approved, await new NpgsqlGateLedger(Source).CurrentStateAsync(ItemA, Version, CancellationToken.None));

        await Assert.ThrowsAsync<GateStateConflictException>(() => lateGate.RecordOwnerVerdictAsync(
            ItemA, Version, ApprovalVerdict.SentBack, "a contradictory verdict", behind.UtcNow, CancellationToken.None));

        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM gate_transitions WHERE from_state = 'AwaitingOwnerApproval'"));
        Assert.Equal(0L, await ScalarAsync<long>(
            "SELECT COUNT(*) FROM (SELECT from_state FROM gate_transitions GROUP BY item_id, item_version, from_state HAVING COUNT(*) > 1) forks"));
    }

    /// <summary>
    /// EIGHT CONCURRENT GATE WRITERS of one item version, each submitting from the Draft it read:
    /// exactly one moves the item, every other is refused by name, and one transition leaves Draft.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ConcurrentGateWritersOfOneItemLeaveExactlyOneTransition()
    {
        var start = new TaskCompletionSource();
        var writers = Enumerable.Range(0, 8).Select(async _ =>
        {
            await start.Task;
            return await Gate().SubmitForRightsCheckAsync(ItemA, Version, WorkforceRole.Producer, CancellationToken.None);
        }).ToArray();

        start.SetResult();
        var outcomes = await Task.WhenAll(writers);

        Assert.Single(outcomes, o => o is GateStepOutcome.Moved);
        Assert.Equal(7, outcomes.Count(o => o is GateStepOutcome.Refused));
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM gate_transitions"));
        Assert.Equal(GateState.AwaitingRightsCheck, await new NpgsqlGateLedger(Source).CurrentStateAsync(ItemA, Version, CancellationToken.None));
    }

    /// <summary>
    /// METERED OPERATIONS CONCURRENT WITH APPENDERS AND CLOSURES: transactions holding the horizon
    /// shared while waiting for the chain head, appenders holding the chain head while taking the
    /// horizon shared, and month and throughput closures beside them all complete — no deadlock — with
    /// every operation booked and the chain linear.
    /// </summary>
    [RequiresPostgresFact]
    public async Task MeteredOperationsConcurrentWithAppendersAndClosuresAllComplete()
    {
        var month = await ChannelTestKit.BookingMonthAsync(Source);
        var analytics = ChannelTestKit.Analytics(Source, _clock);
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        var start = new TaskCompletionSource();

        var metered = Enumerable.Range(0, 6).Select(async n =>
        {
            await start.Task;
            await using var transaction = await unitOfWork.BeginAsync(CancellationToken.None);
            await transaction.Operations.RecordAsync(Draft(ChannelA, ItemA, 100 + n, 10), CancellationToken.None);
            await Task.Delay(15);
            await transaction.Audit.AppendAsync(Entry($"metered {n}"), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        });

        var appenders = Enumerable.Range(0, 6).Select(async n =>
        {
            await start.Task;
            await using var transaction = await unitOfWork.BeginAsync(CancellationToken.None);
            await transaction.Audit.AppendAsync(Entry($"appender {n}"), CancellationToken.None);
            await Task.Delay(15);
            await transaction.Operations.RecordAsync(Draft(ChannelB, ItemB, 200 + n, 0), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        });

        var closures = Enumerable.Range(0, 3).Select(async _ =>
        {
            await start.Task;
            for (var read = 0; read < 5; read++)
            {
                await analytics.OperationsByChannelAsync(month, CancellationToken.None);
                await analytics.ThroughputByChannelAsync(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow, CancellationToken.None);
            }
        });

        var all = Task.WhenAll(metered.Concat(appenders).Concat(closures).ToArray());
        start.SetResult();
        Assert.Same(all, await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(60))));
        await all;

        Assert.Equal(12L, await ScalarAsync<long>("SELECT COUNT(*) FROM agent_costs"));
        Assert.Equal(12L, await ScalarAsync<long>("SELECT COUNT(*) FROM audit_entries"));
        Assert.Equal(-1, AuditChain.FirstBrokenLink(await EntriesAsync()));
    }

    /// <summary>
    /// THE REPORT SUMS EXACTLY ON THE STORE, with an operation booked between the report's read and
    /// anything after it: every company line equals the sum of its channel lines, because both come
    /// from one read.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheReportsChannelLinesSumToItsCompanyLinesWithAWriteBetweenReads()
    {
        await AppendObservationStartAsync();
        await RecordOperationAsync(ChannelA, ItemA, 1_000, 500);
        await RecordOperationAsync(ChannelB, ItemB, 2_000, 0);
        var month = await ChannelTestKit.BookingMonthAsync(Source);

        var interleaving = new WriteAfterFirstRead(new NpgsqlChannelPartitionReader(Source), () => RecordOperationAsync(ChannelA, ItemA, 3_000, 3_000));
        var lines = await new ReportingService(interleaving, new NpgsqlCompanyRecordReader(Source)).MeasurableNowAsync(month, CancellationToken.None);

        decimal Amount(MeasurementQuantity q) => q is MeasurementQuantity.ObservedValue v ? v.Amount : 0m;
        var company = Amount(lines.Single(l => l.Name == "monthly-cost-total").Quantity);
        var channels = lines.Where(l => l.Name.StartsWith("monthly-cost-total:channel:", StringComparison.Ordinal)).Sum(l => Amount(l.Quantity));

        Assert.True(interleaving.Written);
        Assert.NotEqual(0m, company);
        Assert.Equal(company, channels);
    }

    /// <summary>
    /// RESUME WHERE EVIDENCE AND THE GATE DISAGREE: recorded copyright-check evidence reads releasable,
    /// and since it was recorded an incomplete asset decision was added, so the presentation's own rights
    /// check refuses; the step sends the version back naming the disagreement instead of refusing on
    /// every run.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AResumedRightsCheckWhoseEvidenceAndGateDisagreeIsSentBack()
    {
        await ChannelTestKit.RecordRightsFixtureAsync(Source, ItemA, ChannelA);
        var recorder = new ItemDossierRecorder(new NpgsqlUnitOfWork(Source, _clock), _clock);
        await recorder.OpenAsync(ItemA, Version, WorkforceRole.Producer, CancellationToken.None);
        await Gate().SubmitForRightsCheckAsync(ItemA, Version, WorkforceRole.Producer, CancellationToken.None);

        var dossiers = new NpgsqlItemDossierReader(Source);
        var evidence = await new CopyrightCheckStageHandler(dossiers, new NpgsqlAssetLedger(Source), _clock)
            .HandleAsync(ItemA, Version, Subject(), CancellationToken.None);
        Assert.Equal(StageOutcome.Succeeded, evidence.Evidence.Outcome);
        await recorder.RecordStageAsync(ItemA, Version, evidence.Evidence, WorkforceRole.Copyright, CancellationToken.None);

        await new NpgsqlAssetLedger(Source).RecordAsync(ChannelTestKit.CompleteAsset(ItemA) with { ProofOfLicenceReference = null }, CancellationToken.None);

        var first = await RightsCheck().RunAsync(ItemA, Version, Subject(), CancellationToken.None);

        Assert.Equal(RightsCheckRest.SentBack, first.Rest);
        Assert.False(first.EvidenceRecordedByThisRun);
        Assert.Equal(GateState.SentBack, await new NpgsqlGateLedger(Source).CurrentStateAsync(ItemA, Version, CancellationToken.None));
        Assert.Contains("recorded copyright-check evidence read releasable", await ScalarAsync<string>(
            "SELECT reason FROM gate_transitions WHERE to_state = 'SentBack'"), StringComparison.Ordinal);
    }

    /// <summary>A refused run reports no stage evidence where no check ran, rather than one at an invented instant.</summary>
    [RequiresPostgresFact]
    public async Task ARefusedRunReportsAnUnrunCheckAsAbsent()
    {
        await ChannelTestKit.RecordRightsFixtureAsync(Source, ItemA, ChannelA);
        await ChannelTestKit.PresentAsync(Gate(), ItemA, Version);

        var result = await RightsCheck().RunAsync(ItemA, Version, Subject(), CancellationToken.None);

        Assert.Equal(RightsCheckRest.Refused, result.Rest);
        Assert.Null(result.Evidence);
        Assert.Equal(GateState.AwaitingOwnerApproval, await new NpgsqlGateLedger(Source).CurrentStateAsync(ItemA, Version, CancellationToken.None));
    }

    /// <summary>
    /// A channel partition reader that books one operation right after the first read it serves, which
    /// is a write between the report's read and anything the report might read after it.
    /// </summary>
    private sealed class WriteAfterFirstRead(IChannelPartitionReader inner, Func<Task> write) : IChannelPartitionReader
    {
        public bool Written { get; private set; }

        public async Task<OperationPartitionSummary> OperationsAsync(DateOnly month, CancellationToken cancellationToken)
        {
            var summary = await inner.OperationsAsync(month, cancellationToken);
            if (!Written)
            {
                Written = true;
                await write();
            }

            return summary;
        }

        public Task<MonthClosure> CloseMonthAsync(DateOnly month, CancellationToken cancellationToken) => inner.CloseMonthAsync(month, cancellationToken);

        public Task<DossierPartitionSummary> DossiersAsync(DateOnly month, CancellationToken cancellationToken) => inner.DossiersAsync(month, cancellationToken);

        public Task<ThroughputPartitionSummary> ThroughputAsync(DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken) =>
            inner.ThroughputAsync(periodStart, periodEnd, cancellationToken);

        public Task<BudgetPartitionSummary> BudgetsAsync(DateOnly month, Money ceiling, CancellationToken cancellationToken) =>
            inner.BudgetsAsync(month, ceiling, cancellationToken);

        public Task<TierDistributionSummary> TierDistributionAsync(DateOnly month, CancellationToken cancellationToken) =>
            inner.TierDistributionAsync(month, cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Harness
    // -----------------------------------------------------------------------

    private PublicationGateService Gate() => ChannelTestKit.Gate(Source, new NpgsqlUnitOfWork(Source, _clock), _clock);

    private ChannelProfileService Profiles() => new(
        new NpgsqlOperatingRegisters(Source), new NpgsqlAssetLedger(Source), new NpgsqlConfigurationStore(Source), new SystemTestClock());

    private RightsCheckStep RightsCheck()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        var dossiers = new NpgsqlItemDossierReader(Source);
        return new RightsCheckStep(
            dossiers,
            new NpgsqlGateLedger(Source),
            new ItemDossierRecorder(unitOfWork, _clock),
            ChannelTestKit.Gate(Source, unitOfWork, _clock),
            new CopyrightCheckStageHandler(dossiers, new NpgsqlAssetLedger(Source), _clock));
    }

    private static DeclaredSubject Subject() => new()
    {
        Subject = "fixture subject",
        Pillar = "fixture pillar",
        Format = "fixture format",
        Libraries = [ChannelTestKit.Library],
    };

    private static ConfigurationVersion Value(string key, ChannelId channel, string value, int version, DateTimeOffset from) => new()
    {
        Key = key,
        Scope = ChannelConfigurationKeys.ScopeFor(channel),
        Value = value,
        Version = version,
        ChangedBy = "demonstration fixture",
        Reason = "a demonstration fixture value in a throwaway store",
        ValidFrom = from,
    };

    private Job Unit(ItemId item, ChannelId channel) => new()
    {
        Id = JobId.New(),
        Item = item,
        Channel = channel,
        Workflow = "fixture-workflow",
        Position = LifecyclePosition.Queued,
        ClaimState = ClaimState.Ready,
        AvailableAt = _clock.UtcNow,
    };

    private static OperationDraft Draft(ChannelId channel, ItemId item, long input, long output, bool deterministic = false) => new()
    {
        Id = OperationId.New(),
        Run = RunId.New(),
        Attribution = new Attribution(item, channel, Department, Agent),
        Capability = CapabilityClass.EditorialReasoning,
        Model = deterministic ? null : ModelName,
        DeterministicTaskName = deterministic ? MediaCompany.Deterministic.DeterministicTaskRegistry.Render : null,
        Units = new UnitCounts(input, output, 0, 0),
        CostBasis = CostBasis.Measurement,
        Duration = TimeSpan.FromMilliseconds(10),
        Outcome = OperationOutcome.Succeeded,

        // The caller's observation instant; the datastore stamps and books the operation itself.
        OccurredAt = DateTimeOffset.Parse("2020-01-01T00:00:00Z"),
        Attempt = 1,
    };

    private async Task<OperationRecord> RecordOperationAsync(ChannelId channel, ItemId item, long input, long output, bool deterministic = false)
    {
        await using var transaction = await new NpgsqlUnitOfWork(Source, _clock).BeginAsync(CancellationToken.None);
        var record = await transaction.Operations.RecordAsync(Draft(channel, item, input, output, deterministic), CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return record;
    }

    private static AuditEntryDraft Entry(string reason, string action = "fixture.entry") => new()
    {
        Actor = "fixture",
        Action = action,
        Subject = "fixture",
        Reason = reason,
        InputsReference = "none",
        OutputsReference = "none",
        Decision = "none",
        CostReference = "none",
        Risk = "none",
        RetentionClass = RetentionClass.OperationalRecord,
    };

    private async Task AppendAsync(string action, string reason)
    {
        await using var transaction = await new NpgsqlUnitOfWork(Source, _clock).BeginAsync(CancellationToken.None);
        await transaction.Audit.AppendAsync(Entry(reason, action), CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    /// <summary>One entry, so the append-only record has begun observing before the month read.</summary>
    private Task AppendObservationStartAsync() => AppendAsync("fixture.observation-start", "the record begins observing");

    private async Task<long> CountActionAsync(string action) =>
        await ScalarAsync<long>("SELECT COUNT(*) FROM audit_entries WHERE action = @a", c => c.Parameters.AddWithValue("a", action));

    private async Task<ItemId> NewItemAsync(ChannelId channel)
    {
        var item = ItemId.New();
        await ExecuteAsync(
            "INSERT INTO items (item_id, channel_id, item_version, title) VALUES (@item, @channel, 1, 'fixture item')",
            c =>
            {
                c.Parameters.AddWithValue("item", item.Value);
                c.Parameters.AddWithValue("channel", channel.Value);
            });
        return item;
    }

    private static PublishingSequenceRequest Request(ItemId item, ChannelId channel) => new()
    {
        Item = item,
        Version = Version,
        Channel = channel,
        Subject = Subject(),
        Destination = new DestinationDescriptor("a-video-platform", "a-fixture-channel", "en-GB"),
        TreatmentConditions = Enum.GetValues<TreatmentCondition>(),
        SurfaceDraft = new SurfaceDraft
        {
            Title = "Fixture title",
            Description = "A fixture description with named attribution.",
            Tags = ["fixture"],
            ThumbnailReference = "thumbnail-variant-1",
            Captions = "Fixture captions.",
            AdultSignallingElement = "fixture element",
            AudienceReasoning = "not made for kids: a fixture",
            SubjectTermsUsed = ["fixture"],
        },
        Settings = new PublicationSettings
        {
            Visibility = "unlisted",
            MadeForKids = false,
            CommentPolicy = "held-for-review",
            CategoryName = "science-and-technology",
        },
        TimingPolicy = new PublicationTiming.TimingPolicy { LeadDays = 3, PreferredTimeOfDay = new TimeOnly(14, 0) },
    };

    private async Task<IReadOnlyList<AuditEntry>> EntriesAsync()
    {
        await using var command = Source.CreateCommand(
            """
            SELECT entry_id, actor, action, subject, occurred_at, reason, inputs_reference, outputs_reference,
                   decision, cost_reference, risk, retention_class, previous_entry_hash, entry_hash
            FROM audit_entries ORDER BY sequence_no
            """);
        var entries = new List<AuditEntry>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new AuditEntry
            {
                Id = new AuditEntryId(reader.GetGuid(0)),
                Actor = reader.GetString(1),
                Action = reader.GetString(2),
                Subject = reader.GetString(3),
                OccurredAt = reader.GetFieldValue<DateTimeOffset>(4),
                Reason = reader.GetString(5),
                InputsReference = reader.GetString(6),
                OutputsReference = reader.GetString(7),
                Decision = reader.GetString(8),
                CostReference = reader.GetString(9),
                Risk = reader.GetString(10),
                RetentionClass = Enum.Parse<RetentionClass>(reader.GetString(11)),
                PreviousEntryHash = reader.GetString(12),
                EntryHash = reader.GetString(13),
            });
        }

        return entries;
    }

    private async Task<DateTimeOffset> EarliestEntryAsync()
    {
        await using var command = Source.CreateCommand("SELECT min(occurred_at) FROM audit_entries");
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.GetFieldValue<DateTimeOffset>(0);
    }

    private async Task<DateTimeOffset> DatastoreNowAsync()
    {
        await using var command = Source.CreateCommand("SELECT clock_timestamp()");
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.GetFieldValue<DateTimeOffset>(0);
    }

    private async Task<string[]> ColumnAsync(string sql)
    {
        await using var command = Source.CreateCommand(sql);
        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values.ToArray();
    }

    private async Task SeedAsync() => await ExecuteAsync(
        """
        INSERT INTO companies (company_id, name, operating_state) VALUES (@company, 'Fixture Company', 'building');
        INSERT INTO channels (channel_id, company_id, platform, language, registered) VALUES
            (@a, @company, 'video-platform', 'en', false),
            (@b, @company, 'video-platform', 'vi', false);
        INSERT INTO departments (department_id, company_id, name, budget_holder) VALUES (@department, @company, 'production', 'Owner');
        INSERT INTO workforce_agents (agent_id, department_id, name, role) VALUES (@agent, @department, 'fixture agent', 'Producer');
        INSERT INTO items (item_id, channel_id, item_version, title) VALUES
            (@itemA, @a, 1, 'fixture item of channel A'),
            (@itemB, @b, 1, 'fixture item of channel B');
        INSERT INTO provider_accounts (provider_account_id, provider, commercial_terms_basis, verified_on, status)
            VALUES ('provider-alpha', 'alpha', 'paid tier', '2026-09-01', 'Active');
        INSERT INTO models (model_id, provider_account_id, rated_quality, context_capacity, modality)
            VALUES ('alpha-reasoning', 'provider-alpha', 90, 200000, 'text');
        INSERT INTO model_prices (model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from, valid_to)
            VALUES (gen_random_uuid(), 'alpha-reasoning', 'InputUnit',  0.000003, 'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL),
                   (gen_random_uuid(), 'alpha-reasoning', 'OutputUnit', 0.000015, 'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL);
        """,
        c =>
        {
            c.Parameters.AddWithValue("company", Company.Value);
            c.Parameters.AddWithValue("a", ChannelA.Value);
            c.Parameters.AddWithValue("b", ChannelB.Value);
            c.Parameters.AddWithValue("department", Department.Value);
            c.Parameters.AddWithValue("agent", Agent.Value);
            c.Parameters.AddWithValue("itemA", ItemA.Value);
            c.Parameters.AddWithValue("itemB", ItemB.Value);
        });

    private async Task ExecuteAsync(string sql, Action<NpgsqlCommand>? configure = null)
    {
        await using var command = Source.CreateCommand(sql);
        configure?.Invoke(command);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<T> ScalarAsync<T>(string sql, Action<NpgsqlCommand>? configure = null)
    {
        await using var command = Source.CreateCommand(sql);
        configure?.Invoke(command);
        var result = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class SystemTestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// A unit of work that acts as ANOTHER WORKER once: after a transaction that advanced a unit to the
    /// named position commits, it claims the oldest ready unit through the delivered lifecycle, on its
    /// own transaction, before the drive can claim its unit by name.
    /// </summary>
    private sealed class StealingUnitOfWork(IUnitOfWork inner, WorkLifecycleService otherWorker, LifecyclePosition at) : IUnitOfWork
    {
        private bool _stolen;

        private WorkLifecycleService OtherWorker { get; } = otherWorker;

        private LifecyclePosition At { get; } = at;

        public async Task<IWorkTransaction> BeginAsync(CancellationToken cancellationToken) =>
            new Transaction(await inner.BeginAsync(cancellationToken).ConfigureAwait(false), this);

        private sealed class Transaction(IWorkTransaction inner, StealingUnitOfWork owner) : IWorkTransaction
        {
            private readonly Jobs _jobs = new(inner.Jobs);

            public IAuditAppender Audit => inner.Audit;
            public IOperationRecorder Operations => inner.Operations;
            public IJobWriter Jobs => _jobs;
            public IBudgetEvaluator Budgets => inner.Budgets;
            public IGateWriter Gates => inner.Gates;
            public IRouteAvailabilityWriter Availability => inner.Availability;
            public IDispatchWriter Dispatches => inner.Dispatches;
            public IDossierWriter Dossiers => inner.Dossiers;
            public IAdmissionLedger Admission => inner.Admission;
            public IBenchmarkWriter Benchmarks => inner.Benchmarks;

            public async Task CommitAsync(CancellationToken cancellationToken)
            {
                await inner.CommitAsync(cancellationToken).ConfigureAwait(false);

                if (!owner._stolen && _jobs.AdvancedTo == owner.At)
                {
                    owner._stolen = true;
                    await owner.OtherWorker.ClaimAsync("another-worker", TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
                }
            }

            public ValueTask DisposeAsync() => inner.DisposeAsync();
        }

        private sealed class Jobs(IJobWriter inner) : IJobWriter
        {
            public LifecyclePosition? AdvancedTo { get; private set; }

            public Task<DateTimeOffset> EnqueueAsync(Job job, TimeSpan claimableAfter, CancellationToken ct) => inner.EnqueueAsync(job, claimableAfter, ct);

            public Task RecordStageAsync(JobStage stage, CancellationToken ct) => inner.RecordStageAsync(stage, ct);

            public Task<Job?> ClaimNextAsync(string workerId, TimeSpan lease, CancellationToken ct) => inner.ClaimNextAsync(workerId, lease, ct);

            public Task<Job?> ClaimAsync(JobId job, string workerId, TimeSpan lease, CancellationToken ct) => inner.ClaimAsync(job, workerId, lease, ct);

            public Task<DateTimeOffset> ReleaseAsync(JobId job, ClaimState state, TimeSpan claimableAfter, CancellationToken ct) =>
                inner.ReleaseAsync(job, state, claimableAfter, ct);

            public Task<DateTimeOffset> AdvanceAsync(JobId job, LifecyclePosition position, ClaimState state, TimeSpan claimableAfter, CancellationToken ct)
            {
                AdvancedTo = position;
                return inner.AdvanceAsync(job, position, state, claimableAfter, ct);
            }
        }
    }
}
