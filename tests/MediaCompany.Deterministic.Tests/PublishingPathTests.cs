using MediaCompany.Application.Production;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Publication;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>Shared fixtures for the publishing path.</summary>
internal static class PublishingFixture
{
    internal static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
    internal static readonly DateOnly Observed = new(2026, 9, 27);

    internal static DestinationDescriptor Destination { get; } =
        new("a-video-platform", "the-company-channel", "en-GB");

    internal static PublicationSettings Settings { get; } = new()
    {
        Visibility = "unlisted",
        MadeForKids = false,
        CommentPolicy = "held-for-review",
        CategoryName = "science-and-technology",
    };

    internal static DeclaredSubject Subject { get; } = new()
    {
        Subject = "deep-sea cephalopod physiology",
        Pillar = "physiology",
        Format = "long-form documentary",
        SubjectTerms = ["Architeuthis dux", "chromatophore density"],
        Libraries = ["library-one"],
    };

    internal static SurfaceDraft Draft(string title = "Chromatophore density in Architeuthis dux") => new()
    {
        Title = title,
        Description = "A quantified account of chromatophore density, with named attribution.",
        Tags = ["cephalopod", "physiology", "marine biology"],
        ThumbnailReference = "thumbnail-variant-3",
        Captions = "Chromatophore density varies with depth.",
        AdultSignallingElement = "on-screen mortality statistics at 04:12",
        AudienceReasoning = "not made for kids: the item carries mortality statistics and a scientific register",
        SubjectTermsUsed = ["Architeuthis dux"],
    };

    internal static FirstPublicationConditionRegister AllSatisfied { get; } =
        new(Enum.GetValues<FirstPublicationCondition>().Select(c =>
            new ConditionObservation(c, ConditionState.Satisfied, "fixture", Observed)));

    internal static IReadOnlyList<SurfaceRecord> Surfaces(ItemId item, ItemVersion version, string? title = null) =>
        PublishedFacingSurfaces.Produce(
            item, version, Subject, Destination,
            Enum.GetValues<TreatmentCondition>(),
            title is null ? Draft() : Draft(title),
            Now);

    internal static GateVerdict Passed(ItemId item, ItemVersion version) =>
        new GateVerdict.Passed(new GatePassToken(item, version, GatePredicates.OwnerApprovalGate, Now));
}

/// <summary>
/// The dispatch composer, exercised to its own edge.
///
/// Every test here runs the complete path and asserts where it comes to rest. The furthest any of
/// them reaches is a composed descriptor: there is no case in the outcome union past it, so there
/// is nothing further to assert against.
/// </summary>
public sealed class PublicationDispatchComposerTests
{
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(3);

    /// <summary>
    /// The complete path composes and STOPS. The outcome is a composed descriptor at rest, which
    /// is the stopping surface this wave was built to reach.
    /// </summary>
    [Fact]
    public void TheCompletePathComposesADescriptorAndStopsThere()
    {
        var outcome = PublicationDispatchComposer.Compose(
            WorkforceRole.Publisher,
            PublishingFixture.Passed(Item, Version),
            Item, Version,
            PublishingFixture.Destination,
            PublishingFixture.Settings,
            PublishingFixture.Surfaces(Item, Version),
            PublishingFixture.Now.AddDays(3),
            PublishingFixture.Now);

        var composed = Assert.IsType<DispatchOutcome.Composed>(outcome);

        Assert.Equal(DispatchKey.Derive(Item, Version), composed.Descriptor.Key);
        Assert.Equal(Version, composed.Descriptor.Version);
        Assert.Equal(PublishingFixture.Destination, composed.Descriptor.Destination);
        Assert.NotEmpty(composed.Descriptor.MetadataAndSettingsDigest);

        // The union has exactly two cases, and this is one of them. There is no third to check for.
        Assert.Equal(2, DispatchOutcome.Cases.Count);
    }

