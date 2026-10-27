using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace MediaCompany.Pilot.Web;

/// <summary>A route's answer: the body, the HTTP status, and the code and exit status the log records.</summary>
public sealed record Reply(object Body, int Status = 200, string? Code = null, int? Exit = null);

public sealed record SetupRequest(string PilotFolder, string CeoName, bool CreateShortcut);

public sealed record LiveCheckRequest(IReadOnlyList<LiveField> Fields);

public sealed record ScriptTicksRequest(string ScriptSha256, IReadOnlyList<int> Ticked);

public sealed record ChecklistTicksRequest(IReadOnlyList<string> Ticked);

public sealed record TimingRequest(IReadOnlyList<string> Lines);

public sealed record ApproveRequest(string Sha256, IReadOnlyList<string> Ticked, string PressedBy, string? EmployeeName);

/// <summary>
/// THE ROUTES (technical design, API impact). Read routes: the page and its files, setup status, the video list and status, produce
/// progress, the timing form, the checklist, the package video and frame. State-changing routes, all POST and all behind the request
/// guard's Origin and token check: setup save, shortcut, fact sheet save, live check, image upload, screenshot upload, script run,
/// script tick save, take upload, take accept, recording upload, clip upload, produce start, timing save, checklist tick save,
/// approval, verify. There is no route that sends a file anywhere.
/// </summary>
public static class Routes
{
    public static readonly IReadOnlyList<string> StateChanging =
    [
        "/api/setup", "/api/shortcut", "/api/facts", "/api/live-check", "/api/image", "/api/screenshot", "/api/script", "/api/script-ticks",
        "/api/take", "/api/take-accept", "/api/recording", "/api/clip", "/api/produce", "/api/timing", "/api/checklist-ticks", "/api/approve",
        "/api/verify",
    ];

    private const long SmallBody = 1L << 20;
    private const long ImageBody = 40L << 20;
    private const long TakeBody = 100L << 20;
    private const long RecordingBody = 400L << 20;
    private const long ClipBody = 4L << 30;

    public static void Map(WebApplication app, WebContext web)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(web);

        app.Use(async (http, next) =>
        {
            var watch = Stopwatch.StartNew();
            RequestGuard.SecurityHeaders(http.Response.Headers);
            var refusal = RequestGuard.Check(http.Request.Method, http.Request.Headers.Host.ToString() is { Length: > 0 } h ? h : null,
                http.Request.Headers.Origin.ToString() is { Length: > 0 } o ? o : null,
                http.Request.Headers[GuardState.TokenHeader].ToString() is { Length: > 0 } t ? t : null,
                http.Connection.RemoteIpAddress, web.Guard);
            if (refusal is { } refused)
            {
                http.Response.StatusCode = refused.Status;
                var view = Outcomes.FromWeb(new WebRefusal(refused.Code, refused.Message, refused.Status));
                await http.Response.WriteAsJsonAsync(new { ok = false, outcome = view }, WebJson.Requests);
                web.Log.Write(http.Request.Method, http.Request.Path, refused.Status, refused.Code, null, watch.Elapsed.TotalMilliseconds);
                return;
            }

            await next(http);
        });

        app.MapGet("/", (HttpContext http) => Results.Text(web.Pages.Index(web.Guard, web.Options.TestMode), "text/html; charset=utf-8"));
        foreach (var name in new[] { "app.js", "app.css" })
        {
            app.MapGet("/" + name, () => Results.Bytes(web.Pages.Bytes(name), PageAssets.ContentType(name)));
        }

        if (web.Options.TestMode)
        {
            app.MapGet("/test-driver.js", () => Results.Bytes(web.Pages.Bytes("test-driver.js"), PageAssets.ContentType("test-driver.js")));
            app.MapPost("/test/result", Answer(web, async http =>
            {
                using var reader = new StreamReader(http.Request.Body, Encoding.UTF8);
                var text = await reader.ReadToEndAsync();
                await File.WriteAllTextAsync(web.Options.TestResultPath!, text, new UTF8Encoding(false));
                return new Reply(new { ok = true });
            }));
        }

        app.MapGet("/api/setup", Answer(web, http => Task.FromResult(SetupStatus(web))));
        app.MapPost("/api/setup", Answer(web, http => SaveSetup(http, web)));
        app.MapPost("/api/shortcut", Answer(web, http => Task.FromResult(CreateShortcut(web))));
        app.MapGet("/api/videos", Answer(web, http => Task.FromResult(Videos(web))));
        app.MapGet("/api/video", Answer(web, http => Task.FromResult(VideoStatus(http, web))));
        app.MapPost("/api/live-check", Answer(web, http => LiveCheck(http)));
        app.MapPost("/api/facts", Answer(web, http => SaveFacts(http, web)));
        app.MapPost("/api/screenshot", Answer(web, http => UploadScreenshot(http, web)));
        app.MapPost("/api/image", Answer(web, http => UploadImage(http, web)));
        app.MapPost("/api/script", Answer(web, http => Task.FromResult(RunScript(http, web))));
        app.MapPost("/api/script-ticks", Answer(web, http => SaveScriptTicks(http, web)));
        app.MapPost("/api/take", Answer(web, http => UploadTake(http, web)));
        app.MapPost("/api/take-accept", Answer(web, http => Task.FromResult(AcceptTake(http, web))));
        app.MapPost("/api/recording", Answer(web, http => UploadRecording(http, web)));
        app.MapPost("/api/clip", Answer(web, http => UploadClip(http, web)));
        app.MapPost("/api/produce", Answer(web, http => Task.FromResult(StartProduce(http, web))));
        app.MapGet("/api/progress", Answer(web, http => Task.FromResult(Progress(http, web))));
        app.MapGet("/api/timing", Answer(web, http => Task.FromResult(TimingForm(http, web))));
        app.MapPost("/api/timing", Answer(web, http => SaveTiming(http, web)));
        app.MapGet("/api/checklist", Answer(web, http => Task.FromResult(ChecklistStatus(http, web))));
        app.MapPost("/api/checklist-ticks", Answer(web, http => SaveChecklistTicks(http, web)));
        app.MapPost("/api/approve", Answer(web, http => Approve(http, web)));
        app.MapPost("/api/verify", Answer(web, http => Task.FromResult(Verify(http, web))));
        app.MapGet("/api/input/recording", (HttpContext http) => RecordingFile(http, web));
        app.MapGet("/api/package/video", (HttpContext http) => PackageFile(http, web, video: true));
        app.MapGet("/api/package/frame", (HttpContext http) => PackageFile(http, web, video: false));
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // The answer wrapper: every refusal becomes a Vietnamese outcome; nothing is logged but route, status, code, exit and time.
    // -----------------------------------------------------------------------------------------------------------------------------

