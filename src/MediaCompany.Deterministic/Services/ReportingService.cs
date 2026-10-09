using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Reporting;
using MediaCompany.Deterministic.Rights;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Rights;

namespace MediaCompany.Deterministic.Services;

/// <summary>
/// One line of the measurable-now report.
///
/// The figure is a <see cref="MeasurementQuantity"/> rather than a free-form string. The delivered
/// shape carried the value as a string, which meant the rendered figure carried no measurement
/// state at all and any string a caller composed reached the reader unclassified — both the way an
/// unobserved quantity could arrive looking like a number, and the only route by which record
/// content could reach a reader through this surface without being classified first.
/// </summary>
public sealed record ReportedMeasure(string Name, MeasurementQuantity Quantity, string Source, bool IsEstimate);

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
    private readonly IChannelPartitionReader _partitions;
    private readonly IClock _clock;

    /// <summary>
    /// The report reads ONE source, the channel partition of the operation record (the multi-channel
    /// change, decision D-003, as corrected in its review): the company lines and the channel lines are
    /// the datastore's aggregation of the same rows in the same read, so every additive company line is
    /// the exact sum of its channel lines by construction, in a month that is not final as in one that is.
    /// </summary>
    public ReportingService(IChannelPartitionReader partitions, IClock clock)
    {
        _partitions = partitions;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ReportedMeasure>> MeasurableNowAsync(DateOnly period, CancellationToken cancellationToken)
    {
        // ONE READ, under the record horizon's closure taken first: the company row and every channel
        // row come from it, and every line states whether the month can still change (decision D-006).
        // Every money figure is the datastore's own aggregation, the variance included; nothing here
        // performs money arithmetic, and no second read is taken that an operation booked in between
        // could make disagree with the first.
        var partition = ChannelAnalyticsComposers.Operations(
            await _partitions.OperationsAsync(period, cancellationToken).ConfigureAwait(false));
        var finality = partition.Finality.Statement;
        var company = partition.Company;

        // The measurement state of every company figure is decided at the one composing site: a period
        // holding no recorded operation is UNMEASURED and never a zero amount, and a period whose
        // records aggregate to zero is an observed zero.
        var lines = new List<ReportedMeasure>
        {
            new("monthly-cost-total", company.Cost,
                "recorded operations, exact decimal aggregation", company.ContainsEstimates),

            new("cost-variance-against-envelope", company.VarianceAgainstEnvelope,
                $"recorded operations against the approved envelope of {ApprovedEnvelope.MonthlyTotal} "
                    + $"(metered {ApprovedEnvelope.Metered}, standing {ApprovedEnvelope.Standing}), "
                    + "subtracted by the datastore",
                company.ContainsEstimates),

            new("deterministic-set-ai-cost", company.DeterministicSetCost,
                "recorded operations attributed to the named deterministic set", IsEstimate: false),
        };

        lines.AddRange(company.CostByCapability.Select(pair => new ReportedMeasure(
            $"cost-by-capability:{pair.Key}",
            pair.Value,
            "recorded operations, exact decimal aggregation",
            company.ContainsEstimates)));

        // The same figures for every channel the register holds or the rows name, each the
        // datastore's aggregation over that channel's rows in the same read as the company's, so the
        // channel lines of an additive figure sum exactly to the company line.
        foreach (var channel in partition.Channels)
        {
            var name = $"channel:{channel.Channel}";
            var standing = channel.Standing == ChannelPartitionStanding.InRegister
                ? string.Empty
                : " (a channel identifier the channel register does not hold)";

            lines.Add(new ReportedMeasure(
                $"monthly-cost-total:{name}", channel.Cost,
                $"recorded operations of the channel{standing}, exact decimal aggregation", channel.ContainsEstimates));
            lines.Add(new ReportedMeasure(
                $"deterministic-set-ai-cost:{name}", channel.DeterministicSetCost,
                $"recorded operations of the channel{standing} attributed to the named deterministic set", IsEstimate: false));
            lines.AddRange(channel.CostByCapability.Select(pair => new ReportedMeasure(
                $"cost-by-capability:{pair.Key}:{name}", pair.Value,
                $"recorded operations of the channel{standing}, exact decimal aggregation", channel.ContainsEstimates)));
        }

        // Every line says whether its month can still change.
        return lines.Select(line => line with { Source = $"{line.Source}; {finality}" }).ToArray();
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
