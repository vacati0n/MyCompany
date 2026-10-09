using System.Security.Cryptography;
using MediaCompany.Production;
using Xunit;

namespace MediaCompany.Production.Tests;

/// <summary>
/// The one writer (the production change, decision D-014 of its design): an output root inside the repository is
/// refused before anything is written; every hash returned is of the stored file, re-read; staging an interrupted run
/// left is cleared at the next run's start.
/// </summary>
public sealed class ArtifactWriterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mediacompany-writer-" + Guid.NewGuid().ToString("N"));
    private readonly string _repository = Path.Combine(Path.GetTempPath(), "mediacompany-repository-" + Guid.NewGuid().ToString("N"));

    public ArtifactWriterTests()
    {
        Directory.CreateDirectory(_repository);
    }

    public void Dispose()
    {
        foreach (var folder in new[] { _root, _repository })
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    /// <summary>An output root inside, equal to or containing the repository is refused by name, before anything is written.</summary>
    [Theory]
    [InlineData("inside")]
    [InlineData("equal")]
    [InlineData("containing")]
    [InlineData("inside, cased otherwise")]
    public void AnOutputRootInsideOrAroundTheRepositoryIsRefused(string where)
    {
        var root = where switch
        {
            "inside" => Path.Combine(_repository, "output"),
            "equal" => _repository,
            "containing" => Path.GetDirectoryName(_repository)!,
            _ => Path.Combine(_repository.ToUpperInvariant(), "output"),
        };

        if (where == "inside, cased otherwise" && !OperatingSystem.IsWindows())
        {
            return;
        }

        var refused = Assert.Throws<ProductionSettingRefusedException>(() => new ArtifactWriter(root, _repository));
        Assert.Equal("outputRoot", refused.Setting);
        Assert.False(Directory.Exists(Path.Combine(_repository, "output")));
    }

    /// <summary>The real repository refuses an output root inside its tracked tree.</summary>
    [Fact]
    public void TheRealRepositoryRefusesAnOutputRootInsideIt()
    {
        var repository = MediaTool.RepositoryRoot();
        Assert.Throws<ProductionSettingRefusedException>(() => new ArtifactWriter(Path.Combine(repository, "wave-2", "output"), repository));
    }

    /// <summary>The hash and length returned are those recomputed from the STORED file, and the file is where the record says.</summary>
    [Fact]
    public async Task EveryReturnedHashEqualsTheHashOfTheStoredFile()
    {
        var writer = new ArtifactWriter(_root, _repository);
        var staging = writer.BeginRun("item/v2");
        var bytes = RandomNumberGenerator.GetBytes(100_000);

        var stored = await writer.StoreAsync(staging, bytes, "item/v2/audio/part-001.wav", CancellationToken.None);

        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(stored.FullPath))), stored.Sha256);
        Assert.Equal(bytes.Length, stored.Length);
        Assert.Equal(writer.FullPath("item/v2/audio/part-001.wav"), stored.FullPath);
        Assert.Empty(Directory.GetFiles(staging));
    }

    /// <summary>A promoted file is never overwritten, so a recorded hash always names the bytes that are there.</summary>
    [Fact]
    public async Task APromotedFileIsNeverOverwritten()
    {
        var writer = new ArtifactWriter(_root, _repository);
        var staging = writer.BeginRun("item/v2");
        await writer.StoreAsync(staging, new byte[] { 1 }, "item/v2/x.bin", CancellationToken.None);

        await Assert.ThrowsAsync<IOException>(() => writer.StoreAsync(staging, new byte[] { 2 }, "item/v2/x.bin", CancellationToken.None));
        Assert.Equal(new byte[] { 1 }, await File.ReadAllBytesAsync(writer.FullPath("item/v2/x.bin")));
    }

    /// <summary>Staging an interrupted run left is cleared at the next run's start; no promoted file is touched.</summary>
    [Fact]
    public async Task StagingLeftByAnInterruptedRunIsClearedAtTheNextStart()
    {
        var writer = new ArtifactWriter(_root, _repository);
        var staging = writer.BeginRun("item/v2");
        await writer.WriteStagingTextAsync(staging, "left-behind.txt", "an interrupted run's input", CancellationToken.None);
        await writer.StoreAsync(staging, new byte[] { 7 }, "item/v2/kept.bin", CancellationToken.None);

        var again = writer.BeginRun("item/v2");

        Assert.Empty(Directory.GetFiles(again));
        Assert.True(File.Exists(writer.FullPath("item/v2/kept.bin")));
    }

    /// <summary>A path that would leave the output root is refused.</summary>
    [Fact]
    public void APathLeavingTheOutputRootIsRefused()
    {
        var writer = new ArtifactWriter(_root, _repository);
        Assert.Throws<ArgumentException>(() => writer.FullPath("../escape.bin"));
        Assert.Throws<ArgumentException>(() => writer.FullPath(Path.GetFullPath("/escape.bin")));
    }
}
