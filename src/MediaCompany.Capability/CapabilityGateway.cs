using MediaCompany.Application.Ports;
using MediaCompany.Capability.Providers;
using MediaCompany.Credentials;
using MediaCompany.Deterministic;
using MediaCompany.Deterministic.Accounting;
using MediaCompany.Deterministic.Resilience;
using MediaCompany.Deterministic.Routing;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
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

/// <summary>
/// The realization of the boundary.
///
/// ONE ADMISSION TRANSACTION, ONE BOOKING INSTANT (the AI-economics change, decision D-001 of its
/// design). The boundary opens its transaction BEFORE resolution and keeps it to the commit. Its first
/// statement reserves the booking instant — the later of the datastore's clock and the record horizon,
/// under a shared hold on the horizon kept to the commit — and every instant this boundary writes or
/// decides by is that one: the prices in force, the cost controller's readings and the evidence are read
/// at it, resolution takes it as its instant so a held outcome escalates from it, a recorded availability
/// transition takes effect at it, the operation is booked at it and its decision record is stamped with
/// it. The process clock decides nothing here. The provider is called with the transaction open; the
/// audit chain head is taken only at the first append, after the call returns.
///
/// Where the transaction is lost after the provider was called, the incurred attempt is recorded on a
/// fresh transaction under a fresh reservation, and its decision record says that the admitted month's
/// reservation was lost and re-evaluates the governing readings against the month it was booked into.
/// </summary>
public sealed class CapabilityGateway : ICapabilityGateway
{
    /// <summary>The reservation statement of every admission whose reservation held to its booking.</summary>
    public const string ReservationHeld =
        "the booking instant reserved at admission held to the booking, under the shared hold on the record horizon, so "
        + "the operation is booked into the month it was admitted against";

    private readonly IRouteRegistry _routes;
    private readonly IRouteAvailabilityLedger _availability;
    private readonly IOperatingRegisters _registers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICredentialBroker _broker;
    private readonly IReadOnlyDictionary<ProviderAccountId, IProviderAdapter> _adapters;

    internal CapabilityGateway(
        IRouteRegistry routes,
        IRouteAvailabilityLedger availability,
        IOperatingRegisters registers,
        IUnitOfWork unitOfWork,
        ICredentialBroker broker,
        IReadOnlyDictionary<ProviderAccountId, IProviderAdapter> adapters)
    {
        _routes = routes;
        _availability = availability;
        _registers = registers;
        _unitOfWork = unitOfWork;
        _broker = broker;
        _adapters = adapters;
    }

    public async Task<CapabilityOutcome> ExecuteAsync(
        CapabilityRequest request,
        CapabilityInvocationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        // Routes, forbidden sources, availability and accounts stay read through their delivered ports.
        var admitted = await _routes.AdmittedRoutesAsync(request.Capability, cancellationToken).ConfigureAwait(false);
        var forbidden = await _routes.ForbiddenSourcesAsync(cancellationToken).ConfigureAwait(false);
        var availability = await _availability
            .CurrentAsync(admitted.Select(r => r.Id).ToArray(), cancellationToken).ConfigureAwait(false);
        var accounts = await _registers.ProviderAccountsAsync(cancellationToken).ConfigureAwait(false);

        await using var held = new HeldTransaction(await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false));

        var admission = await AdmitAsync(held.Transaction, request, admitted, forbidden, availability, accounts, cancellationToken)
            .ConfigureAwait(false);

