using MediaCompany.Application.Ports;
using MediaCompany.Production;
using Xunit;

namespace MediaCompany.Production.Tests;

/// <summary>
/// The one process starter (the production change, decision D-013 of its design): an absent or missing executable is
/// refused by its setting's name before anything starts; an invocation past its bound terminates its process tree and
/// ends named; stills are bit-exact, so one input yields one byte sequence.
/// </summary>
public sealed class ExternalMediaToolTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mediacompany-tool-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    /// <summary>An absent setting and a setting naming no file are each refused, naming the setting.</summary>
    [Theory]
    [InlineData("rendererPath", "")]
    [InlineData("rendererPath", "no-such-renderer.exe")]
    [InlineData("probePath", "")]
    [InlineData("fontFile", "no-such-font.ttf")]
    public void AnAbsentOrMissingExecutableIsRefusedNamingTheSetting(string setting, string value)
    {
        var settings = MediaTool.Settings(_root, MediaTool.RepositoryRoot()) with
        {
            RendererPath = "renderer-present.exe",
            ProbePath = "probe-present.exe",
            FontFile = "font-present.ttf",
        };
        Directory.CreateDirectory(_root);
        foreach (var file in new[] { "renderer-present.exe", "probe-present.exe", "font-present.ttf" })
        {
            File.WriteAllText(Path.Combine(_root, file), "a stand-in file, never started");
        }

        settings = settings with
        {
            RendererPath = Path.Combine(_root, "renderer-present.exe"),
            ProbePath = Path.Combine(_root, "probe-present.exe"),
            FontFile = Path.Combine(_root, "font-present.ttf"),
        };
        settings = setting switch
        {
            "rendererPath" => settings with { RendererPath = value },
            "probePath" => settings with { ProbePath = value },
            _ => settings with { FontFile = value },
        };

        var refused = Assert.Throws<ProductionSettingRefusedException>(() => new ExternalMediaTool(settings));
        Assert.Equal(setting, refused.Setting);
    }

    /// <summary>Two drawings of one text are byte-identical: bit-exact, one thread, the fixed font.</summary>
    [RequiresMediaToolFact]
    public async Task TwoDrawingsOfOneTextAreByteIdentical()
    {
        var (tool, writer, staging) = Compose(MediaTool.Settings(_root, MediaTool.RepositoryRoot()));

        var first = await DrawAsync(tool, writer, staging, "a", "run/a.png");
        var second = await DrawAsync(tool, writer, staging, "b", "run/b.png");

        Assert.Equal(first.Sha256, second.Sha256);
        Assert.True(first.Length > 0);
    }

    /// <summary>
    /// AN INVOCATION PAST ITS BOUND ends under the named outcome, its process tree terminated, and no output is left
    /// to promote: a render bound of one millisecond is passed before the renderer can finish.
    /// </summary>
    [RequiresMediaToolFact]
    public async Task ARenderPastItsBoundEndsNamedWithNothingLeftToPromote()
    {
        var settings = MediaTool.Settings(_root, MediaTool.RepositoryRoot()) with { StillBound = TimeSpan.FromMilliseconds(1) };
        var (tool, writer, staging) = Compose(settings);
        await writer.CopyIntoStagingAsync(staging, settings.FontFile, ExternalMediaTool.StagedFontName, CancellationToken.None);
        await writer.WriteStagingTextAsync(staging, "slow.txt", "past its bound", CancellationToken.None);

        var exceeded = await Assert.ThrowsAsync<MediaToolBoundExceededException>(() =>
            tool.DrawStillAsync(new StillSpecification(staging, "slow.txt", "slow.png", StillStyle.Graphic), CancellationToken.None));

        Assert.Equal("still", exceeded.Invocation);
        Assert.Equal(TimeSpan.FromMilliseconds(1), exceeded.Bound);
        await Assert.ThrowsAnyAsync<IOException>(() => writer.PromoteAsync(staging, "slow.png", "run/slow.png", CancellationToken.None));
    }

    /// <summary>The probe measures a stored file's streams and duration; the decode counts zero errors over a sound file.</summary>
    [RequiresMediaToolFact]
    public async Task TheProbeAndTheDecodeMeasureAStoredFile()
    {
        var (tool, writer, staging) = Compose(MediaTool.Settings(_root, MediaTool.RepositoryRoot()));
        var still = await DrawAsync(tool, writer, staging, "probe", "run/probe.png");
        var wave = await writer.StoreAsync(staging, Wave(seconds: 2), "run/two-seconds.wav", CancellationToken.None);

        var probe = await tool.ProbeAsync(wave.FullPath, CancellationToken.None);
        var decode = await tool.DecodeAsync(wave.FullPath, CancellationToken.None);

        Assert.Equal(["audio"], probe.StreamKinds);
        Assert.Equal(TimeSpan.FromSeconds(2), probe.Duration);
        Assert.Equal(0, decode.Errors);
        Assert.Equal(0, (await tool.DecodeAsync(still.FullPath, CancellationToken.None)).Errors);
        await Assert.ThrowsAsync<MediaToolFailedException>(() => tool.ProbeAsync(still.FullPath, CancellationToken.None));
        Assert.StartsWith("ffmpeg version", await tool.VersionLineAsync(CancellationToken.None), StringComparison.Ordinal);
    }

    /// <summary>A silent mono 16-bit wave of whole seconds at 16,000 samples a second.</summary>
    private static byte[] Wave(int seconds)
    {
        var data = seconds * 16_000 * 2;
        var wave = new byte[44 + data];
        System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wave, 0);
        BitConverter.GetBytes(36 + data).CopyTo(wave, 4);
        System.Text.Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(wave, 8);
        BitConverter.GetBytes(16).CopyTo(wave, 16);
        BitConverter.GetBytes((short)1).CopyTo(wave, 20);
        BitConverter.GetBytes((short)1).CopyTo(wave, 22);
        BitConverter.GetBytes(16_000).CopyTo(wave, 24);
        BitConverter.GetBytes(32_000).CopyTo(wave, 28);
        BitConverter.GetBytes((short)2).CopyTo(wave, 32);
        BitConverter.GetBytes((short)16).CopyTo(wave, 34);
        System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wave, 36);
        BitConverter.GetBytes(data).CopyTo(wave, 40);
        return wave;
    }

    private (ExternalMediaTool Tool, ArtifactWriter Writer, string Staging) Compose(ProductionSettings settings)
    {
        var writer = new ArtifactWriter(_root, settings.RepositoryRoot);
        return (new ExternalMediaTool(settings), writer, writer.BeginRun("run"));
    }

    private static async Task<StoredFile> DrawAsync(ExternalMediaTool tool, ArtifactWriter writer, string staging, string name, string relative)
    {
        await writer.CopyIntoStagingAsync(staging, MediaTool.Font!, ExternalMediaTool.StagedFontName, CancellationToken.None);
        await writer.WriteStagingTextAsync(staging, name + ".txt", "GFX-00\n\nthe same text, θ and 13–15 Hz", CancellationToken.None);
        await tool.DrawStillAsync(new StillSpecification(staging, name + ".txt", name + ".png", StillStyle.Graphic), CancellationToken.None);
        return await writer.PromoteAsync(staging, name + ".png", relative, CancellationToken.None);
    }
}
