using System.Net;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MediaCompany.Pilot.Tests;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>The in-process cases: names, outcome display, the request guard, the live check, conversion arguments, setup, static checks.</summary>
internal static class UnitCases
{
    /// <summary>One example per banned class of the held list, as a field value.</summary>
    public static readonly IReadOnlyList<(string ClassId, string Term, string Text)> ClassExamples =
    [
        ("a", "giảm giá", "bình giữ nhiệt đang giảm giá"),
        ("b", "tốt nhất", "bình giữ nhiệt tốt nhất"),
        ("c", "sức khỏe", "tốt cho sức khỏe"),
        ("d", "mình đã dùng", "mình đã dùng bình này"),
        ("e", "tiktok", "giống bình bán trên tiktok"),
        ("f", "so với", "nhẹ so với bình thép"),
        ("g", "chỉ hôm nay", "chỉ hôm nay còn hàng"),
    ];

    public static void Add(Runner r)
    {
        r.Add("T-001", "approver text has exactly the two forms; a blank name or one with a control character, '(', ')' or ';' is refused", () =>
        {
            Expect.Equal("Nguyễn Văn An (CEO)", ApproverText.Compose(" Nguyễn Văn An ", null), "CEO presses");
            Expect.Equal("Nguyễn Văn An (CEO; nhân viên Trần Thị Bình bấm duyệt theo chỉ thị bằng văn bản của CEO)",
                ApproverText.Compose("Nguyễn Văn An", "Trần Thị Bình"), "employee presses on written instruction");
            var decomposed = "Nguye\u0302\u0303n Va\u0306n An";
            Expect.Equal("Nguyễn Văn An (CEO)", ApproverText.Compose(decomposed, null), "NFC");
            foreach (var bad in new[] { "", "   ", "An (CEO)", "An; B", "An\u0007" })
            {
                var refusal = Expect.Throws<WebRefusal>(() => ApproverText.Compose("Nguyễn Văn An", bad), $"employee name \"{bad}\"");
                Expect.True(refusal.Code is "name-missing" or "name-invalid", $"code for \"{bad}\" is {refusal.Code}");
                Expect.Throws<WebRefusal>(() => ApproverText.Compose(bad, null), $"CEO name \"{bad}\"");
            }

            r.Note("2 forms composed; 5 bad names refused for each of CEO and employee");
        });

        r.Add("T-002", "every one of the 14 operator-guide codes is shown with a reason and a next step; exit statuses map to the five kinds", () =>
        {
            Expect.Equal(14, Outcomes.Guide.Count, "guide codes");
            foreach (var code in Outcomes.Guide.Keys)
            {
                var view = Outcomes.FromToolchain(new ToolchainRun("produce", 2, $"TỪ CHỐI [{code}]: thử\n", 0));
                Expect.Equal(code == "package-held" ? Outcomes.Held : Outcomes.Refused, view.Kind, $"kind of {code}");
                Expect.Equal(code, view.Code, "code");
                Expect.True(view.Reason.Length > 0 && view.NextStep.Length > 0, $"{code} has a reason and a next step");
                Expect.Equal($"TỪ CHỐI [{code}]: thử\n", view.Line, "the toolchain line is shown unchanged");
            }

            Expect.Equal(Outcomes.Done, Outcomes.FromToolchain(new ToolchainRun("check", 0, "ĐẠT [check]: x\n", 0)).Kind, "exit 0");
            Expect.Equal(Outcomes.Held, Outcomes.FromToolchain(new ToolchainRun("produce", 2, "GIỮ LẠI [package-held]: gói\n", 0)).Kind, "exit 2 held");
            Expect.Equal(Outcomes.ToolFailure, Outcomes.FromToolchain(new ToolchainRun("produce", 3, "LỖI CÔNG CỤ [tool-failed]: x\n", 0)).Kind, "exit 3");
            Expect.Equal(Outcomes.AppFault, Outcomes.FromToolchain(new ToolchainRun("produce", 1, "Cách dùng\n", 0)).Kind, "exit 1");
            Expect.Equal(Outcomes.ToolFailure, Outcomes.FromToolchain(new ToolchainRun("produce", -1, "LỖI CÔNG CỤ [tool-failed]: quá giờ\n", 0)).Kind, "timeout");
            var unknown = Outcomes.FromToolchain(new ToolchainRun("check", 2, "TỪ CHỐI [header-field-missing]: Thiếu\n", 0));
            Expect.True(unknown.Reason.Length > 0 && unknown.NextStep.Length > 0, "a code outside the guide still has a reason and next step");
            var web = Outcomes.FromWeb(new WebRefusal("recording-too-long", "dài"));
            Expect.True(web.Reason.Length > 0 && web.NextStep.Length > 0, "an app refusal with a guide code takes the guide's words");
            r.Note("14 guide codes; exits 0, 1, 2, 2 held, 3, timeout mapped");
        });

        r.Add("T-003", "request guard: loopback peer, exact host, exact origin and per-start token on every POST; other methods refused", () =>
        {
            var guard = new GuardState();
            Expect.Equal(503, RequestGuard.Check("GET", "127.0.0.1:0", null, null, IPAddress.Loopback, guard)?.Status, "not ready before the port is known");
            guard.Port = 47817;
            var loop = IPAddress.Loopback;
            var cases = new (string Method, string? Host, string? Origin, string? Token, IPAddress Peer, string? Code)[]
            {
                ("GET", "127.0.0.1:47817", null, null, loop, null),
                ("HEAD", "127.0.0.1:47817", null, null, loop, null),
                ("GET", "localhost:47817", null, null, loop, "host-not-allowed"),
                ("GET", "evil.example:47817", null, null, loop, "host-not-allowed"),
                ("GET", "127.0.0.1:47817", null, null, IPAddress.Parse("192.168.1.20"), "peer-not-local"),
                ("POST", "127.0.0.1:47817", null, guard.Token, loop, "origin-not-allowed"),
                ("POST", "127.0.0.1:47817", "http://evil.example", guard.Token, loop, "origin-not-allowed"),
                ("POST", "127.0.0.1:47817", "http://localhost:47817", guard.Token, loop, "origin-not-allowed"),
                ("POST", "127.0.0.1:47817", "null", guard.Token, loop, "origin-not-allowed"),
                ("POST", "127.0.0.1:47817", guard.Origin, null, loop, "token-not-valid"),
                ("POST", "127.0.0.1:47817", guard.Origin, new string('0', 64), loop, "token-not-valid"),
                ("POST", "127.0.0.1:47817", guard.Origin, guard.Token, loop, null),
                ("PUT", "127.0.0.1:47817", guard.Origin, guard.Token, loop, "method-not-allowed"),
                ("DELETE", "127.0.0.1:47817", guard.Origin, guard.Token, loop, "method-not-allowed"),
                ("OPTIONS", "127.0.0.1:47817", guard.Origin, guard.Token, loop, "method-not-allowed"),
            };
            foreach (var c in cases)
            {
                Expect.Equal(c.Code, RequestGuard.Check(c.Method, c.Host, c.Origin, c.Token, c.Peer, guard)?.Code, $"{c.Method} host {c.Host} origin {c.Origin}");
            }

            Expect.True(new GuardState().Token != guard.Token, "each start has its own token");
            r.Note($"{cases.Length} guard cases");
        });

        r.Add("T-004", "live check: each of the 7 banned classes is named with its term and class, as the check verb names it; clean text has none", () =>
        {
            var root = PilotTestEnvironment.NewFolder("w-live");
            var pilot = Path.Combine(root, "pilot");
            var settings = PilotTestEnvironment.WriteSettings(root, PilotTestEnvironment.Settings(pilot));
            var folder = Fixtures.Stills(Path.Combine(root, "in"));
            foreach (var (classId, term, text) in ClassExamples)
            {
                var findings = FactSteps.LiveCheck([new LiveField("Đầu bảng (phân loại)", text, false)]);
                Expect.True(findings.Any(f => f.ClassId == classId && f.Term == term), $"class {classId} finding for \"{text}\": {string.Join("; ", findings)}");
                var sheet = Fixtures.Sheet("V-01");
                sheet["classification"] = text;
                var path = Path.Combine(folder, "bang.json");
                File.WriteAllText(path, Fixtures.Text(sheet), new UTF8Encoding(false));
                var (exit, output) = Direct.Run("check", "--settings", settings, "--facts", path);
                Expect.Equal(2, exit, $"check verb exit for class {classId}");
                Expect.Equal("banned-term", Direct.CodeOf(output), "check verb code");
                var first = findings[0];
                Expect.Contains($"\"{first.Term}\" (nhóm {first.ClassId}: {first.ClassName})", Direct.FirstLine(output), "the check verb names the live check's first term and class");
            }

            foreach (var clean in new[] { "bình giữ nhiệt inox 500 ml", "dung tích 500 ml", "nhất định giữ nhiệt", "thép không gỉ 304" })
            {
                Expect.Equal(0, FactSteps.LiveCheck([new LiveField("Giá trị", clean, false)]).Count, $"clean \"{clean}\"");
            }

            var person = FactSteps.LiveCheck([new LiveField("Dòng F1, câu thay thế", "mình thấy bình giữ nhiệt nhẹ", true)]);
            Expect.True(person.Any(f => f.Term == "mình"), "an override's first-person word is named");
            Expect.Equal(0, FactSteps.LiveCheck([new LiveField("Dòng F1, giá trị", "mình thấy", false)]).Count(f => f.Term == "mình"), "first person is checked in overrides only");
            r.Note("7 classes matched by the check verb run directly; 4 clean strings; override first person");
        });

        r.Add("T-005", "the take conversion argument list: one local input, first audio stream, mono 16-bit PCM WAV, bit-exact, no metadata, capped at 60 s", () =>
        {
            var args = MediaSteps.ConversionArguments(@"C:\t\take.webm", @"C:\t\out.wav", 60.0);
            var text = string.Join(' ', args);
            foreach (var expected in new[] { "-nostdin", "-map 0:a:0", "-t 60.000", "-ac 1", "-ar 48000", "-c:a pcm_s16le", "-map_metadata -1", "-fflags +bitexact", "-flags:a +bitexact", "-f wav" })
            {
                Expect.Contains(expected, text, "conversion arguments");
            }

            Expect.Equal(1, args.Count(a => a == "-i"), "one input");
            Expect.True(!text.Contains("lavfi", StringComparison.Ordinal) && !text.Contains("http", StringComparison.Ordinal), "no device or network input");
            var input = Array.IndexOf(args.ToArray(), "-i");
            Expect.True(input >= 2 && args[input - 2] == "-protocol_whitelist" && args[input - 1] == "file", "the toolchain's input rule: file protocol only, before the input");
            r.Note(text);
        });

        r.Add("T-006", "fact sheet text is stored NFC-normalised", () =>
        {
            var sheet = Fixtures.Sheet("V-01");
            sheet["productName"] = "Bi\u0300nh giu\u031B\u0303 nhie\u0323\u0302t";
            var stored = Encoding.UTF8.GetString(FactSteps.Normalise(Encoding.UTF8.GetBytes(Fixtures.Text(sheet))));
            Expect.Contains("\"Bình giữ nhiệt\"", stored, "NFC form");
            Expect.True(stored.IsNormalized(NormalizationForm.FormC), "whole file NFC");
        });

        r.Add("T-007", "setup composes settings equal to the example's CEO numbers; a relative or in-repository folder and a missing tool are refused, naming the item", () =>
        {
            var repo = PilotTestEnvironment.RepositoryRoot;
            var pilot = Path.Combine(PilotTestEnvironment.NewFolder("w-setup"), "pilot");
            var plan = Setup.Compose(repo, pilot, Setup.ToolFolders());
            var example = JsonNode.Parse(File.ReadAllText(Path.Combine(repo, Setup.ExampleRelativePath)))!.AsObject();
            var composed = JsonNode.Parse(JsonSerializer.Serialize(plan.Settings, PilotSettings.Json))!.AsObject();
            var compared = 0;
            foreach (var (key, value) in example)
            {
                if (key is "repositoryRoot" or "pilotFolder" or "mediaTool" or "probeTool" or "face")
                {
                    continue;
                }

                Expect.True(SameValues(value, composed[key]), $"setting {key}: example {value?.ToJsonString()}, composed {composed[key]?.ToJsonString()}");
                compared++;
            }

            Expect.Equal(example["mediaTool"]!["sha256"]!.GetValue<string>(), plan.Settings.MediaTool.Sha256, "ffmpeg digest is the approved one");
            Expect.Equal(example["probeTool"]!["sha256"]!.GetValue<string>(), plan.Settings.ProbeTool.Sha256, "ffprobe digest is the approved one");
            Expect.Equal("setup-pilot-folder", Expect.Throws<WebRefusal>(() => Setup.Compose(repo, "relative\\pilot", Setup.ToolFolders()), "relative").Code, "relative folder");
            Expect.Equal("pilot-folder-in-repository", Expect.Throws<PilotRefusal>(() => Setup.Compose(repo, Path.Combine(repo, "pilot"), Setup.ToolFolders()), "in repo").Code, "in-repository folder");
            var empty = PilotTestEnvironment.NewFolder("w-notools");
            var missing = Expect.Throws<WebRefusal>(() => Setup.Compose(repo, pilot, [empty]), "no tools");
            Expect.Equal("setup-tool-missing", missing.Code, "missing tool");
            Expect.Contains("ffmpeg.exe", missing.Message, "the missing tool is named");
            r.Note($"{compared} CEO settings equal the example; toolchain {plan.ToolchainPath}");
        });

        r.Add("T-008", "static: no package reference in the app or its tests, no external address in the pages, no media file added, the toolchain never references the app", () =>
        {
            var repo = PilotTestEnvironment.RepositoryRoot;
            var projects = new[]
            {
                Path.Combine(repo, "src", "MediaCompany.Pilot.Web", "MediaCompany.Pilot.Web.csproj"),
                Path.Combine(repo, "tests", "MediaCompany.Pilot.Web.Tests", "MediaCompany.Pilot.Web.Tests.csproj"),
            };
            foreach (var project in projects)
            {
                Expect.True(!File.ReadAllText(project).Contains("PackageReference", StringComparison.Ordinal), $"{project} has no PackageReference");
            }

            var assets = projects.Select(p => Path.Combine(Path.GetDirectoryName(p)!, "obj", "project.assets.json")).Where(File.Exists).ToList();
            foreach (var asset in assets)
            {
                var libraries = JsonNode.Parse(File.ReadAllText(asset))!["libraries"]!.AsObject();
                Expect.True(libraries.All(l => l.Value!["type"]!.GetValue<string>() == "project"), $"{asset} restores projects only: {string.Join(", ", libraries.Select(l => l.Key))}");
            }

            var pages = Directory.EnumerateFiles(Path.Combine(repo, "src", "MediaCompany.Pilot.Web", "Pages")).ToList();
            var external = new Regex(@"(https?:)?//[a-z0-9.-]+\.[a-z]{2,}|@import|\burl\(", RegexOptions.IgnoreCase);
            foreach (var page in pages)
            {
                var hit = external.Match(File.ReadAllText(page));
                Expect.True(!hit.Success, $"{Path.GetFileName(page)} references \"{hit.Value}\"");
            }

            var media = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".wav", ".mp3", ".m4a", ".aac", ".flac", ".ogg", ".mp4", ".mov", ".m4v", ".mkv", ".webm", ".raw", ".ttf", ".woff", ".woff2" };
            var folders = new[] { Path.Combine(repo, "src", "MediaCompany.Pilot.Web"), Path.Combine(repo, "tests", "MediaCompany.Pilot.Web.Tests") };
            var found = folders.SelectMany(f => Directory.EnumerateFiles(f, "*", SearchOption.AllDirectories))
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Where(f => media.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
            Expect.Equal(0, found.Count, $"media files: {string.Join(", ", found)}");

            using (var pilot = new PEReader(File.OpenRead(typeof(PilotSettings).Assembly.Location)))
            {
                var reader = pilot.GetMetadataReader();
                var references = reader.AssemblyReferences.Select(h => reader.GetString(reader.GetAssemblyReference(h).Name)).ToList();
                Expect.True(!references.Contains("MediaCompany.Pilot.Web"), "the toolchain does not reference the app");
                r.Note("toolchain references: " + string.Join(", ", references));
            }

            using (var web = new PEReader(File.OpenRead(typeof(Routes).Assembly.Location)))
            {
                var reader = web.GetMetadataReader();
                var types = reader.TypeReferences.Select(h => reader.GetTypeReference(h)).Select(t => $"{reader.GetString(t.Namespace)}.{reader.GetString(t.Name)}").ToList();
                foreach (var forbidden in new[] { "MediaCompany.Pilot.Producer", "MediaCompany.Pilot.Program", "MediaCompany.Pilot.Composition" })
                {
                    Expect.True(!types.Contains(forbidden), $"the app references {forbidden}: produce never runs in the app's process");
                }

                // MC-18 (design decision D-001; review correction CR-002): the product read is the app's one outbound client, and the
                // guard fails closed. (1) Every networking type the app's metadata references, nested types included, is one of the read
                // sender's declared set. (2) No networking assembly outside the declared set is referenced. (3) No source file other than
                // ReadSender.cs names a networking namespace or an outbound type, written plainly, fully qualified, aliased or as a string.
                var full = NetworkingGuard.TypeReferences(reader);
                var undeclared = NetworkingGuard.Undeclared(full);
                Expect.Equal(0, undeclared.Count, $"networking types outside the read sender's declared set: {string.Join(", ", undeclared)}");
                var assemblies = reader.AssemblyReferences.Select(h => reader.GetString(reader.GetAssemblyReference(h).Name)).ToList();
                var networkAssemblies = assemblies.Where(a => a.StartsWith("System.Net", StringComparison.Ordinal)).ToList();
                var otherAssemblies = networkAssemblies.Where(a => !NetworkingGuard.DeclaredAssemblies.Contains(a)).ToList();
                Expect.Equal(0, otherAssemblies.Count, $"networking assemblies outside the declared set: {string.Join(", ", otherAssemblies)}");
                var sources = Directory.EnumerateFiles(Path.Combine(repo, "src", "MediaCompany.Pilot.Web"), "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                    .Select(f => (File: Path.GetFileName(f), Text: File.ReadAllText(f))).ToList();
                var outside = NetworkingGuard.OutsideSender(sources);
                Expect.Equal(0, outside.Count, $"networking named outside ReadSender.cs: {string.Join("; ", outside)}");
                Expect.True(NetworkingGuard.SourceHits(sources.Single(s => s.File == NetworkingGuard.SenderFile).Text).Count > 0, "the scan sees the read sender's own networking (it is not blind)");

                // The guard catches what it must: a disallowed use in any other file, and an undeclared type in the metadata.
                NetworkingGuard.SelfTest();
                var used = full.Where(NetworkingGuard.IsNetworking).Distinct().Order(StringComparer.Ordinal).ToList();
                Expect.Equal(string.Join(", ", NetworkingGuard.Declared.Order(StringComparer.Ordinal)), string.Join(", ", used), "the declared set is exactly what the app references (no unused allowance)");
                r.Note($"app networking references ({used.Count}), all in the read sender's declared set: {string.Join(", ", used)}");
                r.Note($"networking assemblies: {string.Join(", ", networkAssemblies)}; {sources.Count} source files scanned, networking named only in {NetworkingGuard.SenderFile}");
                r.Note($"guard self-test: {NetworkingGuard.SelfTestSources.Count} disallowed uses in another file caught, {NetworkingGuard.SelfTestTypes.Count} undeclared types caught, benign source passed");
                r.Note($"app type references checked: {types.Count}");
            }

            var guard = NetworkGuard.Check();
            Expect.True(guard.Passed, "the toolchain's own network guard still passes: " + string.Join("; ", guard.Findings));
            r.Note("toolchain network guard closure: " + string.Join(", ", guard.Closure));
        });
    }

    /// <summary>
    /// Every value the example file holds is held, equal, by the composed settings. Members the settings record computes from them
    /// (an image panel's rectangle) are written as well and are not in the example; they are not CEO numbers and are not compared.
    /// </summary>
    internal static bool SameValues(JsonNode? example, JsonNode? composed) => (example, composed) switch
    {
        (JsonObject e, JsonObject c) => e.All(p => c.ContainsKey(p.Key) && SameValues(p.Value, c[p.Key])),
        (JsonArray e, JsonArray c) => e.Count == c.Count && e.Zip(c).All(z => SameValues(z.First, z.Second)),
        _ => JsonNode.DeepEquals(example, composed),
    };
}

/// <summary>
/// THE NETWORKING GUARD OF CASE 008 (MC-18 design decision D-001; review correction CR-002). The app has one outbound client, the
/// product read in ReadSender.cs. The guard fails closed three ways: a networking type the app's metadata references must be one the
/// read sender declares; a networking assembly the app references must be one the read sender needs; and no source file other than
/// ReadSender.cs may name a networking namespace or an outbound type, whether written plainly, fully qualified (global:: included),
/// through a using alias or a using static, or inside a string for reflection. Comments are scanned too: the guard prefers a false
/// alarm to a miss.
/// </summary>
internal static partial class NetworkingGuard
{
    public const string SenderFile = "ReadSender.cs";

