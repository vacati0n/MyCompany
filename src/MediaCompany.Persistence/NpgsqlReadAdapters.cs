using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Configuration;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Rights;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>The route policy register and the forbidden-source register (module M-002).</summary>
public sealed class NpgsqlRouteRegistry : IRouteRegistry
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlRouteRegistry(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<Route>> AdmittedRoutesAsync(CapabilityClass capability, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT route_id, capability_class, tier, target_kind, provider_account_id, model_id,
                   substitute_task, hold_reason, rated_quality, context_capacity, terms_basis, terms_verified_on
            FROM routes
            WHERE capability_class = @capability
            ORDER BY tier
            """);
        command.Parameters.AddWithValue("capability", capability.ToString());

        var routes = new List<Route>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            RouteTarget target = reader.GetString(3) switch
            {
                "ProviderRoute" => new RouteTarget.ProviderRoute(
                    new ProviderAccountId(reader.GetString(4)),
                    new ModelId(reader.GetString(5))),
                "NonAiSubstitute" => new RouteTarget.NonAiSubstitute(reader.GetString(6)),
                "HoldAndEscalate" => new RouteTarget.HoldAndEscalate(reader.GetString(7)),
                var kind => throw new InvalidOperationException($"Unknown route target kind '{kind}'."),
            };

            routes.Add(new Route(
                new RouteId(reader.GetGuid(0)),
                Enum.Parse<CapabilityClass>(reader.GetString(1)),
                Enum.Parse<RouteTier>(reader.GetString(2)),
                target,
                new QualityRating(reader.GetInt32(8)),
                new ContextCapacity(reader.GetInt32(9)),
                reader.GetString(10),
                reader.GetFieldValue<DateOnly>(11)));
        }

        return routes;
    }

    public async Task<IReadOnlyList<ForbiddenSource>> ForbiddenSourcesAsync(CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT kind, identifier, reason, evidence_reference FROM forbidden_sources ORDER BY kind, identifier");

        var entries = new List<ForbiddenSource>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            entries.Add(new ForbiddenSource(
                Enum.Parse<ForbiddenSourceKind>(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)));
        }

        return entries;
    }
}

/// <summary>Route availability state (module M-004), current state in one seek per route.</summary>
public sealed class NpgsqlRouteAvailabilityLedger : IRouteAvailabilityLedger
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlRouteAvailabilityLedger(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyDictionary<RouteId, RouteAvailability>> CurrentAsync(
        IReadOnlyCollection<RouteId> routes,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<RouteId, RouteAvailability>();
        if (routes.Count == 0)
        {
            return result;
        }

        await using var command = _dataSource.CreateCommand(
            """
            SELECT DISTINCT ON (route_id)
                   route_id, state, effective_from, reason, reset_or_probe_point, observed_quality
            FROM route_availability
            WHERE route_id = ANY(@routes)
            ORDER BY route_id, effective_from DESC
            """);
        command.Parameters.Add("routes", NpgsqlDbType.Array | NpgsqlDbType.Uuid).Value =
            routes.Select(r => r.Value).ToArray();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var routeId = new RouteId(reader.GetGuid(0));
            result[routeId] = new RouteAvailability(
                routeId,
                Enum.Parse<AvailabilityState>(reader.GetString(1)),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
                reader.IsDBNull(5) ? null : new QualityRating(reader.GetInt32(5)));
        }

        return result;
    }
}

/// <summary>The operating registers (module M-011).</summary>
public sealed class NpgsqlOperatingRegisters : IOperatingRegisters
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlOperatingRegisters(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<ProviderAccount>> ProviderAccountsAsync(CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT provider_account_id, provider, commercial_terms_basis, verified_on, status FROM provider_accounts ORDER BY provider");

        var accounts = new List<ProviderAccount>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            accounts.Add(new ProviderAccount(
                new ProviderAccountId(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetFieldValue<DateOnly>(3),
                Enum.Parse<ProviderAccountStatus>(reader.GetString(4))));
        }

        return accounts;
    }

    public async Task<IReadOnlyList<Model>> ModelsAsync(CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT model_id, provider_account_id, rated_quality, context_capacity, modality FROM models ORDER BY model_id");

        var models = new List<Model>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            models.Add(new Model(
                new ModelId(reader.GetString(0)),
                new ProviderAccountId(reader.GetString(1)),
                new QualityRating(reader.GetInt32(2)),
                new ContextCapacity(reader.GetInt32(3)),
                reader.GetString(4)));
        }

        return models;
    }

    public async Task<IReadOnlyList<ModelPrice>> PricesInForceAsync(DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from, valid_to
            FROM model_prices
            WHERE valid_from <= @as_of AND (valid_to IS NULL OR valid_to > @as_of)
            ORDER BY model_id, unit_kind
            """);
        command.Parameters.AddWithValue("as_of", asOf);

        var prices = new List<ModelPrice>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            prices.Add(new ModelPrice(
                new ModelPriceId(reader.GetGuid(0)),
                new ModelId(reader.GetString(1)),
                Enum.Parse<PriceUnitKind>(reader.GetString(2)),
                reader.GetDecimal(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetFieldValue<DateOnly>(6),
                reader.GetFieldValue<DateTimeOffset>(7),
                reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8)));
        }

        return prices;
    }

    public async Task<IReadOnlyList<Channel>> ChannelsAsync(CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT channel_id, company_id, platform, language, registered FROM channels ORDER BY platform");

        var channels = new List<Channel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            channels.Add(new Channel(
                new ChannelId(reader.GetGuid(0)),
                new CompanyId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetBoolean(4)));
        }

        return channels;
    }

    public async Task<IReadOnlyList<Department>> DepartmentsAsync(CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT department_id, company_id, name, budget_holder FROM departments ORDER BY name");

        var departments = new List<Department>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            departments.Add(new Department(
                new DepartmentId(reader.GetGuid(0)),
                new CompanyId(reader.GetGuid(1)),
                reader.GetString(2),
                Enum.Parse<MediaCompany.Domain.Authority.WorkforceRole>(reader.GetString(3))));
        }

        return departments;
    }

    public async Task<IReadOnlyList<WorkforceAgent>> AgentsAsync(CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT agent_id, department_id, name, role FROM workforce_agents ORDER BY name");

        var agents = new List<WorkforceAgent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            agents.Add(new WorkforceAgent(
                new AgentId(reader.GetGuid(0)),
                new DepartmentId(reader.GetGuid(1)),
                reader.GetString(2),
                Enum.Parse<MediaCompany.Domain.Authority.WorkforceRole>(reader.GetString(3))));
        }

        return agents;
    }
}

