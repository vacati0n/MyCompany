using System.Globalization;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Production;
using MediaCompany.Deterministic.Routing;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Registry;

namespace MediaCompany.Production;

/// <summary>One planned narration part, with its worst case and its estimate where a boundary source narrates it.</summary>
public sealed record PlannedOperation(NarrationPart Part, long WorstCaseCharacters, Money? EstimatedCost);

/// <summary>
/// The plan of one production (the production change, decision D-015 of its design): every planned part with its
/// source and the reason it was chosen (the own-voice change, decision D-001), and for a boundary source its route, its
/// worst-case units and its ESTIMATED cost at the configured prices in force, the total labelled an estimate and never
/// netted with development cost, and the cap with its source and its counted total. It is printed before any call, in
/// every mode.
/// </summary>
public sealed record ProductionPlan
{
    public required RecordedPackage Package { get; init; }
    public required IReadOnlyList<PlannedOperation> Operations { get; init; }

    /// <summary>The narration source rule's selection, or its refusal, with every source passed over.</summary>
    public NarrationSelection? Selection { get; init; }

    /// <summary>The latest registration of the item's package version, where one is recorded.</summary>
    public RecordingRegistration? Recording { get; init; }

    /// <summary>The in-house model's whole verification, where the rule consulted it.</summary>
    public InstallationReport? Installation { get; init; }

    /// <summary>The script's own delivery rate, for the expectation printed per part.</summary>
    public ScriptRate? Rate { get; init; }

    /// <summary>The narration route the configured ordering serves first, or null where none is admitted.</summary>
    public Route? Route { get; init; }

    public ProviderAccount? Account { get; init; }

    /// <summary>The route's prices in force, or null where a kind it is billed by has none.</summary>
    public UnitPrices? Prices { get; init; }

    public IReadOnlyList<ModelPrice> PriceRows { get; init; } = [];

    public IReadOnlyList<PriceUnitKind> BilledKinds { get; init; } = [];

    /// <summary>The estimated total, or null where it cannot be stated.</summary>
    public Money? EstimatedTotal { get; init; }

    public DateTimeOffset PricedAt { get; init; }

    /// <summary>Composes a boundary source's plan from the recorded package, the split narration and the prices in force.</summary>
    public static ProductionPlan Compose(RecordedPackage package, IReadOnlyList<NarrationPart> parts, PlanPricing pricing)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(pricing);

        // The configured ordering: the first provider route by tier. No evidence ranks a narration route.
        var route = pricing.Routes.Where(r => r.Target is RouteTarget.ProviderRoute).OrderBy(r => r.Tier).FirstOrDefault();
        var provider = route?.Target as RouteTarget.ProviderRoute;
        var billed = provider is not null && pricing.BilledKinds.TryGetValue(provider.Model, out var kinds) ? kinds : [];
        var rows = provider is null ? [] : pricing.Prices.Where(p => p.Model == provider.Model).ToArray();
        var prices = provider is null ? null : UnitPrices.From(rows, billed);

        var operations = parts.Select(p =>
        {
            var worst = (long)p.Text.Length;
            Money? estimate = prices is null ? null : new Money(worst * prices.PerCharacterUnit, prices.Currency);
            return new PlannedOperation(p, worst, estimate);
        }).ToArray();

        Money? total = prices is null ? null : operations.Aggregate(Money.Zero(prices.Currency), (sum, o) => sum + o.EstimatedCost!.Value);

