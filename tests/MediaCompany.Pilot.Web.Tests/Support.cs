using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MediaCompany.Pilot.Tests;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>One answer of the app: the HTTP status and the JSON body.</summary>
internal sealed record Answer(int Status, JsonElement Body)
{
    public bool Ok => Body.ValueKind == JsonValueKind.Object && Body.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True;

    public string Code => Outcome is { } o && o.TryGetProperty("code", out var c) ? c.GetString() ?? string.Empty : string.Empty;

    public string Kind => Outcome is { } o && o.TryGetProperty("kind", out var k) ? k.GetString() ?? string.Empty : string.Empty;

    public string Line => Outcome is { } o && o.TryGetProperty("line", out var l) ? l.GetString() ?? string.Empty : string.Empty;

    public JsonElement? Outcome => Body.ValueKind == JsonValueKind.Object && Body.TryGetProperty("outcome", out var o) && o.ValueKind == JsonValueKind.Object ? o : null;

    public JsonElement this[string name] => Body.GetProperty(name);

    public override string ToString() => $"{Status} {Body.GetRawText()}";
}

/// <summary>
/// The web app started as its own process, exactly as the shortcut starts it but with a system-chosen port, its own application
/// folder and desktop folder, and no browser. The test talks to it over HTTP like the page does: Origin and token on every POST.
/// </summary>
internal sealed class AppProcess : IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly List<string> _output = [];

    private AppProcess(Process process, string appFolder, string origin, string token)
    {
        Process = process;
        AppFolder = appFolder;
        Origin = origin;
        Token = token;
        Http = new HttpClient(new SocketsHttpHandler { UseProxy = false, AllowAutoRedirect = false }) { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromMinutes(15) };
    }

    public Process Process { get; }
    public string AppFolder { get; }
    public string Origin { get; }
    public string Token { get; }
    public HttpClient Http { get; }

    public int Port => new Uri(Origin).Port;

    public IReadOnlyList<string> Output
    {
        get
        {
            lock (_output)
            {
                return [.. _output];
            }
        }
    }

    public static string WebExecutable =>
        Path.Combine(PilotTestEnvironment.RepositoryRoot, "src", "MediaCompany.Pilot.Web", "bin", Configuration, "net10.0", "MediaCompany.Pilot.Web.exe");

    public static string ToolchainExecutable =>
        Path.Combine(PilotTestEnvironment.RepositoryRoot, "src", "MediaCompany.Pilot", "bin", Configuration, "net10.0", "MediaCompany.Pilot.exe");

    public static string Configuration =>
        AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug";

    public static Process Launch(IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(WebExecutable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        return Process.Start(start) ?? throw new InvalidOperationException("the web app did not start");
    }

    public static AppProcess Start(string appFolder, string desktopFolder, AddressHarness? harness, params string[] extra)
    {
        var process = Launch(["--port", "0", "--app-folder", appFolder, "--desktop-folder", desktopFolder, "--no-browser", .. extra]);
        harness?.Track(process);
        var lines = new List<string>();
        string? ready = null;
        var reading = Task.Run(() =>
        {
            while (process.StandardOutput.ReadLine() is { } line)
            {
                lock (lines)
                {
                    lines.Add(line);
                }

                if (line.StartsWith(Web.Program.ReadyPrefix, StringComparison.Ordinal))
                {
                    ready = line[Web.Program.ReadyPrefix.Length..].Trim();
                    return;
                }
            }
        });
        if (!reading.Wait(TimeSpan.FromSeconds(30)) || ready is null)
        {
            process.Kill(entireProcessTree: true);
            throw new CaseFailure($"the web app printed no ready line: {string.Join(" | ", lines)} {process.StandardError.ReadToEnd()}");
        }

        var page = new HttpClient(new SocketsHttpHandler { UseProxy = false }).GetStringAsync(ready + "/").GetAwaiter().GetResult();
        var token = Regex.Match(page, "name=\"mc-token\" content=\"([0-9a-f]{64})\"").Groups[1].Value;
        var app = new AppProcess(process, appFolder, ready, token);
        lock (app._output)
        {
            app._output.AddRange(lines);
        }

        _ = Task.Run(() =>
        {
            while (process.StandardOutput.ReadLine() is { } line)
            {
                lock (app._output)
                {
                    app._output.Add(line);
                }
            }
        });
        _ = process.StandardError.ReadToEndAsync();
        return app;
    }

    public Answer Get(string path)
    {
        using var response = Http.GetAsync(path).GetAwaiter().GetResult();
        return Read(response);
    }

    public Answer Post(string path, object? body = null) =>
        Send(path, new StringContent(JsonSerializer.Serialize(body ?? new { }, Json), Encoding.UTF8, "application/json"));

    public Answer PostBytes(string path, byte[] bytes, string contentType = "application/octet-stream")
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        return Send(path, content);
    }

    public Answer PostFile(string path, string file, string contentType = "application/octet-stream") => PostBytes(path, File.ReadAllBytes(file), contentType);

    public Answer Send(string path, HttpContent content, string? origin = "", string? token = "", string method = "POST", string? host = null)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = content };
        if (origin is not null)
        {
            request.Headers.TryAddWithoutValidation("Origin", origin.Length == 0 ? Origin : origin);
        }

        if (token is not null)
        {
            request.Headers.TryAddWithoutValidation(GuardState.TokenHeader, token.Length == 0 ? Token : token);
        }

        if (host is not null)
        {
            request.Headers.Host = host;
        }

        using var response = Http.SendAsync(request).GetAwaiter().GetResult();
        return Read(response);
    }

    /// <summary>Starts produce and waits for the job to finish; returns the last progress answer.</summary>
    public Answer Produce(string code, TimeSpan limit)
    {
        var started = Post($"/api/produce?code={code}");
        if (!started.Ok)
        {
            return started;
        }

        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < limit)
        {
            Thread.Sleep(500);
            var progress = Get($"/api/progress?code={code}");
            if (progress.Ok && !progress["running"].GetBoolean())
            {
                return progress;
            }
        }

        throw new CaseFailure($"produce of {code} did not finish within {limit}");
    }

    private static Answer Read(HttpResponseMessage response)
    {
        var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        JsonElement body;
        try
        {
            body = text.Length == 0 ? default : JsonSerializer.Deserialize<JsonElement>(text);
        }
        catch (JsonException)
        {
            body = JsonSerializer.SerializeToElement(new { raw = text });
        }

        return new Answer((int)response.StatusCode, body);
    }

    public void Dispose()
    {
        Http.Dispose();
        if (!Process.HasExited)
        {
            Process.Kill(entireProcessTree: true);
            Process.WaitForExit(10000);
        }

        Process.Dispose();
    }
}

