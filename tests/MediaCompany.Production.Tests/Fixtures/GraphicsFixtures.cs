using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using MediaCompany.Production;

namespace MediaCompany.Graphics.Fixtures;

/// <summary>
/// A face file made in the test (the graphics change, decision D-009 of its design): a TrueType container holding only a
/// character map (format 12 of the Windows Unicode subtable) and a name table, which is all the coverage check reads. It maps
/// the code-point ranges given, minus any excluded, so a test can make a face lacking exactly one character.
/// </summary>
internal static class FixtureFace
{
    /// <summary>The ranges a fixture face maps by default: Basic Latin, Latin-1 and Extended-A, Greek, and the general punctuation block.</summary>
    internal static readonly (int First, int Last)[] DefaultRanges = [(0x20, 0x7E), (0xA0, 0x17F), (0x370, 0x3FF), (0x2000, 0x206F)];

    internal static byte[] Build(string family, string subfamily, IEnumerable<(int First, int Last)>? ranges = null, IReadOnlySet<int>? excluded = null)
    {
        var points = (ranges ?? DefaultRanges).SelectMany(r => Enumerable.Range(r.First, r.Last - r.First + 1))
            .Where(p => excluded is null || !excluded.Contains(p)).Distinct().Order().ToArray();
        var groups = new List<(int Start, int End)>();
        foreach (var point in points)
        {
            if (groups.Count > 0 && groups[^1].End == point - 1)
            {
                groups[^1] = (groups[^1].Start, point);
            }
            else
            {
                groups.Add((point, point));
            }
        }

        // cmap: version, one subtable (3, 10) at offset 12, then format 12.
        var subtable = new byte[16 + (12 * groups.Count)];
        BinaryPrimitives.WriteUInt16BigEndian(subtable, 12);
        BinaryPrimitives.WriteUInt32BigEndian(subtable.AsSpan(4), (uint)subtable.Length);
        BinaryPrimitives.WriteUInt32BigEndian(subtable.AsSpan(12), (uint)groups.Count);
        var glyph = 1u;
        for (var g = 0; g < groups.Count; g++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(subtable.AsSpan(16 + (g * 12)), (uint)groups[g].Start);
            BinaryPrimitives.WriteUInt32BigEndian(subtable.AsSpan(20 + (g * 12)), (uint)groups[g].End);
            BinaryPrimitives.WriteUInt32BigEndian(subtable.AsSpan(24 + (g * 12)), glyph);
            glyph += (uint)(groups[g].End - groups[g].Start + 1);
        }

        var cmap = new byte[12 + subtable.Length];
        BinaryPrimitives.WriteUInt16BigEndian(cmap.AsSpan(2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(cmap.AsSpan(4), 3);
        BinaryPrimitives.WriteUInt16BigEndian(cmap.AsSpan(6), 10);
        BinaryPrimitives.WriteUInt32BigEndian(cmap.AsSpan(8), 12);
        subtable.CopyTo(cmap, 12);

        // name: family (1), subfamily (2), version (5), Windows Unicode US English.
        var names = new[] { (1, family), (2, subfamily), (5, "Version 1.000; a fixture face made in the test") };
        var strings = names.Select(n => Encoding.BigEndianUnicode.GetBytes(n.Item2)).ToArray();
        var name = new byte[6 + (12 * names.Length) + strings.Sum(s => s.Length)];
        BinaryPrimitives.WriteUInt16BigEndian(name.AsSpan(2), (ushort)names.Length);
        BinaryPrimitives.WriteUInt16BigEndian(name.AsSpan(4), (ushort)(6 + (12 * names.Length)));
        var offset = 0;
        for (var i = 0; i < names.Length; i++)
        {
            var record = 6 + (12 * i);
            BinaryPrimitives.WriteUInt16BigEndian(name.AsSpan(record), 3);
            BinaryPrimitives.WriteUInt16BigEndian(name.AsSpan(record + 2), 1);
            BinaryPrimitives.WriteUInt16BigEndian(name.AsSpan(record + 4), 0x0409);
            BinaryPrimitives.WriteUInt16BigEndian(name.AsSpan(record + 6), (ushort)names[i].Item1);
            BinaryPrimitives.WriteUInt16BigEndian(name.AsSpan(record + 8), (ushort)strings[i].Length);
            BinaryPrimitives.WriteUInt16BigEndian(name.AsSpan(record + 10), (ushort)offset);
            strings[i].CopyTo(name, 6 + (12 * names.Length) + offset);
            offset += strings[i].Length;
        }

        // The container: the offset table and two table records.
        var font = new byte[12 + 32 + cmap.Length + name.Length];
        BinaryPrimitives.WriteUInt32BigEndian(font, 0x00010000);
        BinaryPrimitives.WriteUInt16BigEndian(font.AsSpan(4), 2);
        void Record(int at, string tag, int tableOffset, int length)
        {
            Encoding.ASCII.GetBytes(tag).CopyTo(font, at);
            BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(at + 8), (uint)tableOffset);
            BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(at + 12), (uint)length);
        }

        Record(12, "cmap", 44, cmap.Length);
        Record(28, "name", 44 + cmap.Length, name.Length);
        cmap.CopyTo(font, 44);
        name.CopyTo(font, 44 + cmap.Length);
        return font;
    }
}

