using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediaCompany.Pilot.Tests;
using Microsoft.AspNetCore.Builder;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>
/// The web app started inside the test program through the internal start path (MC-18 design decision D-004), with the read fixture's
/// transport seam: the same routes, guard and pages as the shipped app, on a system-chosen 127.0.0.1 port, but every read goes to the
/// loopback fixture. The test talks to it over HTTP like the page does: Origin and token on every POST.
/// </summary>
internal sealed class ReadApp : IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly WebApplication _app;

    private ReadApp(WebApplication app, WebContext web, string token)
    {
        _app = app;
        Web = web;
        Origin = web.Guard.Origin;
        Token = token;
        Http = new HttpClient(new SocketsHttpHandler { UseProxy = false, AllowAutoRedirect = false }) { BaseAddress = new Uri(Origin), Timeout = TimeSpan.FromMinutes(5) };
    }

    public WebContext Web { get; }
    public string Origin { get; }
    public string Token { get; }
    public HttpClient Http { get; }

    public string AppFolder => Web.Options.AppFolder;

    public Workspace Workspace => Web.Workspace();

    public int Port => new Uri(Origin).Port;

    /// <summary>Starts the app on the given folders with the fixture's seam; with a pilot folder, setup is saved through the app's own route.</summary>
    public static ReadApp Start(string root, ReadFixture fixture, string? pilotFolder, int port = 0, bool testMode = false, string? resultPath = null, string? fixtureOrigin = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var options = new WebOptions
        {
            Port = port,
            AppFolder = Path.Combine(root, "app"),
            DesktopFolder = Path.Combine(root, "desktop"),
            OpenBrowser = false,
            TestMode = testMode,
            TestResultPath = resultPath,
        };
        Directory.CreateDirectory(options.AppFolder);
        var (app, web, exit) = MediaCompany.Pilot.Web.Program.Start(options, fixture.Transport with { FixtureOrigin = fixtureOrigin }).GetAwaiter().GetResult();
        if (app is null)
        {
            throw new CaseFailure($"the in-process app did not start (exit {exit})");
        }

        using var page = new HttpClient(new SocketsHttpHandler { UseProxy = false });
        var token = Regex.Match(page.GetStringAsync(web.Guard.Origin + "/").GetAwaiter().GetResult(), "name=\"mc-token\" content=\"([0-9a-f]{64})\"").Groups[1].Value;
        var started = new ReadApp(app, web, token);
        if (pilotFolder is not null && started.Web.Record is null)
        {
            var setup = started.Post("/api/setup", new { pilotFolder, ceoName = "Nguyễn Văn An", createShortcut = false });
            if (!setup.Ok)
            {
                started.Dispose();
                throw new CaseFailure($"setup of the in-process app: {setup}");
            }
        }

        return started;
    }

    public Answer Get(string path)
    {
        using var response = Http.GetAsync(path).GetAwaiter().GetResult();
        return Read(response);
    }

    public byte[] GetBytes(string path, out int status)
    {
        using var response = Http.GetAsync(path).GetAwaiter().GetResult();
        status = (int)response.StatusCode;
        return response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    }

    public Answer Post(string path, object? body = null) =>
        Send(path, new StringContent(JsonSerializer.Serialize(body ?? new { }, Json), Encoding.UTF8, "application/json"));

    public Answer PostBytes(string path, byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        return Send(path, content);
    }

    public Answer Send(string path, HttpContent content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        request.Headers.TryAddWithoutValidation("Origin", Origin);
        request.Headers.TryAddWithoutValidation(GuardState.TokenHeader, Token);
        using var response = Http.SendAsync(request).GetAwaiter().GetResult();
        return Read(response);
    }

    /// <summary>A JSON POST with the given Origin and token headers, each left out when null (review correction CR-005).</summary>
    public Answer PostAs(string path, object body, string? origin, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json"),
        };
        if (origin is not null)
        {
            request.Headers.TryAddWithoutValidation("Origin", origin);
        }

        if (token is not null)
        {
            request.Headers.TryAddWithoutValidation(GuardState.TokenHeader, token);
        }

        using var response = Http.SendAsync(request).GetAwaiter().GetResult();
        return Read(response);
    }

    /// <summary>One read click of the page, as the page sends it.</summary>
    public Answer Read(string code, string link) => Post($"/api/read?code={code}", new { link });

    public Answer Paste(string code, string text) => Post($"/api/paste?code={code}", new { text });

    public Answer Switch(bool on) => Post("/api/read-switch", new { on });

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
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

