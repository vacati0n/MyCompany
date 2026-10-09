namespace MediaCompany.Domain.Capabilities;

/// <summary>
/// The representative tasks the benchmark corpus is drawn from (the AI-economics change, decision
/// D-002 of its design): the six the master plan's benchmark section names, as a CLOSED set, so a
/// seventh is a code change a reviewer sees rather than a string a caller invents.
///
/// A task class REFINES a capability class rather than mapping to one: several of these tasks share
/// one capability class, so a capability class cannot key a comparison of one task. A request names
/// its task class, and evidence is compared by the pair of the candidate routes' capability class and
/// that task class. No relation table between the two sets exists, and none is invented here.
/// </summary>
public enum TaskClass
{
    /// <summary>Research synthesis over a fixed source set.</summary>
    ResearchSynthesis = 1,

    /// <summary>A script pass over a fixed research brief.</summary>
    ScriptPass = 2,

    /// <summary>A fact-check over a script with known planted errors.</summary>
    FactCheck = 3,

    /// <summary>The compliance gate pack over items with known policy issues.</summary>
    ComplianceGatePack = 4,

    /// <summary>The search-optimisation pack.</summary>
    SeoPack = 5,

    /// <summary>The shot list and scene brief.</summary>
    ShotListAndSceneBrief = 6,
}

/// <summary>
/// Where a resolved route stands against the reasoning tier the request asked for (decision D-005 of
/// the AI-economics design). Every resolved outcome carries one, and the decision record holds it.
/// </summary>
public enum TierOutcome
{
    /// <summary>The route states the requested tier.</summary>
    AtRequestedTier = 1,

    /// <summary>The route states a higher tier than requested, because no route at the requested tier survived.</summary>
    HigherTier = 2,

    /// <summary>The route states no tier, because no route stating the requested or a higher one survived.</summary>
    Untiered = 3,

    /// <summary>The route states a lower tier than requested, served ONLY as a recorded controller downgrade.</summary>
    Downgraded = 4,
}

/// <summary>
/// Whether a selection rests on evidence or on the configured ordering (decision D-003 of the
/// AI-economics design). Every resolved outcome says which.
/// </summary>
public enum SelectionBasis
{
    /// <summary>Every compared quantity of every candidate was observed in one unit, and the cheapest candidate meeting the floor was taken.</summary>
    Evidence = 1,

    /// <summary>At least one compared quantity was not observed in one unit, so the delivered route-tier ordering was taken exactly.</summary>
    Configured = 2,
}
