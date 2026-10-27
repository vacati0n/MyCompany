using System.Diagnostics;

namespace MediaCompany.Pilot.Web;

/// <summary>One produce job: the video, the running clock, and the toolchain's result when it ends.</summary>
public sealed class ProduceJob(string code)
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public string Code { get; } = code;

    public bool Running { get; private set; } = true;

    public double ElapsedSeconds => Math.Round(_clock.Elapsed.TotalSeconds, 1);

    public ToolchainRun? Result { get; private set; }

    public OutcomeView? View { get; private set; }

    public string? PackageFolder { get; private set; }

    internal void Finish(ToolchainRun? result, OutcomeView view, string? packageFolder)
    {
        Result = result;
        View = view;
        PackageFolder = packageFolder;
        _clock.Stop();
        Running = false;
    }
}

/// <summary>
/// PRODUCE IN THE BACKGROUND (design decision D-009): one produce job at a time for the whole app; the page polls a read-only progress
/// route for the elapsed time and the running state, because the toolchain prints only when it ends; a second produce while one runs
/// is refused in Vietnamese.
/// </summary>
public sealed class ProduceJobs
{
    private readonly object _gate = new();
    private readonly Dictionary<string, ProduceJob> _latest = new(StringComparer.Ordinal);
    private ProduceJob? _running;

    public ProduceJob Start(string code, Func<ProduceJob, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        ProduceJob job;
        lock (_gate)
        {
            if (_running is { Running: true } busy)
            {
                throw new WebRefusal("produce-busy",
                    $"Đang tạo video {busy.Code} (đã chạy {busy.ElapsedSeconds:0} giây). Mỗi lần chỉ tạo một video; chờ xong rồi bấm lại.", 409);
            }

            job = new ProduceJob(code);
            _running = job;
            _latest[code] = job;
        }

        _ = Task.Run(() => work(job));
        return job;
    }

    public ProduceJob? Latest(string code)
    {
        lock (_gate)
        {
            return _latest.TryGetValue(code, out var job) ? job : null;
        }
    }

    public static void Finish(ProduceJob job, ToolchainRun? result, OutcomeView view, string? packageFolder)
    {
        ArgumentNullException.ThrowIfNull(job);
        job.Finish(result, view, packageFolder);
    }
}
