using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Configuration;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Rights;

namespace MediaCompany.Application.Ports;

/// <summary>The clock, so that elapsed minutes and validity intervals are testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// The route policy register (module M-002). Admission is the first of the two independent
/// refusals constraint C-001 requires: a route matching a forbidden-source entry cannot be
/// admitted to the table at all, so an admission-time check and a resolution-time check would
/// both have to fail before a forbidden path existed.
/// </summary>
public interface IRouteRegistry
{
    Task<IReadOnlyList<Route>> AdmittedRoutesAsync(CapabilityClass capability, CancellationToken cancellationToken);

    Task<IReadOnlyList<ForbiddenSource>> ForbiddenSourcesAsync(CancellationToken cancellationToken);
}

/// <summary>Route availability state (module M-004), an explicit recorded state per decision D-008.</summary>
public interface IRouteAvailabilityLedger
{
    Task<IReadOnlyDictionary<RouteId, RouteAvailability>> CurrentAsync(
        IReadOnlyCollection<RouteId> routes,
        CancellationToken cancellationToken);
}

/// <summary>The operating registers (module M-011).</summary>
public interface IOperatingRegisters
{
    Task<IReadOnlyList<ProviderAccount>> ProviderAccountsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Model>> ModelsAsync(CancellationToken cancellationToken);

    /// <summary>The price rows in force at <paramref name="asOf"/>, one per model and unit kind.</summary>
    Task<IReadOnlyList<ModelPrice>> PricesInForceAsync(DateTimeOffset asOf, CancellationToken cancellationToken);

    Task<IReadOnlyList<Channel>> ChannelsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Department>> DepartmentsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkforceAgent>> AgentsAsync(CancellationToken cancellationToken);
}

/// <summary>The asset and rights ledger (module M-015).</summary>
public interface IAssetLedger
{
    Task<IReadOnlyList<Asset>> ForItemAsync(ItemId item, CancellationToken cancellationToken);

    Task<IReadOnlyList<LibraryRegistration>> RegistrationsAsync(ChannelId channel, CancellationToken cancellationToken);

    Task RecordAsync(Asset asset, CancellationToken cancellationToken);
}

/// <summary>Blocks and approvals over the publication gate (module M-009).</summary>
public interface IGateLedger
{
    Task<IReadOnlyList<Block>> OpenBlocksAsync(ItemId item, CancellationToken cancellationToken);

    Task<IReadOnlyList<Approval>> ApprovalsAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken);

    Task<GateState> CurrentStateAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// The three conditions of first publication, as recorded observations for one channel.
    ///
    /// Returns a register that is TOTAL over the closed three-member set whatever the store holds,
    /// so a channel with no observations yields a register in which all three resolve absent and
    /// therefore refuse. There is no return value meaning "nothing to check".
    /// </summary>
    Task<FirstPublicationConditionRegister> FirstPublicationConditionsAsync(
        ChannelId channel,
        CancellationToken cancellationToken);
}

/// <summary>Temporal configuration (module M-012), read without redeployment (constraint C-011).</summary>
public interface IConfigurationStore
{
    Task<ConfigurationVersion?> InForceAsync(string key, string scope, DateTimeOffset asOf, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConfigurationVersion>> HistoryAsync(string key, string scope, CancellationToken cancellationToken);

    /// <summary>
    /// Supersedes the value in force with a new row rather than updating in place, so the prior
    /// value stays retrievable with its validity dates (acceptance criterion AC-021).
    /// </summary>
    Task SupersedeAsync(ConfigurationVersion next, CancellationToken cancellationToken);
}

/// <summary>Cost rollups (module M-006), aggregated by the datastore over the recorded operations.</summary>
public interface ICostRollupReader
{
    Task<Money> CostForItemAsync(ItemId item, CancellationToken cancellationToken);

    Task<Money> CostForPeriodAsync(DateOnly period, CancellationToken cancellationToken);