/// <summary>The asset and rights ledger (module M-015).</summary>
public sealed class NpgsqlAssetLedger : IAssetLedger
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlAssetLedger(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<Asset>> ForItemAsync(ItemId item, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT asset_id, item_id, library, source, creator, licence_type, licence_reference,
                   commercial_use_permitted, modification_permitted, attribution_requirement,
                   platform_restrictions, expiry, proof_of_licence_reference, assessed_risk,
                   verified_by, verified_on
            FROM assets WHERE item_id = @item_id ORDER BY asset_id
            """);
        command.Parameters.AddWithValue("item_id", item.Value);

        var assets = new List<Asset>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            assets.Add(new Asset
            {
                Id = new AssetId(reader.GetGuid(0)),
                Item = new ItemId(reader.GetGuid(1)),
                Library = reader.GetString(2),
                Source = reader.IsDBNull(3) ? null : reader.GetString(3),
                Creator = reader.IsDBNull(4) ? null : reader.GetString(4),
                LicenceType = reader.IsDBNull(5) ? LicenceType.Unspecified : Enum.Parse<LicenceType>(reader.GetString(5)),
                LicenceReference = reader.IsDBNull(6) ? null : reader.GetString(6),
                CommercialUsePermitted = reader.IsDBNull(7) ? null : reader.GetBoolean(7),
                ModificationPermitted = reader.IsDBNull(8) ? null : reader.GetBoolean(8),
                AttributionRequirement = reader.IsDBNull(9) ? null : reader.GetString(9),
                PlatformRestrictions = reader.IsDBNull(10) ? null : reader.GetString(10),
                Expiry = reader.IsDBNull(11) ? null : reader.GetFieldValue<DateOnly>(11),
                ProofOfLicenceReference = reader.IsDBNull(12) ? null : reader.GetString(12),
                AssessedRisk = reader.IsDBNull(13) ? AssessedRisk.Unspecified : Enum.Parse<AssessedRisk>(reader.GetString(13)),
                VerifiedBy = reader.IsDBNull(14) ? null : Enum.Parse<MediaCompany.Domain.Authority.WorkforceRole>(reader.GetString(14)),
                VerifiedOn = reader.IsDBNull(15) ? null : reader.GetFieldValue<DateOnly>(15),
            });
        }

        return assets;
    }

    public async Task<IReadOnlyList<LibraryRegistration>> RegistrationsAsync(ChannelId channel, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT channel_id, library, registered_on FROM library_registrations WHERE channel_id = @channel_id");
        command.Parameters.AddWithValue("channel_id", channel.Value);

        var registrations = new List<LibraryRegistration>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            registrations.Add(new LibraryRegistration(
                new ChannelId(reader.GetGuid(0)),
                reader.GetString(1),
                reader.GetFieldValue<DateOnly>(2)));
        }

        return registrations;
    }

    public async Task RecordAsync(Asset asset, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(asset);

        await using var command = _dataSource.CreateCommand(
            """
            INSERT INTO assets (asset_id, item_id, library, source, creator, licence_type, licence_reference,
                                commercial_use_permitted, modification_permitted, attribution_requirement,
                                platform_restrictions, expiry, proof_of_licence_reference, assessed_risk,
                                verified_by, verified_on)
            VALUES (@asset_id, @item_id, @library, @source, @creator, @licence_type, @licence_reference,
                    @commercial, @modification, @attribution, @platform, @expiry, @proof, @risk,
                    @verified_by, @verified_on)
            """);
        command.Parameters.AddWithValue("asset_id", asset.Id.Value);
        command.Parameters.AddWithValue("item_id", asset.Item.Value);
        command.Parameters.AddWithValue("library", asset.Library);
        Add(command, "source", NpgsqlDbType.Text, asset.Source);
        Add(command, "creator", NpgsqlDbType.Text, asset.Creator);
        Add(command, "licence_type", NpgsqlDbType.Text,
            asset.LicenceType == LicenceType.Unspecified ? null : asset.LicenceType.ToString());
        Add(command, "licence_reference", NpgsqlDbType.Text, asset.LicenceReference);
        Add(command, "commercial", NpgsqlDbType.Boolean, asset.CommercialUsePermitted);
        Add(command, "modification", NpgsqlDbType.Boolean, asset.ModificationPermitted);
        Add(command, "attribution", NpgsqlDbType.Text, asset.AttributionRequirement);
        Add(command, "platform", NpgsqlDbType.Text, asset.PlatformRestrictions);
        Add(command, "expiry", NpgsqlDbType.Date, asset.Expiry);
        Add(command, "proof", NpgsqlDbType.Text, asset.ProofOfLicenceReference);
        Add(command, "risk", NpgsqlDbType.Text,
            asset.AssessedRisk == AssessedRisk.Unspecified ? null : asset.AssessedRisk.ToString());
        Add(command, "verified_by", NpgsqlDbType.Text, asset.VerifiedBy?.ToString());
        Add(command, "verified_on", NpgsqlDbType.Date, asset.VerifiedOn);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object? value) =>
        command.Parameters.Add(name, type).Value = value ?? DBNull.Value;
}

/// <summary>Temporal configuration (module M-012).</summary>
public sealed class NpgsqlConfigurationStore : IConfigurationStore
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlConfigurationStore(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<ConfigurationVersion?> InForceAsync(string key, string scope, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT config_key, scope, value, version, changed_by, reason, valid_from, valid_to
            FROM configuration
            WHERE config_key = @key AND scope = @scope
              AND valid_from <= @as_of AND (valid_to IS NULL OR valid_to > @as_of)
            ORDER BY valid_from DESC LIMIT 1
            """);
        command.Parameters.AddWithValue("key", key);
        command.Parameters.AddWithValue("scope", scope);
        command.Parameters.AddWithValue("as_of", asOf);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<ConfigurationVersion>> HistoryAsync(string key, string scope, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT config_key, scope, value, version, changed_by, reason, valid_from, valid_to
            FROM configuration WHERE config_key = @key AND scope = @scope ORDER BY valid_from
            """);
        command.Parameters.AddWithValue("key", key);
        command.Parameters.AddWithValue("scope", scope);

        var history = new List<ConfigurationVersion>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            history.Add(Map(reader));
        }

        return history;
    }

    /// <summary>
    /// Closes the interval of the value in force and inserts the next one. The prior row is never
    /// updated in place beyond its <c>valid_to</c>, so its value and its validity dates stay
    /// retrievable (acceptance criterion AC-021).
    /// </summary>
    public async Task SupersedeAsync(ConfigurationVersion next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!ConfigurationKeys.IsAdmitted(next.Key))
        {
            throw new InvalidOperationException(
                $"'{next.Key}' is not an admitted configuration key. The set is closed, which is what keeps the owner-approval step out of configuration.");
        }

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var close = new NpgsqlCommand(
            """
            UPDATE configuration SET valid_to = @valid_from
            WHERE config_key = @key AND scope = @scope AND valid_to IS NULL AND valid_from < @valid_from
            """,
            connection,
            transaction))
        {
            close.Parameters.AddWithValue("key", next.Key);
            close.Parameters.AddWithValue("scope", next.Scope);
            close.Parameters.AddWithValue("valid_from", next.ValidFrom);
            await close.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO configuration (config_key, scope, valid_from, valid_to, value, version, changed_by, reason)
            VALUES (@key, @scope, @valid_from, @valid_to, @value, @version, @changed_by, @reason)
            """,
            connection,
            transaction))
        {
            insert.Parameters.AddWithValue("key", next.Key);
            insert.Parameters.AddWithValue("scope", next.Scope);
            insert.Parameters.AddWithValue("valid_from", next.ValidFrom);
            insert.Parameters.Add("valid_to", NpgsqlDbType.TimestampTz).Value = (object?)next.ValidTo ?? DBNull.Value;
            insert.Parameters.AddWithValue("value", next.Value);
            insert.Parameters.AddWithValue("version", next.Version);
            insert.Parameters.AddWithValue("changed_by", next.ChangedBy);
            insert.Parameters.AddWithValue("reason", next.Reason);
            await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ConfigurationVersion Map(NpgsqlDataReader reader) => new()
    {
        Key = reader.GetString(0),
        Scope = reader.GetString(1),
        Value = reader.GetString(2),
        Version = reader.GetInt32(3),
        ChangedBy = reader.GetString(4),
        Reason = reader.GetString(5),
        ValidFrom = reader.GetFieldValue<DateTimeOffset>(6),
        ValidTo = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
    };
}

