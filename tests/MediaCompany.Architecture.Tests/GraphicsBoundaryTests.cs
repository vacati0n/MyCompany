using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace MediaCompany.Architecture.Tests;

/// <summary>
/// The boundary assertions the graphics change adds (decision D-011 of its design). The delivered assertions keep every case:
/// one process starter and one writer across the production assemblies, now holding the graphics types too, and no subject
/// term in production code. These extend the scans to the drawing program and the render script, which run outside .NET, and
/// to the graphics types' reach. Each scan is shown to FIND its seeded violation in a probe, so a scan that found nothing
/// because it could see nothing would fail here.
/// </summary>
public sealed partial class BoundaryTests
{
    /// <summary>Every word a string literal of the drawing program may hold: the closed vocabulary, style keys and SVG keywords.</summary>
    private static readonly HashSet<string> DrawingVocabulary = new(StringComparer.Ordinal)
    {
        // Element, form and motion types, fields and roles of the specification format.
        "text", "point", "bearing", "path", "axis", "trace", "angle", "region", "panel", "blocks", "bars", "polyline", "smooth", "loop",
        "oscillation", "linear", "band", "sector", "spread", "cone", "ellipse", "box", "reveal", "rotate", "sequence", "grow", "follow",
        "type", "id", "form", "points", "arrow", "dashed", "filled", "cycles", "extents", "count", "gap", "boxWidth", "boxHeight", "length",
        "radius", "width", "height", "from", "to", "anchor", "size", "shading", "shade", "targets", "target", "sets", "accumulate",
        "fromAngle", "toAngle", "fromValue", "toValue", "property", "shared", "elements", "motion", "kind", "still", "frames", "fps",
        // The style file's keys.
        "colours", "background", "line", "secondary", "accent", "strokes", "thin", "heavy", "sizes", "small", "dash", "margin", "motionEnd", "family",
        // SVG, CSS and module keywords.
        "none", "round", "normal", "italic", "end", "url", "object", "loaded", "react", "remotion", "graphic", "js", "jsx",
    };

    private static readonly string[] DrawingImports = ["react", "remotion"];

    private static readonly string[] RenderScriptImports = ["node:fs", "node:path", "node:module"];

    private static readonly string[] RenderScriptAnchoredRequires = ["@remotion/bundler", "@remotion/renderer"];

    /// <summary>What neither the drawing program nor the render script may name: each a way to the network, a clock, chance, markup or a licence.</summary>
    private static readonly (string Name, Regex Pattern)[] ForbiddenInGraphicsSources =
    [
        ("a licence key", new Regex(@"licen[cs]eKey|apiKey", RegexOptions.IgnoreCase)),
        ("the still render function", new Regex(@"\brenderStill\b")),
        ("the media render function", new Regex(@"\brenderMedia\b")),
        ("a browser download", new Regex(@"\b(ensureBrowser|downloadBrowser|BrowserFetcher)\b")),
        ("a network call", new Regex(@"\b(fetch|XMLHttpRequest|WebSocket|EventSource)\s*\(|node:(http|https|net|dgram|tls)|['""](http|https|net)['""]")),
        ("an address", new Regex(@"(https?|wss?|ftp)://", RegexOptions.IgnoreCase)),
        ("a clock", new Regex(@"\bDate\b|performance\.now|process\.hrtime")),
        ("a random source", new Regex(@"Math\.random|crypto\.|getRandomValues|randomUUID")),
        ("raw markup", new Regex(@"dangerouslySetInnerHTML|innerHTML|outerHTML|insertAdjacentHTML|document\.write")),
        ("a CSS animation", new Regex(@"@keyframes|animation\s*:|transition\s*:")),
        ("bundler caching on", new Regex(@"enableCaching\s*:\s*true")),
        ("a fallback family", new Regex(@"sans-serif|serif|monospace|system-ui|Arial|Helvetica", RegexOptions.IgnoreCase)),
        ("an excluded speech client", new Regex(@"elevenlabs|openai-whisper|whisper", RegexOptions.IgnoreCase)),
    ];