    private static RequestDelegate Answer(WebContext web, Func<HttpContext, Task<Reply>> action) => http => Handle(http, web, action);

    private static async Task Handle(HttpContext http, WebContext web, Func<HttpContext, Task<Reply>> action)
    {
        var watch = Stopwatch.StartNew();
        Reply reply;
        try
        {
            reply = await action(http);
        }
        catch (WebRefusal refusal)
        {
            reply = new Reply(new { ok = false, outcome = Outcomes.FromWeb(refusal) }, refusal.Status, refusal.Code);
        }
        catch (PilotRefusal refusal)
        {
            reply = new Reply(new { ok = false, outcome = Outcomes.FromRefusal(refusal) }, 400, refusal.Code);
        }
        catch (PilotToolFailure failure)
        {
            reply = new Reply(new { ok = false, outcome = Outcomes.FromToolFailure(failure) }, 500, "tool-failed");
        }
        catch (BadHttpRequestException bad) when (bad.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            var refusal = new WebRefusal("upload-too-large", "Tệp tải lên lớn quá mức ứng dụng nhận cho bước này. Chọn đúng tệp, hoặc xuất lại tệp nhỏ hơn.", 413);
            reply = new Reply(new { ok = false, outcome = Outcomes.FromWeb(refusal) }, 413, refusal.Code);
        }
        catch (JsonException)
        {
            var refusal = new WebRefusal("request-unreadable", "Yêu cầu gửi lên không đúng dạng. Tải lại trang (phím F5) rồi làm lại.");
            reply = new Reply(new { ok = false, outcome = Outcomes.FromWeb(refusal) }, 400, refusal.Code);
        }
        catch (IOException error)
        {
            var view = new OutcomeView(Outcomes.AppFault, null, "file-error", $"LỖI ỨNG DỤNG [file-error]: {error.Message}\n",
                "Ứng dụng không đọc hoặc ghi được một tệp trong thư mục thí điểm (có thể tệp đang mở ở chương trình khác).",
                "Đóng chương trình đang mở tệp đó rồi bấm lại; nếu vẫn lỗi, báo người phụ trách kỹ thuật kèm dòng trên.");
            reply = new Reply(new { ok = false, outcome = view }, 500, "file-error");
        }

        http.Response.StatusCode = reply.Status;
        await http.Response.WriteAsJsonAsync(reply.Body, WebJson.Requests);
        web.Log.Write(http.Request.Method, http.Request.Path, reply.Status, reply.Code, reply.Exit, watch.Elapsed.TotalMilliseconds);
    }

    private static Reply FromRun(ToolchainRun run, object? extra = null)
    {
        var outcome = Outcomes.FromToolchain(run);
        return new Reply(new { ok = run.ExitCode == PilotExit.Done, outcome, extra }, 200, outcome.Code.Length == 0 ? null : outcome.Code, run.ExitCode);
    }

    private static async Task<T> Body<T>(HttpContext http)
    {
        SetLimit(http, SmallBody);
        return await JsonSerializer.DeserializeAsync<T>(http.Request.Body, WebJson.Requests)
            ?? throw new WebRefusal("request-unreadable", "Yêu cầu gửi lên trống. Tải lại trang (phím F5) rồi làm lại.");
    }

