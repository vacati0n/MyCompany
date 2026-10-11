using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using MediaCompany.Application.Ports;

namespace MediaCompany.Production;

/// <summary>
/// THE ONE PROCESS STARTER, its graphics part (the graphics change, decisions D-002 and D-007 of its design). It starts the
/// approved renderer: Node, located by setting, running the repository's staged render script with exactly three arguments,
/// in a CLEARED environment, inside a job object that ends every member when it is closed; the contact sheet and the preview
/// are two more invocations of the installed media tool. No transaction is held by this type.
/// </summary>
public sealed partial class ExternalMediaTool : IGraphicRenderer
{
    /// <summary>
    /// One render-script invocation. Refused by name, before any process, where Node, the staged render script or request, the
    /// browser or a face is absent. The child's environment is CLEARED and holds only the system root, a search path of the
    /// system folder alone, the temporary, application-data and profile variables all pointing at the request's staging
    /// folder, the browser's log file in that folder, the time zone UTC and the production flag; so nothing inherited can redirect what runs, and every file the
    /// renderer, the browser or the compositor writes lands in staging. The process is put into a new job set to end every
    /// member when it closes BEFORE its start line is written: the render script reads that line before it loads anything, so
    /// every process it or the browser starts is in the job. Past the bound, on cancellation and after exit, the job is closed;
    /// the processes still in it after exit are counted first and returned.
    /// </summary>
    public async Task<GraphicInvocation> InvokeAsync(GraphicInvocationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var graphics = _settings.Graphics ?? throw new ProductionSettingRefusedException("graphics", "no graphics section is configured");
        Require("graphics.nodeExecutable", graphics.NodeExecutable);
        Require("graphics.browserExecutable", graphics.BrowserExecutable);
        Require("graphics.regularFace", graphics.RegularFace);
        Require("graphics.italicFace", graphics.ItalicFace);
        if (request.ScriptFile.IndexOfAny(['/', '\\', ':']) >= 0 || request.RequestFile.IndexOfAny(['/', '\\', ':']) >= 0)
        {
            throw new ArgumentException("The render script and the request are plain names in the staging folder.", nameof(request));
        }

        Require("graphics.renderScript (as staged)", Path.Combine(request.StagingFolder, request.ScriptFile));
        Require("the render request (as staged)", Path.Combine(request.StagingFolder, request.RequestFile));

        var mode = request.Mode == GraphicMode.Bundle ? "bundle" : "render";
        var invocation = $"graphic {mode}";
        var start = new ProcessStartInfo(graphics.NodeExecutable)
        {
            WorkingDirectory = request.StagingFolder,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add(request.ScriptFile);
        start.ArgumentList.Add(mode);
        start.ArgumentList.Add(request.RequestFile);

        start.Environment.Clear();
        foreach (var kept in new[] { "SystemRoot", "windir" })
        {
            if (Environment.GetEnvironmentVariable(kept) is { Length: > 0 } value)
            {
                start.Environment[kept] = value;
            }
        }

        start.Environment["PATH"] = Environment.SystemDirectory;

        // The same staging folder in its short form where the volume keeps one: the browser builds its profile and caches
        // several folders deep inside it, and past the platform's path limit it fails to create them and logs each failure
        // beside its own executable (measured: a debug.log under the browser folder from a render whose staging path was long).
        var temporary = ShortForm(request.TemporaryFolder);
        foreach (var redirected in new[] { "TEMP", "TMP", "LOCALAPPDATA", "APPDATA", "USERPROFILE", "HOME" })
        {
            start.Environment[redirected] = temporary;
        }

        start.Environment["TZ"] = "UTC";
        start.Environment["NODE_ENV"] = "production";

        // The browser writes its own log beside its executable unless told otherwise (measured: a debug.log under the installed
        // browser folder after a direct render); its log file is named here, in staging, so nothing lands under an installed folder.
        start.Environment["CHROME_LOG_FILE"] = Path.Combine(temporary, "browser.log");

        var stopwatch = Stopwatch.StartNew();
        using var job = JobObject.Create();
        using var process = Process.Start(start)
            ?? throw new MediaToolFailedException(invocation, -1, "the process could not be started");
        try
        {
            job.Assign(process);
        }
        catch
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var errors = process.StandardError.ReadToEndAsync(CancellationToken.None);
        using var bounded = new CancellationTokenSource(request.Bound);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(bounded.Token, cancellationToken);
        try
        {
            // Only now, inside the job, may the render script load anything.
            await process.StandardInput.WriteLineAsync("start".AsMemory(), linked.Token).ConfigureAwait(false);
            await process.StandardInput.FlushAsync(linked.Token).ConfigureAwait(false);
            process.StandardInput.Close();
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (Exception ended) when (ended is OperationCanceledException or IOException)
        {
            // Closing the job ends Node, the browser it started, and anything either started, whether its parent exited or not.
            job.Close();
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            if (bounded.IsCancellationRequested)
            {
                throw new MediaToolBoundExceededException(invocation, request.Bound);
            }

            if (ended is IOException && !cancellationToken.IsCancellationRequested)
            {
                throw new MediaToolFailedException(invocation, process.ExitCode, "the process ended before it read its start line");
            }

            throw;
        }

        // A console process's host can outlive it by a moment; the job is read until it is empty or two seconds have passed,
        // and whatever is still in it then is counted, recorded and ended by closing the job.
        var leftover = job.ActiveProcesses();
        var settle = Stopwatch.StartNew();
        while (leftover > 0 && settle.Elapsed < TimeSpan.FromSeconds(2))
        {
            await Task.Delay(50, CancellationToken.None).ConfigureAwait(false);
            leftover = job.ActiveProcesses();
        }

        job.Close();
        stopwatch.Stop();
        var said = await errors.ConfigureAwait(false);
        _ = await output.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            var first = said.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            var why = process.ExitCode switch
            {
                3 => "refused before the renderer was called",
                4 => "the renderer failed",
                _ => "an exit the render script does not document",
            };
            throw new MediaToolFailedException(invocation, process.ExitCode, $"{why}: {(first is { Length: > 300 } ? first[..300] : first ?? "no message")}");
        }

        return new GraphicInvocation([graphics.NodeExecutable, request.ScriptFile, mode, request.RequestFile], leftover, stopwatch.Elapsed);
    }

    /// <summary>
    /// The owner's contact sheet (decision D-007): each tile, a byte copy of a graphic's final frame, scaled into a grid of the
    /// given columns with its label file drawn beneath it in the staged face, on one thread, bit-exact, under the still bound.
    /// </summary>
    public Task ComposeSheetAsync(SheetSpecification sheet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var c = CultureInfo.InvariantCulture;
        var arguments = new List<string> { "-hide_banner", "-nostdin", "-loglevel", "error", "-y" };
        foreach (var tile in sheet.TileFiles)
        {
            arguments.Add("-i");
            arguments.Add(tile);
        }

        var graph = new StringBuilder();
        var layout = new List<string>();
        var cell = sheet.TileHeight + sheet.LabelHeight;
        for (var i = 0; i < sheet.TileFiles.Count; i++)
        {
            graph.Append(c, $"[{i}:v]scale={sheet.TileWidth}:{sheet.TileHeight},pad={sheet.TileWidth}:{cell}:0:0:color=white,");
            graph.Append(c, $"drawtext=fontfile={sheet.FaceFile}:textfile={sheet.LabelFiles[i]}:expansion=none:fontcolor=black:fontsize=15:line_spacing=4:x=8:y={sheet.TileHeight + 8}[t{i}];");
            layout.Add(string.Create(c, $"{(i % sheet.Columns) * sheet.TileWidth}_{(i / sheet.Columns) * cell}"));
        }

        for (var i = 0; i < sheet.TileFiles.Count; i++)
        {
            graph.Append(c, $"[t{i}]");
        }

        graph.Append(c, $"xstack=inputs={sheet.TileFiles.Count}:layout={string.Join("|", layout)}:fill=white,format=rgb24[sheet]");
        arguments.AddRange(["-filter_complex", graph.ToString(), "-map", "[sheet]", "-frames:v", "1", "-threads", "1",
            "-fflags", "+bitexact", "-flags", "+bitexact", "-map_metadata", "-1", sheet.OutputFile]);
        return RunCheckedAsync("contact sheet", _settings.RendererPath, arguments, sheet.StagingFolder, _settings.StillBound, cancellationToken);
    }

    /// <summary>The owner's video-only preview (decision D-007): the delivered encoder settings over a segment list, no audio, under the render bound.</summary>
    public Task RenderPreviewAsync(PreviewSpecification preview, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var fps = _settings.FramesPerSecond.ToString(CultureInfo.InvariantCulture);
        return RunCheckedAsync(
            "preview",
            _settings.RendererPath,
            [
                "-hide_banner", "-nostdin", "-loglevel", "error", "-nostats", "-y",
                "-f", "concat", "-safe", "0", "-i", preview.SegmentListFile,
                "-map", "0:v:0",
                "-vf", string.Create(CultureInfo.InvariantCulture, $"fps={fps},scale={_settings.Width}:{_settings.Height},format=yuv420p"),
                "-r", fps,
                "-frames:v", preview.TotalFrames.ToString(CultureInfo.InvariantCulture),
                "-c:v", "libx264", "-preset", "veryfast", "-tune", "stillimage",
                "-an",
                "-movflags", "+faststart",
                preview.OutputFile,
            ],
            preview.StagingFolder,
            _settings.RenderBound,
            cancellationToken);
    }

    /// <summary>An existing folder's short form, or the folder as given where the volume keeps no short names.</summary>
    private static string ShortForm(string folder)
    {
        var buffer = new StringBuilder(1024);
        var length = GetShortPathNameW(folder, buffer, (uint)buffer.Capacity);
        return length == 0 || length > buffer.Capacity ? folder : buffer.ToString();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetShortPathNameW(string longPath, StringBuilder shortPath, uint bufferLength);

    /// <summary>
    /// A job object set to end every member when it is closed: a browser whose parent exited is still in the job, so closing it
    /// ends the whole set, which a walk of the process tree cannot do.
    /// </summary>
    private sealed partial class JobObject : IDisposable
    {
        private const int ExtendedLimitInformation = 9;
        private const int BasicAccountingInformation = 1;
        private const uint KillOnJobClose = 0x2000;
        private IntPtr _handle;

        private JobObject(IntPtr handle) => _handle = handle;

        public static JobObject Create()
        {
            var handle = CreateJobObjectW(IntPtr.Zero, null);
            if (handle == IntPtr.Zero)
            {
                throw new MediaToolFailedException("graphic job", Marshal.GetLastPInvokeError(), "a job object could not be created");
            }

            var limits = new ExtendedLimits { Basic = new BasicLimits { LimitFlags = KillOnJobClose } };
            if (!SetInformationJobObject(handle, ExtendedLimitInformation, ref limits, (uint)Marshal.SizeOf<ExtendedLimits>()))
            {
                var error = Marshal.GetLastPInvokeError();
                CloseHandle(handle);
                throw new MediaToolFailedException("graphic job", error, "the job could not be set to end its members on close");
            }

            return new JobObject(handle);
        }

        public void Assign(Process process)
        {
            if (!AssignProcessToJobObject(_handle, process.Handle))
            {
                throw new MediaToolFailedException("graphic job", Marshal.GetLastPInvokeError(), "the process could not be put into its job");
            }
        }

        /// <summary>The processes still in the job, read before it is closed; -1 where the job cannot be read.</summary>
        public int ActiveProcesses() =>
            _handle != IntPtr.Zero && QueryInformationJobObject(_handle, BasicAccountingInformation, out var accounting, (uint)Marshal.SizeOf<Accounting>(), IntPtr.Zero)
                ? (int)accounting.ActiveProcesses
                : -1;

        public void Close()
        {
            if (_handle != IntPtr.Zero)
            {
                CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }

        public void Dispose() => Close();

        [StructLayout(LayoutKind.Sequential)]
        private struct BasicLimits
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ExtendedLimits
        {
            public BasicLimits Basic;
            public IoCounters Io;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Accounting
        {
            public long TotalUserTime;
            public long TotalKernelTime;
            public long ThisPeriodTotalUserTime;
            public long ThisPeriodTotalKernelTime;
            public uint TotalPageFaultCount;
            public uint TotalProcesses;
            public uint ActiveProcesses;
            public uint TotalTerminatedProcesses;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObjectW(IntPtr attributes, string? name);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetInformationJobObject(IntPtr job, int informationClass, ref ExtendedLimits information, uint length);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryInformationJobObject(IntPtr job, int informationClass, out Accounting information, uint length, IntPtr returned);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