    /// <summary>Namespaces whose every type is networking.</summary>
    public static readonly IReadOnlyList<string> NetworkingNamespaces =
    [
        "System.Net.Http", "System.Net.Sockets", "System.Net.WebSockets", "System.Net.Security", "System.Net.Quic", "System.Net.NetworkInformation",
        "System.Net.Mail", "System.Net.PeerToPeer", "System.Net.Cache",
    ];

    /// <summary>Types of System.Net itself that resolve names, open connections, or configure a client for them.</summary>
    public static readonly IReadOnlySet<string> NetworkingTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "System.Net.Dns", "System.Net.IPHostEntry", "System.Net.WebClient", "System.Net.WebRequest", "System.Net.HttpWebRequest",
        "System.Net.FtpWebRequest", "System.Net.FileWebRequest", "System.Net.WebResponse", "System.Net.HttpWebResponse", "System.Net.WebProxy",
        "System.Net.IWebProxy", "System.Net.CookieContainer", "System.Net.HttpListener", "System.Net.ServicePointManager", "System.Net.ServicePoint",
        "System.Net.DecompressionMethods", "System.Net.CredentialCache", "System.Net.NetworkCredential",
    };

    /// <summary>The read sender's declared set: the networking types ReadSender.cs uses, and nothing else.</summary>
    public static readonly IReadOnlySet<string> Declared = new HashSet<string>(StringComparer.Ordinal)
    {
        "System.Net.Dns", "System.Net.DecompressionMethods", "System.Net.IWebProxy",
        "System.Net.Http.HttpClient", "System.Net.Http.HttpMessageInvoker", "System.Net.Http.HttpMessageHandler", "System.Net.Http.SocketsHttpHandler",
        "System.Net.Http.SocketsHttpConnectionContext", "System.Net.Http.HttpRequestMessage", "System.Net.Http.HttpResponseMessage",
        "System.Net.Http.HttpMethod", "System.Net.Http.HttpContent", "System.Net.Http.HttpCompletionOption", "System.Net.Http.HttpVersionPolicy",
        "System.Net.Http.HttpRequestException", "System.Net.Http.Headers.HttpHeaders", "System.Net.Http.Headers.HttpRequestHeaders",
        "System.Net.Http.Headers.HttpResponseHeaders", "System.Net.Http.Headers.HttpContentHeaders", "System.Net.Http.Headers.MediaTypeHeaderValue",
        "System.Net.Sockets.Socket", "System.Net.Sockets.SocketType", "System.Net.Sockets.ProtocolType", "System.Net.Sockets.AddressFamily",
        "System.Net.Sockets.NetworkStream", "System.Net.Sockets.SocketException",
        "System.Net.Security.SslClientAuthenticationOptions", "System.Net.Security.RemoteCertificateValidationCallback",
        "System.Net.Security.SslPolicyErrors",
    };

