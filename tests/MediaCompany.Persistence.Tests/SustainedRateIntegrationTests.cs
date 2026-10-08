using MediaCompany.Application.Ports;
using MediaCompany.Capability;
using MediaCompany.Credentials;
using MediaCompany.Deterministic;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Resilience;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Work;
using Npgsql;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// A clock that advances one second on every read, starting hours in the datastore's PAST.
///
/// The queue claim measures availability against the datastore's own clock, so every instant the
/// demonstrations write must already have passed by that clock; and several delivered tables key
/// on an instant, so two writes must never share one. Whole seconds keep every stored instant
/// exact at the datastore's precision.
/// </summary>
internal sealed class SteppingClock : IClock
{
    private DateTimeOffset _next;

    public SteppingClock(DateTimeOffset start) => _next = start;

    public static SteppingClock HoursAgo(int hours)
    {
        var now = DateTimeOffset.UtcNow.AddHours(-hours);
        return new SteppingClock(new DateTimeOffset(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), TimeSpan.Zero));
    }

    /// <summary>The instant the next read will return, without consuming it.</summary>
    public DateTimeOffset Peek => _next;

    public DateTimeOffset UtcNow
    {
        get
        {
            var current = _next;
            _next = _next.AddSeconds(1);
            return current;
        }
    }
}