    /// <summary>
    /// A direct attempt to go further is refused, and the refusal names the STRUCTURAL reason
    /// rather than reporting a setting or a failure.
    /// </summary>
    [Fact]
    public void TheRefusalForNoEffectingComponentNamesTheStructuralReason()
    {
        var reason = DispatchRefusalReason.NoEffectingComponentExists;

        // The reason exists in the closed set, so a caller asking to effect a dispatch gets a named
        // structural answer rather than a missing-method exception.
        Assert.Contains(reason, Enum.GetValues<DispatchRefusalReason>());

        // And no member of the refusal set names a configuration value, because none is consulted.
        foreach (var member in Enum.GetNames<DispatchRefusalReason>())
        {
            Assert.DoesNotContain("Disabled", member, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NotConfigured", member, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FlagOff", member, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>A role holding no dispatch action is refused, and the refusal says why no role could send.</summary>
    [Fact]
    public void ARoleHoldingNoDispatchActionIsRefused()
    {
        var outcome = PublicationDispatchComposer.Compose(
            WorkforceRole.Researcher,
            PublishingFixture.Passed(Item, Version),
            Item, Version,
            PublishingFixture.Destination,
            PublishingFixture.Settings,
            PublishingFixture.Surfaces(Item, Version),
            PublishingFixture.Now.AddDays(3),
            PublishingFixture.Now);

        var refused = Assert.IsType<DispatchOutcome.Refused>(outcome);
        Assert.Equal(DispatchRefusalReason.ActorHoldsNoDispatchAction, refused.Reason);
        Assert.Contains("no egress action at all", refused.Detail);
    }

    /// <summary>A gate refusal for a condition of first publication surfaces as its own dispatch reason.</summary>
    [Fact]
    public void AFirstPublicationConditionRefusalSurfacesAsItsOwnDispatchReason()
    {
        var gateRefusal = new GateVerdict.Refused(
            GateRefusal.FirstPublicationPaymentAccountUnmet, "the payment account does not exist");

        var outcome = PublicationDispatchComposer.Compose(
            WorkforceRole.Publisher, gateRefusal, Item, Version,
            PublishingFixture.Destination, PublishingFixture.Settings,
            PublishingFixture.Surfaces(Item, Version),
            PublishingFixture.Now.AddDays(3), PublishingFixture.Now);

        var refused = Assert.IsType<DispatchOutcome.Refused>(outcome);
        Assert.Equal(DispatchRefusalReason.FirstPublicationConditionUnmet, refused.Reason);
    }

    /// <summary>A dispatch presented against a changed version is refused at dispatch time.</summary>
    [Fact]
    public void ADispatchAgainstAChangedVersionIsRefused()
    {
        var outcome = PublicationDispatchComposer.Compose(
            WorkforceRole.Publisher,
            PublishingFixture.Passed(Item, new ItemVersion(2)),
            Item,
            new ItemVersion(3),
            PublishingFixture.Destination,
            PublishingFixture.Settings,
            PublishingFixture.Surfaces(Item, new ItemVersion(3)),
            PublishingFixture.Now.AddDays(3),
            PublishingFixture.Now);

        var refused = Assert.IsType<DispatchOutcome.Refused>(outcome);
        Assert.Equal(DispatchRefusalReason.ApprovalBoundToAnotherVersion, refused.Reason);
    }

    /// <summary>A surface refused at screening stops the dispatch, naming the surface and the term.</summary>
    [Fact]
    public void ASurfaceRefusedAtScreeningStopsTheDispatch()
    {
        var outcome = PublicationDispatchComposer.Compose(
            WorkforceRole.Publisher,
            PublishingFixture.Passed(Item, Version),
            Item, Version,
            PublishingFixture.Destination,
            PublishingFixture.Settings,
            PublishingFixture.Surfaces(Item, Version, "Cute baby animals for kids"),
            PublishingFixture.Now.AddDays(3),
            PublishingFixture.Now);

        var refused = Assert.IsType<DispatchOutcome.Refused>(outcome);
        Assert.Equal(DispatchRefusalReason.SurfaceScreeningFailed, refused.Reason);
        Assert.Contains("Title", refused.Detail);
    }

    /// <summary>
    /// A retry composes to the SAME key. Exactly-once at the application edge: the key carries no
    /// per-attempt component, so a second attempt cannot address a second record.
    /// </summary>
    [Fact]
    public void ARetryComposesToTheSameKey()
    {
        DispatchOutcome Attempt(DateTimeOffset at) => PublicationDispatchComposer.Compose(
            WorkforceRole.Publisher,
            PublishingFixture.Passed(Item, Version),
            Item, Version,
            PublishingFixture.Destination,
            PublishingFixture.Settings,
            PublishingFixture.Surfaces(Item, Version),
            PublishingFixture.Now.AddDays(3),
            at);

        var first = Assert.IsType<DispatchOutcome.Composed>(Attempt(PublishingFixture.Now));
        var retry = Assert.IsType<DispatchOutcome.Composed>(Attempt(PublishingFixture.Now.AddHours(9)));

        Assert.Equal(first.Descriptor.Key, retry.Descriptor.Key);

        // The composition instant differs, which proves the clock reached the descriptor and still
        // did not reach the key.
        Assert.NotEqual(first.Descriptor.ComposedAt, retry.Descriptor.ComposedAt);
    }

    /// <summary>Every attempt, refused ones included, answers all five audit questions.</summary>
    [Fact]
    public void EveryAttemptRecordAnswersAllFiveQuestions()
    {
        var composed = PublicationDispatchComposer.Compose(
            WorkforceRole.Publisher, PublishingFixture.Passed(Item, Version), Item, Version,
            PublishingFixture.Destination, PublishingFixture.Settings,
            PublishingFixture.Surfaces(Item, Version),
            PublishingFixture.Now.AddDays(3), PublishingFixture.Now);

        var refusedOutcome = PublicationDispatchComposer.Compose(
            WorkforceRole.Researcher, PublishingFixture.Passed(Item, Version), Item, Version,
            PublishingFixture.Destination, PublishingFixture.Settings,
            PublishingFixture.Surfaces(Item, Version),
            PublishingFixture.Now.AddDays(3), PublishingFixture.Now);

        foreach (var outcome in new[] { composed, refusedOutcome })
        {
            var record = PublicationDispatchComposer.RecordFor(
                outcome, Item, Version, PublishingFixture.Destination,
                "owner", "0".PadLeft(64, '0'), PublishingFixture.Now);

            Assert.True(record.AnswersAllFive);
            Assert.Equal(Item, record.Item);
            Assert.Equal(Version, record.Version);
            Assert.Equal(PublishingFixture.Destination.Canonical, record.DestinationCanonical);
            Assert.NotEqual(default, record.AttemptedAt);
            Assert.NotEmpty(record.MetadataAndSettingsDigest);
            Assert.NotEmpty(record.ApprovedBy);
        }
    }

    /// <summary>A refused attempt additionally names the condition it failed.</summary>
    [Fact]
    public void ARefusedAttemptNamesTheConditionItFailed()
    {
        var outcome = PublicationDispatchComposer.Compose(
            WorkforceRole.Researcher, PublishingFixture.Passed(Item, Version), Item, Version,
            PublishingFixture.Destination, PublishingFixture.Settings,
            PublishingFixture.Surfaces(Item, Version),
            PublishingFixture.Now.AddDays(3), PublishingFixture.Now);

        var record = PublicationDispatchComposer.RecordFor(
            outcome, Item, Version, PublishingFixture.Destination,
            string.Empty, "0".PadLeft(64, '0'), PublishingFixture.Now);

        Assert.True(record.Refused);
        Assert.Equal(nameof(DispatchRefusalReason.ActorHoldsNoDispatchAction), record.RefusalReason);

        // Answer five stays answerable even where there was no approval.
        Assert.Contains("none", record.ApprovedBy);
    }

    /// <summary>An attempt record missing an audit answer is not constructible.</summary>
    [Fact]
    public void AnAttemptMissingAnAuditAnswerIsNotConstructible()
    {
        Assert.Throws<ArgumentException>(() => new AttemptRecord(
            Guid.NewGuid(), DispatchKey.Derive(Item, Version), Item, Version,
            destinationCanonical: "   ", PublishingFixture.Now,
            "0".PadLeft(64, '0'), "owner", refused: false, string.Empty, string.Empty));

        Assert.Throws<ArgumentException>(() => new AttemptRecord(
            Guid.NewGuid(), DispatchKey.Derive(Item, Version), Item, Version,
            PublishingFixture.Destination.Canonical, PublishingFixture.Now,
            "0".PadLeft(64, '0'), approvedBy: "  ", refused: false, string.Empty, string.Empty));
    }
}

/// <summary>The five published-facing surfaces and their screening.</summary>
public sealed class PublishedFacingSurfaceTests
{
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(1);

    /// <summary>All five surfaces are produced, each with a recorded outcome for its step.</summary>
    [Fact]
    public void AllFiveSurfacesAreProducedWithARecordedOutcome()
    {
        var surfaces = PublishingFixture.Surfaces(Item, Version);

        Assert.Equal(5, surfaces.Count);
        Assert.Equal(
            Enum.GetValues<SurfaceKind>().Order().ToArray(),
            surfaces.Select(s => s.Kind).Order().ToArray());
        Assert.All(surfaces, s => Assert.NotEmpty(s.Reason));
    }

    /// <summary>Each produced surface is bound to the exact item version it was produced from.</summary>
    [Fact]
    public void EverySurfaceIsBoundToTheExactVersion()
    {
        var surfaces = PublishingFixture.Surfaces(Item, new ItemVersion(9));
        Assert.All(surfaces, s => Assert.Equal(new ItemVersion(9), s.Version));
    }

    /// <summary>Each produced surface carries an assessment against all NINE treatment conditions.</summary>
    [Fact]
    public void EveryProducedSurfaceIsAssessedAgainstAllNineTreatmentConditions()
    {
        var produced = PublishingFixture.Surfaces(Item, Version)
            .Where(s => s.Outcome == SurfaceProductionOutcome.Produced)
            .ToArray();

        Assert.NotEmpty(produced);
        Assert.All(produced, s => Assert.Equal(
            Enum.GetValues<TreatmentCondition>().Length,
            s.TreatmentAssessment.Select(v => v.Condition).Distinct().Count()));
    }

    /// <summary>
    /// K-8 resolves as the standing obligation it is, naming the parameter it waits on. It is never
    /// an observed pass, because its evidence surface opens only after publication.
    /// </summary>
    [Fact]
    public void TheAudienceShareConditionResolvesAsAStandingObligation()
    {
        var title = PublishingFixture.Surfaces(Item, Version).First(s => s.Kind == SurfaceKind.Title);

        var k8 = title.TreatmentAssessment
            .Single(v => v.Condition == TreatmentCondition.AudienceShareMonitoring);

        Assert.Equal(TreatmentVerdictKind.StandingObligation, k8.Kind);
        Assert.False(k8.Blocks);
        Assert.NotNull(k8.AwaitedParameter);
    }

    /// <summary>
    /// A barred child-directed term in the title is refused, and the refusal NAMES the term. A
    /// composite verdict cannot be acted on.
    /// </summary>
    [Theory]
    [InlineData("Cute animals for kids")]
    [InlineData("Funny baby animals")]
    [InlineData("Learn animals with us")]
    public void ABarredTermInTheTitleIsRefusedWithTheTermNamed(string title)
    {
        var surfaces = PublishingFixture.Surfaces(Item, Version, title);
        var titleSurface = surfaces.Single(s => s.Kind == SurfaceKind.Title);

        Assert.Equal(SurfaceProductionOutcome.RefusedAtScreening, titleSurface.Outcome);
        Assert.Contains("barred child-directed term", titleSurface.Reason);
        Assert.False(titleSurface.IsHoldable);
    }

    /// <summary>An unscreened produced surface cannot enter the record at all.</summary>
    [Fact]
    public void AProducedSurfaceWithoutAFullAssessmentIsNotConstructible()
    {
        Assert.Throws<ArgumentException>(() => new SurfaceRecord(
            SurfaceKind.Title, Item, Version, SurfaceProductionOutcome.Produced,
            "a title", "produced", [], PublishingFixture.Now));
    }

    /// <summary>A step producing nothing records that outcome with its reason, never absent or skipped.</summary>
    [Fact]
    public void AStepProducingNothingRecordsTheOutcomeWithItsReason()
    {
        var draft = PublishingFixture.Draft() with
        {
            Captions = string.Empty,
            NotProducedReasons = new Dictionary<SurfaceKind, string>
            {
                [SurfaceKind.Captions] = "no spoken audio track exists on this item",
            },
        };

        var captions = PublishedFacingSurfaces.Produce(
                Item, Version, PublishingFixture.Subject, PublishingFixture.Destination,
                Enum.GetValues<TreatmentCondition>(), draft, PublishingFixture.Now)
            .Single(s => s.Kind == SurfaceKind.Captions);

        Assert.Equal(SurfaceProductionOutcome.NotProduced, captions.Outcome);
        Assert.Contains("no spoken audio track", captions.Reason);

        // Not produced is still holdable: it is a recorded outcome, not a failure.
        Assert.True(captions.IsHoldable);
    }

    /// <summary>
    /// Every step accepts a subject and a destination it was not configured with. The five
    /// re-pointable values are parameters, so re-pointing is a value change and nothing more.
    /// </summary>
    [Fact]
    public void EveryStepAcceptsASubjectAndDestinationItWasNotBuiltWith()
    {
        var otherSubject = PublishingFixture.Subject with
        {
            Subject = "high-altitude corvid cognition",
            Pillar = "cognition",
            SubjectTerms = ["Pyrrhocorax graculus"],
            Libraries = ["library-two"],
        };
        var otherDestination = new DestinationDescriptor("another-platform", "another-channel", "fr-FR");

        var surfaces = PublishedFacingSurfaces.Produce(
            Item, Version, otherSubject, otherDestination,
            Enum.GetValues<TreatmentCondition>(), PublishingFixture.Draft(), PublishingFixture.Now);

        Assert.Equal(5, surfaces.Count);
        Assert.All(
            surfaces.Where(s => s.Outcome == SurfaceProductionOutcome.Produced),
            s => Assert.NotEmpty(s.TreatmentAssessment));
    }

    /// <summary>Changing the destination changes the descriptor's destination and nothing else.</summary>
    [Fact]
    public void ChangingTheDestinationChangesThatValueAlone()
    {
        var elsewhere = new DestinationDescriptor("another-platform", "another-channel", "fr-FR");

        DispatchDescriptor Compose(DestinationDescriptor destination) =>
            ((DispatchOutcome.Composed)PublicationDispatchComposer.Compose(
                WorkforceRole.Publisher, PublishingFixture.Passed(Item, Version), Item, Version,
                destination, PublishingFixture.Settings,
                PublishingFixture.Surfaces(Item, Version),
                PublishingFixture.Now.AddDays(3), PublishingFixture.Now)).Descriptor;

        var here = Compose(PublishingFixture.Destination);
        var there = Compose(elsewhere);

        Assert.NotEqual(here.Destination, there.Destination);

        // The key depends on item and version only, so re-pointing does not make it a new dispatch.
        Assert.Equal(here.Key, there.Key);
        Assert.Equal(here.PlannedAt, there.PlannedAt);
    }
}

/// <summary>The planned publication time, held and read by nothing.</summary>
public sealed class PublicationTimingTests
{
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(1);

    private static PublicationTiming.TimingPolicy Policy => new()
    {
        LeadDays = 3,
        PreferredTimeOfDay = new TimeOnly(14, 0),
    };

    /// <summary>A time is computed and held, with the computation recorded so it is re-derivable.</summary>
    [Fact]
    public void ATimeIsComputedAndHeldWithItsComputationRecorded()
    {
        var planned = PublicationTiming.Compute(Item, Version, PublishingFixture.Now, Policy);

        Assert.Equal(new DateTimeOffset(2026, 10, 4, 14, 0, 0, TimeSpan.Zero), planned.PlannedAt);
        Assert.Contains("3 lead day(s)", planned.Computation);
        Assert.Contains("read by nothing", planned.Computation);
    }

    /// <summary>
    /// The computation commits neither a publication date nor a publication pattern.
    ///
    /// Asserted against what the type actually carries, rather than against a property hardcoded to
    /// false: a held time is a single instant and its computation record, with no recurrence, no
    /// commitment and no state beyond that. A pattern would have to be a member here to exist, and
    /// a member added to carry one fails this.
    /// </summary>
    [Fact]
    public void TheComputationCommitsNoPublicationDateOrPattern()
    {
        var planned = PublicationTiming.Compute(Item, Version, PublishingFixture.Now, Policy);

        var members = planned.GetType()
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(m => m.Name)
            .Order()
            .ToArray();

        Assert.Equal(new[] { "Computation", "Item", "PlannedAt", "Version" }, members);

        foreach (var forbidden in new[] { "Recurrence", "Pattern", "Cadence", "Cron", "Repeat", "Committed" })
        {
            Assert.DoesNotContain(members, m => m.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// A planned time that has elapsed produces no dispatch, because nothing reads a due time.
    ///
    /// The demonstration is the absence of a reader: the planned time is an attribute of the
    /// descriptor, and no component in the build selects anything by it. Composing after the
    /// planned time has passed still comes to rest at the descriptor, because there is no case
    /// past composition to reach.
    /// </summary>
    [Fact]
    public void AnElapsedPlannedTimeProducesNoDispatch()
    {
        var elapsed = PublicationTiming.Compute(
            Item, Version, PublishingFixture.Now.AddYears(-1), Policy);

        Assert.True(elapsed.PlannedAt < PublishingFixture.Now);

        var outcome = PublicationDispatchComposer.Compose(
            WorkforceRole.Publisher, PublishingFixture.Passed(Item, Version), Item, Version,
            PublishingFixture.Destination, PublishingFixture.Settings,
            PublishingFixture.Surfaces(Item, Version),
            elapsed.PlannedAt, PublishingFixture.Now);

        var composed = Assert.IsType<DispatchOutcome.Composed>(outcome);

        // The elapsed instant is carried as an attribute and changes nothing: the outcome is the
        // same composed descriptor at rest that a future instant would have produced.
        Assert.Equal(elapsed.PlannedAt, composed.Descriptor.PlannedAt);
    }

    /// <summary>The arithmetic is pure: the same inputs always give the same answer.</summary>
    [Fact]
    public void TheArithmeticIsPure()
    {
        var a = PublicationTiming.Compute(Item, Version, PublishingFixture.Now, Policy);
        var b = PublicationTiming.Compute(Item, Version, PublishingFixture.Now, Policy);

        Assert.Equal(a.PlannedAt, b.PlannedAt);
        Assert.Equal(a.Computation, b.Computation);
    }

    /// <summary>A policy permitting no day has no candidate instant, and says so rather than looping.</summary>
    [Fact]
    public void APolicyPermittingNoDayIsRefused()
    {
        var impossible = new PublicationTiming.TimingPolicy
        {
            LeadDays = 1,
            PreferredTimeOfDay = new TimeOnly(9, 0),
            PermittedDays = [],
        } with
        { PermittedDays = [(DayOfWeek)99] };

        Assert.Throws<ArgumentException>(() =>
            PublicationTiming.Compute(Item, Version, PublishingFixture.Now, impossible));
    }
}

/// <summary>The weakest-across-components evidence rule.</summary>
public sealed class EvidenceCompositionTests
{
    private static readonly DateTimeOffset Now = PublishingFixture.Now;

    private static EvidenceComponent Evidenced(string name) =>
        new(name, DeterminationOutcome.Evidenced, $"{name} was produced by a used surface", string.Empty, string.Empty);

    private static EvidenceComponent NotEvidenceable(string name) =>
        new(name, DeterminationOutcome.RecordedNotEvidenceable, string.Empty,
            $"{name} has no reachable evidence surface short of upload",
            $"{name} becomes evidenceable once the item is published");

    /// <summary>
    /// A determination whose external half cannot be evidenced reads as recorded-not-evidenceable
    /// EVEN WHERE its internal half was computed. Building a surface is not evidence.
    /// </summary>
    [Fact]
    public void APartiallyEvidencedDeterminationIsNotEvidenced()
    {
        var resolution = EvidenceComposition.Resolve(
            ComplianceDetermination.DuplicateDetection,
            [Evidenced("own-catalogue distance"), NotEvidenceable("external-source index")],
            Now);

        Assert.Equal(DeterminationOutcome.RecordedNotEvidenceable, resolution.Outcome);
        Assert.True(resolution.IsWellFormed);
        Assert.False(resolution.Blocks);

        // The produced half is NAMED rather than discarded.
        Assert.Contains("own-catalogue distance", resolution.Evidence);
        Assert.Contains("own-catalogue distance", resolution.NotEvidenceableReason);
        Assert.NotEmpty(resolution.WhatWouldMakeItEvidenceable!);
    }

    /// <summary>All components evidenced resolves as evidenced.</summary>
    [Fact]
    public void AFullyEvidencedDeterminationResolvesAsEvidenced()
    {
        var resolution = EvidenceComposition.Resolve(
            ComplianceDetermination.AdvertiserSuitability,
            [Evidenced("fourteen-category classification"), Evidenced("derived rating")],
            Now);

        Assert.Equal(DeterminationOutcome.Evidenced, resolution.Outcome);
        Assert.True(resolution.IsWellFormed);
    }

    /// <summary>
    /// A determination with NO components is unresolved, not evidenced. An empty component set is
    /// the absence of evidence, and reading it as sufficient is the failure the rule prevents.
    /// </summary>
    [Fact]
    public void ADeterminationWithNoComponentsIsUnresolved()
    {
        var resolution = EvidenceComposition.Resolve(
            ComplianceDetermination.DuplicateDetection, [], Now);

        Assert.Equal(DeterminationOutcome.Unresolved, resolution.Outcome);
        Assert.True(resolution.Blocks);
    }

    /// <summary>One failed component makes the whole determination fail, whatever else was evidenced.</summary>
    [Fact]
    public void OneFailedComponentDecidesTheWholeDetermination()
    {
        var failed = new EvidenceComponent(
            "treatment screen", DeterminationOutcome.Failed, string.Empty, string.Empty, string.Empty);

        var resolution = EvidenceComposition.Resolve(
            ComplianceDetermination.DuplicateDetection,
            [Evidenced("own-catalogue distance"), failed],
            Now);

        Assert.Equal(DeterminationOutcome.Failed, resolution.Outcome);
        Assert.True(resolution.Blocks);
    }

    /// <summary>A component claiming evidence without naming any is not constructible.</summary>
    [Fact]
    public void AnEvidencedComponentMustNameWhatWasProduced()
    {
        Assert.Throws<ArgumentException>(() => new EvidenceComponent(
            "half", DeterminationOutcome.Evidenced, "   ", string.Empty, string.Empty));
    }

    /// <summary>A not-evidenceable component carries both its reason and its remedy.</summary>
    [Fact]
    public void ANotEvidenceableComponentCarriesReasonAndRemedy()
    {
        Assert.Throws<ArgumentException>(() => new EvidenceComponent(
            "half", DeterminationOutcome.RecordedNotEvidenceable, string.Empty, "a reason", "   "));
    }
}

/// <summary>The served reasoning tier, read from the admitting route.</summary>
public sealed class ServedReasoningTierTests
{
    private static Route RouteStating(ReasoningTier? tier) => new(
        RouteId.New(),
        CapabilityClass.EditorialReasoning,
        RouteTier.Primary,
        new RouteTarget.HoldAndEscalate("held"),
        new QualityRating(80),
        new ContextCapacity(100_000),
        "commercial terms",
        new DateOnly(2026, 9, 1),
        tier);

    /// <summary>A route that states a tier carries it, and it is the route's value, not the request's.</summary>
    [Fact]
    public void ARouteThatStatesATierCarriesIt()
    {
        Assert.Equal(ReasoningTier.Deep, RouteStating(ReasoningTier.Deep).StatedReasoningTier);
    }

    /// <summary>
    /// A route that states none yields the EXPLICIT ABSENCE MARKER. It does not mean unknown, and
    /// it does not mean standard: an untiered route stays visible as untiered.
    /// </summary>
    [Fact]
    public void ARouteStatingNoTierYieldsTheAbsenceMarker()
    {
        Assert.Null(RouteStating(null).StatedReasoningTier);
    }

    /// <summary>
    /// A record carrying a requested tier and an absent served tier carries no tier evidence. The
    /// count of records carrying it is a measured count that may legitimately be zero.
    /// </summary>
    [Fact]
    public void ARecordWithNoServedTierCarriesNoTierEvidence()
    {
        var withServed = new MediaCompany.Domain.Accounting.OperationRecord
        {
            Id = OperationId.New(),
            Run = RunId.New(),
            Attribution = new Attribution(ItemId.New(), ChannelId.New(), DepartmentId.New(), AgentId.New()),
            Capability = CapabilityClass.EditorialReasoning,
            Units = MediaCompany.Domain.Accounting.UnitCounts.None,
            CostBasis = MediaCompany.Domain.Accounting.CostBasis.Measurement,
            Duration = TimeSpan.Zero,
            Outcome = MediaCompany.Domain.Accounting.OperationOutcome.Succeeded,
            OccurredAt = PublishingFixture.Now,
            Attempt = 1,
            ReasoningTierRequested = ReasoningTier.Deep,
            ReasoningTierServed = ReasoningTier.Deep,
        };

        var withoutServed = withServed with { ReasoningTierServed = null };

        Assert.True(withServed.CarriesTierEvidence);
        Assert.False(withoutServed.CarriesTierEvidence);
    }
}
