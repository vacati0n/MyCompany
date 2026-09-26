using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Reporting;
using MediaCompany.Deterministic.Rights;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Rights;

namespace MediaCompany.Deterministic.Services;

/// <summary>One line of the measurable-now report.</summary>
public sealed record ReportedMeasure(string Name, string Value, string Unit, string Source, bool IsEstimate);

/// <summary>
/// The reporting surface (module M-016, plan task T-025).
///
/// Two rules shape it. First, the reported set carries no revenue-denominated measure, because
/// none is computable before a revenue parameter is observed. Second, a deferred measure is
/// rendered from <see cref="Measure.Deferred.Display"/>, which has no value field at all, so it
/// cannot be shown as zero, a dash, or anything a reader could mistake for a measurement
/// (acceptance criterion AC-026).
///
/// Every cost figure reported here is an ESTIMATE against unit prices that no first-hand
/// verification has confirmed, and the report says so per figure.
/// </summary>
public sealed class ReportingService
{
    private readonly ICostRollupReader _costs;
    private readonly IClock _clock;

    public ReportingService(ICostRollupReader costs, IClock clock)
    {
        _costs = costs;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ReportedMeasure>> MeasurableNowAsync(DateOnly period, CancellationToken cancellationToken)
    {
        var monthly = await _costs.CostForPeriodAsync(period, cancellationToken).ConfigureAwait(false);
        var byCapability = await _costs.CostByCapabilityAsync(period, cancellationToken).ConfigureAwait(false);
        var deterministic = await _costs.CostForDeterministicSetAsync(period, cancellationToken).ConfigureAwait(false);

        var lines = new List<ReportedMeasure>
        {
            new("monthly-cost-total", monthly.ToString(), "USD",
                "recorded operations, exact decimal aggregation", IsEstimate: true),

            new("cost-variance-against-envelope",
                $"{monthly.Amount - ApprovedEnvelope.MonthlyTotal.Amount} USD against an envelope of "
                    + $"{ApprovedEnvelope.MonthlyTotal.Amount} (metered {ApprovedEnvelope.Metered.Amount}, "
                    + $"standing {ApprovedEnvelope.Standing.Amount})",
                "USD",
                "recorded operations against the approved envelope",
                IsEstimate: true),

            new("deterministic-set-ai-cost", deterministic.ToString(), "USD",
                "recorded operations attributed to the named deterministic set", IsEstimate: false),
        };

        lines.AddRange(byCapability.Select(pair => new ReportedMeasure(
            $"cost-by-capability:{pair.Key}",
            pair.Value.ToString(),
            "USD",
            "recorded operations, exact decimal aggregation",
            IsEstimate: true)));

        return lines;
    }

    /// <summary>
    /// The deferred set, each naming the parameter it waits on. No value is rendered, because the
    /// type carries none.
    /// </summary>
    public IReadOnlyList<string> DeferredMeasures() =>
        MeasureCatalogue.Deferred.Select(d => $"{d.Name}: {d.Display}").ToArray();

    /// <summary>
    /// Whether the reported set contains a revenue-denominated measure. The answer is always
    /// false while every revenue parameter is unobserved, and the check is the inspection
    /// acceptance criterion AC-025 names.
    /// </summary>
    public static bool ReportedSetContainsRevenueMeasure(IReadOnlyList<ReportedMeasure> reported)
    {
        ArgumentNullException.ThrowIfNull(reported);
        var deferredNames = MeasureCatalogue.Deferred.Select(d => d.Name).ToHashSet(StringComparer.Ordinal);
        return reported.Any(r => deferredNames.Contains(r.Name));
    }

    public DateOnly CurrentPeriod()
    {
        var now = _clock.UtcNow;
        return new DateOnly(now.Year, now.Month, 1);
    }
}

/// <summary>
/// The operating registers the company produces from its own records (module M-011, plan task
/// T-021). No entry lacking a verification date is presented as current (acceptance criterion
/// AC-020).
/// </summary>
public sealed class OperatingRegisterReport
{
    private readonly IOperatingRegisters _registers;
    private readonly IClock _clock;

