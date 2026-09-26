using MediaCompany.Application.Ports;
using MediaCompany.Capability.Providers;
using MediaCompany.Credentials;
using MediaCompany.Deterministic;
using MediaCompany.Deterministic.Resilience;
using MediaCompany.Deterministic.Routing;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;

namespace MediaCompany.Capability;

/// <summary>What the boundary did with one request.</summary>
public abstract record CapabilityOutcome
{
    private CapabilityOutcome()
    {
    }

    public sealed record Completed(OperationRecord Operation, IReadOnlyList<BudgetAlert> Alerts) : CapabilityOutcome;

    public sealed record Held(RefusalReason Reason, QualityRating FloorRequired, DateTimeOffset EscalatesAt, bool EscalatedToOwner)
        : CapabilityOutcome;

    public sealed record Refused(RefusalReason Reason, string Detail) : CapabilityOutcome;

    /// <summary>A non-AI substitute was selected; the named deterministic task carries the work at zero AI cost.</summary>
    public sealed record Substituted(string DeterministicTaskName, OperationRecord Operation) : CapabilityOutcome;
}

/// <summary>
/// The single capability-resolution boundary (module M-001, decision D-001).
///
/// This interface is the ONLY public way any component reaches an external capability. The
/// provider adapter set is internal to this assembly, so a consumer holds no dependency on an
/// adapter and cannot construct one; the alternative path was removed rather than guarded.
///
/// The boundary also owns the floor, the refusal, the credential acquisition, the metering and
/// the record, and it writes the operation record and the audit entry in the same transaction as
/// the work it performed, so accounting cannot be bypassed or double-counted (constraint C-004,
/// decision D-006).
/// </summary>
public interface ICapabilityGateway
{
    Task<CapabilityOutcome> ExecuteAsync(
        CapabilityRequest request,
        CapabilityInvocationContext context,
        CancellationToken cancellationToken);
}

/// <summary>Who is asking, and on behalf of which unit of work.</summary>
public sealed record CapabilityInvocationContext
{
    public required RunId Run { get; init; }
    public required JobId Job { get; init; }
    public required LifecyclePosition Stage { get; init; }
    public required WorkforceRole Actor { get; init; }
    public required int Attempt { get; init; }
}

/// <summary>The realization of the boundary.</summary>
public sealed class CapabilityGateway : ICapabilityGateway
{
    private readonly IRouteRegistry _routes;
    private readonly IRouteAvailabilityLedger _availability;
    private readonly IOperatingRegisters _registers;
    private readonly IBudgetReader _budgets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICredentialBroker _broker;
    private readonly IClock _clock;
    private readonly IReadOnlyDictionary<ProviderAccountId, IProviderAdapter> _adapters;

    internal CapabilityGateway(
        IRouteRegistry routes,
        IRouteAvailabilityLedger availability,
        IOperatingRegisters registers,
        IBudgetReader budgets,
        IUnitOfWork unitOfWork,
        ICredentialBroker broker,
        IClock clock,
        IReadOnlyDictionary<ProviderAccountId, IProviderAdapter> adapters)
    {
        _routes = routes;
        _availability = availability;
        _registers = registers;
        _budgets = budgets;
        _unitOfWork = unitOfWork;
        _broker = broker;
        _clock = clock;
        _adapters = adapters;
    }

