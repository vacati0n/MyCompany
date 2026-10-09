using MediaCompany.Production;
using Xunit;

namespace MediaCompany.Production.Tests;

/// <summary>
/// Where the installed media tool and the fixed font are, for the tests only: found on the process's search path and
/// in the platform's font folder, never written in production source. A test that needs them is not-run, and says
/// why, where they are absent.
/// </summary>
internal static class MediaTool
{
    internal static string? Renderer { get; } = OnPath(OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");

    internal static string? Probe { get; } = OnPath(OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe");

    internal static string? Font { get; } = FindFont();

    internal static bool Present => Renderer is not null && Probe is not null && Font is not null;

    /// <summary>The settings a test runs the tool with: the bounds and profile ruled at the Design Gate, unless a test narrows one.</summary>
    internal static ProductionSettings Settings(string outputRoot, string repositoryRoot) => new()
    {
        OutputRoot = outputRoot,
        RepositoryRoot = repositoryRoot,
        RendererPath = Renderer ?? string.Empty,
        ProbePath = Probe ?? string.Empty,
        FontFile = Font ?? string.Empty,
        Width = 1920,
        Height = 1080,
        FramesPerSecond = 30,
        RenderBound = TimeSpan.FromMinutes(60),
        DecodeBound = TimeSpan.FromMinutes(60),
        ProbeBound = TimeSpan.FromSeconds(60),
        StillBound = TimeSpan.FromSeconds(60),
        ProviderCallBound = TimeSpan.FromSeconds(60),
        NarrationCharacterMaximum = 1_500,
        NarrationVoice = "a-demonstration-voice-the-fakes-ignore",
    };

    internal static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MediaCompany.slnx")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }

    private static string? OnPath(string name) =>
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(d => Path.Combine(d.Trim(), name))
            .FirstOrDefault(File.Exists);

    private static string? FindFont()
    {
        var fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        var arial = string.IsNullOrEmpty(fonts) ? null : Path.Combine(fonts, "arial.ttf");
        return arial is not null && File.Exists(arial) ? arial : null;
    }
}

/// <summary>A fact that needs the installed media tool; not-run, saying so, where it is absent.</summary>
public sealed class RequiresMediaToolFactAttribute : FactAttribute
{
    public RequiresMediaToolFactAttribute()
    {
        if (!MediaTool.Present)
        {
            Skip = "not-run: the media tool or the fixed font is not installed on this machine.";
        }
    }
}