/// <summary>
/// Cost rollups and budget headroom (module M-006). Every figure here is produced by an
/// aggregation in the datastore's exact decimal type; this class reads results and performs no
/// money arithmetic (constraint C-005).
/// </summary>
public sealed class NpgsqlCostReader : ICostRollupReader, IBudgetReader, IServedTierReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlCostReader(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<Money> CostForItemAsync(ItemId item, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT COALESCE(total_cost, 0), COALESCE(currency, 'USD') FROM v_cost_per_item WHERE item_id = @item_id");
        command.Parameters.AddWithValue("item_id", item.Value);
        return await ReadMoneyAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Money> CostForPeriodAsync(DateOnly period, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT COALESCE(total_cost, 0), 'USD' FROM v_cost_per_period WHERE period = @period");
        command.Parameters.Add("period", NpgsqlDbType.Date).Value = period;
        return await ReadMoneyAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PeriodSummary> PeriodSummaryAsync(DateOnly period, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT total_cost, envelope_total, envelope_metered, envelope_standing,
                   variance_against_envelope, contains_estimates, operations
            FROM v_cost_per_period WHERE period = @period
            """);
        command.Parameters.Add("period", NpgsqlDbType.Date).Value = period;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // No operation was recorded in the period, so the whole envelope is unspent. The
            // variance is still the datastore's arithmetic on the next read; here there is
            // nothing to aggregate.
            return new PeriodSummary(
                period,
                Money.Zero(),
                ApprovedEnvelope.MonthlyTotal,
                ApprovedEnvelope.Metered,
                ApprovedEnvelope.Standing,
                new Money(-ApprovedEnvelope.MonthlyTotal.Amount),
                ContainsEstimates: false,
                Operations: 0);
        }

        return new PeriodSummary(
            period,
            new Money(reader.GetDecimal(0)),
            new Money(reader.GetDecimal(1)),
            new Money(reader.GetDecimal(2)),
            new Money(reader.GetDecimal(3)),
            new Money(reader.GetDecimal(4)),
            reader.GetBoolean(5),
            reader.GetInt64(6));
    }

    public async Task<IReadOnlyDictionary<CapabilityClass, Money>> CostByCapabilityAsync(DateOnly period, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT capability_class, total_cost FROM v_cost_by_capability WHERE period = @period");
        command.Parameters.Add("period", NpgsqlDbType.Date).Value = period;

        var result = new Dictionary<CapabilityClass, Money>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result[Enum.Parse<CapabilityClass>(reader.GetString(0))] = new Money(reader.GetDecimal(1));
        }

        return result;
    }

    public async Task<Money> CostForDeterministicSetAsync(DateOnly period, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT COALESCE(SUM(total_cost), 0), 'USD' FROM v_deterministic_ai_cost WHERE period = @period");
        command.Parameters.Add("period", NpgsqlDbType.Date).Value = period;
        return await ReadMoneyAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Money> RemainingAsync(Attribution attribution, DateOnly period, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attribution);

        // The subtraction is a numeric expression in the datastore, not application arithmetic.
        await using var command = _dataSource.CreateCommand(
            """
            SELECT COALESCE(MIN(b.amount - u.utilized), 0), COALESCE(MIN(b.currency), 'USD')
            FROM fn_budgets_for(@channel_id, @department_id, @period) b
            CROSS JOIN LATERAL fn_budget_utilization(b.budget_id) u
            """);
        command.Parameters.AddWithValue("channel_id", attribution.Channel.Value);
        command.Parameters.AddWithValue("department_id", attribution.Department.Value);
        command.Parameters.Add("period", NpgsqlDbType.Date).Value = period;

        return await ReadMoneyAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// One item's cost position, mirroring the delivered period summary. No row in the view means
    /// no operation is recorded for the item, which is a different fact from a recorded cost of
    /// zero and is reported as a count of zero operations rather than as a zero amount.
    /// </summary>
    public async Task<ItemSummary> ItemSummaryAsync(ItemId item, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT total_cost, COALESCE(currency, 'USD'), contains_estimates, operations
            FROM v_cost_per_item WHERE item_id = @item_id
            """);
        command.Parameters.AddWithValue("item_id", item.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return new ItemSummary(item, Money.Zero(), ContainsEstimates: false, Operations: 0);
        }

        return new ItemSummary(
            item,
            new Money(reader.GetDecimal(0), reader.GetString(1)),
            reader.GetBoolean(2),
            reader.GetInt64(3));
    }

    /// <summary>
    /// The three-state item cost. The rule that turns an aggregation into a measurement state
    /// lives in one place, in the analytics composer, so the adapter and the surface cannot drift.
    /// </summary>
    public async Task<MeasurementQuantity> ItemCostQuantityAsync(ItemId item, CancellationToken cancellationToken)
    {
        var summary = await ItemSummaryAsync(item, cancellationToken).ConfigureAwait(false);

        return AnalyticsComposers.FromOperations(
            summary.Operations, summary.Total, $"no operation is recorded for item {item}");
    }

    /// <summary>The three-state period cost, on the same rule.</summary>
    public async Task<MeasurementQuantity> PeriodCostQuantityAsync(DateOnly period, CancellationToken cancellationToken)
    {
        var summary = await PeriodSummaryAsync(period, cancellationToken).ConfigureAwait(false);

        return AnalyticsComposers.FromOperations(
            summary.Operations, summary.Total, $"no operation is recorded in period {period:yyyy-MM}");
    }

    /// <summary>
    /// The recorded tier pair of every accounted operation in a period. Both columns are read as
    /// recorded; a null served tier is returned as null, which is the explicit absence marker the
    /// resolution boundary wrote, and nothing here substitutes the requested tier for it.
    /// </summary>
    public async Task<IReadOnlyList<ServedTierRecord>> RecordsForPeriodAsync(
        DateOnly period,
        CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT operation_id, period, reasoning_tier_requested, reasoning_tier_served
            FROM agent_costs WHERE period = @period ORDER BY occurred_at, operation_id
            """);
        command.Parameters.Add("period", NpgsqlDbType.Date).Value = period;

        var records = new List<ServedTierRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            records.Add(new ServedTierRecord
            {
                Operation = new OperationId(reader.GetGuid(0)),
                Period = reader.GetFieldValue<DateOnly>(1),
                Requested = reader.IsDBNull(2) ? null : Enum.Parse<ReasoningTier>(reader.GetString(2)),
                Served = reader.IsDBNull(3) ? null : Enum.Parse<ReasoningTier>(reader.GetString(3)),
            });
        }

        return records;
    }

    private static async Task<Money> ReadMoneyAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return Money.Zero();
        }

        return new Money(reader.GetDecimal(0), reader.GetString(1));
    }
}

/// <summary>
/// The observed-revenue-parameter register (decision D-003).
///
/// The register is created empty by the appended schema resource and this change records no row in
/// it. A row carrying no source observation, or no date for one, is returned as recorded and
/// admits nothing: the admission rule sits in one place, in the domain type's own factory, so a
/// redefinition of what counts as observed changes one site.
/// </summary>
public sealed class NpgsqlRevenueParameterRegister : IRevenueParameterRegister
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlRevenueParameterRegister(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<RevenueParameterRecord>> RecordedAsync(CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT amount, unit, source_observation, observed_on
            FROM observed_revenue_parameters ORDER BY recorded_at, parameter_id
            """);

        var records = new List<RevenueParameterRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var unit = reader.GetString(1);

            records.Add(new RevenueParameterRecord
            {
                Observation = reader.IsDBNull(0)
                    ? MeasurementQuantity.NotMeasured(
                        UnmeasuredReason.NoObservationExists,
                        "the register row records no amount")
                    : MeasurementQuantity.Observed(reader.GetDecimal(0), unit),
                SourceObservation = reader.IsDBNull(2) ? null : reader.GetString(2),
                ObservedOn = reader.IsDBNull(3) ? null : reader.GetFieldValue<DateOnly>(3),
            });
        }

        return records;
    }

    public async Task RecordAsync(RevenueParameterRecord parameter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        await using var command = _dataSource.CreateCommand(
            """
            INSERT INTO observed_revenue_parameters
                (parameter_id, recorded_at, amount, unit, source_observation, observed_on)
            VALUES (@parameter_id, now(), @amount, @unit, @source_observation, @observed_on)
            """);

        command.Parameters.AddWithValue("parameter_id", Guid.NewGuid());
        command.Parameters.Add("amount", NpgsqlDbType.Numeric).Value =
            parameter.Observation is MeasurementQuantity.ObservedValue observed
                ? observed.Amount
                : (object)DBNull.Value;
        command.Parameters.AddWithValue("unit", UnitOf(parameter.Observation));
        command.Parameters.Add("source_observation", NpgsqlDbType.Text).Value =
            (object?)parameter.SourceObservation ?? DBNull.Value;
        command.Parameters.Add("observed_on", NpgsqlDbType.Date).Value =
            (object?)parameter.ObservedOn ?? DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The unit an observed case carries; an unmeasured row states the unit it was sought in.</summary>
    private static string UnitOf(MeasurementQuantity observation) => observation switch
    {
        MeasurementQuantity.ObservedValue value => value.Unit,
        MeasurementQuantity.ObservedZero zero => zero.Unit,
        _ => Money.DefaultCurrency,
    };
}
