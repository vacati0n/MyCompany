using System.Globalization;

namespace MediaCompany.Pilot.Web;

/// <summary>
/// The start-up switches. Everyday use needs none: the desktop shortcut starts the app with no argument. --port picks the port (0
/// lets the system choose); --app-folder replaces the local application-data folder; --desktop-folder replaces the desktop for the
/// shortcut; --no-browser does not open the default browser; --test-mode, with --test-result, serves the recording-test driver and its
/// result route, and is set only by the test program on its own command line.
/// </summary>
public sealed record WebOptions
{
    public int? Port { get; init; }
    public string AppFolder { get; init; } = AppRecordStore.DefaultFolder;
    public string? DesktopFolder { get; init; }
    public bool OpenBrowser { get; init; } = true;
    public bool TestMode { get; init; }
    public string? TestResultPath { get; init; }

    public const string Usage =
        "Cách dùng: MediaCompany.Pilot.Web [--port <số>] [--app-folder <thư-mục>] [--desktop-folder <thư-mục>] [--no-browser]\n"
        + "Thường không cần tham số nào: bấm biểu tượng trên màn hình nền.\n";

    public static bool TryParse(string[] args, out WebOptions options)
    {
        ArgumentNullException.ThrowIfNull(args);
        options = new WebOptions();
        for (var i = 0; i < args.Length; i++)
        {
            string? Next() => i + 1 < args.Length ? args[++i] : null;
            switch (args[i])
            {
                case "--port" when Next() is { } port && int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number is >= 0 and <= 65535:
                    options = options with { Port = number };
                    break;
                case "--app-folder" when Next() is { Length: > 0 } folder:
                    options = options with { AppFolder = Path.GetFullPath(folder) };
                    break;
                case "--desktop-folder" when Next() is { Length: > 0 } desktop:
                    options = options with { DesktopFolder = Path.GetFullPath(desktop) };
                    break;
                case "--no-browser":
                    options = options with { OpenBrowser = false };
                    break;
                case "--test-mode":
                    options = options with { TestMode = true };
                    break;
                case "--test-result" when Next() is { Length: > 0 } result:
                    options = options with { TestResultPath = Path.GetFullPath(result) };
                    break;
                default:
                    return false;
            }
        }

        return !options.TestMode || options.TestResultPath is not null;
    }
}

/// <summary>What every route shares: the options, the guard, the produce jobs, the page set, the app record and the log.</summary>
public sealed class WebContext
{
    /// <summary>The app's own bound on one toolchain verb other than produce, and on produce (design decision D-009).</summary>
    public static readonly TimeSpan VerbBound = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan ProduceBound = TimeSpan.FromMinutes(40);

    private AppRecord? _record;

    public WebContext(WebOptions options, GuardState guard)
    {
        Options = options;
        Guard = guard;
        _record = AppRecordStore.Read(options.AppFolder);
        Log = new WebLog(() => Record is { } record ? new Workspace(record).WebFolder : Options.AppFolder);
    }

    public WebOptions Options { get; }

    public GuardState Guard { get; }

    public ProduceJobs Jobs { get; } = new();

    public PageAssets Pages { get; } = new();

    public WebLog Log { get; }

    public AppRecord? Record
    {
        get => Volatile.Read(ref _record);
        set => Volatile.Write(ref _record, value);
    }

    public Workspace Workspace() =>
        Record is { } record
            ? new Workspace(record)
            : throw new WebRefusal("setup-needed", "Ứng dụng chưa được cài đặt. Mở màn hình cài đặt, điền thư mục thí điểm và tên CEO, rồi bấm lưu.", 409);

    /// <summary>The settings through the toolchain's own reader, which refuses settings that loosen the CEO's values.</summary>
    public PilotSettings Settings() => PilotSettings.Read(Workspace().Record.SettingsPath);

    public ToolchainRunner Runner()
    {
        var record = Workspace().Record;
        return new ToolchainRunner(record.ToolchainPath, record.ToolchainSha256);
    }
}
