using System.Globalization;
using MediaCompany.Application.Ports;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// The production record on one transaction (the production change, decisions D-010 and D-014 of its design). Every
/// instant is the datastore's, stamped in the statement or by the table's own trigger; no member takes one.
/// </summary>
internal sealed class NpgsqlProductionLedger : IProductionLedger
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    internal NpgsqlProductionLedger(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<ProductionVersionOpened> OpenProductionVersionAsync(ItemId item, ProductionMode mode, CancellationToken cancellationToken)
    {
        if (mode == ProductionMode.PlanOnly)
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "A plan-only run opens no version and writes nothing.");
        }

        // The next version is one above the highest recorded for the item; a concurrent production meets the
        // version key of the dossier header and its transaction ends with the datastore's refusal.
        await using var command = new NpgsqlCommand(
            """
            WITH next AS (
                SELECT COALESCE(max(item_version), 0) + 1 AS version FROM item_dossiers WHERE item_id = @item_id
            ),
            opened AS (
                INSERT INTO item_dossiers (item_id, item_version, opened_at)
                SELECT @item_id, next.version, clock_timestamp() FROM next
                RETURNING item_version, opened_at
            )
            SELECT item_version, opened_at FROM opened
            """,
            _connection,
            _transaction);
        command.Parameters.AddWithValue("item_id", item.Value);

        int version;
        DateTimeOffset openedAt;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            version = reader.GetInt32(0);
            openedAt = reader.GetFieldValue<DateTimeOffset>(1);
        }

        await using var header = new NpgsqlCommand(
            """
            INSERT INTO production_versions (item_id, item_version, mode, designation, recorded_at)
            VALUES (@item_id, @item_version, @mode, (SELECT designation FROM store_designation), clock_timestamp())
            """,
            _connection,
            _transaction);
        header.Parameters.AddWithValue("item_id", item.Value);
        header.Parameters.AddWithValue("item_version", version);
        header.Parameters.AddWithValue("mode", mode.ToString());
        await header.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return new ProductionVersionOpened(new ItemVersion(version), openedAt);
    }

    public async Task<ArtifactRecord> RecordArtifactAsync(ArtifactRecord draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO production_artifacts (
                item_id, item_version, relative_path, stage, role, implements, length_bytes, sha256, measured_duration_ms, recorded_at)
            VALUES (@item_id, @item_version, @relative_path, @stage, @role, @implements, @length, @sha256, @duration_ms, clock_timestamp())
            RETURNING recorded_at
            """,
            _connection,
            _transaction);
        command.Parameters.AddWithValue("item_id", draft.Item.Value);
        command.Parameters.AddWithValue("item_version", draft.Version.Value);
        command.Parameters.AddWithValue("relative_path", draft.RelativePath);
        command.Parameters.AddWithValue("stage", draft.Stage.ToString());
        command.Parameters.AddWithValue("role", draft.Role.ToString());
        command.Parameters.AddWithValue("implements", draft.Implements);
        command.Parameters.AddWithValue("length", draft.Length);
        command.Parameters.AddWithValue("sha256", draft.Sha256);
        command.Parameters.Add("duration_ms", NpgsqlDbType.Bigint).Value =
            draft.MeasuredDuration is { } d ? (long)Math.Round(d.TotalMilliseconds) : DBNull.Value;

        DateTimeOffset at;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            at = reader.GetFieldValue<DateTimeOffset>(0);
        }

        return draft with { RecordedAt = at };
    }
}

/// <summary>
/// The store's designation and the connected database's name (the production change, decision D-005 of its
/// design), read without a transaction. A store the ninth resource was never applied to reads no designation.
/// </summary>
public sealed class NpgsqlStoreDesignationReader : IStoreDesignationReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlStoreDesignationReader(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<StoreIdentity> ReadAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var exists = new NpgsqlCommand(
            "SELECT current_database(), to_regclass('store_designation') IS NOT NULL", connection);

        string database;
        bool hasRecord;
        await using (var reader = await exists.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            database = reader.GetString(0);
            hasRecord = reader.GetBoolean(1);
        }

        if (!hasRecord)
        {
            return new StoreIdentity(null, database, null);
        }

        await using var read = new NpgsqlCommand("SELECT designation, recorded_at FROM store_designation", connection);
        await using var row = await read.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await row.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? new StoreIdentity(Enum.Parse<StoreDesignation>(row.GetString(0)), database, row.GetFieldValue<DateTimeOffset>(1))
            : new StoreIdentity(null, database, null);
    }
}

/// <summary>
/// The reads of the produce path (the production change, decisions D-010, D-015 and D-017 of its design). Read
/// members only, each one statement on its own connection; every figure is the datastore's.
/// </summary>
public sealed class NpgsqlProductionReader : IProductionReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlProductionReader(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<RecordedPackage?> PackageAsync(ItemId item, ItemVersion packageVersion, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            """
            SELECT i.channel_id, i.title,
                   (SELECT COALESCE(max(d.item_version), 0) FROM item_dossiers d WHERE d.item_id = i.item_id),
                   (SELECT json_build_object(
                        'amount', c.amount, 'currency', c.currency, 'source', c.source,
                        'booked', COALESCE((SELECT sum(a.computed_cost) FROM agent_costs a
                                            WHERE a.item_id = c.item_id AND a.cost_stated IS TRUE), 0),
                        'unstated', (SELECT count(*) FROM agent_costs a
                                     WHERE a.item_id = c.item_id AND a.cost_stated IS NOT TRUE),
                        'open_amount', COALESCE((SELECT sum(r.worst_case_amount) FROM admission_reservations r
                                                 WHERE r.item_id = c.item_id
                                                   AND NOT EXISTS (SELECT 1 FROM agent_costs a WHERE a.operation_id = r.operation_id)), 0),
                        'open_count', (SELECT count(*) FROM admission_reservations r
                                       WHERE r.item_id = c.item_id
                                         AND NOT EXISTS (SELECT 1 FROM agent_costs a WHERE a.operation_id = r.operation_id)))
                    FROM item_caps c WHERE c.item_id = i.item_id)::text
            FROM items i
            WHERE i.item_id = @item_id
            """,
            connection);
        command.Parameters.AddWithValue("item_id", item.Value);

        ChannelId channel;
        string title;
        int highest;
        ItemCapReading? cap;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            channel = new ChannelId(reader.GetGuid(0));
            title = reader.GetString(1);
            highest = reader.GetInt32(2);
            cap = reader.IsDBNull(3) ? null : SnapshotJson.ItemCap(reader.GetString(3));
        }

        return new RecordedPackage
        {
            Item = item,
            Channel = channel,
            Title = title,
            PackageVersion = packageVersion,
            PackageStages = await StagesAsync(item, packageVersion, cancellationToken).ConfigureAwait(false),
            HighestVersion = new ItemVersion(highest),
            Cap = cap,
        };
    }

    public async Task<PlanPricing> PricingAsync(CapabilityClass capability, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            """
            WITH now_at AS (SELECT clock_timestamp() AS at),
            admitted AS (
                SELECT route_id, capability_class, tier, target_kind, provider_account_id, model_id,
                       substitute_task, hold_reason, rated_quality, context_capacity, terms_basis, terms_verified_on,
                       reasoning_tier_stated, automated_access_position, customer_content_position,
                       terms_positions_evidence, terms_positions_read_on
                FROM routes WHERE capability_class = @capability
            ),
            in_force AS (
                SELECT DISTINCT ON (p.model_id, p.unit_kind)
                       p.model_price_id, p.model_id, p.unit_kind, p.unit_price, p.currency, p.source, p.verified_on,
                       p.valid_from, p.valid_to
                FROM model_prices p, now_at
                WHERE p.model_id IN (SELECT model_id FROM admitted WHERE model_id IS NOT NULL)
                  AND p.valid_from <= now_at.at AND (p.valid_to IS NULL OR p.valid_to > now_at.at)
                ORDER BY p.model_id, p.unit_kind, p.unit_price DESC, p.valid_from DESC
            )
            SELECT (SELECT at FROM now_at),
                   (SELECT COALESCE(json_agg(a ORDER BY a.tier, a.route_id), '[]') FROM admitted a)::text,
                   (SELECT COALESCE(json_agg(i ORDER BY i.model_id, i.unit_kind), '[]') FROM in_force i)::text,
                   (SELECT COALESCE(json_object_agg(m.model_id, m.billed_kinds), json_build_object())
                    FROM models m WHERE m.model_id IN (SELECT model_id FROM admitted WHERE model_id IS NOT NULL)
                      AND m.billed_kinds IS NOT NULL)::text,
                   (SELECT COALESCE(json_agg(pa ORDER BY pa.provider), '[]') FROM provider_accounts pa)::text
            """,
            connection);
        command.Parameters.AddWithValue("capability", capability.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return new PlanPricing(
            reader.GetFieldValue<DateTimeOffset>(0),
            SnapshotJson.Routes(reader.GetString(1)),
            SnapshotJson.Prices(reader.GetString(2)),
            SnapshotJson.BilledKinds(reader.GetString(3)),
            SnapshotJson.Accounts(reader.GetString(4)));
    }

    public async Task<IReadOnlyList<ProducedStageOutcome>> StagesAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            """
            SELECT stage, outcome, summary, recorded_at
            FROM dossier_stage_evidence
            WHERE item_id = @item_id AND item_version = @item_version
            """,
            connection);
        command.Parameters.AddWithValue("item_id", item.Value);
        command.Parameters.AddWithValue("item_version", version.Value);

        var stages = new List<ProducedStageOutcome>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            stages.Add(new ProducedStageOutcome(
                Enum.Parse<ProductionStage>(reader.GetString(0)),
                Enum.Parse<StageOutcome>(reader.GetString(1)),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3)));
        }

        // Listed in the CLOSED stage order, whatever order they were recorded in.
        return stages.OrderBy(s => ProductionStageSet.All.ToList().IndexOf(s.Stage)).ToArray();
    }

    public async Task<IReadOnlyList<ArtifactRecord>> ArtifactsAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            """
            SELECT relative_path, stage, role, implements, length_bytes, sha256, measured_duration_ms, recorded_at
            FROM production_artifacts
            WHERE item_id = @item_id AND item_version = @item_version
            ORDER BY recorded_at, relative_path
            """,
            connection);
        command.Parameters.AddWithValue("item_id", item.Value);
        command.Parameters.AddWithValue("item_version", version.Value);

        var artifacts = new List<ArtifactRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            artifacts.Add(new ArtifactRecord
            {
                Item = item,
                Version = version,
                RelativePath = reader.GetString(0),
                Stage = Enum.Parse<ProductionStage>(reader.GetString(1)),
                Role = Enum.Parse<ArtifactRole>(reader.GetString(2)),
                Implements = reader.GetString(3),
                Length = reader.GetInt64(4),
                Sha256 = reader.GetString(5),
                MeasuredDuration = reader.IsDBNull(6) ? null : TimeSpan.FromMilliseconds(reader.GetInt64(6)),
                RecordedAt = reader.GetFieldValue<DateTimeOffset>(7),
            });
        }

        return artifacts;
    }
}

/// <summary>
/// The preparation (the production change, decision D-016 of its design): ONE TRANSACTION that records, from
/// recorded configuration and a recorded package, every row a production run reads, INSERTING WHERE ABSENT and
/// refusing, by name, any row that is present and differs. It records no budget amount and no configuration
/// value. The designation is written once: a store designated otherwise refuses the whole preparation.
/// </summary>
public sealed class NpgsqlPreparationWriter : IPreparationWriter
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlPreparationWriter(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<PreparationOutcome> PrepareAsync(
        PreparationRecord record,
        StoreDesignation designation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var run = new Run(connection, transaction);

        // The designation first: a store designated otherwise refuses everything, before a row is written.
        var designated = await run.ScalarAsync<string?>("SELECT designation FROM store_designation", _ => { }, cancellationToken).ConfigureAwait(false);
        if (designated is not null && designated != designation.ToString())
        {
            return new PreparationOutcome(0, 0,
                [$"the store is designated {designated}, so a {designation} preparation is refused; a designation is written once"]);
        }

        await run.EnsureAsync(
            "store designation",
            "INSERT INTO store_designation (only_row, designation, database_name, recorded_at) VALUES (true, @a, current_database(), clock_timestamp()) ON CONFLICT DO NOTHING",
            "SELECT designation FROM store_designation",
            [designation.ToString()],
            c => c.Parameters.AddWithValue("a", designation.ToString()),
            cancellationToken).ConfigureAwait(false);

        await run.EnsureAsync(
            $"company {record.Company}",
            "INSERT INTO companies (company_id, name, operating_state) VALUES (@id, @a, @b) ON CONFLICT DO NOTHING",
            "SELECT name, operating_state FROM companies WHERE company_id = @id",
            [record.CompanyName, record.CompanyOperatingState],
            c =>
            {
                c.Parameters.AddWithValue("id", record.Company.Value);
                c.Parameters.AddWithValue("a", record.CompanyName);
                c.Parameters.AddWithValue("b", record.CompanyOperatingState);
            },
            cancellationToken).ConfigureAwait(false);

        // The channel, with NO budget amount and NO configuration value: neither table is written.
        await run.EnsureAsync(
            $"channel {record.Channel}",
            "INSERT INTO channels (channel_id, company_id, platform, language, registered) VALUES (@id, @company, @a, @b, false) ON CONFLICT DO NOTHING",
            "SELECT platform, language, company_id::text FROM channels WHERE channel_id = @id",
            [record.ChannelPlatform, record.ChannelLanguage, record.Company.Value.ToString()],
            c =>
            {
                c.Parameters.AddWithValue("id", record.Channel.Value);
                c.Parameters.AddWithValue("company", record.Company.Value);
                c.Parameters.AddWithValue("a", record.ChannelPlatform);
                c.Parameters.AddWithValue("b", record.ChannelLanguage);
            },
            cancellationToken).ConfigureAwait(false);

        await run.EnsureAsync(
            $"department {record.Department}",
            "INSERT INTO departments (department_id, company_id, name, budget_holder) VALUES (@id, @company, @a, @b) ON CONFLICT DO NOTHING",
            "SELECT name, budget_holder FROM departments WHERE department_id = @id",
            [record.DepartmentName, record.DepartmentBudgetHolder],
            c =>
            {
                c.Parameters.AddWithValue("id", record.Department.Value);
                c.Parameters.AddWithValue("company", record.Company.Value);
                c.Parameters.AddWithValue("a", record.DepartmentName);
                c.Parameters.AddWithValue("b", record.DepartmentBudgetHolder);
            },
            cancellationToken).ConfigureAwait(false);

        await run.EnsureAsync(
            $"agent {record.Agent}",
            "INSERT INTO workforce_agents (agent_id, department_id, name, role) VALUES (@id, @department, @a, @b) ON CONFLICT DO NOTHING",
            "SELECT name, role FROM workforce_agents WHERE agent_id = @id",
            [record.AgentName, record.AgentRole],
            c =>
            {
                c.Parameters.AddWithValue("id", record.Agent.Value);
                c.Parameters.AddWithValue("department", record.Department.Value);
                c.Parameters.AddWithValue("a", record.AgentName);
                c.Parameters.AddWithValue("b", record.AgentRole);
            },
            cancellationToken).ConfigureAwait(false);

        foreach (var account in record.Accounts)
        {
            await run.EnsureAsync(
                $"provider account {account.Id}",
                """
                INSERT INTO provider_accounts (provider_account_id, provider, commercial_terms_basis, verified_on, status, credential_scope, authentication_scheme)
                VALUES (@id, @a, @b, @c, 'Active', @d, @e) ON CONFLICT DO NOTHING
                """,
                "SELECT provider, commercial_terms_basis, verified_on::text, credential_scope, authentication_scheme FROM provider_accounts WHERE provider_account_id = @id",
                [account.Provider, account.TermsBasis, Date(account.VerifiedOn), account.Scope.ToString(), account.Scheme.ToString()],
                c =>
                {
                    c.Parameters.AddWithValue("id", account.Id.Value);
                    c.Parameters.AddWithValue("a", account.Provider);
                    c.Parameters.AddWithValue("b", account.TermsBasis);
                    c.Parameters.AddWithValue("c", account.VerifiedOn);
                    c.Parameters.AddWithValue("d", account.Scope.ToString());
                    c.Parameters.AddWithValue("e", account.Scheme.ToString());
                },
                cancellationToken).ConfigureAwait(false);
        }

        foreach (var model in record.Models)
        {
            var kinds = model.BilledKinds.Select(k => k.ToString()).ToArray();
            await run.EnsureAsync(
                $"model {model.Id}",
                """
                INSERT INTO models (model_id, provider_account_id, rated_quality, context_capacity, modality, billed_kinds)
                VALUES (@id, @a, @b, @c, @d, @e) ON CONFLICT DO NOTHING
                """,
                "SELECT provider_account_id, rated_quality::text, context_capacity::text, modality, array_to_string(billed_kinds, ',') FROM models WHERE model_id = @id",
                [model.Account.Value, Number(model.RatedQuality.Value), Number(model.Context.Units), model.Modality, string.Join(',', kinds)],
                c =>
                {
                    c.Parameters.AddWithValue("id", model.Id.Value);
                    c.Parameters.AddWithValue("a", model.Account.Value);
                    c.Parameters.AddWithValue("b", model.RatedQuality.Value);
                    c.Parameters.AddWithValue("c", model.Context.Units);
                    c.Parameters.AddWithValue("d", model.Modality);
                    c.Parameters.AddWithValue("e", kinds);
                },
                cancellationToken).ConfigureAwait(false);
        }

        foreach (var route in record.Routes)
        {
            await run.EnsureAsync(
                $"route {route.Id}",
                """
                INSERT INTO routes (route_id, capability_class, tier, target_kind, provider_account_id, model_id, rated_quality,
                                    context_capacity, terms_basis, terms_verified_on, automated_access_position,
                                    customer_content_position, terms_positions_evidence, terms_positions_read_on)
                VALUES (@id, @a, @b, 'ProviderRoute', @c, @d, @e, @f, @g, @h, @i, @j, @k, @l) ON CONFLICT DO NOTHING
                """,
                """
                SELECT capability_class, tier, provider_account_id, model_id, rated_quality::text, context_capacity::text, terms_basis,
                       terms_verified_on::text, automated_access_position, customer_content_position, terms_positions_evidence,
                       terms_positions_read_on::text
                FROM routes WHERE route_id = @id
                """,
                [
                    route.Capability.ToString(), route.Tier.ToString(), route.Account.Value, route.Model.Value,
                    Number(route.RatedQuality.Value), Number(route.Context.Units), route.TermsBasis, Date(route.TermsVerifiedOn),
                    route.Positions.AutomatedAccess?.ToString(), route.Positions.CustomerContent?.ToString(),
                    route.Positions.EvidenceReference, route.Positions.EvidenceReadOn is { } read ? Date(read) : null,
                ],
                c =>
                {
                    c.Parameters.AddWithValue("id", route.Id.Value);
                    c.Parameters.AddWithValue("a", route.Capability.ToString());
                    c.Parameters.AddWithValue("b", route.Tier.ToString());
                    c.Parameters.AddWithValue("c", route.Account.Value);
                    c.Parameters.AddWithValue("d", route.Model.Value);
                    c.Parameters.AddWithValue("e", route.RatedQuality.Value);
                    c.Parameters.AddWithValue("f", route.Context.Units);
                    c.Parameters.AddWithValue("g", route.TermsBasis);
                    c.Parameters.AddWithValue("h", route.TermsVerifiedOn);
                    c.Parameters.Add("i", NpgsqlDbType.Text).Value = (object?)route.Positions.AutomatedAccess?.ToString() ?? DBNull.Value;
                    c.Parameters.Add("j", NpgsqlDbType.Text).Value = (object?)route.Positions.CustomerContent?.ToString() ?? DBNull.Value;
                    c.Parameters.Add("k", NpgsqlDbType.Text).Value = (object?)route.Positions.EvidenceReference ?? DBNull.Value;
                    c.Parameters.Add("l", NpgsqlDbType.Date).Value = (object?)route.Positions.EvidenceReadOn ?? DBNull.Value;
                },
                cancellationToken).ConfigureAwait(false);

            // A route with no recorded serving state is not a candidate (decision D-008 of the delivered design), so
            // the preparation records it serving once, at the datastore's instant, where no state is recorded.
            await run.ExecuteAsync(
                """
                INSERT INTO route_availability (route_id, effective_from, state, reason)
                SELECT @id, clock_timestamp(), 'Serving', 'recorded serving by the preparation, from recorded configuration'
                WHERE NOT EXISTS (SELECT 1 FROM route_availability WHERE route_id = @id)
                """,
                c => c.Parameters.AddWithValue("id", route.Id.Value),
                cancellationToken).ConfigureAwait(false);
        }

        foreach (var price in record.Prices)
        {
            await run.EnsureAsync(
                $"price {price.Id}",
                """
                INSERT INTO model_prices (model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from)
                VALUES (@id, @a, @b, @c, @d, @e, @f, @g) ON CONFLICT DO NOTHING
                """,
                "SELECT model_id, unit_kind, unit_price::text, currency, source, verified_on::text FROM model_prices WHERE model_price_id = @id",
                [price.Model.Value, price.Kind.ToString(), price.UnitPrice.ToString("0.0000000000", CultureInfo.InvariantCulture), price.Currency, price.Source, Date(price.VerifiedOn)],
                c =>
                {
                    c.Parameters.AddWithValue("id", price.Id.Value);
                    c.Parameters.AddWithValue("a", price.Model.Value);
                    c.Parameters.AddWithValue("b", price.Kind.ToString());
                    c.Parameters.Add("c", NpgsqlDbType.Numeric).Value = price.UnitPrice;
                    c.Parameters.AddWithValue("d", price.Currency);
                    c.Parameters.AddWithValue("e", price.Source);
                    c.Parameters.AddWithValue("f", price.VerifiedOn);
                    c.Parameters.AddWithValue("g", price.ValidFrom);
                },
                cancellationToken).ConfigureAwait(false);
        }

        await run.EnsureAsync(
            $"item {record.Item}",
            "INSERT INTO items (item_id, channel_id, item_version, title) VALUES (@id, @channel, @version, @a) ON CONFLICT DO NOTHING",
            "SELECT channel_id::text, title FROM items WHERE item_id = @id",
            [record.Channel.Value.ToString(), record.ItemTitle],
            c =>
            {
                c.Parameters.AddWithValue("id", record.Item.Value);
                c.Parameters.AddWithValue("channel", record.Channel.Value);
                c.Parameters.AddWithValue("version", record.PackageVersion.Value);
                c.Parameters.AddWithValue("a", record.ItemTitle);
            },
            cancellationToken).ConfigureAwait(false);

        // The recorded package's version: its dossier header and its twelve recorded outcomes, as the package records
        // them, each stamped by the datastore when first written.
        await run.EnsureAsync(
            $"dossier of item {record.Item} version {record.PackageVersion}",
            "INSERT INTO item_dossiers (item_id, item_version, opened_at) VALUES (@id, @version, clock_timestamp()) ON CONFLICT DO NOTHING",
            "SELECT item_version::text FROM item_dossiers WHERE item_id = @id AND item_version = @version",
            [record.PackageVersion.Value.ToString(CultureInfo.InvariantCulture)],
            c =>
            {
                c.Parameters.AddWithValue("id", record.Item.Value);
                c.Parameters.AddWithValue("version", record.PackageVersion.Value);
            },
            cancellationToken).ConfigureAwait(false);

        foreach (var stage in record.PackageStages)
        {
            await run.EnsureAsync(
                $"stage {stage.Stage} of item {record.Item} version {record.PackageVersion}",
                """
                INSERT INTO dossier_stage_evidence (item_id, item_version, stage, outcome, summary, recorded_at)
                VALUES (@id, @version, @a, @b, @c, clock_timestamp()) ON CONFLICT DO NOTHING
                """,
                "SELECT outcome, summary FROM dossier_stage_evidence WHERE item_id = @id AND item_version = @version AND stage = @a",
                [stage.Outcome.ToString(), stage.Summary],
                c =>
                {
                    c.Parameters.AddWithValue("id", record.Item.Value);
                    c.Parameters.AddWithValue("version", record.PackageVersion.Value);
                    c.Parameters.AddWithValue("a", stage.Stage.ToString());
                    c.Parameters.AddWithValue("b", stage.Outcome.ToString());
                    c.Parameters.AddWithValue("c", stage.Summary);
                },
                cancellationToken).ConfigureAwait(false);
        }

        await run.EnsureAsync(
            $"cap of item {record.Item}",
            "INSERT INTO item_caps (item_id, amount, currency, source, recorded_at) VALUES (@id, @a, @b, @c, clock_timestamp()) ON CONFLICT DO NOTHING",
            "SELECT amount::text, currency, source FROM item_caps WHERE item_id = @id",
            [record.Cap.Amount.ToString("0.00000000", CultureInfo.InvariantCulture), record.Cap.Currency, record.CapSource],
            c =>
            {
                c.Parameters.AddWithValue("id", record.Item.Value);
                c.Parameters.Add("a", NpgsqlDbType.Numeric).Value = record.Cap.Amount;
                c.Parameters.AddWithValue("b", record.Cap.Currency);
                c.Parameters.AddWithValue("c", record.CapSource);
            },
            cancellationToken).ConfigureAwait(false);

        if (run.Refusals.Count > 0)
        {
            // Nothing is half-prepared: a differing row refuses the whole preparation, and the transaction is rolled back.
            return new PreparationOutcome(0, 0, run.Refusals);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new PreparationOutcome(run.Inserted, run.Present, []);
    }

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>One preparation's statements on its one transaction, counting what it inserted, found and refused.</summary>
    private sealed class Run(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        public int Inserted { get; private set; }

        public int Present { get; private set; }

        public List<string> Refusals { get; } = [];

        public async Task<T?> ScalarAsync<T>(string sql, Action<NpgsqlCommand> bind, CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            bind(command);
            var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return value is null or DBNull ? default : (T)value;
        }

        public async Task ExecuteAsync(string sql, Action<NpgsqlCommand> bind, CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            bind(command);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Inserts where absent; where present, compares the stored values and refuses, by name, on a difference.</summary>
        public async Task EnsureAsync(
            string what,
            string insert,
            string select,
            IReadOnlyList<string?> expected,
            Action<NpgsqlCommand> bind,
            CancellationToken cancellationToken)
        {
            await using (var command = new NpgsqlCommand(insert, connection, transaction))
            {
                bind(command);
                if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1)
                {
                    Inserted++;
                    return;
                }
            }

            await using var read = new NpgsqlCommand(select, connection, transaction);
            bind(read);
            await using var reader = await read.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            for (var i = 0; i < expected.Count; i++)
            {
                var stored = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture);
                if (!string.Equals(stored, expected[i], StringComparison.Ordinal))
                {
                    Refusals.Add($"the recorded {what} differs from the configuration in column {reader.GetName(i)}: stored '{stored}', configured '{expected[i]}'");
                    return;
                }
            }

            Present++;
        }
    }
}