    /// <summary>The networking assemblies the read sender needs (and the address types the MC-17 listener uses).</summary>
    public static readonly IReadOnlySet<string> DeclaredAssemblies = new HashSet<string>(StringComparer.Ordinal)
    {
        "System.Net.Primitives", "System.Net.Http", "System.Net.Sockets", "System.Net.Security", "System.Net.NameResolution",
    };

    /// <summary>Disallowed uses in a file other than ReadSender.cs, each of which the source scan must catch.</summary>
    public static readonly IReadOnlyList<string> SelfTestSources =
    [
        "var c = new System.Net.Sockets.TcpClient(\"shopee.vn\", 443);",
        "using var s = new global::System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);",
        "using System.Net;\nvar a = await Dns.GetHostAddressesAsync(host);",
        "using static System.Net.Dns;\nvar a = GetHostEntry(host);",
        "using System.Net.WebSockets;\nvar w = new ClientWebSocket();",
        "using N = System . Net . Sockets;\nvar u = new N.UdpClient();",
        "var u = new UdpClient();",
        "using var h = new HttpClient();",
        "var t = Type.GetType(\"System.Net.Sockets.Socket\");",
        "var r = WebRequest.Create(address);",
        "await new Ping().SendPingAsync(host);",
        "var x = new System.Net.Http.SocketsHttpHandler();",
        "var l = new TcpListener(IPAddress.Loopback, 0);",
        "var m = new SmtpClient(\"mail.example\");",
    ];