/// <summary>
/// A fixture graphics installation made in the test, OUTSIDE the repository (the graphics change, decision D-003 of its design):
/// a renderer folder with package files, a browser folder holding a file with a version resource, a font folder with two
/// fixture faces, and a "Node" folder holding the graphics stand-in executable; four manifests in the installed format, each
/// listing its files relative to its folder; and the settings section naming them with their expected hashes computed now.
/// What the stand-in does is chosen by the behaviour file under the renderer's packages.
/// </summary>
internal sealed class GraphicsFixture : IDisposable
{
    private static readonly Lazy<GraphicsFixture> SharedFixture = new(() => new GraphicsFixture("deterministic"));

    public GraphicsFixture(string behaviour, string? root = null, IReadOnlySet<int>? excludedFromRegular = null)
    {
        Root = root ?? Path.Combine(Path.GetTempPath(), "mediacompany-graphics-fixture-" + Guid.NewGuid().ToString("N"));
        Renderer = Path.Combine(Root, "remotion");
        Browser = Path.Combine(Root, "browser");
        Font = Path.Combine(Root, "font");
        Node = Path.Combine(Root, "node");
        Manifests = Path.Combine(Root, "manifests");
        foreach (var folder in new[] { Renderer, Browser, Font, Node, Manifests })
        {
            Directory.CreateDirectory(folder);
        }

        Write(Path.Combine(Renderer, "package.json"), """{"name":"fixture-graphics-renderer","private":true}""");
        Write(Path.Combine(Renderer, "node_modules", "remotion", "package.json"), """{"name":"remotion","version":"0.0.0-fixture"}""");
        Write(Path.Combine(Renderer, "node_modules", "@remotion", "renderer", "package.json"), """{"name":"@remotion/renderer","version":"0.0.0-fixture"}""");
        Write(Path.Combine(Renderer, "node_modules", "@remotion", "bundler", "package.json"), """{"name":"@remotion/bundler","version":"0.0.0-fixture"}""");
        Write(Path.Combine(Renderer, "node_modules", "standin-behaviour.txt"), behaviour);

        // A browser file carrying a fixed version resource (signature 0xFEEF04BD, structure version, then 1.2.3.4).
        BrowserExecutable = Path.Combine(Browser, "fixture-browser.bin");
        var browser = new byte[64];
        BinaryPrimitives.WriteUInt32LittleEndian(browser.AsSpan(16), 0xFEEF04BD);
        BinaryPrimitives.WriteUInt32LittleEndian(browser.AsSpan(20), 0x00010000);
        BinaryPrimitives.WriteUInt32LittleEndian(browser.AsSpan(24), (1u << 16) | 2u);
        BinaryPrimitives.WriteUInt32LittleEndian(browser.AsSpan(28), (3u << 16) | 4u);
        File.WriteAllBytes(BrowserExecutable, browser);

        RegularFace = Path.Combine(Font, "fixture-regular.ttf");
        ItalicFace = Path.Combine(Font, "fixture-italic.ttf");
        // The regular face is also the one the contact sheet's labels are drawn in by the media tool, so where the platform's
        // font is present (as the media tool's tests need it) a copy of it serves; a face lacking a character is made in the test.
        var platformFont = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (excludedFromRegular is null && File.Exists(platformFont))
        {
            File.Copy(platformFont, RegularFace);
        }
        else
        {
            File.WriteAllBytes(RegularFace, FixtureFace.Build("Fixture Sans", "Regular", null, excludedFromRegular));
        }

        File.WriteAllBytes(ItalicFace, FixtureFace.Build("Fixture Sans", "Italic"));

        // The stand-in executable and the files its launcher needs, copied OUT of the repository's build output.
        var built = Path.GetDirectoryName(typeof(GraphicsFixture).Assembly.Location)!;
        foreach (var file in Directory.GetFiles(built, "MediaCompany.GraphicsStandIn.*"))
        {
            File.Copy(file, Path.Combine(Node, Path.GetFileName(file)), overwrite: true);
        }

        NodeExecutable = Path.Combine(Node, "MediaCompany.GraphicsStandIn.exe");
        RendererManifest = Manifest("remotion", Renderer, Directory.GetFiles(Renderer, "*", SearchOption.AllDirectories));
        BrowserManifest = Manifest("browser", Browser, Directory.GetFiles(Browser, "*", SearchOption.AllDirectories));
        FontManifest = Manifest("font", Font, Directory.GetFiles(Font, "*", SearchOption.AllDirectories));
        NodeManifest = Manifest("node", Node, [NodeExecutable]);
    }

