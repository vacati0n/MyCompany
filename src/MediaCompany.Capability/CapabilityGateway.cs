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
///
/// THE CORRECTION CYCLE. Everything an admission decides by is one snapshot of the admission transaction,
/// read by the admission ledger in one statement, and the prices it read are the ones the recorder applies.
/// A metered admission takes the company scope's hold and then the channel scope's before the snapshot it
/// decides on, so two metered admissions of one scope are serialised and cannot together pass a threshold,
/// a budget or the allotment neither reading reached. A provider attempt is recorded under ONE operation
/// identifier minted before the call: a retry after a commit whose outcome is unknown meets the first
/// record and books nothing. Once the provider has returned, recording is not cancellable, so a cancellation
/// never rolls back the record of a cost already incurred.
///
/// THE SECOND CORRECTION CYCLE. A metered admission NEVER WAITS on a scope hold: it tries the company's and
/// then the channel's without waiting, and where either is held by an admission in progress it is deferred
/// and held from the reserved instant under its own recorded reason, so it can never queue behind a provider
/// call into an unnamed timeout, and no wait duration is chosen. The recording of an incurred attempt after a
/// lost transaction takes no scope hold at all; it runs with a command timeout above the provider-call bound,
/// so a row hold it meets is waited out for longer than any admission keeps it, and a recording that still
/// cannot complete ends under a named reason carrying the attempt. A recovered outcome carries the alerts its
/// first commit stored, read back with the operation.
/// </summary>
public sealed class CapabilityGateway : ICapabilityGateway
{
    /// <summary>The reservation statement of every admission whose reservation held to its booking.</summary>
    public const string ReservationHeld =
        "the booking instant reserved at admission held to the booking, under the shared hold on the record horizon, so "
        + "the operation is booked into the month it was admitted against";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICredentialBroker _broker;
    private readonly IReadOnlyDictionary<ProviderAccountId, IProviderAdapter> _adapters;
    private readonly TimeSpan _recoveryCommandTimeout;

    /// <param name="providerCallBound">
    /// The bound on one provider call, the provider client's own timeout. The recording of an incurred
    /// attempt after a lost transaction runs with twice this as its command timeout, so it exceeds the bound
    /// by a whole provider call: a row hold kept by an admission across its call and its writes is waited
    /// out. An infinite bound gives the recording no command timeout.
    /// </param>
    internal CapabilityGateway(
        IUnitOfWork unitOfWork,
        ICredentialBroker broker,
        IReadOnlyDictionary<ProviderAccountId, IProviderAdapter> adapters,
        TimeSpan providerCallBound)
    {
        _unitOfWork = unitOfWork;
        _broker = broker;
        _adapters = adapters;
        _recoveryCommandTimeout = providerCallBound == Timeout.InfiniteTimeSpan || providerCallBound <= TimeSpan.Zero
            ? TimeSpan.Zero
            : providerCallBound + providerCallBound;
    }

    /// <summary>The command timeout the recording of an incurred attempt after a lost transaction runs with; zero is none.</summary>
    internal TimeSpan RecoveryCommandTimeout => _recoveryCommandTimeout;

    public async Task<CapabilityOutcome> ExecuteAsync(
        CapabilityRequest request,
        CapabilityInvocationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        await using var held = new HeldTransaction(await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false));

        var admission = await AdmitAsync(held.Transaction, request, cancellationToken).ConfigureAwait(false);

