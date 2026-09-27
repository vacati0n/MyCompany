using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Work;

namespace MediaCompany.Credentials;

/// <summary>
/// The class of credential a handle authorises. Release credentials are a distinct class whose
/// issuance predicate is stricter, per constraint C-007 and acceptance criterion AC-015.
/// </summary>
public enum CredentialClass
{
    ProviderAccess = 1,
    Release = 2,
}

/// <summary>
/// An opaque scoped handle (module M-010, decision D-004).
///
/// This type carries NO credential material, and there is no member on it that could. The broker
/// returns one of these; the caller holds it, passes it, logs it and serializes it freely, and
/// none of that can leak a secret because the secret is not here. The exchange for the real
/// credential happens at the transport layer inside the provider adapter, after the request object
/// has been assembled, so the credential is never in scope at prompt-assembly time and cannot be
/// serialized into a prompt, an artifact, a log or run evidence (acceptance criterion AC-013).
/// </summary>
public sealed record ScopedHandle
{
    internal ScopedHandle(
        Guid handleId,
        CredentialClass credentialClass,
        ProviderAccountId providerAccount,
        ChannelId? channel,
        CapabilityClass? capability,
        JobId job,
        LifecyclePosition stage,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        int useBound)
    {
        HandleId = handleId;
        CredentialClass = credentialClass;
        ProviderAccount = providerAccount;
        Channel = channel;
        Capability = capability;
        Job = job;
        Stage = stage;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        UseBound = useBound;
    }

    /// <summary>The handle's own identity. Audited on every issuance and presentation; not a secret.</summary>
    public Guid HandleId { get; }

    public CredentialClass CredentialClass { get; }
    public ProviderAccountId ProviderAccount { get; }
    public ChannelId? Channel { get; }
    public CapabilityClass? Capability { get; }
    public JobId Job { get; }
    public LifecyclePosition Stage { get; }
    public DateTimeOffset IssuedAt { get; }
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>How many presentations this handle authorises before it is spent.</summary>
    public int UseBound { get; }

    public bool IsExpiredAt(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>Renders the handle for a log. There is no value to redact, so nothing is redacted.</summary>
    public override string ToString() =>
        $"handle:{HandleId:N} class={CredentialClass} account={ProviderAccount} job={Job} stage={Stage} expires={ExpiresAt:O}";
}

/// <summary>Why a credential request or presentation was refused.</summary>
public enum CredentialRefusal
{
    /// <summary>The handle was presented after its ttl elapsed.</summary>
    Expired = 1,

    /// <summary>The handle was presented for a provider account, channel or capability it does not name.</summary>
    OutOfScope = 2,

    /// <summary>The handle's use bound is spent.</summary>
    UseBoundSpent = 3,

    /// <summary>The requesting role is not the publishing role, for a release credential.</summary>
    NotPublishingRole = 4,

    /// <summary>No gate-pass token for the exact item version was presented.</summary>
    GateNotPassed = 5,

    /// <summary>A block stands against the item.</summary>
    BlockStanding = 6,

    /// <summary>The secret reference was revoked at the store; a revocation takes effect on the next use.</summary>
    Revoked = 7,

    /// <summary>The provider account holds no credential for this channel.</summary>
    NoHolder = 8,

    /// <summary>
    /// At least one of the three conditions of first publication is not satisfied, so no release
    /// credential is issuable.
    ///
    /// STRUCTURAL ABSENCE FIVE. None of the three is discharged, and none can be discharged by
    /// this system: each needs the owner to sign in, verify an identity or open an account. A
    /// hypothetical transport would therefore hold nothing to present, independently of the other
    /// four absences.
    /// </summary>
    FirstPublicationConditionUnmet = 9,
}

/// <summary>The outcome of an issuance or a presentation. A refusal carries its ground and is recorded.</summary>
public abstract record CredentialOutcome
{
    private CredentialOutcome()
    {
    }

    public sealed record Issued(ScopedHandle Handle) : CredentialOutcome;

    public sealed record Attached(Guid HandleId, int RemainingUses) : CredentialOutcome;

    public sealed record Refused(CredentialRefusal Reason, string Detail) : CredentialOutcome;
}
