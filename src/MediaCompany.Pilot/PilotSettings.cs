using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaCompany.Pilot;

/// <summary>An installed executable and the SHA-256 it must hash to before each run (design D-013).</summary>
public sealed record ToolSetting
{
    public required string Path { get; init; }
    public required string Sha256 { get; init; }
}

/// <summary>The installed face, its font manifest and the manifest line that names it (design D-010).</summary>
public sealed record FaceSetting
{
    public required string Path { get; init; }
    public required string Manifest { get; init; }
    public required string ManifestEntry { get; init; }
}

public sealed record FrameSetting
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int FrameRate { get; init; }
}

/// <summary>
/// The phone-screen safe area of decision record entry 1308: margins of the frame kept clear of the players' right-hand
/// buttons and bottom description. Captions sit at its bottom and the overlay at its top.
/// </summary>
public sealed record SafeAreaSetting
{
    public required int Left { get; init; }
    public required int Right { get; init; }
    public required int Top { get; init; }
    public required int Bottom { get; init; }
}

public sealed record TextBoxSetting
{
    public required int FontSize { get; init; }
    public required int PaddingX { get; init; }
    public required int PaddingY { get; init; }
    public required int LineSpacing { get; init; }
    public required int MaximumLinesPerPage { get; init; }
}

public sealed record RectangleSetting
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }

    public bool Intersects(RectangleSetting other) =>
        X < other.X + other.Width && other.X < X + Width && Y < other.Y + other.Height && other.Y < Y + Height;

    public bool Inside(RectangleSetting outer) =>
        X >= outer.X && Y >= outer.Y && X + Width <= outer.X + outer.Width && Y + Height <= outer.Y + outer.Height;

    public override string ToString() => $"x {X}, y {Y}, rộng {Width}, cao {Height}";
}

public sealed record ImagePanelSetting
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string Motion { get; init; }
    public required int ZoomMaximumPercent { get; init; }

    public RectangleSetting Rectangle => new() { X = X, Y = Y, Width = Width, Height = Height };
}

public sealed record PauseSetting
{
    public required int NoiseFloorDb { get; init; }
    public required double MinimumSilenceSeconds { get; init; }
}

/// <summary>
/// The operator's settings file, held outside the repository. Every member is required and none has a default in code; an unknown
/// member is refused. The CEO's safe-area values are the values of the example settings in the operator guide.
/// </summary>
public sealed record PilotSettings
{
    public const int KnownFormVersion = 1;

    public required int FormVersion { get; init; }
    public required string RepositoryRoot { get; init; }
    public required string PilotFolder { get; init; }
    public required ToolSetting MediaTool { get; init; }
    public required ToolSetting ProbeTool { get; init; }
    public required FaceSetting Face { get; init; }
    public required FrameSetting Frame { get; init; }
    public required SafeAreaSetting SafeArea { get; init; }
    public required TextBoxSetting Caption { get; init; }
    public required TextBoxSetting Overlay { get; init; }
    public required ImagePanelSetting ImagePanel { get; init; }
    public required RectangleSetting PresenterInset { get; init; }
    public required PauseSetting Pause { get; init; }
    public required double ClipToleranceSeconds { get; init; }

    /// <summary>The least time one caption page stays on screen (decision record entry 1309); 1.2 s or more.</summary>
    public required double CaptionPageMinimumSeconds { get; init; }
    public required int ProcessBoundSeconds { get; init; }

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    /// <summary>The safe area as a rectangle of the frame.</summary>
    public RectangleSetting SafeRectangle => new()
    {
        X = SafeArea.Left,
        Y = SafeArea.Top,
        Width = Frame.Width - SafeArea.Left - SafeArea.Right,
        Height = Frame.Height - SafeArea.Top - SafeArea.Bottom,
    };

