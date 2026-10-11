using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Production;
using Xunit;

namespace MediaCompany.Production.Tests;

/// <summary>
/// The graphic specification check and the font coverage check (the graphics change, decisions D-001 and D-009 of its design),
/// over item 001's recorded specifications and package, and over faults injected into copies of them made in the test. Each
/// refusal is asserted by the words that name it.
/// </summary>
public sealed class GraphicSpecificationTests
{
    private static readonly string Repository = MediaTool.RepositoryRoot();

    // -----------------------------------------------------------------------
    // Item 001's 22 specifications
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ItemOnesTwentyTwoSpecificationsPassTheCheck()
    {
        var (material, specifications, sources) = await ItemOneAsync();
        var result = GraphicSpecificationCheck.Check(material, specifications, sources);

        Assert.True(result.Passed, string.Join("\n", result.Findings));
        Assert.Equal(22, result.Specifications.Count);
        Assert.Equal(material.Graphics.Select(g => g.Id).Order(StringComparer.Ordinal), result.Specifications.Select(s => s.Position));
    }

    /// <summary>Exactly the twelve positions of the owner's motion list move; the other ten are still.</summary>
    [Fact]
    public async Task ExactlyTheTwelveListedPositionsAreMotionGraphics()
    {
        var (material, specifications, sources) = await ItemOneAsync();
        var result = GraphicSpecificationCheck.Check(material, specifications, sources);

        string[] moving = ["GFX-02", "GFX-05", "GFX-06", "GFX-07", "GFX-08", "GFX-09", "GFX-11", "GFX-12", "GFX-14", "GFX-16", "GFX-18", "GFX-21"];
        Assert.Equal(moving, result.Specifications.Where(s => s.Kind == GraphicKind.Motion).Select(s => s.Position));
        Assert.Equal(10, result.Specifications.Count(s => s.Kind == GraphicKind.Still));
    }

