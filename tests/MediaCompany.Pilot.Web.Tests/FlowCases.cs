using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaCompany.Pilot.Tests;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>
/// The operator's seven steps through the running app, over HTTP, against one pilot folder; every refusal the toolchain could give is
/// compared with the toolchain run directly on the same files. The app process and everything it starts are members of one
/// address harness for the whole flow.
/// </summary>
internal sealed class FlowCases(Runner r) : IDisposable
{
    private const string CeoName = "Nguyễn Văn An";
    private const string EmployeeName = "Trần Thị Bình";
    private const double Length = 17.4;
    private static readonly TimeSpan ProduceLimit = TimeSpan.FromMinutes(20);
    private static readonly string[] GoodTiming = ["0.00 3.40", "3.50 6.90", "7.00 10.40", "10.50 13.90", "14.00 17.40"];

    private readonly AddressHarness _harness = new();
    private readonly string _root = PilotTestEnvironment.NewFolder("w-flow");
    private AppProcess? _app;
    private string? _package;

    private AppProcess App => _app ?? throw new CaseFailure("the app did not start in T-010");

    private string AppFolder => Path.Combine(_root, "app");
    private string Desktop => Path.Combine(_root, "desktop");
    private string Pilot => Path.Combine(_root, "pilot");
    private string SettingsPath => Path.Combine(Pilot, Setup.SettingsFileName);
    private string Stills => Path.Combine(_root, "stills");
    private string Media => Path.Combine(_root, "media");
    private string Input(string code) => Path.Combine(Pilot, "inputs", code);
    private string StatePath(string code) => Path.Combine(Pilot, "web", $"video-{code}.json");
    private string Package => _package ?? throw new CaseFailure("no package was produced in T-020");

