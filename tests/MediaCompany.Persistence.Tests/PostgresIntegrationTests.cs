using MediaCompany.Application.Ports;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Persistence;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// Skips the integration suite when no datastore is reachable, and says why.
///
/// A skipped demonstration is recorded as not-run rather than as passing. The implementation
/// report carries the same statement: these criteria are evidenced by design and by the static
/// schema checks, and their behavioural evidence is owed once a PostgreSQL instance exists.
/// </summary>
public sealed class RequiresPostgresFactAttribute : FactAttribute
{
    public RequiresPostgresFactAttribute()
    {
        if (PostgresIntegrationTests.ConnectionString is null)
        {
            Skip = "not-run: no PostgreSQL datastore is reachable. Set MEDIACOMPANY_TEST_CONNECTION_STRING to run.";
        }
    }
}

/// <summary>
/// The datastore-side demonstrations. Each exercises a property the technical design places in
/// PostgreSQL rather than in application code, so none of them can be evidenced by a unit test.
///
/// These are written against the schema as built and run unchanged the moment a datastore is
/// available; they are recorded as not-run in this implementation.
/// </summary>
public sealed class PostgresIntegrationTests : IAsyncLifetime
{
    internal static string? ConnectionString =>
        Environment.GetEnvironmentVariable("MEDIACOMPANY_TEST_CONNECTION_STRING");

    private NpgsqlDataSource? _dataSource;
    private readonly TestClock _clock = new(DateTimeOffset.Parse("2026-10-01T12:00:00Z"));

