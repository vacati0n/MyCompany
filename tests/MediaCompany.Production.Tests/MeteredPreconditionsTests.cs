using MediaCompany.Application.Ports;
using MediaCompany.Credentials;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Registry;
using MediaCompany.Production;
using Xunit;

namespace MediaCompany.Production.Tests;

/// <summary>
/// The metered mode's refusal (the production change, decision D-015 of its design), over values: every unmet
/// precondition is named before any call, the missing credential variable by its EXACT name, and a ready plan names
/// none. No store is connected and no variable holding a secret is read: presence is passed in as a value.
/// </summary>
public sealed class MeteredPreconditionsTests
{
    private static readonly ProviderAccountId Speech = new("openai");

    /// <summary>A ready plan names no unmet precondition.</summary>
    [Fact]
    public void AReadyPlanNamesNothing()
    {
        Assert.Empty(MeteredPreconditions.Unmet(Ready()));
    }

    /// <summary>Each condition the design lists refuses, naming it; together they are all named at once.</summary>
    [Fact]
    public void EveryUnmetConditionIsNamedAtOnce()
    {
        var plan = Plan(capped: false, priced: false);
        var unmet = MeteredPreconditions.Unmet(Ready() with
        {
            Plan = plan,
            StoreRefusals = ["metered mode books only into the company store, and mediacompany_demo is designated Demonstration"],
            ControllerRestriction = "Refuse for 2026-10 (BudgetAmountNotRecorded)",
            Credentials = [(CredentialBrokerFactory.VariableName(Speech, null), false)],
            FakesConfigured = [Speech],
            EndpointsMissing = [Speech],
            NarrationVoice = null,
        });

        Assert.Contains(unmet, u => u.Contains("designated Demonstration", StringComparison.Ordinal));
        Assert.Contains(unmet, u => u.Contains("no cap is recorded", StringComparison.Ordinal));
        Assert.Contains(unmet, u => u.Contains("no price in force", StringComparison.Ordinal));
        Assert.Contains(unmet, u => u.Contains("cost controller", StringComparison.Ordinal));
        Assert.Contains("the credential variable MEDIACOMPANY_SECRET_OPENAI__GLOBAL is not set", unmet);
        Assert.Contains(unmet, u => u.Contains("a fake provider is configured", StringComparison.Ordinal));
        Assert.Contains(unmet, u => u.Contains("no vendor endpoint", StringComparison.Ordinal));
        Assert.Contains(unmet, u => u.Contains("no narration voice", StringComparison.Ordinal));
        Assert.Equal(8, unmet.Count);
    }

    /// <summary>
    /// THE TOTAL LINE STATES WHAT ITS PRICE ROWS RECORD (the own-voice change, decision D-015 of its design): the vendor plan's
    /// total keeps its ESTIMATE label and its never-netted statement and names each price row's kind, recorded source and
    /// verified-on date; it asserts neither a first-hand verification nor its absence, because no recorded field
    /// distinguishes them; the price row's line drops "re-fetched first hand".
    /// </summary>
    [Fact]
    public async Task TheVendorPlansTotalLineStatesEachPriceRowsRecordedSourceAndDate()
    {
        var plan = Plan(capped: true, priced: true) with
        {
            Selection = new NarrationSelection(NarrationSource.Vendor, "named explicitly below the narration source rule: the vendor path under test", [], []),
        };
        var output = new StringWriter();
        plan.Print(output, ProductionMode.Metered, StoreDesignation.Company, MediaTool.Settings(Path.GetTempPath(), MediaTool.RepositoryRoot()),
            (await ItemPackageLoader.LoadAsync(MediaTool.RepositoryRoot(), "wave-2/item-001/item-material.json", CancellationToken.None)).Material);
        var text = output.ToString();

        Assert.Contains("total: 0.000060 USD ESTIMATE at configured unit prices; each price row as recorded: CharacterUnit, ESTIMATE, verified on 2026-10-09; never netted with development cost", text, StringComparison.Ordinal);
        Assert.Contains("price in force: CharacterUnit 0.000015 USD (ESTIMATE, verified on 2026-10-09) - a configured price, as recorded", text, StringComparison.Ordinal);
        Assert.DoesNotContain("first hand", text, StringComparison.Ordinal);
        Assert.Contains("part 1 (beat 1): Vendor;", text, StringComparison.Ordinal);
    }

    /// <summary>The exact variable names of the two company-level accounts, as the owner guide states them.</summary>
    [Fact]
    public void TheCompanyLevelAccountsVariableNamesAreTheGlobalForm()
    {
        Assert.Equal("MEDIACOMPANY_SECRET_OPENAI__GLOBAL", CredentialBrokerFactory.VariableName(new ProviderAccountId("openai"), null));
        Assert.Equal("MEDIACOMPANY_SECRET_ANTHROPIC__GLOBAL", CredentialBrokerFactory.VariableName(new ProviderAccountId("anthropic"), null));
    }

    private static MeteredReadiness Ready() => new()
    {
        Plan = Plan(capped: true, priced: true),
        StoreRefusals = [],
        ControllerRestriction = null,
        Credentials = [(CredentialBrokerFactory.VariableName(Speech, null), true)],
        FakesConfigured = [],
        EndpointsMissing = [],
        NarrationVoice = "a voice the owner chose",
    };

    private static ProductionPlan Plan(bool capped, bool priced)
    {
        var model = new ModelId("tts-1");
        var route = new Route(RouteId.New(), CapabilityClass.Narration, RouteTier.Primary, new RouteTarget.ProviderRoute(Speech, model),
            new QualityRating(0), new ContextCapacity(0), "terms", new DateOnly(2026, 10, 9));
        var prices = priced
            ? new[] { new ModelPrice(ModelPriceId.New(), model, PriceUnitKind.CharacterUnit, 0.000015m, "USD", "ESTIMATE", new DateOnly(2026, 10, 9), DateTimeOffset.UnixEpoch, null) }
            : [];
        var package = new RecordedPackage
        {
            Item = ItemId.New(),
            Channel = ChannelId.New(),
            Title = "a title",
            PackageVersion = new ItemVersion(1),
            PackageStages = [],
            HighestVersion = new ItemVersion(1),
            Cap = capped
                ? new ItemCapReading { Cap = new Money(5.95m), Source = "the owner", Booked = Money.Zero(), UnstatedOperations = 0, OpenReservations = Money.Zero(), OpenReservationCount = 0 }
                : null,
        };

        return ProductionPlan.Compose(
            package,
            [new NarrationPart(1, 1, "text")],
            new PlanPricing(DateTimeOffset.UnixEpoch, [route], prices,
                new Dictionary<ModelId, IReadOnlyList<PriceUnitKind>> { [model] = [PriceUnitKind.CharacterUnit] },
                [new ProviderAccount(Speech, "vendor", "terms", new DateOnly(2026, 10, 9), ProviderAccountStatus.Active) { Scope = CredentialScope.Company }]));
    }
}