        return admission.Record.Resolution switch
        {
            CapabilityResolution.Refused refused =>
                await RecordNonProviderOutcomeAsync(
                    held.Transaction, admission, request, context, OperationOutcome.Refused,
                    $"{refused.Reason}: {refused.Detail}", null, null, null,
                    _ => new CapabilityOutcome.Refused(refused.Reason, refused.Detail),
                    cancellationToken).ConfigureAwait(false),

            CapabilityResolution.Held hold =>
                await RecordNonProviderOutcomeAsync(
                    held.Transaction, admission, request, context, OperationOutcome.Held,
                    $"held at floor {hold.FloorRequired}: {hold.Reason}; routes tried: {string.Join(",", hold.RoutesTried)}",
                    null, null, null,
                    _ => new CapabilityOutcome.Held(hold.Reason, hold.FloorRequired, hold.EscalatesAt, hold.EscalateToOwner),
                    cancellationToken).ConfigureAwait(false),

            CapabilityResolution.Resolved resolved =>
                await ExecuteResolvedAsync(held, admission, request, context, resolved, cancellationToken).ConfigureAwait(false),

            _ => throw new InvalidOperationException("Unreachable: the resolution union has three members."),
        };
    }

    /// <summary>
    /// Reserves the booking instant and reads, at it, everything resolution decides by: the prices in
    /// force, the governing readings in one statement and the evidence in one statement; the cost
    /// controller decides, and resolution runs on the reserved instant.
    /// </summary>
    private static async Task<Admission> AdmitAsync(
        IWorkTransaction transaction,
        CapabilityRequest request,
        IReadOnlyList<Route> admitted,
        IReadOnlyList<ForbiddenSource> forbidden,
        IReadOnlyDictionary<RouteId, RouteAvailability> availability,
        IReadOnlyList<ProviderAccount> accounts,
        CancellationToken cancellationToken)
    {
        var reservation = await transaction.Admission.ReserveAsync(cancellationToken).ConfigureAwait(false);

        var models = admitted
            .Select(r => r.Target)
            .OfType<RouteTarget.ProviderRoute>()
            .Select(p => p.Model)
            .Distinct()
            .ToArray();

        var prices = models.Length == 0
            ? []
            : await transaction.Admission.PricesInForceAsync(models, cancellationToken).ConfigureAwait(false);

        var readings = await transaction.Admission
            .GoverningReadingsAsync(request.Attribution, ApprovedEnvelope.Metered, cancellationToken).ConfigureAwait(false);

        var controller = CostController.Decide(readings, reservation.Instant);

        var evidence = request.TaskClass is { } taskClass && admitted.Count > 0
            ? await transaction.Admission
                .EvidenceAsync(admitted.Select(r => r.Id).ToArray(), taskClass, cancellationToken).ConfigureAwait(false)
            : [];

        var record = RouteResolver.ResolveWithRecord(
            request,
            new ResolutionInputs
            {
                AdmittedRoutes = admitted,
                ForbiddenSources = forbidden,
                Availability = availability,
                AccountStatus = accounts.ToDictionary(a => a.Id, a => a.Status),
                Prices = BuildPriceTable(prices),
                BudgetRemaining = readings.Headroom,
                Now = reservation.Instant,
                Controller = controller,
                Evidence = evidence,
            });

        return new Admission(reservation, controller, record);
    }

    private async Task<CapabilityOutcome> ExecuteResolvedAsync(
        HeldTransaction held,
        Admission admission,
        CapabilityRequest request,
        CapabilityInvocationContext context,
        CapabilityResolution.Resolved resolved,
        CancellationToken cancellationToken)
    {
        switch (resolved.Route.Target)
        {
            case RouteTarget.HoldAndEscalate hold:
                // The recorded emergency position for high-stakes review. No provider is reached,
                // so the operation carries zero AI cost and is recorded as held. It escalates from the
                // booking instant the datastore reserved, never from a process clock.
                return await RecordNonProviderOutcomeAsync(
                    held.Transaction, admission, request, context, OperationOutcome.Held,
                    hold.Reason, resolved.Route.Id, resolved.Route.StatedReasoningTier, null,
                    _ => new CapabilityOutcome.Held(
                        RefusalReason.NoAvailableRoute,
                        resolved.EffectiveFloorApplied,
                        admission.Reservation.Instant + request.HoldTimeout,
                        EscalatedToOwner: true),
                    cancellationToken).ConfigureAwait(false);

            case RouteTarget.NonAiSubstitute substitute:
                // Routes to a member of the deterministic task set, which holds no dependency on
                // this assembly and therefore carries zero AI cost by construction.
                return await RecordNonProviderOutcomeAsync(
                    held.Transaction, admission, request, context, OperationOutcome.Succeeded,
                    $"non-AI substitute: {substitute.DeterministicTaskName}", resolved.Route.Id,
                    resolved.Route.StatedReasoningTier,
                    substitute.DeterministicTaskName,
                    op => new CapabilityOutcome.Substituted(substitute.DeterministicTaskName, op),
                    cancellationToken).ConfigureAwait(false);

            case RouteTarget.ProviderRoute provider:
                return await ExecuteProviderAsync(held, admission, request, context, resolved, provider, cancellationToken)
                    .ConfigureAwait(false);

            default:
                throw new InvalidOperationException("Unreachable: the route target union has three members.");
        }
    }

    private async Task<CapabilityOutcome> ExecuteProviderAsync(
        HeldTransaction held,
        Admission admission,
        CapabilityRequest request,
        CapabilityInvocationContext context,
        CapabilityResolution.Resolved resolved,
        RouteTarget.ProviderRoute provider,
        CancellationToken cancellationToken)
    {
        if (!_adapters.TryGetValue(provider.ProviderAccount, out var adapter))
        {
            return await RecordNonProviderOutcomeAsync(
                held.Transaction, admission, request, context, OperationOutcome.Failed,
                $"no adapter is registered for provider account {provider.ProviderAccount}",
                resolved.Route.Id, resolved.Route.StatedReasoningTier, null,
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
                held.Transaction, admission, request, context, OperationOutcome.Refused,
                $"credential refused: {credentialRefused.Reason} — {credentialRefused.Detail}",
                resolved.Route.Id, resolved.Route.StatedReasoningTier, null,
                _ => new CapabilityOutcome.Refused(RefusalReason.NoAvailableRoute, credentialRefused.Detail),
                cancellationToken).ConfigureAwait(false);
        }

        var handle = ((CredentialOutcome.Issued)issuance).Handle;

        // The provider is called WITH THE ADMISSION TRANSACTION OPEN, so the shared hold on the record
        // horizon keeps the reserved instant bookable for the whole call; the call is bounded by the
        // client's own timeout and is never retried inside the transaction.
        var attempt = await adapter.InvokeAsync(provider, request, handle, cancellationToken).ConfigureAwait(false);

        try
        {
            return await RecordAttemptAsync(
                held.Transaction, admission, request, context, resolved, provider, attempt, ReservationHeld, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception lost) when (lost is not OperationCanceledException)
        {
            // The admission transaction was lost after the provider was called. The cost was incurred,
            // so it is never dropped: it is recorded on a fresh transaction under a fresh reservation,
            // and the decision record says the admitted month's reservation was lost and re-evaluates the
            // governing readings against the month the attempt is booked into.
            try
            {
                await held.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception disposal) when (disposal is not OperationCanceledException)
            {
                // The lost transaction is already gone; disposing it can fail and changes nothing durable.
            }

            await using var fresh = new HeldTransaction(await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false));
            var reservation = await fresh.Transaction.Admission.ReserveAsync(cancellationToken).ConfigureAwait(false);
            var readings = await fresh.Transaction.Admission
                .GoverningReadingsAsync(request.Attribution, ApprovedEnvelope.Metered, cancellationToken).ConfigureAwait(false);

            var statement =
                $"the booking instant reserved at admission, {admission.Reservation.Instant:O} in "
                + $"{admission.Reservation.Month:yyyy-MM}, was lost after the provider was called ({lost.GetType().Name}); "
                + $"the incurred attempt is booked under a fresh reservation at {reservation.Instant:O} in "
                + $"{reservation.Month:yyyy-MM}, and the governing readings recorded here were re-evaluated against that month";

            return await RecordAttemptAsync(
                fresh.Transaction,
                admission with { Reservation = reservation, Controller = CostController.Decide(readings, reservation.Instant) },
                request, context, resolved, provider, attempt, statement, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Records one provider attempt on the admission transaction: the availability transition it drives,
    /// the operation booked at the reserved instant, its decision record, the audit entry, and the
    /// delivered budget evaluation for the booked month; then commits.
    /// </summary>
    private static async Task<CapabilityOutcome> RecordAttemptAsync(
        IWorkTransaction transaction,
        Admission admission,
        CapabilityRequest request,
        CapabilityInvocationContext context,
        CapabilityResolution.Resolved resolved,
        RouteTarget.ProviderRoute provider,
        ProviderAttempt attempt,
        string reservationStatement,
        CancellationToken cancellationToken)
    {
        // Whatever the provider did, the attempt is accounted. A failed attempt and a retried
        // attempt each get their own record, which is how exactly-once stays well defined under
        // retry (decision D-011). The availability transition the failure drives is written on
        // this same transaction, at the reserved instant, so the state a route moves to and the
        // operation that caused the move commit together on one clock.
        if (!attempt.Succeeded && attempt.Signal is { } signal)
        {
            var state = FailureHandling.StateFor(MapSignal(signal));
            if (state is { } availabilityState)
            {
                await transaction.Availability.RecordAsync(
                    new RouteAvailability(
                        resolved.Route.Id,
                        availabilityState,
                        admission.Reservation.Instant,
                        attempt.FailureReason ?? signal.ToString(),
                        null,
                        null),
                    cancellationToken).ConfigureAwait(false);
            }
        }

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
                CostBasis = attempt.CostBasis,
                Duration = attempt.Duration,
                Outcome = attempt.Succeeded ? OperationOutcome.Succeeded : OperationOutcome.Failed,
                OccurredAt = admission.Reservation.Instant,
                Attempt = context.Attempt,
                FailureReason = attempt.FailureReason,
                ReasoningTierRequested = request.ReasoningTier,

                // Read from the ADMITTED ROUTE, never from the request. A route that states no
                // tier yields the explicit absence marker rather than an inferred value.
                ReasoningTierServed = resolved.Route.StatedReasoningTier,
            },
            cancellationToken).ConfigureAwait(false);

        await RecordDecisionAsync(transaction, admission, operation, reservationStatement, cancellationToken).ConfigureAwait(false);

        await transaction.Audit.AppendAsync(
            new AuditEntryDraft
            {
                Actor = context.Actor.ToString(),
                Action = "capability.resolve-and-invoke",
                Subject = $"item:{request.Attribution.Item}",
                Reason = attempt.FailureReason ?? $"resolved on the {resolved.Basis} basis at or above the declared floor",
                InputsReference = $"job:{context.Job} stage:{context.Stage} attempt:{context.Attempt}",
                OutputsReference = $"operation:{operation.Id}",
                Decision = $"route:{resolved.Route.Id} tier:{resolved.Route.Tier} floor:{resolved.EffectiveFloorApplied} "
                    + $"basis:{resolved.Basis} tier-outcome:{resolved.TierOutcome} controller:{admission.Controller.Action}",
                CostReference = $"operation:{operation.Id}",
                Risk = resolved.FloorWasReduced
                    ? "floor reduced under the request's declared policy; the item is barred from a releasable state until re-verified"
                    : "none",
                RetentionClass = RetentionClass.FinancialRecord,
            },
            cancellationToken).ConfigureAwait(false);

        // Budgets are evaluated for the month the datastore BOOKED the operation in, read from the
        // stored instant the recorder returned, which is the reserved instant, so an alert is raised
        // against the month the operation was admitted against and counts towards.
        var booked = operation.OccurredAt.UtcDateTime;
        var bookedPeriod = new DateOnly(booked.Year, booked.Month, 1);

        var alerts = await transaction.Budgets
            .EvaluateAsync(request.Attribution, bookedPeriod, operation.OccurredAt, cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new CapabilityOutcome.Completed(operation, alerts);
    }

    /// <summary>
    /// Records an operation that reached no provider — a refusal, a hold, a deferral or a non-AI
    /// substitute — on the admission transaction, with zero units and no applied price, so the cost
    /// the datastore computes for it is zero, together with its decision record.
    /// </summary>
    private static async Task<CapabilityOutcome> RecordNonProviderOutcomeAsync(
        IWorkTransaction transaction,
        Admission admission,
        CapabilityRequest request,
        CapabilityInvocationContext context,
        OperationOutcome outcome,
        string reason,
        RouteId? route,
        MediaCompany.Domain.Capabilities.ReasoningTier? servedTier,
        string? deterministicTaskName,
        Func<OperationRecord, CapabilityOutcome> project,
        CancellationToken cancellationToken)
    {
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
                CostBasis = CostBasis.Measurement,
                Duration = TimeSpan.Zero,
                Outcome = outcome,
                OccurredAt = admission.Reservation.Instant,
                Attempt = context.Attempt,
                FailureReason = outcome is OperationOutcome.Succeeded ? null : reason,
                ReasoningTierRequested = request.ReasoningTier,
                ReasoningTierServed = servedTier,
            },
            cancellationToken).ConfigureAwait(false);

        await RecordDecisionAsync(transaction, admission, operation, ReservationHeld, cancellationToken).ConfigureAwait(false);

        await transaction.Audit.AppendAsync(
            new AuditEntryDraft
            {
                Actor = context.Actor.ToString(),
                Action = $"capability.{outcome.ToString().ToLowerInvariant()}",
                Subject = $"item:{request.Attribution.Item}",
                Reason = reason,
                InputsReference = $"job:{context.Job} stage:{context.Stage} attempt:{context.Attempt}",
                OutputsReference = $"operation:{operation.Id}",
                Decision = $"{outcome} controller:{admission.Controller.Action}",
                CostReference = $"operation:{operation.Id}",
                Risk = outcome == OperationOutcome.Held ? "work is held and escalates on its declared timeout" : "none",
                RetentionClass = RetentionClass.OperationalRecord,
            },
            cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return project(operation);
    }

    private static Task RecordDecisionAsync(
        IWorkTransaction transaction,
        Admission admission,
        OperationRecord operation,
        string reservationStatement,
        CancellationToken cancellationToken) =>
        transaction.Admission.RecordDecisionAsync(
            new AdmissionDecisionDraft
            {
                Operation = operation.Id,
                Controller = admission.Controller,
                Selection = admission.Record.Selection,
                TierOutcome = admission.Record.Resolution is CapabilityResolution.Resolved resolved ? resolved.TierOutcome : null,
                TierStatement = admission.Record.TierStatement,
                ReservationStatement = reservationStatement,
            },
            cancellationToken);

    /// <summary>
    /// The price table resolution estimates with. A model enters it only where it is priced in force for
    /// EVERY ONE of the input, output and cached unit kinds; a model missing any of them is absent, so
    /// resolution removes its routes with their own reason rather than estimating a missing price as zero.
    /// Where two rows of one kind are in force the higher price is taken, as the recorder applies it.
    /// </summary>
    private static IReadOnlyDictionary<ModelId, UnitPrices> BuildPriceTable(IReadOnlyList<ModelPrice> prices)
    {
        var table = new Dictionary<ModelId, UnitPrices>();
        foreach (var group in prices.GroupBy(p => p.Model))
        {
            var input = group.Where(p => p.UnitKind == PriceUnitKind.InputUnit).MaxBy(p => p.UnitPrice);
            var output = group.Where(p => p.UnitKind == PriceUnitKind.OutputUnit).MaxBy(p => p.UnitPrice);
            var cached = group.Where(p => p.UnitKind == PriceUnitKind.CachedUnit).MaxBy(p => p.UnitPrice);

            if (input is null || output is null || cached is null)
            {
                continue;
            }

            table[group.Key] = new UnitPrices(input.UnitPrice, output.UnitPrice, cached.UnitPrice, input.Currency);
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

    /// <summary>What one admission reserved, decided and resolved.</summary>
    private sealed record Admission(BookingReservation Reservation, ControllerDecision Controller, ResolutionRecord Record);

    /// <summary>The admission transaction, disposed exactly once whichever path ends it.</summary>
    private sealed class HeldTransaction(IWorkTransaction transaction) : IAsyncDisposable
    {
        private bool _disposed;

        public IWorkTransaction Transaction { get; } = transaction;

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await Transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}
