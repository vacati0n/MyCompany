using MediaCompany.Domain;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;

namespace MediaCompany.Application.Production;

/// <summary>
/// The declared subject a stage operates on (decision D-005, criteria A-031 and A-032).
///
/// The subject reaches a stage ONLY as a parameter of this contract. A handler that held its
/// subject internally could not later be re-pointed by changing a value, so the parameter is what
/// makes re-pointing a configuration change rather than a rebuild — and it is why this type exists
/// separately from the configuration register that holds the current value.
/// </summary>
public sealed record DeclaredSubject
{
    public required string Subject { get; init; }
    public required string Pillar { get; init; }
    public required string Format { get; init; }

    /// <summary>The specific species, behaviours and habitats the subject's script will call for.</summary>
    public IReadOnlyList<string> SubjectTerms { get; init; } = [];

    /// <summary>The libraries the item's assets may come from.</summary>
    public IReadOnlyList<string> Libraries { get; init; } = [];
}

/// <summary>What a stage produced, and the evidence it wrote.</summary>
public sealed record StageResult
{
    public required ProductionStage Stage { get; init; }
    public required StageEvidence Evidence { get; init; }
}

/// <summary>
/// The stage handler contract (module M-021, decision D-014).
///
/// The twelve handlers are one component set behind one contract, so the declared-subject
/// parameter, the capability-request discipline and the evidence-write discipline are each stated
/// once rather than twelve times.
///
/// Two properties are structural rather than conventional. A handler reaches an external
/// capability only through the resolution boundary, which it cannot bypass because this assembly
/// holds no provider or library dependency; and a handler writes evidence only through the
/// dossier, which is why <see cref="HandleAsync"/> returns evidence rather than writing state of
/// its own.
/// </summary>
public interface IStageHandler
{
    /// <summary>The stage this handler serves. One handler per stage, and the set is closed.</summary>
    ProductionStage Stage { get; }

    /// <summary>
    /// Runs the stage against a subject the handler was not necessarily configured with. Accepting
    /// the subject here rather than reading it from configuration is the whole of criterion A-032.
    /// </summary>
    Task<StageResult> HandleAsync(
        ItemId item,
        ItemVersion version,
        DeclaredSubject subject,
        CancellationToken cancellationToken);
}

/// <summary>
/// The configuration keys the production path adds (decision D-005, criterion A-031).
///
/// The list is deliberately short and deliberately excludes every control. Gate states, refusals
/// and stage capabilities are absent from it, so a control cannot become a setting: there is no
/// key that reaches the publish-ready state, no key that reaches a refusal, and no key that
/// reaches a treatment condition. Re-pointing changes these values and nothing else.
/// </summary>
public static class ProductionConfigurationKeys
{
    public const string SubjectMatter = "production.subject-matter";
    public const string Pillars = "production.pillars";
    public const string TreatmentConditions = "production.treatment-conditions";
    public const string LibrarySet = "production.library-set";

    public static readonly IReadOnlySet<string> Admitted = new HashSet<string>(StringComparer.Ordinal)
    {
        SubjectMatter,
        Pillars,
        TreatmentConditions,
        LibrarySet,
    };

    /// <summary>
    /// The substrings no admitted production key may contain. This is the non-configurability
    /// property expressed as a rule that can be tested, rather than as a claim in a document: a key
    /// reaching a gate state, a refusal or a stage capability fails here.
    /// </summary>
    public static readonly IReadOnlyList<string> ForbiddenKeyFragments =
    [
        "gate",
        "refusal",
        "refuse",
        "approval",
        "publish-ready",
        "capability",
        "block",
    ];

    public static bool IsAdmitted(string key) => Admitted.Contains(key);

    /// <summary>Whether a proposed key would reach a control. Used by the re-pointing demonstration.</summary>
    public static bool ReachesAControl(string key) =>
        ForbiddenKeyFragments.Any(f => key.Contains(f, StringComparison.OrdinalIgnoreCase));
}
