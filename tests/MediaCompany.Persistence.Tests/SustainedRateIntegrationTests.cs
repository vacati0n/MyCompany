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
        // The fifth resource, applied after the four it depends on; the multi-channel change appends a
        // sixth after it.
        Assert.Equal("MediaCompany.Persistence.Schema.005-sustained-rate.sql", SchemaInstaller.ResourceNames[4]);

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
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM audit_record_horizon"));
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
    /// Every shape a route can record its tier in — each of the three tiers, and none — is
    /// constructed by the composed registry in exactly that shape: a stated tier as that tier, and
    /// none as none, never as a default tier.
    /// </summary>
    [RequiresPostgresTheory]
    [InlineData("Light")]
    [InlineData("Standard")]
    [InlineData("Deep")]
    [InlineData("none")]
    public async Task EveryStatedTierShapeReadsBackInTheShapeItWasWritten(string stated)
    {
        var route = RouteId.New();
        await InsertSubstituteRouteAsync(route, "Primary", stated == "none" ? "NULL" : $"'{stated}'");

        var read = Assert.Single(await new NpgsqlRouteRegistry(Source)
            .AdmittedRoutesAsync(CapabilityClass.EditorialReasoning, CancellationToken.None));

        Assert.Equal(route, read.Id);
        Assert.Equal(stated == "none" ? null : Enum.Parse<ReasoningTier>(stated), read.StatedReasoningTier);
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
    ///
    /// Re-pointed by the AI-economics change (decision D-005 of its design): the route states a higher
    /// tier than requested, because a route stating a lower one is no longer served outside a recorded
    /// controller downgrade; the two still differ.
    [RequiresPostgresFact]
    public async Task TheComposedPathRecordsTheRouteStatedTierAsServed()
    {
        var route = RouteId.New();
        await InsertSubstituteRouteAsync(route, "Primary", "'Deep'");
        await MarkServingAsync(route);

        var outcome = await ComposedGateway().ExecuteAsync(Request(ReasoningTier.Light), Context(), CancellationToken.None);

        var substituted = Assert.IsType<CapabilityOutcome.Substituted>(outcome);
        Assert.Equal(UnitCounts.None, substituted.Operation.Units);
        Assert.Null(substituted.Operation.AppliedPrice);
        Assert.Equal(0m, substituted.Operation.ComputedCost.Amount);

        var stored = Assert.Single((await new NpgsqlCostReader(Source)
            .RecordsForPeriodAsync(Period(substituted.Operation.OccurredAt), CancellationToken.None)).Value);

        Assert.Equal(ReasoningTier.Light, stored.Requested);
        Assert.Equal(ReasoningTier.Deep, stored.Served);
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
        var stored = Assert.Single((await new NpgsqlCostReader(Source)
            .RecordsForPeriodAsync(Period(substituted.Operation.OccurredAt), CancellationToken.None)).Value);

        Assert.Equal(ReasoningTier.Deep, stored.Requested);
        Assert.Null(stored.Served);
    }

    /// <summary>
    /// The tier ratio from recorded pairs: one record over the composed path presents no ratio;
    /// two carrying both tiers present the agreement share with its definition, and a recorded
    /// operation carrying a served tier and no requested tier does not enter the denominator.
    /// </summary>
    ///
    /// Re-pointed by the AI-economics change (decision D-005 of its design): the route states Deep, and
    /// the two requests ask Light (served Deep, disagreeing) and Deep (agreeing), because a request for
    /// a tier above the route's stated one is no longer served. The pairs, the counts and the share are
    /// the delivered ones.
    [RequiresPostgresFact]
    public async Task TheTierRatioIsReadFromRecordedPairsWithItsDefinition()
    {
        var route = RouteId.New();
        await InsertSubstituteRouteAsync(route, "Primary", "'Deep'");
        await MarkServingAsync(route);

        var gateway = ComposedGateway();
        var first = Assert.IsType<CapabilityOutcome.Substituted>(
            await gateway.ExecuteAsync(Request(ReasoningTier.Light), Context(), CancellationToken.None));
        var period = Period(first.Operation.OccurredAt);
        var reader = new NpgsqlCostReader(Source);

        var one = AnalyticsComposers.TierRatio(period, (await reader.RecordsForPeriodAsync(period, CancellationToken.None)).Value);
        Assert.IsType<MeasurementQuantity.Unmeasured>(one.Ratio);
        Assert.Equal(AnalyticsComposers.TierRatioDefinition, one.Definition);

        await gateway.ExecuteAsync(Request(ReasoningTier.Deep), Context(), CancellationToken.None);
        await RecordOperationAsync(requested: null, served: ReasoningTier.Light, first.Operation.OccurredAt);

        var records = (await reader.RecordsForPeriodAsync(period, CancellationToken.None)).Value;
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
    /// A failure policy with no backoff, so a unit released for retry is claimable at once by the
    /// datastore's clock. Its values are demonstration parameters.
    /// </summary>
    private static readonly FailurePolicy NoBackoff = new()
    {
        MaxAttempts = 3,
        InitialBackoff = TimeSpan.Zero,
        BackoffMultiplier = 2.0,
        MaxBackoff = TimeSpan.Zero,
    };

    /// <summary>
    /// Units carried through the DELIVERED services — enqueued, claimed, advanced, retried,
    /// escalated and completed — and read back for a period from the record's first entry to an
    /// instant the datastore had reached. Every count is read from the append-only record, all
    /// eight counts the reader returns are as recorded, and the waiting count at the close agrees
    /// with the queue table's own ready count.
    ///
    /// The three units and their paths are DEMONSTRATION PARAMETERS resting on the Scope Gate
    /// reading that sizing waits for a production series: they exercise the counting rule, they
    /// observe nothing about the company's work, and no rate, depth or threshold follows from them.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheThroughputReadingCountsEveryTransitionTheDeliveredServicesRecorded()
    {
        var lifecycle = new WorkLifecycleService(new NpgsqlUnitOfWork(Source, _clock), _clock);
        var single = new WorkflowDefinition("demonstration", [LifecyclePosition.Queued], NoBackoff);

        // Unit A: claimed, advanced, retried once, then refused and escalated.
        var a = Unit(PublishingWorkflow.Name);
        var aEntered = await lifecycle.EnqueueAsync(a, CancellationToken.None);
        var aClaim = await lifecycle.ClaimUnitAsync(a.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);
        await lifecycle.RecordStageOutcomeAsync(aClaim!, PublishingWorkflow.Definition, LifecyclePosition.Queued, aEntered,
            true, null, 1, NoBackoff, TimeSpan.Zero, TimeSpan.FromHours(1), null, CancellationToken.None);
        var aSecond = await lifecycle.ClaimUnitAsync(a.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);
        await lifecycle.RecordStageOutcomeAsync(aSecond!, PublishingWorkflow.Definition, aSecond!.Position, aSecond.AvailableAt,
            false, FailureClass.Transient, 1, NoBackoff, TimeSpan.Zero, TimeSpan.FromHours(1), "transient", CancellationToken.None);
        var aThird = await lifecycle.ClaimUnitAsync(a.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);
        await lifecycle.RecordStageOutcomeAsync(aThird!, PublishingWorkflow.Definition, aThird!.Position, aSecond.AvailableAt,
            false, FailureClass.Refusal, 2, NoBackoff, TimeSpan.Zero, TimeSpan.FromHours(1), "refused", CancellationToken.None);

        // Unit B: claimed and completed at the only position its workflow declares.
        var b = Unit(single.Name);
        var bEntered = await lifecycle.EnqueueAsync(b, CancellationToken.None);
        var bClaim = await lifecycle.ClaimUnitAsync(b.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);
        await lifecycle.RecordStageOutcomeAsync(bClaim!, single, LifecyclePosition.Queued, bEntered,
            true, null, 1, NoBackoff, TimeSpan.Zero, TimeSpan.FromHours(1), null, CancellationToken.None);

        // Unit C: enqueued and left waiting.
        await lifecycle.EnqueueAsync(Unit(PublishingWorkflow.Name), CancellationToken.None);

        // The period opens at the record's first entry and closes at an instant the datastore has
        // already reached, which the read's own horizon will lie at or after.
        var start = await EarliestEntryAsync();
        var close = await DatastoreNowAsync();

        var summary = await new NpgsqlThroughputReader(Source).ReadAsync(start, close, CancellationToken.None);
        Assert.True(summary.Quiet);
        Assert.True(summary.Horizon >= close);
        Assert.Equal(
            new long[] { 3, 1, 1, 4, 4, 1, 1, 1 },
            new[]
            {
                summary.EnqueuedToClose, summary.StageSucceededToClose, summary.RetriedToClose, summary.ClaimedToClose,
                summary.ClaimedInPeriod, summary.RetriedInPeriod, summary.EscalatedInPeriod, summary.CompletedInPeriod,
            });

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
    /// SIDE BY SIDE, every quantity: a period after the record's first entry, closed below the
    /// horizon the read set and holding no transition, reads OBSERVED ZERO in all five quantities —
    /// the unit enqueued earlier was claimed, so none waits — and the in-period counts behind them
    /// are all zero; a period beginning before the first entry, and one ending after the horizon
    /// the read set, read UNMEASURED in all five, naming why.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnObservedZeroPeriodAndAnUnmeasuredPeriodReadApartFromTheStore()
    {
        var lifecycle = new WorkLifecycleService(new NpgsqlUnitOfWork(Source, _clock), _clock);
        var unit = Unit(PublishingWorkflow.Name);
        await lifecycle.EnqueueAsync(unit, CancellationToken.None);
        await lifecycle.ClaimUnitAsync(unit.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);

        var earliest = await EarliestEntryAsync();
        var quietStart = await DatastoreNowAsync();
        await Task.Delay(20);
        var quietEnd = await DatastoreNowAsync();

        var reader = new NpgsqlThroughputReader(Source);
        var raw = await reader.ReadAsync(quietStart, quietEnd, CancellationToken.None);
        Assert.True(raw.Quiet);
        Assert.Equal(
            new long[] { 1, 0, 0, 1, 0, 0, 0, 0 },
            new[]
            {
                raw.EnqueuedToClose, raw.StageSucceededToClose, raw.RetriedToClose, raw.ClaimedToClose,
                raw.ClaimedInPeriod, raw.RetriedInPeriod, raw.EscalatedInPeriod, raw.CompletedInPeriod,
            });

        var service = Analytics();
        var quiet = await service.ThroughputAsync(quietStart, quietEnd, CancellationToken.None);
        var beforeRecord = await service.ThroughputAsync(earliest.AddHours(-1), quietEnd, CancellationToken.None);
        var notFinal = await service.ThroughputAsync(quietStart, quietEnd.AddHours(1), CancellationToken.None);

        foreach (var quantity in Quantities(quiet))
        {
            Assert.IsType<MeasurementQuantity.ObservedZero>(quantity);
        }

        foreach (var quantity in Quantities(beforeRecord))
        {
            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(quantity);
            Assert.Contains("earliest entry", unmeasured.Detail, StringComparison.Ordinal);
        }

        foreach (var quantity in Quantities(notFinal))
        {
            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(quantity);
            Assert.Contains("ends after the record horizon", unmeasured.Detail, StringComparison.Ordinal);
        }

        foreach (var (zero, none) in Quantities(quiet).Zip(Quantities(beforeRecord)))
        {
            Assert.NotEqual(zero.Describe(), none.Describe());
        }
    }

    /// <summary>
    /// AN OBSERVED ZERO IS FINAL. A period read as observed zero claims stays observed zero: a claim
    /// committed through the delivered services afterwards is stamped at or after the horizon the
    /// read set, outside the period; an entry inserted DIRECTLY with a stamp inside the period,
    /// bypassing the appender, is refused by the datastore; and a second read of the period still
    /// reads observed zero.
    /// </summary>
    [RequiresPostgresFact]
    public async Task APeriodReadAsObservedZeroCannotGainAnEntryAfterTheRead()
    {
        var lifecycle = new WorkLifecycleService(new NpgsqlUnitOfWork(Source, _clock), _clock);
        var unit = Unit(PublishingWorkflow.Name);
        await lifecycle.EnqueueAsync(unit, CancellationToken.None);

        var periodStart = await DatastoreNowAsync();
        await Task.Delay(20);
        var periodEnd = await DatastoreNowAsync();

        var reader = new NpgsqlThroughputReader(Source);
        var first = await reader.ReadAsync(periodStart, periodEnd, CancellationToken.None);
        Assert.IsType<MeasurementQuantity.ObservedZero>(AnalyticsComposers.Throughput(first).Claimed);

        await lifecycle.ClaimUnitAsync(unit.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);
        var claimStamp = await ScalarStampAsync(
            "SELECT occurred_at FROM audit_entries WHERE action = 'job.claimed' ORDER BY sequence_no DESC LIMIT 1");
        Assert.True(claimStamp >= first.Horizon);

        var refused = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO audit_entries (entry_id, actor, action, subject, occurred_at, reason, inputs_reference,
                                       outputs_reference, decision, cost_reference, risk, retention_class,
                                       previous_entry_hash, entry_hash)
            VALUES (gen_random_uuid(), 'lifecycle', 'job.claimed', 'a late entry', @inside, 'bypassing the appender',
                    'none', 'none', 'job.claimed', 'none', 'none', 'OperationalRecord', 'none', md5(random()::text))
            """,
            c => c.Parameters.AddWithValue("inside", periodStart + ((periodEnd - periodStart) / 2))));
        Assert.Equal(PostgresErrorCodes.IntegrityConstraintViolation, refused.SqlState);
        Assert.Contains("closed below", refused.MessageText, StringComparison.Ordinal);

        var second = await reader.ReadAsync(periodStart, periodEnd, CancellationToken.None);
        Assert.IsType<MeasurementQuantity.ObservedZero>(AnalyticsComposers.Throughput(second).Claimed);
        Assert.Equal(0L, second.ClaimedInPeriod);
    }

    /// <summary>
    /// While an audited transaction is in flight the read never waits and never states a count:
    /// every attempt to close the record fails, and every quantity reads unmeasured naming the
    /// in-flight transitions. Once the transaction commits, a read counts its entry.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AReadMeetingAnInFlightTransitionReadsUnmeasuredAndCountsItOnceCommitted()
    {
        var lifecycle = new WorkLifecycleService(new NpgsqlUnitOfWork(Source, _clock), _clock);
        await lifecycle.EnqueueAsync(Unit(PublishingWorkflow.Name), CancellationToken.None);
        var start = await EarliestEntryAsync();

        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        await using (var inFlight = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await inFlight.Audit.AppendAsync(
                new AuditEntryDraft
                {
                    Actor = "lifecycle",
                    Action = LifecycleActions.Claimed,
                    Subject = "an in-flight demonstration claim",
                    Reason = "held open while a read is attempted",
                    InputsReference = "none",
                    OutputsReference = "none",
                    Decision = LifecycleActions.Claimed,
                    CostReference = "none",
                    Risk = "none",
                    RetentionClass = Domain.Audit.RetentionClass.OperationalRecord,
                },
                CancellationToken.None);

            var during = await Analytics().ThroughputAsync(start, await DatastoreNowAsync(), CancellationToken.None);
            foreach (var quantity in Quantities(during))
            {
                var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(quantity);
                Assert.Equal(UnmeasuredReason.SourceCannotStateOne, unmeasured.Reason);
                Assert.Contains("in flight", unmeasured.Detail, StringComparison.Ordinal);
            }

            await inFlight.CommitAsync(CancellationToken.None);
        }

        var after = await Analytics().ThroughputAsync(start, await DatastoreNowAsync(), CancellationToken.None);
        Assert.Equal("1 claims", after.Claimed.Describe());
    }

    /// <summary>The record horizon only moves forward and cannot be removed.</summary>
    [RequiresPostgresFact]
    public async Task TheRecordHorizonOnlyMovesForward()
    {
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM audit_record_horizon"));

        var backwards = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteAsync("UPDATE audit_record_horizon SET horizon = horizon - interval '1 hour'"));
        Assert.Contains("only moves forward", backwards.MessageText, StringComparison.Ordinal);

        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync("DELETE FROM audit_record_horizon"));
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM audit_record_horizon"));
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

        var reading = await Analytics().ThroughputAsync(await EarliestEntryAsync(), await DatastoreNowAsync(), CancellationToken.None);

        var waiting = Assert.IsType<MeasurementQuantity.Unmeasured>(reading.Waiting);
        Assert.Equal(UnmeasuredReason.SourceCannotStateOne, waiting.Reason);
        Assert.Equal("1 claims", reading.Claimed.Describe());
    }

    /// <summary>
    /// The named claim against the store: it takes the named unit and no other ready one, and it
    /// does not take a unit enqueued with a delay the datastore's clock has not yet run out.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheNamedClaimTakesOnlyTheNamedAvailableUnit()
    {
        var lifecycle = new WorkLifecycleService(new NpgsqlUnitOfWork(Source, _clock), _clock);
        var other = Unit(PublishingWorkflow.Name);
        var named = Unit(PublishingWorkflow.Name);
        var later = Unit(PublishingWorkflow.Name);
        await lifecycle.EnqueueAsync(other, CancellationToken.None);
        await lifecycle.EnqueueAsync(named, CancellationToken.None);
        await lifecycle.EnqueueAsync(later, TimeSpan.FromDays(1), CancellationToken.None);

        var claimed = await lifecycle.ClaimUnitAsync(named.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.Equal(named.Id, claimed!.Id);
        Assert.Equal("Ready", await ScalarAsync<string>(
            "SELECT claim_state FROM jobs WHERE job_id = @job", c => c.Parameters.AddWithValue("job", other.Id.Value)));
        Assert.Null(await lifecycle.ClaimUnitAsync(later.Id, "demo", TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    private static IEnumerable<MeasurementQuantity> Quantities(ThroughputReadModel reading) =>
        [reading.Waiting, reading.Claimed, reading.Retried, reading.Escalated, reading.Completed];

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

    private AnalyticsReportService Analytics() => ChannelTestKit.Analytics(Source, _clock);

    private static DateOnly Period(DateTimeOffset occurredAt) => new(occurredAt.Year, occurredAt.Month, 1);

    /// <summary>
    /// The resolution boundary built from the delivered adapters over the record store. The broker
    /// holds no secret and no endpoint is supplied, so no provider adapter exists to be reached.
    /// </summary>
    private ICapabilityGateway ComposedGateway()
    {
        var broker = CredentialBrokerFactory.Create(new SecretStoreOptions(), () => _clock.UtcNow);

        return CapabilityGatewayFactory.Create(
            new NpgsqlRouteRegistry(Source),
            new NpgsqlRouteAvailabilityLedger(Source),
            new NpgsqlOperatingRegisters(Source),
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

    /// <summary>The datastore's own clock, the clock every audit stamp and every availability instant is set on.</summary>
    private Task<DateTimeOffset> DatastoreNowAsync() => ScalarStampAsync("SELECT clock_timestamp()");

    private async Task<DateTimeOffset> ScalarStampAsync(string sql)
    {
        await using var command = Source.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.GetFieldValue<DateTimeOffset>(0);
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