/// <summary>
/// The sustained-rate capability against the record store: the stated tier through the composed
/// registry and the composed resolution path, the throughput reading from the append-only record,
/// and the fifth schema resource itself.
///
/// EVERY DEMONSTRATION RUNS AGAINST A THROWAWAY STORE THAT IT DROPS AND RECREATES. Every unit, count
/// and depth seeded here is a DEMONSTRATION PARAMETER chosen to exercise a rule. None is an
/// observation of the company's work, and no threshold, rate or sizing figure is derived from any.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class SustainedRateIntegrationTests : IAsyncLifetime
{
    private NpgsqlDataSource? _dataSource;
    private readonly SteppingClock _clock = SteppingClock.HoursAgo(6);

    private NpgsqlDataSource Source => _dataSource
        ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    private static readonly CompanyId Company = CompanyId.New();
    private static readonly ChannelId Channel = ChannelId.New();
    private static readonly DepartmentId Department = DepartmentId.New();
    private static readonly AgentId Agent = AgentId.New();
    private static readonly ItemId Item = ItemId.New();

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
            // A demonstration that fails while preparing its store still leaves no row behind.
            await ThrowawayStore.DropAsync(_dataSource);
            throw;
        }
    }

    /// <summary>
    /// Drops the schema on completion, whatever the demonstration did, so no row it wrote — a
    /// condition recorded satisfied, an owner approval, a gate state or a dispatch record included —
    /// outlives it. The drop at start stays as well.
    /// </summary>
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
    // The fifth schema resource
    // -----------------------------------------------------------------------

    /// <summary>
    /// The fifth resource installs after the four delivered ones and records NOTHING: no
    /// first-publication condition, no approval, no gate transition, no route and no dossier row.
    /// Applying it a second time creates no duplicate and fails nothing, because every object is
    /// created only where it is absent.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheFifthResourceRecordsNothingAndReappliesCleanly()
    {
        Assert.Equal("MediaCompany.Persistence.Schema.005-sustained-rate.sql", SchemaInstaller.ResourceNames[^1]);

        foreach (var table in new[]
                 {
                     "first_publication_conditions", "approvals", "gate_transitions", "routes",
                     "item_dossiers", "dossier_stage_evidence", "dossier_supply_audit_entries",
                     "dossier_determinations", "dossier_components",
                 })
        {
            Assert.Equal(0L, await ScalarAsync<long>($"SELECT COUNT(*) FROM {table}"));
        }

        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.005-sustained-rate.sql"));

        Assert.Equal(5L, await ScalarAsync<long>(
            """
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name IN ('item_dossiers', 'dossier_stage_evidence', 'dossier_supply_audit_entries',
                                 'dossier_determinations', 'dossier_components')
            """));
        Assert.Equal(1L, await ScalarAsync<long>(
            "SELECT COUNT(*) FROM pg_indexes WHERE indexname = 'audit_entries_by_action'"));
        Assert.Equal(1L, await ScalarAsync<long>(
            "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'routes' AND column_name = 'reasoning_tier_stated' AND column_default IS NULL"));
    }

    /// <summary>
    /// The review component's marks are mandatory in the TABLE as well as in the type: an approval
    /// row with no presentation instant is refused, so a missing review mark is a refused
    /// construction at both, never a component that resolves.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheApprovalTableRefusesARowWithoutItsReviewMarks()
    {
        await ExecuteAsync("INSERT INTO roles (role) VALUES ('Owner') ON CONFLICT DO NOTHING");

        var refused = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO approvals (item_id, item_version, gate, approver, verdict, reason, presented_at, decided_at)
            VALUES (@item, 1, 'publication', 'Owner', 'Approved', 'approved', NULL, now())
            """,
            c => c.Parameters.AddWithValue("item", Item.Value)));

        Assert.Equal(PostgresErrorCodes.NotNullViolation, refused.SqlState);
    }

    // -----------------------------------------------------------------------
    // The stated reasoning tier
    // -----------------------------------------------------------------------

    /// <summary>
    /// A route recorded stating a tier is constructed by the composed registry stating THAT tier,
    /// and one recorded stating none is constructed stating none — never a default tier.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ARouteStatingATierAndOneStatingNoneReadBackInTheShapeTheyWereWritten()
    {
        var stating = RouteId.New();
        var statingNone = RouteId.New();
        await InsertSubstituteRouteAsync(stating, "Primary", "'Light'");
        await InsertSubstituteRouteAsync(statingNone, "Secondary", "NULL");

        var routes = await new NpgsqlRouteRegistry(Source)
            .AdmittedRoutesAsync(CapabilityClass.EditorialReasoning, CancellationToken.None);

        Assert.Equal(ReasoningTier.Light, routes.Single(r => r.Id == stating).StatedReasoningTier);
        Assert.Null(routes.Single(r => r.Id == statingNone).StatedReasoningTier);
    }

    /// <summary>The register refuses a tier outside the closed three-value set.</summary>
    [RequiresPostgresFact]
    public async Task TheRouteRegisterRefusesATierOutsideTheClosedSet()
    {
        var refused = await Assert.ThrowsAsync<PostgresException>(
            () => InsertSubstituteRouteAsync(RouteId.New(), "Primary", "'Extreme'"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, refused.SqlState);
    }

    /// <summary>
    /// THE COMPOSED PATH. The delivered resolution boundary, built from the delivered registry, the
    /// delivered availability ledger, the delivered operating registers, the delivered budget
    /// reader and the delivered unit of work over the record store — no test double on the path —
    /// admits a non-AI substitute route stating Light for a request asking Deep, and records an
    /// operation carrying Deep as requested and Light as served. Read back out of the store, the
    /// two differ, so a served value copied from the request is excluded. Zero units, no applied
    /// price, a datastore-computed cost of zero: nothing is spent and no provider is reached.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheComposedPathRecordsTheRouteStatedTierAsServed()
    {
        var route = RouteId.New();
        await InsertSubstituteRouteAsync(route, "Primary", "'Light'");
        await MarkServingAsync(route);

        var outcome = await ComposedGateway().ExecuteAsync(Request(ReasoningTier.Deep), Context(), CancellationToken.None);

        var substituted = Assert.IsType<CapabilityOutcome.Substituted>(outcome);
        Assert.Equal(UnitCounts.None, substituted.Operation.Units);
        Assert.Null(substituted.Operation.AppliedPrice);
        Assert.Equal(0m, substituted.Operation.ComputedCost.Amount);

        var stored = Assert.Single(await new NpgsqlCostReader(Source)
            .RecordsForPeriodAsync(Period(substituted.Operation.OccurredAt), CancellationToken.None));

        Assert.Equal(ReasoningTier.Deep, stored.Requested);
        Assert.Equal(ReasoningTier.Light, stored.Served);
        Assert.NotEqual(stored.Requested, stored.Served);
        Assert.Equal(0m, await ScalarAsync<decimal>("SELECT COALESCE(SUM(computed_cost), 0) FROM agent_costs"));
    }

    /// <summary>
    /// The absence branch on the composed path: a route stating none yields the explicit absence
    /// marker as served, and the requested tier is not substituted for it.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheComposedPathRecordsTheAbsenceMarkerWhereTheRouteStatesNone()
    {
        var route = RouteId.New();
        await InsertSubstituteRouteAsync(route, "Primary", "NULL");
        await MarkServingAsync(route);

        var outcome = await ComposedGateway().ExecuteAsync(Request(ReasoningTier.Deep), Context(), CancellationToken.None);

        var substituted = Assert.IsType<CapabilityOutcome.Substituted>(outcome);
        var stored = Assert.Single(await new NpgsqlCostReader(Source)
            .RecordsForPeriodAsync(Period(substituted.Operation.OccurredAt), CancellationToken.None));

        Assert.Equal(ReasoningTier.Deep, stored.Requested);
        Assert.Null(stored.Served);
    }

    /// <summary>
    /// The tier ratio from recorded pairs: one record over the composed path presents no ratio;
    /// two carrying both tiers present the agreement share with its definition, and a recorded
    /// operation carrying a served tier and no requested tier does not enter the denominator.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheTierRatioIsReadFromRecordedPairsWithItsDefinition()
    {
        var route = RouteId.New();
        await InsertSubstituteRouteAsync(route, "Primary", "'Light'");
        await MarkServingAsync(route);

        var gateway = ComposedGateway();
        var first = Assert.IsType<CapabilityOutcome.Substituted>(
            await gateway.ExecuteAsync(Request(ReasoningTier.Deep), Context(), CancellationToken.None));
        var period = Period(first.Operation.OccurredAt);
        var reader = new NpgsqlCostReader(Source);

        var one = AnalyticsComposers.TierRatio(period, await reader.RecordsForPeriodAsync(period, CancellationToken.None));
        Assert.IsType<MeasurementQuantity.Unmeasured>(one.Ratio);
        Assert.Equal(AnalyticsComposers.TierRatioDefinition, one.Definition);

        await gateway.ExecuteAsync(Request(ReasoningTier.Light), Context(), CancellationToken.None);
        await RecordOperationAsync(requested: null, served: ReasoningTier.Light, first.Operation.OccurredAt);

        var records = await reader.RecordsForPeriodAsync(period, CancellationToken.None);
        Assert.Equal(3, records.Count);

        var ratio = AnalyticsComposers.TierRatio(period, records);
        Assert.Equal("50 percent", ratio.Ratio.Describe());
        Assert.Equal("3 served-tier records", ratio.Caveat.ServedTierRecords.Describe());
        Assert.Equal(AnalyticsComposers.TierRatioDefinition, ratio.Definition);
    }

    // -----------------------------------------------------------------------
    // Throughput and queue quantities, from the append-only record
    // -----------------------------------------------------------------------

    /// <summary>
    /// Units carried through the DELIVERED services — enqueued, claimed, advanced, retried,
    /// escalated and completed — and read back for a period covering them. Every count is read
    /// from the append-only record, and the waiting count at the close agrees with the queue
    /// table's own ready count.
    ///
    /// The three units and their paths are DEMONSTRATION PARAMETERS resting on the Scope Gate
    /// reading that sizing waits for a production series: they exercise the counting rule, they
    /// observe nothing about the company's work, and no rate, depth or threshold follows from them.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheThroughputReadingCountsEveryTransitionTheDeliveredServicesRecorded()
    {
        var lifecycle = new WorkLifecycleService(new NpgsqlUnitOfWork(Source, _clock), _clock);
        var single = new WorkflowDefinition("demonstration", [LifecyclePosition.Queued], FailurePolicy.Default);

        // Unit A: claimed, advanced, retried once, then refused and escalated.
        var a = Unit(PublishingWorkflow.Name);
        var aEntered = await lifecycle.EnqueueAsync(a, CancellationToken.None);
        var aClaim = await lifecycle.ClaimUnitAsync(a.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);
        await lifecycle.RecordStageOutcomeAsync(aClaim!, PublishingWorkflow.Definition, LifecyclePosition.Queued, aEntered,
            true, null, 1, FailurePolicy.Default, TimeSpan.Zero, TimeSpan.FromHours(1), null, CancellationToken.None);
        var aSecond = await lifecycle.ClaimUnitAsync(a.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);
        await lifecycle.RecordStageOutcomeAsync(aSecond!, PublishingWorkflow.Definition, aSecond!.Position, aSecond.AvailableAt,
            false, FailureClass.Transient, 1, FailurePolicy.Default, TimeSpan.Zero, TimeSpan.FromHours(1), "transient", CancellationToken.None);
        var aThird = await lifecycle.ClaimUnitAsync(a.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);
        await lifecycle.RecordStageOutcomeAsync(aThird!, PublishingWorkflow.Definition, aThird!.Position, aSecond.AvailableAt,
            false, FailureClass.Refusal, 2, FailurePolicy.Default, TimeSpan.Zero, TimeSpan.FromHours(1), "refused", CancellationToken.None);

        // Unit B: claimed and completed at the only position its workflow declares.
        var b = Unit(single.Name);
        var bEntered = await lifecycle.EnqueueAsync(b, CancellationToken.None);
        var bClaim = await lifecycle.ClaimUnitAsync(b.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);
        await lifecycle.RecordStageOutcomeAsync(bClaim!, single, LifecyclePosition.Queued, bEntered,
            true, null, 1, FailurePolicy.Default, TimeSpan.Zero, TimeSpan.FromHours(1), null, CancellationToken.None);

        // Unit C: enqueued and left waiting.
        await lifecycle.EnqueueAsync(Unit(PublishingWorkflow.Name), CancellationToken.None);

        // The period opens at the record's first entry, which the coverage rule requires of a period
        // over which the count was taken, and closes after the last.
        var start = await EarliestEntryAsync();
        var close = _clock.Peek;
        var reading = await Analytics().ThroughputAsync(start, close, CancellationToken.None);

        Assert.Equal("4 claims", reading.Claimed.Describe());
        Assert.Equal("1 retried stage attempts", reading.Retried.Describe());
        Assert.Equal("1 escalated units", reading.Escalated.Describe());
        Assert.Equal("1 completed units", reading.Completed.Describe());
        Assert.Equal("1 units waiting", reading.Waiting.Describe());
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM jobs WHERE claim_state = 'Ready'"));
        Assert.Contains("demonstration parameter", reading.Statement, StringComparison.Ordinal);
    }

    /// <summary>
    /// SIDE BY SIDE: an elapsed period after the record's first entry holding no transition reads
    /// as an OBSERVED ZERO; a period beginning before the first entry, and one not yet elapsed,
    /// read UNMEASURED naming the coverage bounds. The waiting count is a stock at the close, so in
    /// the quiet period it still reports the unit left waiting.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnObservedZeroPeriodAndAnUnmeasuredPeriodReadApartFromTheStore()
    {
        var lifecycle = new WorkLifecycleService(new NpgsqlUnitOfWork(Source, _clock), _clock);
        var first = _clock.Peek;
        await lifecycle.EnqueueAsync(Unit(PublishingWorkflow.Name), CancellationToken.None);
        var afterEntries = _clock.Peek;

        var service = Analytics();
        var quiet = await service.ThroughputAsync(afterEntries.AddMinutes(10), afterEntries.AddMinutes(20), CancellationToken.None);
        var beforeRecord = await service.ThroughputAsync(first.AddHours(-1), afterEntries, CancellationToken.None);
        var notElapsed = await service.ThroughputAsync(afterEntries, DateTimeOffset.UtcNow.AddHours(1), CancellationToken.None);

        Assert.IsType<MeasurementQuantity.ObservedZero>(quiet.Claimed);
        Assert.IsType<MeasurementQuantity.ObservedZero>(quiet.Completed);
        Assert.Equal("1 units waiting", quiet.Waiting.Describe());

        var before = Assert.IsType<MeasurementQuantity.Unmeasured>(beforeRecord.Claimed);
        Assert.Contains("earliest entry", before.Detail, StringComparison.Ordinal);
        var pending = Assert.IsType<MeasurementQuantity.Unmeasured>(notElapsed.Claimed);
        Assert.Contains("had not elapsed", pending.Detail, StringComparison.Ordinal);

        Assert.NotEqual(quiet.Claimed.Describe(), beforeRecord.Claimed.Describe());
    }

    /// <summary>
    /// A unit placed in the ready queue OUTSIDE the delivered services, and then claimed through
    /// them, leaves a claim with no arrival: the waiting count cannot be reconstructed, and reads
    /// unmeasured because the source cannot state one rather than as a negative number.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AClaimStateChangedOutsideTheServicesMakesTheWaitingCountUnmeasured()
    {
        var job = JobId.New();
        await ExecuteAsync(
            """
            INSERT INTO jobs (job_id, item_id, channel_id, workflow, position, claim_state, available_at)
            VALUES (@job, @item, @channel, 'publishing', 'Queued', 'Ready', now() - interval '1 day')
            """,
            c =>
            {
                c.Parameters.AddWithValue("job", job.Value);
                c.Parameters.AddWithValue("item", Item.Value);
                c.Parameters.AddWithValue("channel", Channel.Value);
            });

        var lifecycle = new WorkLifecycleService(new NpgsqlUnitOfWork(Source, _clock), _clock);
        Assert.NotNull(await lifecycle.ClaimUnitAsync(job, "demo", TimeSpan.FromMinutes(5), CancellationToken.None));

        var reading = await Analytics().ThroughputAsync(await EarliestEntryAsync(), _clock.Peek, CancellationToken.None);

        var waiting = Assert.IsType<MeasurementQuantity.Unmeasured>(reading.Waiting);
        Assert.Equal(UnmeasuredReason.SourceCannotStateOne, waiting.Reason);
        Assert.Equal("1 claims", reading.Claimed.Describe());
    }

    /// <summary>
    /// The named claim against the store: it takes the named unit and no other ready one, and it
    /// does not take a unit whose availability is in the datastore's future.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheNamedClaimTakesOnlyTheNamedAvailableUnit()
    {
        var lifecycle = new WorkLifecycleService(new NpgsqlUnitOfWork(Source, _clock), _clock);
        var other = Unit(PublishingWorkflow.Name);
        var named = Unit(PublishingWorkflow.Name);
        var later = Unit(PublishingWorkflow.Name) with { AvailableAt = DateTimeOffset.UtcNow.AddDays(1) };
        await lifecycle.EnqueueAsync(other, CancellationToken.None);
        await lifecycle.EnqueueAsync(named, CancellationToken.None);
        await lifecycle.EnqueueAsync(later, CancellationToken.None);

        var claimed = await lifecycle.ClaimUnitAsync(named.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.Equal(named.Id, claimed!.Id);
        Assert.Equal("Ready", await ScalarAsync<string>(
            "SELECT claim_state FROM jobs WHERE job_id = @job", c => c.Parameters.AddWithValue("job", other.Id.Value)));
        Assert.Null(await lifecycle.ClaimUnitAsync(later.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    // -----------------------------------------------------------------------
    // Harness
    // -----------------------------------------------------------------------

    private Job Unit(string workflow) => new()
    {
        Id = JobId.New(),
        Item = Item,
        Channel = Channel,
        Workflow = workflow,
        Position = LifecyclePosition.Queued,
        ClaimState = ClaimState.Ready,
        AvailableAt = _clock.UtcNow,
    };

    private AnalyticsReportService Analytics()
    {
        var costs = new NpgsqlCostReader(Source);
        return new AnalyticsReportService(
            costs, costs, new NpgsqlRevenueParameterRegister(Source), new NpgsqlThroughputReader(Source),
            new NpgsqlItemDossierReader(Source), new NpgsqlGateLedger(Source), new NpgsqlAssetLedger(Source), _clock);
    }

    private static DateOnly Period(DateTimeOffset occurredAt) => new(occurredAt.Year, occurredAt.Month, 1);

    /// <summary>
    /// The resolution boundary built from the delivered adapters over the record store. The broker
    /// holds no secret and no endpoint is supplied, so no provider adapter exists to be reached.
    /// </summary>
    private ICapabilityGateway ComposedGateway()
    {
        var broker = CredentialBrokerFactory.Create(new SecretStoreOptions(), () => _clock.UtcNow);
        var costs = new NpgsqlCostReader(Source);

        return CapabilityGatewayFactory.Create(
            new NpgsqlRouteRegistry(Source),
            new NpgsqlRouteAvailabilityLedger(Source),
            new NpgsqlOperatingRegisters(Source),
            costs,
            new NpgsqlUnitOfWork(Source, _clock),
            broker,
            broker,
            _clock,
            endpoints: [],
            httpClient: new HttpClient());
    }

    private static CapabilityInvocationContext Context() => new()
    {
        Run = RunId.New(),
        Job = JobId.New(),
        Stage = LifecyclePosition.Script,
        Actor = WorkforceRole.Producer,
        Attempt = 1,
    };

    private static CapabilityRequest Request(ReasoningTier requested) => new(
        CapabilityClass.EditorialReasoning,
        requested,
        new QualityRating(70),
        new ContextCapacity(8_000),
        new Money(1.00m),
        Criticality.Routine,
        new Attribution(Item, Channel, Department, Agent),
        new EstimatedUnits(1_000, 500, 0),
        TimeSpan.FromHours(4),
        ReducedFloorPolicy.Forbidden);

    private async Task InsertSubstituteRouteAsync(RouteId route, string tier, string statedTierSql) =>
        await ExecuteAsync(
            $"""
            INSERT INTO routes (route_id, capability_class, tier, target_kind, substitute_task,
                                rated_quality, context_capacity, terms_basis, terms_verified_on, reasoning_tier_stated)
            VALUES (@route, 'EditorialReasoning', '{tier}', 'NonAiSubstitute', @task,
                    100, 2000000, 'non-AI substitute; no provider account and no commercial terms', CURRENT_DATE,
                    {statedTierSql})
            """,
            c =>
            {
                c.Parameters.AddWithValue("route", route.Value);
                c.Parameters.AddWithValue("task", DeterministicTaskRegistry.MetadataTemplatePopulation);
            });

    private async Task MarkServingAsync(RouteId route) =>
        await ExecuteAsync(
            """
            INSERT INTO route_availability (route_id, effective_from, state, reason)
            VALUES (@route, now() - interval '1 day', 'Serving', 'demonstration fixture: the substitute route is serving')
            """,
            c => c.Parameters.AddWithValue("route", route.Value));

    private async Task RecordOperationAsync(ReasoningTier? requested, ReasoningTier? served, DateTimeOffset occurredAt)
    {
        await using var transaction = await new NpgsqlUnitOfWork(Source, _clock).BeginAsync(CancellationToken.None);
        await transaction.Operations.RecordAsync(
            new OperationDraft
            {
                Id = OperationId.New(),
                Run = RunId.New(),
                Attribution = new Attribution(Item, Channel, Department, Agent),
                Capability = CapabilityClass.EditorialReasoning,
                DeterministicTaskName = DeterministicTaskRegistry.MetadataTemplatePopulation,
                Units = UnitCounts.None,
                CostBasis = CostBasis.Measurement,
                Duration = TimeSpan.FromMilliseconds(5),
                Outcome = OperationOutcome.Succeeded,
                OccurredAt = occurredAt,
                Attempt = 1,
                ReasoningTierRequested = requested,
                ReasoningTierServed = served,
            },
            CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    private async Task SeedAsync()
    {
        await ExecuteAsync(
            """
            INSERT INTO companies (company_id, name, operating_state) VALUES (@company, 'Media Company', 'building');
            INSERT INTO channels (channel_id, company_id, platform, language, registered)
                VALUES (@channel, @company, 'video-platform', 'en', false);
            INSERT INTO departments (department_id, company_id, name, budget_holder)
                VALUES (@department, @company, 'production', 'Owner');
            INSERT INTO workforce_agents (agent_id, department_id, name, role)
                VALUES (@agent, @department, 'scriptwriter', 'Producer');
            INSERT INTO items (item_id, channel_id, item_version, title)
                VALUES (@item, @channel, 1, 'demonstration item');
            """,
            c =>
            {
                c.Parameters.AddWithValue("company", Company.Value);
                c.Parameters.AddWithValue("channel", Channel.Value);
                c.Parameters.AddWithValue("department", Department.Value);
                c.Parameters.AddWithValue("agent", Agent.Value);
                c.Parameters.AddWithValue("item", Item.Value);
            });
    }

    private async Task<DateTimeOffset> EarliestEntryAsync()
    {
        await using var command = Source.CreateCommand("SELECT min(occurred_at) FROM audit_entries");
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.GetFieldValue<DateTimeOffset>(0);
    }

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
        return (T)(await command.ExecuteScalarAsync())!;
    }
}