    public async Task<CapabilityOutcome> ExecuteAsync(
        CapabilityRequest request,
        CapabilityInvocationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var now = _clock.UtcNow;
        var period = new DateOnly(now.Year, now.Month, 1);

        var admitted = await _routes.AdmittedRoutesAsync(request.Capability, cancellationToken).ConfigureAwait(false);
        var forbidden = await _routes.ForbiddenSourcesAsync(cancellationToken).ConfigureAwait(false);
        var availability = await _availability
            .CurrentAsync(admitted.Select(r => r.Id).ToArray(), cancellationToken).ConfigureAwait(false);
        var accounts = await _registers.ProviderAccountsAsync(cancellationToken).ConfigureAwait(false);
        var prices = await _registers.PricesInForceAsync(now, cancellationToken).ConfigureAwait(false);
        var remaining = await _budgets.RemainingAsync(request.Attribution, period, cancellationToken).ConfigureAwait(false);

        var inputs = new ResolutionInputs
        {
            AdmittedRoutes = admitted,
            ForbiddenSources = forbidden,
            Availability = availability,
            AccountStatus = accounts.ToDictionary(a => a.Id, a => a.Status),
            Prices = BuildPriceTable(prices),
            BudgetRemaining = remaining,
            Now = now,
        };

        var resolution = RouteResolver.Resolve(request, inputs);

        return resolution switch
        {
            CapabilityResolution.Refused refused =>
                await RecordNonProviderOutcomeAsync(
                    request, context, now, period, OperationOutcome.Refused,
                    $"{refused.Reason}: {refused.Detail}", null, null,
                    _ => new CapabilityOutcome.Refused(refused.Reason, refused.Detail),
                    cancellationToken).ConfigureAwait(false),

            CapabilityResolution.Held held =>
                await RecordHeldAsync(request, context, now, period, held, cancellationToken).ConfigureAwait(false),

            CapabilityResolution.Resolved resolved =>
                await ExecuteResolvedAsync(request, context, now, period, resolved, cancellationToken).ConfigureAwait(false),

            _ => throw new InvalidOperationException("Unreachable: the resolution union has three members."),
        };
    }

    private async Task<CapabilityOutcome> ExecuteResolvedAsync(
        CapabilityRequest request,
        CapabilityInvocationContext context,
        DateTimeOffset now,
        DateOnly period,
        CapabilityResolution.Resolved resolved,
        CancellationToken cancellationToken)
    {
        switch (resolved.Route.Target)
        {
            case RouteTarget.HoldAndEscalate hold:
                // The recorded emergency position for high-stakes review. No provider is reached,
                // so the operation carries zero AI cost and is recorded as held.
                return await RecordNonProviderOutcomeAsync(
                    request, context, now, period, OperationOutcome.Held,
                    hold.Reason, resolved.Route.Id, null,
                    _ => new CapabilityOutcome.Held(
                        RefusalReason.NoAvailableRoute,
                        resolved.EffectiveFloorApplied,
                        now + request.HoldTimeout,
                        EscalatedToOwner: true),
                    cancellationToken).ConfigureAwait(false);

            case RouteTarget.NonAiSubstitute substitute:
                // Routes to a member of the deterministic task set, which holds no dependency on
                // this assembly and therefore carries zero AI cost by construction.
                return await RecordNonProviderOutcomeAsync(
                    request, context, now, period, OperationOutcome.Succeeded,
                    $"non-AI substitute: {substitute.DeterministicTaskName}", resolved.Route.Id,
                    substitute.DeterministicTaskName,
                    op => new CapabilityOutcome.Substituted(substitute.DeterministicTaskName, op),
                    cancellationToken).ConfigureAwait(false);

            case RouteTarget.ProviderRoute provider:
                return await ExecuteProviderAsync(request, context, now, period, resolved, provider, cancellationToken)
                    .ConfigureAwait(false);

            default:
                throw new InvalidOperationException("Unreachable: the route target union has three members.");
        }
    }