    /// <summary>
    /// The monthly total against the approved envelope, with the variance. Every figure is
    /// aggregated and subtracted by the datastore in its exact decimal type, so the reported
    /// variance and the recorded one are one number rather than two (constraint C-005).
    /// </summary>
    Task<PeriodSummary> PeriodSummaryAsync(DateOnly period, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<CapabilityClass, Money>> CostByCapabilityAsync(DateOnly period, CancellationToken cancellationToken);

    /// <summary>
    /// The total cost attributed to the named deterministic task set over a period. Acceptance
    /// criterion AC-017 measures this to zero.
    /// </summary>
    Task<Money> CostForDeterministicSetAsync(DateOnly period, CancellationToken cancellationToken);

    // -----------------------------------------------------------------------
    // The parallel three-state members (decision D-005).
    //
    // The delivered currency-returning members above keep their signatures and their behaviour, so
    // every existing caller compiles and behaves unchanged through the coexistence period. They are
    // removed only once the build shows no caller, which is a condition the build decides rather
    // than a judgement somebody makes.
    //
    // The defect the parallel members exist to correct is concrete: a currency value cannot express
    // the absence of an observation, so a period holding no recorded operation is returned today as
    // a zero amount and is indistinguishable from a period that was measured and cost nothing.
    // -----------------------------------------------------------------------

    /// <summary>One item's cost in three states. Unmeasured where no operation is recorded for it.</summary>
    Task<MeasurementQuantity> ItemCostQuantityAsync(ItemId item, CancellationToken cancellationToken);

    /// <summary>One period's cost in three states. Unmeasured where the period holds no recorded operation.</summary>
    Task<MeasurementQuantity> PeriodCostQuantityAsync(DateOnly period, CancellationToken cancellationToken);

    /// <summary>
    /// One item's cost position as the datastore computes it, mirroring the delivered period
    /// summary. <see cref="ItemSummary.Operations"/> is what decides the measurement state, exactly
    /// as the period summary's count already does.
    /// </summary>
    Task<ItemSummary> ItemSummaryAsync(ItemId item, CancellationToken cancellationToken);
}

/// <summary>
/// What remains of a governing budget, read before a route is admitted at resolution step 4. The
/// remaining amount is computed by the datastore from the recorded operations and the configured
/// budget, not here (constraint C-005).
/// </summary>
public interface IBudgetReader
{
    Task<Money> RemainingAsync(Attribution attribution, DateOnly period, CancellationToken cancellationToken);
}

/// <summary>
/// One period's cost position as the datastore computes it. <see cref="ContainsEstimates"/> is
/// true whenever any operation in the period was accounted from estimated rather than measured
/// unit counts, which every presentation of these figures must state.
/// </summary>
public sealed record PeriodSummary(
    DateOnly Period,
    Money Total,
    Money EnvelopeTotal,
    Money EnvelopeMetered,
    Money EnvelopeStanding,
    Money VarianceAgainstEnvelope,
    bool ContainsEstimates,
    long Operations);

/// <summary>
/// One item's cost position as the datastore computes it, on the same shape as
/// <see cref="PeriodSummary"/>. <see cref="Operations"/> is the count of recorded operations the
/// total aggregates, and a count of zero is what makes the figure unmeasured rather than zero.
/// </summary>
public sealed record ItemSummary(
    ItemId Item,
    Money Total,
    bool ContainsEstimates,
    long Operations);

/// <summary>
/// The served reasoning tier as recorded, one entry per accounted operation in a period.
///
/// The served value on each record was written at the resolution boundary from the ADMITTED
/// ROUTE's stated tier and never from the request, and a null served value is the explicit absence
/// marker for a route that stated none. This port reads those records; it derives nothing.
/// </summary>
public interface IServedTierReader
{
    Task<IReadOnlyList<ServedTierRecord>> RecordsForPeriodAsync(DateOnly period, CancellationToken cancellationToken);
}

/// <summary>
/// The register in which an observed revenue parameter is recorded with the source observation it
/// came from and that observation's date (decision D-003).
///
/// The register is created empty and this change records no row in it. A row recorded without a
/// source observation and its date is an assumed parameter: it is readable here and it admits
/// nothing, which is what keeps the six revenue-derived figures unlit.
/// </summary>
public interface IRevenueParameterRegister
{
    Task<IReadOnlyList<RevenueParameterRecord>> RecordedAsync(CancellationToken cancellationToken);

    Task RecordAsync(RevenueParameterRecord parameter, CancellationToken cancellationToken);
}
