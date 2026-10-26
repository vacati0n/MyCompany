using System.Globalization;

namespace MediaCompany.Pilot;

/// <summary>The outcome of one produce: the package folder, its manifest and the ledger entry when the package is ready.</summary>
public sealed record ProduceResult(string PackageFolder, PackageManifest Manifest, LedgerEntry? Entry);

/// <summary>The run record of a produce, written into the package, or into the refused folder when the run is refused.</summary>
public sealed record RunRecord
{
    public required int FormVersion { get; init; }
    public required string VideoCode { get; init; }
    public required string Outcome { get; init; }
    public required string? RefusalCode { get; init; }
    public required string? RefusalMessage { get; init; }
    public required IReadOnlyList<RunCheck> Checks { get; init; }
    public required GuardResult? Guard { get; init; }
    public required int SocketEvents { get; init; }
    public required string? TimingSource { get; init; }
    public required IReadOnlyList<DrawnCharacter> DrawnCharacters { get; init; }
    public required IReadOnlyList<string> Captions { get; init; }
    public required IReadOnlyList<ScriptSentence> Provenance { get; init; }
    public required string? FaceFamily { get; init; }
    public required IReadOnlyList<ToolInvocation> ToolLog { get; init; }
    public required string RecordedAt { get; init; }
}

/// <summary>
/// THE PRODUCE VERB (design O-002 end to end). In order: the static network guard; the fact sheet; the ledger limits; the approved
/// script; the tool and face digests; the recording; the presenter clip; the safe-area layout and the face's coverage of every drawn
/// character; the sentence windows; the composition; the composed video's checks; the package. A refusal at any step ends the run
/// with a run record naming the step and no video. A package whose own checks fail is written as held, never as ready.
/// </summary>
public sealed class Producer(PilotSettings settings, HeldData held, BannedTermCheck banned, Func<DateTimeOffset> clock)
{
    public const string ScriptFileName = "script.json";
    public const string ScriptTextFileName = "script.md";

    private readonly List<RunCheck> _checks = [];
    private MediaTool? _tool;
    private GuardResult? _guard;
    private string? _timingSource;
    private IReadOnlyList<DrawnCharacter> _drawn = [];
    private IReadOnlyList<string> _captions = [];
    private IReadOnlyList<ScriptSentence> _provenance = [];
    private string? _faceFamily;

    public IReadOnlyList<RunCheck> Checks => _checks;

