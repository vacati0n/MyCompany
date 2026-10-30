using System.Diagnostics;

namespace MediaCompany.Pilot.Web;

/// <summary>One produce job: the video, the running clock, and the toolchain's result when it ends.</summary>
public sealed class ProduceJob(string code)
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private volatile bool _running = true;

    public string Code { get; } = code;

    public bool Running => _running;

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
        _running = false;
    }
}

/// <summary>
/// PRODUCE IN THE BACKGROUND (design decision D-009): one produce job at a time for the whole app; the page polls a read-only progress
/// route for the elapsed time and the running state, because the toolchain prints only when it ends; a second produce while one runs
/// is refused in Vietnamese.
/// <para>
/// A job always ends (review correction CR-004): whatever the work throws, and even if it returns without finishing the job, the job
/// is finished as an app fault, so a later produce is never refused by a job that has stopped.
/// </para>
/// <para>
/// THE LEDGER TURN (CEO-D-1312 item 2): a produce job holds the pilot ledger while it runs, because the toolchain child writes it. A
/// step-4 recording admission, which reads the ledger in this process, runs only through <see cref="OutsideProduce{T}"/>: it waits
/// while a produce job runs and refuses in Vietnamese if the job outlasts the wait; and a produce does not start while an admission
/// runs. So the app never reads the ledger while the toolchain writes it.
/// </para>
/// </summary>
public sealed class ProduceJobs
{
    /// <summary>How long a step-4 admission waits for a running produce, and a produce start for a running admission.</summary>
    public static readonly TimeSpan LedgerWait = TimeSpan.FromMinutes(3);

    private readonly object _gate = new();
    private readonly Dictionary<string, ProduceJob> _latest = new(StringComparer.Ordinal);
    private ProduceJob? _running;
    private int _admissions;

    public ProduceJob Start(string code, Func<ProduceJob, Task> work) => Start(code, work, LedgerWait);

    public ProduceJob Start(string code, Func<ProduceJob, Task> work, TimeSpan admissionWait)
    {
        ArgumentNullException.ThrowIfNull(work);
        ProduceJob job;
        lock (_gate)
        {
            RefuseWhileProducing();
            var watch = Stopwatch.StartNew();
            while (_admissions > 0)
            {
                var left = admissionWait - watch.Elapsed;
                if (left <= TimeSpan.Zero)
                {
                    throw new WebRefusal("recording-check-busy",
                        "Ứng dụng đang kiểm một bản ghi ở bước 4 nên chưa tạo video được. Chờ bước 4 báo xong rồi bấm \"Tạo video\" lại.", 409);
                }

                Monitor.Wait(_gate, left);
                RefuseWhileProducing();
            }

            job = new ProduceJob(code);
            _running = job;
            _latest[code] = job;
        }

        _ = Task.Run(() => RunJob(job, work));
        return job;
    }

    /// <summary>
    /// Runs one step that reads the ledger (a step-4 recording admission) when no produce job runs: it waits up to
    /// <paramref name="wait"/> for a running job to end, then refuses in Vietnamese. While the step runs, no produce starts.
    /// </summary>
    public T OutsideProduce<T>(Func<T> step, TimeSpan wait)
    {
        ArgumentNullException.ThrowIfNull(step);
        lock (_gate)
        {
            var watch = Stopwatch.StartNew();
            while (_running is { Running: true } busy)
            {
                var left = wait - watch.Elapsed;
                if (left <= TimeSpan.Zero)
                {
                    throw new WebRefusal("ledger-busy",
                        $"Đang tạo video {busy.Code} (đã chạy {busy.ElapsedSeconds:0} giây), bộ công cụ đang ghi sổ theo dõi nên chưa kiểm được bản ghi. Chờ tạo video xong rồi bấm lại; bản ghi vừa thu vẫn được giữ.", 409);
                }

                Monitor.Wait(_gate, left);
            }

            _admissions++;
        }

        try
        {
            return step();
        }
        finally
        {
            lock (_gate)
            {
                _admissions--;
                Monitor.PulseAll(_gate);
            }
        }
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

    /// <summary>The view of a job that ended by an exception, or without a result: an app fault naming the exception's type.</summary>
    public static OutcomeView Fault(Exception? error) =>
        new(Outcomes.AppFault, null, "app-fault",
            error is null
                ? "LỖI ỨNG DỤNG [app-fault]: việc tạo video dừng mà không có kết quả.\n"
                : $"LỖI ỨNG DỤNG [app-fault]: {error.GetType().Name}: {error.Message}\n",
            "Việc tạo video dừng giữa chừng vì một lỗi của ứng dụng. Có thể bấm \"Tạo video\" lại.", Outcomes.TechnicalNext);

    private void RefuseWhileProducing()
    {
        if (_running is { Running: true } busy)
        {
            throw new WebRefusal("produce-busy",
                $"Đang tạo video {busy.Code} (đã chạy {busy.ElapsedSeconds:0} giây). Mỗi lần chỉ tạo một video; chờ xong rồi bấm lại.", 409);
        }
    }

    private async Task RunJob(ProduceJob job, Func<ProduceJob, Task> work)
    {
        try
        {
            await work(job).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            // The last line of defence: the work's own handler finishes and logs every failure; whatever still escapes ends the job.
            if (job.Running)
            {
                job.Finish(null, Fault(error), null);
            }
        }
        finally
        {
            if (job.Running)
            {
                job.Finish(null, Fault(null), null);
            }

            lock (_gate)
            {
                Monitor.PulseAll(_gate);
            }
        }
    }
}