/// <summary>The toolchain run directly, as the operator guide runs it: the control every parity case compares the app against.</summary>
internal static class Direct
{
    public static (int Exit, string Output) Run(params string[] arguments)
    {
        var start = new ProcessStartInfo(AppProcess.ToolchainExecutable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(AppProcess.ToolchainExecutable)!,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        _ = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromMinutes(15)))
        {
            process.Kill(entireProcessTree: true);
            throw new CaseFailure($"the direct toolchain run {string.Join(' ', arguments)} did not finish");
        }

        return (process.ExitCode, output.Result);
    }

    public static string FirstLine(string output) => output.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n')[0];

    public static string CodeOf(string output) => Outcomes.CodeOf(output);
}

/// <summary>Fixtures drawn at test time: fact sheets as the page sends them, and media through the MC-16 fixture helpers.</summary>
internal static class Fixtures
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static JsonObject Sheet(string code, string pagePath = "/san-pham/binh-giu-nhiet-mau", int videoNumber = 1) => new()
    {
        ["formVersion"] = 1,
        ["videoCode"] = code,
        ["productName"] = "Bình giữ nhiệt mẫu 500 ml",
        ["subjectNoun"] = "bình giữ nhiệt",
        ["classification"] = "loại 500 ml màu xanh",
        ["pagePath"] = pagePath,
        ["shopNameAndMall"] = "Gian hàng mẫu, không phải Mall",
        ["readDate"] = "2026-10-26",
        ["niche"] = "đồ dùng nhà bếp",
        ["imagesUsed"] = new JsonArray("anh-1.png", "anh-2.png", "anh-3.png"),
        ["productVideoNumber"] = videoNumber,
        ["videoFocus"] = "dung tích",
        ["callToAction"] = 1,
        ["facts"] = new JsonArray(new JsonObject
        {
            ["label"] = "F1",
            ["subjectNoun"] = "bình giữ nhiệt",
            ["attribute"] = "dung tích",
            ["value"] = "500 ml",
            ["sourceSection"] = "Thông số: Dung tích",
            ["screenshot"] = "chup-1.png",
        }),
    };

    /// <summary>The four stills every sheet names, drawn once per folder.</summary>
    public static string Stills(string folder)
    {
        Directory.CreateDirectory(folder);
        PilotTestEnvironment.Image(folder, "anh-1.png", "0x3a5a7a");
        PilotTestEnvironment.Image(folder, "anh-2.png", "0x7a5a3a");
        PilotTestEnvironment.Image(folder, "anh-3.png", "0x3a7a5a");
        PilotTestEnvironment.Image(folder, "chup-1.png", "0x2a2a2a");
        return folder;
    }

    public static string Text(JsonNode node) => node.ToJsonString(Json);

    public static void CopyFolder(string from, string to, Func<string, bool>? keep = null)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.EnumerateFiles(from))
        {
            if (keep is null || keep(Path.GetFileName(file)))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
            }
        }
    }

    public static Dictionary<string, string> Hashes(string folder) =>
        Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToDictionary(f => f, MediaTool.Sha256File, StringComparer.OrdinalIgnoreCase)
            : [];
}