    private async Task<CapabilityOutcome> ExecuteProviderAsync(
        CapabilityRequest request,
        CapabilityInvocationContext context,
        DateTimeOffset now,
        DateOnly period,
        CapabilityResolution.Resolved resolved,
        RouteTarget.ProviderRoute provider,
        CancellationToken cancellationToken)
    {
        if (!_adapters.TryGetValue(provider.ProviderAccount, out var adapter))
        {
            return await RecordNonProviderOutcomeAsync(
                request, context, now, period, OperationOutcome.Failed,
                $"no adapter is registered for provider account {provider.ProviderAccount}",
                resolved.Route.Id, null,
                _ => new CapabilityOutcome.Refused(
                    RefusalReason.NoAvailableRoute,
                    $"No adapter is registered for provider account {provider.ProviderAccount}."),
                cancellationToken).ConfigureAwait(false);
        }

        var issuance = await _broker.IssueAsync(
            new CredentialRequest
            {
                CredentialClass = CredentialClass.ProviderAccess,
                ProviderAccount = provider.ProviderAccount,
                Job = context.Job,
                Stage = context.Stage,
                Actor = context.Actor,
                Channel = request.Attribution.Channel,
                Capability = request.Capability,
            },
            cancellationToken).ConfigureAwait(false);

        if (issuance is CredentialOutcome.Refused credentialRefused)
        {
            return await RecordNonProviderOutcomeAsync(
                request, context, now, period, OperationOutcome.Refused,
                $"credential refused: {credentialRefused.Reason} — {credentialRefused.Detail}",
                resolved.Route.Id, null,
                _ => new CapabilityOutcome.Refused(RefusalReason.NoAvailableRoute, credentialRefused.Detail),
                cancellationToken).ConfigureAwait(false);
        }

        var handle = ((CredentialOutcome.Issued)issuance).Handle;
        var attempt = await adapter.InvokeAsync(provider, request, handle, cancellationToken).ConfigureAwait(false);

        // Whatever the provider did, the attempt is accounted. A failed attempt and a retried
        // attempt each get their own record, which is how exactly-once stays well defined under
        // retry (decision D-011).
        if (!attempt.Succeeded && attempt.Signal is { } signal)
        {
            var state = FailureHandling.StateFor(MapSignal(signal));
            if (state is { } availabilityState)
            {
                await _availability.RecordAsync(
                    new RouteAvailability(
                        resolved.Route.Id,
                        availabilityState,
                        now,
                        attempt.FailureReason ?? signal.ToString(),
                        null,
                        null),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        var appliedPrice = await ResolveAppliedPriceAsync(provider.Model, now, cancellationToken).ConfigureAwait(false);

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);

        var operation = await transaction.Operations.RecordAsync(
            new OperationDraft
            {
                Id = OperationId.New(),
                Run = context.Run,
                Attribution = request.Attribution,
                Capability = request.Capability,
                Route = resolved.Route.Id,
                Model = provider.Model,
                DeterministicTaskName = null,
                Units = attempt.Units,
                AppliedPrice = appliedPrice,
                CostBasis = attempt.CostBasis,
                Duration = attempt.Duration,
                Outcome = attempt.Succeeded ? OperationOutcome.Succeeded : OperationOutcome.Failed,
                OccurredAt = now,
                Attempt = context.Attempt,
                FailureReason = attempt.FailureReason,
            },
            cancellationToken).ConfigureAwait(false);

        await transaction.Audit.AppendAsync(
            new AuditEntryDraft
            {
                Actor = context.Actor.ToString(),
                Action = "capability.resolve-and-invoke",
                Subject = $"item:{request.Attribution.Item}",
                Reason = attempt.FailureReason ?? "resolved to the first route at or above the declared floor",
                InputsReference = $"job:{context.Job} stage:{context.Stage} attempt:{context.Attempt}",
                OutputsReference = $"operation:{operation.Id}",
                Decision = $"route:{resolved.Route.Id} tier:{resolved.Route.Tier} floor:{resolved.EffectiveFloorApplied}",
                CostReference = $"operation:{operation.Id}",
                Risk = resolved.FloorWasReduced
                    ? "floor reduced under the request's declared policy; the item is barred from a releasable state until re-verified"
                    : "none",
                RetentionClass = RetentionClass.FinancialRecord,
            },
            cancellationToken).ConfigureAwait(false);

        var alerts = await transaction.Budgets
            .EvaluateAsync(request.Attribution, period, now, cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new CapabilityOutcome.Completed(operation, alerts);
    }

    /// <summary>
    /// Records an operation that reached no provider — a refusal, a hold, or a non-AI substitute —
    /// with zero units and no applied price, so the cost the datastore computes for it is zero.
    /// </summary>
    private async Task<CapabilityOutcome> RecordNonProviderOutcomeAsync(
        CapabilityRequest request,
        CapabilityInvocationContext context,
        DateTimeOffset now,
        DateOnly period,
        OperationOutcome outcome,
        string reason,
        RouteId? route,
        string? deterministicTaskName,
        Func<OperationRecord, CapabilityOutcome> project,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);

        var operation = await transaction.Operations.RecordAsync(
            new OperationDraft
            {
                Id = OperationId.New(),
                Run = context.Run,
                Attribution = request.Attribution,
                Capability = request.Capability,
                Route = route,
                Model = null,
                DeterministicTaskName = deterministicTaskName,
                Units = UnitCounts.None,
                AppliedPrice = null,
                CostBasis = CostBasis.Measurement,
                Duration = TimeSpan.Zero,
                Outcome = outcome,
                OccurredAt = now,
                Attempt = context.Attempt,
                FailureReason = outcome is OperationOutcome.Succeeded ? null : reason,
            },
            cancellationToken).ConfigureAwait(false);

        await transaction.Audit.AppendAsync(
            new AuditEntryDraft
            {
                Actor = context.Actor.ToString(),
                Action = $"capability.{outcome.ToString().ToLowerInvariant()}",
                Subject = $"item:{request.Attribution.Item}",
                Reason = reason,
                InputsReference = $"job:{context.Job} stage:{context.Stage} attempt:{context.Attempt}",
                OutputsReference = $"operation:{operation.Id}",
                Decision = outcome.ToString(),
                CostReference = $"operation:{operation.Id}",
                Risk = outcome == OperationOutcome.Held ? "work is held and escalates on its declared timeout" : "none",
                RetentionClass = RetentionClass.OperationalRecord,
            },
            cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return project(operation);
    }

    private Task<CapabilityOutcome> RecordHeldAsync(
        CapabilityRequest request,
        CapabilityInvocationContext context,
        DateTimeOffset now,
        DateOnly period,
        CapabilityResolution.Held held,
        CancellationToken cancellationToken)
    {
        var stamped = RouteResolver.WithEscalation(held, now, request.HoldTimeout);
        return RecordNonProviderOutcomeAsync(
            request, context, now, period, OperationOutcome.Held,
            $"held at floor {stamped.FloorRequired}: {stamped.Reason}; routes tried: {string.Join(",", stamped.RoutesTried)}",
            null, null,
            _ => new CapabilityOutcome.Held(
                stamped.Reason,
                stamped.FloorRequired,
                stamped.EscalatesAt,
                stamped.EscalateToOwner),
            cancellationToken);
    }

    private async Task<ModelPriceId?> ResolveAppliedPriceAsync(
        ModelId model,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var prices = await _registers.PricesInForceAsync(asOf, cancellationToken).ConfigureAwait(false);
        return prices.FirstOrDefault(p => p.Model.Equals(model))?.Id;
    }

    private static IReadOnlyDictionary<ModelId, UnitPrices> BuildPriceTable(IReadOnlyList<ModelPrice> prices)
    {
        var table = new Dictionary<ModelId, UnitPrices>();
        foreach (var group in prices.GroupBy(p => p.Model))
        {
            var input = group.FirstOrDefault(p => p.UnitKind == PriceUnitKind.InputUnit);
            var output = group.FirstOrDefault(p => p.UnitKind == PriceUnitKind.OutputUnit);
            var cached = group.FirstOrDefault(p => p.UnitKind == PriceUnitKind.CachedUnit);
            var currency = input?.Currency ?? output?.Currency ?? cached?.Currency ?? Money.DefaultCurrency;

            table[group.Key] = new UnitPrices(
                input?.UnitPrice ?? 0m,
                output?.UnitPrice ?? 0m,
                cached?.UnitPrice ?? 0m,
                currency);
        }

        return table;
    }

    private static FailureClass MapSignal(ProviderFailureSignal signal) => signal switch
    {
        ProviderFailureSignal.RateLimited => FailureClass.RateLimited,
        ProviderFailureSignal.QuotaExhausted => FailureClass.QuotaExhausted,
        ProviderFailureSignal.Outage => FailureClass.Outage,
        _ => FailureClass.Transient,
    };
}
