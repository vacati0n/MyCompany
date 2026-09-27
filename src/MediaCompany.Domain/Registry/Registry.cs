using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Domain.Registry;

public sealed record Company(CompanyId Id, string Name, string OperatingState);

public sealed record Channel(ChannelId Id, CompanyId Company, string Platform, string Language, bool Registered);

public sealed record Department(DepartmentId Id, CompanyId Company, string Name, WorkforceRole BudgetHolder);

public sealed record WorkforceAgent(AgentId Id, DepartmentId Department, string Name, WorkforceRole Role);

/// <summary>
/// What one agent may ask of one capability class: its declared quality floor, its cost ceiling
/// and its criticality. Read by the resolution boundary when a request is assembled.
/// </summary>
public sealed record AgentCapability(
    AgentId Agent,
    CapabilityClass Capability,
    QualityRating QualityFloor,
    Money CostCeiling,
    Criticality Criticality);

public enum ProviderAccountStatus
{
    Active = 1,
    Disabled = 2,
    Revoked = 3,
}

/// <summary>
/// A provider account held under commercial terms. One production account per provider
/// (constraint C-002); the credential-holder key is the pair of provider account and channel and
/// is unique, so a secret reference cannot be held by a second holder and sharing is
/// unrepresentable rather than discouraged.
/// </summary>
public sealed record ProviderAccount(
    ProviderAccountId Id,
    string Provider,
    string CommercialTermsBasis,
    DateOnly VerifiedOn,
    ProviderAccountStatus Status);

public sealed record Model(
    ModelId Id,
    ProviderAccountId ProviderAccount,
    QualityRating RatedQuality,
    ContextCapacity ContextCapacity,
    string Modality);

public enum PriceUnitKind
{
    InputUnit = 1,
    OutputUnit = 2,
    CachedUnit = 3,
    PerOperation = 4,
    PerSecond = 5,
}

/// <summary>
/// A price row over a validity interval. Prices are temporal and never updated in place
/// (decision D-010), so a superseded row stays retrievable and the cost that referenced it stays
/// re-derivable (risk R-006, acceptance criterion AC-021).
///
/// Every price the company holds is an ESTIMATE over unit prices no first-hand verification has
/// confirmed. Risk RK-002 requires a re-fetch immediately before any spend, and this wave commits
/// none.
/// </summary>
public sealed record ModelPrice(
    ModelPriceId Id,
    ModelId Model,
    PriceUnitKind UnitKind,
    decimal UnitPrice,
    string Currency,
    string Source,
    DateOnly VerifiedOn,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo)
{
    /// <summary>
    /// The date this price was re-fetched from its source (decision D-010, criterion A-017).
    ///
    /// Nullable, and deliberately so: a price lacking a re-fetch date resolves as ABSENT rather
    /// than as current, and the absence must be representable in order to be refused. Making it
    /// required at construction would hide the very condition the criterion measures.
    /// </summary>
    public DateOnly? RefetchedOn { get; init; }

    /// <summary>
    /// Whether this price may be applied to an operation that occurred on a given date. A price is
    /// current only when it was re-fetched no earlier than the operation it prices; an unverified
    /// price cannot silently enter the ledger.
    /// </summary>
    public bool IsApplicableTo(DateOnly operationOn) =>
        RefetchedOn is not null && RefetchedOn.Value >= operationOn;
}