    // -----------------------------------------------------------------------
    // The renderer is reached only through the one process starter
    // -----------------------------------------------------------------------

    /// <summary>Exactly one type realises the graphics renderer across every production assembly: the one process starter.</summary>
    [Fact]
    public void OnlyTheProcessStarterRealisesTheGraphicsRenderer()
    {
        var realising = ProductionAssemblies.Select(Load).SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(i => i.FullName == "MediaCompany.Application.Ports.IGraphicRenderer"))
            .Select(t => t.FullName!).ToArray();
        Assert.Equal(new[] { TheProcessStarter }, realising);
    }

    /// <summary>
    /// The drawing path reaches no capability gateway, vendor adapter or credential: none of the graphics types of the production
    /// assembly names a member of the capability or credential assemblies.
    /// </summary>
    [Fact]
    public void TheDrawingPathReachesNoCapabilityOrCredential()
    {
        var production = Load(Production);
        string[] graphicsTypes =
        [
            "MediaCompany.Production.GraphicsPlanner", "MediaCompany.Production.GraphicsPlan", "MediaCompany.Production.RendererInstallationVerifier",
            "MediaCompany.Production.ReviewGraphicsService", "MediaCompany.Production.HeldFiles",
        ];
        var reach = graphicsTypes.SelectMany(name => CapabilityReach(production.GetType(name, throwOnError: true)!)).ToArray();
        Assert.Empty(reach);
    }

    [Fact]
    public void TheCapabilityReachScanFindsAProbeNamingTheGatewayAndNamesIt()
    {
        Assert.NotEmpty(CapabilityReach(typeof(ProbeDrawingPathCallingAGateway)));
    }

    // -----------------------------------------------------------------------
    // The drawing program and the render script, which run outside .NET
    // -----------------------------------------------------------------------

    /// <summary>
    /// The drawing program, the render script and the style file hold no subject term of the item material, no item identifier
    /// and no graphic position identifier: a graphic's subject reaches the frame only from its specification.
    /// </summary>
    [Fact]
    public void TheGraphicsSourcesHoldNoSubjectTermNoItemAndNoPosition()
    {
        var (terms, item) = MaterialTerms();
        Assert.Empty(GraphicsSubjectHits(GraphicsSources().Append(Path.Combine(RepositoryRoot(), "config", "graphics-style.json")), terms, item));
    }

    [Fact]
    public void TheGraphicsSubjectScanFindsAProbeHoldingAPositionAndATerm()
    {
        var (terms, item) = MaterialTerms();
        var probe = WriteProbe("probe-subject.jsx", "const title = 'the waggle dance'; const where = 'GFX-08';");
        Assert.Equal(2, GraphicsSubjectHits([probe], terms, item).Count);
    }

    /// <summary>
    /// The drawing program draws no text of its own: no JSX text child, and every word inside its string literals belongs to the
    /// specification vocabulary, the style keys or the SVG keywords. Every drawn word comes from a specification.
    /// </summary>
    [Fact]
    public void TheDrawingProgramHoldsNoOnScreenLiteral()
    {
        var hits = DrawingFiles().SelectMany(OnScreenLiterals).ToArray();
        Assert.Empty(hits);
    }

    [Fact]
    public void TheOnScreenLiteralScanFindsAProbeWithATextChildAndAWord()
    {
        var probe = WriteProbe("probe-literal.jsx", "export const P = () => <text x={1}>a label typed in code</text>; const caption = 'distance';");
        var hits = OnScreenLiterals(probe);
        Assert.Contains(hits, h => h.Contains("text child", StringComparison.Ordinal));
        Assert.Contains(hits, h => h.Contains("distance", StringComparison.Ordinal));
    }

    /// <summary>The drawing program imports only the two packages the design lists, and its own files.</summary>
    [Fact]
    public void TheDrawingProgramImportsOnlyReactAndTheRenderer()
    {
        var foreign = DrawingFiles().SelectMany(f => Imports(f).Where(i => !i.StartsWith("./", StringComparison.Ordinal) && !DrawingImports.Contains(i)).Select(i => $"{f}: {i}")).ToArray();
        Assert.Empty(foreign);
    }

    /// <summary>
    /// The render script imports only Node's file, path and module modules, and loads only the bundler and the frame renderer
    /// through its anchored require.
    /// </summary>
    [Fact]
    public void TheRenderScriptImportsOnlyNodesFilePathAndModuleAndLoadsOnlyTheBundlerAndTheRenderer()
    {
        var script = RenderScript();
        Assert.Equal(RenderScriptImports.Order(StringComparer.Ordinal), Imports(script).Order(StringComparer.Ordinal));
        var loads = LoadRequire().Matches(File.ReadAllText(script)).Select(m => m.Groups["module"].Value).Distinct().Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(RenderScriptAnchoredRequires.Order(StringComparer.Ordinal), loads);
    }

    [Fact]
    public void TheImportScanFindsAProbeImportingAnotherPackage()
    {
        var probe = WriteProbe("probe-import.jsx", "import x from '@remotion/elevenlabs';\nimport y from 'node:https';");
        Assert.Equal(new[] { "@remotion/elevenlabs", "node:https" }, Imports(probe));
    }

    /// <summary>
    /// Neither the drawing program nor the render script names a licence key, the still or media render function, a browser
    /// download, a network call or an address, a clock, a random source, raw markup, a CSS animation, bundler caching on, a
    /// fallback font family or an excluded speech client.
    /// </summary>
    [Fact]
    public void TheGraphicsSourcesNameNoForbiddenApi()
    {
        var hits = GraphicsSources().SelectMany(ForbiddenHits).ToArray();
        Assert.Empty(hits);
    }

    [Fact]
    public void TheForbiddenApiScanFindsEverySeededViolation()
    {
        var probe = WriteProbe("probe-forbidden.mjs", string.Join("\n",
            "renderMedia({licenseKey: 'k'}); renderStill(); ensureBrowser(); fetch('https://example.invalid');",
            "const t = Date.now(); const r = Math.random(); el.innerHTML = s; const css = '@keyframes spin';",
            "bundle({enableCaching: true}); const f = 'sans-serif'; import w from '@remotion/openai-whisper';"));
        var found = ForbiddenHits(probe).Select(h => h.Split(':')[^1].Trim()).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(ForbiddenInGraphicsSources.Length, found.Count);
    }

    /// <summary>The style file is data: the palette, the strokes, the type sizes and the motion-end fraction, with no text to draw.</summary>
    [Fact]
    public void TheStyleFileHoldsOnlyTheDeclaredKeys()
    {
        using var style = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "config", "graphics-style.json")));
        Assert.Equal(
            new[] { "arrow", "colours", "dash", "family", "format", "margin", "motionEnd", "sizes", "strokes" },
            style.RootElement.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.Equal(
            new[] { "accent", "background", "line", "secondary", "shade", "text" },
            style.RootElement.GetProperty("colours").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
    }

    // -----------------------------------------------------------------------
    // The scans
    // -----------------------------------------------------------------------

    private static IEnumerable<string> GraphicsSources() => DrawingFiles().Append(RenderScript());

    private static IEnumerable<string> DrawingFiles() =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "graphics", "drawing")).Order(StringComparer.Ordinal);

    private static string RenderScript() => Path.Combine(RepositoryRoot(), "graphics", "render.mjs");

    private static IReadOnlyList<string> GraphicsSubjectHits(IEnumerable<string> files, IReadOnlyList<string> terms, string item)
    {
        var patterns = terms.Select(t => new Regex($@"\b{Regex.Escape(t)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .Append(new Regex(Regex.Escape(item), RegexOptions.IgnoreCase))
            .Append(PositionIdentifier())
            .ToArray();
        return files.SelectMany(f =>
        {
            var text = File.ReadAllText(f);
            return patterns.Where(p => p.IsMatch(text)).Select(p => $"{f}: {p}");
        }).ToArray();
    }

    /// <summary>Every JSX text child and every string-literal word outside the vocabulary, in one drawing file.</summary>
    private static IReadOnlyList<string> OnScreenLiterals(string file)
    {
        var text = LineComment().Replace(File.ReadAllText(file), string.Empty);
        var hits = new List<string>();
        foreach (Match child in JsxTextChild().Matches(text))
        {
            hits.Add($"{file}: a JSX text child \"{child.Groups["text"].Value.Trim()}\"");
        }

        foreach (Match literal in StringLiteral().Matches(text))
        {
            var value = TemplateHole().Replace(literal.Groups["value"].Value, string.Empty);
            foreach (Match word in WordOfTwo().Matches(value))
            {
                if (!DrawingVocabulary.Contains(word.Value))
                {
                    hits.Add($"{file}: the word \"{word.Value}\" in a string literal is outside the specification vocabulary");
                }
            }
        }

        return hits;
    }

    private static IReadOnlyList<string> Imports(string file) =>
        ImportFrom().Matches(File.ReadAllText(file)).Select(m => m.Groups["module"].Value).ToArray();

    private static IEnumerable<string> ForbiddenHits(string file)
    {
        var text = LineComment().Replace(File.ReadAllText(file), string.Empty);
        return ForbiddenInGraphicsSources.Where(f => f.Pattern.IsMatch(text)).Select(f => $"{file}: {f.Name}");
    }

    /// <summary>Every member of the capability or credential assemblies a type names in a compiled body or a declared field.</summary>
    private static IReadOnlyList<string> CapabilityReach(Type type)
    {
        static bool Forbidden(Type? t) => t is not null && ((t.Namespace ?? string.Empty).StartsWith("MediaCompany.Capability", StringComparison.Ordinal)
                                                            || (t.Namespace ?? string.Empty).StartsWith("MediaCompany.Credentials", StringComparison.Ordinal));
        var reach = new List<string>();
        foreach (var nested in new[] { type }.Concat(type.GetNestedTypes(Declared)))
        {
            foreach (var field in nested.GetFields(Declared).Where(f => Forbidden(f.FieldType)))
            {
                reach.Add($"{nested.FullName}.{field.Name}");
            }

            var bodies = nested.GetMethods(Declared).Where(m => m.DeclaringType == nested).Cast<MethodBase>().Concat(nested.GetConstructors(Declared));
            foreach (var member in bodies.SelectMany(m => IlOperands(m).OfType<MemberInfo>()))
            {
                if (Forbidden(member.DeclaringType) || (member is Type t && Forbidden(t)))
                {
                    reach.Add($"{nested.FullName} names {member.DeclaringType?.Name}.{member.Name}");
                }
            }
        }

        return reach;
    }

    private static string WriteProbe(string name, string text)
    {
        var path = Path.Combine(Path.GetTempPath(), "mediacompany-architecture-probe-" + Guid.NewGuid().ToString("N") + "-" + name);
        File.WriteAllText(path, text);
        return path;
    }

    [GeneratedRegex(@"\bGFX-\d{2}\b")]
    private static partial Regex PositionIdentifier();

    [GeneratedRegex(@"(?<!:)//[^\n]*")]
    private static partial Regex LineComment();

    [GeneratedRegex(@">(?<text>[^<>{}]*[A-Za-z][^<>{}]*)</")]
    private static partial Regex JsxTextChild();

    [GeneratedRegex(@"'(?<value>[^'\n]*)'|""(?<value>[^""\n]*)""|`(?<value>[^`]*)`")]
    private static partial Regex StringLiteral();

    [GeneratedRegex(@"\$\{[^}]*\}")]
    private static partial Regex TemplateHole();

    [GeneratedRegex(@"[A-Za-z]{2,}")]
    private static partial Regex WordOfTwo();

    [GeneratedRegex(@"^\s*import\s+(?:[^'""]*?\s+from\s+)?['""](?<module>[^'""]+)['""]", RegexOptions.Multiline)]
    private static partial Regex ImportFrom();

    [GeneratedRegex(@"\bload\(\s*['""](?<module>[^'""]+)['""]\s*\)")]
    private static partial Regex LoadRequire();
}

/// <summary>A probe on the drawing path that names the capability gateway.</summary>
internal static class ProbeDrawingPathCallingAGateway
{
    public static Type Gateway() => typeof(MediaCompany.Capability.ICapabilityGateway);
}