    /// <summary>The fixture most tests share: deterministic, made once per test process.</summary>
    public static GraphicsFixture Shared => SharedFixture.Value;

    public string Root { get; }

    public string Renderer { get; }

    public string Browser { get; }

    public string Font { get; }

    public string Node { get; }

    public string Manifests { get; }

    public string BrowserExecutable { get; }

    public string RegularFace { get; }

    public string ItalicFace { get; }

    public string NodeExecutable { get; }

    public InstalledManifest RendererManifest { get; private set; }

    public InstalledManifest BrowserManifest { get; private set; }

    public InstalledManifest FontManifest { get; private set; }

    public InstalledManifest NodeManifest { get; private set; }

    /// <summary>The graphics section over this fixture: the Design Gate's ruled bounds, concurrency 1, and a fixture licence.</summary>
    public GraphicsSettings Settings() => new()
    {
        Renderer = RendererManifest,
        Browser = BrowserManifest,
        Font = FontManifest,
        Node = NodeManifest,
        NodeExecutable = NodeExecutable,
        BrowserExecutable = BrowserExecutable,
        RegularFace = RegularFace,
        ItalicFace = ItalicFace,
        RenderScript = "graphics/render.mjs",
        DrawingFolder = "graphics/drawing",
        StyleFile = "config/graphics-style.json",
        Concurrency = 1,
        FrameTimeout = TimeSpan.FromSeconds(30),
        BundleBound = TimeSpan.FromSeconds(120),
        RenderFixed = TimeSpan.FromSeconds(120),
        RenderPerFrame = TimeSpan.FromSeconds(0.5),
        HashBound = TimeSpan.FromSeconds(300),
        Licence = new ConfiguredLicence("a fixture licence", "the test that made this fixture"),
    };

    /// <summary>Re-writes the four manifests over the files now present (after a test changed the fixture on purpose).</summary>
    public void Rewrite()
    {
        RendererManifest = Manifest("remotion", Renderer, Directory.GetFiles(Renderer, "*", SearchOption.AllDirectories));
        BrowserManifest = Manifest("browser", Browser, Directory.GetFiles(Browser, "*", SearchOption.AllDirectories));
        FontManifest = Manifest("font", Font, Directory.GetFiles(Font, "*", SearchOption.AllDirectories));
        NodeManifest = Manifest("node", Node, [NodeExecutable]);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A process the test ended may still be releasing a file; the folder is under the temporary folder.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    internal static string Sha256(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    private InstalledManifest Manifest(string name, string folder, IEnumerable<string> files)
    {
        var lines = files.Order(StringComparer.Ordinal)
            .Select(f => $"{Sha256(f)} *{Path.GetRelativePath(folder, f).Replace('\\', '/')}\n");
        var path = Path.Combine(Manifests, name + ".sha256");
        File.WriteAllText(path, string.Concat(lines));
        return new InstalledManifest(path, folder, Sha256(path));
    }

    private static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