    public OperatingRegisterReport(IOperatingRegisters registers, IClock clock)
    {
        _registers = registers;
        _clock = clock;
    }

    public sealed record CapabilityEntry(
        string Model,
        string ProviderAccount,
        string UnitKind,
        decimal UnitPrice,
        string Currency,
        string Source,
        DateOnly? VerifiedOn)
    {
        /// <summary>An entry with no verification date is never presented as current.</summary>
        public bool PresentedAsCurrent => VerifiedOn is not null;
    }

    public sealed record Register(
        IReadOnlyList<Channel> Channels,
        IReadOnlyList<Department> Departments,
        IReadOnlyList<WorkforceAgent> Agents,
        IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissions,
        IReadOnlyList<CapabilityEntry> Capabilities);

    public async Task<Register> ProduceAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var channels = await _registers.ChannelsAsync(cancellationToken).ConfigureAwait(false);
        var departments = await _registers.DepartmentsAsync(cancellationToken).ConfigureAwait(false);
        var agents = await _registers.AgentsAsync(cancellationToken).ConfigureAwait(false);
        var models = await _registers.ModelsAsync(cancellationToken).ConfigureAwait(false);
        var prices = await _registers.PricesInForceAsync(now, cancellationToken).ConfigureAwait(false);

        var byModel = models.ToDictionary(m => m.Id);

        var capabilities = prices
            .Select(p => new CapabilityEntry(
                p.Model.Value,
                byModel.TryGetValue(p.Model, out var model) ? model.ProviderAccount.Value : "(unknown)",
                p.UnitKind.ToString(),
                p.UnitPrice,
                p.Currency,
                p.Source,
                p.VerifiedOn))
            .Where(e => e.PresentedAsCurrent)
            .ToArray();

        var rolePermissions = MediaCompany.Domain.Authority.ActionSet.Roles.ToDictionary(
            role => role.ToString(),
            IReadOnlyList<string> (role) => MediaCompany.Domain.Authority.ActionSet.For(role)
                .Select(a => a.ToString())
                .OrderBy(a => a, StringComparer.Ordinal)
                .ToArray());

        return new Register(channels, departments, agents, rolePermissions, capabilities);
    }
}

/// <summary>
/// The permission answer for a finished item (module M-015 over M-007, plan task T-011).
///
/// The answer is assembled from the record alone: the basis is on the asset row, the registration
/// proof is the library join, and the verifier and date are on the same row. No source outside
/// the record is consulted (acceptance criterion AC-008).
/// </summary>
public sealed class PermissionAnswerService
{
    private readonly IAssetLedger _assets;
    private readonly IClock _clock;

    public PermissionAnswerService(IAssetLedger assets, IClock clock)
    {
        _assets = assets;
        _clock = clock;
    }

    public sealed record AssetAnswer(AssetId Asset, bool Permitted, string Basis, string VerifiedBy, string VerifiedOn);

    public async Task<IReadOnlyList<AssetAnswer>> AnswerAsync(
        ItemId item,
        ChannelId channel,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var assets = await _assets.ForItemAsync(item, cancellationToken).ConfigureAwait(false);
        var registrations = await _assets.RegistrationsAsync(channel, cancellationToken).ConfigureAwait(false);

        return assets
            .Select(asset => PermissionBasis.Resolve(asset, registrations, today))
            .Select(verdict => verdict switch
            {
                PermissionVerdict.Permitted p =>
                    new AssetAnswer(p.Asset, true, p.Basis, p.VerifiedByRole, p.VerifiedOn.ToString("yyyy-MM-dd")),
                PermissionVerdict.NotPermitted n =>
                    new AssetAnswer(n.Asset, false, $"not permitted: {string.Join("; ", n.MissingOrFailing)}", "none", "none"),
                _ => throw new InvalidOperationException("Unreachable: the verdict union has two members."),
            })
            .ToArray();
    }
}
