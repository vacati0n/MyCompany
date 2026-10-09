using System.Text.Json;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Accounting;
using MediaCompany.Deterministic.Production;
using MediaCompany.Deterministic.Routing;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The production change's pure rules (decisions D-003, D-005, D-006, D-007, D-009, D-010 and D-011 of its design):
/// the excluded-service refusal by recorded terms positions, the billed-kind pricing rule, the item-cap filter, the
/// channel-without-amount reading under a cap, the held-stage refusal, and the narration split, material check, beat
/// timeline and store guard over item 001's recorded package as it is in the repository.
/// </summary>
public sealed class ProductionRulesTests
{
    private static readonly ModelId Speech = new("speech-model");

    // -----------------------------------------------------------------------
    // Excluded services (decision D-003)
    // -----------------------------------------------------------------------

    /// <summary>
    /// With an excluded service as the ONLY candidate, the call is refused under the forbidden-source reason, naming
    /// the kind: a terms position prohibiting automated access, one taking a licence over the customer's content,
    /// and, for a narration or still-image route, a position not recorded at all.
    /// </summary>
    [Theory]
    [InlineData(AutomatedAccessPosition.Prohibits, CustomerContentPosition.NoLicenceTaken, ForbiddenSourceKind.AutomatedAccessProhibited)]
    [InlineData(AutomatedAccessPosition.Permits, CustomerContentPosition.LicenceTaken, ForbiddenSourceKind.CustomerContentLicence)]
    [InlineData(null, CustomerContentPosition.NoLicenceTaken, ForbiddenSourceKind.AutomatedAccessProhibited)]
    [InlineData(AutomatedAccessPosition.Permits, null, ForbiddenSourceKind.CustomerContentLicence)]
    public void AnExcludedNarrationServiceIsRefusedUnderItsNamedKind(AutomatedAccessPosition? access, CustomerContentPosition? content, ForbiddenSourceKind kind)
    {
        var route = NarrationRoute(new RouteTermsPositions(access, content, "terms page, a demonstration reference", new DateOnly(2026, 10, 9)));

        var refused = Assert.IsType<CapabilityResolution.Refused>(RouteResolver.Resolve(Narration(100), Inputs(route)));

        Assert.Equal(RefusalReason.ForbiddenSource, refused.Reason);
        Assert.Equal(kind, refused.ForbiddenKind);
    }

    /// <summary>A narration route recording NO positions at all is refused: an unrecorded position is never permitted.</summary>
    [Fact]
    public void ANarrationRouteRecordingNoPositionIsRefused()
    {
        var refused = Assert.IsType<CapabilityResolution.Refused>(RouteResolver.Resolve(Narration(100), Inputs(NarrationRoute(null))));
        Assert.Equal(ForbiddenSourceKind.AutomatedAccessProhibited, refused.ForbiddenKind);
    }

    /// <summary>
    /// The delivered behaviour is unchanged for every other capability: a reasoning route with no recorded positions
    /// still resolves, and the delivered register refusal still refuses.
    /// </summary>
    [Fact]
    public void ADeliveredRouteRecordingNoPositionStillResolves()
    {
        var route = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        Assert.IsType<CapabilityResolution.Resolved>(RouteResolver.Resolve(Fixture.Request(), Fixture.Inputs([route])));
    }

    // -----------------------------------------------------------------------
    // Pricing in every billed kind (decision D-007)
    // -----------------------------------------------------------------------

    /// <summary>A route billed ONLY in characters is priced, and selectable, with no cached-unit price in force.</summary>
    [Fact]
    public void ARouteBilledOnlyInCharactersIsSelectableWithNoCachedPrice()
    {
        var prices = UnitPrices.From([Price(PriceUnitKind.CharacterUnit, 0.000015m)], [PriceUnitKind.CharacterUnit]);

        Assert.NotNull(prices);
        Assert.Equal(0.000015m, prices.PerCharacterUnit);
        Assert.Equal(0m, prices.PerCachedUnit);
        var resolved = Assert.IsType<CapabilityResolution.Resolved>(RouteResolver.Resolve(Narration(1_000), Inputs(Permitted(), prices)));
        Assert.Equal(Speech, ((RouteTarget.ProviderRoute)resolved.Route.Target).Model);
    }