    public ProduceResult Produce(string factSheetPath)
    {
        using var telemetry = new SocketTelemetry();
        var code = "unknown";
        try
        {
            _guard = NetworkGuard.Check();
            Pass("network-guard-static", _guard.Passed, _guard.Passed ? $"closure {string.Join(", ", _guard.Closure)}" : string.Join("; ", _guard.Findings));
            if (!_guard.Passed)
            {
                throw new PilotRefusal("network-guard-failed", $"Bộ công cụ không qua kiểm tra mạng: {string.Join("; ", _guard.Findings)}");
            }

            if (!File.Exists(factSheetPath))
            {
                throw new PilotRefusal("fact-sheet-missing", $"Không thấy bảng thông tin {factSheetPath}.");
            }

            var inputFolder = Path.GetDirectoryName(Path.GetFullPath(factSheetPath))!;
            var sheetBytes = File.ReadAllBytes(factSheetPath);
            var sheet = FactSheetCheck.Check(sheetBytes, inputFolder, banned);
            code = sheet.VideoCode;
            Pass("fact-sheet", true, $"{sheet.Facts.Count} dòng thông tin, {sheet.ImagesUsed.Count} ảnh");
            var ledger = Ledger.Open(settings.PilotFolder, held);
            ledger.RefuseOverLimits(sheet);
            Pass("ledger-limits", true, $"{ledger.Current.Count} video trong sổ, chuỗi băm hợp lệ");

            var scriptPath = Path.Combine(inputFolder, ScriptFileName);
            if (!File.Exists(scriptPath))
            {
                throw new PilotRefusal("script-missing", $"Không thấy {ScriptFileName} trong thư mục đầu vào; chạy lệnh script trước.");
            }

            var scriptBytes = File.ReadAllBytes(scriptPath);
            var script = ScriptGenerator.Approved(scriptBytes, sheet, held, banned);
            Pass("script", true, $"{script.Sentences.Count} câu, SHA-256 {ScriptGenerator.Sha256(scriptBytes)}");

            if (settings.Frame.Width != 1080 || settings.Frame.Height != 1920)
            {
                throw new PilotRefusal("frame-not-vertical", $"Khung đã cấu hình là {settings.Frame.Width}x{settings.Frame.Height}; video phải là 1080x1920 (9:16).");
            }

            _tool = new MediaTool(settings);
            Pass("tool-digests", true, $"ffmpeg {_tool.MediaToolDigest}, ffprobe {_tool.ProbeToolDigest}; {_tool.VersionLine}");
            var face = Face.Admit(settings.Face);
            _faceFamily = $"{face.Reading.Family} {face.Reading.Subfamily} {face.Reading.Version}";
            Pass("face-digest", true, $"{face.Sha256} = {face.ManifestLine}");

            var recording = Admission.Recording(inputFolder, _tool, held, ledger, sheet.VideoCode);
            Pass("recording", true, string.Create(CultureInfo.InvariantCulture,
                $"{recording.Name}, {recording.Facts.DurationSeconds:0.000} s ≤ {held.RecordingMaximumSeconds:0.0} s, SHA-256 {recording.Sha256} chưa dùng cho video khác"));
            var clip = Admission.Clip(inputFolder, _tool, recording, settings.ClipToleranceSeconds);
            Pass("presenter-clip", true, string.Create(CultureInfo.InvariantCulture,
                $"{clip.Name}, {clip.Facts.Displayed.Width}x{clip.Facts.Displayed.Height}, {clip.Facts.DurationSeconds:0.000} s, lệch {Math.Abs(clip.Facts.DurationSeconds - recording.Facts.DurationSeconds):0.000} s ≤ {settings.ClipToleranceSeconds:0.0} s"));

            var layout = CaptionLayout.Boxes(settings, face.Metrics, held);
            Pass("safe-area", true, $"vùng an toàn {layout.Safe}; lớp nhãn {layout.OverlayBox}; phụ đề {layout.CaptionBox}");
            var drawn = new List<(string Where, string Text)> { ("lớp nhãn", held.OverlayText) };
            drawn.AddRange(script.Sentences.Select(s => ($"câu {s.Number}", s.Text)));
            _provenance = script.Sentences;
            _drawn = Face.DrawnCharacters(face, drawn.Select(d => d.Text));
            Face.RequireCoverage(face, drawn);
            Pass("glyph-coverage", true, $"{_drawn.Count} ký tự khác nhau, phông chữ có đủ");

            var (windows, source, timingBytes) = CaptionTiming.Windows(inputFolder, recording, script.Sentences.Count, _tool, settings);
            _timingSource = source;
            var pages = CaptionLayout.Pages(script.Sentences, windows, layout, settings, face.Metrics);
            _captions = script.Sentences.Select(s => CaptionLayout.Rejoined(pages, s.Number)).ToList();
            Pass("caption-timing", true, string.Create(CultureInfo.InvariantCulture,
                $"{windows.Count} khoảng câu từ {source}, {pages.Count} trang phụ đề, mỗi trang ít nhất {settings.CaptionPageMinimumSeconds:0.0#} giây"));
            var changed = script.Sentences.FirstOrDefault(s => !string.Equals(CaptionLayout.Rejoined(pages, s.Number), s.Text, StringComparison.Ordinal));
            if (changed is not null)
            {
                throw new PilotRefusal("caption-text-changed",
                    $"Phụ đề của câu {changed.Number} ghép lại là \"{CaptionLayout.Rejoined(pages, changed.Number)}\", khác câu trong kịch bản \"{changed.Text}\".");
            }

            Pass("caption-text", true, $"phụ đề của {script.Sentences.Count} câu ghép lại đúng từng chữ của kịch bản");

            var stamp = clock().UtcDateTime.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
            var package = Path.Combine(settings.PilotFolder, "runs", $"{Safe(sheet.VideoCode)}-{stamp}");
            var work = Path.Combine(package, "work");
            var inputs = Path.Combine(package, PackageFiles.Inputs);
            Directory.CreateDirectory(work);
            Directory.CreateDirectory(inputs);
            File.WriteAllBytes(Path.Combine(work, Composition.FaceFileName), face.Bytes.ToArray());

            // Each input is read once: those bytes are hashed, compared with the digest admitted earlier where there is one, written
            // into the package, and the composition reads the package copies, so the video is made from the bytes that were hashed.
            var inputList = new List<PackageInput>();
            var staged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string Stage(string role, string full, byte[]? alreadyRead = null, string? admitted = null)
            {
                var name = Path.GetFileName(full);
                var target = Path.Combine(inputs, name);
                if (!staged.TryGetValue(name, out var digest))
                {
                    var bytes = alreadyRead ?? File.ReadAllBytes(full);
                    digest = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
                    File.WriteAllBytes(target, bytes);
                    staged[name] = digest;
                }

                if (admitted is not null && !string.Equals(digest, admitted, StringComparison.OrdinalIgnoreCase))
                {
                    throw new PilotRefusal("input-changed",
                        $"Tệp {name} đã đổi trong lúc chạy: SHA-256 lúc kiểm là {admitted}, lúc chép vào gói là {digest}. Không dùng tệp này; chạy lại.");
                }

                inputList.Add(new PackageInput(role, name, digest));
                return target;
            }

            Stage("fact-sheet", Path.GetFullPath(factSheetPath), sheetBytes);
            Stage("script", scriptPath, scriptBytes);
            var stagedRecording = recording with { FullPath = Stage("recording", recording.FullPath, admitted: recording.Sha256) };
            var stagedClip = clip with { FullPath = Stage("presenter-clip", clip.FullPath, admitted: clip.Sha256) };
            var images = sheet.ImagesUsed.Select(n => Stage("image", Path.Combine(inputFolder, n))).ToList();
            foreach (var screenshot in sheet.Facts.Select(f => Path.Combine(inputFolder, f.Screenshot)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                Stage("screenshot", screenshot);
            }

            if (_timingSource == "timing-file")
            {
                Stage("timing-file", Path.Combine(inputFolder, CaptionTiming.TimingFileName), timingBytes);
            }

            var plan = new CompositionPlan(work, images, stagedClip, stagedRecording, layout, pages, held.OverlayText, Path.Combine(package, PackageFiles.Video));
            Composition.WriteTextFiles(plan);
            _tool.Media("compose video", Composition.Arguments(plan, settings), work);
            Directory.Delete(work, recursive: true);

            var check = Composition.Check(_tool, plan.OutputFile, layout, recording.Facts.DurationSeconds);
            var vertical = check.Width == 1080 && check.Height == 1920;
            Pass("video-vertical", vertical, $"ffprobe {check.Width}x{check.Height}");
            var lengthOk = Math.Abs(check.DurationSeconds - recording.Facts.DurationSeconds) <= 0.1 && check.HasAudio;
            Pass("video-length", lengthOk, string.Create(CultureInfo.InvariantCulture, $"video {check.DurationSeconds:0.000} s, bản ghi {recording.Facts.DurationSeconds:0.000} s, có tiếng: {check.HasAudio}"));
            var overlay = new RunCheck("overlay-present", check.OverlayBrightestAtStart >= 200 && check.OverlayBrightestAtEnd >= 200,
                $"chữ lớp nhãn sáng nhất {check.OverlayBrightestAtStart} ở 0,5 giây và {check.OverlayBrightestAtEnd} ở khung cuối (cần ≥ 200)");
            _checks.Add(overlay);
            var disclosure = pages.First(p => p.Sentence == script.Sentences.First(x => x.Kind == SentenceKind.OpeningDisclosure).Number && p.Page == 1);
            Composition.ExtractFrame(_tool, plan.OutputFile, Math.Round((disclosure.Start + disclosure.End) / 2, 3), Path.Combine(package, PackageFiles.Frame));
            Pass("network-guard-running", telemetry.Events == 0, $"{telemetry.Events} sự kiện socket hoặc phân giải tên trong tiến trình");

            var held_ = _checks.Where(c => !c.Passed).Select(c => $"{c.Name}: {c.Detail}").ToList();
            var status = held_.Count == 0 ? "ready" : "held";
            var videoSha = MediaTool.Sha256File(plan.OutputFile);

            File.WriteAllBytes(Path.Combine(package, PackageFiles.Script), scriptBytes);
            File.WriteAllText(Path.Combine(package, PackageFiles.ScriptText), ScriptGenerator.Render(script, sheet), new System.Text.UTF8Encoding(false));
            PackageFiles.WriteJson(Path.Combine(package, PackageFiles.Captions), pages);
            File.WriteAllText(Path.Combine(package, PackageFiles.Checklist),
                Checklist.Render(held, sheet.VideoCode, Checklist.Decide(held, banned, script, overlay)), new System.Text.UTF8Encoding(false));

            LedgerEntry? entry = null;
            if (status == "ready")
            {
                entry = ledger.Append(sheet, recording.Sha256, videoSha, package, clock());
                File.WriteAllBytes(Path.Combine(package, PackageFiles.LedgerEntry), Ledger.SerializeEntry(entry));
            }

            var manifest = new PackageManifest
            {
                FormVersion = 1,
                VideoCode = sheet.VideoCode,
                Status = status,
                HeldReasons = held_,
                VideoFile = PackageFiles.Video,
                VideoSha256 = videoSha,
                ScriptSha256 = ScriptGenerator.Sha256(scriptBytes),
                RecordingSha256 = recording.Sha256,
                Inputs = inputList,
                MediaToolPath = settings.MediaTool.Path,
                MediaToolSha256 = _tool.MediaToolDigest,
                ProbeToolPath = settings.ProbeTool.Path,
                ProbeToolSha256 = _tool.ProbeToolDigest,
                MediaToolVersionLine = _tool.VersionLine,
                FacePath = face.Path,
                FaceSha256 = face.Sha256,
                FaceManifestLine = face.ManifestLine,
                Checks = [.. _checks],
                LedgerEntryHash = entry?.EntryHash,
                ProducedAt = clock().UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            };
            PackageFiles.WriteJson(Path.Combine(package, PackageFiles.Manifest), manifest);
            PackageFiles.WriteJson(Path.Combine(package, PackageFiles.RunRecord), Record(sheet.VideoCode, status, null, telemetry.Events));
            return new ProduceResult(package, manifest, entry);
        }
        catch (PilotRefusal refusal)
        {
            WriteRefused(code, refusal.Code, refusal.Message, telemetry.Events);
            throw;
        }
        catch (PilotToolFailure failure)
        {
            WriteRefused(code, "tool-failed", failure.Message, telemetry.Events);
            throw;
        }
    }

    private void Pass(string name, bool passed, string detail) => _checks.Add(new RunCheck(name, passed, detail));

    private RunRecord Record(string code, string outcome, PilotRefusal? refusal, int sockets) => new()
    {
        FormVersion = 1,
        VideoCode = code,
        Outcome = outcome,
        RefusalCode = refusal?.Code,
        RefusalMessage = refusal?.Message,
        Checks = [.. _checks],
        Guard = _guard,
        SocketEvents = sockets,
        TimingSource = _timingSource,
        DrawnCharacters = _drawn,
        Captions = _captions,
        Provenance = _provenance,
        FaceFamily = _faceFamily,
        ToolLog = _tool?.Log.ToList() ?? [],
        RecordedAt = clock().UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
    };

    private void WriteRefused(string code, string refusalCode, string message, int sockets)
    {
        try
        {
            var folder = Path.Combine(settings.PilotFolder, "refused");
            Directory.CreateDirectory(folder);
            var stamp = clock().UtcDateTime.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
            var record = Record(code, "refused", new PilotRefusal(refusalCode, message), sockets) with
            {
                Checks = [.. _checks, new RunCheck(refusalCode, false, message)],
            };
            PackageFiles.WriteJson(Path.Combine(folder, $"{Safe(code)}-{stamp}-{refusalCode}.json"), record);
        }
        catch (IOException)
        {
            // The refusal itself is what the operator must see; a run record that cannot be written does not hide it.
        }
    }

    private static string Safe(string code) =>
        new([.. code.Select(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_')]);
}
