using MediaCompany.Deterministic;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Rights;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// Demonstrations for the twelve-stage production path, its terminal state and the blocking
/// conditions that guard it.
///
/// Each test exhibits a refusal rather than asserting a property in prose. That is the shape the
/// design's test strategy asks for: one demonstration per named condition, because the acceptance
/// evidence for this change is largely refusal demonstrations and a composite verdict cannot be
/// designed against.
/// </summary>
public sealed class ProductionPathTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly ChannelId Channel = ChannelId.New();
    private const string Library = "Storyblocks";

    // ------------------------------------------------------------------
    // The twelve-stage path and its terminal state
    // ------------------------------------------------------------------

    [Fact]
    public void ThePathCarriesExactlyTwelveStages()
    {
        Assert.Equal(12, ProductionStageSet.Count);
        Assert.Equal(12, ProductionStageSet.All.Count);
        Assert.Equal(12, ProductionStageSet.All.Distinct().Count());
        Assert.Equal(ProductionStageSet.All.Count, Enum.GetValues<ProductionStage>().Length);
    }

    [Fact]
    public void PublishReadyIsTerminalWithNoTransitionOutOfIt()
    {
        Assert.Empty(GateTransitionTable.From(GateState.PublishReady));
    }

    [Fact]
    public void NoTransitionLeadsFromPublishReadyToPublished()
    {
        Assert.False(GateTransitionTable.IsAllowed(GateState.PublishReady, GateState.Published));
        Assert.False(GateTransitionTable.IsAllowed(GateState.PublishReady, GateState.Approved));
    }

    [Fact]
    public void AddingPublishReadyLeavesEveryPathToPublishedPassingOwnerApproval()
    {
        Assert.True(MediaCompany.Deterministic.Publication.GatePredicates.EveryPathPassesOwnerApproval());
    }

    [Fact]
    public void ACompleteDossierReachesPublishReady()
    {
        var verdict = Evaluate(CompleteDossier());

        Assert.True(verdict.Passed, Describe(verdict));
        Assert.Empty(verdict.Refusals);
    }

    // ------------------------------------------------------------------
    // A-001 — a stage without a recorded outcome is a refusal, not a gap
    // ------------------------------------------------------------------

    [Fact]
    public void AMissingStageOutcomeRefusesPublishReadyAndNamesTheStage()
    {
        var dossier = CompleteDossier() with
        {
            Stages = CompleteDossier().Stages.Where(s => s.Stage != ProductionStage.Audio).ToArray(),
        };

        var verdict = Evaluate(dossier);

        Assert.False(verdict.Passed);
        var refusal = Assert.Single(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.StageOutcomeMissing);
        Assert.Contains("Audio", refusal.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void AStageRecordedAsFailedRefusesPublishReady()
    {
        var stages = CompleteDossier().Stages
            .Select(s => s.Stage == ProductionStage.QualityCheck ? s with { Outcome = StageOutcome.Failed } : s)
            .ToArray();

        var verdict = Evaluate(CompleteDossier() with { Stages = stages });

        Assert.Contains(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.StageOutcomeMissing);
    }

    // ------------------------------------------------------------------
    // A-004, A-005 — the nine treatment conditions and the audience designation
    // ------------------------------------------------------------------

    [Fact]
    public void AllNineTreatmentConditionsAreModelled()
    {
        Assert.Equal(9, Enum.GetValues<TreatmentCondition>().Length);
    }

    [Fact]
    public void ADeliberatelyViolatedTreatmentConditionRefusesAndNamesTheCondition()
    {
        var treatment = CompleteDossier().Treatment
            .Select(t => t.Condition == TreatmentCondition.NoAnthropomorphism
                ? t with { Kind = TreatmentVerdictKind.Failed }
                : t)
            .ToArray();

        var verdict = Evaluate(CompleteDossier() with { Treatment = treatment });

        Assert.False(verdict.Passed);
        var refusal = Assert.Single(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.TreatmentConditionFailed);
        Assert.Contains("NoAnthropomorphism", refusal.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// K-8 is observable only after publication, so it resolves as a standing obligation naming the
    /// parameter it waits on. That resolution does not block; a standing obligation naming NOTHING
    /// does, which is what keeps it from becoming a way of passing the other eight.
    /// </summary>
    [Fact]
    public void TheAudienceShareConditionResolvesAsAStandingObligationWithoutBlocking()
    {
        var verdict = new TreatmentVerdict
        {
            Condition = TreatmentCondition.AudienceShareMonitoring,
            Kind = TreatmentVerdictKind.StandingObligation,
            Evidence = "Observable only after publication.",
            AwaitedParameter = "Studio age-demographic report, from month one.",
            RecordedAt = Now,
        };

        Assert.False(verdict.Blocks);
    }

    [Fact]
    public void AStandingObligationNamingNoParameterStillBlocks()
    {
        var verdict = new TreatmentVerdict
        {
            Condition = TreatmentCondition.AudienceShareMonitoring,
            Kind = TreatmentVerdictKind.StandingObligation,
            Evidence = "Observable only after publication.",
            AwaitedParameter = null,
            RecordedAt = Now,
        };

        Assert.True(verdict.Blocks);
    }

    [Fact]
    public void AStandingObligationOnAnyOtherConditionBlocks()
    {
        var verdict = new TreatmentVerdict
        {
            Condition = TreatmentCondition.ScientificRegister,
            Kind = TreatmentVerdictKind.StandingObligation,
            Evidence = "Deferred.",
            AwaitedParameter = "something",
            RecordedAt = Now,
        };

        Assert.True(verdict.Blocks);
    }

    [Fact]
    public void AnAudienceDesignationWithNoReasoningRefusesPublishReady()
    {
        var dossier = CompleteDossier() with
        {
            Audience = new AudienceDesignation { MadeForKids = false, Reasoning = "   ", RecordedAt = Now },
        };

        Assert.Contains(Evaluate(dossier).Refusals, r => r.Refusal == PublishReadyRefusal.AudienceDesignationInvalid);
    }

    [Fact]
    public void AnAudienceDesignationTakenFromAChannelDefaultRefusesPublishReady()
    {
        var dossier = CompleteDossier() with
        {
            Audience = new AudienceDesignation
            {
                MadeForKids = false,
                Reasoning = "Channel default.",
                FromChannelDefault = true,
                RecordedAt = Now,
            },
        };

        Assert.Contains(Evaluate(dossier).Refusals, r => r.Refusal == PublishReadyRefusal.AudienceDesignationInvalid);
    }

    // ------------------------------------------------------------------
    // A-006 — the barred-term metadata screen, in all three surfaces
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("A video for kids about bees", "A documentary.", "apiology")]
    [InlineData("The waggle dance", "Cute bees doing a dance.", "apiology")]
    [InlineData("The waggle dance", "A documentary.", "funny")]
    public void ABarredTermInAnySurfaceIsRefusedWithTheTermNamed(string title, string description, string tag)
    {
        var metadata = new ItemMetadata { Title = title, Description = description, Tags = [tag] };

        var hits = BarredTermScreen.Screen(metadata);

        Assert.NotEmpty(hits);
        Assert.False(BarredTermScreen.Passes(metadata));
        Assert.Contains(hits.First().Term, BarredTermScreen.DescribeRefusal(hits), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheScreenMatchesOnAWordBoundarySoAnUnrelatedWordDoesNotFire()
    {
        // "kids" must not fire inside "kidskin". A screen that cries wolf gets disabled.
        var metadata = new ItemMetadata { Title = "Kidskin and cuticle", Description = "A documentary.", Tags = [] };

        Assert.True(BarredTermScreen.Passes(metadata));
    }

    [Fact]
    public void TheItemsOwnMetadataPassesTheScreen()
    {
        Assert.True(BarredTermScreen.Passes(ItemMetadataUnderTest()));
    }

    // ------------------------------------------------------------------
    // A-024 to A-026 — the supply audit and its term-fidelity guard
    // ------------------------------------------------------------------

    /// <summary>
    /// The recorded proof case: the library answers `saola` with "181 results found for saona", a
    /// Caribbean island. The true count is zero, so the reported total is no count at all.
    /// </summary>
    [Fact]
    public void ATermTheLibrarySubstitutedResolvesAsNoCount()
    {
        var fidelity = TermFidelityGuard.Classify("saola", "saona");

        Assert.Equal(TermFidelity.SpellingCorrected, fidelity);
        Assert.False(TermFidelityGuard.IsAdmissibleAsCount(fidelity));
    }

    [Fact]
    public void ATermTheLibraryReplacedWithADistantWordResolvesAsSubstituted()
    {
        var fidelity = TermFidelityGuard.Classify("binturong", "hurricane");

        Assert.Equal(TermFidelity.Substituted, fidelity);
        Assert.False(TermFidelityGuard.IsAdmissibleAsCount(fidelity));
    }

    /// <summary>
    /// A multi-word phrase is inadmissible on its own shape. `xyzzy monkey` returning 26 results
    /// proves the query is a relevance blend rather than an intersection, so even an exact echo of
    /// the phrase does not make the total a count.
    /// </summary>
    [Theory]
    [InlineData("waggle dance")]
    [InlineData("honey bee")]
    [InlineData("proboscis monkey")]
    public void AMultiWordTermIsInadmissibleEvenWhenEchoedBackExactly(string term)
    {
        var fidelity = TermFidelityGuard.Classify(term, term);

        Assert.Equal(TermFidelity.MultiWordPhrase, fidelity);
        Assert.False(TermFidelityGuard.IsAdmissibleAsCount(fidelity));
    }

    [Fact]
    public void ASingleTokenTheLibraryEchoedBackIsTheOnlyAdmissibleCount()
    {
        var fidelity = TermFidelityGuard.Classify("pangolin", "pangolin");

        Assert.Equal(TermFidelity.LiteralSingleToken, fidelity);
        Assert.True(TermFidelityGuard.IsAdmissibleAsCount(fidelity));
    }

    [Fact]
    public void AnUnreachableLibrarySurfaceResolvesAsUnknownRatherThanSubstituted()
    {
        Assert.Equal(TermFidelity.Unknown, TermFidelityGuard.Classify("pangolin", null));
    }

    [Fact]
    public void AMultiWordEntryCarryingATotalStillHasNoAdmissibleCount()
    {
        var entry = new SupplyAuditEntry
        {
            RequestedTerm = "honey bee",
            Library = Library,
            Fidelity = TermFidelity.MultiWordPhrase,
            TermAnswered = "honey bee",
            Count = 1213,
            AuditedAt = Now,
        };

        Assert.False(entry.HasAdmissibleCount);
    }

    [Fact]
    public void AnUnobtainedCountMustNameTheReasonAndTheRemedy()
    {
        Assert.Throws<ArgumentException>(() => TermFidelityGuard.NotObtained(
            "waggle dance", Library, TermFidelity.MultiWordPhrase,
            CountUnobtainedReason.None, "something", Now));

        Assert.Throws<ArgumentException>(() => TermFidelityGuard.NotObtained(
            "waggle dance", Library, TermFidelity.MultiWordPhrase,
            CountUnobtainedReason.RequiresPerClipConfirmation, "  ", Now));
    }

    [Fact]
    public void AnUnobtainedCountThatNamesBothIsAResolvedEntry()
    {
        var entry = TermFidelityGuard.NotObtained(
            "waggle dance",
            Library,
            TermFidelity.MultiWordPhrase,
            CountUnobtainedReason.RequiresPerClipConfirmation,
            "An authenticated subscription session, confirming each candidate clip by eye.",
            Now);

        Assert.True(entry.IsResolved);
        Assert.False(entry.HasAdmissibleCount);
        Assert.Null(entry.Count);
    }

    /// <summary>
    /// An absent count and a count of zero are different facts. A library that answered literally
    /// and reported nothing HAS counted; a library that was never reached has not.
    /// </summary>
    [Fact]
    public void AnAbsentCountIsNotTheSameFactAsACountOfZero()
    {
        var zero = new SupplyAuditEntry
        {
            RequestedTerm = "saola",
            Library = Library,
            Fidelity = TermFidelity.LiteralSingleToken,
            TermAnswered = "saola",
            Count = 0,
            AuditedAt = Now,
        };

        var absent = TermFidelityGuard.NotObtained(
            "saola", Library, TermFidelity.Unknown,
            CountUnobtainedReason.LibrarySurfaceUnreachable,
            "A reachable library search surface.", Now);

        Assert.True(zero.HasAdmissibleCount);
        Assert.False(absent.HasAdmissibleCount);
        Assert.NotEqual(zero.Count, absent.Count);
    }

    /// <summary>
    /// A prior observation that was itself inadmissible cannot be laundered into evidence by being
    /// old. It is carried with its date and its admissibility, and the guard does not read it.
    /// </summary>
    [Fact]
    public void AnInadmissiblePriorObservationStaysInadmissible()
    {
        var prior = new PriorObservation
        {
            ReportedTotal = 1213,
            ObservedOn = new DateOnly(2026, 9, 26),
            Source = "animal-niche-analysis.md section 3.3",
            FidelityAtObservation = TermFidelity.MultiWordPhrase,
        };

        var entry = TermFidelityGuard.NotObtained(
            "honey bee", Library, TermFidelity.MultiWordPhrase,
            CountUnobtainedReason.TermNotAnsweredLiterally,
            "A literal single-token query inside an authenticated subscription session.",
            Now, prior: prior);

        Assert.False(prior.WasAdmissible);
        Assert.False(entry.HasAdmissibleCount);
        Assert.True(entry.IsResolved);
    }

    [Fact]
    public void AnUnresolvedSupplyEntryRefusesPublishReady()
    {
        var dossier = CompleteDossier() with
        {
            SupplyAudit =
            [
                new SupplyAuditEntry
                {
                    RequestedTerm = "orphan subject",
                    Library = Library,
                    Fidelity = TermFidelity.MultiWordPhrase,
                    AuditedAt = Now.AddDays(-1),
                },
            ],
        };

        Assert.Contains(Evaluate(dossier).Refusals, r => r.Refusal == PublishReadyRefusal.SupplyAuditIncomplete);
    }

    /// <summary>
    /// The ordering obligation. The criterion compares recorded dates, so an audit made after the
    /// commit is unsatisfiable for the item it concerns and cannot be repaired afterwards.
    /// </summary>
    [Fact]
    public void ASupplyAuditRecordedAfterTheScriptCommitRefusesPublishReady()
    {
        var committed = Now.AddDays(-2);
        var dossier = CompleteDossier() with
        {
            SupplyAudit = CompleteDossier().SupplyAudit
                .Select(e => e with { AuditedAt = committed.AddHours(1) })
                .ToArray(),
        };

        var verdict = PublishReadyPredicate.Evaluate(
            dossier, GateState.AwaitingRightsCheck, Registrations(), Channel, committed);

        Assert.Contains(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.SupplyAuditAfterScriptCommit);
    }

    // ------------------------------------------------------------------
    // A-027, A-028 — the clip-origin assessment as a precondition of use
    // ------------------------------------------------------------------

    [Fact]
    public void AClipAssessedAfterItWasUsedRefusesPublishReady()
    {
        var dossier = CompleteDossier() with
        {
            ClipOrigins =
            [
                new ClipOriginAssessment
                {
                    ClipReference = "SB-00042",
                    Origin = ClipOrigin.RecordedFootage,
                    Basis = "Field footage, apiary, named contributor.",
                    AssessedAt = Now,
                    UsedAt = Now.AddHours(-1),
                },
            ],
        };

        var verdict = Evaluate(dossier);

        Assert.Contains(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.ClipOriginAssessedAfterUse);
    }

    [Fact]
    public void AnUnassessedClipThatEnteredTheCutRefusesPublishReady()
    {
        var dossier = CompleteDossier() with
        {
            ClipOrigins =
            [
                new ClipOriginAssessment
                {
                    ClipReference = "SB-00043",
                    Origin = ClipOrigin.Unassessed,
                    Basis = "not assessed",
                    AssessedAt = Now.AddHours(-2),
                    UsedAt = Now.AddHours(-1),
                },
            ],
        };

        Assert.Contains(Evaluate(dossier).Refusals, r => r.Refusal == PublishReadyRefusal.ClipUnassessed);
    }

    [Theory]
    [InlineData(ClipOrigin.GeneratedOrComposited)]
    [InlineData(ClipOrigin.NameCollision)]
    [InlineData(ClipOrigin.Indeterminate)]
    [InlineData(ClipOrigin.Unassessed)]
    public void OnlyAClipEstablishedAsRecordedFootageIsAdmissibleForACut(ClipOrigin origin)
    {
        var clip = new ClipOriginAssessment
        {
            ClipReference = "SB-00044",
            Origin = origin,
            Basis = "assessed",
            AssessedAt = Now.AddHours(-2),
        };

        Assert.False(clip.AdmissibleForCut);
    }

    [Fact]
    public void AClipAssessedAsRecordedFootageBeforeUseIsAdmissible()
    {
        var clip = new ClipOriginAssessment
        {
            ClipReference = "SB-00045",
            Origin = ClipOrigin.RecordedFootage,
            Basis = "Field footage of a marked forager on a vertical comb.",
            AssessedAt = Now.AddHours(-2),
            UsedAt = Now.AddHours(-1),
        };

        Assert.True(clip.AdmissibleForCut);
    }

    // ------------------------------------------------------------------
    // A-007 to A-009 — admissible visual sources and the cutaway reason
    // ------------------------------------------------------------------

    [Fact]
    public void AVisualResolvingToNeitherAdmissibleSourceCannotBeUsed()
    {
        var visual = new VisualProvenance
        {
            VisualReference = "third-party clip",
            Source = VisualSource.Inadmissible,
            RecordedAt = Now,
        };

        Assert.False(visual.IsAdmissible);
    }

    [Fact]
    public void ALicensedStockVisualWithoutALicenceReferenceIsInadmissible()
    {
        var visual = new VisualProvenance
        {
            VisualReference = "SB-00046",
            Source = VisualSource.LicensedStock,
            Asset = AssetId.New(),
            LicenceReference = null,
            RecordedAt = Now,
        };

        Assert.False(visual.IsAdmissible);
    }

    [Fact]
    public void AGeneratedCutawayWithNoRecordedReasonIsRefusedByName()
    {
        var dossier = CompleteDossier() with
        {
            Visuals =
            [
                new VisualProvenance
                {
                    VisualReference = "cutaway-1",
                    Source = VisualSource.GeneratedCutaway,
                    CutawayReason = null,
                    RecordedAt = Now,
                },
            ],
        };

        Assert.Contains(
            Evaluate(dossier).Refusals,
            r => r.Refusal == PublishReadyRefusal.GeneratedCutawayLacksRecordedReason);
    }

    [Fact]
    public void AGeneratedCutawayReasonNamingNoAlternativesIsIncomplete()
    {
        var reason = new GeneratedCutawayReason
        {
            WhyNoLicensedAssetServed = "Nothing in the library showed it.",
            AlternativesConsidered = [],
            RecordedAt = Now,
        };

        Assert.False(reason.IsComplete);
    }

    // ------------------------------------------------------------------
    // A-010, A-011 — attribution and the footage-removal originality standard
    // ------------------------------------------------------------------

    [Fact]
    public void AnUnattributedClaimRefusesPublishReady()
    {
        var dossier = CompleteDossier() with
        {
            Claims =
            [
                new ClaimAttribution { ClaimId = "CL-99", Claim = "Bees do a thing.", Source = "", RecordedAt = Now },
            ],
        };

        Assert.Contains(Evaluate(dossier).Refusals, r => r.Refusal == PublishReadyRefusal.ClaimUnattributed);
    }

    [Fact]
    public void AnArgumentThatDoesNotSurviveFootageRemovalRefusesPublishReady()
    {
        var dossier = CompleteDossier() with
        {
            Originality = new FootageRemovalAssessment
            {
                ClaimsTotal = 10,
                ClaimsCarriedWithoutFootage = 7,
                Judgement = "Three claims rest on the footage alone.",
                AssessedAt = Now,
            },
        };

        Assert.Contains(
            Evaluate(dossier).Refusals,
            r => r.Refusal == PublishReadyRefusal.ArgumentDoesNotSurviveFootageRemoval);
    }

    [Fact]
    public void AnArgumentCarriedEntirelyByScriptAndGraphicsSurvives()
    {
        var assessment = new FootageRemovalAssessment
        {
            ClaimsTotal = 10,
            ClaimsCarriedWithoutFootage = 10,
            Judgement = "Every claim is carried by the narration and the original graphics.",
            AssessedAt = Now,
        };

        Assert.True(assessment.ArgumentSurvives);
    }

    // ------------------------------------------------------------------
    // A-019 to A-023 — the five determinations
    // ------------------------------------------------------------------

    [Fact]
    public void ThereAreExactlyFiveDeterminations()
    {
        Assert.Equal(5, Enum.GetValues<ComplianceDetermination>().Length);
    }

    /// <summary>
    /// The determination trichotomy. Only the unresolved case refuses; a determination recorded as
    /// not evidenceable, with its reason and its remedy, satisfies the determination. That is the
    /// whole point of the third resolution.
    /// </summary>
    [Fact]
    public void ADeterminationRecordedAsNotEvidenceableDoesNotBlock()
    {
        var resolution = Determinations.NotEvidenceable(
            ComplianceDetermination.AdvertiserSuitability,
            "The self-certification questionnaire exists only in the upload workflow.",
            "Completing the questionnaire at upload, which this change does not reach.",
            Now);

        Assert.True(resolution.IsWellFormed);
        Assert.False(resolution.Blocks);
    }

    [Fact]
    public void ADeliberatelyUnresolvedDeterminationRefusesPublishReadyByName()
    {
        var dossier = CompleteDossier() with
        {
            Determinations = CompleteDossier().Determinations
                .Where(d => d.Determination != ComplianceDetermination.DuplicateDetection)
                .ToArray(),
        };

        var verdict = Evaluate(dossier);

        var refusal = Assert.Single(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.DeterminationUnresolved);
        Assert.Contains("DuplicateDetection", refusal.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void ANotEvidenceableResolutionMissingItsReasonOrRemedyIsRefused()
    {
        Assert.Throws<ArgumentException>(() => Determinations.NotEvidenceable(
            ComplianceDetermination.AdvertiserSuitability, "  ", "a remedy", Now));

        Assert.Throws<ArgumentException>(() => Determinations.NotEvidenceable(
            ComplianceDetermination.AdvertiserSuitability, "a reason", "  ", Now));
    }

    [Fact]
    public void AMalformedNotEvidenceableResolutionBlocks()
    {
        var malformed = new DeterminationResolution
        {
            Determination = ComplianceDetermination.AdvertiserSuitability,
            Outcome = DeterminationOutcome.RecordedNotEvidenceable,
            NotEvidenceableReason = "A reason.",
            WhatWouldMakeItEvidenceable = null,
            ResolvedAt = Now,
        };

        Assert.False(malformed.IsWellFormed);
        Assert.True(malformed.Blocks);
    }

    [Fact]
    public void AnEvidencedDeterminationRequiresItsEvidence()
    {
        Assert.Throws<ArgumentException>(() => Determinations.Evidenced(
            ComplianceDetermination.OriginalitySelfAssessment, "   ", Now));
    }

    // A-022 — the self-rating is derived, never defaulted.

    [Fact]
    public void ARatingWithNoClassificationBehindItResolvesAsAbsent()
    {
        Assert.Equal(AdSuitabilityRating.Absent, Determinations.DeriveRating([]));
    }

    [Fact]
    public void AFixedAnswerWithNoBasisProducesNoRating()
    {
        var fixedAnswers = Enum.GetValues<SuitabilityCategory>()
            .Select(c => new SuitabilityClassification { Category = c, Present = false, Basis = "" })
            .ToArray();

        Assert.Equal(AdSuitabilityRating.Absent, Determinations.DeriveRating(fixedAnswers));
    }

    [Fact]
    public void AClassificationOfAllFourteenCategoriesWithABasisProducesARating()
    {
        Assert.Equal(AdSuitabilityRating.FullAdRevenue, Determinations.DeriveRating(FullClassification()));
    }

    [Fact]
    public void OneCategoryLeftUnclassifiedCollapsesTheRatingToAbsent()
    {
        var partial = FullClassification().Where(c => c.Category != SuitabilityCategory.Violence).ToArray();

        Assert.Equal(AdSuitabilityRating.Absent, Determinations.DeriveRating(partial));
    }

    [Fact]
    public void ThereAreExactlyFourteenSuitabilityCategories()
    {
        Assert.Equal(14, Enum.GetValues<SuitabilityCategory>().Length);
    }

    // A-023 — the synthetic-media determination over the element set.

    [Fact]
    public void AnUnassessedElementPreventsTheSyntheticMediaDeterminationFromResolving()
    {
        var elements = new[]
        {
            new SyntheticElementAssessment
            {
                ElementReference = "narration",
                DisclosureRequired = false,
                Basis = "",
                AssessedAt = Now,
            },
        };

        var resolution = Determinations.ResolveSyntheticMedia(
            elements, Now, "synthetic content policy", new DateOnly(2026, 9, 18));

        Assert.Equal(DeterminationOutcome.Unresolved, resolution.Outcome);
        Assert.True(resolution.Blocks);
    }

    [Fact]
    public void AFullyAssessedElementSetResolvesTheSyntheticMediaDetermination()
    {
        var resolution = Determinations.ResolveSyntheticMedia(
            SyntheticElements(), Now, "synthetic content policy", new DateOnly(2026, 9, 18));

        Assert.Equal(DeterminationOutcome.Evidenced, resolution.Outcome);
        Assert.False(resolution.Blocks);
    }

    // A-021 — the duplicate result carries its distance.

    [Fact]
    public void ADuplicateResultStatedWithoutADistanceDoesNotResolve()
    {
        var result = new DuplicateResult
        {
            DuplicateAgainstOwnCatalogue = false,
            DuplicateAgainstExternalSources = false,
            Distance = null,
            Method = "perceptual hash",
            ComputedAt = Now,
        };

        Assert.Equal(DeterminationOutcome.Unresolved, Determinations.ResolveDuplicate(result, Now).Outcome);
    }

    [Fact]
    public void ADuplicateResultWithItsDistanceResolves()
    {
        Assert.Equal(DeterminationOutcome.Evidenced, Determinations.ResolveDuplicate(DuplicateUnderTest(), Now).Outcome);
    }

    // ------------------------------------------------------------------
    // A-029 — library registration, and A-003 — the runtime floor
    // ------------------------------------------------------------------

    [Fact]
    public void AnUnregisteredLibraryRefusesPublishReadyAndNamesTheLibrary()
    {
        var verdict = PublishReadyPredicate.Evaluate(
            CompleteDossier(), GateState.AwaitingRightsCheck, [], Channel, null);

        var refusal = Assert.Single(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.LibraryUnregistered);
        Assert.Contains(Library, refusal.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void ARuntimeBelowTheFloorRefusesPublishReady()
    {
        var dossier = CompleteDossier() with { Runtime = TimeSpan.FromMinutes(7) };

        Assert.Contains(Evaluate(dossier).Refusals, r => r.Refusal == PublishReadyRefusal.RuntimeBelowFloor);
    }

    [Fact]
    public void TheRuntimeFloorIsTenMinutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(10), PublishReadyPredicate.RuntimeFloor);
    }

    /// <summary>
    /// An unrecorded runtime is refused under its OWN name, not under the too-short name. The two
    /// conditions have different remedies — measure, against re-cut — so a record that conflated
    /// them would send a reader to the wrong repair.
    /// </summary>
    [Fact]
    public void AnUnrecordedRuntimeIsRefusedAsNotRecordedAndNotAsBelowTheFloor()
    {
        var dossier = CompleteDossier() with { Runtime = null };

        var refusals = Evaluate(dossier).Refusals;

        Assert.Contains(refusals, r => r.Refusal == PublishReadyRefusal.RuntimeNotRecorded);
        Assert.DoesNotContain(refusals, r => r.Refusal == PublishReadyRefusal.RuntimeBelowFloor);
    }

    /// <summary>The converse: a measured short runtime is refused as too short, never as unrecorded.</summary>
    [Fact]
    public void AShortRuntimeIsRefusedAsBelowTheFloorAndNotAsUnrecorded()
    {
        var dossier = CompleteDossier() with { Runtime = TimeSpan.FromMinutes(7) };

        var refusals = Evaluate(dossier).Refusals;

        Assert.Contains(refusals, r => r.Refusal == PublishReadyRefusal.RuntimeBelowFloor);
        Assert.DoesNotContain(refusals, r => r.Refusal == PublishReadyRefusal.RuntimeNotRecorded);
    }

    /// <summary>
    /// The two refusals carry different recorded reasons, so the record distinguishes them to a
    /// reader and not merely to a switch statement.
    /// </summary>
    [Fact]
    public void TheTwoRuntimeRefusalsCarryDifferentRecordedReasons()
    {
        var unrecorded = Evaluate(CompleteDossier() with { Runtime = null })
            .Refusals.Single(r => r.Refusal == PublishReadyRefusal.RuntimeNotRecorded).Detail;
        var tooShort = Evaluate(CompleteDossier() with { Runtime = TimeSpan.FromMinutes(7) })
            .Refusals.Single(r => r.Refusal == PublishReadyRefusal.RuntimeBelowFloor).Detail;

        Assert.NotEqual(unrecorded, tooShort);
        Assert.Contains("not recorded", unrecorded, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("below", tooShort, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("below", unrecorded, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The general property design constraint 014 states, asserted over the whole enum rather than
    /// over the one member that was found conflated: no two refusal members share a name, and the
    /// runtime pair in particular is two members rather than one.
    /// </summary>
    [Fact]
    public void EveryRefusalMemberIsDistinctAndTheRuntimeConditionsAreTwoOfThem()
    {
        var members = Enum.GetValues<PublishReadyRefusal>();

        Assert.Equal(members.Length, members.Distinct().Count());
        Assert.Equal(members.Length, members.Select(m => m.ToString()).Distinct().Count());
        Assert.Contains(PublishReadyRefusal.RuntimeBelowFloor, members);
        Assert.Contains(PublishReadyRefusal.RuntimeNotRecorded, members);
    }

    // ------------------------------------------------------------------
    // A-018, A-031 — the rule-determined set and non-configurability
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(DeterministicTaskRegistry.TermFidelityComparison)]
    [InlineData(DeterministicTaskRegistry.BarredTermMetadataScreen)]
    [InlineData(DeterministicTaskRegistry.StageCompletenessCheck)]
    [InlineData(DeterministicTaskRegistry.PublishReadyPrecondition)]
    [InlineData(DeterministicTaskRegistry.TreatmentConditionEvaluation)]
    [InlineData(DeterministicTaskRegistry.ComplianceDetermination)]
    [InlineData(DeterministicTaskRegistry.AdvertiserSuitabilityDerivation)]
    [InlineData(DeterministicTaskRegistry.ClaimToSourceJoin)]
    [InlineData(DeterministicTaskRegistry.FootageRemovalAssessment)]
    [InlineData(DeterministicTaskRegistry.ClipOriginPrecondition)]
    [InlineData(DeterministicTaskRegistry.RuntimeMeasurement)]
    [InlineData(DeterministicTaskRegistry.ItemLedgerArithmetic)]
    public void EveryDeterministicTaskThePathAddsIsNamedInTheRuleDeterminedSet(string taskName)
    {
        Assert.True(DeterministicTaskRegistry.Contains(taskName));
    }

    /// <summary>
    /// No configuration key the production path adds reaches a gate state, a refusal or a stage
    /// capability. This is the non-configurability property expressed as something testable rather
    /// than as a claim in a document.
    /// </summary>
    [Fact]
    public void NoProductionConfigurationKeyReachesAControl()
    {
        Assert.All(
            Application.Production.ProductionConfigurationKeys.Admitted,
            key => Assert.False(Application.Production.ProductionConfigurationKeys.ReachesAControl(key)));
    }

    [Fact]
    public void AKeyThatWouldReachAControlIsRecognisedAsSuch()
    {
        Assert.True(Application.Production.ProductionConfigurationKeys.ReachesAControl("production.gate-state"));
        Assert.True(Application.Production.ProductionConfigurationKeys.ReachesAControl("production.owner-approval"));
        Assert.False(Application.Production.ProductionConfigurationKeys.IsAdmitted("production.gate-state"));
    }

    // ------------------------------------------------------------------
    // Helpers — the complete dossier every refusal test degrades from
    // ------------------------------------------------------------------

    private static PublishReadyVerdict Evaluate(ItemDossier dossier) =>
        PublishReadyPredicate.Evaluate(dossier, GateState.AwaitingRightsCheck, Registrations(), Channel, null);

    private static string Describe(PublishReadyVerdict verdict) =>
        string.Join(" | ", verdict.Refusals.Select(r => r.Refusal + ": " + r.Detail));

    private static IReadOnlyList<LibraryRegistration> Registrations() =>
        [new LibraryRegistration(Channel, Library, new DateOnly(2026, 9, 26))];

    private static ItemMetadata ItemMetadataUnderTest() => new()
    {
        Title = "The Waggle Dance: How a Honey Bee Encodes a Vector",
        Description = "A mechanism explainer on Apis mellifera dance communication, after von Frisch.",
        Tags = ["apis mellifera", "waggle dance", "animal communication", "ethology", "von frisch"],
    };

    private static IReadOnlyList<SuitabilityClassification> FullClassification() =>
        Enum.GetValues<SuitabilityCategory>()
            .Select(c => new SuitabilityClassification
            {
                Category = c,
                Present = false,
                Basis = "Classified against the finished cut and the committed narration.",
            })
            .ToArray();

    private static IReadOnlyList<SyntheticElementAssessment> SyntheticElements() =>
    [
        new SyntheticElementAssessment
        {
            ElementReference = "narration",
            DisclosureRequired = false,
            Basis = "Synthesised voice reading an original script; makes no real person appear to say anything.",
            AssessedAt = Now,
        },
        new SyntheticElementAssessment
        {
            ElementReference = "motion graphics",
            DisclosureRequired = false,
            Basis = "Original data-visualisation artefacts; depict no real place, person or event.",
            AssessedAt = Now,
        },
    ];

    private static DuplicateResult DuplicateUnderTest() => new()
    {
        DuplicateAgainstOwnCatalogue = false,
        DuplicateAgainstExternalSources = false,
        Distance = 0.91,
        Method = "perceptual hash over keyframes, plus script shingling",
        ComputedAt = Now,
    };

    private static ItemDossier CompleteDossier()
    {
        var item = ItemId.New();

        return new ItemDossier
        {
            Item = item,
            Version = new ItemVersion(1),
            Runtime = TimeSpan.FromMinutes(12) + TimeSpan.FromSeconds(40),
            Metadata = ItemMetadataUnderTest(),
            Stages = ProductionStageSet.All
                .Select(s => new StageEvidence
                {
                    Stage = s,
                    Outcome = StageOutcome.Succeeded,
                    Summary = s + " complete.",
                    RecordedAt = Now.AddHours(-2),
                })
                .ToArray(),
            Treatment = Enum.GetValues<TreatmentCondition>()
                .Select(c => new TreatmentVerdict
                {
                    Condition = c,
                    Kind = c == TreatmentCondition.AudienceShareMonitoring
                        ? TreatmentVerdictKind.StandingObligation
                        : TreatmentVerdictKind.Passed,
                    Evidence = "Assessed against the finished cut.",
                    AwaitedParameter = c == TreatmentCondition.AudienceShareMonitoring
                        ? "Studio age-demographic report, from month one."
                        : null,
                    RecordedAt = Now.AddHours(-1),
                })
                .ToArray(),
            Audience = new AudienceDesignation
            {
                MadeForKids = false,
                Reasoning = "Adult general-science register; no characters, no play-acting, mortality treated factually.",
                RecordedAt = Now.AddHours(-1),
            },
            Visuals =
            [
                new VisualProvenance
                {
                    VisualReference = "GFX-01 angle-to-sun encoding",
                    Source = VisualSource.OriginalGraphic,
                    RecordedAt = Now.AddHours(-3),
                },
                new VisualProvenance
                {
                    VisualReference = "SB-00042 forager on comb",
                    Source = VisualSource.LicensedStock,
                    Asset = AssetId.New(),
                    LicenceReference = "SB-UAA-2026-0001",
                    RecordedAt = Now.AddHours(-3),
                },
            ],
            ClipOrigins =
            [
                new ClipOriginAssessment
                {
                    ClipReference = "SB-00042",
                    Origin = ClipOrigin.RecordedFootage,
                    Basis = "Field footage of a marked forager; no compositing or green-screen indicators.",
                    AssessedAt = Now.AddHours(-4),
                    UsedAt = Now.AddHours(-3),
                },
            ],
            Claims =
            [
                new ClaimAttribution
                {
                    ClaimId = "CL-01",
                    Claim = "The dance angle relative to vertical encodes the bearing to the resource relative to the sun.",
                    Source = "von Frisch, Nobel Prize in Physiology or Medicine 1973.",
                    RecordedAt = Now.AddHours(-5),
                },
            ],
            Originality = new FootageRemovalAssessment
            {
                ClaimsTotal = 1,
                ClaimsCarriedWithoutFootage = 1,
                Judgement = "Every claim is carried by the narration and the original graphics.",
                AssessedAt = Now.AddHours(-1),
            },
            SupplyAudit =
            [
                TermFidelityGuard.NotObtained(
                    "honey bee", Library, TermFidelity.MultiWordPhrase,
                    CountUnobtainedReason.TermNotAnsweredLiterally,
                    "A literal single-token query inside an authenticated subscription session.",
                    Now.AddDays(-1)),
            ],
            Determinations =
            [
                Determinations.Evidenced(
                    ComplianceDetermination.OriginalitySelfAssessment,
                    "Argument survives the removal of every licensed clip.",
                    Now),
                Determinations.ResolveDuplicate(DuplicateUnderTest(), Now),
                Determinations.ResolveSyntheticMedia(
                    SyntheticElements(), Now, "synthetic content policy", new DateOnly(2026, 9, 18)),
                Determinations.Evidenced(
                    ComplianceDetermination.AffiliateAndPaidPromotion,
                    "Neither an affiliate link nor a paid placement is present in this item.",
                    Now),
                Determinations.NotEvidenceable(
                    ComplianceDetermination.AdvertiserSuitability,
                    "The self-certification questionnaire exists only in the upload workflow.",
                    "Completing the questionnaire at upload, which this change does not reach.",
                    Now),
            ],
        };
    }
}
