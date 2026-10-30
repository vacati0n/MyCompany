using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MediaCompany.Pilot.Tests;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>
/// The in-process cases of the MC-17 review corrections: the ledger turn between step-4 admission and produce (CR-001, CEO-D-1312), a
/// produce job that always ends (CR-004), and the shortcut failure shown as a refusal (CR-006).
/// </summary>
internal static class CorrectionCases
{
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);

    public static void Add(Runner r)
    {
        r.Add("T-031", "ledger turn (CEO-D-1312): a step-4 admission waits while a produce job runs and runs only after it ends; it refuses (ledger-busy) when the job outlasts the wait; a produce waits for a running admission and refuses (recording-check-busy) when it outlasts the wait", () =>
        {
            var jobs = new ProduceJobs();
            using var release = new ManualResetEventSlim(false);
            using var started = new ManualResetEventSlim(false);
            var job = jobs.Start("V-01", _ =>
            {
                started.Set();
                release.Wait(Long);
                return Task.CompletedTask;
            });
            Expect.True(started.Wait(Long), "the job started");

            var refused = Expect.Throws<WebRefusal>(() => jobs.OutsideProduce(() => 1, TimeSpan.FromMilliseconds(200)), "admission during produce");
            Expect.Equal("ledger-busy", refused.Code, "admission refusal code");
            Expect.Equal(409, refused.Status, "admission refusal status");
            Expect.Contains("V-01", refused.Message, "the running video is named");

            var jobRunningInside = (bool?)null;
            var admission = Task.Run(() => jobs.OutsideProduce(() =>
            {
                jobRunningInside = job.Running;
                return 7;
            }, Long));
            Thread.Sleep(500);
            Expect.True(!admission.IsCompleted, "the admission waits while the job runs");
            release.Set();
            Expect.True(admission.Wait(Long), "the admission ran after the job ended");
            Expect.Equal(7, admission.Result, "the admission's own result");
            Expect.Equal(false, jobRunningInside, "no produce job ran while the admission ran");

            using var holding = new ManualResetEventSlim(false);
            using var letGo = new ManualResetEventSlim(false);
            var held = Task.Run(() => jobs.OutsideProduce(() =>
            {
                holding.Set();
                letGo.Wait(Long);
                return 0;
            }, Long));
            Expect.True(holding.Wait(Long), "an admission is running");
            var busy = Expect.Throws<WebRefusal>(() => jobs.Start("V-02", _ => Task.CompletedTask, TimeSpan.FromMilliseconds(200)), "produce during admission");
            Expect.Equal("recording-check-busy", busy.Code, "produce refusal code");
            Expect.Equal(null, jobs.Latest("V-02"), "no job was recorded for the refused start");

            ProduceJob? waited = null;
            var start = Task.Run(() => waited = jobs.Start("V-02", _ => Task.CompletedTask, Long));
            Thread.Sleep(500);
            Expect.True(!start.IsCompleted, "the produce start waits while the admission runs");
            letGo.Set();
            Expect.True(start.Wait(Long) && held.Wait(Long), "the produce started after the admission ended");
            Expect.True(waited is not null && SpinWait.SpinUntil(() => !waited.Running, Long), "the second job ended");
        });

        r.Add("T-032", "a produce job always ends (CR-004): work that throws, and work that returns without finishing the job, end it as an app fault; the next produce is not refused", () =>
        {
            var jobs = new ProduceJobs();
            var thrown = jobs.Start("V-01", _ => throw new InvalidOperationException("hỏng giữa chừng"));
            Expect.True(SpinWait.SpinUntil(() => !thrown.Running, Long), "a job whose work throws ends");
            Expect.Equal("app-fault", thrown.View?.Code, "thrown: code");
            Expect.Contains("InvalidOperationException: hỏng giữa chừng", thrown.View?.Line, "thrown: the exception is named");

            var silent = jobs.Start("V-01", _ => Task.CompletedTask);
            Expect.True(SpinWait.SpinUntil(() => !silent.Running, Long), "a job whose work returns without finishing ends");
            Expect.Equal("app-fault", silent.View?.Code, "silent: code");

            var faulted = jobs.Start("V-01", _ => Task.FromException(new TimeoutException("hết giờ")));
            Expect.True(SpinWait.SpinUntil(() => !faulted.Running, Long), "a job whose task faults ends");
            Expect.Contains("TimeoutException", faulted.View?.Line, "faulted: the exception is named");
            Expect.True(ReferenceEquals(faulted, jobs.Latest("V-01")), "the last job is the one shown");
        });

        r.Add("T-033", "the produce work (CR-004): an exception of any type from the toolchain call or from recording the package ends the job with a category and a log line; a refusal is shown as a refusal", () =>
        {
            var pilot = PilotTestEnvironment.NewFolder("w-producework");
            var workspace = new Workspace(new AppRecord
            {
                FormVersion = AppRecord.KnownFormVersion,
                PilotFolder = pilot,
                SettingsPath = Path.Combine(pilot, "settings.json"),
                ToolchainPath = Path.Combine(pilot, "toolchain.exe"),
                ToolchainSha256 = new string('0', 64),
                Port = 1,
                CeoName = "Nguyễn Văn An",
            });
            var logFolder = PilotTestEnvironment.NewFolder("w-producework-log");
            var log = new WebLog(() => logFolder);
            var package = Path.Combine(workspace.RunsFolder, "goi-1");
            var written = new ToolchainRun("produce", 0, $"XONG\n  video: {Path.Combine(package, "video.mp4")}\n", 1.5);
            var cases = new (string Name, Func<ToolchainRun> Produce, Action Before, string Kind, string Code, string LineHas, bool PackageShown)[]
            {
                ("the toolchain call throws NotSupportedException", () => throw new NotSupportedException("không hỗ trợ"), () => { }, Outcomes.AppFault, "app-fault", "NotSupportedException: không hỗ trợ", false),
                ("the toolchain call throws a step refusal", () => throw new WebRefusal("tool-changed", "Tệp chạy đã đổi."), () => { }, Outcomes.Refused, "tool-changed", "Tệp chạy đã đổi.", false),
                ("the video state cannot be written (IOException: the web folder is a file)", () => written, () => File.WriteAllText(workspace.WebFolder, "x"), Outcomes.AppFault, "app-fault", "IOException", true),
            };
            var lines = 0;
            foreach (var c in cases)
            {
                c.Before();
                var job = new ProduceJob("V-01");
                Routes.ProduceWork(job, c.Produce, workspace, "V-01", log);
                Expect.True(!job.Running, $"{c.Name}: the job ended");
                Expect.Equal(c.Kind, job.View?.Kind, $"{c.Name}: category");
                Expect.Equal(c.Code, job.View?.Code, $"{c.Name}: code");
                Expect.Contains(c.LineHas, job.View?.Line, $"{c.Name}: line");
                if (c.PackageShown)
                {
                    Expect.Equal(package, job.PackageFolder, $"{c.Name}: the written package is still shown");
                    Expect.Contains(package, job.View?.Reason, $"{c.Name}: the reason names the package");
                }

                var logged = File.ReadAllLines(log.FilePath);
                Expect.Equal(++lines, logged.Length, $"{c.Name}: one log line");
                Expect.Contains("\tJOB\t/api/produce\t", logged[^1], $"{c.Name}: the log line");
                r.Note($"{c.Name}: {job.View?.Line.Trim()}");
            }
        });

        r.Add("T-034", "a shortcut that cannot be saved is refused (shortcut-failed) in Vietnamese, naming the program to open instead", () =>
        {
            var desktop = PilotTestEnvironment.NewFolder("w-shortcut");
            Directory.CreateDirectory(Path.Combine(desktop, Shortcut.FileName));
            var target = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName;
            var refused = Expect.Throws<WebRefusal>(() => Shortcut.Create(desktop, target), "save over a folder");
            Expect.Equal("shortcut-failed", refused.Code, "code");
            Expect.Contains(Path.GetFileName(target), refused.Message, "the program is named");
            Expect.Contains("Cài đặt không bị ảnh hưởng", refused.Message, "setup is said to be unaffected");
            r.Note(refused.Message);
        });

        r.Add("T-035", "socket harness and reused process ids: a row is kept only for a process in the job now; an id that was a member once and now belongs to a process outside the job is not counted, and a job member's own socket still is", () =>
        {
            var outsider = new SocketRow(7788, "?", "tcp", IPAddress.Loopback, 60784, IPAddress.Loopback, 59163, 5);
            var member = outsider with { Owner = 4242, LocalPort = 50000 };
            var kept = AddressHarness.OwnedByMembers([outsider, member], [4242], []).ToList();
            Expect.Equal(1, kept.Count, "rows kept from the before-list");
            Expect.Equal(4242, kept[0].Owner, "the kept row is the current member's");
            Expect.Equal(1, AddressHarness.OwnedByMembers([outsider, member], [], [4242]).Count(), "a member seen only in the after-list is kept");
            Expect.Equal(0, AddressHarness.OwnedByMembers([outsider, member], [], []).Count(), "no current member, no row");

            // Live: this test process (outside the job) listens on loopback while its id is recorded as an ended member, the reuse case
            // T-042 met; a child in the job listens too and must still be seen.
            using var harness = new AddressHarness();
            harness.RecordEndedMember(Environment.ProcessId, "MediaCompany.Pilot");
            var own = new TcpListener(IPAddress.Loopback, 0);
            own.Start();
            using var child = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -Command \"$l=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0);$l.Start();Start-Sleep -Seconds 30\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            })!;
            try
            {
                harness.Track(child);
                var deadline = DateTime.UtcNow + Long;
                while (DateTime.UtcNow < deadline && !harness.Rows.Any(row => row.Owner == child.Id && row.State == SocketRow.Listen))
                {
                    Thread.Sleep(50);
                }

                var rows = harness.Rows;
                var childRows = rows.Where(row => row.Owner == child.Id && row.State == SocketRow.Listen).ToList();
                Expect.True(childRows.Count > 0, "the job member's listener is seen");
                var stale = rows.Where(row => row.Owner == Environment.ProcessId).ToList();
                Expect.Equal(0, stale.Count, $"rows of the reused id outside the job: {string.Join("; ", stale)}");
                Expect.True(harness.Members.Any(m => m.Id == child.Id && m.Name == "powershell"), "the child is a member by its own name");
                r.Note($"{harness.Samples} samples; kept {childRows[0]}; this process ({Environment.ProcessId}, recorded as an ended member) listening on {own.LocalEndpoint} was not counted");
            }
            finally
            {
                own.Stop();
                if (!child.HasExited)
                {
                    child.Kill(entireProcessTree: true);
                }

                child.WaitForExit();
            }
        });
    }
}
