using MediaCompany.Domain.Authority;

namespace MediaCompany.Domain.Rights;

public enum LicenceType
{
    Unspecified = 0,
    RoyaltyFreeStock = 1,
    RightsManagedStock = 2,
    CreativeCommons = 3,
    OriginalWork = 4,
    ProviderGenerated = 5,
    PublicDomain = 6,
}

public enum AssessedRisk
{
    Unspecified = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}

/// <summary>
/// One asset held against an item, carrying its permission basis on the row itself rather than
/// by reference to an external system, so that the permission answer of acceptance criterion
/// AC-008 needs no lookup outside the record.
///
/// Every basis field is nullable by construction. That is deliberate: an incomplete record must
/// be representable so that it can resolve as not permitted (constraint C-022), rather than
/// being rejected at construction and never appearing in the record at all.
/// </summary>
public sealed record Asset
{
    public required AssetId Id { get; init; }
    public required ItemId Item { get; init; }
    public required string Library { get; init; }

    public string? Source { get; init; }
    public string? Creator { get; init; }
    public LicenceType LicenceType { get; init; } = LicenceType.Unspecified;
    public string? LicenceReference { get; init; }
    public bool? CommercialUsePermitted { get; init; }
    public bool? ModificationPermitted { get; init; }
    public string? AttributionRequirement { get; init; }
    public string? PlatformRestrictions { get; init; }
    public DateOnly? Expiry { get; init; }
    public string? ProofOfLicenceReference { get; init; }
    public AssessedRisk AssessedRisk { get; init; } = AssessedRisk.Unspecified;
    public WorkforceRole? VerifiedBy { get; init; }
    public DateOnly? VerifiedOn { get; init; }
}

/// <summary>A channel's registration state on one library, with its date (acceptance criterion AC-007).</summary>
public sealed record LibraryRegistration(ChannelId Channel, string Library, DateOnly RegisteredOn);