    public void Add()
    {
        r.Add("T-010", "the app starts on 127.0.0.1 only, prints its ready line, serves the page with its token and the security headers, and asks for setup first", () =>
        {
            Directory.CreateDirectory(Desktop);
            Fixtures.Stills(Stills);
            Directory.CreateDirectory(Media);
            _app = AppProcess.Start(AppFolder, Desktop, _harness);
            Expect.True(App.Origin.StartsWith("http://127.0.0.1:", StringComparison.Ordinal), $"ready address {App.Origin}");
            Expect.Equal(64, App.Token.Length, "page token");
            using var page = App.Http.GetAsync("/").GetAwaiter().GetResult();
            var csp = string.Join(' ', page.Headers.GetValues("Content-Security-Policy"));
            Expect.Contains("default-src 'self'", csp, "CSP");
            Expect.Contains("connect-src 'self'", csp, "CSP");
            Expect.True(!page.Headers.Contains("Server"), "no Server header");
            Expect.True(!page.Headers.Contains("Access-Control-Allow-Origin"), "no cross-origin grant");
            var html = page.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Expect.True(!html.Contains("test-driver", StringComparison.Ordinal), "no test driver outside test mode");
            Expect.Equal(404, (int)App.Http.GetAsync("/test-driver.js").GetAwaiter().GetResult().StatusCode, "test driver not served outside test mode");
            var videos = App.Get("/api/videos");
            Expect.Equal(409, videos.Status, "videos before setup");
            Expect.Equal("setup-needed", videos.Code, "setup-needed");
            var setup = App.Get("/api/setup");
            Expect.True(setup.Ok && !setup["configured"].GetBoolean(), "setup status");
            Expect.Equal(0.5, setup["ceoNumbers"].GetProperty("clipToleranceSeconds").GetDouble(), "CEO clip tolerance shown");
            Thread.Sleep(300);
            var listeners = _harness.Rows.Where(row => row.Owner == App.Process.Id && row.State == SocketRow.Listen).ToList();
            Expect.True(listeners.Count > 0 && listeners.All(row => row.Local.ToString() == "127.0.0.1"), $"listeners: {string.Join("; ", listeners)}");
            r.Note($"ready line {App.Origin}; listeners: {string.Join("; ", listeners)}");
        });

        r.Add("T-012", "setup: an unknown field (a CEO number) and an in-repository folder are refused; the saved settings equal the example and pass the toolchain's reader; the shortcut is written on request", () =>
        {
            var smuggled = App.Send("/api/setup", new StringContent(
                JsonSerializer.Serialize(new { pilotFolder = Pilot, ceoName = CeoName, createShortcut = false, clipToleranceSeconds = 0.9 }), Encoding.UTF8, "application/json"));
            Expect.Equal(400, smuggled.Status, "unknown field status");
            Expect.Equal("request-unreadable", smuggled.Code, "unknown field code");
            var inRepo = App.Post("/api/setup", new { pilotFolder = Path.Combine(PilotTestEnvironment.RepositoryRoot, "pilot"), ceoName = CeoName, createShortcut = false });
            Expect.Equal("pilot-folder-in-repository", inRepo.Code, "in-repository folder");
            var badName = App.Post("/api/setup", new { pilotFolder = Pilot, ceoName = "An (CEO)", createShortcut = false });
            Expect.Equal("name-invalid", badName.Code, "CEO name with brackets");
            Expect.True(!Directory.Exists(Pilot), "nothing written by a refused setup");
            var saved = App.Post("/api/setup", new { pilotFolder = Pilot, ceoName = CeoName, createShortcut = true });
            Expect.True(saved.Ok, $"setup saved: {saved}");
            var settings = PilotSettings.Read(SettingsPath);
            var example = JsonNode.Parse(File.ReadAllText(Path.Combine(PilotTestEnvironment.RepositoryRoot, Setup.ExampleRelativePath)))!.AsObject();
            var written = JsonNode.Parse(File.ReadAllText(SettingsPath))!.AsObject();
            foreach (var (key, value) in example.Where(p => p.Key is not ("repositoryRoot" or "pilotFolder" or "mediaTool" or "probeTool" or "face")))
            {
                Expect.True(UnitCases.SameValues(value, written[key]), $"setting {key}: example {value?.ToJsonString()}, written {written[key]?.ToJsonString()}");
            }

            Expect.Equal(Pilot, settings.PilotFolder, "pilot folder");
            var shortcut = Path.Combine(Desktop, Shortcut.FileName);
            Expect.True(File.Exists(shortcut) && new FileInfo(shortcut).Length > 0, "desktop shortcut written");
            var record = File.ReadAllText(Path.Combine(AppFolder, AppRecordStore.FileName));
            Expect.True(!record.Contains(App.Token, StringComparison.Ordinal), "the token is never stored");
            Expect.True(App.Get("/api/setup")["configured"].GetBoolean(), "configured");
            r.Note($"settings {SettingsPath}; shortcut {shortcut}");
        });

        r.Add("T-013", "fact sheet: 17 blank-field sheets are each refused with the code and words the check verb gives on the same sheet", () =>
        {
            Expect.True(App.PostFile("/api/screenshot?code=V-01&name=chup-1.png", Path.Combine(Stills, "chup-1.png")).Ok, "screenshot attached");
            var direct = Path.Combine(_root, "direct-sheet");
            Fixtures.CopyFolder(Stills, direct);
            var cases = new List<(string What, JsonObject Sheet)>();
            foreach (var (member, _) in FactSheetCheck.HeaderFields)
            {
                var sheet = Fixtures.Sheet("V-01");
                sheet[member] = member switch
                {
                    "imagesUsed" => new JsonArray(),
                    "productVideoNumber" => null,
                    _ => "",
                };
                cases.Add(($"header {member}", sheet));
            }

            foreach (var (member, _) in FactSheetCheck.RowFields)
            {
                var sheet = Fixtures.Sheet("V-01");
                sheet["facts"]![0]![member] = "";
                cases.Add(($"row {member}", sheet));
            }

            var none = Fixtures.Sheet("V-01");
            none["facts"] = new JsonArray();
            cases.Add(("no fact row", none));
            var exact = 0;
            foreach (var (what, sheet) in cases)
            {
                var answer = App.Post("/api/facts?code=V-01", sheet);
                Expect.Equal(400, answer.Status, $"{what} status ({answer})");
                var path = Path.Combine(direct, "bang-thong-tin.json");
                File.WriteAllText(path, Fixtures.Text(sheet), new UTF8Encoding(false));
                var (exit, output) = Direct.Run("check", "--settings", SettingsPath, "--facts", path);
                Expect.Equal(2, exit, $"{what} direct exit");
                Expect.Equal(Direct.CodeOf(output), answer.Code, $"{what} code, app versus check verb");
                exact += Direct.FirstLine(output) == answer.Line.TrimEnd('\n') ? 1 : 0;
                r.Note($"{what}: {answer.Code}");
            }

            Expect.Equal(cases.Count, exact, "every refusal line equals the check verb's line");
            Expect.Equal(17, cases.Count, "cases");
            Expect.True(!File.Exists(Path.Combine(Input("V-01"), Workspace.FactSheetFileName)), "no refused sheet was saved");
        });

        r.Add("T-014", "fact sheet: a screenshot not attached and one whose name is a path are refused as the check verb refuses them; the saved sheet is NFC", () =>
        {
            var direct = Path.Combine(_root, "direct-shot");
            Fixtures.CopyFolder(Stills, direct);
            foreach (var (shot, code) in new[] { ("chup-9.png", "file-missing"), ("..\\chup-1.png", "file-name-not-plain") })
            {
                var sheet = Fixtures.Sheet("V-01");
                sheet["facts"]![0]!["screenshot"] = shot;
                var answer = App.Post("/api/facts?code=V-01", sheet);
                Expect.Equal(code, answer.Code, $"screenshot {shot}");
                var path = Path.Combine(direct, "bang-thong-tin.json");
                File.WriteAllText(path, Fixtures.Text(sheet), new UTF8Encoding(false));
                var (_, output) = Direct.Run("check", "--settings", SettingsPath, "--facts", path);
                Expect.Equal(Direct.CodeOf(output), answer.Code, $"screenshot {shot}, app versus check verb");
            }

            var notPlain = App.PostFile("/api/screenshot?code=V-01&name=..%5Cx.png", Path.Combine(Stills, "chup-1.png"));
            Expect.Equal("file-name-not-plain", notPlain.Code, "upload name with a path");
            var other = App.Post("/api/facts?code=V-01", Fixtures.Sheet("V-09"));
            Expect.Equal("video-code-differs", other.Code, "a sheet for another code");
            var nfc = Fixtures.Sheet("V-01");
            nfc["productName"] = "Bi\u0300nh giu\u031B\u0303 nhie\u0323\u0302t mẫu 500 ml";
            var saved = App.Post("/api/facts?code=V-01", nfc);
            Expect.True(saved.Ok, $"sheet saved: {saved}");
            var stored = File.ReadAllText(Path.Combine(Input("V-01"), Workspace.FactSheetFileName));
            Expect.Contains("Bình giữ nhiệt mẫu 500 ml", stored, "NFC stored");
            Expect.True(stored.IsNormalized(NormalizationForm.FormC), "the whole file is NFC");
        });

        r.Add("T-015", "images: the script waits for every declared image; an undeclared name and a different file type are refused; three images complete the step", () =>
        {
            Expect.Equal("images-missing", App.Post("/api/script?code=V-01").Code, "script before images");
            Expect.Equal("image-not-declared", App.PostFile("/api/image?code=V-01&name=anh-9.png&source=anh-9.png", Path.Combine(Stills, "anh-1.png")).Code, "undeclared image");
            Expect.Equal("image-type-differs", App.PostFile("/api/image?code=V-01&name=anh-1.png&source=anh-1.jpg", Path.Combine(Stills, "anh-1.png")).Code, "other type");
            var answers = new[] { "anh-1.png", "anh-2.png" }.Select(n => App.PostFile($"/api/image?code=V-01&name={n}&source={n}", Path.Combine(Stills, n))).ToList();
            Expect.True(answers.All(a => a.Ok), "two images");
            Expect.Equal(1, answers[^1]["missing"].GetArrayLength(), "one still missing after two");
            Expect.Equal("images-missing", App.Post("/api/script?code=V-01").Code, "script with two of three images");
            var last = App.PostFile("/api/image?code=V-01&name=anh-3.png&source=anh-3.png", Path.Combine(Stills, "anh-3.png"));
            Expect.Equal(0, last["missing"].GetArrayLength(), "none missing after three");
        });

        r.Add("T-016", "script: written by the check and script verbs in the generator's order; recording waits until every sentence's source is ticked", () =>
        {
            var answer = App.Post("/api/script?code=V-01");
            Expect.True(answer.Ok, $"script written: {answer}");
            var sentences = answer["extra"].GetProperty("sentences").EnumerateArray().Select(s => s.GetProperty("text").GetString()).ToList();
            var expected = ScriptGenerator.Generate(FactSheetCheck.Read(Path.Combine(Input("V-01"), Workspace.FactSheetFileName), BannedTermCheck.Held), HeldData.Current, BannedTermCheck.Held);
            Expect.Equal(string.Join("|", expected.Sentences.Select(s => s.Text)), string.Join("|", sentences), "sentences in the generator's order");
            var sha = answer["extra"].GetProperty("sha256").GetString()!;
            Expect.Equal("script-changed", App.Post("/api/script-ticks?code=V-01", new { scriptSha256 = new string('0', 64), ticked = new[] { 1 } }).Code, "stale script");
            var four = App.Post("/api/script-ticks?code=V-01", new { scriptSha256 = sha, ticked = Enumerable.Range(1, sentences.Count - 1).ToArray() });
            Expect.True(four.Ok && !four["complete"].GetBoolean(), "four of five");
            Expect.Equal("script-not-ticked", App.PostFile("/api/recording?code=V-01&name=recording.wav", PilotTestEnvironment.Tone(Media, 5, "five.wav")).Code, "upload before all ticks");
            Expect.Equal("script-not-ticked", App.Post("/api/take-accept?code=V-01").Code, "take before all ticks");
            var five = App.Post("/api/script-ticks?code=V-01", new { scriptSha256 = sha, ticked = Enumerable.Range(1, sentences.Count).ToArray() });
            Expect.True(five.Ok && five["complete"].GetBoolean(), "five of five");
            r.Note($"{sentences.Count} sentences; script SHA-256 {sha}");
        });

        r.Add("T-017", "recording upload: 60.0 s is kept byte for byte, 61.0 s is refused as produce refuses it, a .webm upload is refused with the accepted list", () =>
        {
            var sixty = PilotTestEnvironment.Tone(Media, 60.0, "sixty.wav");
            var kept = App.PostFile("/api/recording?code=V-01&name=sixty.wav", sixty);
            Expect.True(kept.Ok, $"60.0 s accepted: {kept}");
            Expect.Equal(60.0, kept["recording"].GetProperty("seconds").GetDouble(), "length");
            Expect.Equal(MediaTool.Sha256File(sixty), MediaTool.Sha256File(Path.Combine(Input("V-01"), "recording.wav")), "byte for byte");
            var sixtyOne = PilotTestEnvironment.Tone(Media, 61.0, "sixty-one.wav", pitch: 610);
            var refused = App.PostFile("/api/recording?code=V-01&name=sixty-one.wav", sixtyOne);
            Expect.Equal("recording-too-long", refused.Code, "61.0 s");
            Expect.Equal(MediaTool.Sha256File(sixty), MediaTool.Sha256File(Path.Combine(Input("V-01"), "recording.wav")), "the refused upload replaced nothing");
            var direct = Path.Combine(_root, "direct-long");
            Fixtures.CopyFolder(Input("V-01"), direct, n => n != "recording.wav");
            File.Copy(sixtyOne, Path.Combine(direct, "recording.wav"));
            var (exit, output) = Direct.Run("produce", "--settings", SettingsPath, "--facts", Path.Combine(direct, Workspace.FactSheetFileName));
            Expect.Equal(2, exit, "direct produce exit");
            Expect.Equal(Direct.FirstLine(output), refused.Line.TrimEnd('\n'), "the app's refusal line equals produce's");
            PilotTestEnvironment.Draw(Media, "-f", "lavfi", "-i", "sine=f=440:r=48000:d=3", "-c:a", "libopus", "take.webm");
            var webm = App.PostFile("/api/recording?code=V-01&name=take.webm", Path.Combine(Media, "take.webm"));
            Expect.Equal("recording-format-refused", webm.Code, ".webm upload");
            foreach (var extension in Admission.RecordingExtensions)
            {
                Expect.Contains(extension, webm.Line, "the refusal lists the accepted types");
            }

            r.Note(refused.Line.TrimEnd('\n'));
        });

        r.Add("T-018", "browser take conversion: a take converts to one digest every time; an unknown take type is refused", () =>
        {
            PrepareVideo("V-02", "/san-pham/am-dun-mau");
            var take = Path.Combine(Media, "take.webm");
            var stored = App.PostFile("/api/take?code=V-02", take, "audio/webm;codecs=opus");
            Expect.True(stored.Ok, $"take stored: {stored}");
            Expect.Equal(MediaTool.Sha256File(take), stored["sha256"].GetString(), "take kept byte for byte");
            var first = App.Post("/api/take-accept?code=V-02");
            Expect.True(first.Ok, $"first conversion: {first}");
            var second = App.Post("/api/take-accept?code=V-02");
            Expect.True(App.PostFile("/api/take?code=V-02", take, "audio/webm").Ok, "same take again");
            var third = App.Post("/api/take-accept?code=V-02");
            var digests = new[] { first, second, third }.Select(a => a["recording"].GetProperty("sha256").GetString()).Distinct().ToList();
            Expect.Equal(1, digests.Count, "one digest for one take");
            Expect.Equal(digests[0], MediaTool.Sha256File(Path.Combine(Input("V-02"), "recording.wav")), "stored recording");
            var facts = new MediaTool(PilotSettings.Read(SettingsPath)).Probe("t", Path.Combine(Input("V-02"), "recording.wav"));
            Expect.True(facts.HasAudio && Math.Abs(facts.DurationSeconds - 3.0) < 0.1, $"converted length {facts.DurationSeconds}");
            Expect.Equal("take-format-refused", App.PostBytes("/api/take?code=V-02", [1, 2, 3], "audio/x-unknown").Code, "unknown type");
            r.Note($"conversion digest {digests[0]}, {facts.DurationSeconds:0.000} s");
        });

        r.Add("T-019", "presenter clip: landscape and 0.6 s longer are refused as produce refuses them; a matching portrait clip is accepted", () =>
        {
            var tone = PilotTestEnvironment.Tone(Media, Length, "tone-17.wav", pitch: 470);
            Expect.True(App.PostFile("/api/recording?code=V-01&name=recording.wav", tone).Ok, "17.4 s recording");
            var cases = new[]
            {
                (Width: 960, Height: 540, Seconds: Length, Code: "clip-not-portrait"),
                (Width: 540, Height: 960, Seconds: Length + 0.6, Code: "clip-length-mismatch"),
            };
            foreach (var c in cases)
            {
                var clip = PilotTestEnvironment.Clip(Media, c.Width, c.Height, c.Seconds, $"clip-{c.Width}-{c.Seconds:0.0}.mp4".Replace(',', '.'));
                var answer = App.PostFile($"/api/clip?code=V-01&name={Path.GetFileName(clip)}", clip);
                Expect.Equal(c.Code, answer.Code, $"{c.Width}x{c.Height} {c.Seconds} s");
                var direct = Path.Combine(_root, $"direct-{c.Code}");
                Fixtures.CopyFolder(Input("V-01"), direct);
                File.Copy(clip, Path.Combine(direct, "presenter.mp4"), overwrite: true);
                var (_, output) = Direct.Run("produce", "--settings", SettingsPath, "--facts", Path.Combine(direct, Workspace.FactSheetFileName));
                Expect.Equal(Direct.FirstLine(output), answer.Line.TrimEnd('\n'), $"{c.Code}: app line equals produce's");
                r.Note(answer.Line.TrimEnd('\n'));
            }

            Expect.Equal(0, Directory.EnumerateFiles(Input("V-01"), "presenter.*").Count(), "no refused clip was kept");
            var good = PilotTestEnvironment.Clip(Media, 540, 960, Length, "clip-good.mp4");
            var accepted = App.PostFile("/api/clip?code=V-01&name=clip-good.mp4", good);
            Expect.True(accepted.Ok, $"portrait clip: {accepted}");
            Expect.Equal(540, accepted["clip"].GetProperty("width").GetInt32(), "width");
            Expect.Equal(960, accepted["clip"].GetProperty("height").GetInt32(), "height");
        });

        r.Add("T-020", "produce: no pauses opens the timing editor; a bad line is refused; a too-short caption page is refused as produce refuses it; a second produce while one runs is refused; good timing produces a ready package", () =>
        {
            var first = App.Produce("V-01", ProduceLimit);
            Expect.Equal("pauses-not-found", first.Code, $"continuous tone: {first}");
            Expect.Equal("mo", first["timingEditor"].GetString(), "timing editor opened");
            Expect.Equal(2, first["exitCode"].GetInt32(), "exit 2");
            var direct = Path.Combine(_root, "direct-pauses");
            Fixtures.CopyFolder(Input("V-01"), direct);
            var (_, output) = Direct.Run("produce", "--settings", SettingsPath, "--facts", Path.Combine(direct, Workspace.FactSheetFileName));
            Expect.Equal(Direct.FirstLine(output), Direct.FirstLine(first.Line), "pauses-not-found line equals produce's");

            var form = App.Get("/api/timing?code=V-01");
            Expect.Equal(5, form["sentences"].GetArrayLength(), "one row per sentence");
            Expect.Equal(Length, form["recordingSeconds"].GetDouble(), "recording length");
            var invalid = App.Post("/api/timing?code=V-01", new { lines = new[] { "0.00 3.40", "x" } });
            Expect.Equal("timing-file-invalid", invalid.Code, "bad line");
            Expect.True(!File.Exists(Path.Combine(Input("V-01"), CaptionTiming.TimingFileName)), "nothing saved");

            string[] shortFirst = ["0.00 0.50", .. GoodTiming[1..]];
            Expect.True(App.Post("/api/timing?code=V-01", new { lines = shortFirst }).Ok, "short timing saved");
            var tooShort = App.Produce("V-01", ProduceLimit);
            Expect.Equal("caption-page-too-short", tooShort.Code, $"short page: {tooShort}");
            Expect.Equal("goi-y", tooShort["timingEditor"].GetString(), "timing editor suggested");
            Fixtures.CopyFolder(Input("V-01"), direct);
            var (_, shortOutput) = Direct.Run("produce", "--settings", SettingsPath, "--facts", Path.Combine(direct, Workspace.FactSheetFileName));
            Expect.Equal(Direct.FirstLine(shortOutput), Direct.FirstLine(tooShort.Line), "caption-page-too-short line equals produce's");

            Expect.True(App.Post("/api/timing?code=V-01", new { lines = GoodTiming }).Ok, "good timing saved");
            var started = App.Post("/api/produce?code=V-01");
            Expect.True(started.Ok, "produce started");
            var busy = App.Post("/api/produce?code=V-01");
            Expect.Equal(409, busy.Status, "second produce status");
            Expect.Equal("produce-busy", busy.Code, "second produce");
            var watch = Stopwatch.StartNew();
            Answer done;
            do
            {
                Thread.Sleep(1000);
                done = App.Get("/api/progress?code=V-01");
            }
            while (done["running"].GetBoolean() && watch.Elapsed < ProduceLimit);
            Expect.Equal("dat", done.Kind, $"produce done: {done}");
            Expect.Equal(0, done["exitCode"].GetInt32(), "exit 0");
            _package = done["package"].GetString();
            Expect.True(_package is not null && Directory.Exists(_package) && Path.GetDirectoryName(_package) == Path.Combine(Pilot, "runs"), $"package {_package}");
            r.Note($"package {_package} in {watch.Elapsed.TotalSeconds:0} s");
        });

        r.Add("T-021", "checklist: the package's 27 kit 04 items, B8 ticked by the toolchain, 26 for the approver, B3 and B5 carrying their notes", () =>
        {
            var list = App.Get("/api/checklist?code=V-01");
            Expect.True(list.Ok, $"checklist: {list}");
            var rows = list["rows"].EnumerateArray().ToList();
            Expect.Equal(27, rows.Count, "items");
            Expect.Equal(string.Join(",", HeldData.Current.Checklist.Select(i => i.Id)), string.Join(",", rows.Select(x => x.GetProperty("id").GetString())), "held order");
            var tool = rows.Where(x => !x.GetProperty("operatorItem").GetBoolean()).ToList();
            Expect.Equal("B8", string.Join(",", tool.Select(x => x.GetProperty("id").GetString())), "tool-ticked item");
            Expect.True(tool[0].GetProperty("toolTicked").GetBoolean(), "B8 ticked");
            Expect.Equal(26, list["operatorItems"].GetInt32(), "operator items");
            foreach (var noted in new[] { "B3", "B5" })
            {
                var row = rows.Single(x => x.GetProperty("id").GetString() == noted);
                Expect.True(row.GetProperty("operatorItem").GetBoolean() && !row.GetProperty("toolTicked").GetBoolean(), $"{noted} left to the approver");
                Expect.True(row.GetProperty("note").GetString()!.Length > 0, $"{noted} note");
            }

            Expect.True(list["matchesHeld"].GetBoolean() && list["approvable"].GetBoolean(), "approvable");
            var manifest = PackageFiles.ReadJson<PackageManifest>(Path.Combine(Package, PackageFiles.Manifest), "x", "x");
            Expect.Equal(manifest.VideoSha256, list["sha256"].GetString(), "shown digest is the manifest's");
            Expect.Equal(manifest.VideoSha256, MediaTool.Sha256File(Path.Combine(Package, PackageFiles.Video)), "video digest");
            using var video = App.Http.GetAsync("/api/package/video?code=V-01").GetAwaiter().GetResult();
            Expect.Equal(manifest.VideoSha256, Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(video.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult())), "previewed bytes are the package video");
        });