        // A METERED ADMISSION IS SERIALISED PER SCOPE (the correction cycle): where the first snapshot
        // resolves to a provider route, the company scope's hold and then the channel scope's are taken, and
        // the admission is decided again on a snapshot read after both are held, so it reads every booking
        // of an admission of the same scope that committed before it. The holds are TRIED, never waited on
        // (the second correction cycle): where an admission in progress holds either, this one is deferred.
        if (admission.Record.Resolution is CapabilityResolution.Resolved { Route.Target: RouteTarget.ProviderRoute } first)
        {
            if (!await held.Transaction.Admission.TryHoldScopesAsync(request.Attribution.Channel, cancellationToken).ConfigureAwait(false))
            {
                return await RecordDeferralAsync(held.Transaction, admission, request, context, first, cancellationToken)
                    .ConfigureAwait(false);
            }

            admission = await AdmitAsync(held.Transaction, request, cancellationToken).ConfigureAwait(false);
        }

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
                    cancellationToken,
                    new HeldOutcomeDraft(hold.Reason, hold.EscalatesAt, request.HoldTimeout, hold.EscalateToOwner)).ConfigureAwait(false),

            CapabilityResolution.Resolved resolved =>
                await ExecuteResolvedAsync(held, admission, request, context, resolved, cancellationToken).ConfigureAwait(false),

            _ => throw new InvalidOperationException("Unreachable: the resolution union has three members."),
        };
    }

    /// <summary>
    /// Records a metered admission deferred because another metered admission of its scope is in progress
    /// (the second correction cycle): held under its own reason, escalating on the request's declared hold
    /// timeout from the reserved instant, with the zero-cost operation, the decision record and the audit
    /// entry on the admission transaction. No provider is reached and nothing waited.
    /// </summary>
    private static Task<CapabilityOutcome> RecordDeferralAsync(
        IWorkTransaction transaction,
        Admission admission,
        CapabilityRequest request,
        CapabilityInvocationContext context,
        CapabilityResolution.Resolved first,
        CancellationToken cancellationToken)
    {
        var hold = new CapabilityResolution.Held(
            RefusalReason.MeteredAdmissionInProgress,
            request.QualityFloor,
            first.RoutesTried,
            admission.Reservation.Instant + request.HoldTimeout,
            request.Criticality == Criticality.Critical);

        var deferred = admission with
        {
            Record = admission.Record with
            {
                Resolution = hold,
                TierStatement = "no route was served: a metered admission of the company scope or of channel "
                    + $"{request.Attribution.Channel} was already in progress, so this admission was deferred without waiting",
            },
        };

        return RecordNonProviderOutcomeAsync(
            transaction, deferred, request, context, OperationOutcome.Held,
            $"held at floor {hold.FloorRequired}: {hold.Reason} - a metered admission of the company scope or of channel "
            + $"{request.Attribution.Channel} holds the admission scope, so this one is deferred from the reserved instant "
            + $"and escalates at {hold.EscalatesAt:O}; routes tried: {string.Join(",", hold.RoutesTried)}",
            null, null, null,
            _ => new CapabilityOutcome.Held(hold.Reason, hold.FloorRequired, hold.EscalatesAt, hold.EscalateToOwner),
            cancellationToken,
            new HeldOutcomeDraft(hold.Reason, hold.EscalatesAt, request.HoldTimeout, hold.EscalateToOwner));
    }

    /// <summary>
    /// Reserves the booking instant and reads, at it, everything resolution decides by: the prices in
    /// force, the governing readings in one statement and the evidence in one statement; the cost
    /// controller decides, and resolution runs on the reserved instant.
    /// </summary>
    private static async Task<Admission> AdmitAsync(
        IWorkTransaction transaction,
        CapabilityRequest request,
        CancellationToken cancellationToken)
    {
        var reservation = await transaction.Admission.ReserveAsync(cancellationToken).ConfigureAwait(false);

        // ONE SNAPSHOT (the correction cycle): routes, forbidden sources, availability, accounts, prices,
        // governing readings and evidence in one statement at the reserved instant.
        var snapshot = await transaction.Admission
            .ReadAsync(request.Capability, request.Attribution, ApprovedEnvelope.Metered, request.TaskClass, cancellationToken)
            .ConfigureAwait(false);

        var controller = CostController.Decide(snapshot.Readings, reservation.Instant);

        // The headroom is consulted only where the controller admits metered work, which requires every
        // operation of the month to state its cost (the company reading covers them all), so it is never a
        // partial sum where it decides.
        var record = RouteResolver.ResolveWithRecord(
            request,
            new ResolutionInputs
            {
                AdmittedRoutes = snapshot.Routes,
                ForbiddenSources = snapshot.ForbiddenSources,
                Availability = snapshot.Availability,
                AccountStatus = snapshot.Accounts.ToDictionary(a => a.Id, a => a.Status),
                Prices = BuildPriceTable(snapshot.Prices),
                BudgetRemaining = snapshot.Readings.Headroom,
                Now = reservation.Instant,
                Controller = controller,
                Evidence = snapshot.Evidence,
            });

        return new Admission(reservation, controller, record, snapshot.Prices);
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
                    cancellationToken,
                    new HeldOutcomeDraft(
                        RefusalReason.NoAvailableRoute,
                        admission.Reservation.Instant + request.HoldTimeout,
                        request.HoldTimeout,
                        EscalatesToOwner: true)).ConfigureAwait(false);

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

        // ONE IDENTIFIER PER ATTEMPT, minted before the call (the correction cycle): every write of this
        // attempt, on this transaction or on a fresh one after a loss, is under it, so it is booked once.
        var operationId = OperationId.New();

        // The provider is called WITH THE ADMISSION TRANSACTION OPEN, so the shared hold on the record
        // horizon keeps the reserved instant bookable for the whole call; the call is bounded by the
        // client's own timeout and is never retried inside the transaction.
        var attempt = await adapter.InvokeAsync(provider, request, handle, cancellationToken).ConfigureAwait(false);

        // From here the cost is incurred, so recording is NOT CANCELLABLE: a cancellation that arrives now
        // must not roll back the record of what the provider already did.
        var recording = CancellationToken.None;

        try
        {
            return await RecordAttemptAsync(
                held.Transaction, admission, request, context, resolved, provider, attempt, operationId, ReservationHeld, recording)
                .ConfigureAwait(false);
        }
        catch (Exception lost)
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

            return await RecordIncurredAttemptAsync(
                admission, request, context, resolved, provider, attempt, operationId, lost).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Records an incurred attempt on a fresh transaction after its admission transaction was lost (the second
    /// correction cycle). It takes NO admission scope hold, so an admission in progress can neither delay nor
    /// refuse it, and its transaction runs with a command timeout above the provider-call bound, so a row hold
    /// it meets (the horizon, the chain head, the operation key) is waited out for longer than any admission
    /// keeps one. Where the first commit reached the datastore, the stored operation is returned with the
    /// alerts that commit stored. Any other failure ends under a named reason carrying the attempt.
    /// </summary>
    private async Task<CapabilityOutcome> RecordIncurredAttemptAsync(
        Admission admission,
        CapabilityRequest request,
        CapabilityInvocationContext context,
        CapabilityResolution.Resolved resolved,
        RouteTarget.ProviderRoute provider,
        ProviderAttempt attempt,
        OperationId operationId,
        Exception lost)
    {
        // The cost is incurred, so nothing here is cancellable.
        var recording = CancellationToken.None;

        try
        {
            await using var fresh = new HeldTransaction(
                await _unitOfWork.BeginAsync(_recoveryCommandTimeout, recording).ConfigureAwait(false));
            var reservation = await fresh.Transaction.Admission.ReserveAsync(recording).ConfigureAwait(false);
            await fresh.Transaction.Admission.RestorePricesAsync(admission.Prices, recording).ConfigureAwait(false);
            var readings = await fresh.Transaction.Admission
                .GoverningReadingsAsync(request.Attribution, ApprovedEnvelope.Metered, recording).ConfigureAwait(false);

            var statement =
                $"the booking instant reserved at admission, {admission.Reservation.Instant:O} in "
                + $"{admission.Reservation.Month:yyyy-MM}, was lost after the provider was called ({lost.GetType().Name}); "
                + $"the incurred attempt is booked under a fresh reservation at {reservation.Instant:O} in "
                + $"{reservation.Month:yyyy-MM}, and the governing readings recorded here were re-evaluated against that month";

            try
            {
                return await RecordAttemptAsync(
                    fresh.Transaction,
                    admission with { Reservation = reservation, Controller = CostController.Decide(readings, reservation.Instant) },
                    request, context, resolved, provider, attempt, operationId, statement, recording).ConfigureAwait(false);
            }
            catch (OperationAlreadyRecordedException already)
            {
                // The first commit reached the datastore although its reply was lost: the attempt is already
                // booked under its one identifier, with its decision, audit entry and alerts, so nothing more
                // is written and the fresh transaction is rolled back. The alerts that commit stored are read
                // back with the operation, by its booked month and instant, and are not raised twice.
                var booked = already.Recorded.OccurredAt.UtcDateTime;
                var alerts = await fresh.Transaction.Budgets
                    .StoredAlertsAsync(
                        request.Attribution, new DateOnly(booked.Year, booked.Month, 1), already.Recorded.OccurredAt, recording)
                    .ConfigureAwait(false);
                return new CapabilityOutcome.Completed(already.Recorded, alerts);
            }
        }
        catch (Exception failure)
        {
            throw new IncurredAttemptNotRecordedException(
                IsTimeout(failure) ? IncurredAttemptRecordingFailure.RecordingTimedOut : IncurredAttemptRecordingFailure.RecordingFailed,
                operationId,
                request.Attribution,
                resolved.Route.Id,
                provider.Model,
                attempt.Units,
                admission.Reservation.Instant,
                lost,
                failure);
        }
    }

    /// <summary>Whether a failure is, or was caused by, a timeout.</summary>
    private static bool IsTimeout(Exception failure)
    {
        for (var cause = failure; cause is not null; cause = cause.InnerException)
        {
            if (cause is TimeoutException)
            {
                return true;
            }
        }

        return false;
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
        OperationId operationId,
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
                Id = operationId,
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
        CancellationToken cancellationToken,
        HeldOutcomeDraft? held = null)
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

        await RecordDecisionAsync(transaction, admission, operation, ReservationHeld, cancellationToken, held).ConfigureAwait(false);

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
        CancellationToken cancellationToken,
        HeldOutcomeDraft? held = null) =>
        transaction.Admission.RecordDecisionAsync(
            new AdmissionDecisionDraft
            {
                // The held outcome, on a held admission only, written beside the decision on this transaction
                // (the AI-management change, decision D-006 of its design).
                Held = held,
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
    private sealed record Admission(
        BookingReservation Reservation,
        ControllerDecision Controller,
        ResolutionRecord Record,
        IReadOnlyList<ModelPrice> Prices);

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