/// <summary>
/// THE FIXTURE HOST MODE (MC-18 design decision D-004): the test program, started with --fixture-host, runs the app through the internal
/// start path with the read fixture's seam, so step 1 can be shown in a browser against the code-generated fixture site. Only the test
/// program has this mode; the shipped app's options refuse the argument and its entry point never passes a seam. The app folder and
/// the pilot folder are fresh folders outside the repository unless given; setup is saved through the app's own route; the read switch
/// is on unless --switch off is given. The ready line names the app's address and the fixture's ports; the fixture links follow.
/// </summary>
internal static class FixtureHost
{
    public const string Argument = "--fixture-host";
    public const string ReadyPrefix = "MC18-FIXTURE-HOST ";
    public const int DefaultPort = 47900;

    public static int Run(string[] args)
    {
        var port = DefaultPort;
        var switchOn = true;
        string? root = null;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--port" when i + 1 < args.Length && int.TryParse(args[i + 1], out var number) && number is >= 0 and <= 65535:
                    port = number;
                    i++;
                    break;
                case "--switch" when i + 1 < args.Length && args[i + 1] is "on" or "off":
                    switchOn = args[i + 1] == "on";
                    i++;
                    break;
                case "--root" when i + 1 < args.Length:
                    root = Path.GetFullPath(args[i + 1]);
                    i++;
                    break;
                default:
                    Console.Out.WriteLine("Cách dùng: MediaCompany.Pilot.Web.Tests --fixture-host [--port <số>] [--switch on|off] [--root <thư-mục ngoài kho mã>]");
                    return 1;
            }
        }

        root ??= PilotTestEnvironment.NewFolder("fixture-host");
        Directory.CreateDirectory(root);
        using var fixture = new ReadFixture();
        using var app = ReadApp.Start(root, fixture, Path.Combine(root, "pilot"), port);
        if (!app.Switch(switchOn).Ok)
        {
            Console.Out.WriteLine("the read switch could not be set");
            return 1;
        }

        var pastePath = Path.Combine(root, "chu-trang-mau.txt");
        File.WriteAllText(pastePath, FixtureSite.GoodPasteText(), new UTF8Encoding(false));
        Console.Out.WriteLine($"{ReadyPrefix}{app.Origin} fixture-port={fixture.Port} silent-port={fixture.SilentPort} closed-port={fixture.ClosedPort} proxy-port={fixture.ProxyPort} switch={(switchOn ? "on" : "off")}");
        Console.Out.WriteLine($"Ứng dụng (chế độ fixture của chương trình kiểm thử): {app.Origin}/  thư mục: {root}");
        Console.Out.WriteLine("Đường dẫn để dán (mọi lần đọc chỉ tới fixture trên 127.0.0.1; không tới shopee.vn):");
        Console.Out.WriteLine($"  sản phẩm tốt:   {FixtureSite.GoodLink}");
        Console.Out.WriteLine($"  đường dẫn ngắn: {FixtureSite.ShortLink}");
        Console.Out.WriteLine($"  trang bị chặn:  {FixtureSite.Stops[0].Link}");
        Console.Out.WriteLine($"  trang CAPTCHA:  {FixtureSite.Stops[4].Link}");
        Console.Out.WriteLine($"  sản phẩm thứ hai: {FixtureSite.SecondLink}");
        Console.Out.WriteLine($"Chữ trang mẫu để dán: {pastePath}");
        Console.Out.WriteLine("Nhấn Ctrl+C để tắt.");
        Console.Out.Flush();

        using var done = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            done.Set();
        };
        done.Wait();
        return 0;
    }
}