    /// <summary>
    /// THE FIGURE SET IS THE RULED SET (the Planning Gate's ruling): 13-15 Hz, ~2 km, 60 degrees, nine o'clock and noon, and
    /// the publication years of the end card's citations — every one quoted, every year found in the claim record's source column.
    /// </summary>
    [Fact]
    public async Task TheItemsFigureSetIsTheRuledSet()
    {
        var (material, specifications, sources) = await ItemOneAsync();
        var result = GraphicSpecificationCheck.Check(material, specifications, sources);
        Assert.True(result.Passed, string.Join("\n", result.Findings));

        var figures = result.Figures.Where(f => f.Role != "citation").Select(f => f.Text).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "13–15 Hz", "60°", "nine o'clock", "noon", "~2 km" }.Order(StringComparer.Ordinal), figures);

        var claimRecord = sources["wave-2/item-001/claim-to-source.md"];
        var years = result.Figures.Where(f => f.Role == "citation").SelectMany(f => f.Tokens).Distinct().Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "1967", "1973", "1992", "1996", "2000", "2004", "2005", "2014" }, years);
        Assert.All(years, y => Assert.Contains(y, claimRecord, StringComparison.Ordinal));
        Assert.All(result.Figures.Where(f => f.Role == "citation"), f => Assert.Equal("GFX-22", f.Position));
    }

    /// <summary>
    /// THE END CARD LISTS ONLY AUTHORED CITATIONS (the owner's ruling at the contact-sheet review): the recorded source with no
    /// author, "comparative dialect literature", is omitted like the three sources the owner omitted earlier; the ten
    /// authored citations stay, in the claim record's order.
    /// </summary>
    [Fact]
    public async Task TheEndCardOmitsTheRecordedSourceWithNoAuthor()
    {
        var (material, specifications, sources) = await ItemOneAsync();
        var result = GraphicSpecificationCheck.Check(material, specifications, sources);
        Assert.True(result.Passed, string.Join("\n", result.Findings));

        var endCard = Encoding.UTF8.GetString(specifications["gfx-22.json"]);
        Assert.DoesNotContain("comparative dialect literature", endCard, StringComparison.Ordinal);
        Assert.Equal(10, JsonNode.Parse(endCard)!["elements"]!.AsArray()
            .Count(e => (string?)e!["role"] == "citation" && !((string)e["text"]!).StartsWith("studied by", StringComparison.Ordinal)));
    }

    /// <summary>The not-admitted conversion figure stays off screen: quoting it from the narration is refused by the number rule.</summary>
    [Fact]
    public async Task AFigureTheOwnerDidNotAdmitIsRefusedThoughItIsQuoted()
    {
        var (material, specifications, sources) = await ItemOneAsync();
        Mutate(specifications, "gfx-10.json", root => Elements(root).Add(Text("conversion", "roughly one second of waggling per kilometre", "wave-2/item-001/narration.txt")));

        var result = GraphicSpecificationCheck.Check(material, specifications, sources);
        Assert.Contains(result.Findings, f => f.Contains("gfx-10.json element conversion", StringComparison.Ordinal) && f.Contains("is not admitted", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheCheckHashesExactlyTheBytesItRead()
    {
        var (material, specifications, sources) = await ItemOneAsync();
        var result = GraphicSpecificationCheck.Check(material, specifications, sources);

        Assert.All(result.Specifications, s =>
        {
            Assert.Same(specifications[s.FileName], s.Bytes);
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(specifications[s.FileName])), s.Sha256);
        });
    }

    // -----------------------------------------------------------------------
    // Each injected fault, refused by name
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("missing", "gfx-04.json: missing")]
    [InlineData("extra", "gfx-23.json: extra")]
    [InlineData("row", "gfx-04.json: its row differs")]
    [InlineData("beat", "gfx-04.json: its beat 5 is not the material's beat 3")]
    [InlineData("unquoted", "its quotation is not found in wave-2/item-001/narration.txt")]
    [InlineData("misquoted", "its text differs from its quotation by at least one character")]
    [InlineData("unadmitted", "the figure \"twelve\" (tokens twelve) is not admitted")]
    [InlineData("undisclosed", "a bearing draws a quantity, and the specification carries no disclosure")]
    [InlineData("wording", "its disclosure \"schematic\" is not the owner's quoted wording")]
    [InlineData("element", "unknown element type circle")]
    [InlineData("motion", "unknown motion type spin")]
    [InlineData("field", "unknown field colour")]
    [InlineData("missingField", "missing field length; the drawing program fills no default")]
    [InlineData("reference", "it names the undefined element nowhere")]
    [InlineData("stillMoves", "a still graphic carries motion")]
    [InlineData("kind", "its kind is Still, and the owner's motion list it quotes names GFX-05")]
    [InlineData("shared", "its shared value theta = 39 differs from 38")]
    [InlineData("address", "it holds an address-like text")]
    [InlineData("claims", "its claims [CL-05, CL-06] differ from the claims its row cites [CL-05]")]
    [InlineData("carried", "the claim record says CL-01 is carried by GFX-01, its row does not cite it, and no owner answer resolving it is cited")]
    [InlineData("duplicateId", "duplicate element id")]
    public async Task EachInjectedFaultIsRefusedByName(string fault, string expected)
    {
        var (material, specifications, sources) = await ItemOneAsync();
        switch (fault)
        {
            case "missing":
                specifications.Remove("gfx-04.json");
                break;
            case "extra":
                specifications["gfx-23.json"] = specifications["gfx-04.json"];
                break;
            case "row":
                Mutate(specifications, "gfx-04.json", root => root["row"]!["quote"] = "| `GFX-04` | an altered row | `CL-05` |");
                break;
            case "beat":
                Mutate(specifications, "gfx-04.json", root => root["beat"] = 5);
                break;
            case "unquoted":
                Mutate(specifications, "gfx-04.json", root => Elements(root).Add(Text("invented", "a label nobody recorded", "wave-2/item-001/narration.txt")));
                break;
            case "misquoted":
                Mutate(specifications, "gfx-04.json", root => Elements(root).OfType<JsonObject>().First(e => (string?)e["id"] == "howLong")["text"] = "how long it lasted");
                break;
            case "unadmitted":
                Mutate(specifications, "gfx-04.json", root => Elements(root).Add(Text("number", "twelve", "research/ceo-decision-record.md")));
                break;
            case "undisclosed":
                Mutate(specifications, "gfx-04.json", root => root.Remove("disclosure"));
                break;
            case "wording":
                Mutate(specifications, "gfx-04.json", root => root["disclosure"]!["text"] = "schematic");
                break;
            case "element":
                Mutate(specifications, "gfx-04.json", root => Elements(root).Add(new JsonObject { ["id"] = "c", ["type"] = "circle" }));
                break;
            case "motion":
                Mutate(specifications, "gfx-05.json", root => ((JsonArray)root["motion"]!).Add(new JsonObject { ["type"] = "spin", ["from"] = 0, ["to"] = 1 }));
                break;
            case "field":
                Mutate(specifications, "gfx-04.json", root => Elements(root).OfType<JsonObject>().First(e => (string?)e["id"] == "runA")["colour"] = "#FF0000");
                break;
            case "missingField":
                Mutate(specifications, "gfx-04.json", root => Elements(root).OfType<JsonObject>().First(e => (string?)e["id"] == "runA").Remove("length"));
                break;
            case "reference":
                Mutate(specifications, "gfx-05.json", root => ((JsonArray)root["motion"]!).Add(new JsonObject { ["type"] = "reveal", ["targets"] = new JsonArray("nowhere"), ["from"] = 0, ["to"] = 1 }));
                break;
            case "stillMoves":
                Mutate(specifications, "gfx-04.json", root => root["motion"] = new JsonArray(new JsonObject { ["type"] = "reveal", ["targets"] = new JsonArray("runA"), ["from"] = 0, ["to"] = 1 }));
                break;
            case "kind":
                Mutate(specifications, "gfx-05.json", root =>
                {
                    root["kind"] = "still";
                    root.Remove("motion");
                });
                break;
            case "shared":
                Mutate(specifications, "gfx-07.json", root => root["shared"]!["theta"] = 39);
                break;
            case "address":
                Mutate(specifications, "gfx-04.json", root => Elements(root).OfType<JsonObject>().First(e => (string?)e["id"] == "howLong")["source"] = "https://example.invalid/narration");
                break;
            case "claims":
                Mutate(specifications, "gfx-04.json", root => root["claims"] = new JsonArray("CL-05", "CL-06"));
                break;
            case "carried":
                Mutate(specifications, "gfx-01.json", root => root.Remove("alsoCarries"));
                break;
            case "duplicateId":
                Mutate(specifications, "gfx-04.json", root => Elements(root).Add(new JsonObject { ["id"] = "runA", ["type"] = "point", ["x"] = 1, ["y"] = 1, ["r"] = 1, ["filled"] = true }));
                break;
        }

        var result = GraphicSpecificationCheck.Check(material, specifications, sources);
        Assert.False(result.Passed);
        Assert.Contains(result.Findings, f => f.Contains(expected, StringComparison.Ordinal));
    }

    /// <summary>A duplicate position in two files is refused naming both.</summary>
    [Fact]
    public async Task ADuplicatePositionIsRefused()
    {
        var (material, specifications, sources) = await ItemOneAsync();
        Mutate(specifications, "gfx-04.json", root => root["position"] = "GFX-03");
        var result = GraphicSpecificationCheck.Check(material, specifications, sources);
        Assert.Contains(result.Findings, f => f.Contains("gfx-04.json: its position GFX-03 is not the material's GFX-04", StringComparison.Ordinal));
    }

    /// <summary>Markup inside a quotation is data: the check treats it as characters, and only an exact quotation passes.</summary>
    [Fact]
    public async Task MarkupInAQuotationIsCheckedAsCharacters()
    {
        var (material, specifications, sources) = await ItemOneAsync();
        Mutate(specifications, "gfx-04.json", root => Elements(root).Add(Text("markup", "<b>how long it lasts</b>", "wave-2/item-001/narration.txt")));
        var result = GraphicSpecificationCheck.Check(material, specifications, sources);
        Assert.Contains(result.Findings, f => f.Contains("gfx-04.json element markup: its quotation is not found", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // Font coverage
    // -----------------------------------------------------------------------

    /// <summary>A face lacking one character refuses a specification drawing it, naming the character, its code point and the graphic.</summary>
    [Fact]
    public async Task AFaceLackingOneCharacterRefusesTheGraphicDrawingItNamingTheCharacter()
    {
        var (material, specifications, sources) = await ItemOneAsync();
        var result = GraphicSpecificationCheck.Check(material, specifications, sources);
        var lacking = FontCoverage.Read(MediaCompany.Graphics.Fixtures.FixtureFace.Build("Fixture Sans", "Regular", null, new HashSet<int> { 0x03B8 }));
        var drawn = result.Specifications.SelectMany(s => s.Texts.Where(t => !t.Italic).Select(t => ($"{s.FileName} element {t.Element}", t.Text)));

        var findings = FontCoverage.Uncovered(lacking, drawn);
        Assert.Contains(findings, f => f.Contains("gfx-05.json element thetaLabel", StringComparison.Ordinal) && f.Contains("'θ' (U+03B8)", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, f => !f.Contains("U+03B8", StringComparison.Ordinal));
    }

    [Fact]
    public void AFaceIsReadForItsFamilyAndItsCharacterMap()
    {
        var face = FontCoverage.Read(MediaCompany.Graphics.Fixtures.FixtureFace.Build("Fixture Sans", "Italic"));
        Assert.Equal("Fixture Sans", face.Family);
        Assert.Equal("Italic", face.Subfamily);
        Assert.True(face.Covers('θ'));
        Assert.False(face.Covers(0x4E00));
        Assert.Throws<FontRefusedException>(() => FontCoverage.Read(Encoding.ASCII.GetBytes("not a face at all")));
    }

    /// <summary>
    /// THE APPROVED FACES COVER EVERY CHARACTER ITEM 001 DRAWS, read from the installed Inter files' own bytes. Needs the installed
    /// renderer; not-run, saying why, where its path is unset.
    /// </summary>
    [RequiresInstalledRendererFact]
    public async Task TheInstalledFacesCoverEveryCharacterOfItemOne()
    {
        var (material, specifications, sources) = await ItemOneAsync();
        var result = GraphicSpecificationCheck.Check(material, specifications, sources);
        var regular = FontCoverage.Read(await File.ReadAllBytesAsync(InstalledRenderer.RegularFace!));
        var italic = FontCoverage.Read(await File.ReadAllBytesAsync(InstalledRenderer.ItalicFace!));

        Assert.Equal("Inter", regular.Family);
        Assert.Empty(FontCoverage.Uncovered(regular, result.Specifications.SelectMany(s => s.Texts.Where(t => !t.Italic).Select(t => (s.Position, t.Text)))
            .Append(("disclosure", "schematic, not to scale"))));
        Assert.Empty(FontCoverage.Uncovered(italic, result.Specifications.SelectMany(s => s.Texts.Where(t => t.Italic).Select(t => (s.Position, t.Text)))));
        Assert.True(regular.Covers('θ') && regular.Covers('°') && regular.Covers('~') && regular.Covers('–'));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    internal static async Task<(ItemMaterial Material, Dictionary<string, byte[]> Specifications, Dictionary<string, string> Sources)> ItemOneAsync()
    {
        var loaded = await ItemPackageLoader.LoadAsync(Repository, "wave-2/item-001/item-material.json", CancellationToken.None);
        var folder = Path.Combine(Repository, "wave-2", "item-001", "graphics");
        var specifications = Directory.GetFiles(folder).ToDictionary(f => Path.GetFileName(f), f => File.ReadAllBytes(f), StringComparer.Ordinal);
        var sources = GraphicSpecificationCheck.CitedSources(specifications)
            .Where(p => File.Exists(Path.Combine(Repository, p)))
            .ToDictionary(p => p, p => ItemPackageLoader.Normalised(File.ReadAllBytes(Path.Combine(Repository, p))), StringComparer.Ordinal);
        return (loaded.Material, specifications, sources);
    }

    private static void Mutate(Dictionary<string, byte[]> specifications, string file, Action<JsonObject> change)
    {
        var root = JsonNode.Parse(specifications[file])!.AsObject();
        change(root);
        specifications[file] = Encoding.UTF8.GetBytes(root.ToJsonString(new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }

    private static JsonArray Elements(JsonObject root) => (JsonArray)root["elements"]!;

    private static JsonObject Text(string id, string text, string source) => new()
    {
        ["id"] = id, ["type"] = "text", ["role"] = "label", ["text"] = text, ["source"] = source, ["quote"] = text,
        ["x"] = 10, ["y"] = 10, ["anchor"] = "start", ["size"] = "label",
    };
}

/// <summary>
/// Where the installed approved renderer, browser and faces are, for the tests marked as needing them: named by the environment
/// variable MEDIACOMPANY_GRAPHICS_ROOT (the installed graphics folder), never written in production source.
/// </summary>
internal static class InstalledRenderer
{
    internal static string? Root { get; } = Environment.GetEnvironmentVariable("MEDIACOMPANY_GRAPHICS_ROOT") is { Length: > 0 } root && Directory.Exists(root) ? root : null;

    internal static string? RegularFace => Root is null ? null : Path.Combine(Root, "font", "Inter-4.1", "extras", "ttf", "Inter-Regular.ttf");

    internal static string? ItalicFace => Root is null ? null : Path.Combine(Root, "font", "Inter-4.1", "extras", "ttf", "Inter-Italic.ttf");
}

/// <summary>A fact that needs the installed renderer; not-run, naming why, where its path is unset.</summary>
public sealed class RequiresInstalledRendererFactAttribute : FactAttribute
{
    public RequiresInstalledRendererFactAttribute()
    {
        if (InstalledRenderer.Root is null)
        {
            Skip = "not-run: needs the installed renderer; set MEDIACOMPANY_GRAPHICS_ROOT to the installed graphics folder to run it.";
        }
    }
}