        r.Add("T-022", "approval: 25 of 26 ticks is refused with no approval written; 26 ticks and an employee on the CEO's written instruction approve by hash; verify then passes", () =>
        {
            var items = ChecklistView.OperatorItems(HeldData.Current);
            var sha = App.Get("/api/checklist?code=V-01")["sha256"].GetString()!;
            var approval = Path.Combine(Package, PackageFiles.Approval);
            var partial = App.Post("/api/approve?code=V-01", new { sha256 = sha, ticked = items.Take(25).ToArray(), pressedBy = "ceo", employeeName = (string?)null });
            Expect.Equal("checklist-incomplete", partial.Code, "25 ticks");
            Expect.True(!File.Exists(approval), "no approval at 25");
            var nobody = App.Post("/api/approve?code=V-01", new { sha256 = sha, ticked = items.ToArray(), pressedBy = "", employeeName = (string?)null });
            Expect.Equal("pressed-by-missing", nobody.Code, "who pressed");
            Expect.True(App.Post("/api/checklist-ticks?code=V-01", new { ticked = items.ToArray() })["ticked"].GetInt32() == 26, "ticks saved");
            Expect.Equal("tick-unknown", App.Post("/api/checklist-ticks?code=V-01", new { ticked = new[] { "B8" } }).Code, "B8 is the toolchain's");
            var approved = App.Post("/api/approve?code=V-01", new { sha256 = sha, ticked = items.ToArray(), pressedBy = "nhan-vien", employeeName = EmployeeName });
            Expect.True(approved.Ok, $"approved: {approved}");
            Expect.Equal(0, approved["extra"].GetProperty("verifyExit").GetInt32(), "verify after approval");
            Expect.True(approved["extra"].GetProperty("verify").GetProperty("line").GetString()!.StartsWith("ĐẠT [verify]", StringComparison.Ordinal), "verify line");
            var record = PackageFiles.ReadJson<ApprovalRecord>(approval, "x", "x");
            Expect.Equal(ApproverText.Compose(CeoName, EmployeeName), record.ApprovedBy, "approver text");
            Expect.Equal(sha, record.VideoSha256, "approved digest");
            var verify = App.Post("/api/verify?code=V-01");
            Expect.True(verify.Ok && verify.Line.Contains(record.ApprovedBy, StringComparison.Ordinal), $"verify: {verify.Line}");
            r.Note(approved.Line.TrimEnd('\n'));
        });

