using System.Net;
using System.Reflection;
using System.Text;

namespace MediaCompany.Pilot.Web;

/// <summary>
/// The page set (technical design module M-002): Vietnamese HTML, CSS and script embedded in this assembly and served from memory.
/// No external address, font or script is referenced. The page carries the per-start token in a meta element, readable only by a page
/// of this origin. In test mode only (a start-up switch the browser cannot set) the page also loads the recording-test driver.
/// </summary>
public sealed class PageAssets
{
    private static readonly IReadOnlyDictionary<string, string> Types = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["index.html"] = "text/html; charset=utf-8",
        ["app.css"] = "text/css; charset=utf-8",
        ["app.js"] = "text/javascript; charset=utf-8",
        ["test-driver.js"] = "text/javascript; charset=utf-8",
    };

    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public PageAssets()
    {
        var assembly = typeof(PageAssets).Assembly;
        foreach (var name in Types.Keys)
        {
            using var stream = assembly.GetManifestResourceStream($"page:{name}")
                ?? throw new InvalidOperationException($"the embedded page file {name} is missing");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            _files[name] = memory.ToArray();
        }
    }

    public static IReadOnlyList<string> Names => [.. Types.Keys];

    public string Text(string name) => Encoding.UTF8.GetString(_files[name]);

    public static string ContentType(string name) => Types[name];

    /// <summary>The page with the per-start token, and the test driver's script element in test mode only.</summary>
    public string Index(GuardState guard, bool testMode)
    {
        ArgumentNullException.ThrowIfNull(guard);
        var html = Text("index.html").Replace("{{TOKEN}}", WebUtility.HtmlEncode(guard.Token), StringComparison.Ordinal);
        return html.Replace("<!--TEST-DRIVER-->", testMode ? "<script src=\"/test-driver.js\" defer></script>" : string.Empty, StringComparison.Ordinal);
    }

    public byte[] Bytes(string name) => _files[name];
}
