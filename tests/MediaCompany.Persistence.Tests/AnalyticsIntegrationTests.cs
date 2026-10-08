using MediaCompany.Application.Ports;
using MediaCompany.Capability;
using MediaCompany.Credentials;
using MediaCompany.Deterministic;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Rights;
using MediaCompany.Domain.Work;
using Npgsql;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The analytics surface against the record store, and the served-tier evidence obtained through
/// the delivered resolution boundary.
///
/// Each demonstration drops and recreates the schema, so they share the datastore collection and
/// run sequentially.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class AnalyticsIntegrationTests : IAsyncLifetime
{
    private NpgsqlDataSource? _dataSource;

    private NpgsqlDataSource Source => _dataSource
        ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
    private static readonly DateOnly Period = new(2026, 10, 1);

    private static readonly CompanyId Company = CompanyId.New();
    private static readonly ChannelId Channel = ChannelId.New();
    private static readonly DepartmentId Department = DepartmentId.New();
    private static readonly AgentId Agent = AgentId.New();
    private static readonly ItemId Item = ItemId.New();
    private static readonly RouteId SubstituteRoute = RouteId.New();

    private readonly TestClock _clock = new(Now);

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
    // The concrete defect, corrected against the datastore
    // -----------------------------------------------------------------------

    /// <summary>
    /// A period holding no recorded operation reads as UNMEASURED through the parallel member and
    /// as a ZERO AMOUNT through the delivered one.
    ///
    /// Both are asserted here deliberately: the delivered member keeps its behaviour through the
    /// coexistence period, and the two readings side by side are the defect and its correction in
    /// one demonstration.
    /// </summary>
    [RequiresPostgresFact]
    public async Task APeriodWithNoRecordedOperationReadsUnmeasuredAndTheDeliveredMemberStillReadsZero()
    {
        var reader = new NpgsqlCostReader(Source);

        var delivered = await reader.CostForPeriodAsync(Period, CancellationToken.None);
        Assert.Equal(0m, delivered.Amount);

        var quantity = await reader.PeriodCostQuantityAsync(Period, CancellationToken.None);
        var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(quantity);
        Assert.Equal(UnmeasuredReason.NoObservationExists, unmeasured.Reason);

        var itemQuantity = await reader.ItemCostQuantityAsync(Item, CancellationToken.None);
        Assert.IsType<MeasurementQuantity.Unmeasured>(itemQuantity);
    }

    /// <summary>
    /// With a recorded operation the figure is observed, and it states the number of records it
    /// aggregates. The aggregation is the datastore's; nothing here performs money arithmetic.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ARecordedOperationMakesTheCostFigureObservedAndCounted()
    {
        await RecordOperationAsync(inputUnits: 1_000, outputUnits: 500);

        var reader = new NpgsqlCostReader(Source);
        var summary = await reader.ItemSummaryAsync(Item, CancellationToken.None);
        var model = AnalyticsComposers.Cost(summary);

        Assert.Equal(1, summary.Operations);
        Assert.IsType<MeasurementQuantity.ObservedValue>(model.Cost);
        Assert.Equal("1 operations", model.Operations.Describe());

        var period = AnalyticsComposers.Cost(
            await reader.PeriodSummaryAsync(Period, CancellationToken.None));
        Assert.IsType<MeasurementQuantity.ObservedValue>(period.Cost);
    }

    // -----------------------------------------------------------------------
    // The served reasoning tier, recorded by the delivered boundary executing
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE SERVED-TIER EVIDENCE. One operation record, written to the record store by the
    /// DELIVERED resolution boundary executing, carrying the reasoning tier the request asked for
    /// together with the tier the ADMITTED ROUTE stated it served.
    ///
    /// Nothing is spent to obtain it. The admitted route is a non-AI substitute, so no provider is
    /// reached, no credential is issued, no account exists and the recorded operation carries zero
    /// units and no applied price — the datastore computes its cost as zero. The served value is
    /// nonetheless read from <c>Route.StatedReasoningTier</c> at the boundary, which is the same
    /// field the provider path writes from; the evidence is a property of the resolution boundary
    /// rather than of a provider's response, so it is obtainable here at no cost and with no
    /// account at all.
    ///
    /// The requested and served tiers differ on purpose. A record in which they agree cannot
    /// distinguish a served value read from the route from one copied out of the request.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheBoundaryRecordsBothTiersAgainstTheRecordStoreWithoutReachingAProvider()
    {
        var gateway = BuildGateway(statedTier: ReasoningTier.Light);

        var outcome = await gateway.ExecuteAsync(Request(ReasoningTier.Deep), Context(), CancellationToken.None);

        var substituted = Assert.IsType<CapabilityOutcome.Substituted>(outcome);
        Assert.Equal(ReasoningTier.Deep, substituted.Operation.ReasoningTierRequested);
        Assert.Equal(ReasoningTier.Light, substituted.Operation.ReasoningTierServed);
        Assert.True(substituted.Operation.CarriesTierEvidence);

        // No spend: zero units, no applied price, and a cost the datastore computed as zero.
        Assert.Equal(UnitCounts.None, substituted.Operation.Units);
        Assert.Null(substituted.Operation.AppliedPrice);
        Assert.Equal(0m, substituted.Operation.ComputedCost.Amount);

        // And it is in the store, read back through the read port rather than from the return value.
        var records = await new NpgsqlCostReader(Source)
            .RecordsForPeriodAsync(Period, CancellationToken.None);

        var stored = Assert.Single(records);
        Assert.Equal(substituted.Operation.Id, stored.Operation);
        Assert.Equal(ReasoningTier.Deep, stored.Requested);
        Assert.Equal(ReasoningTier.Light, stored.Served);
        Assert.True(stored.CarriesTierEvidence);

        // The output reporting it carries the single-record caveat, which no option suppresses.
        var reported = Assert.Single(AnalyticsComposers.ServedTiers(records));
        Assert.Contains("does not settle", reported.Caveat.Statement, StringComparison.Ordinal);
        Assert.Equal("1 served-tier records", reported.Caveat.ServedTierRecords.Describe());

        // One record settles no ratio.
        var ratio = AnalyticsComposers.TierRatio(Period, records);
        Assert.IsType<MeasurementQuantity.Unmeasured>(ratio.Ratio);
    }

    /// <summary>
    /// The absence branch, against the record store. A route that states no tier yields the
    /// EXPLICIT ABSENCE MARKER: the stored served value is null, no tier value stands in its
    /// place, and the requested tier is not substituted for it. The run is a completed outcome.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ARouteStatingNoTierRecordsTheAbsenceMarkerAgainstTheRecordStore()
    {
        var gateway = BuildGateway(statedTier: null);

        var outcome = await gateway.ExecuteAsync(Request(ReasoningTier.Deep), Context(), CancellationToken.None);

        var substituted = Assert.IsType<CapabilityOutcome.Substituted>(outcome);
        Assert.Equal(ReasoningTier.Deep, substituted.Operation.ReasoningTierRequested);
        Assert.Null(substituted.Operation.ReasoningTierServed);
        Assert.False(substituted.Operation.CarriesTierEvidence);

        var stored = Assert.Single(
            await new NpgsqlCostReader(Source).RecordsForPeriodAsync(Period, CancellationToken.None));

        Assert.Equal(ReasoningTier.Deep, stored.Requested);
        Assert.Null(stored.Served);

        var reported = Assert.Single(AnalyticsComposers.ServedTiers([stored]));
        Assert.Contains("absent", reported.ServedStatement, StringComparison.Ordinal);
        Assert.DoesNotContain("Deep", reported.ServedStatement, StringComparison.Ordinal);
    }

    /// <summary>
    /// What the DELIVERED route adapter can carry, read back from the same row the boundary used.
    ///
    /// The routes table holds no column for a stated reasoning tier, and the delivered registry
    /// constructs every route without one. Every route read through the composed production path
    /// therefore states no tier, whatever it would state if it could — so on that path the served
    /// value is the absence marker for every operation. This demonstration records the position
    /// rather than changing it: the column and the adapter change are not in the accepted design,
    /// and the finding is raised rather than taken.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheDeliveredRouteRegistryStatesNoReasoningTierForAnyAdmittedRoute()
    {
        var routes = await new NpgsqlRouteRegistry(Source)
            .AdmittedRoutesAsync(CapabilityClass.EditorialReasoning, CancellationToken.None);

        var route = Assert.Single(routes);
        Assert.Equal(SubstituteRoute, route.Id);
        Assert.Null(route.StatedReasoningTier);
    }

    // -----------------------------------------------------------------------
    // The six revenue-derived figures against the register (plan task T-007)
    // -----------------------------------------------------------------------

    /// <summary>
    /// The register is created EMPTY by the appended schema resource, and this change records no
    /// row in it. With nothing recorded, no revenue-derived figure is visible in any view.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheRevenueRegisterIsCreatedEmptyAndNoFigureIsVisible()
    {
        var register = new NpgsqlRevenueParameterRegister(Source);

        Assert.Empty(await register.RecordedAsync(CancellationToken.None));
        Assert.Empty(AnalyticsComposers.Visible(await register.RecordedAsync(CancellationToken.None)));
    }

    /// <summary>
    /// A parameter recorded WITHOUT its source observation lights none of the six. It is readable
    /// as recorded, and it admits nothing.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AParameterRecordedWithoutItsSourceLightsNoneOfTheSix()
    {
        var register = new NpgsqlRevenueParameterRegister(Source);

        await register.RecordAsync(
            new RevenueParameterRecord { Observation = MeasurementQuantity.Observed(2.50m, "USD") },
            CancellationToken.None);

        var recorded = await register.RecordedAsync(CancellationToken.None);

        Assert.Single(recorded);
        Assert.Null(recorded[0].SourceObservation);
        Assert.Empty(AnalyticsComposers.Visible(recorded));
    }

    /// <summary>
    /// A parameter recorded WITH its source observation and that observation's date admits, the
    /// six become visible, and each shows the recorded source it was lit by.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AParameterRecordedWithItsSourceLightsTheSixAndShowsThatSource()
    {
        var register = new NpgsqlRevenueParameterRegister(Source);

        await register.RecordAsync(
            new RevenueParameterRecord
            {
                Observation = MeasurementQuantity.Observed(2.50m, "USD"),
                SourceObservation = "a platform revenue statement",
                ObservedOn = new DateOnly(2026, 10, 1),
            },
            CancellationToken.None);

        var visible = AnalyticsComposers.Visible(await register.RecordedAsync(CancellationToken.None));

        Assert.Equal(6, visible.Count);
        Assert.All(visible, f => Assert.Equal("a platform revenue statement", f.RecordedSource));
        // The amount comes back in the datastore's own exact decimal scale, which is the point:
        // the figure is the recorded one rather than a rounded restatement of it.
        var revenue = Assert.IsType<MeasurementQuantity.ObservedValue>(
            visible.Single(f => f.Figure == RevenueDerivedFigure.Revenue).Quantity);

        Assert.Equal(2.50m, revenue.Amount);
        Assert.Equal("USD", revenue.Unit);
    }

    /// <summary>
    /// THE ROUND TRIP. A measurement quantity written to the register and read back resolves to
    /// the case it was written in, for all three cases.
    ///
    /// The observed zero is the one this exists for. The first shape of the adapter bound the
    /// amount column from the observed-value case alone, so an observed zero went in as a null
    /// amount and came back as unmeasured: a quantity the company measured and found to be zero
    /// became a quantity it had never measured, across the only delivered path that persists a
    /// measurement quantity. No check crossed the write and the read together, which is why the
    /// defect survived a suite that asserted the distinction everywhere else.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AMeasurementWrittenToTheRegisterReadsBackInTheCaseItWasWrittenIn()
    {
        var register = new NpgsqlRevenueParameterRegister(Source);

        await register.RecordAsync(
            new RevenueParameterRecord { Observation = MeasurementQuantity.Observed(2.50m, "USD") },
            CancellationToken.None);

        await register.RecordAsync(
            new RevenueParameterRecord { Observation = MeasurementQuantity.Zero("USD") },
            CancellationToken.None);

        await register.RecordAsync(
            new RevenueParameterRecord
            {
                Observation = MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.SourceCannotStateOne,
                    "the platform statement does not break revenue out by channel"),
            },
            CancellationToken.None);

        var recorded = await register.RecordedAsync(CancellationToken.None);

        Assert.Equal(3, recorded.Count);

        var value = Assert.IsType<MeasurementQuantity.ObservedValue>(recorded[0].Observation);
        Assert.Equal(2.50m, value.Amount);
        Assert.Equal("USD", value.Unit);

        // The one the defect collapsed: it comes back as an observed zero, not as unmeasured.
        var zero = Assert.IsType<MeasurementQuantity.ObservedZero>(recorded[1].Observation);
        Assert.Equal("USD", zero.Unit);

        // And the two still render differently from one another after the round trip.
        Assert.NotEqual(recorded[1].Observation.Describe(), recorded[2].Observation.Describe());

        // The unmeasured case keeps its reason and its stated detail, so the absence stays
        // diagnosable rather than becoming a generic one on the way through the store.
        var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(recorded[2].Observation);
        Assert.Equal(UnmeasuredReason.SourceCannotStateOne, unmeasured.Reason);
        Assert.Equal("the platform statement does not break revenue out by channel", unmeasured.Detail);
    }

    /// <summary>
    /// A revenue parameter observed to be zero, recorded with its source and its date, still
    /// lights none of the six.
    ///
    /// It is a real observation and it is reported as one; it simply cannot serve as the divisor
    /// the six are derived through. The admitting view excludes it in the datastore and the
    /// admission rule excludes it in the domain, so the two agree.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AParameterObservedToBeZeroIsRecordedAsZeroAndLightsNoneOfTheSix()
    {
        var register = new NpgsqlRevenueParameterRegister(Source);

        await register.RecordAsync(
            new RevenueParameterRecord
            {
                Observation = MeasurementQuantity.Zero("USD"),
                SourceObservation = "a platform revenue statement reporting no revenue",
                ObservedOn = new DateOnly(2026, 10, 1),
            },
            CancellationToken.None);

        var recorded = await register.RecordedAsync(CancellationToken.None);

        Assert.IsType<MeasurementQuantity.ObservedZero>(Assert.Single(recorded).Observation);
        Assert.Empty(AnalyticsComposers.Visible(recorded));

        // The datastore's own admitting view agrees with the domain's admission rule.
        await using var command = Source.CreateCommand("SELECT COUNT(*) FROM v_observed_revenue_parameters");
        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
    }

    /// <summary>
    /// The row shape is the case, and the table admits no other shape. A row carrying neither an
    /// amount nor an unmeasured reason is refused, so a writer cannot create a row whose case a
    /// reader would have to guess at.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheRegisterRefusesARowThatIsInNoMeasurementCase()
    {
        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO observed_revenue_parameters (parameter_id, recorded_at, unit)
            VALUES (gen_random_uuid(), now(), 'USD')
            """));

        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO observed_revenue_parameters
                (parameter_id, recorded_at, amount, unit, unmeasured_reason, unmeasured_detail)
            VALUES (gen_random_uuid(), now(), 2.50, 'USD', 'NoObservationExists', 'both shapes at once')
            """));
    }

    /// <summary>The register refuses a source recorded without the date of the observation.</summary>
    [RequiresPostgresFact]
    public async Task TheRegisterRefusesASourceRecordedWithoutItsObservationDate()
    {
        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO observed_revenue_parameters (parameter_id, recorded_at, amount, unit, source_observation)
            VALUES (gen_random_uuid(), now(), 2.50, 'USD', 'a statement with no date')
            """));
    }

    // -----------------------------------------------------------------------
    // Harness
    // -----------------------------------------------------------------------

    private ICapabilityGateway BuildGateway(ReasoningTier? statedTier)
    {
        var route = new Route(
            SubstituteRoute,
            CapabilityClass.EditorialReasoning,
            RouteTier.Primary,
            new RouteTarget.NonAiSubstitute(DeterministicTaskRegistry.MetadataTemplatePopulation),
            new QualityRating(100),
            new ContextCapacity(int.MaxValue),
            "non-AI substitute; no provider account and no commercial terms",
            new DateOnly(2026, 9, 20),
            statedTier);

        var broker = CredentialBrokerFactory.Create(new SecretStoreOptions(), () => Now);

        // No endpoint is supplied, so no provider adapter is built and none could be reached.
        return CapabilityGatewayFactory.Create(
            new RouteRegistryStatingATier(route),
            new AvailabilityAlwaysServing(),
            new EmptyRegisters(),
            new UnconstrainedBudget(),
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

    /// <summary>
    /// Supplies the admitted route to the boundary. It is a test double for the route register
    /// port and for that port alone: the boundary, the resolver, the operation recorder, the audit
    /// appender and the record store are the delivered ones, so the record this produces is
    /// written by the boundary executing rather than composed here.
    /// </summary>
    private sealed class RouteRegistryStatingATier(Route route) : IRouteRegistry
    {
        public Task<IReadOnlyList<Route>> AdmittedRoutesAsync(CapabilityClass capability, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Route>>(route.Capability == capability ? [route] : []);

        public Task<IReadOnlyList<ForbiddenSource>> ForbiddenSourcesAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ForbiddenSource>>([]);
    }

    private sealed class AvailabilityAlwaysServing : IRouteAvailabilityLedger
    {
        public Task<IReadOnlyDictionary<RouteId, RouteAvailability>> CurrentAsync(
            IReadOnlyCollection<RouteId> routes, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<RouteId, RouteAvailability>>(
                routes.ToDictionary(r => r, r => RouteAvailability.Serving(r, Now)));
    }

    /// <summary>No provider account, no model and no price: there is nothing to reach.</summary>
    private sealed class EmptyRegisters : IOperatingRegisters
    {
        public Task<IReadOnlyList<ProviderAccount>> ProviderAccountsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ProviderAccount>>([]);

        public Task<IReadOnlyList<Model>> ModelsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Model>>([]);

        public Task<IReadOnlyList<ModelPrice>> PricesInForceAsync(DateTimeOffset asOf, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ModelPrice>>([]);

        public Task<IReadOnlyList<Channel>> ChannelsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Channel>>([]);

        public Task<IReadOnlyList<Department>> DepartmentsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Department>>([]);

        public Task<IReadOnlyList<WorkforceAgent>> AgentsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<WorkforceAgent>>([]);
    }

    private sealed class UnconstrainedBudget : IBudgetReader
    {
        public Task<Money> RemainingAsync(Attribution attribution, DateOnly period, CancellationToken ct) =>
            Task.FromResult(new Money(1_000m));
    }

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private async Task RecordOperationAsync(long inputUnits, long outputUnits)
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);

        await using var transaction = await unitOfWork.BeginAsync(CancellationToken.None);

        await transaction.Operations.RecordAsync(
            new OperationDraft
            {
                Id = OperationId.New(),
                Run = RunId.New(),
                Attribution = new Attribution(Item, Channel, Department, Agent),
                Capability = CapabilityClass.EditorialReasoning,
                Model = new ModelId("alpha-reasoning"),
                Units = new UnitCounts(inputUnits, outputUnits, 0, 0),
                CostBasis = CostBasis.Measurement,
                Duration = TimeSpan.FromMilliseconds(250),
                Outcome = OperationOutcome.Succeeded,
                OccurredAt = Now,
                Attempt = 1,
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
                VALUES (@item, @channel, 1, 'first item');
            INSERT INTO provider_accounts (provider_account_id, provider, commercial_terms_basis, verified_on, status)
                VALUES ('provider-alpha', 'alpha', 'paid tier', '2026-09-01', 'Active');
            INSERT INTO models (model_id, provider_account_id, rated_quality, context_capacity, modality)
                VALUES ('alpha-reasoning', 'provider-alpha', 90, 200000, 'text');
            INSERT INTO model_prices (model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from, valid_to)
                VALUES (gen_random_uuid(), 'alpha-reasoning', 'InputUnit',  0.000003, 'USD', 'ESTIMATE, not verified first-hand', '2026-09-01', '2026-09-01T00:00:00Z', NULL),
                       (gen_random_uuid(), 'alpha-reasoning', 'OutputUnit', 0.000015, 'USD', 'ESTIMATE, not verified first-hand', '2026-09-01', '2026-09-01T00:00:00Z', NULL);

            -- The admitted route the boundary resolves to. It is a non-AI substitute: no provider
            -- account, no model, and therefore no spend. The row exists because the recorded
            -- operation references it.
            INSERT INTO routes (route_id, capability_class, tier, target_kind, substitute_task,
                                rated_quality, context_capacity, terms_basis, terms_verified_on)
                VALUES (@route, 'EditorialReasoning', 'Primary', 'NonAiSubstitute', @task,
                        100, 2000000, 'non-AI substitute; no provider account and no commercial terms', '2026-09-20');
            """,
            c =>
            {
                c.Parameters.AddWithValue("company", Company.Value);
                c.Parameters.AddWithValue("channel", Channel.Value);
                c.Parameters.AddWithValue("department", Department.Value);
                c.Parameters.AddWithValue("agent", Agent.Value);
                c.Parameters.AddWithValue("item", Item.Value);
                c.Parameters.AddWithValue("route", SubstituteRoute.Value);
                c.Parameters.AddWithValue("task", DeterministicTaskRegistry.MetadataTemplatePopulation);
            });
    }

    private async Task ExecuteAsync(string sql, Action<NpgsqlCommand>? configure = null)
    {
        await using var command = Source.CreateCommand(sql);
        configure?.Invoke(command);
        await command.ExecuteNonQueryAsync();
    }
}