        r.Add("T-023", "a one-byte change to a package video is refused at approval (approval-hash-mismatch) with no approval written, as the approve verb refuses it", () =>
        {
            var copy = CopyPackage("doi-mot-byte", n => n is not (PackageFiles.Approval or PackageFiles.LedgerEntry));
            FlipByte(Path.Combine(copy, PackageFiles.Video));
            UsePackage(copy);
            var items = ChecklistView.OperatorItems(HeldData.Current);
            var sha = App.Get("/api/checklist?code=V-01")["sha256"].GetString()!;
            var answer = App.Post("/api/approve?code=V-01", new { sha256 = sha, ticked = items.ToArray(), pressedBy = "ceo", employeeName = (string?)null });
            Expect.Equal("approval-hash-mismatch", answer.Code, $"changed video: {answer}");
            Expect.True(!File.Exists(Path.Combine(copy, PackageFiles.Approval)), "no approval written");
            var (exit, output) = Direct.Run("approve", "--package", copy, "--name", ApproverText.Compose(CeoName, null), "--sha256", sha);
            Expect.Equal(2, exit, "direct approve exit");
            Expect.Equal(Direct.FirstLine(output), Direct.FirstLine(answer.Line), "line equals the approve verb's");
            UsePackage(Package);
        });

        r.Add("T-024", "verify through the app gives the verify verb's line for a manifest naming a path, a package missing its ledger entry, and an approved video changed after approval", () =>
        {
            var notPlain = CopyPackage("ten-tep-la", n => n != PackageFiles.LedgerEntry);
            var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(notPlain, PackageFiles.Manifest)))!.AsObject();
            manifest["videoFile"] = "..\\video.mp4";
            File.WriteAllText(Path.Combine(notPlain, PackageFiles.Manifest), manifest.ToJsonString(), new UTF8Encoding(false));
            var noEntry = CopyPackage("khong-muc-so", n => n != PackageFiles.LedgerEntry);
            var changed = CopyPackage("da-duyet-roi-doi", n => n != PackageFiles.LedgerEntry);
            Expect.True(File.Exists(Path.Combine(changed, PackageFiles.Approval)), "the copy carries the approval");
            FlipByte(Path.Combine(changed, PackageFiles.Video));
            foreach (var (folder, code) in new[] { (notPlain, "package-file-not-plain"), (noEntry, "package-ledger-mismatch"), (changed, "approval-void") })
            {
                UsePackage(folder);
                var answer = App.Post("/api/verify?code=V-01");
                Expect.Equal(code, answer.Code, $"verify {Path.GetFileName(folder)}");
                var (_, output) = Direct.Run("verify", "--package", folder);
                Expect.Equal(Direct.FirstLine(output), Direct.FirstLine(answer.Line), $"{code} line equals the verify verb's");
                r.Note(Direct.FirstLine(answer.Line));
            }

