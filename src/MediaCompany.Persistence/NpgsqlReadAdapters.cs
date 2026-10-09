using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Configuration;
using MediaCompany.Domain.Dossier;
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
                   substitute_task, hold_reason, rated_quality, context_capacity, terms_basis, terms_verified_on,
                   reasoning_tier_stated
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

            // The stated tier is passed EXPLICITLY on every row, never left to the constructor's
            // default. A row stating none maps to the domain's absent tier, which means the route
            // cannot state one — never to a default tier, and never to anything derived from a
            // request, which this adapter does not see.
            ReasoningTier? statedTier = reader.IsDBNull(12)
                ? null
                : Enum.Parse<ReasoningTier>(reader.GetString(12));

            routes.Add(new Route(
                new RouteId(reader.GetGuid(0)),
                Enum.Parse<CapabilityClass>(reader.GetString(1)),
                Enum.Parse<RouteTier>(reader.GetString(2)),
                target,
                new QualityRating(reader.GetInt32(8)),
                new ContextCapacity(reader.GetInt32(9)),
                reader.GetString(10),
                reader.GetFieldValue<DateOnly>(11),
                statedReasoningTier: statedTier));
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

        if (!IsAdmittedPairing(next.Key, next.Scope))
        {
            throw new InvalidOperationException(
                $"'{next.Key}' under scope '{next.Scope}' is not an admitted configuration key and scope. The set is closed, "
                + "which is what keeps the owner-approval step out of configuration: a base key is admitted under any scope "
                + "but a channel's, and a channel key only under the scope of exactly one channel.");
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

    /// <summary>
    /// The admitted key-and-scope pairings. A base key is admitted under any scope that does not name
    /// a channel; a channel key (the multi-channel change, decision D-001) only under the scope of
    /// exactly one channel, and only where it reaches no control. Every other pairing is refused, so
    /// no per-channel value can be written under a base key and no base key can be scoped to a channel.
    /// </summary>
    internal static bool IsAdmittedPairing(string key, string scope)
    {
        var channelScope = ChannelConfigurationKeys.IsChannelScope(scope);

        if (ChannelConfigurationKeys.IsAdmitted(key))
        {
            return channelScope && !ChannelConfigurationKeys.ReachesAControl(key);
        }

        return ConfigurationKeys.IsAdmitted(key) && !channelScope;
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
    /// The headroom in the datastore's booking month, the month and the amount in one statement. The
    /// subtraction is the datastore's, as in the delivered member.
    /// </summary>
    public async Task<BookedHeadroom> RemainingInBookingMonthAsync(Attribution attribution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attribution);

        await using var command = _dataSource.CreateCommand(
            """
            WITH booking AS (
                SELECT (date_trunc('month', GREATEST(clock_timestamp(), horizon) AT TIME ZONE 'UTC'))::date AS month
                FROM audit_record_horizon WHERE only_row
            )
            SELECT booking.month,
                   COALESCE((SELECT MIN(b.amount - u.utilized)
                             FROM fn_budgets_for(@channel_id, @department_id, booking.month) b
                             CROSS JOIN LATERAL fn_budget_utilization(b.budget_id) u), 0),
                   COALESCE((SELECT MIN(b.currency) FROM fn_budgets_for(@channel_id, @department_id, booking.month) b), 'USD')
            FROM booking
            """);
        command.Parameters.AddWithValue("channel_id", attribution.Channel.Value);
        command.Parameters.AddWithValue("department_id", attribution.Department.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("The record horizon is missing; the fifth schema resource creates it.");
        }

        return new BookedHeadroom(reader.GetFieldValue<DateOnly>(0), new Money(reader.GetDecimal(1), reader.GetString(2)));
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
            SELECT amount, unit, unmeasured_reason, unmeasured_detail, source_observation, observed_on
            FROM observed_revenue_parameters ORDER BY recorded_at, parameter_id
            """);

        var records = new List<RevenueParameterRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            records.Add(new RevenueParameterRecord
            {
                Observation = ReadMeasurement(reader),
                SourceObservation = reader.IsDBNull(4) ? null : reader.GetString(4),
                ObservedOn = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5),
            });
        }

        return records;
    }

    /// <summary>
    /// Reads the measurement case the row was written in, rather than deducing it from whether a
    /// number is present.
    ///
    /// An amount of exactly zero is an OBSERVED ZERO and comes back as one: the factory routes it
    /// to that case, so the distinction between a quantity observed to be zero and a quantity
    /// never observed survives the round trip. The earlier shape of this adapter wrote an observed
    /// zero as a null amount and read it back as unmeasured, which collapsed the two in the only
    /// delivered path that persists a measurement quantity.
    /// </summary>
    private static MeasurementQuantity ReadMeasurement(NpgsqlDataReader reader)
    {
        if (!reader.IsDBNull(0))
        {
            return MeasurementQuantity.Observed(reader.GetDecimal(0), reader.GetString(1));
        }

        // The table check admits no row carrying neither an amount nor a reason, so the reason and
        // the detail are present here by construction; a store that somehow held one anyway would
        // be reported as unmeasured for the reason it states rather than for one assumed here.
        return MeasurementQuantity.NotMeasured(
            Enum.Parse<UnmeasuredReason>(reader.GetString(2)),
            reader.GetString(3));
    }

    public async Task RecordAsync(RevenueParameterRecord parameter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        await using var command = _dataSource.CreateCommand(
            """
            INSERT INTO observed_revenue_parameters
                (parameter_id, recorded_at, amount, unit, unmeasured_reason, unmeasured_detail,
                 source_observation, observed_on)
            VALUES (@parameter_id, now(), @amount, @unit, @unmeasured_reason, @unmeasured_detail,
                    @source_observation, @observed_on)
            """);

        command.Parameters.AddWithValue("parameter_id", Guid.NewGuid());

        // The case decides the row shape, and the three cases are written apart. An observed zero
        // is written as an amount of zero with its unit, NOT as a null amount: a null amount is
        // the unmeasured shape, and writing a measured zero into it is what turned a measured zero
        // into a never-measured one on the way back out.
        var (amount, unit, reason, detail) = Encode(parameter.Observation);

        command.Parameters.Add("amount", NpgsqlDbType.Numeric).Value = (object?)amount ?? DBNull.Value;
        command.Parameters.Add("unit", NpgsqlDbType.Text).Value = (object?)unit ?? DBNull.Value;
        command.Parameters.Add("unmeasured_reason", NpgsqlDbType.Text).Value = (object?)reason ?? DBNull.Value;
        command.Parameters.Add("unmeasured_detail", NpgsqlDbType.Text).Value = (object?)detail ?? DBNull.Value;
        command.Parameters.Add("source_observation", NpgsqlDbType.Text).Value =
            (object?)parameter.SourceObservation ?? DBNull.Value;
        command.Parameters.Add("observed_on", NpgsqlDbType.Date).Value =
            (object?)parameter.ObservedOn ?? DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The row shape for one measurement case. The match is exhaustive over the closed union, so a
    /// fourth case would not compile rather than falling into an observed or an unmeasured shape
    /// by default.
    /// </summary>
    private static (decimal? Amount, string? Unit, string? Reason, string? Detail) Encode(
        MeasurementQuantity observation) => observation switch
    {
        MeasurementQuantity.ObservedValue value => (value.Amount, value.Unit, null, null),
        MeasurementQuantity.ObservedZero zero => (0m, zero.Unit, null, null),
        MeasurementQuantity.Unmeasured unmeasured =>
            (null, null, unmeasured.Reason.ToString(), unmeasured.Detail),
        _ => throw new InvalidOperationException("Unreachable: the measurement union has three cases."),
    };
}

/// <summary>
/// The throughput reader over the append-only record (decision D-003 of the accepted design).
///
/// It selects the ACTION and the INSTANT of each entry and nothing else: no reason, subject,
/// reference or other free-form column is read, so no recorded content can reach a throughput
/// reading. The action names are the ones the writers use, read from their one declaration. The
/// read instant is the datastore's own clock, the one the queue claim already measures against.
/// Every count comes back BARE; the measurement case is decided at the composing site, not here.
/// </summary>
public sealed class NpgsqlThroughputReader : IThroughputReader
{
    /// <summary>How many non-waiting attempts at the record horizon a read makes before reading unmeasured.</summary>
    public const int HorizonAttempts = 3;

    /// <summary>The pause between two attempts. The read never waits on an in-flight transaction itself.</summary>
    public static readonly TimeSpan HorizonRetryPause = TimeSpan.FromMilliseconds(50);

    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlThroughputReader(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    /// <summary>
    /// Closes the record and counts. The read obtains the record horizon EXCLUSIVELY WITHOUT
    /// WAITING, which the datastore grants only when no audited transaction holds it shared — that
    /// is, when no entry is in flight. It then raises the horizon to the later of the datastore's
    /// clock and its prior value, and takes every count in statements issued after the grant, so
    /// every entry that can ever be stamped below the new horizon is already committed and counted.
    /// If every attempt meets an in-flight transaction, no count is taken and the summary says so.
    /// </summary>
    public async Task<ThroughputSummary> ReadAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= HorizonAttempts; attempt++)
        {
            var summary = await TryReadAsync(periodStart, periodEnd, cancellationToken).ConfigureAwait(false);
            if (summary is not null)
            {
                return summary;
            }

            if (attempt < HorizonAttempts)
            {
                await Task.Delay(HorizonRetryPause, cancellationToken).ConfigureAwait(false);
            }
        }

        return new ThroughputSummary(
            periodStart, periodEnd, EarliestEntry: null, Quiet: false, Horizon: null,
            0, 0, 0, 0, 0, 0, 0, 0);
    }

    private async Task<ThroughputSummary?> TryReadAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        // The delivered closure, now shared with the month readings over the operation record (the
        // multi-channel change): taken without waiting, and raised when granted.
        if (await RecordHorizonClosure.TryCloseAsync(connection, transaction, cancellationToken).ConfigureAwait(false)
            is not { } horizon)
        {
            // An audited transaction is in flight. Never wait on it: report the attempt failed.
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        await using var command = new NpgsqlCommand(
            """
            SELECT
                (SELECT min(occurred_at) FROM audit_entries)                                            AS earliest,
                count(*) FILTER (WHERE action = @enqueued)                                              AS enqueued_to_close,
                count(*) FILTER (WHERE action = @succeeded)                                             AS succeeded_to_close,
                count(*) FILTER (WHERE action = @retried)                                               AS retried_to_close,
                count(*) FILTER (WHERE action = @claimed)                                               AS claimed_to_close,
                count(*) FILTER (WHERE action = @claimed   AND occurred_at >= @period_start)            AS claimed_in_period,
                count(*) FILTER (WHERE action = @retried   AND occurred_at >= @period_start)            AS retried_in_period,
                count(*) FILTER (WHERE action = @escalated AND occurred_at >= @period_start)            AS escalated_in_period,
                count(*) FILTER (WHERE action = @completed AND occurred_at >= @period_start)            AS completed_in_period
            FROM audit_entries
            WHERE action = ANY(@actions) AND occurred_at < @period_end
            """,
            connection,
            transaction);

        command.Parameters.AddWithValue("enqueued", Domain.Work.LifecycleActions.Enqueued);
        command.Parameters.AddWithValue("succeeded", Domain.Work.LifecycleActions.StageSucceeded);
        command.Parameters.AddWithValue("retried", Domain.Work.LifecycleActions.StageRetried);
        command.Parameters.AddWithValue("claimed", Domain.Work.LifecycleActions.Claimed);
        command.Parameters.AddWithValue("escalated", Domain.Work.LifecycleActions.StageEscalated);
        command.Parameters.AddWithValue("completed", Domain.Work.LifecycleActions.Completed);
        command.Parameters.AddWithValue("actions", Domain.Work.LifecycleActions.All.ToArray());
        command.Parameters.AddWithValue("period_start", periodStart);
        command.Parameters.AddWithValue("period_end", periodEnd);

        ThroughputSummary summary;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            summary = new ThroughputSummary(
                periodStart,
                periodEnd,
                reader.IsDBNull(0) ? null : reader.GetFieldValue<DateTimeOffset>(0),
                Quiet: true,
                Horizon: horizon,
                EnqueuedToClose: reader.GetInt64(1),
                StageSucceededToClose: reader.GetInt64(2),
                RetriedToClose: reader.GetInt64(3),
                ClaimedToClose: reader.GetInt64(4),
                ClaimedInPeriod: reader.GetInt64(5),
                RetriedInPeriod: reader.GetInt64(6),
                EscalatedInPeriod: reader.GetInt64(7),
                CompletedInPeriod: reader.GetInt64(8));
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return summary;
    }
}

/// <summary>
/// The item dossier reader over the dossier register (decision D-004 of the accepted design).
///
/// Every member answers for ONE item version, and returns null only where no dossier header
/// exists for it. Rows come back in the order they were recorded, and every closed value is read by
/// name, so a recorded row reads back in the case it was written in.
/// </summary>
public sealed class NpgsqlItemDossierReader : IItemDossierReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlItemDossierReader(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<RecordedDossier?> DossierAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        // One snapshot for the whole reconstitution, so a component recorded mid-read cannot give
        // one member a row the others do not see.
        await using var transaction = await connection
            .BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);

        var header = await HeaderAsync(connection, transaction, item, version, cancellationToken).ConfigureAwait(false);
        if (header is null)
        {
            return null;
        }

        var stages = await StagesAsync(connection, transaction, item, version, cancellationToken).ConfigureAwait(false);
        var audit = await SupplyAsync(connection, transaction, item, version, cancellationToken).ConfigureAwait(false);
        var determinations = await DeterminationRowsAsync(connection, transaction, item, version, cancellationToken)
            .ConfigureAwait(false);
        var components = await ComponentsAsync(connection, transaction, item, version, cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        var dossier = new ItemDossier
        {
            Item = item,
            Version = version,
            Stages = stages,
            Treatment = components.OfType<DossierComponent.Treatment>().Select(c => c.Verdict).ToArray(),
            Audience = components.OfType<DossierComponent.Audience>().SingleOrDefault()?.Designation,
            Visuals = components.OfType<DossierComponent.Visual>().Select(c => c.Provenance).ToArray(),
            ClipOrigins = components.OfType<DossierComponent.ClipOrigin>().Select(c => c.Assessment).ToArray(),
            Claims = components.OfType<DossierComponent.Claim>().Select(c => c.Attribution).ToArray(),
            SupplyAudit = audit,
            Determinations = determinations,
            Runtime = components.OfType<DossierComponent.Runtime>().SingleOrDefault()?.Duration,
            Metadata = components.OfType<DossierComponent.Metadata>().SingleOrDefault()?.Surfaces,
            Originality = components.OfType<DossierComponent.Originality>().SingleOrDefault()?.Assessment,
        };

        return new RecordedDossier(dossier, header.Value.Channel, header.Value.OpenedAt);
    }

    public async Task<IReadOnlyList<StageEvidence>?> StageEvidenceAsync(
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken) =>
        await ReadIfOpenedAsync(item, version, StagesAsync, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<SupplyAuditEntry>?> SupplyAuditAsync(
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken) =>
        await ReadIfOpenedAsync(item, version, SupplyAsync, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<DeterminationResolution>?> DeterminationsAsync(
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken) =>
        await ReadIfOpenedAsync(item, version, DeterminationRowsAsync, cancellationToken).ConfigureAwait(false);

    private delegate Task<IReadOnlyList<T>> RowReader<T>(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken);

    private async Task<IReadOnlyList<T>?> ReadIfOpenedAsync<T>(
        ItemId item,
        ItemVersion version,
        RowReader<T> rows,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection
            .BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);

        var header = await HeaderAsync(connection, transaction, item, version, cancellationToken).ConfigureAwait(false);
        var result = header is null
            ? null
            : await rows(connection, transaction, item, version, cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static NpgsqlCommand Command(
        string sql,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ItemId item,
        ItemVersion version)
    {
        var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("item_id", item.Value);
        command.Parameters.AddWithValue("item_version", version.Value);
        return command;
    }

    private static async Task<(ChannelId Channel, DateTimeOffset OpenedAt)?> HeaderAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken)
    {
        await using var command = Command(
            """
            SELECT i.channel_id, d.opened_at
            FROM item_dossiers d JOIN items i ON i.item_id = d.item_id
            WHERE d.item_id = @item_id AND d.item_version = @item_version
            """,
            connection, transaction, item, version);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return (new ChannelId(reader.GetGuid(0)), reader.GetFieldValue<DateTimeOffset>(1));
    }

    private static async Task<IReadOnlyList<StageEvidence>> StagesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken)
    {
        await using var command = Command(
            """
            SELECT stage, outcome, summary, recorded_at, evidence_reference
            FROM dossier_stage_evidence
            WHERE item_id = @item_id AND item_version = @item_version
            ORDER BY row_no
            """,
            connection, transaction, item, version);

        var rows = new List<StageEvidence>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(new StageEvidence
            {
                Stage = Enum.Parse<Domain.Production.ProductionStage>(reader.GetString(0)),
                Outcome = Enum.Parse<Domain.Work.StageOutcome>(reader.GetString(1)),
                Summary = reader.GetString(2),
                RecordedAt = reader.GetFieldValue<DateTimeOffset>(3),
                EvidenceReference = reader.IsDBNull(4) ? null : reader.GetString(4),
            });
        }

        return rows;
    }

    private static async Task<IReadOnlyList<SupplyAuditEntry>> SupplyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken)
    {
        await using var command = Command(
            """
            SELECT requested_term, library, fidelity, audited_at, term_answered, clip_count, unobtained_reason,
                   what_would_obtain_it, prior_reported_total, prior_observed_on, prior_source, prior_fidelity,
                   recorded_unavailable
            FROM dossier_supply_audit_entries
            WHERE item_id = @item_id AND item_version = @item_version
            ORDER BY row_no
            """,
            connection, transaction, item, version);

        var rows = new List<SupplyAuditEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(new SupplyAuditEntry
            {
                RequestedTerm = reader.GetString(0),
                Library = reader.GetString(1),
                Fidelity = Enum.Parse<TermFidelity>(reader.GetString(2)),
                AuditedAt = reader.GetFieldValue<DateTimeOffset>(3),
                TermAnswered = reader.IsDBNull(4) ? null : reader.GetString(4),

                // Null stays null and zero stays zero. The two are different facts, and this is
                // the read that the table's shape exists to keep apart.
                Count = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                UnobtainedReason = Enum.Parse<CountUnobtainedReason>(reader.GetString(6)),
                WhatWouldObtainIt = reader.IsDBNull(7) ? null : reader.GetString(7),

                // The table check admits a prior only whole, so one column present means all are.
                Prior = reader.IsDBNull(8)
                    ? null
                    : new PriorObservation
                    {
                        ReportedTotal = reader.GetInt32(8),
                        ObservedOn = reader.GetFieldValue<DateOnly>(9),
                        Source = reader.GetString(10),
                        FidelityAtObservation = Enum.Parse<TermFidelity>(reader.GetString(11)),
                    },
                RecordedUnavailable = reader.GetBoolean(12),
            });
        }

        return rows;
    }

    private static async Task<IReadOnlyList<DeterminationResolution>> DeterminationRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken)
    {
        await using var command = Command(
            """
            SELECT determination, outcome, resolved_at, evidence, not_evidenceable_reason,
                   what_would_make_it_evidenceable, policy_reference, policy_verified_on
            FROM dossier_determinations
            WHERE item_id = @item_id AND item_version = @item_version
            ORDER BY row_no
            """,
            connection, transaction, item, version);

        var rows = new List<DeterminationResolution>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(new DeterminationResolution
            {
                Determination = Enum.Parse<ComplianceDetermination>(reader.GetString(0)),
                Outcome = Enum.Parse<DeterminationOutcome>(reader.GetString(1)),
                ResolvedAt = reader.GetFieldValue<DateTimeOffset>(2),
                Evidence = reader.IsDBNull(3) ? null : reader.GetString(3),
                NotEvidenceableReason = reader.IsDBNull(4) ? null : reader.GetString(4),
                WhatWouldMakeItEvidenceable = reader.IsDBNull(5) ? null : reader.GetString(5),
                PolicyReference = reader.IsDBNull(6) ? null : reader.GetString(6),
                PolicyVerifiedOn = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateOnly>(7),
            });
        }

        return rows;
    }

    private static async Task<IReadOnlyList<DossierComponent>> ComponentsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken)
    {
        await using var command = Command(
            """
            SELECT kind, payload::text
            FROM dossier_components
            WHERE item_id = @item_id AND item_version = @item_version
            ORDER BY row_no
            """,
            connection, transaction, item, version);

        var rows = new List<DossierComponent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(DossierPayload.Deserialize(
                Enum.Parse<DossierComponentKind>(reader.GetString(0)),
                reader.GetString(1)));
        }

        return rows;
    }
}
