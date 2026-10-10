using System.Net.Http.Headers;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;

namespace MediaCompany.Credentials;

/// <summary>What a caller asks the broker for. It names a scope, never a secret.</summary>
public sealed record CredentialRequest
{
    public required CredentialClass CredentialClass { get; init; }
    public required ProviderAccountId ProviderAccount { get; init; }
    public required JobId Job { get; init; }
    public required LifecyclePosition Stage { get; init; }
    public required WorkforceRole Actor { get; init; }
    public ChannelId? Channel { get; init; }
    public CapabilityClass? Capability { get; init; }

    /// <summary>
    /// How the account's secret is presented (the production change, decision D-002 of its design), as the
    /// account records it. The default is the delivered bearer authorization.
    /// </summary>
    public MediaCompany.Domain.Registry.AuthenticationScheme Scheme { get; init; } =
        MediaCompany.Domain.Registry.AuthenticationScheme.BearerAuthorization;

    /// <summary>The vendor's key header for the key-header scheme, named by the vendor contract; not a secret.</summary>
    public string? KeyHeaderName { get; init; }

    /// <summary>Required for <see cref="CredentialClass.Release"/>; ignored otherwise.</summary>
    public GatePassToken? GatePass { get; init; }

    /// <summary>Required for <see cref="CredentialClass.Release"/>: the count of open blocks on the item.</summary>
    public int OpenBlockCount { get; init; }

    /// <summary>
    /// The three conditions of first publication, required for <see cref="CredentialClass.Release"/>.
    ///
    /// Null is NOT a way past the check: a release request carrying no register is refused exactly
    /// as one carrying an unsatisfied register is. An empty register refuses all three by name,
    /// which is the correct reading of no recorded observation.
    /// </summary>
    public FirstPublicationConditionRegister? FirstPublicationConditions { get; init; }
}