            UsePackage(Package);
            Expect.True(App.Post("/api/verify?code=V-01").Ok, "the real package still verifies");
        });

        r.Add("T-025", "a recording already used by another video is refused at step 4 as produce refuses it (recording-reused)", () =>
        {
            var used = Path.Combine(Input("V-01"), "recording.wav");
            var before = MediaTool.Sha256File(Path.Combine(Input("V-02"), "recording.wav"));
            var answer = App.PostFile("/api/recording?code=V-02&name=recording.wav", used);
            Expect.Equal("recording-reused", answer.Code, $"reused: {answer}");
            Expect.Equal(before, MediaTool.Sha256File(Path.Combine(Input("V-02"), "recording.wav")), "V-02 keeps its own recording");
            var direct = Path.Combine(_root, "direct-reused");
            Fixtures.CopyFolder(Input("V-02"), direct);
            File.Copy(used, Path.Combine(direct, "recording.wav"), overwrite: true);
            var (exit, output) = Direct.Run("produce", "--settings", SettingsPath, "--facts", Path.Combine(direct, Workspace.FactSheetFileName));
            Expect.Equal(2, exit, "direct produce exit");
            Expect.Equal(Direct.FirstLine(output), Direct.FirstLine(answer.Line), "line equals produce's");
            r.Note(Direct.FirstLine(answer.Line));
        });

        r.Add("T-026", "settings loosened by hand are refused (settings-looser-than-ceo) with the toolchain's own line", () =>
        {
            var original = File.ReadAllBytes(SettingsPath);
            try
            {
                var settings = JsonNode.Parse(original)!.AsObject();
                settings["clipToleranceSeconds"] = 0.6;
                File.WriteAllText(SettingsPath, settings.ToJsonString(), new UTF8Encoding(false));
                var answer = App.Get("/api/video?code=V-01");
                Expect.Equal("settings-looser-than-ceo", answer.Code, $"loosened: {answer}");
                var (_, output) = Direct.Run("check", "--settings", SettingsPath, "--facts", Path.Combine(Input("V-01"), Workspace.FactSheetFileName));
                Expect.Equal(Direct.FirstLine(output), Direct.FirstLine(answer.Line), "line equals the check verb's");
                r.Note(Direct.FirstLine(answer.Line));
            }
            finally
            {
                File.WriteAllBytes(SettingsPath, original);
            }

            Expect.True(App.Get("/api/video?code=V-01").Ok, "restored settings accepted");
        });

        r.Add("T-011", "six foreign requests (no Origin, foreign Origin, no token, wrong token, foreign Host, PUT) are refused and change no file", () =>
        {
            var before = Fixtures.Hashes(Pilot).Where(p => !p.Key.EndsWith(WebLog.FileName, StringComparison.Ordinal)).ToDictionary();
            var appBefore = Fixtures.Hashes(AppFolder);
            StringContent Sheet() => new(Fixtures.Text(Fixtures.Sheet("V-01")), Encoding.UTF8, "application/json");
            var cases = new (string What, Func<Answer> Send, int Status, string Code)[]
            {
                ("POST without Origin", () => App.Send("/api/facts?code=V-01", Sheet(), origin: null), 403, "origin-not-allowed"),
                ("POST from a foreign Origin", () => App.Send("/api/approve?code=V-01", Json(new { sha256 = "x", ticked = Array.Empty<string>(), pressedBy = "ceo" }), origin: "http://evil.example"), 403, "origin-not-allowed"),
                ("POST without token", () => App.Send("/api/setup", Json(new { pilotFolder = "C:\\x", ceoName = "X", createShortcut = true }), token: null), 403, "token-not-valid"),
                ("POST with a wrong token", () => App.Send("/api/produce?code=V-01", Json(new { }), token: new string('a', 64)), 403, "token-not-valid"),
                ("GET with a foreign Host", () => App.Send("/api/videos", Json(new { }), method: "GET", host: "evil.example"), 403, "host-not-allowed"),
                ("PUT", () => App.Send("/api/facts?code=V-01", Sheet(), method: "PUT"), 405, "method-not-allowed"),
            };
            foreach (var c in cases)
            {
                var answer = c.Send();
                Expect.Equal(c.Status, answer.Status, $"{c.What} status");
                Expect.Equal(c.Code, answer.Code, $"{c.What} code");
                r.Note($"{c.What}: {answer.Status} {answer.Code}");
            }

            var after = Fixtures.Hashes(Pilot).Where(p => !p.Key.EndsWith(WebLog.FileName, StringComparison.Ordinal)).ToDictionary();
            Expect.Equal(string.Join("|", before.OrderBy(p => p.Key).Select(p => p.Key + p.Value)), string.Join("|", after.OrderBy(p => p.Key).Select(p => p.Key + p.Value)), "pilot folder unchanged");
            Expect.Equal(string.Join("|", appBefore.Values.Order()), string.Join("|", Fixtures.Hashes(AppFolder).Values.Order()), "app folder unchanged");
            r.Note($"{before.Count} pilot files unchanged");
        });

        r.Add("T-027", "a second start with the same application folder opens the running one and exits without listening", () =>
        {
            using var second = AppProcess.Launch(["--port", "0", "--app-folder", AppFolder, "--desktop-folder", Desktop, "--no-browser"]);
            var output = second.StandardOutput.ReadToEndAsync();
            Expect.True(second.WaitForExit(30000), "the second start exits");
            Expect.Equal(0, second.ExitCode, "exit 0");
            Expect.Contains(App.Origin, output.Result, "it names the running address");
            Expect.True(!output.Result.Contains(Web.Program.ReadyPrefix, StringComparison.Ordinal), "it never listened");
            r.Note(output.Result.Trim());
        });

        r.Add("T-028", "the app's log holds route, status, code, exit and time only: no token, no CEO name, no query, no file content", () =>
        {
            var log = File.ReadAllText(Path.Combine(Pilot, "web", WebLog.FileName));
            Expect.True(log.Length > 0, "log written");
            foreach (var secret in new[] { App.Token, CeoName, EmployeeName, "code=", "Bình giữ nhiệt", Pilot })
            {
                Expect.True(!log.Contains(secret, StringComparison.Ordinal), $"log contains \"{secret}\"");
            }

            r.Note($"{log.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length} log lines");
        });
    }

    /// <summary>The cases that change the ledger, and the harness evidence; they run after every other case.</summary>
    public void AddClosing()
    {
        r.Add("T-040", "a ledger that lost an entry is refused (ledger-entry-missing) through the app with the check verb's line", () =>
        {
            var ledger = Path.Combine(Pilot, Ledger.FileName);
            var original = File.ReadAllBytes(ledger);
            try
            {
                var emptied = JsonNode.Parse(original)!.AsObject();
                emptied["entries"] = new JsonArray();
                File.WriteAllText(ledger, emptied.ToJsonString(), new UTF8Encoding(false));
                var answer = App.Post("/api/script?code=V-01");
                Expect.Equal("ledger-entry-missing", answer.Code, $"emptied ledger: {answer}");
                var (_, output) = Direct.Run("check", "--settings", SettingsPath, "--facts", Path.Combine(Input("V-01"), Workspace.FactSheetFileName));
                Expect.Equal(Direct.FirstLine(output), Direct.FirstLine(answer.Line), "line equals the check verb's");
                r.Note(Direct.FirstLine(answer.Line));
            }
            finally
            {
                File.WriteAllBytes(ledger, original);
            }
        });

        r.Add("T-041", "a deleted ledger with its previous generation present is refused (ledger-truncated) through the app with the check verb's line", () =>
        {
            var ledger = Path.Combine(Pilot, Ledger.FileName);
            var previous = Path.Combine(Pilot, Ledger.PreviousFileName);
            var original = File.ReadAllBytes(ledger);
            var hadPrevious = File.Exists(previous);
            try
            {
                if (!hadPrevious)
                {
                    File.WriteAllBytes(previous, original);
                }

                File.Delete(ledger);
                var answer = App.Post("/api/script?code=V-01");
                Expect.Equal("ledger-truncated", answer.Code, $"deleted ledger: {answer}");
                var (_, output) = Direct.Run("check", "--settings", SettingsPath, "--facts", Path.Combine(Input("V-01"), Workspace.FactSheetFileName));
                Expect.Equal(Direct.FirstLine(output), Direct.FirstLine(answer.Line), "line equals the check verb's");
                r.Note(Direct.FirstLine(answer.Line));
            }
            finally
            {
                File.WriteAllBytes(ledger, original);
                if (!hadPrevious)
                {
                    File.Delete(previous);
                }
            }

            Expect.True(App.Post("/api/script?code=V-01").Ok, "restored ledger accepted");
        });

        r.Add("T-042", "socket tables over the whole flow: the toolchain and every ffmpeg and ffprobe the app or the toolchain started own no socket; the app's own rows are loopback only", () =>
        {
            var members = _harness.Members;
            var rows = _harness.Rows;
            var app = App.Process.Id;
            var names = members.Where(m => m.Key != app).Select(m => m.Value).ToList();
            Expect.True(names.Contains("MediaCompany.Pilot"), $"toolchain runs observed: {string.Join(", ", names.Distinct())}");
            Expect.True(names.Contains("ffmpeg") && names.Contains("ffprobe"), "ffmpeg and ffprobe observed");
            var foreign = rows.Where(row => row.Owner != app).ToList();
            Expect.Equal(0, foreign.Count, $"rows owned by the toolchain or its tools: {string.Join("; ", foreign)}");
            var own = rows.Where(row => row.Owner == app).ToList();
            var notLoopback = own.Where(row => !row.IsLoopback).ToList();
            Expect.Equal(0, notLoopback.Count, $"app rows not on loopback: {string.Join("; ", notLoopback)}");
            r.Note($"{_harness.Samples} samples; {members.Count} member processes ({names.Count(n => n == "MediaCompany.Pilot")} toolchain, {names.Count(n => n == "ffmpeg")} ffmpeg, {names.Count(n => n == "ffprobe")} ffprobe, {names.Count(n => n is not ("MediaCompany.Pilot" or "ffmpeg" or "ffprobe"))} other: {string.Join(", ", names.Where(n => n is not ("MediaCompany.Pilot" or "ffmpeg" or "ffprobe")).Distinct())})");
            r.Note($"0 rows owned by non-app members; {own.Count} distinct app rows, all loopback; e.g. {string.Join("; ", own.Take(3))}");
        });
    }

    private static StringContent Json(object value) => new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    /// <summary>A second video brought to step 4: sheet, images, script, every sentence ticked.</summary>
    private void PrepareVideo(string code, string pagePath)
    {
        Expect.True(App.PostFile($"/api/screenshot?code={code}&name=chup-1.png", Path.Combine(Stills, "chup-1.png")).Ok, $"{code} screenshot");
        Expect.True(App.Post($"/api/facts?code={code}", Fixtures.Sheet(code, pagePath)).Ok, $"{code} sheet");
        foreach (var name in new[] { "anh-1.png", "anh-2.png", "anh-3.png" })
        {
            Expect.True(App.PostFile($"/api/image?code={code}&name={name}&source={name}", Path.Combine(Stills, name)).Ok, $"{code} {name}");
        }

        var script = App.Post($"/api/script?code={code}");
        Expect.True(script.Ok, $"{code} script: {script}");
        var count = script["extra"].GetProperty("sentences").GetArrayLength();
        Expect.True(App.Post($"/api/script-ticks?code={code}", new { scriptSha256 = script["extra"].GetProperty("sha256").GetString(), ticked = Enumerable.Range(1, count).ToArray() })["complete"].GetBoolean(), $"{code} ticks");
    }

    private string CopyPackage(string suffix, Func<string, bool> keep)
    {
        var copy = Path.Combine(Pilot, "runs", $"{Path.GetFileName(Package)}-{suffix}");
        Fixtures.CopyFolder(Package, copy, keep);
        return copy;
    }

    private void UsePackage(string folder)
    {
        var state = JsonNode.Parse(File.ReadAllText(StatePath("V-01")))!.AsObject();
        state["lastPackage"] = folder;
        File.WriteAllText(StatePath("V-01"), state.ToJsonString(), new UTF8Encoding(false));
    }

    private static void FlipByte(string file)
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);
        stream.Position = stream.Length / 2;
        var value = stream.ReadByte();
        stream.Position = stream.Length / 2;
        stream.WriteByte((byte)(value ^ 0x01));
    }

    public void Dispose()
    {
        _app?.Dispose();
        _harness.Dispose();
    }

    internal static string Invariant(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);
}
