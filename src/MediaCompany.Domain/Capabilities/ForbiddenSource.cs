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

    /// <summary>
    /// A service whose terms take a licence over the customer's content (the production change, decision D-003
    /// of its design): a narration or image service that would acquire rights in the company's script or
    /// output. Refused like every other kind, before any request is sent.
    /// </summary>
    CustomerContentLicence = 6,
}

/// <summary>A route's recorded position on automated access, from its terms (the production change, decision D-003).</summary>
public enum AutomatedAccessPosition
{
    Permits = 1,
    Prohibits = 2,
}

/// <summary>A route's recorded position on a licence over the customer's content (the production change, decision D-003).</summary>
public enum CustomerContentPosition
{
    NoLicenceTaken = 1,
    LicenceTaken = 2,
}

/// <summary>
/// The two terms positions recorded on a route beside its terms basis, each with the evidence it rests on and
/// the date that evidence was read (the production change, decision D-003 of its design). A position that is
/// absent is NOT RECORDED, which is never read as permitted.
/// </summary>
public sealed record RouteTermsPositions(
    AutomatedAccessPosition? AutomatedAccess,
    CustomerContentPosition? CustomerContent,
    string? EvidenceReference,
    DateOnly? EvidenceReadOn);

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