    /// <summary>Undeclared networking types, each of which the metadata check must catch.</summary>
    public static readonly IReadOnlyList<string> SelfTestTypes =
    [
        "System.Net.Sockets.TcpClient", "System.Net.Sockets.UdpClient", "System.Net.Sockets.TcpListener", "System.Net.WebSockets.ClientWebSocket",
        "System.Net.NetworkInformation.Ping", "System.Net.WebClient", "System.Net.WebProxy", "System.Net.CookieContainer",
        "System.Net.Http.HttpClientHandler", "System.Net.Quic.QuicConnection", "System.Net.Security.SslStream", "System.Net.Http.HttpClient+Nested",
    ];

    public static bool IsNetworking(string fullName) =>
        NetworkingTypes.Contains(Outer(fullName)) || NetworkingNamespaces.Any(ns => fullName.StartsWith(ns + ".", StringComparison.Ordinal));

    /// <summary>The networking type references outside the declared set.</summary>
    public static IReadOnlyList<string> Undeclared(IEnumerable<string> typeReferences) =>
        [.. typeReferences.Where(IsNetworking).Where(t => !Declared.Contains(t)).Distinct().Order(StringComparer.Ordinal)];

    /// <summary>Each networking name the source text holds.</summary>
    public static IReadOnlyList<string> SourceHits(string source) =>
        [.. Denied().Matches(source ?? string.Empty).Select(m => m.Value).Distinct()];

