using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using MediaCompany.Pilot.Tests;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>
/// The recording step in a real browser with no microphone: the digest-checked headless shell, its fake capture device fed from a WAV
/// drawn at test time, and the page's own recorder driven by the test driver the app serves only in test mode. The browser's
/// loopback connection is observed in its own harness and reported apart from the toolchain's count.
/// </summary>
internal static class BrowserCase
{
    private const string Code = "V-03";
    private const string Shell = "chrome-headless-shell-win64/chrome-headless-shell.exe";

    private static string BrowserRoot => Path.Combine(Path.GetDirectoryName(PilotTestEnvironment.FontFolder.TrimEnd(Path.DirectorySeparatorChar))!, "browser");

    public static void Add(Runner r)
    {
        r.Add("T-030", "browser recording with a fake microphone: the page stops the first take at 60 s by itself, plays it back, accepts it; a re-record stopped by hand after about 3 s replaces it", () =>
        {
            var graphics = Path.GetDirectoryName(BrowserRoot)!;
            var shell = Path.Combine(BrowserRoot, Shell.Replace('/', Path.DirectorySeparatorChar));
            var manifest = Path.Combine(graphics, "manifests", "browser.sha256");
            var line = File.ReadAllLines(manifest).Single(l => l.EndsWith(" *" + Shell, StringComparison.Ordinal));
            var expected = line[..64];
            string actual;
            using (var stream = File.OpenRead(shell))
            {
                actual = Convert.ToHexStringLower(SHA256.HashData(stream));
            }

            Expect.Equal(expected, actual, "headless shell digest against browser.sha256");
            r.Note($"headless shell {shell} SHA-256 {actual} matches {manifest}");

            var root = PilotTestEnvironment.NewFolder("w-browser");
            var stills = Fixtures.Stills(Path.Combine(root, "stills"));
            var pilot = Path.Combine(root, "pilot");
            var resultPath = Path.Combine(root, "result.json");
            var harness = new AddressHarness();
            var browserHarness = new AddressHarness();
            Process? browser = null;
            try
            {
                using var app = AppProcess.Start(Path.Combine(root, "app"), Path.Combine(root, "desktop"), harness, "--test-mode", "--test-result", resultPath);
                Expect.True(app.Post("/api/setup", new { pilotFolder = pilot, ceoName = "Nguyễn Văn An", createShortcut = false }).Ok, "setup");
                Expect.True(app.PostFile($"/api/screenshot?code={Code}&name=chup-1.png", Path.Combine(stills, "chup-1.png")).Ok, "screenshot");
                Expect.True(app.Post($"/api/facts?code={Code}", Fixtures.Sheet(Code, "/san-pham/hop-com-mau")).Ok, "sheet");
                foreach (var name in new[] { "anh-1.png", "anh-2.png", "anh-3.png" })
                {
                    Expect.True(app.PostFile($"/api/image?code={Code}&name={name}&source={name}", Path.Combine(stills, name)).Ok, name);
                }

                var script = app.Post($"/api/script?code={Code}");
                Expect.True(script.Ok, $"script: {script}");
                var count = script["extra"].GetProperty("sentences").GetArrayLength();
                Expect.True(app.Post($"/api/script-ticks?code={Code}", new { scriptSha256 = script["extra"].GetProperty("sha256").GetString(), ticked = Enumerable.Range(1, count).ToArray() })["complete"].GetBoolean(), "ticks");

                var wav = PilotTestEnvironment.Recording(root, 5, 3.0, 0.6, "fake-mic.wav");
                var profile = Path.Combine(root, "profile");
                var start = new ProcessStartInfo(shell) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
                foreach (var argument in new[]
                {
                    $"--user-data-dir={profile}",
                    "--use-fake-device-for-media-stream",
                    "--use-fake-ui-for-media-stream",
                    $"--use-file-for-fake-audio-capture={wav}",
                    "--autoplay-policy=no-user-gesture-required",
                    "--no-first-run",
                    "--no-default-browser-check",
                    "--no-proxy-server",
                    "--disable-background-networking",
                    "--disable-component-update",
                    "--disable-sync",
                    "--disable-default-apps",
                    "--disable-extensions",
                    "--metrics-recording-only",
                    "--disable-domain-reliability",
                    "--disable-features=MediaRouter,OptimizationHints,Translate,AsyncDns,DnsOverHttps",
                    "--host-resolver-rules=MAP * ~NOTFOUND , EXCLUDE 127.0.0.1",
                    "--webrtc-ip-handling-policy=disable_non_proxied_udp",
                    "--force-webrtc-ip-handling-policy",
                    app.Origin + "/#test-record=" + Code,
                })
                {
                    start.ArgumentList.Add(argument);
                }

                browser = Process.Start(start) ?? throw new CaseFailure("the headless shell did not start");
                browserHarness.Track(browser);
                _ = browser.StandardOutput.ReadToEndAsync();
                _ = browser.StandardError.ReadToEndAsync();
                var watch = Stopwatch.StartNew();
                while (!File.Exists(resultPath) && watch.Elapsed < TimeSpan.FromMinutes(4) && !browser.HasExited)
                {
                    Thread.Sleep(500);
                }

                Expect.True(File.Exists(resultPath), $"the driver posted no result in {watch.Elapsed.TotalSeconds:0} s (browser exited: {browser.HasExited})");
                Thread.Sleep(200);
                using var result = JsonDocument.Parse(File.ReadAllText(resultPath));
                var root0 = result.RootElement;
                r.Note($"driver result: {root0.GetRawText()}");
                Expect.True(root0.GetProperty("ok").GetBoolean(), $"driver error: {(root0.TryGetProperty("error", out var e) ? e.GetString() : "?")}");
                Expect.Equal(60, root0.GetProperty("limit").GetInt32(), "page limit");
                var first = root0.GetProperty("first");
                Expect.Equal("gioi-han", first.GetProperty("stoppedBy").GetString(), "first take stopped by the page");
                Expect.True(Math.Abs(first.GetProperty("seconds").GetDouble() - 60.0) < 0.01, $"first take timer {first.GetProperty("seconds").GetDouble()}");
                Expect.True(root0.GetProperty("firstPlayback").GetProperty("ended").GetBoolean(), "first playback to the end");
                var firstAccept = root0.GetProperty("firstAccept");
                var firstSeconds = firstAccept.GetProperty("seconds").GetDouble();
                Expect.True(firstSeconds is >= 59.0 and <= 60.0, $"first accepted length {firstSeconds}");
                var second = root0.GetProperty("second");
                Expect.Equal("nguoi-dung", second.GetProperty("stoppedBy").GetString(), "second take stopped by hand");
                Expect.True(root0.GetProperty("secondPlayback").GetProperty("ended").GetBoolean(), "second playback to the end");
                var secondSeconds = root0.GetProperty("secondAccept").GetProperty("seconds").GetDouble();
                Expect.True(secondSeconds is >= 2.0 and <= 5.0, $"second accepted length {secondSeconds}");
                var stored = Path.Combine(pilot, "inputs", Code, "recording.wav");
                var probe = new MediaTool(PilotSettings.Read(Path.Combine(pilot, Setup.SettingsFileName))).Probe("t", stored);
                Expect.True(Math.Abs(probe.DurationSeconds - secondSeconds) < 0.05, $"stored recording is the second take ({probe.DurationSeconds})");
                r.Note($"first take {firstSeconds:0.000} s stopped by the page; second take {secondSeconds:0.000} s stopped by hand; stored {probe.DurationSeconds:0.000} s");

                // Review UX (a): the "recording in use" line changes as soon as a take is accepted, to that take.
                foreach (var (accept, current) in new[] { ("firstAccept", "firstCurrent"), ("secondAccept", "secondCurrent") })
                {
                    var shown = root0.GetProperty(current).GetString() ?? "";
                    var sha = root0.GetProperty(accept).GetProperty("sha256").GetString() ?? "";
                    Expect.True(sha.Length == 64 && shown.Contains(sha, StringComparison.Ordinal), $"{current} after accept names the accepted take: \"{shown}\"");
                    Expect.True(!shown.Contains("chưa có bản ghi", StringComparison.Ordinal), $"{current} still says no recording: \"{shown}\"");
                }

                // Review UX (b): **bold** renders as <b> built from text; markup in the data stays text; an unclosed marker is dropped.
                Expect.Equal("Kiểm <b>giá</b> và &lt;b&gt;x&lt;/b&gt; đậm", root0.GetProperty("richSample").GetString(), "checklist bold rendering");

                var appRows = harness.Rows;
                var foreign = appRows.Where(row => row.Owner != app.Process.Id).ToList();
                Expect.Equal(0, foreign.Count, $"toolchain or tool rows: {string.Join("; ", foreign)}");
                Expect.True(appRows.All(row => row.IsLoopback), "app rows loopback");
                var browserRows = browserHarness.Rows;
                // TCP rows carry both addresses, so every browser connection is shown to be loopback. A UDP row carries only the local
                // address (the operating system's UDP table has no remote column); such a row is reported, not asserted (residual R-005).
                var outside = browserRows.Where(row => row.Protocol.StartsWith("tcp", StringComparison.Ordinal) && !row.IsLoopback).ToList();
                Expect.Equal(0, outside.Count, $"browser TCP rows off loopback: {string.Join("; ", outside)}");
                var udp = browserRows.Where(row => row.Protocol.StartsWith("udp", StringComparison.Ordinal)).ToList();
                r.Note($"browser UDP rows (no remote column, reported only): {(udp.Count == 0 ? "none" : string.Join("; ", udp))}");
                r.Note($"app harness: {harness.Members.Count} members, {appRows.Count} rows, all owned by the app and on loopback; ffmpeg runs {harness.Members.Count(m => m.Name == "ffmpeg")}");
                r.Note($"browser harness (kept apart, Q-003): {browserHarness.Members.Count} processes, {browserRows.Count} distinct rows, TCP all loopback: {string.Join("; ", browserRows.Select(x => $"{x.Protocol} {x.Local}:{x.LocalPort}->{x.Remote}:{x.RemotePort}").Distinct().Take(6))}");
            }
            finally
            {
                if (browser is { HasExited: false })
                {
                    browser.Kill(entireProcessTree: true);
                    browser.WaitForExit(10000);
                }

                browser?.Dispose();
                harness.Dispose();
                browserHarness.Dispose();
            }
        });
    }
}
