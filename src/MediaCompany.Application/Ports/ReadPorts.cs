using MediaCompany.Domain.Accounting;
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