    private static void SetLimit(HttpContext http, long limit)
    {
        if (http.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } feature)
        {
            feature.MaxRequestBodySize = limit;
        }
    }

    /// <summary>Streams the request body into a new file of the upload folder; an empty body is refused.</summary>
    private static async Task<string> SaveUpload(HttpContext http, Workspace workspace, string extension, long limit)
    {
        SetLimit(http, limit);
        Directory.CreateDirectory(workspace.UploadFolder);
        var path = Path.Combine(workspace.UploadFolder, $"tai-len-{Guid.NewGuid():N}{extension}");
        try
        {
            await using (var file = File.Create(path))
            {
                await http.Request.Body.CopyToAsync(file);
            }

            if (new FileInfo(path).Length == 0)
            {
                throw new WebRefusal("upload-empty", "Tệp tải lên trống. Chọn lại tệp rồi tải lên lại.");
            }

            return path;
        }
        catch
        {
            File.Delete(path);
            throw;
        }
    }

    private static string Query(HttpContext http, string name) => http.Request.Query[name].ToString();

    private static string CodeOf(HttpContext http, Workspace workspace) => workspace.Code(Query(http, "code"));

    // -----------------------------------------------------------------------------------------------------------------------------
    // Setup (design decision D-004)
    // -----------------------------------------------------------------------------------------------------------------------------

    private static Reply SetupStatus(WebContext web)
    {
        var record = web.Record;
        PilotSettings? example = null;
        string? problem = null;
        try
        {
            example = Setup.ReadExample(Path.Combine(Setup.RepositoryRoot(AppContext.BaseDirectory), Setup.ExampleRelativePath));
        }
        catch (WebRefusal refusal)
        {
            problem = refusal.Message;
        }

        var held = HeldData.Current;
        return new Reply(new
        {
            ok = true,
            configured = record is not null,
            pilotFolder = record?.PilotFolder ?? Setup.DefaultPilotFolder,
            ceoName = record?.CeoName,
            settingsPath = record?.SettingsPath,
            toolchainPath = record?.ToolchainPath,
            problem,
            ceoNumbers = example is null ? null : new
            {
                frame = $"{example.Frame.Width}x{example.Frame.Height}, {example.Frame.FrameRate} hình/giây",
                safeArea = $"trái {example.SafeArea.Left}, phải {example.SafeArea.Right}, trên {example.SafeArea.Top}, dưới {example.SafeArea.Bottom} điểm ảnh",
                captionFontSize = example.Caption.FontSize,
                overlayFontSize = example.Overlay.FontSize,
                clipToleranceSeconds = example.ClipToleranceSeconds,
                captionPageMinimumSeconds = example.CaptionPageMinimumSeconds,
                recordingMaximumSeconds = held.RecordingMaximumSeconds,
            },
        });
    }

    private static async Task<Reply> SaveSetup(HttpContext http, WebContext web)
    {
        var request = await Body<SetupRequest>(http);
        var ceo = ApproverText.CleanName(request.CeoName, "Tên CEO");
        var repository = Setup.RepositoryRoot(AppContext.BaseDirectory);
        var plan = Setup.Compose(repository, request.PilotFolder, Setup.ToolFolders());
        var settingsPath = Setup.WriteSettings(plan.Settings);
        var record = new AppRecord
        {
            FormVersion = AppRecord.KnownFormVersion,
            PilotFolder = plan.Settings.PilotFolder,
            SettingsPath = settingsPath,
            ToolchainPath = plan.ToolchainPath,
            ToolchainSha256 = plan.ToolchainSha256,
            Port = web.Options.Port is null or 0 ? web.Guard.Port : web.Options.Port.Value,
            CeoName = ceo,
        };
        AppRecordStore.Write(web.Options.AppFolder, record);
        web.Record = record;
        string? shortcut = null;
        var shortcutProblem = string.Empty;
        if (request.CreateShortcut)
        {
            try
            {
                shortcut = ShortcutPath(web);
            }
            catch (WebRefusal refusal)
            {
                // The settings are saved either way; the operator reads why the shortcut is missing and how to start the app.
                shortcutProblem = " " + refusal.Message;
            }
        }

        return new Reply(new
        {
            ok = true,
            message = $"Đã lưu cài đặt vào {settingsPath}. Các số của CEO lấy từ {plan.ExamplePath}; bộ công cụ ở {plan.ToolchainPath}.{shortcutProblem}",
            settingsPath,
            shortcut,
        });
    }

    private static Reply CreateShortcut(WebContext web)
    {
        _ = web.Workspace();
        return new Reply(new { ok = true, shortcut = ShortcutPath(web), message = "Đã tạo biểu tượng trên màn hình nền." });
    }

    private static string ShortcutPath(WebContext web) =>
        Shortcut.Create(web.Options.DesktopFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "MediaCompany.Pilot.Web.exe"));

    // -----------------------------------------------------------------------------------------------------------------------------
    // Videos and status
    // -----------------------------------------------------------------------------------------------------------------------------

    private static Reply Videos(WebContext web)
    {
        var workspace = web.Workspace();
        var held = HeldData.Current;
        return new Reply(new
        {
            ok = true,
            codes = workspace.VideoCodes(),
            ceoName = workspace.Record.CeoName,
            recordingMaximumSeconds = held.RecordingMaximumSeconds,
            operatorItems = ChecklistView.OperatorItems(held).Count,
            headerFields = FactSheetCheck.HeaderFields.Select(f => new { member = f.Member, label = f.Label }),
            rowFields = FactSheetCheck.RowFields.Select(f => new { member = f.Member, label = f.Label }),
            callToAction = held.CallToAction,
        });
    }

    private static Reply VideoStatus(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        var folder = workspace.InputFolder(code);
        var state = workspace.LoadState(code);
        var factPath = workspace.FactSheetPath(code);
        object? facts = null;
        IReadOnlyList<(string Name, bool Present)> images = [];
        if (File.Exists(factPath))
        {
            facts = JsonSerializer.Deserialize<JsonElement>(File.ReadAllBytes(factPath));
            images = FactSteps.DeclaredImages(factPath);
        }

        var script = ReadScript(workspace, code);
        var take = workspace.TakePath(code);
        MediaSummary? recording = null;
        MediaSummary? clip = null;
        var recordings = workspace.FilesWith(code, Admission.RecordingExtensions);
        var clips = workspace.FilesWith(code, Admission.ClipExtensions);
        if (recordings.Count > 0 || clips.Count > 0)
        {
            var tool = new MediaTool(web.Settings());
            if (recordings.Count == 1)
            {
                var admitted = MediaSteps.Recording(tool, folder);
                recording = new MediaSummary(admitted.Name, Math.Round(admitted.Facts.DurationSeconds, 3), admitted.Sha256, 0, 0);
            }

            clip = MediaSteps.Clip(tool, folder);
        }

        var job = web.Jobs.Latest(code);
        return new Reply(new
        {
            ok = true,
            code,
            facts,
            images = images.Select(i => new { name = i.Name, present = i.Present }),
            script = script is null ? null : new
            {
                sha256 = script.Value.Sha256,
                sentences = script.Value.Script.Sentences.Select(s => new { number = s.Number, text = s.Text, label = s.Label, source = s.Source }),
            },
            scriptTicks = script is not null && state.ScriptSha256 == script.Value.Sha256 ? state.ScriptTicks : [],
            take = take is null ? null : new { sha256 = MediaTool.Sha256File(take), bytes = new FileInfo(take).Length },
            recording,
            clip,
            timing = File.Exists(workspace.TimingPath(code)),
            producing = job is { Running: true },
            lastPackage = workspace.LastPackage(code),
        });
    }

    private static (Script Script, string Sha256)? ReadScript(Workspace workspace, string code)
    {
        var path = workspace.ScriptPath(code);
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(path);
        var script = JsonSerializer.Deserialize<Script>(bytes, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ?? throw new WebRefusal("script-unreadable", "Tệp kịch bản không đọc được. Bấm \"Viết kịch bản\" lại ở bước 3.");
        return (script, ScriptGenerator.Sha256(bytes));
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Step 1: fact sheet, live check, screenshots (scope items 1 and 2)
    // -----------------------------------------------------------------------------------------------------------------------------

    private static async Task<Reply> LiveCheck(HttpContext http)
    {
        var request = await Body<LiveCheckRequest>(http);
        var findings = FactSteps.LiveCheck(request.Fields ?? []);
        return new Reply(new { ok = findings.Count == 0, findings });
    }

    private static async Task<Reply> SaveFacts(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        SetLimit(http, SmallBody);
        using var memory = new MemoryStream();
        await http.Request.Body.CopyToAsync(memory);
        var normalised = FactSteps.Normalise(memory.ToArray());
        using (var document = JsonDocument.Parse(normalised))
        {
            var typed = document.RootElement.TryGetProperty("videoCode", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : string.Empty;
            if (typed.Length > 0 && !string.Equals(typed, code, StringComparison.Ordinal))
            {
                throw new WebRefusal("video-code-differs",
                    $"Mã video trong biểu mẫu là \"{typed}\", khác video đang mở \"{code}\". Mở đúng video, hoặc tạo video mới với mã đó.");
            }
        }

        var folder = workspace.InputFolder(code);
        var screenshots = workspace.ScreenshotFolder(code);
        var sheet = FactSteps.CheckBeforeImages(normalised, folder, screenshots, workspace.UploadFolder);
        var refused = FactSteps.PlaceScreenshots(sheet, screenshots, folder);
        File.WriteAllBytes(workspace.FactSheetPath(code), normalised);
        var saved = $"Đã lưu bảng thông tin {sheet.VideoCode}: {sheet.Facts.Count} dòng thông tin, {sheet.ImagesUsed.Count} ảnh cần tải ở bước 2.";
        return new Reply(new
        {
            ok = true,
            refusedScreenshots = refused,
            message = refused.Count == 0
                ? saved
                : $"{saved} Không nhận ảnh chụp {string.Join(", ", refused)}: bảng thông tin không ghi tên này ở dòng nào, nên ảnh đã bị bỏ. Nếu cần ảnh đó, ghi đúng tên ở ô ảnh chụp của dòng thông tin, tải ảnh lên lại rồi bấm lưu.",
        });
    }

    private static async Task<Reply> UploadScreenshot(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        // The screenshot waits outside the input folder; only a saved sheet that declares it for a fact row moves it in (CR-003).
        var waiting = workspace.ScreenshotFolder(code);
        Directory.CreateDirectory(waiting);
        var name = PlainName.Require(Query(http, "name"), "Ảnh chụp màn hình", waiting);
        RequireImageExtension(name);
        var upload = await SaveUpload(http, workspace, Path.GetExtension(name), ImageBody);
        File.Move(upload, Path.Combine(waiting, name), overwrite: true);
        return new Reply(new
        {
            ok = true,
            name,
            message = $"Đã nhận ảnh chụp {name}. Ảnh được dùng khi bạn bấm lưu bảng thông tin, nếu một dòng thông tin ghi đúng tên này.",
        });
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Step 2: images (scope item 3)
    // -----------------------------------------------------------------------------------------------------------------------------

    private static async Task<Reply> UploadImage(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        var factPath = workspace.FactSheetPath(code);
        if (!File.Exists(factPath))
        {
            throw new WebRefusal("facts-needed", "Chưa lưu bảng thông tin của video này. Làm bước 1 trước.");
        }

        var folder = workspace.InputFolder(code);
        var name = PlainName.Require(Query(http, "name"), "Ảnh", folder);
        var declared = FactSteps.DeclaredImages(factPath);
        if (!declared.Any(d => string.Equals(d.Name, name, StringComparison.Ordinal)))
        {
            throw new WebRefusal("image-not-declared",
                $"Ảnh \"{name}\" không có trong danh sách ảnh của bảng thông tin ({string.Join(", ", declared.Select(d => d.Name))}). Chỉ tải đúng các ảnh đã ghi ở bước 1, hoặc sửa danh sách ở bước 1.");
        }

        RequireImageExtension(name);
        var source = Query(http, "source");
        if (source.Length > 0 && !string.Equals(Path.GetExtension(source), Path.GetExtension(name), StringComparison.OrdinalIgnoreCase))
        {
            throw new WebRefusal("image-type-differs",
                $"Tệp đã chọn có đuôi {Path.GetExtension(source)}, nhưng bảng thông tin ghi \"{name}\". Chọn ảnh có đuôi {Path.GetExtension(name)}, hoặc sửa tên ảnh ở bước 1.");
        }

        var upload = await SaveUpload(http, workspace, Path.GetExtension(name), ImageBody);
        File.Move(upload, Path.Combine(folder, name), overwrite: true);
        var missing = FactSteps.DeclaredImages(factPath).Where(d => !d.Present).Select(d => d.Name).ToList();
        return new Reply(new
        {
            ok = true,
            missing,
            message = missing.Count == 0 ? $"Đã nhận {name}. Đủ ảnh." : $"Đã nhận {name}. Còn thiếu: {string.Join(", ", missing)}.",
        });
    }

    private static void RequireImageExtension(string name)
    {
        if (!FactSteps.ImageExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
        {
            throw new WebRefusal("image-format-refused",
                $"\"{name}\" không phải ảnh dạng {string.Join(", ", FactSteps.ImageExtensions)}. Lưu ảnh dưới một trong các dạng đó rồi tải lên lại.");
        }
    }

    private static void RequireImages(Workspace workspace, string code)
    {
        var factPath = workspace.FactSheetPath(code);
        if (!File.Exists(factPath))
        {
            throw new WebRefusal("facts-needed", "Chưa lưu bảng thông tin của video này. Làm bước 1 trước.");
        }

        var missing = FactSteps.DeclaredImages(factPath).Where(d => !d.Present).Select(d => d.Name).ToList();
        if (missing.Count > 0)
        {
            throw new WebRefusal("images-missing", $"Còn thiếu ảnh: {string.Join(", ", missing)}. Tải các ảnh đó lên ở bước 2.");
        }
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Step 3: script (scope item 4): the check and script verbs decide
    // -----------------------------------------------------------------------------------------------------------------------------

    private static Reply RunScript(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        RequireImages(workspace, code);
        var runner = web.Runner();
        var settings = workspace.Record.SettingsPath;
        var facts = workspace.FactSheetPath(code);
        var check = runner.Run("check", ["--settings", settings, "--facts", facts], WebContext.VerbBound);
        if (check.ExitCode != PilotExit.Done)
        {
            return FromRun(check);
        }

        var written = runner.Run("script", ["--settings", settings, "--facts", facts], WebContext.VerbBound);
        if (written.ExitCode != PilotExit.Done)
        {
            return FromRun(written, new { check = Outcomes.FromToolchain(check) });
        }

        var script = ReadScript(workspace, code)!.Value;
        var state = workspace.LoadState(code);
        if (state.ScriptSha256 != script.Sha256)
        {
            workspace.SaveState(code, state with { ScriptSha256 = script.Sha256, ScriptTicks = [] });
        }

        return FromRun(written, new
        {
            check = Outcomes.FromToolchain(check),
            sha256 = script.Sha256,
            sentences = script.Script.Sentences.Select(s => new { number = s.Number, text = s.Text, label = s.Label, source = s.Source }),
        });
    }

    private static async Task<Reply> SaveScriptTicks(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        var request = await Body<ScriptTicksRequest>(http);
        var script = ReadScript(workspace, code)
            ?? throw new WebRefusal("script-needed", "Video này chưa có kịch bản. Bấm \"Viết kịch bản\" ở bước 3.");
        if (!string.Equals(request.ScriptSha256, script.Sha256, StringComparison.Ordinal))
        {
            throw new WebRefusal("script-changed", "Kịch bản đã được viết lại sau khi trang mở. Tải lại trang rồi tích lại từng dòng.");
        }

        var count = script.Script.Sentences.Count;
        var ticks = (request.Ticked ?? []).Distinct().Order().ToList();
        if (ticks.Any(t => t < 1 || t > count))
        {
            throw new WebRefusal("tick-unknown", $"Kịch bản có {count} câu; dấu tích phải thuộc câu 1 đến {count}. Tải lại trang rồi tích lại.");
        }

        workspace.SaveState(code, workspace.LoadState(code) with { ScriptSha256 = script.Sha256, ScriptTicks = ticks });
        return new Reply(new { ok = true, ticked = ticks.Count, total = count, complete = ticks.Count == count });
    }

    private static void RequireScriptApproved(Workspace workspace, string code)
    {
        var script = ReadScript(workspace, code)
            ?? throw new WebRefusal("script-needed", "Video này chưa có kịch bản. Làm bước 3 trước.");
        var state = workspace.LoadState(code);
        var ticked = state.ScriptSha256 == script.Sha256 ? state.ScriptTicks.Count : 0;
        if (ticked != script.Script.Sentences.Count)
        {
            throw new WebRefusal("script-not-ticked",
                $"Mới tích {ticked} trên {script.Script.Sentences.Count} dòng nguồn của kịch bản. Đối chiếu và tích đủ từng câu ở bước 3 trước khi ghi âm.");
        }
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Step 4: recording (scope items 5 and 6; design decision D-002)
    // -----------------------------------------------------------------------------------------------------------------------------

    private static async Task<Reply> UploadTake(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        RequireScriptApproved(workspace, code);
        var type = (http.Request.ContentType ?? string.Empty).Split(';', 2)[0].Trim().ToLowerInvariant();
        var extension = type switch
        {
            "audio/webm" or "video/webm" => ".webm",
            "audio/ogg" => ".ogg",
            "audio/mp4" or "video/mp4" => ".m4a",
            _ => throw new WebRefusal("take-format-refused",
                $"Trình duyệt gửi bản ghi dạng \"{type}\", ứng dụng không nhận dạng này. Dùng trình duyệt Chrome hoặc Edge, hoặc tải bản ghi lên ở phần dưới."),
        };
        var upload = await SaveUpload(http, workspace, extension, TakeBody);
        Directory.CreateDirectory(workspace.TakesFolder);
        if (workspace.TakePath(code) is { } old)
        {
            File.Delete(old);
        }

        var target = Path.Combine(workspace.TakesFolder, $"{code}.take{extension}");
        File.Move(upload, target, overwrite: true);
        return new Reply(new
        {
            ok = true,
            sha256 = MediaTool.Sha256File(target),
            bytes = new FileInfo(target).Length,
            message = "Đã giữ bản ghi vừa thu. Nghe lại; nếu ổn, bấm \"Dùng bản ghi này\".",
        });
    }

    private static Reply AcceptTake(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        RequireScriptApproved(workspace, code);
        var take = workspace.TakePath(code)
            ?? throw new WebRefusal("take-needed", "Chưa có bản ghi nào được thu cho video này. Bấm \"Bắt đầu ghi\" trước.");
        var tool = new MediaTool(web.Settings());
        // CEO-D-1312: the admission reads the ledger in this process, between produce jobs only (it waits for a running one).
        var recording = MediaSteps.Convert(tool, take, Target(workspace, code), HeldData.Current, web.Jobs, ProduceJobs.LedgerWait);
        return new Reply(new
        {
            ok = true,
            recording,
            message = string.Create(CultureInfo.InvariantCulture,
                $"Đã dùng bản ghi này: {recording.Name}, dài {recording.Seconds:0.0} giây, SHA-256 {recording.Sha256}. Bước tiếp: tải đoạn phim người dẫn."),
        });
    }

    private static RecordingTarget Target(Workspace workspace, string code) =>
        new(workspace.InputFolder(code), code, workspace.PilotFolder, workspace.UploadFolder);

    private static async Task<Reply> UploadRecording(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        RequireScriptApproved(workspace, code);
        var folder = workspace.InputFolder(code);
        var name = PlainName.Require(Query(http, "name"), "Bản ghi tải lên", workspace.UploadFolder);
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (!Admission.RecordingExtensions.Contains(extension))
        {
            throw new WebRefusal("recording-format-refused",
                $"Tệp \"{name}\" không phải dạng bản ghi bộ công cụ nhận. Dạng được nhận: {string.Join(", ", Admission.RecordingExtensions)}. Xuất bản ghi sang một trong các dạng đó rồi tải lên lại; ứng dụng không tự chuyển tệp tải lên.");
        }

        var tool = new MediaTool(web.Settings());
        var upload = await SaveUpload(http, workspace, extension, RecordingBody);
        try
        {
            // CEO-D-1312: the admission reads the ledger in this process, between produce jobs only (it waits for a running one).
            var recording = MediaSteps.AcceptUploadedRecording(tool, upload, extension, Target(workspace, code), HeldData.Current, web.Jobs, ProduceJobs.LedgerWait);
            return new Reply(new
            {
                ok = true,
                recording,
                message = string.Create(CultureInfo.InvariantCulture,
                    $"Đã nhận bản ghi {recording.Name}, dài {recording.Seconds:0.0} giây, giữ nguyên từng byte (SHA-256 {recording.Sha256})."),
            });
        }
        finally
        {
            if (File.Exists(upload))
            {
                File.Delete(upload);
            }
        }
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Step 5: presenter clip (scope item 7)
    // -----------------------------------------------------------------------------------------------------------------------------

    private static async Task<Reply> UploadClip(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        var folder = workspace.InputFolder(code);
        var name = PlainName.Require(Query(http, "name"), "Đoạn phim người dẫn", workspace.UploadFolder);
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (!Admission.ClipExtensions.Contains(extension))
        {
            throw new WebRefusal("clip-format-refused",
                $"Tệp \"{name}\" không phải dạng đoạn phim bộ công cụ nhận. Dạng được nhận: {string.Join(", ", Admission.ClipExtensions)}. Xuất lại đoạn phim rồi tải lên lại.");
        }

        var settings = web.Settings();
        var tool = new MediaTool(settings);
        var upload = await SaveUpload(http, workspace, extension, ClipBody);
        try
        {
            var clip = MediaSteps.AcceptClip(tool, settings, upload, extension, folder, workspace.UploadFolder);
            return new Reply(new
            {
                ok = true,
                clip,
                message = string.Create(CultureInfo.InvariantCulture,
                    $"Đã nhận đoạn phim {clip.Name}: {clip.Width}x{clip.Height}, dài {clip.Seconds:0.000} giây. Bước tiếp: tạo video."),
            });
        }
        finally
        {
            if (File.Exists(upload))
            {
                File.Delete(upload);
            }
        }
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Step 6: produce, progress, timing editor (scope items 8 and 9; design decisions D-009 to D-011)
    // -----------------------------------------------------------------------------------------------------------------------------

    private static Reply StartProduce(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        RequireImages(workspace, code);
        RequireScriptApproved(workspace, code);
        if (workspace.FilesWith(code, Admission.RecordingExtensions).Count != 1)
        {
            throw new WebRefusal("recording-needed", "Video này chưa có bản ghi giọng. Làm bước 4 trước.");
        }

        if (workspace.FilesWith(code, Admission.ClipExtensions).Count != 1)
        {
            throw new WebRefusal("clip-needed", "Video này chưa có đoạn phim người dẫn. Làm bước 5 trước.");
        }

        var runner = web.Runner();
        var settings = workspace.Record.SettingsPath;
        var facts = workspace.FactSheetPath(code);
        var job = web.Jobs.Start(code, job =>
        {
            ProduceWork(job, () => runner.Run("produce", ["--settings", settings, "--facts", facts], WebContext.ProduceBound), workspace, code, web.Log);
            return Task.CompletedTask;
        });
        return new Reply(new { ok = true, started = true, message = $"Đang tạo video {job.Code}. Trang sẽ báo khi xong." });
    }

    /// <summary>
    /// The background produce work (design decision D-009): runs the produce verb, records the package it wrote as the video's last
    /// package, then ends the job and writes its log line. Every exception ends the job with a shown category and a log line (review
    /// correction CR-004): a step refusal as a refusal, any other exception as an app fault; a package the toolchain wrote is still
    /// shown when recording it failed.
    /// </summary>
    public static void ProduceWork(ProduceJob job, Func<ToolchainRun> produce, Workspace workspace, string code, WebLog log)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(produce);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(log);
        ToolchainRun? run = null;
        OutcomeView view;
        string? package = null;
        var status = 200;
        try
        {
            run = produce();
            view = Outcomes.FromToolchain(run);
            package = PackageOf(run.Output, workspace);
            if (package is not null)
            {
                workspace.SaveState(code, workspace.LoadState(code) with { LastPackage = package, ChecklistTicks = [] });
            }
        }
        catch (WebRefusal refusal)
        {
            view = Outcomes.FromWeb(refusal);
        }
        catch (Exception error)
        {
            view = new OutcomeView(Outcomes.AppFault, null, "app-fault", $"LỖI ỨNG DỤNG [app-fault]: {error.GetType().Name}: {error.Message}\n",
                package is null
                    ? "Ứng dụng không chạy được bộ công cụ, hoặc không ghi được kết quả."
                    : $"Bộ công cụ đã tạo gói {package}, nhưng ứng dụng không ghi được trạng thái của video này.",
                Outcomes.TechnicalNext);
            status = 500;
        }

        ProduceJobs.Finish(job, run, view, package);
        log.Write("JOB", "/api/produce", status, view.Code.Length == 0 ? null : view.Code, run?.ExitCode, (run?.Seconds ?? 0) * 1000);
    }

    /// <summary>The package folder named by the produce output's video line, when it is a folder of the pilot folder's runs.</summary>
    public static string? PackageOf(string output, Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(workspace);
        var line = output.Split('\n').Select(l => l.TrimEnd('\r')).FirstOrDefault(l => l.StartsWith("  video: ", StringComparison.Ordinal));
        if (line is null)
        {
            return null;
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(line["  video: ".Length..]));
        var runs = Path.TrimEndingDirectorySeparator(Path.GetFullPath(workspace.RunsFolder));
        return folder is not null && string.Equals(Path.GetDirectoryName(folder), runs, StringComparison.OrdinalIgnoreCase) ? folder : null;
    }

    private static Reply Progress(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        var job = web.Jobs.Latest(code);
        if (job is null)
        {
            return new Reply(new { ok = true, running = false, started = false });
        }

        var timingEditor = job.View?.Code switch
        {
            "pauses-not-found" or "timing-file-invalid" => "mo",
            "caption-page-too-short" => "goi-y",
            _ => null,
        };
        return new Reply(new
        {
            ok = true,
            started = true,
            running = job.Running,
            elapsedSeconds = job.ElapsedSeconds,
            outcome = job.View,
            output = job.Result?.Output,
            exitCode = job.Result?.ExitCode,
            package = job.PackageFolder,
            timingEditor,
        });
    }

    private static Reply TimingForm(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        var script = ReadScript(workspace, code)
            ?? throw new WebRefusal("script-needed", "Video này chưa có kịch bản. Làm bước 3 trước.");
        var timing = workspace.TimingPath(code);
        var lines = File.Exists(timing) ? File.ReadAllLines(timing).Where(l => l.Trim().Length > 0).ToList() : [];
        var recording = MediaSteps.Recording(new MediaTool(web.Settings()), workspace.InputFolder(code));
        return new Reply(new
        {
            ok = true,
            recordingSeconds = Math.Round(recording.Facts.DurationSeconds, 3),
            sentences = script.Script.Sentences.Select(s => new { number = s.Number, text = s.Text }),
            lines,
        });
    }

    private static async Task<Reply> SaveTiming(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        var request = await Body<TimingRequest>(http);
        var script = ReadScript(workspace, code)
            ?? throw new WebRefusal("script-needed", "Video này chưa có kịch bản. Làm bước 3 trước.");
        var recording = MediaSteps.Recording(new MediaTool(web.Settings()), workspace.InputFolder(code));
        var text = string.Join("\n", (request.Lines ?? []).Select(l => (l ?? string.Empty).Trim())) + "\n";

        // The toolchain's own timing rules decide; the refusal names the first failing sentence.
        _ = CaptionTiming.ReadTimingFile(text, script.Script.Sentences.Count, recording.Facts.DurationSeconds);
        File.WriteAllText(workspace.TimingPath(code), text, new UTF8Encoding(false));
        return new Reply(new { ok = true, message = $"Đã lưu thời gian {script.Script.Sentences.Count} câu vào {CaptionTiming.TimingFileName}. Bấm \"Tạo video\" lại." });
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Step 7: preview, checklist, approval by hash, verify (scope items 10 to 12; design decisions D-005 and D-012)
    // -----------------------------------------------------------------------------------------------------------------------------

    private static string RequirePackage(Workspace workspace, string code) =>
        workspace.LastPackage(code)
            ?? throw new WebRefusal("package-needed", "Chưa có gói video nào do ứng dụng tạo cho video này. Làm bước 6 trước.");

    private static Reply ChecklistStatus(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        var package = RequirePackage(workspace, code);
        var manifest = PackageFiles.ReadJson<PackageManifest>(Path.Combine(package, PackageFiles.Manifest), "package-unreadable", "tệp kê của gói");
        var held = HeldData.Current;
        var reading = ChecklistView.Read(package, held);
        var state = workspace.LoadState(code);
        var approvalPath = Path.Combine(package, PackageFiles.Approval);
        var approval = File.Exists(approvalPath) ? PackageFiles.ReadJson<ApprovalRecord>(approvalPath, "package-unreadable", "tệp duyệt") : null;
        var operatorItems = ChecklistView.OperatorItems(held);
        return new Reply(new
        {
            ok = true,
            package,
            videoPath = ShownVideoPath(package, manifest),
            status = manifest.Status,
            heldReasons = manifest.HeldReasons,
            sha256 = manifest.VideoSha256,
            rows = reading.Rows,
            matchesHeld = reading.MatchesHeld,
            problem = reading.Problem,
            ticks = state.ChecklistTicks,
            operatorItems = operatorItems.Count,
            approvable = reading.MatchesHeld && manifest.Status == "ready",
            approval,
            ceoName = workspace.Record.CeoName,
        });
    }

    private static async Task<Reply> SaveChecklistTicks(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        _ = RequirePackage(workspace, code);
        var request = await Body<ChecklistTicksRequest>(http);
        var operatorItems = ChecklistView.OperatorItems(HeldData.Current);
        var ticks = (request.Ticked ?? []).Distinct(StringComparer.Ordinal).ToList();
        var unknown = ticks.Where(t => !operatorItems.Contains(t, StringComparer.Ordinal)).ToList();
        if (unknown.Count > 0)
        {
            throw new WebRefusal("tick-unknown",
                $"Mục {string.Join(", ", unknown)} không phải mục người duyệt tích (B8 do bộ công cụ quyết). Tải lại trang rồi tích lại.");
        }

        var ordered = operatorItems.Where(ticks.Contains).ToList();
        workspace.SaveState(code, workspace.LoadState(code) with { ChecklistTicks = ordered });
        return new Reply(new { ok = true, ticked = ordered.Count, total = operatorItems.Count });
    }

    private static async Task<Reply> Approve(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        var package = RequirePackage(workspace, code);
        var request = await Body<ApproveRequest>(http);
        var held = HeldData.Current;
        var reading = ChecklistView.Read(package, held);
        if (!reading.MatchesHeld)
        {
            throw new WebRefusal("checklist-differs", reading.Problem ?? "Danh sách duyệt của gói khác mẫu 04. Không duyệt; tạo video lại ở bước 6.");
        }

        var operatorItems = ChecklistView.OperatorItems(held);
        var ticked = (request.Ticked ?? []).Distinct(StringComparer.Ordinal).ToList();
        var missing = operatorItems.Where(i => !ticked.Contains(i, StringComparer.Ordinal)).ToList();
        if (missing.Count > 0 || ticked.Count != operatorItems.Count)
        {
            throw new WebRefusal("checklist-incomplete",
                $"Mới tích {operatorItems.Count - missing.Count} trên {operatorItems.Count} mục. Kiểm và tích đủ các mục còn lại ({string.Join(", ", missing)}) rồi bấm duyệt; thiếu một mục thì không đăng.");
        }

        var approver = request.PressedBy switch
        {
            "ceo" => ApproverText.Compose(workspace.Record.CeoName, null),
            "nhan-vien" => ApproverText.Compose(workspace.Record.CeoName, request.EmployeeName),
            _ => throw new WebRefusal("pressed-by-missing", "Chọn ai đang bấm duyệt: CEO, hoặc nhân viên theo chỉ thị bằng văn bản của CEO."),
        };
        var runner = web.Runner();
        var approve = runner.Run("approve", ["--package", package, "--name", approver, "--sha256", (request.Sha256 ?? string.Empty).Trim()], WebContext.VerbBound);
        var manifest = PackageFiles.ReadJson<PackageManifest>(Path.Combine(package, PackageFiles.Manifest), "package-unreadable", "tệp kê của gói");
        var videoPath = ShownVideoPath(package, manifest);
        if (approve.ExitCode != PilotExit.Done)
        {
            return FromRun(approve, new { videoPath });
        }

        var verify = runner.Run("verify", ["--package", package], WebContext.VerbBound);
        return FromRun(approve, new { verify = Outcomes.FromToolchain(verify), verifyExit = verify.ExitCode, videoPath, approver });
    }

    private static Reply Verify(HttpContext http, WebContext web)
    {
        var workspace = web.Workspace();
        var code = CodeOf(http, workspace);
        var package = RequirePackage(workspace, code);
        var verify = web.Runner().Run("verify", ["--package", package], WebContext.VerbBound);

        // The shown path comes from the package manifest, as on the checklist and approve routes (review correction CR-005). A manifest
        // the app cannot read, or one naming more than a file name, shows no path; the verify verb's own line already names what is wrong.
        string? videoPath;
        try
        {
            var manifest = PackageFiles.ReadJson<PackageManifest>(Path.Combine(package, PackageFiles.Manifest), "package-unreadable", "tệp kê của gói");
            videoPath = Path.GetFileName(manifest.VideoFile) == manifest.VideoFile ? ShownVideoPath(package, manifest) : null;
        }
        catch (PilotRefusal)
        {
            videoPath = null;
        }

        return FromRun(verify, new { videoPath });
    }

    /// <summary>The package video path every route shows: the package folder and the video file its manifest names.</summary>
    private static string ShownVideoPath(string package, PackageManifest manifest) => Path.Combine(package, manifest.VideoFile);

    /// <summary>The video's admitted recording, played back in the timing editor; read only.</summary>
    private static IResult RecordingFile(HttpContext http, WebContext web)
    {
        try
        {
            var workspace = web.Workspace();
            var code = CodeOf(http, workspace);
            var file = workspace.FilesWith(code, Admission.RecordingExtensions) is [var only] ? only : null;
            return file is null ? Results.NotFound() : Results.File(file, AudioType(Path.GetExtension(file)), enableRangeProcessing: true);
        }
        catch (Exception error) when (error is WebRefusal or PilotRefusal)
        {
            return Results.NotFound();
        }
    }

    private static string AudioType(string extension) => extension.ToLowerInvariant() switch
    {
        ".wav" => "audio/wav",
        ".mp3" => "audio/mpeg",
        ".m4a" => "audio/mp4",
        ".aac" => "audio/aac",
        ".flac" => "audio/flac",
        ".ogg" => "audio/ogg",
        _ => "application/octet-stream",
    };

    private static IResult PackageFile(HttpContext http, WebContext web, bool video)
    {
        try
        {
            var workspace = web.Workspace();
            var code = CodeOf(http, workspace);
            var package = RequirePackage(workspace, code);
            if (!video)
            {
                return Results.File(Path.Combine(package, PackageFiles.Frame), "image/png");
            }

            var manifest = PackageFiles.ReadJson<PackageManifest>(Path.Combine(package, PackageFiles.Manifest), "package-unreadable", "tệp kê của gói");
            return Results.File(Approval.VideoPath(package, manifest), "video/mp4", enableRangeProcessing: true);
        }
        catch (Exception error) when (error is WebRefusal or PilotRefusal)
        {
            return Results.NotFound();
        }
    }
}