    public static PilotSettings Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new PilotRefusal("settings-invalid", $"Không thấy tệp cài đặt {path}.");
        }

        PilotSettings? settings;
        try
        {
            settings = JsonSerializer.Deserialize<PilotSettings>(File.ReadAllBytes(path), Json);
        }
        catch (JsonException error)
        {
            throw new PilotRefusal("settings-invalid", $"Tệp cài đặt sai hoặc thiếu trường: {error.Message}");
        }

        if (settings is null || settings.FormVersion != KnownFormVersion)
        {
            throw new PilotRefusal("settings-invalid", $"Tệp cài đặt phải có formVersion {KnownFormVersion}.");
        }

        settings.Validate(HeldData.Current);
        return settings;
    }

    /// <summary>Refuses settings that break a fixed rule: the CEO's held values (entries 1308 and 1309), a motion name, the pilot folder's location.</summary>
    public void Validate(HeldData held)
    {
        ArgumentNullException.ThrowIfNull(held);
        RefuseLooserThanCeo(held);

        if (Caption.MaximumLinesPerPage is < 1 or > 2)
        {
            throw new PilotRefusal("settings-invalid", "Số dòng phụ đề mỗi lần hiện (caption.maximumLinesPerPage) phải là 1 hoặc 2.");
        }

        if (Overlay.MaximumLinesPerPage != 1)
        {
            throw new PilotRefusal("settings-invalid", "Lớp nhãn chỉ có một dòng (overlay.maximumLinesPerPage phải là 1).");
        }

        if (ImagePanel.Motion is not ("zoom" or "pan"))
        {
            throw new PilotRefusal("settings-invalid", "imagePanel.motion phải là \"zoom\" hoặc \"pan\".");
        }

        if (ImagePanel.ZoomMaximumPercent is <= 100 or > 150)
        {
            throw new PilotRefusal("settings-invalid", "imagePanel.zoomMaximumPercent phải lớn hơn 100 và không quá 150.");
        }

        if (Frame.FrameRate is < 1 or > 60 || ProcessBoundSeconds < 1 || double.IsNaN(ClipToleranceSeconds) || ClipToleranceSeconds < 0 || Pause.MinimumSilenceSeconds <= 0)
        {
            throw new PilotRefusal("settings-invalid", "frameRate (1 đến 60), processBoundSeconds, clipToleranceSeconds và pause.minimumSilenceSeconds phải hợp lệ.");
        }

        var numbers = new[] { SafeArea.Left, SafeArea.Right, SafeArea.Top, SafeArea.Bottom, Caption.PaddingX, Caption.PaddingY, Caption.LineSpacing,
            Overlay.PaddingX, Overlay.PaddingY, Overlay.LineSpacing, ImagePanel.X, ImagePanel.Y, PresenterInset.X, PresenterInset.Y };
        if (numbers.Any(n => n < 0) || ImagePanel.Width < 2 || ImagePanel.Height < 2 || PresenterInset.Width < 2 || PresenterInset.Height < 2)
        {
            throw new PilotRefusal("settings-invalid", "Lề, khoảng đệm, vị trí và kích thước trong tệp cài đặt không được âm.");
        }

        var repository = Path.TrimEndingDirectorySeparator(Path.GetFullPath(RepositoryRoot));
        var pilot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(PilotFolder));
        var inside = string.Equals(pilot, repository, StringComparison.OrdinalIgnoreCase)
            || pilot.StartsWith(repository + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        for (var folder = new DirectoryInfo(pilot); folder is not null && !inside; folder = folder.Parent)
        {
            inside = Directory.Exists(Path.Combine(folder.FullName, ".git")) || File.Exists(Path.Combine(folder.FullName, ".git"));
        }

        if (inside)
        {
            throw new PilotRefusal("pilot-folder-in-repository", $"Thư mục thí điểm {pilot} nằm trong kho mã; hãy đặt nó ngoài kho mã.");
        }
    }

    /// <summary>
    /// Refuses settings that loosen decision record entries 1308 and 1309, naming the setting and the CEO's value. The cases: caption
    /// text below the held minimum (56 px); overlay text below the held minimum (40 px); a safe-area margin below the held minimum
    /// (left 60, right 180, top 200, bottom 480); a clip tolerance above the held maximum (0.5 s); a caption page minimum below the held
    /// minimum (1.2 s). Stricter values (larger text, wider margins, a smaller tolerance, a longer page time) are accepted.
    /// </summary>
    public void RefuseLooserThanCeo(HeldData held)
    {
        ArgumentNullException.ThrowIfNull(held);
        static void Refuse(string what) => throw new PilotRefusal("settings-looser-than-ceo", what + " Giá trị này nới lỏng quyết định của CEO; chỉ được đặt chặt hơn.");
        if (Caption.FontSize < held.MinimumCaptionFontSize)
        {
            Refuse($"Cỡ chữ phụ đề (caption.fontSize) {Caption.FontSize} px nhỏ hơn mức tối thiểu {held.MinimumCaptionFontSize} px.");
        }

        if (Overlay.FontSize < held.MinimumOverlayFontSize)
        {
            Refuse($"Cỡ chữ lớp nhãn (overlay.fontSize) {Overlay.FontSize} px nhỏ hơn mức tối thiểu {held.MinimumOverlayFontSize} px.");
        }

        var minimum = held.SafeAreaMinimum;
        foreach (var (name, value, least) in new[]
        {
            ("safeArea.left", SafeArea.Left, minimum.Left),
            ("safeArea.right", SafeArea.Right, minimum.Right),
            ("safeArea.top", SafeArea.Top, minimum.Top),
            ("safeArea.bottom", SafeArea.Bottom, minimum.Bottom),
        })
        {
            if (value < least)
            {
                Refuse($"Lề vùng an toàn {name} {value} px nhỏ hơn mức tối thiểu {least} px.");
            }
        }

        if (!(ClipToleranceSeconds <= held.ClipToleranceMaximumSeconds))
        {
            Refuse(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"Độ lệch cho phép của clip (clipToleranceSeconds) {ClipToleranceSeconds} giây lớn hơn mức tối đa {held.ClipToleranceMaximumSeconds} giây."));
        }

        if (!(CaptionPageMinimumSeconds >= held.CaptionPageMinimumSeconds))
        {
            Refuse(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"Thời gian tối thiểu mỗi trang phụ đề (captionPageMinimumSeconds) {CaptionPageMinimumSeconds} giây nhỏ hơn mức tối thiểu {held.CaptionPageMinimumSeconds} giây."));
        }
    }
}
