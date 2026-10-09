using System.Reflection;
using MediaCompany.Application.Ports;
using MediaCompany.Application.Production;
using MediaCompany.Capability;
using MediaCompany.Capability.Providers;
using MediaCompany.Credentials;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Configuration;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The AI-economics capability against the record store (the seventh schema resource and the change
/// that comes with it).
///
/// EVERY DEMONSTRATION RUNS AGAINST A THROWAWAY STORE THAT IT DROPS AND RECREATES, in the separate
/// demonstration database. Every company, channel, budget amount, price row, route, corpus entry,
/// observation, quality figure, unit count and configuration value below is a DEMONSTRATION FIXTURE that
/// exists only there; nothing in production code, seed or migration records any of them, and none is an
/// observation of the company's work or a benchmark result.
///
/// NO METERED CALL IS MADE. Where a demonstration drives the provider path it does so through a STAND-IN
/// provider declared here, which returns a fixture attempt and reaches nothing, with a stand-in credential
/// broker that holds no secret.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class AiEconomicsIntegrationTests : IAsyncLifetime
{
    private NpgsqlDataSource? _dataSource;

    private NpgsqlDataSource Source => _dataSource
        ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    private static readonly CompanyId Company = CompanyId.New();
    private static readonly ChannelId ChannelA = ChannelId.New();
    private static readonly ChannelId ChannelB = ChannelId.New();
    private static readonly DepartmentId Department = DepartmentId.New();
    private static readonly AgentId Agent = AgentId.New();
    private static readonly ItemId ItemA = ItemId.New();
    private static readonly ItemId ItemB = ItemId.New();
    private static readonly ProviderAccountId Alpha = new("provider-alpha");
    private static readonly ProviderAccountId Beta = new("provider-beta");
    private static readonly ModelId AlphaModel = new("alpha-reasoning");
    private static readonly ModelId BetaModel = new("beta-reasoning");
    private static readonly ModelId UnpricedModel = new("alpha-unpriced");

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
    // The seventh schema resource
    // -----------------------------------------------------------------------

    /// <summary>
    /// The seventh resource installs last, creates every record EMPTY and records nothing — no entry, no
    /// observation, no decision, no budget amount, no configuration value — binds its operation-record
    /// check to new rows only, and applies a second time, and after a re-applied sixth resource, cleanly.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheSeventhResourceRecordsNothingAndReappliesCleanly()
    {
        Assert.Equal("MediaCompany.Persistence.Schema.007-ai-economics.sql", SchemaInstaller.ResourceNames[^1]);

        foreach (var table in new[]
                 {
                     "benchmark_corpus_entries", "benchmark_observations", "admission_decisions", "admission_decision_readings",
                     "admission_decision_candidates", "admission_decision_observations", "budgets", "configuration",
                 })
        {
            Assert.Equal(0L, await ScalarAsync<long>($"SELECT COUNT(*) FROM {table}"));
        }

        Assert.Equal(1L, await ScalarAsync<long>(
            "SELECT COUNT(*) FROM pg_constraint WHERE conname = 'agent_costs_deterministic_states_cost' AND NOT convalidated"));

        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.007-ai-economics.sql"));
        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.006-multi-channel.sql"));
        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.007-ai-economics.sql"));

        foreach (var trigger in new[]
                 {
                     "gate_transitions_refuse_unrecorded_from_state", "gate_transitions_same_instant_refused",
                     "gate_transitions_written_once", "benchmark_observations_on_clock", "admission_decisions_on_operation",
                 })
        {
            Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM pg_trigger WHERE tgname = @name", c => c.Parameters.AddWithValue("name", trigger)));
        }

        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM benchmark_observations"));
        Assert.Equal("NO", await ScalarAsync<string>(
            "SELECT is_nullable FROM information_schema.columns WHERE table_name = 'gate_transitions' AND column_name = 'recorded_order'"));
    }

    // -----------------------------------------------------------------------
    // The benchmark record
    // -----------------------------------------------------------------------

    /// <summary>
    /// Each of the three permitted quality shapes written through the writer reads back identical through
    /// both reader members, with all seven carried facts: the corpus entry, the task class, the model, the
    /// route, the quality in its case, the cost read through the operation — observed where the operation
    /// states it, unmeasured where it does not, never the zero it was stored as — and the latency read as
    /// the operation's recorded duration. Every instant is the datastore's.
    /// </summary>
    [RequiresPostgresFact]
    public async Task EveryPermittedObservationShapeRoundTripsWithAllSevenFacts()
    {
        var alpha = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Primary", Alpha, AlphaModel, 90);
        var beta = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Secondary", Beta, BetaModel, 85);

        var priced = await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, input: 1_000, output: 500, other: 0, durationMs: 1_200);
        var unstated = await RecordOperationAsync(beta, BetaModel, ChannelA, ItemA, input: 1_000, output: 500, other: 1, durationMs: 800);
        var third = await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, input: 10, output: 5, other: 0, durationMs: 0);
        Assert.True(priced.CostStated);
        Assert.False(unstated.CostStated);

        var script = CorpusEntryId.New();
        var factCheck = CorpusEntryId.New();
        var before = await DatastoreNowAsync();

        var written = new List<BenchmarkObservation>();
        await using (var transaction = await new NpgsqlUnitOfWork(Source, new FixedClock(DateTimeOffset.UnixEpoch)).BeginAsync(CancellationToken.None))
        {
            var registered = await transaction.Benchmarks.RegisterEntryAsync(script, TaskClass.ScriptPass, CancellationToken.None);
            await transaction.Benchmarks.RegisterEntryAsync(factCheck, TaskClass.FactCheck, CancellationToken.None);
            Assert.True(registered >= before);

            written.Add(await transaction.Benchmarks.RecordObservationAsync(Draft(script, TaskClass.ScriptPass, priced.Id,
                MeasurementQuantity.Observed(92m, BenchmarkObservation.QualityRatingUnit)), CancellationToken.None));
            written.Add(await transaction.Benchmarks.RecordObservationAsync(Draft(script, TaskClass.ScriptPass, unstated.Id,
                MeasurementQuantity.Zero(BenchmarkObservation.QualityRatingUnit)), CancellationToken.None));
            written.Add(await transaction.Benchmarks.RecordObservationAsync(Draft(factCheck, TaskClass.FactCheck, third.Id,
                MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne, "fixture: the judged output was not recorded")),
                CancellationToken.None));
            await transaction.CommitAsync(CancellationToken.None);
        }

        var after = await DatastoreNowAsync();

        var first = written[0];
        Assert.Equal(script, first.Entry);
        Assert.Equal(TaskClass.ScriptPass, first.TaskClass);
        Assert.Equal(alpha, first.Route);
        Assert.Equal(AlphaModel, first.Model);
        Assert.Equal(92m, Assert.IsType<MeasurementQuantity.ObservedValue>(first.Quality).Amount);
        Assert.Equal(0.0105m, Assert.IsType<MeasurementQuantity.ObservedValue>(first.Cost).Amount);
        Assert.Equal(1_200m, Assert.IsType<MeasurementQuantity.ObservedValue>(first.Latency).Amount);
        Assert.InRange(first.ObservedAt, before, after);

        Assert.IsType<MeasurementQuantity.ObservedZero>(written[1].Quality);
        Assert.Contains("is not stated", Assert.IsType<MeasurementQuantity.Unmeasured>(written[1].Cost).Detail, StringComparison.Ordinal);
        Assert.Equal(BetaModel, written[1].Model);

        Assert.IsType<MeasurementQuantity.Unmeasured>(written[2].Quality);
        Assert.IsType<MeasurementQuantity.ObservedZero>(written[2].Latency);

        // The observed zero and the unmeasured quality render apart, side by side.
        Assert.NotEqual(written[1].Quality.Describe(), written[2].Quality.Describe());
        Assert.StartsWith("observed zero", written[1].Quality.Describe(), StringComparison.Ordinal);
        Assert.StartsWith("unmeasured", written[2].Quality.Describe(), StringComparison.Ordinal);

        var month = written[0].Period;
        var monthReading = await new NpgsqlBenchmarkReader(Source).ObservationsAsync(month, CancellationToken.None);
        Assert.Equal(written.OrderBy(o => o.ObservedAt).ThenBy(o => o.Id.Value), monthReading.Value);
        Assert.Equal(month, monthReading.Closure.Month);

        var record = await new NpgsqlBenchmarkReader(Source).RecordAsync(CancellationToken.None);
        Assert.Equal(written.OrderBy(o => o.Id.Value), record.Observations);

        var reading = EconomicsComposers.Benchmark(record);
        var alphaScript = reading.Readings.Single(r => r.Route == alpha && r.TaskClass == TaskClass.ScriptPass);
        Assert.Equal(92m, Assert.IsType<MeasurementQuantity.ObservedValue>(alphaScript.Quality).Amount);
        Assert.Equal("1 observations", alphaScript.Observations.Describe());
        var betaScript = reading.Readings.Single(r => r.Route == beta && r.TaskClass == TaskClass.ScriptPass);
        Assert.IsType<MeasurementQuantity.ObservedZero>(betaScript.Quality);
        Assert.IsType<MeasurementQuantity.Unmeasured>(betaScript.Cost);
    }

    /// <summary>
    /// Every forbidden observation shape, written DIRECTLY against the store, is refused by a table check
    /// or a reference, so no writer — the adapter or one bypassing it — can commit a shape the selection
    /// would rank: an observed value of zero, an observed zero carrying an amount, an observed case with no
    /// unit, an unmeasured case carrying an amount or a unit or no detail or a reason outside the closed
    /// pair, an unknown case, an entry and task class that disagree, and a route and model that disagree
    /// with the operation's.
    /// </summary>
    [RequiresPostgresFact]
    public async Task EveryForbiddenObservationShapeIsRefusedForADirectWriter()
    {
        var alpha = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Primary", Alpha, AlphaModel, 90);
        var beta = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Secondary", Beta, BetaModel, 85);
        var entry = CorpusEntryId.New();
        await RegisterEntryAsync(entry, TaskClass.ScriptPass);

        var shapes = new (string Name, string Case, decimal? Amount, string? Unit, string? Reason, string? Detail)[]
        {
            ("an observed value of zero", "ObservedValue", 0m, "rating on the 0 to 100 scale", null, null),
            ("an observed value with no unit", "ObservedValue", 92m, null, null, null),
            ("an observed value with a blank unit", "ObservedValue", 92m, " ", null, null),
            ("an observed value carrying an unmeasured reason", "ObservedValue", 92m, "rating", "NoObservationExists", "x"),
            ("an observed zero carrying an amount", "ObservedZero", 0m, "rating", null, null),
            ("an observed zero with no unit", "ObservedZero", null, null, null, null),
            ("an unmeasured case carrying an amount", "Unmeasured", 92m, null, "NoObservationExists", "detail"),
            ("an unmeasured case carrying a unit", "Unmeasured", null, "rating", "NoObservationExists", "detail"),
            ("an unmeasured case with no detail", "Unmeasured", null, null, "NoObservationExists", null),
            ("an unmeasured case with a blank detail", "Unmeasured", null, null, "NoObservationExists", "  "),
            ("an unmeasured case with a reason outside the closed pair", "Unmeasured", null, null, "Estimated", "detail"),
            ("a case outside the closed three", "Assumed", 92m, "rating", null, null),
        };

        foreach (var shape in shapes)
        {
            var operation = await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, 1_000, 500, 0, 10);
            var refused = await Assert.ThrowsAsync<PostgresException>(() => InsertObservationDirectAsync(
                entry, "ScriptPass", operation.Id, alpha, AlphaModel, shape.Case, shape.Amount, shape.Unit, shape.Reason, shape.Detail));
            Assert.True(
                refused.ConstraintName == "benchmark_observations_quality_case",
                $"{shape.Name} was refused by {refused.ConstraintName ?? refused.SqlState}, not by the quality-case check");
        }

        var agreeing = await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, 1_000, 500, 0, 10);

        // An entry and a task class that disagree.
        var mismatchedTask = await Assert.ThrowsAsync<PostgresException>(() => InsertObservationDirectAsync(
            entry, "FactCheck", agreeing.Id, alpha, AlphaModel, "ObservedValue", 90m, "rating", null, null));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, mismatchedTask.SqlState);

        // A route and model that disagree with the operation's.
        var mismatchedRoute = await Assert.ThrowsAsync<PostgresException>(() => InsertObservationDirectAsync(
            entry, "ScriptPass", agreeing.Id, beta, BetaModel, "ObservedValue", 90m, "rating", null, null));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, mismatchedRoute.SqlState);

        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM benchmark_observations"));
    }

    /// <summary>
    /// An observation is on the datastore's clock and written once, for every writer: a direct write
    /// stamped below the record horizon, or booked into a month not its instant's, is refused; and an
    /// update or a delete of a recorded observation is refused.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnObservationIsWriteOnceAndOnTheDatastoresClockForEveryWriter()
    {
        var alpha = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Primary", Alpha, AlphaModel, 90);
        var entry = CorpusEntryId.New();
        await RegisterEntryAsync(entry, TaskClass.ScriptPass);
        var operation = await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, 1_000, 500, 0, 10);

        var belowHorizon = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO benchmark_observations (observation_id, entry_id, task_class, operation_id, route_id, model_id,
                quality_case, quality_amount, quality_unit, observed_at, period)
            VALUES (gen_random_uuid(), @entry, 'ScriptPass', @operation, @route, @model, 'ObservedValue', 90, 'rating',
                    '2000-01-01T00:00:00Z', '2000-01-01')
            """,
            c => ObservationParameters(c, entry, operation.Id, alpha, AlphaModel)));
        Assert.Equal("benchmark_observations_on_clock", belowHorizon.ConstraintName);

        var offMonth = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO benchmark_observations (observation_id, entry_id, task_class, operation_id, route_id, model_id,
                quality_case, quality_amount, quality_unit, observed_at, period)
            VALUES (gen_random_uuid(), @entry, 'ScriptPass', @operation, @route, @model, 'ObservedValue', 90, 'rating',
                    clock_timestamp() + interval '1 hour', '2000-01-01')
            """,
            c => ObservationParameters(c, entry, operation.Id, alpha, AlphaModel)));
        Assert.Equal("benchmark_observations_on_clock", offMonth.ConstraintName);

        await using (var transaction = await new NpgsqlUnitOfWork(Source, new FixedClock(DateTimeOffset.UnixEpoch)).BeginAsync(CancellationToken.None))
        {
            await transaction.Benchmarks.RecordObservationAsync(
                Draft(entry, TaskClass.ScriptPass, operation.Id, MeasurementQuantity.Observed(90m, BenchmarkObservation.QualityRatingUnit)),
                CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync("UPDATE benchmark_observations SET quality_amount = 99"));
        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync("DELETE FROM benchmark_observations"));
        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync("DELETE FROM benchmark_corpus_entries"));
        Assert.Equal(90m, await ScalarAsync<decimal>("SELECT quality_amount FROM benchmark_observations"));
    }

    /// <summary>
    /// A store with every resource applied and NO observation reads every quantity of every route, for
    /// every task class, unmeasured, naming the register, the route and the task class looked for; the
    /// observation count is an observed zero. Nothing in the delivered code records an entry or an
    /// observation.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AStoreWithNoObservationReadsEveryQuantityUnmeasuredNamingWhatWasLookedFor()
    {
        var alpha = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Primary", Alpha, AlphaModel, 90);
        await InsertSubstituteRouteAsync(CapabilityClass.EditorialReasoning, "Emergency", null);

        var reading = await ChannelTestKit.Analytics(Source, new FixedClock(DateTimeOffset.UtcNow)).BenchmarkAsync(CancellationToken.None);

        Assert.Equal(2 * Enum.GetValues<TaskClass>().Length, reading.Readings.Count);
        foreach (var route in reading.Readings)
        {
            foreach (var quantity in new[] { route.Quality, route.Cost, route.Latency })
            {
                var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(quantity);
                Assert.Equal(UnmeasuredReason.NoObservationExists, unmeasured.Reason);
                Assert.Contains("benchmark record", unmeasured.Detail, StringComparison.Ordinal);
                Assert.Contains(route.Route.ToString(), unmeasured.Detail, StringComparison.Ordinal);
                Assert.Contains(route.TaskClass.ToString(), unmeasured.Detail, StringComparison.Ordinal);
            }

            Assert.IsType<MeasurementQuantity.ObservedZero>(route.Observations);
        }

        Assert.Contains(reading.Readings, r => r.Route == alpha);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM benchmark_corpus_entries"));
    }

    // -----------------------------------------------------------------------
    // One clock: every instant the datastore's, whatever the process clock says
    // -----------------------------------------------------------------------

    /// <summary>
    /// With the process clock a day behind and a day ahead, every instant the admission writes or decides
    /// by is a datastore instant: the operation is booked at the reserved instant, inside the datastore's
    /// own clock readings taken around the call; its decision is stamped with the same instant and month,
    /// read back from the decision record; and a held request escalates from that instant plus its hold
    /// timeout, never from the process clock.
    /// </summary>
    [RequiresPostgresFact]
    public async Task EveryAdmissionInstantIsTheDatastoresWhateverTheProcessClockSays()
    {
        await InsertSubstituteRouteAsync(CapabilityClass.EditorialReasoning, "Emergency", null);

        foreach (var skew in new[] { TimeSpan.FromDays(-1), TimeSpan.FromDays(1) })
        {
            var clock = new FixedClock(DateTimeOffset.UtcNow + skew);
            var gateway = Gateway(clock);

            var before = await DatastoreNowAsync();
            var substituted = Assert.IsType<CapabilityOutcome.Substituted>(
                await gateway.ExecuteAsync(Request(CapabilityClass.EditorialReasoning, floor: 60), Context(), CancellationToken.None));
            var held = Assert.IsType<CapabilityOutcome.Held>(
                await gateway.ExecuteAsync(Request(CapabilityClass.EditorialReasoning, floor: 95), Context(), CancellationToken.None));
            var after = await DatastoreNowAsync();

            Assert.InRange(substituted.Operation.OccurredAt, before, after);
            var (decidedAt, bookingMonth) = await DecisionInstantAsync(substituted.Operation.Id);
            Assert.Equal(substituted.Operation.OccurredAt, decidedAt);
            Assert.Equal(MonthOf(substituted.Operation.OccurredAt), bookingMonth);

            var heldOperation = await OperationInstantAsync(Outcome: "Held");
            var (heldDecidedAt, _) = await DecisionInstantAsync(heldOperation.Id);
            Assert.Equal(heldOperation.At, heldDecidedAt);
            Assert.InRange(heldOperation.At, before, after);
            Assert.Equal(heldOperation.At + TimeSpan.FromHours(4), held.EscalatesAt);
            Assert.NotEqual(clock.UtcNow + TimeSpan.FromHours(4), held.EscalatesAt);

        }
    }

    /// <summary>
    /// UNIT PRICES ARE READ AT THE RESERVED INSTANT. A fixture price row taking effect an hour after the
    /// datastore's clock, at a price no request's ceiling admits, is not in force at the booking instant,
    /// so the stand-in is reached and the operation is costed at the price in force at that instant; with
    /// the process clock two days ahead, a price read on the process clock would have refused it.
    /// </summary>
    [RequiresPostgresFact]
    public async Task PricesAreReadAtTheReservedInstantAndNotOnTheProcessClock()
    {
        var month = MonthOf(await DatastoreNowAsync());
        await InsertBudgetAsync(ChannelA, month, 1.00m);
        await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", Alpha, AlphaModel, 90);
        await ExecuteAsync(
            """
            INSERT INTO model_prices (model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from, valid_to)
            SELECT gen_random_uuid(), 'alpha-reasoning', kind, 1, 'USD', 'a later fixture price no ceiling admits', CURRENT_DATE,
                   clock_timestamp() + interval '1 hour', NULL
            FROM unnest(ARRAY['InputUnit', 'OutputUnit', 'CachedUnit']) AS kind
            """);

        var standIn = new StandInProvider(Alpha);
        var completed = Assert.IsType<CapabilityOutcome.Completed>(await Gateway(new FixedClock(DateTimeOffset.UtcNow.AddDays(2)), standIn)
            .ExecuteAsync(Request(CapabilityClass.BulkClassification, floor: 60), Context(), CancellationToken.None));

        Assert.Equal(1, standIn.Calls);
        Assert.Equal(0.0105m, completed.Operation.ComputedCost.Amount);
        Assert.True(completed.Operation.CostStated);
    }

    // -----------------------------------------------------------------------
    // Admission against the booking month, across a month end
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE MONTH-END DEMONSTRATION. The record horizon is moved, as a fixture, to just before a month end,
    /// so the datastore's booking instant falls in that month. During the stand-in provider's call — which
    /// reaches nothing — another session tries to close the record (refused without waiting, because the
    /// admission holds the horizon shared) and starts raising the horizon past the month end (which waits
    /// for the admission to commit). The operation is booked into the ADMITTED month, its decision and its
    /// alert carry that month, the horizon passes the month end only after the commit, and the admitted
    /// month then reads final with the operation in it.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnOperationAdmittedBeforeAMonthEndIsBookedIntoTheAdmittedMonth()
    {
        var now = await DatastoreNowAsync();
        var nextMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1);
        var admittedMonth = MonthOf(nextMonth.AddDays(-1));
        await ExecuteAsync("UPDATE audit_record_horizon SET horizon = @h WHERE only_row", c => c.Parameters.AddWithValue("h", nextMonth.AddMilliseconds(-50)));
        await InsertBudgetAsync(ChannelA, admittedMonth, 0.02m);
        await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", Alpha, AlphaModel, 90);

        Task? raise = null;
        var closureRefused = false;
        var standIn = new StandInProvider(Alpha, async () =>
        {
            await using var observer = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString!);
            await using (var connection = await observer.OpenConnectionAsync())
            await using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    await using var close = new NpgsqlCommand(
                        "SELECT horizon FROM audit_record_horizon WHERE only_row FOR UPDATE NOWAIT", connection, transaction);
                    await close.ExecuteNonQueryAsync();
                }
                catch (PostgresException refused) when (refused.SqlState == PostgresErrorCodes.LockNotAvailable)
                {
                    closureRefused = true;
                }
            }

            raise = Task.Run(async () =>
            {
                await using var raiser = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString!);
                await using var command = raiser.CreateCommand("UPDATE audit_record_horizon SET horizon = @h WHERE only_row");
                command.Parameters.AddWithValue("h", nextMonth.AddSeconds(5));
                await command.ExecuteNonQueryAsync();
            });

            await Task.Delay(300);
            Assert.False(raise.IsCompleted, "the horizon was raised while the admission held it shared");
        });

        var completed = Assert.IsType<CapabilityOutcome.Completed>(
            await Gateway(new FixedClock(now), standIn).ExecuteAsync(Request(CapabilityClass.BulkClassification, floor: 70), Context(), CancellationToken.None));

        Assert.Equal(1, standIn.Calls);
        Assert.True(closureRefused);
        await raise!;

        Assert.Equal(admittedMonth, MonthOf(completed.Operation.OccurredAt));
        Assert.Equal(nextMonth.AddMilliseconds(-50), completed.Operation.OccurredAt);
        var (decidedAt, bookingMonth) = await DecisionInstantAsync(completed.Operation.Id);
        Assert.Equal(admittedMonth, bookingMonth);
        Assert.Equal(completed.Operation.OccurredAt, decidedAt);
        var alert = Assert.Single(completed.Alerts);
        Assert.Equal(admittedMonth, alert.Period);
        Assert.Equal(50, alert.Threshold);

        Assert.Equal(nextMonth.AddSeconds(5), await HorizonAsync());
        var partition = await new NpgsqlChannelPartitionReader(Source).OperationsAsync(admittedMonth, CancellationToken.None);
        Assert.True(ChannelAnalyticsComposers.Finality(partition.Closure).IsFinal);
        Assert.Equal(1L, partition.Company.Operations);
        Assert.Equal(0L, await ScalarAsync<long>(
            "SELECT COUNT(*) FROM agent_costs WHERE period = @m", c => c.Parameters.Add("m", NpgsqlDbType.Date).Value = MonthOf(nextMonth)));
    }

    // -----------------------------------------------------------------------
    // Evidence selection, from one read
    // -----------------------------------------------------------------------

    /// <summary>
    /// Over two candidates whose every compared quantity is observed in one unit, selection takes the
    /// CHEAPEST candidate meeting the floor — here the secondary route, against the configured ordering —
    /// labelled evidence; and an observation committed by another session DURING the stand-in's call,
    /// after the evidence was read, is not among the recorded inputs, which read back as exactly the
    /// observations that one statement read, with their count.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AConcurrentObservationCommitDuringARankingLeavesTheRecordedInputsOneRead()
    {
        var alpha = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Primary", Alpha, AlphaModel, 90);
        var beta = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Secondary", Beta, BetaModel, 85);
        var month = MonthOf(await DatastoreNowAsync());
        await InsertBudgetAsync(ChannelA, month, 1.00m);
        var entry = CorpusEntryId.New();
        await RegisterEntryAsync(entry, TaskClass.ScriptPass);

        var alphaRun = await RecordOperationAsync(alpha, AlphaModel, ChannelB, ItemB, 1_000, 500, 0, 900);
        var betaRun = await RecordOperationAsync(beta, BetaModel, ChannelB, ItemB, 1_000, 500, 0, 700);
        var alphaObservation = await RecordObservationAsync(entry, TaskClass.ScriptPass, alphaRun.Id, 92m);
        var betaObservation = await RecordObservationAsync(entry, TaskClass.ScriptPass, betaRun.Id, 88m);

        BenchmarkObservation? concurrent = null;
        var standIn = new StandInProvider(Beta, async () =>
        {
            var late = await RecordOperationAsync(alpha, AlphaModel, ChannelB, ItemB, 10, 5, 0, 100);
            concurrent = await RecordObservationAsync(entry, TaskClass.ScriptPass, late.Id, 99m);
        });

        var completed = Assert.IsType<CapabilityOutcome.Completed>(await Gateway(new FixedClock(DateTimeOffset.UnixEpoch), standIn)
            .ExecuteAsync(Request(CapabilityClass.EditorialReasoning, floor: 80, task: TaskClass.ScriptPass), Context(), CancellationToken.None));

        Assert.Equal(beta, completed.Operation.Route);
        Assert.NotNull(concurrent);

        var decision = await ReadDecisionAsync(completed.Operation.Id);
        Assert.Equal("Evidence", decision.Basis);
        Assert.Equal(2L, decision.ObservationsRankedOn);
        Assert.Equal(
            new[] { alphaObservation.Id.Value, betaObservation.Id.Value }.Order(),
            decision.Observations.Order());
        Assert.DoesNotContain(concurrent!.Id.Value, decision.Observations);
        Assert.Equal(new[] { (beta.Value, 1), (alpha.Value, 2) }, decision.Candidates.Select(c => (c.Route, c.Position)));
        Assert.All(decision.Candidates, c => Assert.Equal(1L, c.Observations));
        Assert.Equal(90, decision.Candidates.Single(c => c.Route == alpha.Value).ConfiguredRating);
        Assert.Equal(92m, decision.Candidates.Single(c => c.Route == alpha.Value).QualityAmount);
        Assert.Contains("ranked on evidence: 2 observations", decision.BasisStatement, StringComparison.Ordinal);

        // The route register is read, never written.
        Assert.Equal(90, await ScalarAsync<int>("SELECT rated_quality FROM routes WHERE route_id = @r", c => c.Parameters.AddWithValue("r", alpha.Value)));
    }

    /// <summary>
    /// Where any compared quantity of any candidate is not observed in one unit — an unmeasured quality,
    /// or a quality observed in another unit — selection is the configured ordering EXACTLY, labelled
    /// configured and naming the quantity, and the decision record reads the candidates back with the
    /// configured rating apart from the observed quality.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnUnmeasuredOrMismatchedQuantityYieldsTheConfiguredOrderingNamingIt()
    {
        var alpha = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Primary", Alpha, AlphaModel, 90);
        var beta = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Secondary", Beta, BetaModel, 85);
        var month = MonthOf(await DatastoreNowAsync());
        await InsertBudgetAsync(ChannelA, month, 1.00m);
        var entry = CorpusEntryId.New();
        await RegisterEntryAsync(entry, TaskClass.FactCheck);

        var alphaRun = await RecordOperationAsync(alpha, AlphaModel, ChannelB, ItemB, 1_000, 500, 0, 900);
        var betaRun = await RecordOperationAsync(beta, BetaModel, ChannelB, ItemB, 1_000, 500, 0, 700);
        await RecordObservationAsync(entry, TaskClass.FactCheck, alphaRun.Id, 92m);
        await using (var transaction = await new NpgsqlUnitOfWork(Source, new FixedClock(DateTimeOffset.UnixEpoch)).BeginAsync(CancellationToken.None))
        {
            await transaction.Benchmarks.RecordObservationAsync(
                Draft(entry, TaskClass.FactCheck, betaRun.Id, MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, "fixture: not judged")),
                CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var standIn = new StandInProvider(Alpha);
        var completed = Assert.IsType<CapabilityOutcome.Completed>(await Gateway(new FixedClock(DateTimeOffset.UnixEpoch), standIn)
            .ExecuteAsync(Request(CapabilityClass.EditorialReasoning, floor: 80, task: TaskClass.FactCheck), Context(), CancellationToken.None));

        Assert.Equal(alpha, completed.Operation.Route);
        var decision = await ReadDecisionAsync(completed.Operation.Id);
        Assert.Equal("Configured", decision.Basis);
        Assert.Equal(0L, decision.ObservationsRankedOn);
        Assert.Contains($"the quality of route {beta}", decision.BasisStatement, StringComparison.Ordinal);
        Assert.Equal(new[] { (alpha.Value, 1), (beta.Value, 2) }, decision.Candidates.Select(c => (c.Route, c.Position)));
        Assert.Equal("Unmeasured", decision.Candidates.Single(c => c.Route == beta.Value).QualityCase);
        Assert.Equal(85, decision.Candidates.Single(c => c.Route == beta.Value).ConfiguredRating);
    }

    // -----------------------------------------------------------------------
    // The cost controller at the datastore
    // -----------------------------------------------------------------------

    /// <summary>
    /// A channel with NO budget amount recorded for the month refuses metered work under its own reason,
    /// naming the channel and the month, never the exceeded reason, while a substitute stays admissible;
    /// and once an operation with an unstated cost is booked into the month, booked spend reads unmeasured
    /// — never zero, never headroom — and metered work is refused under that reason. Each decision reads
    /// back with its readings in their cases.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AMissingAmountAndUnmeasuredSpendEachRefuseUnderTheirOwnReason()
    {
        var month = MonthOf(await DatastoreNowAsync());
        var alphaEditorial = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Primary", Alpha, AlphaModel, 90);
        await InsertSubstituteRouteAsync(CapabilityClass.EditorialReasoning, "Emergency", null);
        var bulk = await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", Alpha, AlphaModel, 90);
        var standIn = new StandInProvider(Alpha);
        var gateway = Gateway(new FixedClock(DateTimeOffset.UnixEpoch), standIn);

        var substituted = Assert.IsType<CapabilityOutcome.Substituted>(
            await gateway.ExecuteAsync(Request(CapabilityClass.EditorialReasoning, floor: 60), Context(), CancellationToken.None));
        var noAmount = Assert.IsType<CapabilityOutcome.Refused>(
            await gateway.ExecuteAsync(Request(CapabilityClass.BulkClassification, floor: 60), Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.BudgetAmountNotRecorded, noAmount.Reason);
        Assert.Contains(ChannelA.ToString(), noAmount.Detail, StringComparison.Ordinal);
        Assert.Contains($"{month:yyyy-MM}", noAmount.Detail, StringComparison.Ordinal);
        Assert.Equal(0, standIn.Calls);

        var reading = await ReadReadingAsync(substituted.Operation.Id, "Channel");
        Assert.Equal("NotRecorded", reading.AmountCase);
        Assert.Equal("Refuse", reading.Action);
        Assert.Equal("BudgetAmountNotRecorded", reading.Reason);
        Assert.Equal("ObservedZero", reading.SpendCase);
        var company = await ReadReadingAsync(substituted.Operation.Id, "Company");
        Assert.Equal("Recorded", company.AmountCase);
        Assert.Equal(34.42m, company.Amount);

        await InsertBudgetAsync(ChannelA, month, 1.00m);
        await RecordOperationAsync(alphaEditorial, AlphaModel, ChannelA, ItemA, 1_000, 500, other: 1, durationMs: 10);

        var unmeasured = Assert.IsType<CapabilityOutcome.Refused>(
            await gateway.ExecuteAsync(Request(CapabilityClass.BulkClassification, floor: 60), Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.SpendUnmeasured, unmeasured.Reason);
        Assert.NotEqual(RefusalReason.CostCeilingOrBudgetExceeded, unmeasured.Reason);
        Assert.Equal(0, standIn.Calls);

        var refusedOperation = await OperationInstantAsync(Outcome: "Refused", latest: true);
        var spend = await ReadReadingAsync(refusedOperation.Id, "Channel");
        Assert.Equal("Unmeasured", spend.SpendCase);
        Assert.Null(spend.SpendAmount);
        Assert.Equal("SpendUnmeasured", spend.Reason);
        Assert.Contains("not stated", spend.SpendDetail!, StringComparison.Ordinal);
        Assert.NotEqual(default, bulk);
    }

    /// <summary>
    /// THE FIRST METERED OPERATION OF A MONTH IS ADMITTED: with a budget amount recorded and nothing booked,
    /// booked spend reads observed zero and the stand-in is called. Then, with one fixture operation booked,
    /// each band yields exactly its action — none, the alert only, a downgrade, a deferral and a refusal —
    /// recorded with its threshold; a hold-and-escalate route stays admissible at 90 and 100 percent; a
    /// metered request at 90 percent is held under the deferral reason, escalating from the booking
    /// instant, and at 100 percent is refused naming the threshold and the scope.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheFirstMeteredOperationIsAdmittedAndEachBandYieldsItsAction()
    {
        var month = MonthOf(await DatastoreNowAsync());
        await InsertBudgetAsync(ChannelA, month, 1.00m);
        await InsertHoldRouteAsync(CapabilityClass.EditorialReasoning, "Primary");
        await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", Alpha, AlphaModel, 90);
        var standIn = new StandInProvider(Alpha);
        var gateway = Gateway(new FixedClock(DateTimeOffset.UnixEpoch), standIn);

        var first = Assert.IsType<CapabilityOutcome.Completed>(
            await gateway.ExecuteAsync(Request(CapabilityClass.BulkClassification, floor: 60), Context(), CancellationToken.None));
        Assert.Equal(1, standIn.Calls);
        var firstReading = await ReadReadingAsync(first.Operation.Id, "Channel");
        Assert.Equal("ObservedZero", firstReading.SpendCase);
        Assert.Equal("None", firstReading.Action);
        Assert.Equal(0.0105m, first.Operation.ComputedCost.Amount);

        foreach (var (amount, threshold, action) in new (decimal, int?, string)[]
                 {
                     (0.0300m, null, "None"), (0.0210m, 50, "AlertOnly"), (0.0140m, 75, "Downgrade"),
                     (0.0116m, 90, "Defer"), (0.0105m, 100, "Refuse"),
                 })
        {
            await ExecuteAsync("UPDATE budgets SET amount = @a", c => c.Parameters.AddWithValue("a", amount));

            var outcome = await gateway.ExecuteAsync(Request(CapabilityClass.EditorialReasoning, floor: 60), Context(), CancellationToken.None);
            Assert.IsType<CapabilityOutcome.Held>(outcome);

            var held = await OperationInstantAsync(Outcome: "Held", latest: true);
            var reading = await ReadReadingAsync(held.Id, "Channel");
            Assert.Equal(threshold, reading.Threshold);
            Assert.Equal(action, reading.Action);
            Assert.Equal(action, (await ReadDecisionAsync(held.Id)).Action);
        }

        await ExecuteAsync("UPDATE budgets SET amount = 0.0116");
        var deferred = Assert.IsType<CapabilityOutcome.Held>(
            await gateway.ExecuteAsync(Request(CapabilityClass.BulkClassification, floor: 60), Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.DeferredAtThreshold, deferred.Reason);
        var deferredOperation = await OperationInstantAsync(Outcome: "Held", latest: true);
        Assert.Equal(deferredOperation.At + TimeSpan.FromHours(4), deferred.EscalatesAt);
        Assert.Equal(0m, await ScalarAsync<decimal>("SELECT computed_cost FROM agent_costs WHERE operation_id = @o", c => c.Parameters.AddWithValue("o", deferredOperation.Id.Value)));

        await ExecuteAsync("UPDATE budgets SET amount = 0.0105");
        var refused = Assert.IsType<CapabilityOutcome.Refused>(
            await gateway.ExecuteAsync(Request(CapabilityClass.BulkClassification, floor: 60), Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.RefusedAtThreshold, refused.Reason);
        Assert.Contains("100 percent", refused.Detail, StringComparison.Ordinal);
        Assert.Contains(ChannelA.ToString(), refused.Detail, StringComparison.Ordinal);
        Assert.Equal(1, standIn.Calls);
    }

    /// <summary>
    /// Where the channel reading and the company reading reach different thresholds, the action of the
    /// higher one applies, and the decision names the company as the reading that decided it; the company
    /// reading is governed against the recorded metered allotment and states so.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheHigherThresholdOfTheChannelAndTheCompanyReadingApplies()
    {
        var month = MonthOf(await DatastoreNowAsync());
        await InsertBudgetAsync(ChannelA, month, 0.0210m);
        await InsertHoldRouteAsync(CapabilityClass.EditorialReasoning, "Primary");
        var alpha = await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", Alpha, AlphaModel, 90);

        // Channel A at 50 percent of its fixture amount; the company at about 87 percent of the metered allotment.
        await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, 1_000, 500, 0, 10);
        await RecordOperationAsync(alpha, AlphaModel, ChannelB, ItemB, 0, 2_000_000, 0, 10);

        var held = Assert.IsType<CapabilityOutcome.Held>(await Gateway(new FixedClock(DateTimeOffset.UnixEpoch))
            .ExecuteAsync(Request(CapabilityClass.EditorialReasoning, floor: 60), Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.NoAvailableRoute, held.Reason);

        var operation = await OperationInstantAsync(Outcome: "Held", latest: true);
        Assert.Equal("AlertOnly", (await ReadReadingAsync(operation.Id, "Channel")).Action);
        Assert.Equal("Downgrade", (await ReadReadingAsync(operation.Id, "Company")).Action);
        var decision = await ReadDecisionAsync(operation.Id);
        Assert.Equal("Downgrade", decision.Action);
        Assert.Contains("recorded metered allotment of 34.42", decision.CompanyBasisStatement, StringComparison.Ordinal);
        Assert.Contains("none was available", decision.TierStatement, StringComparison.Ordinal);
    }

    /// <summary>
    /// A LOST RESERVATION. The stand-in terminates the admission's datastore session during its call, so
    /// the admission transaction is lost after the attempt was incurred. The attempt is recorded once, on a
    /// fresh transaction under a fresh reservation, and its decision record says the admitted reservation
    /// was lost and was re-evaluated against the booked month, on one instant with the operation.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ALostReservationIsRecordedOnAFreshOneThatSaysSo()
    {
        var month = MonthOf(await DatastoreNowAsync());
        await InsertBudgetAsync(ChannelA, month, 1.00m);
        await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", Alpha, AlphaModel, 90);

        var standIn = new StandInProvider(Alpha, async () =>
        {
            await using var killer = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString!);
            await using var command = killer.CreateCommand(
                """
                SELECT count(pg_terminate_backend(pid)) FROM pg_stat_activity
                WHERE datname = current_database() AND pid <> pg_backend_pid() AND state = 'idle in transaction'
                """);
            Assert.True((long)(await command.ExecuteScalarAsync())! >= 1);
        });

        var completed = Assert.IsType<CapabilityOutcome.Completed>(await Gateway(new FixedClock(DateTimeOffset.UnixEpoch), standIn)
            .ExecuteAsync(Request(CapabilityClass.BulkClassification, floor: 60), Context(), CancellationToken.None));

        Assert.Equal(1, standIn.Calls);
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM agent_costs WHERE model_id IS NOT NULL"));
        var decision = await ReadDecisionAsync(completed.Operation.Id);
        Assert.Contains("was lost after the provider was called", decision.ReservationStatement, StringComparison.Ordinal);
        var (decidedAt, bookingMonth) = await DecisionInstantAsync(completed.Operation.Id);
        Assert.Equal(completed.Operation.OccurredAt, decidedAt);
        Assert.Equal(MonthOf(decidedAt), bookingMonth);
    }

    // -----------------------------------------------------------------------
    // The tier distribution and month finality
    // -----------------------------------------------------------------------

    /// <summary>
    /// Per period, each stated tier and the untiered part resolves to one case; counts and unit sums are
    /// observed, an observed zero where a tier holds none; a part whose costs are all stated reads its cost
    /// observed and a part holding an unstated cost reads it unmeasured; every observed part sums exactly to
    /// the period total; the reading carries the labelled assumed split, the evidence count, the statement
    /// that no level-to-tier relation is recorded and its finality; and a month holding no operation reads
    /// unmeasured at every tier, naming what was looked for.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheTierDistributionResolvesEachPartInItsCaseAndSumsToTheTotal()
    {
        var alpha = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Primary", Alpha, AlphaModel, 90);
        var light1 = await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, 1_000, 500, 0, 10, ReasoningTier.Light);
        await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, 2_000, 0, 1, 10, ReasoningTier.Light);
        await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, 3_000, 1_000, 0, 10, ReasoningTier.Deep);
        await RecordOperationAsync(alpha, AlphaModel, ChannelB, ItemB, 100, 100, 0, 10, null);
        var month = MonthOf(light1.OccurredAt);

        var reading = await ChannelTestKit.Analytics(Source, new FixedClock(DateTimeOffset.UnixEpoch)).TierDistributionAsync(month, CancellationToken.None);

        var light = reading.Parts.Single(p => p.Tier == ReasoningTier.Light);
        var standard = reading.Parts.Single(p => p.Tier == ReasoningTier.Standard);
        var deep = reading.Parts.Single(p => p.Tier == ReasoningTier.Deep);
        var untiered = reading.Parts.Single(p => p.Tier is null);

        Assert.Equal("2 operations", light.Operations.Describe());
        Assert.Equal("3000 input units", light.InputUnits.Describe());
        Assert.IsType<MeasurementQuantity.Unmeasured>(light.Cost);
        Assert.IsType<MeasurementQuantity.ObservedZero>(standard.Operations);
        Assert.IsType<MeasurementQuantity.ObservedZero>(standard.Cost);
        Assert.Equal(0.024m, Assert.IsType<MeasurementQuantity.ObservedValue>(deep.Cost).Amount);
        Assert.Equal(0.0018m, Assert.IsType<MeasurementQuantity.ObservedValue>(untiered.Cost).Amount);

        Assert.Equal("4 operations", reading.Total.Operations.Describe());
        Assert.Equal(6_100m, Assert.IsType<MeasurementQuantity.ObservedValue>(reading.Total.InputUnits).Amount);
        Assert.Equal(
            Assert.IsType<MeasurementQuantity.ObservedValue>(reading.Total.InputUnits).Amount,
            reading.Parts.Sum(p => p.InputUnits is MeasurementQuantity.ObservedValue v ? v.Amount : 0m));
        Assert.Equal(
            Assert.IsType<MeasurementQuantity.ObservedValue>(reading.Total.Operations).Amount,
            reading.Parts.Sum(p => p.Operations is MeasurementQuantity.ObservedValue v ? v.Amount : 0m));
        Assert.IsType<MeasurementQuantity.Unmeasured>(reading.Total.Cost);
        Assert.Equal("3 operations carrying a served tier", reading.TierEvidence.Describe());
        Assert.StartsWith("assumed, not measured", reading.AssumedSplit, StringComparison.Ordinal);
        Assert.Contains("decides no split", reading.RelationStatement, StringComparison.Ordinal);
        Assert.Equal(month, reading.Finality.Month);

        var empty = await ChannelTestKit.Analytics(Source, new FixedClock(DateTimeOffset.UnixEpoch))
            .TierDistributionAsync(month.AddMonths(6), CancellationToken.None);
        Assert.All(empty.Parts.Append(empty.Total), part =>
        {
            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(part.Operations);
            Assert.Contains("no operation is recorded in the operation record", unmeasured.Detail, StringComparison.Ordinal);
            Assert.IsType<MeasurementQuantity.Unmeasured>(part.Cost);
        });
    }

    /// <summary>
    /// The period summary, the served-tier list, the tier distribution and the benchmark month each carry
    /// the closure of the read that produced them; once the record horizon passes the month's end each
    /// reads final, and an operation committed afterwards — which can only be booked into a later month —
    /// leaves every final reading unchanged. The horizon and its checks are as delivered.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AReadingPresentedAsFinalIsUnchangedByALaterOperation()
    {
        var alpha = await InsertProviderRouteAsync(CapabilityClass.EditorialReasoning, "Primary", Alpha, AlphaModel, 90);
        var operation = await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, 1_000, 500, 0, 10, ReasoningTier.Standard);
        var month = MonthOf(operation.OccurredAt);
        var analytics = ChannelTestKit.Analytics(Source, new FixedClock(DateTimeOffset.UnixEpoch));

        var open = await analytics.PeriodCostAsync(month, CancellationToken.None);
        Assert.False(open.Finality!.IsFinal);

        // The fixture moves the horizon past the month's end, as a closure at the start of the next month would.
        await ExecuteAsync("UPDATE audit_record_horizon SET horizon = @h WHERE only_row",
            c => c.Parameters.AddWithValue("h", new DateTimeOffset(month.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddMonths(1).AddSeconds(1)));

        var cost = await analytics.PeriodCostAsync(month, CancellationToken.None);
        var tiers = await analytics.ServedTiersAsync(month, CancellationToken.None);
        var ratio = await analytics.TierRatioAsync(month, CancellationToken.None);
        var distribution = await analytics.TierDistributionAsync(month, CancellationToken.None);
        var benchmarks = await analytics.BenchmarkMonthAsync(month, CancellationToken.None);
        Assert.True(cost.Finality!.IsFinal);
        Assert.True(tiers.Finality.IsFinal);
        Assert.True(ratio.Finality!.IsFinal);
        Assert.True(distribution.Finality.IsFinal);
        Assert.True(benchmarks.Finality.IsFinal);

        var later = await RecordOperationAsync(alpha, AlphaModel, ChannelA, ItemA, 1_000, 500, 0, 10, ReasoningTier.Standard);
        Assert.NotEqual(month, MonthOf(later.OccurredAt));

        Assert.Equal(cost.Cost, (await analytics.PeriodCostAsync(month, CancellationToken.None)).Cost);
        Assert.Equal(tiers.Records.Select(r => r.Operation), (await analytics.ServedTiersAsync(month, CancellationToken.None)).Records.Select(r => r.Operation));
        Assert.Equal(distribution.Total, (await analytics.TierDistributionAsync(month, CancellationToken.None)).Total);
    }

    // -----------------------------------------------------------------------
    // The gate-transition record
    // -----------------------------------------------------------------------

    /// <summary>
    /// A transition without a datastore position is refused for a writer that bypasses the position
    /// assignment; every recorded transition carries one; re-applying the resources changes no existing
    /// position; and a recorded transition can be neither updated nor deleted.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ATransitionWithoutAPositionIsRefusedAndTransitionsAreWriteOnce()
    {
        var at = await DatastoreNowAsync();
        await RecordTransitionAsync(ItemA, 1, GateState.Draft, GateState.AwaitingRightsCheck, at);

        await using (var connection = await Source.OpenConnectionAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await using (var disable = new NpgsqlCommand(
                "ALTER TABLE gate_transitions DISABLE TRIGGER gate_transitions_refuse_unrecorded_from_state", connection, transaction))
            {
                await disable.ExecuteNonQueryAsync();
            }

            await using var insert = new NpgsqlCommand(
                """
                INSERT INTO gate_transitions (item_id, item_version, occurred_at, from_state, to_state, reason)
                VALUES (@item, 1, clock_timestamp(), 'AwaitingRightsCheck', 'SentBack', 'a writer bypassing the position')
                """,
                connection,
                transaction);
            insert.Parameters.AddWithValue("item", ItemA.Value);
            var refused = await Assert.ThrowsAsync<PostgresException>(() => insert.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.NotNullViolation, refused.SqlState);
            Assert.Equal("recorded_order", refused.ColumnName);
        }

        var positions = await ScalarAsync<long>("SELECT COALESCE(sum(recorded_order), 0) FROM gate_transitions");
        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.006-multi-channel.sql"));
        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.007-ai-economics.sql"));
        Assert.Equal(positions, await ScalarAsync<long>("SELECT COALESCE(sum(recorded_order), 0) FROM gate_transitions"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM gate_transitions WHERE recorded_order IS NULL"));

        var update = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync("UPDATE gate_transitions SET reason = 'rewritten'"));
        Assert.Equal("gate_transitions_written_once", update.ConstraintName);
        var delete = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync("DELETE FROM gate_transitions"));
        Assert.Equal("gate_transitions_written_once", delete.ConstraintName);
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM gate_transitions"));
    }

    /// <summary>
    /// Two transitions of one item version presented at one instant yield EXACTLY ONE recorded transition
    /// and ONE NAMED OUTCOME, through the writer and through the gate service, and no unnamed datastore
    /// error reaches either caller; a transition of another item version at the same instant is unaffected;
    /// and two concurrent writers of one item version at one instant leave one transition, the other
    /// receiving a named conflict.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TwoTransitionsOfOneItemVersionAtOneInstantRecordOneAndNameTheOther()
    {
        var at = await DatastoreNowAsync();

        await RecordTransitionAsync(ItemA, 1, GateState.Draft, GateState.AwaitingRightsCheck, at);
        var conflict = await Assert.ThrowsAsync<GateStateConflictException>(
            () => RecordTransitionAsync(ItemA, 1, GateState.AwaitingRightsCheck, GateState.SentBack, at));
        Assert.Equal(GateConflictReason.InstantAlreadyRecorded, conflict.Reason);
        Assert.Equal(1L, await CountTransitionsAsync(ItemA, 1));

        await RecordTransitionAsync(ItemA, 2, GateState.Draft, GateState.AwaitingRightsCheck, at);
        Assert.Equal(1L, await CountTransitionsAsync(ItemA, 2));

        var fixedAt = at.AddMinutes(5);
        var gate = ChannelTestKit.Gate(Source, new NpgsqlUnitOfWork(Source, new FixedClock(fixedAt)), new FixedClock(fixedAt));
        Assert.IsType<GateStepOutcome.Moved>(await gate.SubmitForRightsCheckAsync(ItemB, new ItemVersion(1), WorkforceRole.Producer, CancellationToken.None));
        var sentBack = Assert.IsType<GateStepOutcome.Refused>(await gate.SendBackFromRightsCheckAsync(
            ItemB, new ItemVersion(1), WorkforceRole.Copyright, "fixture: a send-back at the submission's instant", CancellationToken.None));
        Assert.Equal(GateStepRefusal.TransitionAtRecordedInstant, sentBack.Reason);
        Assert.Equal(1L, await CountTransitionsAsync(ItemB, 1));

        // Two concurrent writers of one item version at one instant: the first admitted under the item hold
        // is recorded and the other is named.
        var unitOfWork = new NpgsqlUnitOfWork(Source, new FixedClock(fixedAt));
        var concurrentAt = at.AddMinutes(10);
        await using var first = await unitOfWork.BeginAsync(CancellationToken.None);
        await first.Gates.RecordTransitionAsync(ItemA, new ItemVersion(3), GateState.Draft, GateState.AwaitingRightsCheck, "first", concurrentAt, CancellationToken.None);
        var second = Task.Run(() => RecordTransitionAsync(ItemA, 3, GateState.Draft, GateState.AwaitingRightsCheck, concurrentAt));
        await Task.Delay(300);
        Assert.False(second.IsCompleted);
        await first.CommitAsync(CancellationToken.None);
        var named = await Assert.ThrowsAsync<GateStateConflictException>(() => second);
        Assert.True(Enum.IsDefined(named.Reason));
        Assert.Equal(1L, await CountTransitionsAsync(ItemA, 3));
    }

    // -----------------------------------------------------------------------
    // The production and publishing keys
    // -----------------------------------------------------------------------

    /// <summary>
    /// Each production and publishing key records under a company scope and reads back identical, with its
    /// history; each is refused under a channel scope and under a scope naming no company; a channel key is
    /// refused under a company scope; a base key keeps its delivered pairing; and no fragment rule changed.
    /// Every value is a demonstration fixture in the throwaway store.
    /// </summary>
    [RequiresPostgresFact]
    public async Task EachProductionAndPublishingKeyRoundTripsUnderTheCompanyScopeOnly()
    {
        var store = new NpgsqlConfigurationStore(Source);
        var companyScope = CompanyConfigurationScope.ScopeFor(Company);
        var channelScope = ChannelConfigurationKeys.ScopeFor(ChannelA);
        var from = (await DatastoreNowAsync()).AddMinutes(-1);

        foreach (var key in ProductionConfigurationKeys.Admitted.Concat(PublishingConfigurationKeys.Admitted))
        {
            await store.SupersedeAsync(Value(key, companyScope, $"fixture value for {key}", from), CancellationToken.None);

            var inForce = await store.InForceAsync(key, companyScope, from.AddSeconds(1), CancellationToken.None);
            Assert.NotNull(inForce);
            Assert.Equal($"fixture value for {key}", inForce!.Value);
            Assert.Single(await store.HistoryAsync(key, companyScope, CancellationToken.None));

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.SupersedeAsync(Value(key, channelScope, "x", from), CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.SupersedeAsync(Value(key, "global", "x", from), CancellationToken.None));
            Assert.False(ProductionConfigurationKeys.IsAdmitted(key)
                ? ProductionConfigurationKeys.ReachesAControl(key)
                : PublishingConfigurationKeys.ReachesAControl(key));
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SupersedeAsync(Value(ChannelConfigurationKeys.Brand, companyScope, "x", from), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SupersedeAsync(Value(ConfigurationKeys.BudgetAmount, channelScope, "x", from), CancellationToken.None));
        await store.SupersedeAsync(Value(ConfigurationKeys.CredentialTtlSeconds, companyScope, "fixture", from), CancellationToken.None);

        Assert.True(PublishingConfigurationKeys.Admitted.All(k => ChannelConfigurationKeys.ReachesAControl(k)));
        Assert.Equal(
            ProductionConfigurationKeys.Admitted.Count + PublishingConfigurationKeys.Admitted.Count + 1,
            await ScalarAsync<long>("SELECT COUNT(*) FROM configuration WHERE scope = @s", c => c.Parameters.AddWithValue("s", companyScope)));
    }

    // -----------------------------------------------------------------------
    // Harness
    // -----------------------------------------------------------------------

    private CapabilityGateway Gateway(IClock clock, StandInProvider? standIn = null)
    {
        var adapters = new Dictionary<ProviderAccountId, IProviderAdapter>();
        if (standIn is not null)
        {
            adapters[standIn.ProviderAccount] = standIn;
        }

        return new CapabilityGateway(
            new NpgsqlRouteRegistry(Source),
            new NpgsqlRouteAvailabilityLedger(Source),
            new NpgsqlOperatingRegisters(Source),
            new NpgsqlUnitOfWork(Source, clock),
            new StandInBroker(),
            adapters);
    }

    private static CapabilityInvocationContext Context() => new()
    {
        Run = RunId.New(),
        Job = JobId.New(),
        Stage = LifecyclePosition.Script,
        Actor = WorkforceRole.Producer,
        Attempt = 1,
    };

    private static CapabilityRequest Request(CapabilityClass capability, int floor, TaskClass? task = null) =>
        new(capability,
            ReasoningTier.Standard,
            new QualityRating(floor),
            new ContextCapacity(8_000),
            new Money(1.00m),
            Criticality.Routine,
            new Attribution(ItemA, ChannelA, Department, Agent),
            new EstimatedUnits(1_000, 500, 0),
            TimeSpan.FromHours(4),
            ReducedFloorPolicy.Forbidden)
        {
            TaskClass = task,
        };

    private static BenchmarkObservationDraft Draft(CorpusEntryId entry, TaskClass task, OperationId operation, MeasurementQuantity quality) => new()
    {
        Id = BenchmarkObservationId.New(),
        Entry = entry,
        TaskClass = task,
        Operation = operation,
        Quality = quality,
    };

    private static ConfigurationVersion Value(string key, string scope, string value, DateTimeOffset from) => new()
    {
        Key = key,
        Scope = scope,
        Value = value,
        Version = 1,
        ChangedBy = "fixture",
        Reason = "a demonstration fixture",
        ValidFrom = from,
    };

    private static DateOnly MonthOf(DateTimeOffset instant) => new(instant.UtcDateTime.Year, instant.UtcDateTime.Month, 1);

    private async Task<OperationRecord> RecordOperationAsync(
        RouteId route,
        ModelId model,
        ChannelId channel,
        ItemId item,
        long input,
        long output,
        long other,
        long durationMs,
        ReasoningTier? served = null)
    {
        await using var transaction = await new NpgsqlUnitOfWork(Source, new FixedClock(DateTimeOffset.UnixEpoch)).BeginAsync(CancellationToken.None);
        var record = await transaction.Operations.RecordAsync(
            new OperationDraft
            {
                Id = OperationId.New(),
                Run = RunId.New(),
                Attribution = new Attribution(item, channel, Department, Agent),
                Capability = CapabilityClass.EditorialReasoning,
                Route = route,
                Model = model,
                Units = new UnitCounts(input, output, 0, other),
                CostBasis = CostBasis.Measurement,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                Outcome = OperationOutcome.Succeeded,
                OccurredAt = DateTimeOffset.UnixEpoch,
                Attempt = 1,
                ReasoningTierRequested = served is null ? null : ReasoningTier.Standard,
                ReasoningTierServed = served,
            },
            CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return record;
    }

    private async Task<BenchmarkObservation> RecordObservationAsync(CorpusEntryId entry, TaskClass task, OperationId operation, decimal quality)
    {
        await using var transaction = await new NpgsqlUnitOfWork(Source, new FixedClock(DateTimeOffset.UnixEpoch)).BeginAsync(CancellationToken.None);
        var observation = await transaction.Benchmarks.RecordObservationAsync(
            Draft(entry, task, operation, MeasurementQuantity.Observed(quality, BenchmarkObservation.QualityRatingUnit)), CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return observation;
    }

    private async Task RegisterEntryAsync(CorpusEntryId entry, TaskClass task)
    {
        await using var transaction = await new NpgsqlUnitOfWork(Source, new FixedClock(DateTimeOffset.UnixEpoch)).BeginAsync(CancellationToken.None);
        await transaction.Benchmarks.RegisterEntryAsync(entry, task, CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    private async Task RecordTransitionAsync(ItemId item, int version, GateState from, GateState to, DateTimeOffset at)
    {
        await using var transaction = await new NpgsqlUnitOfWork(Source, new FixedClock(at)).BeginAsync(CancellationToken.None);
        await transaction.Gates.RecordTransitionAsync(item, new ItemVersion(version), from, to, "a demonstration fixture", at, CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    private Task<long> CountTransitionsAsync(ItemId item, int version) =>
        ScalarAsync<long>(
            "SELECT COUNT(*) FROM gate_transitions WHERE item_id = @i AND item_version = @v",
            c =>
            {
                c.Parameters.AddWithValue("i", item.Value);
                c.Parameters.AddWithValue("v", version);
            });

    private async Task InsertObservationDirectAsync(
        CorpusEntryId entry, string task, OperationId operation, RouteId route, ModelId model,
        string theCase, decimal? amount, string? unit, string? reason, string? detail) =>
        await ExecuteAsync(
            """
            INSERT INTO benchmark_observations (observation_id, entry_id, task_class, operation_id, route_id, model_id,
                quality_case, quality_amount, quality_unit, quality_unmeasured_reason, quality_unmeasured_detail, observed_at, period)
            SELECT gen_random_uuid(), @entry, @task, @operation, @route, @model, @case, @amount, @unit, @reason, @detail,
                   GREATEST(clock_timestamp(), h.horizon) + interval '1 second',
                   (date_trunc('month', (GREATEST(clock_timestamp(), h.horizon) + interval '1 second') AT TIME ZONE 'UTC'))::date
            FROM audit_record_horizon h
            """,
            c =>
            {
                ObservationParameters(c, entry, operation, route, model);
                c.Parameters["task"].Value = task;
                c.Parameters.AddWithValue("case", theCase);
                c.Parameters.Add("amount", NpgsqlDbType.Numeric).Value = (object?)amount ?? DBNull.Value;
                c.Parameters.Add("unit", NpgsqlDbType.Text).Value = (object?)unit ?? DBNull.Value;
                c.Parameters.Add("reason", NpgsqlDbType.Text).Value = (object?)reason ?? DBNull.Value;
                c.Parameters.Add("detail", NpgsqlDbType.Text).Value = (object?)detail ?? DBNull.Value;
            });

    private static void ObservationParameters(NpgsqlCommand c, CorpusEntryId entry, OperationId operation, RouteId route, ModelId model)
    {
        c.Parameters.AddWithValue("entry", entry.Value);
        c.Parameters.AddWithValue("task", "ScriptPass");
        c.Parameters.AddWithValue("operation", operation.Value);
        c.Parameters.AddWithValue("route", route.Value);
        c.Parameters.AddWithValue("model", model.Value);
    }

    private async Task<RouteId> InsertProviderRouteAsync(
        CapabilityClass capability, string tier, ProviderAccountId account, ModelId model, int rated, string? stated = null)
    {
        var route = RouteId.New();
        await ExecuteAsync(
            """
            INSERT INTO routes (route_id, capability_class, tier, target_kind, provider_account_id, model_id,
                                rated_quality, context_capacity, terms_basis, terms_verified_on, reasoning_tier_stated)
            VALUES (@route, @capability, @tier, 'ProviderRoute', @account, @model, @rated, 200000,
                    'paid tier under commercial terms, a demonstration fixture', CURRENT_DATE, @stated);
            INSERT INTO route_availability (route_id, effective_from, state, reason)
            VALUES (@route, now() - interval '1 day', 'Serving', 'demonstration fixture: serving');
            """,
            c =>
            {
                c.Parameters.AddWithValue("route", route.Value);
                c.Parameters.AddWithValue("capability", capability.ToString());
                c.Parameters.AddWithValue("tier", tier);
                c.Parameters.AddWithValue("account", account.Value);
                c.Parameters.AddWithValue("model", model.Value);
                c.Parameters.AddWithValue("rated", rated);
                c.Parameters.Add("stated", NpgsqlDbType.Text).Value = (object?)stated ?? DBNull.Value;
            });
        return route;
    }

    private async Task<RouteId> InsertSubstituteRouteAsync(CapabilityClass capability, string tier, string? stated)
    {
        var route = RouteId.New();
        await ExecuteAsync(
            """
            INSERT INTO routes (route_id, capability_class, tier, target_kind, substitute_task,
                                rated_quality, context_capacity, terms_basis, terms_verified_on, reasoning_tier_stated)
            VALUES (@route, @capability, @tier, 'NonAiSubstitute', 'metadata-template-population', 70, 2000000,
                    'non-AI substitute; no provider account and no commercial terms', CURRENT_DATE, @stated);
            INSERT INTO route_availability (route_id, effective_from, state, reason)
            VALUES (@route, now() - interval '1 day', 'Serving', 'demonstration fixture: serving');
            """,
            c =>
            {
                c.Parameters.AddWithValue("route", route.Value);
                c.Parameters.AddWithValue("capability", capability.ToString());
                c.Parameters.AddWithValue("tier", tier);
                c.Parameters.Add("stated", NpgsqlDbType.Text).Value = (object?)stated ?? DBNull.Value;
            });
        return route;
    }

    private async Task InsertHoldRouteAsync(CapabilityClass capability, string tier)
    {
        var route = RouteId.New();
        await ExecuteAsync(
            """
            INSERT INTO routes (route_id, capability_class, tier, target_kind, hold_reason,
                                rated_quality, context_capacity, terms_basis, terms_verified_on)
            VALUES (@route, @capability, @tier, 'HoldAndEscalate', 'the recorded emergency position', 100, 2000000,
                    'no provider; the recorded emergency position', CURRENT_DATE);
            INSERT INTO route_availability (route_id, effective_from, state, reason)
            VALUES (@route, now() - interval '1 day', 'Serving', 'demonstration fixture: serving');
            """,
            c =>
            {
                c.Parameters.AddWithValue("route", route.Value);
                c.Parameters.AddWithValue("capability", capability.ToString());
                c.Parameters.AddWithValue("tier", tier);
            });
    }

    private Task InsertBudgetAsync(ChannelId channel, DateOnly month, decimal amount) =>
        ExecuteAsync(
            "INSERT INTO budgets (budget_id, scope_kind, scope_id, period, amount, currency) VALUES (gen_random_uuid(), 'Channel', @c, @p, @a, 'USD')",
            c =>
            {
                c.Parameters.AddWithValue("c", channel.Value);
                c.Parameters.Add("p", NpgsqlDbType.Date).Value = month;
                c.Parameters.AddWithValue("a", amount);
            });

    private async Task<(DateTimeOffset DecidedAt, DateOnly Month)> DecisionInstantAsync(OperationId operation)
    {
        await using var command = Source.CreateCommand("SELECT decided_at, booking_month FROM admission_decisions WHERE operation_id = @o");
        command.Parameters.AddWithValue("o", operation.Value);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), $"no decision is recorded for operation {operation}");
        return (reader.GetFieldValue<DateTimeOffset>(0), reader.GetFieldValue<DateOnly>(1));
    }

    private async Task<(OperationId Id, DateTimeOffset At)> OperationInstantAsync(string Outcome, bool latest = true)
    {
        await using var command = Source.CreateCommand(
            $"SELECT operation_id, occurred_at FROM agent_costs WHERE outcome = @o ORDER BY occurred_at {(latest ? "DESC" : "ASC")}, operation_id LIMIT 1");
        command.Parameters.AddWithValue("o", Outcome);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), $"no {Outcome} operation is recorded");
        return (new OperationId(reader.GetGuid(0)), reader.GetFieldValue<DateTimeOffset>(1));
    }

    private async Task<DateTimeOffset> HorizonAsync()
    {
        await using var command = Source.CreateCommand("SELECT horizon FROM audit_record_horizon WHERE only_row");
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.GetFieldValue<DateTimeOffset>(0);
    }

    private sealed record DecisionRow(
        string Action,
        string Basis,
        string BasisStatement,
        string TierStatement,
        string CompanyBasisStatement,
        string ReservationStatement,
        long ObservationsRankedOn,
        IReadOnlyList<Guid> Observations,
        IReadOnlyList<CandidateRow> Candidates);

    private sealed record CandidateRow(Guid Route, int Position, int ConfiguredRating, string QualityCase, decimal? QualityAmount, long Observations);

    private sealed record ReadingRow(
        string AmountCase,
        decimal? Amount,
        string SpendCase,
        decimal? SpendAmount,
        string? SpendDetail,
        int? Threshold,
        string Action,
        string? Reason);

    private async Task<DecisionRow> ReadDecisionAsync(OperationId operation)
    {
        string action, basis, basisStatement, tierStatement, companyBasis, reservation;
        long rankedOn;
        await using (var command = Source.CreateCommand(
            """
            SELECT action, basis, basis_statement, tier_statement, company_basis_statement, reservation_statement, observations_ranked_on
            FROM admission_decisions WHERE operation_id = @o
            """))
        {
            command.Parameters.AddWithValue("o", operation.Value);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), $"no decision is recorded for operation {operation}");
            (action, basis, basisStatement, tierStatement, companyBasis, reservation, rankedOn) = (
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                reader.GetString(5), reader.GetInt64(6));
        }

        var observations = new List<Guid>();
        await using (var command = Source.CreateCommand("SELECT observation_id FROM admission_decision_observations WHERE operation_id = @o"))
        {
            command.Parameters.AddWithValue("o", operation.Value);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                observations.Add(reader.GetGuid(0));
            }
        }

        var candidates = new List<CandidateRow>();
        await using (var command = Source.CreateCommand(
            """
            SELECT route_id, position, configured_rating, quality_case, quality_amount, observations
            FROM admission_decision_candidates WHERE operation_id = @o ORDER BY position
            """))
        {
            command.Parameters.AddWithValue("o", operation.Value);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                candidates.Add(new CandidateRow(
                    reader.GetGuid(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetDecimal(4), reader.GetInt64(5)));
            }
        }

        return new DecisionRow(action, basis, basisStatement, tierStatement, companyBasis, reservation, rankedOn, observations, candidates);
    }

    private async Task<ReadingRow> ReadReadingAsync(OperationId operation, string scope)
    {
        await using var command = Source.CreateCommand(
            """
            SELECT amount_case, amount, spend_case, spend_amount, spend_unmeasured_detail, threshold, action, reason
            FROM admission_decision_readings WHERE operation_id = @o AND scope_kind = @s
            """);
        command.Parameters.AddWithValue("o", operation.Value);
        command.Parameters.AddWithValue("s", scope);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), $"no {scope} reading is recorded for operation {operation}");
        return new ReadingRow(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetDecimal(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetDecimal(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetInt32(5),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7));
    }

    private async Task<DateTimeOffset> DatastoreNowAsync()
    {
        await using var command = Source.CreateCommand("SELECT clock_timestamp()");
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
        var result = await command.ExecuteScalarAsync();
        if (typeof(T) == typeof(object))
        {
            return (T)result!;
        }

        if (result is T typed)
        {
            return typed;
        }

        return (T)Convert.ChangeType(result!, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
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
        INSERT INTO provider_accounts (provider_account_id, provider, commercial_terms_basis, verified_on, status) VALUES
            ('provider-alpha', 'alpha', 'paid tier, a demonstration fixture', '2026-09-01', 'Active'),
            ('provider-beta', 'beta', 'paid tier, a demonstration fixture', '2026-09-01', 'Active');
        INSERT INTO models (model_id, provider_account_id, rated_quality, context_capacity, modality) VALUES
            ('alpha-reasoning', 'provider-alpha', 90, 200000, 'text'),
            ('beta-reasoning', 'provider-beta', 85, 200000, 'text'),
            ('alpha-unpriced', 'provider-alpha', 80, 200000, 'text');
        INSERT INTO model_prices (model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from, valid_to) VALUES
            (gen_random_uuid(), 'alpha-reasoning', 'InputUnit',  0.000003,  'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL),
            (gen_random_uuid(), 'alpha-reasoning', 'OutputUnit', 0.000015,  'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL),
            (gen_random_uuid(), 'alpha-reasoning', 'CachedUnit', 0.0000003, 'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL),
            (gen_random_uuid(), 'beta-reasoning',  'InputUnit',  0.000001,  'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL),
            (gen_random_uuid(), 'beta-reasoning',  'OutputUnit', 0.000005,  'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL),
            (gen_random_uuid(), 'beta-reasoning',  'CachedUnit', 0.0000001, 'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL),
            (gen_random_uuid(), 'alpha-unpriced',  'InputUnit',  0.000003,  'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL);
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

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    /// <summary>
    /// The STAND-IN PROVIDER. It implements the boundary's internal adapter port, reaches NOTHING — no
    /// network, no account, no credential — and returns a fixture attempt: 1,000 input and 500 output
    /// units, measured, in 1.2 seconds. A demonstration may run a step during the call, which is when the
    /// admission transaction is open and holds the record horizon shared.
    /// </summary>
    private sealed class StandInProvider(ProviderAccountId account, Func<Task>? during = null) : IProviderAdapter
    {
        public int Calls { get; private set; }

        public ProviderAccountId ProviderAccount { get; } = account;

        public async Task<ProviderAttempt> InvokeAsync(
            RouteTarget.ProviderRoute route, CapabilityRequest request, ScopedHandle handle, CancellationToken cancellationToken)
        {
            Calls++;
            if (during is not null)
            {
                await during();
            }

            return new ProviderAttempt(true, new UnitCounts(1_000, 500, 0, 0), CostBasis.Measurement, TimeSpan.FromMilliseconds(1_200), null, null);
        }
    }

    /// <summary>
    /// A stand-in credential broker. It holds NO SECRET and issues a scoped handle that attaches to nothing;
    /// the handle type's constructor is internal to the credentials assembly, so it is reached by reflection
    /// here, in a test, rather than by widening that assembly's visibility.
    /// </summary>
    private sealed class StandInBroker : ICredentialBroker
    {
        public Task<CredentialOutcome> IssueAsync(CredentialRequest request, CancellationToken cancellationToken)
        {
            var constructor = typeof(ScopedHandle).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(c => c.GetParameters().Length == 10);
            var handle = (ScopedHandle)constructor.Invoke(
            [
                Guid.NewGuid(), request.CredentialClass, request.ProviderAccount, request.Channel, request.Capability,
                request.Job, request.Stage, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), 1,
            ]);
            return Task.FromResult<CredentialOutcome>(new CredentialOutcome.Issued(handle));
        }
    }
}
