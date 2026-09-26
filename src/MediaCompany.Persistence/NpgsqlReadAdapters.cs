using MediaCompany.Application.Ports;
using MediaCompany.Domain.Accounting;
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

    public async Task RecordAsync(RouteAvailability availability, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(availability);

        await using var command = _dataSource.CreateCommand(
            """
            INSERT INTO route_availability (route_id, effective_from, state, reason, reset_or_probe_point, observed_quality)
            VALUES (@route_id, @effective_from, @state, @reason, @reset, @observed)
            ON CONFLICT (route_id, effective_from) DO NOTHING
            """);
        command.Parameters.AddWithValue("route_id", availability.Route.Value);
        command.Parameters.AddWithValue("effective_from", availability.EffectiveFrom);
        command.Parameters.AddWithValue("state", availability.State.ToString());
        command.Parameters.AddWithValue("reason", availability.Reason);
        command.Parameters.Add("reset", NpgsqlDbType.TimestampTz).Value =
            (object?)availability.ResetOrProbePoint ?? DBNull.Value;
        command.Parameters.Add("observed", NpgsqlDbType.Integer).Value =
            (object?)availability.ObservedQuality?.Value ?? DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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
public sealed class NpgsqlCostReader : ICostRollupReader, IBudgetReader
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