    /// <summary>A route billing a kind with NO price in force is removed under its own reason, never estimated at zero.</summary>
    [Fact]
    public void ARouteBillingAKindWithNoPriceInForceIsRefusedNotEstimatedAtZero()
    {
        Assert.Null(UnitPrices.From([Price(PriceUnitKind.InputUnit, 0.000003m)], [PriceUnitKind.CharacterUnit]));

        var held = Assert.IsType<CapabilityResolution.Held>(RouteResolver.Resolve(Narration(1_000), Inputs(Permitted(), null)));
        Assert.Equal(RefusalReason.PriceNotInForce, held.Reason);
    }

    /// <summary>A model whose billed kinds are not recorded is priced as delivered: input, output and cached, all three.</summary>
    [Fact]
    public void AModelWithNoRecordedBilledKindsIsPricedAsDelivered()
    {
        Assert.Null(UnitPrices.From([Price(PriceUnitKind.InputUnit, 1m), Price(PriceUnitKind.OutputUnit, 1m)], null));
        Assert.NotNull(UnitPrices.From([Price(PriceUnitKind.InputUnit, 1m), Price(PriceUnitKind.OutputUnit, 1m), Price(PriceUnitKind.CachedUnit, 1m)], null));
    }

    // -----------------------------------------------------------------------
    // The item cap at resolution (decision D-006)
    // -----------------------------------------------------------------------

    /// <summary>
    /// A route whose WORST CASE would take the counted total past the cap is removed, and the call refused naming the
    /// cap, the counted total and the worst case; one whose worst case fits stays.
    /// </summary>
    [Fact]
    public void AWorstCasePastTheRemainingCapIsRefusedNamingTheCapTheTotalAndTheWorstCase()
    {
        var prices = UnitPrices.From([Price(PriceUnitKind.CharacterUnit, 0.01m)], [PriceUnitKind.CharacterUnit])!;
        var cap = Cap(5.95m, booked: 5.00m, open: 0.50m);

        var refused = Assert.IsType<CapabilityResolution.Refused>(
            RouteResolver.Resolve(Narration(46), Inputs(Permitted(), prices) with { ItemCap = cap }));
        Assert.Equal(RefusalReason.ItemCapExceeded, refused.Reason);
        Assert.Contains("0.46", refused.Detail, StringComparison.Ordinal);
        Assert.Contains("5.95", refused.Detail, StringComparison.Ordinal);
        Assert.Contains("counted total 5.50", refused.Detail, StringComparison.Ordinal);

        Assert.IsType<CapabilityResolution.Resolved>(
            RouteResolver.Resolve(Narration(45), Inputs(Permitted(), prices) with { ItemCap = cap }));
    }

    // -----------------------------------------------------------------------
    // The controller under a cap (decision D-006; the owner's decision on a channel with no amount)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Under a recorded item cap, a channel with no budget amount demands no action and records no amount; uncapped it
    /// refuses as delivered; the company reading still defers at 90 percent either way.
    /// </summary>
    [Fact]
    public void UnderACapAChannelWithNoAmountDemandsNothingAndTheCompanyReadingStillGoverns()
    {
        var quiet = Summary(companyBooked: 1m);
        var capped = CostController.Decide(quiet, Fixture.Now, itemCapped: true);
        var channel = capped.Readings.Single(r => r.Scope == GoverningScope.Channel);
        Assert.Equal(ControllerAction.None, capped.Action);
        Assert.IsType<RecordedAmount.NotRecorded>(channel.Amount);
        Assert.Contains("the company ceiling and the item's recorded cap govern", channel.Statement, StringComparison.Ordinal);

        Assert.Equal(RefusalReason.BudgetAmountNotRecorded, CostController.Decide(quiet, Fixture.Now).Reason);

        var deferred = CostController.Decide(Summary(companyBooked: 31.50m), Fixture.Now, itemCapped: true);
        Assert.Equal(ControllerAction.Defer, deferred.Action);
        Assert.Equal(GoverningScope.Company, deferred.DecidedBy);
    }

