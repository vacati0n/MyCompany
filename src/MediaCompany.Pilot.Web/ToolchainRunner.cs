using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MediaCompany.Pilot.Web;

/// <summary>One run of a toolchain verb: the verb, the exit status, the printed text exactly as printed, and the time taken.</summary>
public sealed record ToolchainRun(string Verb, int ExitCode, string Output, double Seconds);

/// <summary>
/// THE TOOLCHAIN CHILD (design decision D-001). check, script, produce, approve and verify are decided by the MC-16 toolchain executable,
/// started from its own build output (so its network guard reads its own dependency file) as a child process: the executable's
/// SHA-256 is compared with the one setup recorded before every start; arguments go as an argument list, never through a shell; the
/// output is read as UTF-8; the child runs in a kill-on-close job, so it and every ffmpeg or ffprobe it starts end when the job is
/// closed; a child that overruns the bound is ended with its job and reported as a tool failure.
/// </summary>
public sealed class ToolchainRunner(string executable, string expectedSha256)
{
    public static readonly IReadOnlySet<string> Verbs = new HashSet<string>(["check", "script", "produce", "approve", "verify"], StringComparer.Ordinal);

    public string Executable { get; } = executable;

    public ToolchainRun Run(string verb, IReadOnlyList<string> options, TimeSpan bound)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Verbs.Contains(verb))
        {
            throw new ArgumentException($"unknown toolchain verb {verb}", nameof(verb));
        }

        RequireDigest();
        var start = new ProcessStartInfo(Executable)
        {
            WorkingDirectory = Path.GetDirectoryName(Executable)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
        };
        start.ArgumentList.Add(verb);
        foreach (var option in options)
        {
            start.ArgumentList.Add(option);
        }

        var stopwatch = Stopwatch.StartNew();
        using var job = ChildJob.Create();
        using var process = Process.Start(start)
            ?? throw new WebRefusal("toolchain-not-started", "Không khởi động được bộ công cụ. Báo người phụ trách kỹ thuật.", 500);
        try
        {
            job.Assign(process);
        }
        catch
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(bound))
        {
            job.Close();
            process.WaitForExit();
            var printed = output.GetAwaiter().GetResult();
            return new ToolchainRun(verb, -1,
                printed + string.Create(CultureInfo.InvariantCulture,
                    $"LỖI CÔNG CỤ [tool-failed]: bộ công cụ chạy quá {bound.TotalSeconds:0} giây và đã bị dừng cùng mọi tiến trình nó khởi động.\n"),
                Math.Round(stopwatch.Elapsed.TotalSeconds, 3));
        }

        process.WaitForExit();
        job.Close();
        var text = output.GetAwaiter().GetResult();
        var errorText = errors.GetAwaiter().GetResult();
        if (process.ExitCode is not (PilotExit.Done or PilotExit.Refused) && errorText.Length > 0)
        {
            text += errorText;
        }

        return new ToolchainRun(verb, process.ExitCode, text, Math.Round(stopwatch.Elapsed.TotalSeconds, 3));
    }

    /// <summary>Refuses to start a toolchain executable that is missing or whose digest differs from the one setup recorded.</summary>
    public void RequireDigest()
    {
        if (!File.Exists(Executable))
        {
            throw new WebRefusal("toolchain-missing",
                $"Không thấy bộ công cụ ở {Executable}. Mở màn hình cài đặt và lưu lại; nếu vẫn thiếu, báo người phụ trách kỹ thuật.");
        }

        var actual = MediaTool.Sha256File(Executable);
        if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new WebRefusal("toolchain-changed",
                $"Bộ công cụ ở {Executable} có SHA-256 {actual}, khác bản đã ghi lúc cài đặt ({expectedSha256}). Không chạy bản lạ. Mở màn hình cài đặt và lưu lại nếu người phụ trách kỹ thuật vừa cập nhật bộ công cụ.");
        }
    }
}
