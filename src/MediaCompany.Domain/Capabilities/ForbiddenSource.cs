namespace MediaCompany.Domain.Capabilities;

/// <summary>
/// The kinds of capacity a provider's terms forbid, recorded at design fact F-005. The set is
/// closed so that a new kind is a code change the Review Gate sees.
/// </summary>
public enum ForbiddenSourceKind
{
    /// <summary>A consumer chat subscription driven programmatically.</summary>
    ConsumerChatSubscription = 1,

    /// <summary>Multiple personal accounts held to multiply an allowance.</summary>
    MultipliedPersonalAccount = 2,

    /// <summary>A credential shared across accounts, providers or channels.</summary>
    SharedCredential = 3,

    /// <summary>A service whose terms forbid automated access outright.</summary>
    AutomatedAccessProhibited = 4,

    /// <summary>A generation interface withdrawn by its provider.</summary>
    WithdrawnInterface = 5,
}

/// <summary>One entry of the forbidden-source register.</summary>
public sealed record ForbiddenSource
{
    public ForbiddenSource(ForbiddenSourceKind kind, string identifier, string reason, string evidenceReference)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("A forbidden-source entry names the source it forbids.", nameof(identifier));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A forbidden-source entry carries its reason.", nameof(reason));
        }

        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException("A forbidden-source entry carries its evidence reference.", nameof(evidenceReference));
        }

        Kind = kind;
        Identifier = identifier;
        Reason = reason;
        EvidenceReference = evidenceReference;
    }

    public ForbiddenSourceKind Kind { get; }

    /// <summary>The provider account or interface identifier this entry forbids.</summary>
    public string Identifier { get; }

    public string Reason { get; }
    public string EvidenceReference { get; }
}