        return new ProductionPlan
        {
            Package = package,
            Operations = operations,
            Route = route,
            Account = provider is null ? null : pricing.Accounts.FirstOrDefault(a => a.Id == provider.ProviderAccount),
            Prices = prices,
            PriceRows = rows,
            BilledKinds = billed,
            EstimatedTotal = total,
            PricedAt = pricing.At,
        };
    }

    /// <summary>
    /// Composes an own-source plan (the own-voice change, decision D-001 of its design): one part per beat, the rule's
    /// selection or refusal, and what the rule read. No route, no price and no estimate: an own source makes no metered call.
    /// </summary>
    public static ProductionPlan ComposeOwn(
        RecordedPackage package, IReadOnlyList<NarrationPart> parts, NarrationSelection selection, RecordingRegistration? recording, InstallationReport? installation, LoadedItem loaded)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(loaded);
        return new ProductionPlan
        {
            Package = package,
            Operations = parts.Select(p => new PlannedOperation(p, p.Text.Length, null)).ToArray(),
            Selection = selection,
            Recording = selection.Source == NarrationSource.Recording ? recording : null,
            Installation = installation,
            Rate = loaded.Rate,
        };
    }

    /// <summary>Prints the plan, every figure named for what it is.</summary>
    public void Print(TextWriter output, ProductionMode mode, StoreDesignation designation, ProductionSettings settings, ItemMaterial material)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(material);
        var c = CultureInfo.InvariantCulture;
        var source = Selection?.Source;
        output.WriteLine($"Plan for item {material.Item} ({Package.Item}), mode {mode}, store designated {designation}:");
        output.WriteLine($"  narration: {Operations.Count} part(s) over {Operations.Sum(o => o.Part.Text.Length).ToString("N0", c)} characters "
            + $"(the recorded text, {material.NarrationCharacters.ToString("N0", c)} characters after line endings are normalised)");
        output.WriteLine(Selection is null
            ? "  source: not selected"
            : source is { } chosen ? $"  source: {chosen}, for every part, because {Selection.Reason}" : "  source: NONE; the narration source rule refused (each reason below)");
        foreach (var passed in Selection?.PassedOver ?? [])
        {
            output.WriteLine($"    {passed}");
        }

        foreach (var refusal in Selection?.Refusals ?? [])
        {
            output.WriteLine($"    refused: {refusal}");
        }

        if (source is NarrationSource.Fake or NarrationSource.Vendor)
        {
            PrintBoundary(output, c);
        }
        else
        {
            PrintOwn(output, source, settings, c);
        }

        output.WriteLine(Package.Cap is { } cap
            ? $"  cap: {cap.Describe()}"
            : "  cap: none recorded for the item");
        output.WriteLine($"  bounds: {settings.DescribeBounds()}");
        output.WriteLine(source is NarrationSource.Fake or NarrationSource.Vendor
            ? "  calls: narration only; no reasoning call and no image call; no stock footage and no music; nothing is published"
            : "  calls: none; no capability call, no network client and no metered charge; no stock footage and no music; nothing is published");
    }

    private void PrintBoundary(TextWriter output, CultureInfo c)
    {
        if (Route?.Target is RouteTarget.ProviderRoute provider)
        {
            output.WriteLine($"  route: {Route.Id} {Route.Tier} ({provider.ProviderAccount} / {provider.Model}), billed by {(BilledKinds.Count == 0 ? "input, output and cached units (not recorded)" : string.Join(", ", BilledKinds))}");
            foreach (var row in PriceRows)
            {
                // The total-line ruling (the own-voice change, decision D-015 of its design): a price row states what it
                // records, its source and its verification date; no field records a first-hand verification, so none is claimed.
                output.WriteLine($"    price in force: {row.UnitKind} {row.UnitPrice.ToString("0.##########", c)} {row.Currency} ({row.Source}, verified on {row.VerifiedOn:yyyy-MM-dd}) - a configured price, as recorded");
            }
        }
        else
        {
            output.WriteLine("  route: none admitted for narration");
        }

        foreach (var operation in Operations)
        {
            output.WriteLine($"  part {operation.Part.Ordinal} (beat {operation.Part.Beat}): {Selection?.Source}; {operation.WorstCaseCharacters.ToString("N0", c)} characters, "
                + $"worst case {operation.WorstCaseCharacters.ToString("N0", c)} character units, estimated cost "
                + $"{(operation.EstimatedCost is { } e ? e.Amount.ToString("0.000000", c) + " " + e.Currency : "not stated: no price in force")} (estimate)");
        }

        output.WriteLine(EstimatedTotal is { } total
            ? $"  total: {total.Amount.ToString("0.000000", c)} {total.Currency} ESTIMATE at configured unit prices; each price row as recorded: "
                + string.Join("; ", PriceRows.Select(r => $"{r.UnitKind}, {r.Source}, verified on {r.VerifiedOn:yyyy-MM-dd}"))
                + "; never netted with development cost"
            : "  total: not stated, because the route has no price in force for a kind it is billed by");
    }

    private void PrintOwn(TextWriter output, NarrationSource? source, ProductionSettings settings, CultureInfo c)
    {
        var reason = Selection?.Reason ?? "no source";
        foreach (var operation in Operations)
        {
            var words = DurationExpectation.Words(operation.Part.Text);
            var expected = Rate is null ? null : DurationExpectation.ExpectedSeconds(words, Rate.WordsPerMinute);
            output.WriteLine($"  part {operation.Part.Ordinal} (beat {operation.Part.Beat}): {(source?.ToString() ?? "NO SOURCE")} - {reason}; "
                + $"{operation.Part.Text.Length.ToString("N0", c)} characters, {words.ToString("N0", c)} words"
                + (expected is { } e ? $", expected {e.ToString("0.0", c)} s at the script's rate (a reported reference, never a target)" : string.Empty));
        }

        if (Recording is { } recording && source == NarrationSource.Recording)
        {
            output.WriteLine($"  recording: registration {recording.Id} recorded at {recording.RecordedAt:O}; {recording.Release.Describe()}");
            foreach (var beat in recording.Beats.OrderBy(b => b.Beat))
            {
                output.WriteLine($"    beat {beat.Beat:00}: {beat.StoredPath}, sha256 {beat.Sha256} (re-hashed now; equals the registered value), {beat.Measured.Format}, "
                    + $"{beat.Measured.DescribeDuration()}");
            }
        }

        if (Installation is { } installation && settings.InHouseModel is { } model)
        {
            output.WriteLine($"  in-house model: {installation.RuntimeName ?? "name not read"} {installation.RuntimeVersion ?? "version not read"} "
                + $"(code licence {installation.RuntimeLicence ?? "not read"}, read from its package metadata); voice {installation.VoiceName ?? "not read"}; "
                + $"{installation.RecordEntriesVerified.ToString(c)} record entries verified, {installation.RecordEntriesWithoutHash.ToString(c)} listed without a hash");
            foreach (var file in installation.Files)
            {
                output.WriteLine($"    {file.Describe()}");
            }

            output.WriteLine($"    generation settings, passed explicitly: {model.Generation.Describe()}; the voice configuration states "
                + (installation.VoiceDefaults.Count == 0 ? "no inference values" : string.Join(", ", installation.VoiceDefaults.Select(d => $"{d.Key} {d.Value.ToString(c)}"))));
            output.WriteLine($"    model part bound {model.PartBound.TotalSeconds.ToString("0", c)} s on the monotonic clock (configured); verification read files only and started no process");
        }

        output.WriteLine(source == NarrationSource.Recording
            ? "  total: no metered spend is planned, USD 0.00; recording time unmeasured; local compute unmeasured"
            : "  total: no metered spend is planned, USD 0.00; local compute unmeasured");
    }
}
