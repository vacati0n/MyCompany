using System.Collections.Frozen;

namespace MediaCompany.Deterministic;

/// <summary>
/// The named set of work whose output is fully determined by its inputs and a rule, and which
/// therefore carries zero AI cost (constraint C-009, acceptance criterion AC-017). The set is the
/// one carried from the upstream recommendation, restricted to what this wave delivers.
///
/// The property is structural, not asserted. This assembly holds no dependency on the capability
/// resolution boundary, on the provider adapter set, or on the credential broker, so a model call
/// is not expressible from any member of this set. The dependency direction is verified by the
/// boundary test in the architecture test project (decision D-005, sequencing constraint P-007).
/// </summary>
public static class DeterministicTaskRegistry
{
    // Media and timing operations.
    public const string Render = "render";
    public const string Mux = "mux";
    public const string Encode = "encode";
    public const string Transcode = "transcode";
    public const string LoudnessNormalisation = "loudness-normalisation";
    public const string AspectConform = "aspect-conform";
    public const string SubtitleTimingForcedAlignment = "subtitle-timing-forced-alignment";
    public const string CaptionEmission = "caption-emission";
    public const string ChapterTimestampArithmetic = "chapter-timestamp-arithmetic";

    // Rights and registration.
    public const string AssetRightsLedger = "asset-rights-ledger";
    public const string LicenceJoinProof = "licence-join-proof";
    public const string LibraryRegistrationStateMachine = "library-registration-state-machine";

    // Gate and approval.
    public const string GateBlocking = "gate-blocking";
    public const string GateRelease = "gate-release";
    public const string ApprovalTokenVerification = "approval-token-verification";

    // Scheduling and accounting.
    public const string ScheduleArithmetic = "schedule-arithmetic";
    public const string CostMetering = "cost-metering";
    public const string PerItemRollup = "per-item-rollup";
    public const string HeadroomCalculation = "headroom-calculation";

    // Routing and resilience.
    public const string ProviderRouting = "provider-routing";
    public const string QuotaAndSpendAccounting = "quota-and-spend-accounting";
    public const string FailoverSelection = "failover-selection";
    public const string RetryAndBackoff = "retry-and-backoff";
    public const string CircuitBreaking = "circuit-breaking";

    // Duplicate detection and metadata.
    public const string ExactDuplicateDetection = "exact-duplicate-detection";
    public const string PerceptualDuplicateDetection = "perceptual-duplicate-detection";
    public const string MetadataTemplatePopulation = "metadata-template-population";
    public const string ThumbnailVariantCompositing = "thumbnail-variant-compositing";

    // Record.
    public const string AuditLogging = "audit-logging";

    // The production path's rule-determined work (plan task T-026, sequencing constraint P-004).
    // Each is fully determined by its inputs and a rule, so each carries zero capability cost, and
    // the property is the same dependency-direction property the set above rests on rather than a
    // second claim about these members in particular.
    public const string TermFidelityComparison = "term-fidelity-comparison";
    public const string BarredTermMetadataScreen = "barred-term-metadata-screen";
    public const string StageCompletenessCheck = "stage-completeness-check";
    public const string PublishReadyPrecondition = "publish-ready-precondition";
    public const string TreatmentConditionEvaluation = "treatment-condition-evaluation";
    public const string ComplianceDetermination = "compliance-determination";
    public const string AdvertiserSuitabilityDerivation = "advertiser-suitability-derivation";
    public const string ClaimToSourceJoin = "claim-to-source-join";
    public const string FootageRemovalAssessment = "footage-removal-assessment";
    public const string ClipOriginPrecondition = "clip-origin-precondition";
    public const string RuntimeMeasurement = "runtime-measurement";
    public const string ItemLedgerArithmetic = "item-ledger-arithmetic";

    /// <summary>
    /// The complete named set. The idempotent upload the upstream recommendation also names is
    /// excluded here by constraint C-013: this wave publishes nothing, so no upload path exists.
    /// </summary>
    public static readonly FrozenSet<string> Names = new[]
    {
        Render,
        Mux,
        Encode,
        Transcode,
        LoudnessNormalisation,
        AspectConform,
        SubtitleTimingForcedAlignment,
        CaptionEmission,
        ChapterTimestampArithmetic,
        AssetRightsLedger,
        LicenceJoinProof,
        LibraryRegistrationStateMachine,
        GateBlocking,
        GateRelease,
        ApprovalTokenVerification,
        ScheduleArithmetic,
        CostMetering,
        PerItemRollup,
        HeadroomCalculation,
        ProviderRouting,
        QuotaAndSpendAccounting,
        FailoverSelection,
        RetryAndBackoff,
        CircuitBreaking,
        ExactDuplicateDetection,
        PerceptualDuplicateDetection,
        MetadataTemplatePopulation,
        ThumbnailVariantCompositing,
        AuditLogging,
        TermFidelityComparison,
        BarredTermMetadataScreen,
        StageCompletenessCheck,
        PublishReadyPrecondition,
        TreatmentConditionEvaluation,
        ComplianceDetermination,
        AdvertiserSuitabilityDerivation,
        ClaimToSourceJoin,
        FootageRemovalAssessment,
        ClipOriginPrecondition,
        RuntimeMeasurement,
        ItemLedgerArithmetic,
    }.ToFrozenSet(StringComparer.Ordinal);

    public static bool Contains(string taskName) => Names.Contains(taskName);
}