    private NpgsqlDataSource Source => _dataSource
        ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    public async Task InitializeAsync()
    {
        if (ConnectionString is null)
        {
            return;
        }

        _dataSource = NpgsqlDataSource.Create(ConnectionString);
        await DropEverythingAsync();
        await SchemaInstaller.InstallAsync(_dataSource, CancellationToken.None);
        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }
    }

    private static readonly CompanyId Company = CompanyId.New();
    private static readonly ChannelId Channel = ChannelId.New();
    private static readonly DepartmentId Department = DepartmentId.New();
    private static readonly AgentId Agent = AgentId.New();
    private static readonly ItemId Item = ItemId.New();
    private static readonly ProviderAccountId Account = new("provider-alpha");
    private static readonly ModelId ModelName = new("alpha-reasoning");
    private static readonly DateOnly Period = new(2026, 10, 1);

    private static Attribution Attribution() => new(Item, Channel, Department, Agent);

    /// <summary>
    /// Constraint C-005 and decision D-006. The cost is computed by the datastore from the unit
    /// counts and the applied unit prices, and equals the same arithmetic computed independently
    /// here — which is exactly what acceptance criterion AC-004 asks to be demonstrated.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheDatastoreComputedCostEqualsAnIndependentRecomputation()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Operations.RecordAsync(Draft(inputUnits: 1_000, outputUnits: 500), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var reader = new NpgsqlCostReader(Source);
        var stored = await reader.CostForItemAsync(Item, CancellationToken.None);

        // Recomputed independently from the seeded prices: 1000 * 0.000003 + 500 * 0.000015.
        var expected = (1_000m * 0.000_003m) + (500m * 0.000_015m);

        Assert.Equal(expected, stored.Amount);
    }

    /// <summary>
    /// Assumption A-006 and risk R-006: the cost stays re-derivable after the price row that
    /// produced it has been superseded, because the row stores the applied price and its
    /// reference.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ACostStaysReDerivableAfterItsPriceIsSuperseded()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Operations.RecordAsync(Draft(1_000, 500), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var before = await new NpgsqlCostReader(Source).CostForItemAsync(Item, CancellationToken.None);

        await ExecuteAsync(
            """
            UPDATE model_prices SET valid_to = @now WHERE model_id = @model AND valid_to IS NULL;
            INSERT INTO model_prices (model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from, valid_to)
            VALUES (gen_random_uuid(), @model, 'InputUnit', 0.000009, 'USD', 'a later price', @today, @now, NULL);
            """,
            c =>
            {
                c.Parameters.AddWithValue("now", _clock.UtcNow);
                c.Parameters.AddWithValue("model", ModelName.Value);
                c.Parameters.Add("today", NpgsqlDbType.Date).Value = Period;
            });

        var after = await new NpgsqlCostReader(Source).CostForItemAsync(Item, CancellationToken.None);

        Assert.Equal(before.Amount, after.Amount);
    }

    /// <summary>
    /// Constraint C-004 under an induced crash. The transaction that records the operation and its
    /// audit entry is rolled back, and neither is durable: there is no state in which the
    /// operation happened and its record did not.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnInducedCrashBetweenTheOperationAndItsRecordLeavesNeither()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Operations.RecordAsync(Draft(1_000, 500), CancellationToken.None);
            await transaction.Audit.AppendAsync(Entry(), CancellationToken.None);
            // No commit: the disposal rolls back, standing in for a crash.
        }

        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM agent_costs"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM audit_entries"));
    }

    /// <summary>Decision D-011: a retry is its own record, so two attempts produce two rows.</summary>
    [RequiresPostgresFact]
    public async Task EachAttemptIsItsOwnOperationRecord()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        foreach (var attempt in new[] { 1, 2, 3 })
        {
            await using var transaction = await unitOfWork.BeginAsync(CancellationToken.None);
            await transaction.Operations.RecordAsync(
                Draft(100, 50) with { Id = OperationId.New(), Attempt = attempt },
                CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.Equal(3L, await ScalarAsync<long>("SELECT COUNT(*) FROM agent_costs"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM v_incomplete_attribution"));
    }

    /// <summary>
    /// Constraint C-014 and acceptance criterion AC-009. An amendment is refused by the datastore,
    /// the refusal is appended as its own entry naming the entry it was attempted against, and the
    /// original entry is returned unchanged afterwards.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnAmendmentIsRefusedTheRefusalIsRecordedAndTheOriginalIsUnchanged()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        AuditEntry original;
        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            original = await transaction.Audit.AppendAsync(Entry(), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var thrown = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            "UPDATE audit_entries SET reason = 'rewritten' WHERE entry_id = @id",
            c => c.Parameters.AddWithValue("id", original.Id.Value)));

        Assert.Contains("append-only", thrown.Message, StringComparison.OrdinalIgnoreCase);

        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Audit.AppendRefusalAsync(
                original.Id, AuditRefusalReason.AmendmentAttempted, "operator", CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var reasonNow = await ScalarAsync<string>(
            $"SELECT reason FROM audit_entries WHERE entry_id = '{original.Id.Value}'");
        Assert.Equal(original.Reason, reasonNow);

        var refusalCount = await ScalarAsync<long>(
            $"SELECT COUNT(*) FROM audit_entries WHERE action = 'record.amendment-refused' AND subject = 'audit-entry:{original.Id}'");
        Assert.Equal(1L, refusalCount);
    }

    /// <summary>A deletion is refused the same way.</summary>
    [RequiresPostgresFact]
    public async Task ADeletionOfARecordedEntryIsRefused()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        AuditEntry original;
        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            original = await transaction.Audit.AppendAsync(Entry(), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            "DELETE FROM audit_entries WHERE entry_id = @id",
            c => c.Parameters.AddWithValue("id", original.Id.Value)));
    }

    /// <summary>
    /// Acceptance criterion AC-027: driving recorded cost past 50, 75, 90 and 100 per cent in turn
    /// raises one alert at each crossing, and re-evaluating raises none.
    /// </summary>
    [RequiresPostgresFact]
    public async Task EachThresholdCrossingRaisesExactlyOneAlert()
    {
        var budgetId = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO budgets (budget_id, scope_kind, scope_id, period, amount, currency)
            VALUES (@id, 'Channel', @channel, @period, 1.00, 'USD')
            """,
            c =>
            {
                c.Parameters.AddWithValue("id", budgetId);
                c.Parameters.AddWithValue("channel", Channel.Value);
                c.Parameters.Add("period", NpgsqlDbType.Date).Value = Period;
            });

        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        var raised = new List<int>();

        // 0.000003 per input unit: each step drives utilization past the next threshold.
        foreach (var units in new[] { 200_000L, 100_000L, 60_000L, 50_000L })
        {
            await using var transaction = await unitOfWork.BeginAsync(CancellationToken.None);
            await transaction.Operations.RecordAsync(
                Draft(units, 0) with { Id = OperationId.New() }, CancellationToken.None);
            var alerts = await transaction.Budgets.EvaluateAsync(
                Attribution(), Period, _clock.UtcNow, CancellationToken.None);
            raised.AddRange(alerts.Select(a => a.Threshold));
            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.Equal([50, 75, 90, 100], raised);

        // Re-evaluating raises nothing: the alert is idempotent per budget, period and threshold.
        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            var again = await transaction.Budgets.EvaluateAsync(
                Attribution(), Period, _clock.UtcNow, CancellationToken.None);
            Assert.Empty(again);
            await transaction.CommitAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Constraint C-001 at the datastore: a route matching the forbidden-source register cannot be
    /// inserted at all, independently of the application check.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AForbiddenRouteCannotBeInsertedAtAll()
    {
        await ExecuteAsync(
            """
            INSERT INTO forbidden_sources (kind, identifier, reason, evidence_reference)
            VALUES ('ConsumerChatSubscription', 'provider-alpha', 'prohibited by terms', 'policy-dossier')
            """);

        var thrown = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO routes (route_id, capability_class, tier, target_kind, provider_account_id, model_id,
                                rated_quality, context_capacity, terms_basis, terms_verified_on)
            VALUES (gen_random_uuid(), 'EditorialReasoning', 'Primary', 'ProviderRoute', 'provider-alpha',
                    'alpha-reasoning', 90, 200000, 'paid tier', '2026-09-20')
            """));

        Assert.Contains("forbidden source", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Decision D-003 layer three at the datastore: no bypass into Published.</summary>
    [RequiresPostgresFact]
    public async Task AGateBypassIntoPublishedIsRefused()
    {
        var thrown = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO gate_transitions (item_id, item_version, occurred_at, from_state, to_state, reason)
            VALUES (@item, 1, now(), 'Draft', 'Published', 'bypass attempt')
            """,
            c => c.Parameters.AddWithValue("item", Item.Value)));

        Assert.Contains("reachable only from Approved", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Acceptance criterion AC-012 at the datastore: a self-approved exception is refused.</summary>
    [RequiresPostgresFact]
    public async Task ASelfApprovedExceptionIsRefusedAtTheDatastore()
    {
        await ExecuteAsync(
            """
            INSERT INTO controls (control_id, description, non_cuttable) VALUES ('CTRL-1', 'a control', false);
            INSERT INTO control_subject_roles (control_id, role) VALUES ('CTRL-1', 'Producer');
            INSERT INTO exception_requests (exception_id, control_id, requester, reason, requested_at)
            VALUES ('EXC-1', 'CTRL-1', 'Producer', 'convenience', now());
            """);

        var thrown = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO exception_approvals (exception_id, approver, granted, reason, decided_at)
            VALUES ('EXC-1', 'Producer', true, 'self', now())
            """));

        Assert.Contains("may not approve it", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Acceptance criterion AC-018: elapsed minutes are computed by the datastore from the two
    /// recorded timestamps, and no writer supplies one.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ElapsedMinutesAreComputedByTheDatastore()
    {
        await ExecuteAsync(
            """
            INSERT INTO approvals (item_id, item_version, gate, approver, verdict, reason, presented_at, decided_at)
            VALUES (@item, 1, 'publication', 'Owner', 'Approved', 'looks right',
                    '2026-10-01T09:00:00Z', '2026-10-01T09:47:00Z')
            """,
            c => c.Parameters.AddWithValue("item", Item.Value));

        var elapsed = await ScalarAsync<int>("SELECT elapsed_minutes FROM approvals LIMIT 1");
        Assert.Equal(47, elapsed);

        var missing = await ScalarAsync<long>("SELECT COUNT(*) FROM approvals WHERE elapsed_minutes IS NULL");
        Assert.Equal(0L, missing);
    }

    /// <summary>
    /// Acceptance criterion AC-024: two workers claiming concurrently take different jobs, because
    /// the claim skips locked rows.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TwoWorkersClaimDifferentJobs()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        await using (var seed = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            for (var i = 0; i < 2; i++)
            {
                await seed.Jobs.EnqueueAsync(
                    new MediaCompany.Domain.Work.Job
                    {
                        Id = JobId.New(),
                        Item = Item,
                        Channel = Channel,
                        Workflow = "single-item",
                        Position = MediaCompany.Domain.Work.LifecyclePosition.Queued,
                        ClaimState = MediaCompany.Domain.Work.ClaimState.Ready,
                        AvailableAt = _clock.UtcNow.AddMinutes(-1),
                    },
                    CancellationToken.None);
            }

            await seed.CommitAsync(CancellationToken.None);
        }

        await using var first = await unitOfWork.BeginAsync(CancellationToken.None);
        await using var second = await unitOfWork.BeginAsync(CancellationToken.None);

        var a = await first.Jobs.ClaimNextAsync("worker-a", TimeSpan.FromMinutes(5), CancellationToken.None);
        var b = await second.Jobs.ClaimNextAsync("worker-b", TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.NotEqual(a!.Id, b!.Id);

        await first.CommitAsync(CancellationToken.None);
        await second.CommitAsync(CancellationToken.None);
    }

    /// <summary>
    /// Acceptance criterion AC-021: a price change takes effect as configuration alone, and the
    /// prior value stays retrievable with its validity dates.
    /// </summary>
    [RequiresPostgresFact]
    public async Task APriceChangeRetainsThePriorValueWithItsValidityDates()
    {
        var store = new NpgsqlConfigurationStore(Source);

        await store.SupersedeAsync(
            new MediaCompany.Domain.Configuration.ConfigurationVersion
            {
                Key = MediaCompany.Domain.Configuration.ConfigurationKeys.ModelPrice,
                Scope = ModelName.Value,
                Value = "0.000003",
                Version = 1,
                ChangedBy = "Operator",
                Reason = "initial, ESTIMATE",
                ValidFrom = _clock.UtcNow.AddDays(-2),
            },
            CancellationToken.None);

        await store.SupersedeAsync(
            new MediaCompany.Domain.Configuration.ConfigurationVersion
            {
                Key = MediaCompany.Domain.Configuration.ConfigurationKeys.ModelPrice,
                Scope = ModelName.Value,
                Value = "0.000009",
                Version = 2,
                ChangedBy = "Operator",
                Reason = "re-fetched before spend",
                ValidFrom = _clock.UtcNow,
            },
            CancellationToken.None);

        var current = await store.InForceAsync(
            MediaCompany.Domain.Configuration.ConfigurationKeys.ModelPrice, ModelName.Value,
            _clock.UtcNow, CancellationToken.None);
        Assert.Equal("0.000009", current?.Value);

        var history = await store.HistoryAsync(
            MediaCompany.Domain.Configuration.ConfigurationKeys.ModelPrice, ModelName.Value, CancellationToken.None);
        Assert.Equal(2, history.Count);
        Assert.Equal("0.000003", history[0].Value);
        Assert.NotNull(history[0].ValidTo);
    }

    /// <summary>A configuration key outside the closed admitted set is refused.</summary>
    [RequiresPostgresFact]
    public async Task AnUnadmittedConfigurationKeyIsRefused()
    {
        var store = new NpgsqlConfigurationStore(Source);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SupersedeAsync(
            new MediaCompany.Domain.Configuration.ConfigurationVersion
            {
                Key = "gate.owner-approval-required",
                Scope = "global",
                Value = "false",
                Version = 1,
                ChangedBy = "Operator",
                Reason = "attempt to configure the approval step away",
                ValidFrom = _clock.UtcNow,
            },
            CancellationToken.None));
    }

    /// <summary>
    /// Acceptance criterion AC-017 at the datastore: a row attributed to the named deterministic
    /// set cannot carry a price, so its cost is zero.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ADeterministicTaskRowCarriesZeroCost()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Operations.RecordAsync(
                Draft(0, 0) with { DeterministicTaskName = MediaCompany.Deterministic.DeterministicTaskRegistry.Render },
                CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var cost = await new NpgsqlCostReader(Source)
            .CostForDeterministicSetAsync(Period, CancellationToken.None);

        Assert.Equal(0m, cost.Amount);
    }

    // -----------------------------------------------------------------------

    private static OperationDraft Draft(long inputUnits, long outputUnits) => new()
    {
        Id = OperationId.New(),
        Run = RunId.New(),
        Attribution = Attribution(),
        Capability = CapabilityClass.EditorialReasoning,
        Route = null,
        Model = ModelName,
        Units = new UnitCounts(inputUnits, outputUnits, 0, 0),
        CostBasis = CostBasis.Measurement,
        Duration = TimeSpan.FromMilliseconds(250),
        Outcome = OperationOutcome.Succeeded,
        OccurredAt = DateTimeOffset.Parse("2026-10-01T12:00:00Z"),
        Attempt = 1,
    };

    private static AuditEntryDraft Entry() => new()
    {
        Actor = "producer",
        Action = "capability.resolve-and-invoke",
        Subject = $"item:{Item}",
        Reason = "resolved to the primary route",
        InputsReference = "job:1 stage:Script",
        OutputsReference = "operation:1",
        Decision = "resolved",
        CostReference = "operation:1",
        Risk = "none",
        RetentionClass = RetentionClass.FinancialRecord,
    };

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
                VALUES (@item, @channel, 1, 'first item');
            INSERT INTO provider_accounts (provider_account_id, provider, commercial_terms_basis, verified_on, status)
                VALUES ('provider-alpha', 'alpha', 'paid tier', '2026-09-01', 'Active');
            INSERT INTO models (model_id, provider_account_id, rated_quality, context_capacity, modality)
                VALUES ('alpha-reasoning', 'provider-alpha', 90, 200000, 'text');
            INSERT INTO model_prices (model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from, valid_to)
                VALUES (gen_random_uuid(), 'alpha-reasoning', 'InputUnit',  0.000003, 'USD', 'ESTIMATE, not verified first-hand', '2026-09-01', '2026-09-01T00:00:00Z', NULL),
                       (gen_random_uuid(), 'alpha-reasoning', 'OutputUnit', 0.000015, 'USD', 'ESTIMATE, not verified first-hand', '2026-09-01', '2026-09-01T00:00:00Z', NULL);
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

    private Task DropEverythingAsync() => ExecuteAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");

    private async Task ExecuteAsync(string sql, Action<NpgsqlCommand>? configure = null)
    {
        await using var command = Source.CreateCommand(sql);
        configure?.Invoke(command);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var command = Source.CreateCommand(sql);
        var result = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
