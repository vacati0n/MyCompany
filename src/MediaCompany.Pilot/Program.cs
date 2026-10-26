using System.Text;

namespace MediaCompany.Pilot;

/// <summary>
/// The operator's five verbs (design O-002): check, script, produce, approve, verify. Messages are Vietnamese; every refusal also
/// prints its English code, which the operator guide lists with its remedy. Exit 0 done, 1 usage, 2 refused, 3 tool failed.
/// </summary>
public static class Program
{
    public const string Usage =
        "Cách dùng:\n"
        + "  MediaCompany.Pilot check   --settings <tệp-cài-đặt.json> --facts <bảng-thông-tin.json>\n"
        + "  MediaCompany.Pilot script  --settings <tệp-cài-đặt.json> --facts <bảng-thông-tin.json>\n"
        + "  MediaCompany.Pilot produce --settings <tệp-cài-đặt.json> --facts <bảng-thông-tin.json>\n"
        + "  MediaCompany.Pilot approve --package <thư-mục-gói> --name <tên-người-duyệt> --sha256 <mã-băm-video>\n"
        + "  MediaCompany.Pilot verify  --package <thư-mục-gói>\n";

    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        return Run(args, Console.Out, () => DateTimeOffset.Now);
    }

    public static int Run(string[] args, TextWriter output, Func<DateTimeOffset> clock)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(clock);
        if (args.Length == 0 || !TryOptions(args.Skip(1).ToArray(), out var options))
        {
            output.Write(Usage);
            return PilotExit.Usage;
        }

        try
        {
            return args[0] switch
            {
                "check" => Check(Need(options, "settings"), Need(options, "facts"), output),
                "script" => WriteScript(Need(options, "settings"), Need(options, "facts"), output),
                "produce" => Produce(Need(options, "settings"), Need(options, "facts"), output, clock),
                "approve" => Approve(Need(options, "package"), Need(options, "name"), Need(options, "sha256"), output, clock),
                "verify" => Verify(Need(options, "package"), output),
                _ => UsageExit(output),
            };
        }
        catch (UsageException)
        {
            output.Write(Usage);
            return PilotExit.Usage;
        }
        catch (PilotRefusal refusal)
        {
            output.WriteLine($"TỪ CHỐI [{refusal.Code}]: {refusal.Message}");
            return PilotExit.Refused;
        }
        catch (PilotToolFailure failure)
        {
            output.WriteLine($"LỖI CÔNG CỤ [tool-failed]: {failure.Message}");
            return PilotExit.ToolFailed;
        }
    }

    private static int Check(string settingsPath, string facts, TextWriter output)
    {
        var settings = PilotSettings.Read(settingsPath);
        var held = HeldData.Current;
        var sheet = FactSheetCheck.Read(facts, BannedTermCheck.Held);
        var ledger = Ledger.Open(settings.PilotFolder, held);
        ledger.RefuseOverLimits(sheet);
        output.WriteLine($"ĐẠT [check]: bảng thông tin {sheet.VideoCode} hợp lệ: {sheet.Facts.Count} dòng thông tin, {sheet.ImagesUsed.Count} ảnh; sổ theo dõi có {ledger.Current.Count} video, chuỗi băm hợp lệ.");
        return PilotExit.Done;
    }

    private static int WriteScript(string settingsPath, string facts, TextWriter output)
    {
        _ = PilotSettings.Read(settingsPath);
        var held = HeldData.Current;
        var sheet = FactSheetCheck.Read(facts, BannedTermCheck.Held);
        var script = ScriptGenerator.Generate(sheet, held, BannedTermCheck.Held);
        var folder = Path.GetDirectoryName(Path.GetFullPath(facts))!;
        var bytes = ScriptGenerator.Serialize(script);
        File.WriteAllBytes(Path.Combine(folder, Producer.ScriptFileName), bytes);
        File.WriteAllText(Path.Combine(folder, Producer.ScriptTextFileName), ScriptGenerator.Render(script, sheet), new UTF8Encoding(false));
        output.WriteLine($"ĐẠT [script]: đã viết {Producer.ScriptFileName} và {Producer.ScriptTextFileName} ({script.Sentences.Count} câu), SHA-256 kịch bản {ScriptGenerator.Sha256(bytes)}.");
        foreach (var sentence in script.Sentences)
        {
            output.WriteLine($"  {sentence.Number}. {sentence.Text}");
        }

        return PilotExit.Done;
    }

    private static int Produce(string settingsPath, string facts, TextWriter output, Func<DateTimeOffset> clock)
    {
        var settings = PilotSettings.Read(settingsPath);
        var result = new Producer(settings, HeldData.Current, BannedTermCheck.Held, clock).Produce(facts);
        var manifest = result.Manifest;
        var word = manifest.Status == "ready" ? "ĐẠT [produce]" : "GIỮ LẠI [package-held]";
        output.WriteLine($"{word}: gói {manifest.VideoCode} ở {result.PackageFolder}");
        output.WriteLine($"  video: {Path.Combine(result.PackageFolder, manifest.VideoFile)}");
        output.WriteLine($"  SHA-256 video: {manifest.VideoSha256}");
        foreach (var check in manifest.Checks)
        {
            output.WriteLine($"  {(check.Passed ? "đạt" : "KHÔNG ĐẠT")} {check.Name}: {check.Detail}");
        }

        return manifest.Status == "ready" ? PilotExit.Done : PilotExit.Refused;
    }

    private static int Approve(string package, string name, string sha256, TextWriter output, Func<DateTimeOffset> clock)
    {
        var record = Approval.Approve(package, name, sha256, clock());
        output.WriteLine($"ĐẠT [approve]: {record.ApprovedBy} duyệt video {record.VideoCode} ngày {record.ApprovedOn}, SHA-256 {record.VideoSha256}.");
        return PilotExit.Done;
    }

    private static int Verify(string package, TextWriter output)
    {
        var (manifest, approval, sha) = Approval.Verify(package);
        var approved = approval is null ? "chưa duyệt" : $"đã duyệt bởi {approval.ApprovedBy} ngày {approval.ApprovedOn}";
        output.WriteLine($"ĐẠT [verify]: video {manifest.VideoCode} vẫn có SHA-256 {sha}; trạng thái gói {manifest.Status}; {approved}.");
        return PilotExit.Done;
    }

    private static int UsageExit(TextWriter output)
    {
        output.Write(Usage);
        return PilotExit.Usage;
    }

    private static string Need(Dictionary<string, string> options, string name) =>
        options.TryGetValue(name, out var value) && value.Length > 0 ? value : throw new UsageException();

    private static bool TryOptions(string[] rest, out Dictionary<string, string> options)
    {
        options = new Dictionary<string, string>(StringComparer.Ordinal);
        if (rest.Length % 2 != 0)
        {
            return false;
        }

        for (var i = 0; i < rest.Length; i += 2)
        {
            if (!rest[i].StartsWith("--", StringComparison.Ordinal) || !options.TryAdd(rest[i][2..], rest[i + 1]))
            {
                return false;
            }
        }

        return true;
    }

    private sealed class UsageException : Exception;
}
