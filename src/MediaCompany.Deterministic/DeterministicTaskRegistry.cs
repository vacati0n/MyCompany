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

    // The publishing path's rule-determined work. Each joins the SAME named set rather than
    // forming a second one, so the delivered boundary check covers them without a second
    // mechanism: the property is a dependency direction of the enclosing assembly, and a member
    // added here inherits it by construction.
    //
    // The idempotent dispatch is a member because deriving a key from an item and its version is
    // arithmetic over its inputs. The EFFECTING of a dispatch is not a member, because no such
    // step exists anywhere in the build to register.
    public const string PublishedFacingSurfaceProduction = "published-facing-surface-production";
    public const string PublicationTimingArithmetic = "publication-timing-arithmetic";
    public const string DispatchKeyDerivation = "dispatch-key-derivation";
    public const string DispatchDescriptorComposition = "dispatch-descriptor-composition";
    public const string FirstPublicationConditionEvaluation = "first-publication-condition-evaluation";
    public const string ApprovalQuantitySeparation = "approval-quantity-separation";
    public const string AttemptAnswerCompleteness = "attempt-answer-completeness";

    // The AI-economics change (decisions D-003 and D-004 of its design). The cost controller and the
    // evidence selection join the SAME named set, so the delivered dependency-direction proof covers
    // them: each is a pure rule over its inputs and cannot express a model call.
    public const string CostControl = "cost-control";
    public const string EvidenceSelection = "evidence-selection";

    // The AI-management change (decisions D-004, D-005, D-009, D-011 and D-012 of its design). The comparable-run
    // count, the report composition, the rule evaluation, the brief composition and the dashboard composition
    // join the SAME named set: each is a pure rule over the values of one read, so the delivered
    // dependency-direction proof covers them and none can express a model call, a metered call or a write.
    public const string ComparableRunCount = "comparable-run-count";
    public const string ReportComposition = "report-composition";
    public const string RuleEvaluation = "rule-evaluation";
    public const string BriefComposition = "brief-composition";
    public const string DashboardComposition = "dashboard-composition";

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
        PublishedFacingSurfaceProduction,
        PublicationTimingArithmetic,
        DispatchKeyDerivation,
        DispatchDescriptorComposition,
        FirstPublicationConditionEvaluation,
        ApprovalQuantitySeparation,
        AttemptAnswerCompleteness,
        CostControl,
        EvidenceSelection,
        ComparableRunCount,
        ReportComposition,
        RuleEvaluation,
        BriefComposition,
        DashboardComposition,
    }.ToFrozenSet(StringComparer.Ordinal);

    // There is deliberately no second list of the publishing members here. A list restating
    // what Names already holds can only be compared against itself: a step absent from both
    // would satisfy the comparison, which is the one case the check exists to catch. The
    // build-time check instead discovers the steps from the types that realize them and
    // asserts each declared name is in Names above.

    public static bool Contains(string taskName) => Names.Contains(taskName);
}