    // -----------------------------------------------------------------------
    // The held-stage refusal (decision D-010)
    // -----------------------------------------------------------------------

    /// <summary>A HELD stage refuses publish-ready, naming the stage and the summary it was held with.</summary>
    [Fact]
    public void AHeldStageRefusesPublishReadyNamingTheStageAndItsSummary()
    {
        var dossier = new ItemDossier
        {
            Item = ItemId.New(),
            Version = new ItemVersion(2),
            Stages = ProductionStageSet.All.Select(s => new StageEvidence
            {
                Stage = s,
                Outcome = s is ProductionStage.Production or ProductionStage.CopyrightCheck ? StageOutcome.Held : StageOutcome.Succeeded,
                Summary = s == ProductionStage.Production ? "three clips specified, not sourced" : "recorded",
                RecordedAt = Fixture.Now,
            }).ToArray(),
        };

        var verdict = PublishReadyPredicate.Evaluate(dossier, GateState.AwaitingRightsCheck, [], ChannelId.New(), null);

        var held = verdict.Refusals.Where(r => r.Refusal == PublishReadyRefusal.StageHeld).ToArray();
        Assert.Equal(2, held.Length);
        Assert.Contains(held, r => r.Detail.Contains("Production", StringComparison.Ordinal) && r.Detail.Contains("not sourced", StringComparison.Ordinal));
        Assert.Contains(held, r => r.Detail.Contains("CopyrightCheck", StringComparison.Ordinal));
        Assert.DoesNotContain(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.StageOutcomeMissing);
    }

    // -----------------------------------------------------------------------
    // Item 001's recorded package: the material, the split, the timeline
    // -----------------------------------------------------------------------

    /// <summary>The item material quotes every entry from its source: the check over the repository's files finds no mismatch.</summary>
    [Fact]
    public void ItemOnesMaterialQuotesEveryEntryFromItsSource()
    {
        var (material, sources) = Package();
        Assert.Null(MaterialCheck.FirstMismatch(material, sources));
        Assert.Equal(22, material.Graphics.Count);
        Assert.Equal(18, material.Clips.Count);
        Assert.Equal(6, material.Thumbnails.Count);
        Assert.Single(material.Thumbnails, t => t.Placeholder);
        Assert.All(material.Thumbnails, t => Assert.Null(t.AlternativeText));
    }

    /// <summary>A quotation altered by one character is refused, naming the entry; so is a displayed line not in its quotation.</summary>
    [Fact]
    public void AnAlteredQuotationOrAnInventedLineIsRefusedNamingTheEntry()
    {
        var (material, sources) = Package();
        var altered = material with
        {
            Graphics = material.Graphics.Select(g => g.Id == "GFX-03" ? g with { Quote = g.Quote.Replace("13", "12", StringComparison.Ordinal) } : g).ToArray(),
        };
        Assert.Contains("GFX-03", MaterialCheck.FirstMismatch(altered, sources), StringComparison.Ordinal);

        var invented = material with
        {
            Graphics = material.Graphics.Select(g => g.Id == "GFX-10" ? g with { Lines = [.. g.Lines, "1.3 seconds per kilometre"] } : g).ToArray(),
        };
        Assert.Contains("GFX-10", MaterialCheck.FirstMismatch(invented, sources), StringComparison.Ordinal);
    }

