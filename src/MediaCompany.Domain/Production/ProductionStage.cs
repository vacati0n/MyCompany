namespace MediaCompany.Domain.Production;

/// <summary>
/// The twelve stages of the production path (decision D-001, acceptance criterion A-001). The
/// path is a workflow definition over the delivered work lifecycle; these are its positions, and
/// the set is closed so that a stage outside it is not expressible rather than merely unexpected.
///
/// A stage without a recorded outcome refuses the terminal state. It is a refusal and not a gap
/// (constraint C-006), which is why <see cref="ProductionStageSet.All"/> is what the completeness
/// predicate iterates over, rather than whatever outcomes happen to have been written.
/// </summary>
public enum ProductionStage
{
    Idea = 1,
    IdeaScoring = 2,
    Research = 3,
    Script = 4,
    OriginalityCheck = 5,
    Design = 6,
    Production = 7,
    Audio = 8,
    Thumbnail = 9,
    QualityCheck = 10,
    CopyrightCheck = 11,
    PolicyCheck = 12,
}

/// <summary>The closed stage set, in the order the path requires.</summary>
public static class ProductionStageSet
{
    public const int Count = 12;

    public static readonly IReadOnlyList<ProductionStage> All =
    [
        ProductionStage.Idea,
        ProductionStage.IdeaScoring,
        ProductionStage.Research,
        ProductionStage.Script,
        ProductionStage.OriginalityCheck,
        ProductionStage.Design,
        ProductionStage.Production,
        ProductionStage.Audio,
        ProductionStage.Thumbnail,
        ProductionStage.QualityCheck,
        ProductionStage.CopyrightCheck,
        ProductionStage.PolicyCheck,
    ];
}
