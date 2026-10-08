using MediaCompany.Application.Ports;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;
using MediaCompany.Persistence;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The datastore-side demonstrations the publishing capability adds.
///
/// EXACTLY-ONCE CANNOT BE DEMONSTRATED WITHOUT A DATASTORE. It is a property of a uniqueness
/// constraint and a transaction, and both live in PostgreSQL rather than in application code, so
/// an in-memory double would demonstrate the double rather than the property. Each test here is
/// therefore recorded as NOT-RUN when no instance is reachable, and never as a predicted pass.
///
/// The variable has three states worth distinguishing: unset, and these skip with a recorded
/// reason; set and reachable, and they execute; set but UNREACHABLE, and they FAIL rather than
/// skip, so a stale connection string reports a broken suite rather than an unexercised one.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class PublicationDispatchIntegrationTests : IAsyncLifetime
{
    private NpgsqlDataSource? _dataSource;
    private readonly FixedClock _clock = new(DateTimeOffset.Parse("2026-10-01T12:00:00Z"));

    /// <summary>A fixed clock, so every derived interval in these demonstrations is exact.</summary>
    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private NpgsqlDataSource Source => _dataSource
        ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    private static readonly CompanyId Company = CompanyId.New();
    private static readonly ChannelId Channel = ChannelId.New();
    private static readonly DepartmentId Department = DepartmentId.New();
    private static readonly AgentId Agent = AgentId.New();
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(3);
    private static readonly DateOnly Observed = new(2026, 9, 27);

    private static readonly DestinationDescriptor Destination =
        new("a-video-platform", "the-company-channel", "en-GB");

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
    // Exactly-once
    // -----------------------------------------------------------------------

    /// <summary>
    /// A dispatch repeated after a recorded SUCCESS leaves exactly one publication record.
    ///
    /// The key is derived from the item and its exact version, so the retry addresses the row that
    /// already exists rather than a new one, and the uniqueness constraint would refuse a second
    /// even if it tried.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ADispatchRepeatedAfterSuccessLeavesExactlyOneRecord()
    {
        var first = await DispatchAsync();
        var retry = await DispatchAsync();

        Assert.Equal(DispatchWriteOutcome.Created, first);
        Assert.Equal(DispatchWriteOutcome.AlreadyRecorded, retry);
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM publication_dispatches"));

        // Both attempts are recorded, because an attempt is not a dispatch: two attempts, one
        // record, which is what exactly-once means here.
        Assert.Equal(2L, await ScalarAsync<long>("SELECT COUNT(*) FROM publication_attempts"));
    }

    /// <summary>
    /// A dispatch repeated after a recorded FAILURE leaves exactly one publication record.
    ///
    /// The failed attempt rolls back and leaves no dispatch row, so the retry is the first write
    /// and there is still exactly one at the end.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ADispatchRepeatedAfterFailureLeavesExactlyOneRecord()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        // The failing attempt: written, then abandoned without a commit.
        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Dispatches.RecordDispatchAsync(Record(), CancellationToken.None);
            await transaction.Dispatches.RecordAttemptAsync(Attempt(refused: false), CancellationToken.None);
            // No commit.
        }

        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM publication_dispatches"));

        var retry = await DispatchAsync();

        Assert.Equal(DispatchWriteOutcome.Created, retry);
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM publication_dispatches"));
    }

    /// <summary>
    /// An interruption between the gate state change and the queue entry leaves NEITHER.
    ///
    /// This is the property the single transaction buys: there is no window in which the state
    /// moved and the queue entry did not, so no reconciliation is needed and no outbox exists.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnInterruptionBetweenTheStateChangeAndTheQueueEntryLeavesNeither()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        var job = JobId.New();
        await EnqueueJobAsync(job);

        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Dispatches.RecordDispatchAsync(Record(), CancellationToken.None);
            // A state change from the state the record holds, Draft: the datastore refuses a
            // from-state the record does not hold (the multi-channel change), so the interrupted
            // change starts where the item is recorded.
            await transaction.Gates.RecordTransitionAsync(
                Item, Version, GateState.Draft, GateState.AwaitingRightsCheck,
                "submitted, with the dispatch, in a transaction that is interrupted", _clock.UtcNow, CancellationToken.None);

            // The interruption lands here, between the state change and the queue entry.
        }

        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM publication_dispatches"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM gate_transitions"));
        Assert.Equal(
            "Ready",
            await ScalarAsync<string>("SELECT claim_state FROM jobs LIMIT 1"));
    }

    /// <summary>
    /// A concurrent duplicate dispatch leaves exactly one record.
    ///
    /// The two transactions serialize on the datastore's own key rather than on an application
    /// lock, so one creates and the other resolves to what is already there.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AConcurrentDuplicateDispatchLeavesExactlyOneRecord()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        var barrier = new SemaphoreSlim(0, 1);

        async Task<DispatchWriteOutcome> Racer(bool waits)
        {
            await using var transaction = await unitOfWork.BeginAsync(CancellationToken.None);
            if (waits)
            {
                await barrier.WaitAsync();
            }

            var outcome = await transaction.Dispatches
                .RecordDispatchAsync(Record(), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
            return outcome;
        }

        var second = Racer(waits: true);
        var first = await Racer(waits: false);
        barrier.Release();
        var secondOutcome = await second;

        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM publication_dispatches"));
        Assert.Equal(DispatchWriteOutcome.Created, first);
        Assert.Equal(DispatchWriteOutcome.AlreadyRecorded, secondOutcome);
    }

    /// <summary>A dispatch record refuses in-place amendment at the datastore.</summary>
    [RequiresPostgresFact]
    public async Task ADispatchRecordRefusesAmendment()
    {
        await DispatchAsync();

        var failure = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteAsync("UPDATE publication_dispatches SET destination = 'elsewhere'"));

        Assert.Contains("written once", failure.MessageText);
    }

    // -----------------------------------------------------------------------
    // The attempt record and its five answers
    // -----------------------------------------------------------------------

    /// <summary>Every recorded attempt, refused attempts included, answers all five questions.</summary>
    [RequiresPostgresFact]
    public async Task EveryRecordedAttemptAnswersTheFiveAuditQuestions()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Dispatches.RecordAttemptAsync(Attempt(refused: false), CancellationToken.None);
            await transaction.Dispatches.RecordAttemptAsync(Attempt(refused: true), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var answered = await ScalarAsync<long>(
            """
            SELECT COUNT(*) FROM publication_attempts
            WHERE item_id IS NOT NULL AND item_version IS NOT NULL
              AND length(btrim(destination)) > 0
              AND attempted_at IS NOT NULL
              AND length(btrim(metadata_digest)) > 0
              AND length(btrim(approved_by)) > 0
            """);

        Assert.Equal(2L, answered);

        // And the refused one additionally names the condition it failed.
        Assert.Equal(
            1L,
            await ScalarAsync<long>(
                "SELECT COUNT(*) FROM publication_attempts WHERE refused AND length(btrim(refusal_reason)) > 0"));
    }

    /// <summary>A refused attempt with no named condition is refused by the datastore.</summary>
    [RequiresPostgresFact]
    public async Task ARefusedAttemptWithNoNamedConditionIsRefused()
    {
        var failure = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO publication_attempts (
                attempt_id, dispatch_key, item_id, item_version, destination, attempted_at,
                metadata_digest, approved_by, refused, refusal_reason, refusal_detail)
            VALUES (gen_random_uuid(), @key, @item, @version, 'dest', @now, @digest, 'owner', true, NULL, NULL)
            """,
            c =>
            {
                c.Parameters.AddWithValue("key", new string('a', 64));
                c.Parameters.AddWithValue("item", Item.Value);
                c.Parameters.AddWithValue("version", Version.Value);
                c.Parameters.AddWithValue("now", _clock.UtcNow);
                c.Parameters.AddWithValue("digest", new string('b', 64));
            }));

        Assert.Contains("refusal_is_named", failure.MessageText + failure.ConstraintName);
    }

    // -----------------------------------------------------------------------
    // The three conditions of first publication
    // -----------------------------------------------------------------------

    /// <summary>
    /// The three-valued observation round-trips, and a channel with no rows yields a register in
    /// which all three resolve ABSENT and therefore refuse.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheConditionRegisterReadsRecordedObservationsAndRefusesWhenAbsent()
    {
        var ledger = new NpgsqlGateLedger(Source);

        var empty = await ledger.FirstPublicationConditionsAsync(Channel, CancellationToken.None);
        Assert.Equal(3, empty.Evaluate().Count);
        Assert.False(empty.AllSatisfied());
        Assert.All(empty.Evaluate(), s => Assert.Equal(ConditionResolution.Absent, s.Resolution));

        // The payment account is a COMPANY-LEVEL observation (the multi-channel change), recorded in
        // the company-level record and read through the channel's company; the other two are the
        // channel's own.
        await ExecuteAsync(
            """
            INSERT INTO first_publication_conditions (channel_id, condition, state, evidence, observed_on)
            VALUES (@channel, 'LibraryRegistration', 'NotSatisfied', 'not registered on any library', @on),
                   (@channel, 'TwoStepVerification', 'Unknown',      'unconfirmed; no strike stands', @on);
            INSERT INTO company_payment_account_observations (company_id, state, evidence, observed_on)
            SELECT company_id, 'NotSatisfied', 'the payment account does not exist', @on
            FROM channels WHERE channel_id = @channel
            """,
            c =>
            {
                c.Parameters.AddWithValue("channel", Channel.Value);
                c.Parameters.Add("on", NpgsqlDbType.Date).Value = Observed;
            });

        var recorded = await ledger.FirstPublicationConditionsAsync(Channel, CancellationToken.None);

        Assert.False(recorded.AllSatisfied());
        Assert.Equal(3, recorded.Unsatisfied().Count);

        // The unknown one stays unknown rather than collapsing into false.
        Assert.Equal(
            ConditionResolution.Unknown,
            recorded.Evaluate().Single(s => s.Condition == FirstPublicationCondition.TwoStepVerification).Resolution);
    }

    /// <summary>
    /// An observation with no evidence is refused at the datastore as well as in code, on the
    /// per-channel record and on the company-level payment-account record alike. The per-channel
    /// probe names a condition the per-channel record still admits, so it is the EVIDENCE check that
    /// refuses it rather than the refusal of a new per-channel payment-account row.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnUnevidencedObservationIsRefusedByTheDatastore()
    {
        var perChannel = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO first_publication_conditions (channel_id, condition, state, evidence, observed_on)
            VALUES (@channel, 'LibraryRegistration', 'Satisfied', '   ', @on)
            """,
            c =>
            {
                c.Parameters.AddWithValue("channel", Channel.Value);
                c.Parameters.Add("on", NpgsqlDbType.Date).Value = Observed;
            }));
        Assert.Equal(PostgresErrorCodes.CheckViolation, perChannel.SqlState);
        Assert.Equal("first_publication_conditions_evidence_check", perChannel.ConstraintName);

        var companyLevel = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO company_payment_account_observations (company_id, state, evidence, observed_on)
            SELECT company_id, 'Satisfied', '   ', @on FROM channels WHERE channel_id = @channel
            """,
            c =>
            {
                c.Parameters.AddWithValue("channel", Channel.Value);
                c.Parameters.Add("on", NpgsqlDbType.Date).Value = Observed;
            }));
        Assert.Equal(PostgresErrorCodes.CheckViolation, companyLevel.SqlState);
        Assert.Equal("company_payment_account_observations_evidence_check", companyLevel.ConstraintName);
    }

    // -----------------------------------------------------------------------
    // The approval marks
    // -----------------------------------------------------------------------

    /// <summary>
    /// An approval missing the queued mark resolves as UNMEASURED at the datastore, not as a zero
    /// queue time. The three quantities are derived there, from the recorded marks.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnApprovalMissingItsQueuedMarkResolvesAsUnmeasured()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Gates.RecordApprovalAsync(
                new Approval(
                    Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
                    "approved", _clock.UtcNow.AddMinutes(-10), _clock.UtcNow),
                CancellationToken.None);

            await transaction.Gates.RecordApprovalAsync(
                new Approval(
                    Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
                    "approved", _clock.UtcNow.AddMinutes(-60), _clock.UtcNow.AddMinutes(-45),
                    queuedAt: _clock.UtcNow.AddMinutes(-75)),
                CancellationToken.None);

            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.Equal(
            1L,
            await ScalarAsync<long>(
                "SELECT COUNT(*) FROM v_approval_measurements WHERE resolution = 'Unmeasured'"));

        // The unmeasured one has a NULL queue time, not a zero: they mean different things.
        Assert.Equal(
            1L,
            await ScalarAsync<long>(
                "SELECT COUNT(*) FROM v_approval_measurements WHERE queue_minutes IS NULL"));

        Assert.Equal(
            15L,
            await ScalarAsync<long>(
                "SELECT queue_minutes FROM v_approval_measurements WHERE queue_minutes IS NOT NULL"));
    }

    /// <summary>A queued mark later than the presentation is refused by the datastore.</summary>
    [RequiresPostgresFact]
    public async Task AQueuedMarkAfterThePresentationIsRefused()
    {
        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO approvals (item_id, item_version, gate, approver, verdict, reason,
                                   presented_at, decided_at, queued_at)
            VALUES (@item, @version, 'publication', 'Owner', 'Approved', 'ok', @presented, @decided, @queued)
            """,
            c =>
            {
                c.Parameters.AddWithValue("item", Item.Value);
                c.Parameters.AddWithValue("version", Version.Value);
                c.Parameters.AddWithValue("presented", _clock.UtcNow.AddMinutes(-10));
                c.Parameters.AddWithValue("decided", _clock.UtcNow);
                c.Parameters.AddWithValue("queued", _clock.UtcNow.AddMinutes(30));
            }));
    }

    // -----------------------------------------------------------------------
    // The served reasoning tier
    // -----------------------------------------------------------------------

    /// <summary>
    /// The served tier round-trips, and a null served value is stored as the explicit absence
    /// marker rather than being backfilled from the requested tier.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheServedTierIsStoredAsReadAndItsAbsenceStaysAbsent()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Operations.RecordAsync(
                Draft() with { ReasoningTierRequested = ReasoningTier.Deep, ReasoningTierServed = ReasoningTier.Deep },
                CancellationToken.None);

            await transaction.Operations.RecordAsync(
                Draft() with { ReasoningTierRequested = ReasoningTier.Deep, ReasoningTierServed = null },
                CancellationToken.None);

            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.Equal(
            2L,
            await ScalarAsync<long>("SELECT operations FROM v_served_tier_evidence"));

        // One record carries a served tier; the other carries the absence marker. The count of
        // records carrying tier evidence is a MEASURED count, and it is one here rather than two.
        Assert.Equal(
            1L,
            await ScalarAsync<long>("SELECT with_served_tier FROM v_served_tier_evidence"));
        Assert.Equal(
            1L,
            await ScalarAsync<long>("SELECT carrying_tier_evidence FROM v_served_tier_evidence"));
    }

    // -----------------------------------------------------------------------
    // helpers
    // -----------------------------------------------------------------------

    private async Task<DispatchWriteOutcome> DispatchAsync()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        await using var transaction = await unitOfWork.BeginAsync(CancellationToken.None);
        var outcome = await transaction.Dispatches.RecordDispatchAsync(Record(), CancellationToken.None);
        await transaction.Dispatches.RecordAttemptAsync(Attempt(refused: false), CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return outcome;
    }

    private DispatchRecord Record() => new()
    {
        Key = DispatchKey.Derive(Item, Version),
        Item = Item,
        Version = Version,
        DestinationCanonical = Destination.Canonical,
        PlannedAt = _clock.UtcNow.AddDays(3),
        MetadataAndSettingsDigest = new string('b', 64),
        ComposedAt = _clock.UtcNow,
    };

    private AttemptRecord Attempt(bool refused) => new(
        Guid.NewGuid(),
        DispatchKey.Derive(Item, Version),
        Item,
        Version,
        Destination.Canonical,
        _clock.UtcNow,
        new string('b', 64),
        "Owner",
        refused,
        refused ? nameof(DispatchRefusalReason.FirstPublicationConditionUnmet) : string.Empty,
        refused ? "the payment account does not exist" : string.Empty);

    private OperationDraft Draft() => new()
    {
        Id = OperationId.New(),
        Run = RunId.New(),
        Attribution = new Attribution(Item, Channel, Department, Agent),
        Capability = CapabilityClass.EditorialReasoning,
        Model = new ModelId("alpha-reasoning"),
        Units = new UnitCounts(100, 50, 0, 0),
        CostBasis = CostBasis.Estimate,
        Duration = TimeSpan.FromSeconds(1),
        Outcome = OperationOutcome.Succeeded,
        OccurredAt = _clock.UtcNow,
        Attempt = 1,
    };

    private Task EnqueueJobAsync(JobId job) => ExecuteAsync(
        """
        INSERT INTO jobs (job_id, item_id, channel_id, workflow, position, claim_state, available_at)
        VALUES (@job, @item, @channel, 'publishing', 'Queued', 'Ready', @now)
        """,
        c =>
        {
            c.Parameters.AddWithValue("job", job.Value);
            c.Parameters.AddWithValue("item", Item.Value);
            c.Parameters.AddWithValue("channel", Channel.Value);
            c.Parameters.AddWithValue("now", _clock.UtcNow);
        });

    private Task SeedAsync() => ExecuteAsync(
        """
        INSERT INTO companies (company_id, name, operating_state) VALUES (@company, 'Media Company', 'building');
        INSERT INTO channels (channel_id, company_id, platform, language, registered)
            VALUES (@channel, @company, 'video-platform', 'en', false);
        INSERT INTO departments (department_id, company_id, name, budget_holder)
            VALUES (@department, @company, 'production', 'Owner');
        INSERT INTO workforce_agents (agent_id, department_id, name, role)
            VALUES (@agent, @department, 'scriptwriter', 'Producer');
        INSERT INTO items (item_id, channel_id, item_version, title)
            VALUES (@item, @channel, 3, 'the carried item');
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
}
