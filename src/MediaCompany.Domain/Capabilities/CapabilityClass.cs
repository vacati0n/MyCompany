namespace MediaCompany.Domain.Capabilities;

/// <summary>
/// The nine capability classes the company consumes, recorded at design fact F-004.
/// The set is closed: a capability outside it cannot be requested.
/// </summary>
public enum CapabilityClass
{
    EditorialReasoning = 1,
    HighStakesReview = 2,
    BulkClassification = 3,
    Narration = 4,
    StillImages = 5,
    GeneratedCutaways = 6,
    StockFootage = 7,
    MusicAndSoundEffects = 8,
    WebSearch = 9,
}

/// <summary>Reasoning depth a request asks for. Ordered.</summary>
public enum ReasoningTier
{
    Light = 1,
    Standard = 2,
    Deep = 3,
}

/// <summary>Route precedence. Ordering is the resolution order of design step 5.</summary>
public enum RouteTier
{
    Primary = 1,
    Secondary = 2,
    Emergency = 3,
}

/// <summary>How badly the work is needed when no route at or above the floor survives.</summary>
public enum Criticality
{
    Routine = 1,
    Critical = 2,
}