    /// <summary>"file: names" for every file other than ReadSender.cs that names networking.</summary>
    public static IReadOnlyList<string> OutsideSender(IEnumerable<(string File, string Text)> sources) =>
        [.. sources.Where(s => s.File != SenderFile).Select(s => (s.File, Hits: SourceHits(s.Text))).Where(s => s.Hits.Count > 0)
            .Select(s => $"{s.File}: {string.Join(", ", s.Hits)}")];

    /// <summary>Every type reference of the assembly by its full name; a nested type carries its enclosing type ("Outer+Inner").</summary>
    public static IReadOnlyList<string> TypeReferences(MetadataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        string Name(TypeReferenceHandle handle)
        {
            var type = reader.GetTypeReference(handle);
            var name = reader.GetString(type.Name);
            if (type.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                return Name((TypeReferenceHandle)type.ResolutionScope) + "+" + name;
            }

            var space = reader.GetString(type.Namespace);
            return space.Length == 0 ? name : $"{space}.{name}";
        }

        return [.. reader.TypeReferences.Select(Name)];
    }

    /// <summary>Fails the case when the guard would miss a disallowed use, or would flag a benign one.</summary>
    public static void SelfTest()
    {
        foreach (var source in SelfTestSources)
        {
            var caught = OutsideSender([("Routes.cs", source), (SenderFile, "using var h = new HttpClient();")]);
            Expect.True(caught.Count == 1 && caught[0].StartsWith("Routes.cs: ", StringComparison.Ordinal), $"the source scan misses \"{source}\" in Routes.cs");
        }

        Expect.Equal(0, OutsideSender([(SenderFile, string.Join("\n", SelfTestSources))]).Count, "the read sender itself may name networking");
        var benign = "using System.Net;\nvar a = IPAddress.Loopback;\nvar e = WebUtility.HtmlEncode(text);\nvar p = new IPEndPoint(a, 0);\n// tệp, ảnh, trang\n";
        Expect.Equal(0, SourceHits(benign).Count, $"the source scan flags benign code: {string.Join(", ", SourceHits(benign))}");
        foreach (var type in SelfTestTypes)
        {
            Expect.Equal(type, string.Join(",", Undeclared([type, "System.Net.IPAddress", "System.Net.WebUtility", "System.Net.Http.HttpClient"])), $"the metadata check misses {type}");
        }

        Expect.Equal(0, Undeclared(Declared).Count, "the declared set passes");
    }

    private static string Outer(string fullName) => fullName.Split('+')[0];

    [GeneratedRegex(
        @"\bSystem\s*\.\s*Net\s*\.\s*(Http|Sockets|WebSockets|Security|Quic|NetworkInformation|Mail|PeerToPeer|Cache)\b"
        + @"|\b(Dns|IPHostEntry|TcpClient|TcpListener|UdpClient|Socket|SocketAsyncEventArgs|NetworkStream|SslStream|NegotiateStream|HttpClient"
        + @"|HttpClientHandler|SocketsHttpHandler|WinHttpHandler|HttpMessageHandler|DelegatingHandler|HttpMessageInvoker|HttpRequestMessage"
        + @"|HttpResponseMessage|ClientWebSocket|WebSocket|WebClient|WebRequest|HttpWebRequest|FtpWebRequest|FileWebRequest|WebProxy|IWebProxy"
        + @"|CookieContainer|HttpListener|ServicePointManager|Ping|SmtpClient|QuicConnection|QuicListener|ConnectCallback)\b",
        RegexOptions.CultureInvariant, 2000)]
    private static partial Regex Denied();
}