/// <summary>
/// The credential broker (module M-010, decision D-004). It is the only module in the system that
/// holds a dependency on the secret store; every other module depends on
/// <see cref="ScopedHandle"/>, which carries no secret.
/// </summary>
public interface ICredentialBroker
{
    /// <summary>
    /// Issues a handle scoped to one task. The secret reference is resolved at issuance time and
    /// never at start-up, so a rotation takes effect on the next issuance and a revocation
    /// invalidates live handles at their next presentation, with no redeployment and no restart
    /// (constraint C-008, risk R-007).
    /// </summary>
    Task<CredentialOutcome> IssueAsync(CredentialRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Exchanges a handle for the provider credential AT THE TRANSPORT LAYER.
///
/// The method attaches the credential to an outbound request and returns nothing but an outcome.
/// There is no overload that returns the credential, and none can be added without changing this
/// interface, which is the structural form decision D-004 requires: credential material never
/// enters the caller's scope, because no method here gives the caller a way to hold it.
/// </summary>
public interface ICredentialExchange
{
    Task<CredentialOutcome> AttachAsync(
        ScopedHandle handle,
        HttpRequestMessage request,
        CancellationToken cancellationToken);
}

/// <summary>
/// The dedicated external secret store (assumption A-007). It is INTERNAL to this assembly: no
/// other project can reference it, so no other project can read a secret value even by accident.
/// </summary>
internal interface ISecretStore
{
    /// <summary>
    /// Resolves the secret for one holder, or null when the holder does not exist or the secret
    /// was revoked. Called at issuance and at presentation, never at start-up.
    /// </summary>
    Task<string?> ResolveAsync(CredentialHolderKey holder, CancellationToken cancellationToken);
}

/// <summary>
/// The holder key, being the pair of provider account and channel, and unique. Because the key is
/// the identity of a holder, a secret reference cannot be held by a second holder: sharing a
/// credential across providers, accounts or channels is unrepresentable rather than discouraged
/// (constraint C-008, acceptance criterion AC-016).
/// </summary>
public readonly record struct CredentialHolderKey(ProviderAccountId ProviderAccount, ChannelId? Channel);

/// <summary>
/// The broker and the exchange, realized together because they share the handle table. A handle
/// is held here, in the broker, and the caller holds only its identity.
/// </summary>
public sealed class CredentialBroker : ICredentialBroker, ICredentialExchange
{
    private readonly ISecretStore _store;
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _ttl;
    private readonly Dictionary<Guid, LiveHandle> _live = new();
    private readonly List<CredentialAuditEvent> _audit = [];
    private readonly Lock _gate = new();

    internal CredentialBroker(ISecretStore store, Func<DateTimeOffset> clock, TimeSpan ttl)
    {
        _store = store;
        _clock = clock;
        _ttl = ttl;
    }

    /// <summary>Every issuance and every presentation, carrying the handle identity, never the value.</summary>
    public IReadOnlyList<CredentialAuditEvent> AuditTrail
    {
        get
        {
            lock (_gate)
            {
                return _audit.ToArray();
            }
        }
    }

    public async Task<CredentialOutcome> IssueAsync(CredentialRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CredentialClass == CredentialClass.Release)
        {
            // Layer four of decision D-003. A release credential is reachable only by the
            // publishing role, only with a gate-pass token naming the exact item version, and only
            // with zero open blocks.
            if (request.Actor != WorkforceRole.Publisher)
            {
                return Record(request, new CredentialOutcome.Refused(
                    CredentialRefusal.NotPublishingRole,
                    $"{request.Actor} is not the publishing role; a release credential is reachable by no other role."));
            }

            if (request.GatePass is null)
            {
                return Record(request, new CredentialOutcome.Refused(
                    CredentialRefusal.GateNotPassed,
                    "No gate-pass token was presented; a release credential is not reachable before the gate passes."));
            }

            if (request.OpenBlockCount > 0)
            {
                return Record(request, new CredentialOutcome.Refused(
                    CredentialRefusal.BlockStanding,
                    $"{request.OpenBlockCount} block(s) stand against item {request.GatePass.Item}."));
            }

            // The issuance predicate ADDITIONALLY requires all three conditions of first
            // publication. None of them is discharged, so no release credential is issuable at
            // all, and this holds whatever the gate, the blocks and the role say.
            //
            // An absent register refuses: it is read as no recorded observation, which folds to
            // unknown and therefore to not satisfied, never as nothing to check.
            var conditions = request.FirstPublicationConditions ?? FirstPublicationConditionRegister.Empty;
            var unsatisfied = conditions.Unsatisfied();
            if (unsatisfied.Count > 0)
            {
                return Record(request, new CredentialOutcome.Refused(
                    CredentialRefusal.FirstPublicationConditionUnmet,
                    string.Join(" ", unsatisfied.Select(s => s.Describe()))));
            }
        }

        if (!ActionSet.Holds(request.Actor, request.CredentialClass == CredentialClass.Release
                ? ActionKind.ReleaseCredentialRequest
                : ActionKind.JobDispatch))
        {
            return Record(request, new CredentialOutcome.Refused(
                CredentialRefusal.NotPublishingRole,
                $"{request.Actor} holds no action that requests a {request.CredentialClass} credential."));
        }

        var holder = new CredentialHolderKey(request.ProviderAccount, request.Channel);

        // Resolved now, not at start-up. This is what makes rotation and revocation take effect
        // without a redeployment.
        var secret = await _store.ResolveAsync(holder, cancellationToken).ConfigureAwait(false);
        if (secret is null)
        {
            return Record(request, new CredentialOutcome.Refused(
                CredentialRefusal.NoHolder,
                $"No holder exists for {holder.ProviderAccount} on channel {holder.Channel?.ToString() ?? "(none)"}, or its secret was revoked."));
        }

        var now = _clock();
        var handle = new ScopedHandle(
            Guid.NewGuid(),
            request.CredentialClass,
            request.ProviderAccount,
            request.Channel,
            request.Capability,
            request.Job,
            request.Stage,
            now,
            now + _ttl,
            useBound: 1);

        lock (_gate)
        {
            // The scheme and the vendor's header are held HERE, in the broker's own table, beside the handle it
            // issued (the production change, decision D-002 of its design): the handle the caller holds carries
            // neither, so no caller can redirect the secret to another header by presenting an altered copy.
            _live[handle.HandleId] = new LiveHandle(handle, holder, RemainingUses: handle.UseBound)
            {
                Scheme = request.Scheme,
                HeaderName = request.KeyHeaderName,
            };
            _audit.Add(new CredentialAuditEvent(now, "issued", handle.HandleId, holder, null));
        }

        return new CredentialOutcome.Issued(handle);
    }

    public async Task<CredentialOutcome> AttachAsync(
        ScopedHandle handle,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(request);

        var now = _clock();
        LiveHandle live;

        lock (_gate)
        {
            if (!_live.TryGetValue(handle.HandleId, out var found))
            {
                _audit.Add(new CredentialAuditEvent(now, "refused", handle.HandleId, null, CredentialRefusal.OutOfScope));
                return new CredentialOutcome.Refused(
                    CredentialRefusal.OutOfScope,
                    "The handle is not live in this broker.");
            }

            live = found;
        }

        if (handle.IsExpiredAt(now))
        {
            return RecordPresentation(now, handle, live.Holder, CredentialRefusal.Expired,
                $"The handle expired at {handle.ExpiresAt:O} and was presented at {now:O}.");
        }

        if (live.RemainingUses <= 0)
        {
            return RecordPresentation(now, handle, live.Holder, CredentialRefusal.UseBoundSpent,
                "The handle's use bound is spent.");
        }

        if (!live.Handle.ProviderAccount.Equals(handle.ProviderAccount) ||
            !Nullable.Equals(live.Handle.Channel, handle.Channel))
        {
            return RecordPresentation(now, handle, live.Holder, CredentialRefusal.OutOfScope,
                "The handle was presented outside the provider account or channel it names.");
        }

        // Re-resolved at presentation. A revocation between issuance and presentation invalidates
        // the live handle here, with no restart.
        var secret = await _store.ResolveAsync(live.Holder, cancellationToken).ConfigureAwait(false);
        if (secret is null)
        {
            return RecordPresentation(now, handle, live.Holder, CredentialRefusal.Revoked,
                "The secret reference was revoked at the store; the live handle is invalid at its next presentation.");
        }

        // The one place the secret value exists in this process, on the transport message. It is
        // never returned, never logged, and never handed to the caller. The scheme is the ISSUED
        // handle's, as the account records it (the production change, decision D-002 of its design).
        if (live.Scheme == MediaCompany.Domain.Registry.AuthenticationScheme.KeyHeader)
        {
            if (string.IsNullOrWhiteSpace(live.HeaderName))
            {
                return RecordPresentation(now, handle, live.Holder, CredentialRefusal.OutOfScope,
                    "The account presents its secret in a key header, and no key header was named for the handle.");
            }

            request.Headers.Remove(live.HeaderName);
            request.Headers.TryAddWithoutValidation(live.HeaderName, secret);
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        }

        int remaining;
        lock (_gate)
        {
            remaining = live.RemainingUses - 1;
            _live[handle.HandleId] = live with { RemainingUses = remaining };
            _audit.Add(new CredentialAuditEvent(now, "presented", handle.HandleId, live.Holder, null));
        }

        return new CredentialOutcome.Attached(handle.HandleId, remaining);
    }

    private CredentialOutcome Record(CredentialRequest request, CredentialOutcome outcome)
    {
        if (outcome is CredentialOutcome.Refused refused)
        {
            lock (_gate)
            {
                _audit.Add(new CredentialAuditEvent(
                    _clock(),
                    "refused",
                    Guid.Empty,
                    new CredentialHolderKey(request.ProviderAccount, request.Channel),
                    refused.Reason));
            }
        }

        return outcome;
    }

    private CredentialOutcome RecordPresentation(
        DateTimeOffset now,
        ScopedHandle handle,
        CredentialHolderKey holder,
        CredentialRefusal reason,
        string detail)
    {
        lock (_gate)
        {
            _audit.Add(new CredentialAuditEvent(now, "refused", handle.HandleId, holder, reason));
        }

        return new CredentialOutcome.Refused(reason, detail);
    }

    private sealed record LiveHandle(ScopedHandle Handle, CredentialHolderKey Holder, int RemainingUses)
    {
        /// <summary>How the issued handle's secret is presented, as its account records it.</summary>
        public MediaCompany.Domain.Registry.AuthenticationScheme Scheme { get; init; } =
            MediaCompany.Domain.Registry.AuthenticationScheme.BearerAuthorization;

        /// <summary>The vendor's header for the key-header scheme; not a secret.</summary>
        public string? HeaderName { get; init; }
    }
}

/// <summary>One audited credential event. It carries the handle identity and the holder, never the value.</summary>
public sealed record CredentialAuditEvent(
    DateTimeOffset At,
    string Event,
    Guid HandleId,
    CredentialHolderKey? Holder,
    CredentialRefusal? Refusal);