    /// <summary>
    /// The narration split: the parts concatenate to the recorded narration EXACTLY, all 11,096 characters; no part
    /// is above the maximum; every beat opens a part; a sentence above the maximum refuses the plan, naming it.
    /// </summary>
    [Fact]
    public void TheNarrationSplitConcatenatesToTheRecordedTextExactly()
    {
        var (material, sources) = Package();
        var narration = sources[material.NarrationFile];

        var parts = NarrationSplitter.Split(narration, material.Beats, 1_500);

        Assert.Equal(11_096, narration.Length);
        Assert.Equal(narration, string.Concat(parts.Select(p => p.Text)));
        Assert.All(parts, p => Assert.InRange(p.Text.Length, 1, 1_500));
        Assert.Equal(Enumerable.Range(1, 13), parts.Select(p => p.Beat).Distinct());
        Assert.All(material.Beats, b => Assert.StartsWith(b.Opening, parts.First(p => p.Beat == b.Number).Text, StringComparison.Ordinal));

        var refusal = Assert.Throws<NarrationSplitRefusedException>(() => NarrationSplitter.Split(narration, material.Beats, 120));
        Assert.Contains("above the configured maximum of 120", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The beat timeline: every beat's span is its measured duration in whole frames; its graphics and placeholders
    /// divide it equally; every clip placeholder lies inside its own beat's span; the frames add up to the total.
    /// </summary>
    [Fact]
    public void EveryPlaceholderSitsInsideItsBeatsMeasuredSpan()
    {
        var (material, _) = Package();
        var durations = material.Beats.Select(b => (b.Number, TimeSpan.FromSeconds(30 + (b.Number * 7.3)))).ToArray();
        var visuals = material.Graphics.Select(g => (g.Id, g.Beat, $"design/{g.Id}.png"))
            .Concat(material.Clips.Select(c => (c.Id, c.Beat, $"production/{c.Id}.png")))
            .ToArray();

        var timeline = BeatTimeline.Compose(durations, visuals, 30);

        var total = BeatTimeline.Frame(TimeSpan.FromTicks(durations.Sum(d => d.Item2.Ticks)), 30);
        Assert.Equal(total, timeline.Sum(s => s.Frames));
        Assert.Equal(40, timeline.Count);
        var elapsed = TimeSpan.Zero;
        foreach (var (beat, duration) in durations)
        {
            var from = BeatTimeline.Frame(elapsed, 30);
            elapsed += duration;
            var to = BeatTimeline.Frame(elapsed, 30);
            Assert.All(timeline.Where(s => s.Beat == beat), s => Assert.True(s.StartFrame >= from && s.StartFrame + s.Frames <= to, $"{s.Implements} leaves beat {beat}"));
        }
    }

    // -----------------------------------------------------------------------
    // The store guard (decision D-005), over values: no store is connected
    // -----------------------------------------------------------------------

    /// <summary>
    /// Fake mode only on a demonstration store; metered mode only on the company store whose name matches the
    /// configured identity; an undesignated store refuses both; plan-only reads any designated store.
    /// </summary>
    [Theory]
    [InlineData(ProductionMode.Fake, StoreDesignation.Demonstration, "mediacompany_demo", true)]
    [InlineData(ProductionMode.Fake, StoreDesignation.Company, "mediacompany", false)]
    [InlineData(ProductionMode.Metered, StoreDesignation.Demonstration, "mediacompany_demo", false)]
    [InlineData(ProductionMode.Metered, StoreDesignation.Company, "mediacompany", true)]
    [InlineData(ProductionMode.Metered, StoreDesignation.Company, "another_store", false)]
    [InlineData(ProductionMode.PlanOnly, StoreDesignation.Demonstration, "mediacompany_demo", true)]
    public void TheStoreGuardAdmitsEachModeOnlyOnItsStore(ProductionMode mode, StoreDesignation designation, string database, bool admitted)
    {
        var verdict = StoreGuard.Judge(mode, designation, database, "mediacompany");
        Assert.Equal(admitted, verdict.Admitted);
        Assert.Equal(admitted, verdict.Refusals.Count == 0);
    }

    [Fact]
    public void AnUndesignatedStoreRefusesEveryMode()
    {
        Assert.All(Enum.GetValues<ProductionMode>(), m => Assert.False(StoreGuard.Judge(m, null, "mediacompany", "mediacompany").Admitted));
    }

    // -----------------------------------------------------------------------

    private static (ItemMaterial Material, IReadOnlyDictionary<string, string> Sources) Package()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "MediaCompany.slnx")))
        {
            root = root.Parent;
        }

        var repository = root!.FullName;
        var material = JsonSerializer.Deserialize<ItemMaterial>(
            File.ReadAllBytes(Path.Combine(repository, "wave-2", "item-001", "item-material.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var paths = new[] { material.NarrationFile, material.SpecifiedRuntime.Source, material.UnsourcedClips.Source, material.ClipPlaceholders.Source }
            .Concat(material.Beats.SelectMany(b => new[] { b.StructureSource, b.OpeningSource, b.CueSource }))
            .Concat(material.Graphics.Select(g => g.Source)).Concat(material.Clips.Select(c => c.Source))
            .Concat(material.Thumbnails.Select(t => t.Source)).Concat(material.Thumbnails.Where(t => t.PlaceholderSource is not null).Select(t => t.PlaceholderSource!))
            .Distinct();
        return (material, paths.ToDictionary(p => p, p => File.ReadAllText(Path.Combine(repository, p)).Replace("\r\n", "\n", StringComparison.Ordinal)));
    }

    private static Route NarrationRoute(RouteTermsPositions? positions) =>
        new(RouteId.New(), CapabilityClass.Narration, RouteTier.Primary, new RouteTarget.ProviderRoute(Fixture.PrimaryAccount, Speech),
            new QualityRating(0), new ContextCapacity(0), "pay-per-use terms, a demonstration basis", Fixture.Today)
        {
            TermsPositions = positions,
        };

    private static Route Permitted() =>
        NarrationRoute(new RouteTermsPositions(AutomatedAccessPosition.Permits, CustomerContentPosition.NoLicenceTaken, "terms", Fixture.Today));

    private static CapabilityRequest Narration(long characters) =>
        new(CapabilityClass.Narration, ReasoningTier.Light, new QualityRating(0), new ContextCapacity(0), new Money(5.95m), Criticality.Routine,
            Fixture.Attribution(), new EstimatedUnits(0, 0, 0) { CharacterUnits = characters }, TimeSpan.Zero, ReducedFloorPolicy.Forbidden);

    private static ResolutionInputs Inputs(Route route, UnitPrices? prices = null) =>
        Fixture.Inputs([route], prices: prices is null
            ? new Dictionary<ModelId, UnitPrices>()
            : new Dictionary<ModelId, UnitPrices> { [Speech] = prices });

    private static ModelPrice Price(PriceUnitKind kind, decimal price) =>
        new(ModelPriceId.New(), Speech, kind, price, "USD", "ESTIMATE, a demonstration parameter", Fixture.Today, Fixture.Now.AddDays(-1), null);

    private static ItemCapReading Cap(decimal cap, decimal booked, decimal open) => new()
    {
        Cap = new Money(cap),
        Source = "the owner's cap, a demonstration parameter",
        Booked = new Money(booked),
        UnstatedOperations = 0,
        OpenReservations = new Money(open),
        OpenReservationCount = open > 0 ? 1 : 0,
    };

    private static GoverningReadingsSummary Summary(decimal companyBooked) =>
        new(new DateOnly(2026, 10, 1), ChannelId.New(), null, new ScopeSpend(0, 0, Money.Zero()), null, CompanyId.New(),
            new ScopeSpend(1, 0, new Money(companyBooked)), Math.Round(companyBooked * 100m / ApprovedEnvelope.Metered.Amount, 4), new Money(30m));
}
