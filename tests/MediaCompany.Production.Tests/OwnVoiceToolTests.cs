using System.Diagnostics;
using System.Text;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Production;
using MediaCompany.OwnVoice.Fixtures;
using MediaCompany.Production;
using Xunit;

namespace MediaCompany.Production.Tests;

/// <summary>
/// The own-voice change at the process and file boundaries, with no datastore (decisions D-005, D-006 and D-007 of its
/// design): measurement from DECODED audio against fixtures whose header disagrees and tones of known level; the exact join;
/// the in-house model run through the one starter as an in-test stand-in process, its argument list, its bound and its
/// failure; and the installation verifier over a fixture installation, its findings, its held handles and the platform's
/// refusal to replace a held file while the stand-in still loads it. No test runs the real model.
/// </summary>
public sealed class OwnVoiceToolTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mediacompany-own-voice-" + Guid.NewGuid().ToString("N"));

    public OwnVoiceToolTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    // -----------------------------------------------------------------------
    // Measurement from decoded audio
    // -----------------------------------------------------------------------

    /// <summary>
    /// A FIXTURE WHOSE HEADER STATES ANOTHER DURATION (a variable-rate file with no frame index, whose first frames are
    /// silence, so the container's estimate from its first frame's rate is far off): the measurement reports the DECODED
    /// duration, samples over rate, and never the probe's container figure.
    /// </summary>
    [RequiresMediaToolFact]
    public async Task ADurationIsMeasuredFromDecodedSamplesNotTheContainersHeader()
    {
        var file = Path.Combine(_root, "header-disagrees.mp3");
        AudioFixtures.Render(MediaTool.Renderer!,
            "-f", "lavfi", "-i", "anullsrc=r=44100:cl=mono", "-f", "lavfi", "-i", "anoisesrc=d=3:r=44100",
            "-filter_complex", "[0]atrim=duration=2[s];[s][1]concat=n=2:v=0:a=1", "-c:a", "libmp3lame", "-q:a", "9", "-write_xing", "0", file);
        var tool = Tool();

        var header = await tool.ProbeAsync(file, CancellationToken.None);
        var measured = await tool.MeasureAudioAsync(file, CancellationToken.None);

        Assert.Equal(44_100, measured.SampleRate);
        Assert.Equal((decimal)measured.DecodedSamples / 44_100, measured.DurationSeconds);
        Assert.InRange(measured.DurationSeconds, 4.9m, 5.2m);
        Assert.True(Math.Abs((decimal)header.Duration.TotalSeconds - measured.DurationSeconds) > 0.5m,
            $"the fixture's header states {header.Duration.TotalSeconds} s and its decoded audio {measured.DurationSeconds} s; the fixture must disagree");
        Assert.Equal("mp3", measured.Codec);
        Assert.Empty(measured.DecodeFindings);
    }

    /// <summary>
    /// TONES OF KNOWN LEVEL: a 1 kHz sine written by the test at a peak of -20 and -30 dBFS (mono, 48 kHz) measures -23.0 and
    /// -33.0 LUFS integrated (BS.1770: a sine's mean square is its peak minus 3.01 dB, and the weighting is unity at 1 kHz),
    /// with the measure and the unit named. Silence measures NOT MEASURABLE with the meter's reason, never zero.
    /// </summary>
    [RequiresMediaToolFact]
    public async Task TonesOfKnownLevelMeasureTheirLoudnessAndSilenceIsNotMeasurableNeverZero()
    {
        var tool = Tool();
        foreach (var (peak, expected) in new[] { (-20.0, -23.0m), (-30.0, -33.0m) })
        {
            var file = Path.Combine(_root, $"tone{peak}.wav");
            await File.WriteAllBytesAsync(file, AudioFixtures.Wave(48_000, 1, 16, 10, 1_000, Math.Pow(10, peak / 20)));
            var measured = await tool.MeasureAudioAsync(file, CancellationToken.None);
            Assert.NotNull(measured.IntegratedLoudness);
            Assert.InRange(measured.IntegratedLoudness!.Value, expected - 0.15m, expected + 0.15m);
            Assert.Null(measured.LoudnessNotMeasurable);
            Assert.Contains("LUFS MEASURED", measured.DescribeLoudness(), StringComparison.Ordinal);
            Assert.Contains("EBU R128", measured.DescribeLoudness(), StringComparison.Ordinal);
            Assert.Equal(480_000L, measured.DecodedSamples);
        }

        var silence = Path.Combine(_root, "silence.wav");
        await File.WriteAllBytesAsync(silence, AudioFixtures.Wave(22_050, 1, 16, 2, 440, 0));
        var quiet = await tool.MeasureAudioAsync(silence, CancellationToken.None);
        Assert.Null(quiet.IntegratedLoudness);
        Assert.Contains("absolute gate", quiet.LoudnessNotMeasurable, StringComparison.Ordinal);
        Assert.Contains("NOT MEASURABLE", quiet.DescribeLoudness(), StringComparison.Ordinal);
        Assert.DoesNotContain("0.0 LUFS MEASURED", quiet.DescribeLoudness(), StringComparison.Ordinal);
    }

    /// <summary>A measurement that passes its bound (the decode bound, which it borrows) ends named, its process tree terminated.</summary>
    [RequiresMediaToolFact]
    public async Task AMeasurementPastItsBoundEndsNamed()
    {
        var file = Path.Combine(_root, "long.wav");
        await File.WriteAllBytesAsync(file, AudioFixtures.Wave(48_000, 2, 16, 600, 440, 0.1));
        var tool = new ExternalMediaTool(MediaTool.Settings(_root, MediaTool.RepositoryRoot()) with { DecodeBound = TimeSpan.FromMilliseconds(1) });

        var bound = await Assert.ThrowsAsync<MediaToolBoundExceededException>(() => tool.MeasureAudioAsync(file, CancellationToken.None));
        Assert.Equal("audio measurement", bound.Invocation);
    }

    /// <summary>
    /// THE JOIN IS EXACT: 24-bit parts join into a 24-in-32-bit file, never requantised to the delivered 16 bits, and the
    /// joined narration's decoded samples equal the sum of its parts'.
    /// </summary>
    [RequiresMediaToolFact]
    public async Task TheJoinKeepsTheCommonSampleFormatAndEveryDecodedSample()
    {
        var tool = Tool();
        var writer = new ArtifactWriter(_root, MediaTool.RepositoryRoot());
        var staging = writer.BeginRun("join");
        var parts = new List<AudioMeasurement>();
        for (var i = 1; i <= 3; i++)
        {
            await File.WriteAllBytesAsync(Path.Combine(_root, "join", $"p{i}.wav"), AudioFixtures.Wave(22_050, 1, 24, 0.5 * i, 300 + (i * 50), 0.3));
            parts.Add(await tool.MeasureAudioAsync(Path.Combine(_root, "join", $"p{i}.wav"), CancellationToken.None));
        }

        Assert.All(parts, p => Assert.Equal("s32", p.SampleFormat));
        await writer.WriteStagingTextAsync(staging, "list.txt", "file '../p1.wav'\nfile '../p2.wav'\nfile '../p3.wav'\n", CancellationToken.None);
        await tool.ConcatenateAudioAsync(staging, "list.txt", "joined.wav", parts[0].SampleFormat, CancellationToken.None);
        var joined = await tool.MeasureAudioAsync(Path.Combine(staging, "joined.wav"), CancellationToken.None);

        Assert.Equal(parts.Sum(p => p.DecodedSamples), joined.DecodedSamples);
        Assert.Equal("s32", joined.SampleFormat);
        Assert.Equal("pcm_s32le", joined.Codec);
        Assert.Throws<MediaToolFailedException>(() => ExternalMediaTool.PcmFor("not-a-format"));

        // 16-bit parts join BIT-EXACT: the joined samples are the parts' samples, in order, with no gain change, stretch,
        // pad or trim — the narration source is never adjusted.
        var pcm = new List<byte>();
        for (var i = 1; i <= 3; i++)
        {
            var wave = AudioFixtures.Wave(22_050, 1, 16, 0.4 * i, 250 + (i * 70), 0.25);
            await File.WriteAllBytesAsync(Path.Combine(_root, "join", $"s{i}.wav"), wave);
            pcm.AddRange(wave[44..]);
        }

        await writer.WriteStagingTextAsync(staging, "list16.txt", "file '../s1.wav'\nfile '../s2.wav'\nfile '../s3.wav'\n", CancellationToken.None);
        await tool.ConcatenateAudioAsync(staging, "list16.txt", "joined16.wav", "s16", CancellationToken.None);
        var joinedBytes = await File.ReadAllBytesAsync(Path.Combine(staging, "joined16.wav"));
        var data = joinedBytes.AsSpan().IndexOf("data"u8.ToArray());
        Assert.True(data > 0);
        Assert.Equal(pcm.ToArray(), joinedBytes[(data + 8)..]);
    }

    // -----------------------------------------------------------------------
    // The in-house model through the one starter: an in-test stand-in process
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE MODEL RUN THROUGH THE ONE STARTER: the stand-in receives exactly the runtime's argument list — isolated interpreter,
    /// bytecode off, the module, the model's and the configuration's FULL paths, the text and output as plain names in the
    /// staging folder, all six settings — and nothing naming a voice, a data folder, a speaker or a device; it writes one WAV
    /// whose decoded length follows the text, while the installation's files are HELD read-only by the verifier.
    /// </summary>
    [RequiresMediaToolFact]
    public async Task TheModelRunsThroughTheOneStarterWithItsExactArgumentListWhileItsFilesAreHeld()
    {
        using var installation = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(60));
        var settings = MediaTool.Settings(_root, MediaTool.RepositoryRoot()) with { InHouseModel = installation.Settings() };
        var tool = new ExternalMediaTool(settings);
        var writer = new ArtifactWriter(_root, MediaTool.RepositoryRoot());
        var staging = writer.BeginRun("model");
        await writer.WriteStagingTextAsync(staging, "part-001.txt", "one two three four five six seven eight nine ten", CancellationToken.None);

        IReadOnlyList<string> arguments;
        await using (var held = await new InstallationVerifier(settings.InHouseModel!, settings.RepositoryRoot).HoldAsync(CancellationToken.None))
        {
            arguments = await tool.GenerateAsync(Generation(staging, settings.InHouseModel!, "part-001.txt", "part-001.wav"), CancellationToken.None);
            Assert.Equal(8, held.Files.Count);
        }

        Assert.Equal(
            [
                installation.Interpreter, "-I", "-B", "-m", "runtime_fixture", "-m", installation.Model, "-c", installation.Configuration,
                "-i", "part-001.txt", "-f", "part-001.wav", "--length-scale", "1", "--noise-scale", "0.667", "--noise-w-scale", "0.8",
                "--sentence-silence", "0", "--volume", "1",
            ],
            arguments);
        Assert.DoesNotContain(arguments, a => a is "--data-dir" or "-s" or "--speaker" or "--cuda" or "--output-raw" or "-d");
        var measured = await tool.MeasureAudioAsync(Path.Combine(staging, "part-001.wav"), CancellationToken.None);
        Assert.Equal(22_050, measured.SampleRate);
        Assert.Equal((long)Math.Round(10 * 0.3 * 22_050), measured.DecodedSamples);
    }

    /// <summary>
    /// A STAND-IN THAT OUTLASTS ITS BOUND is terminated, process tree and all, under the configured model part bound on the
    /// monotonic clock, and ends named; a FAILING stand-in ends named with its first error line truncated to 200 characters.
    /// </summary>
    [Fact]
    public async Task AStandInPastTheModelPartBoundIsTerminatedAndAFailingOneEndsNamed()
    {
        foreach (var (behaviour, bound) in new[] { ("slow", TimeSpan.FromSeconds(3)), ("fail", TimeSpan.FromSeconds(60)) })
        {
            using var installation = new FixtureInstallation(behaviour, bound);
            var settings = Settings(installation);
            var tool = new ExternalMediaTool(settings);
            var writer = new ArtifactWriter(_root, MediaTool.RepositoryRoot());
            var staging = writer.BeginRun("bound-" + behaviour);
            await writer.WriteStagingTextAsync(staging, "part-001.txt", "a part", CancellationToken.None);
            var watch = Stopwatch.StartNew();

            var ended = await Assert.ThrowsAnyAsync<Exception>(() => tool.GenerateAsync(Generation(staging, settings.InHouseModel!, "part-001.txt", "part-001.wav"), CancellationToken.None));

            if (behaviour == "slow")
            {
                var exceeded = Assert.IsType<MediaToolBoundExceededException>(ended);
                Assert.Equal("in-house model", exceeded.Invocation);
                Assert.InRange(watch.Elapsed, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30));
                Assert.DoesNotContain(Process.GetProcessesByName("MediaCompany.VoiceStandIn"), p => SafeStart(p) >= DateTime.Now - watch.Elapsed - TimeSpan.FromSeconds(1));
                Assert.False(File.Exists(Path.Combine(staging, "part-001.wav")));
            }
            else
            {
                var failed = Assert.IsType<MediaToolFailedException>(ended);
                Assert.Equal(1, failed.ExitCode);
                Assert.Contains("stand-in failure", failed.Message, StringComparison.Ordinal);
            }
        }
    }

    // -----------------------------------------------------------------------
    // The installation verifier
    // -----------------------------------------------------------------------

    /// <summary>
    /// A MATCHING FIXTURE INSTALLATION VERIFIES, naming every file with both hashes, reading the runtime's name, version and
    /// licence from its package metadata, the voice from its configuration and the dataset's licence from its model card;
    /// every hashed record entry is verified. Verification READS FILES ONLY: the configured "interpreter" here is a text file
    /// that cannot be started, and it verifies.
    /// </summary>
    [Fact]
    public async Task AMatchingInstallationVerifiesNamingEveryFileAndStartsNoProcess()
    {
        using var installation = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(60));
        await File.WriteAllTextAsync(installation.Interpreter, "not an executable: verification reads it and never starts it");
        var report = await new InstallationVerifier(installation.Settings(), MediaTool.RepositoryRoot()).VerifyAsync(entries: true, CancellationToken.None);

        Assert.True(report.Verified, string.Join("; ", report.Findings));
        Assert.Equal(
            ["interpreter", "environment configuration", "voice model", "voice configuration", "model card", "runtime record", "dependency record 1", "base interpreter"],
            report.Files.Select(f => f.Name));
        Assert.All(report.Files, f => Assert.True(f.Matches));
        Assert.Equal(("runtime-fixture", "1.0", "a-fixture-code-licence"), (report.RuntimeName, report.RuntimeVersion, report.RuntimeLicence));
        Assert.Equal("xx_XX-fixture-test", report.VoiceName);
        Assert.Equal(22_050, report.VoiceSampleRate);
        Assert.Equal(0.667m, report.VoiceDefaults["noise_scale"]);
        Assert.StartsWith("a fixture dataset licence", report.DatasetLicence, StringComparison.Ordinal);
        Assert.Equal(4, report.RecordEntriesVerified);
        Assert.Equal(4, report.RecordEntriesWithoutHash);
    }

    /// <summary>
    /// A MISSING FILE, A ONE-BYTE CHANGE, A CHANGED RECORD ENTRY, A CONFIGURATION THAT IS NOT THE MODEL PATH PLUS ".json" and A
    /// LOCATION INSIDE THE REPOSITORY each refuse, naming the file and, for a change, both hashes.
    /// </summary>
    [Fact]
    public async Task EachDifferingMissingMisplacedOrInsideTheRepositoryFileRefusesNamingIt()
    {
        using var installation = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(60));
        var repository = MediaTool.RepositoryRoot();

        var settings = installation.Settings();
        var before = AudioFixtures.Sha256(installation.Model);
        await File.AppendAllTextAsync(installation.Model, "x");
        var changed = await new InstallationVerifier(settings, repository).VerifyAsync(entries: false, CancellationToken.None);
        var finding = Assert.Single(changed.Findings);
        Assert.Contains(installation.Model, finding, StringComparison.Ordinal);
        Assert.Contains(before, finding, StringComparison.Ordinal);
        Assert.Contains(AudioFixtures.Sha256(installation.Model), finding, StringComparison.Ordinal);

        var missing = await new InstallationVerifier(settings with { ModelCard = settings.ModelCard with { Path = settings.ModelCard.Path + ".gone" } }, repository)
            .VerifyAsync(entries: false, CancellationToken.None);
        Assert.Contains(missing.Findings, f => f.Contains(".gone", StringComparison.Ordinal) && f.Contains("does not exist", StringComparison.Ordinal));

        var elsewhere = Path.Combine(installation.Root, "model", "elsewhere.json");
        File.Copy(installation.Configuration, elsewhere);
        var misplaced = await new InstallationVerifier(settings with { Configuration = new ExpectedFile(elsewhere, AudioFixtures.Sha256(elsewhere)) }, repository)
            .VerifyAsync(entries: false, CancellationToken.None);
        Assert.Contains(misplaced.Findings, f => f.Contains("not the model path plus \".json\"", StringComparison.Ordinal));

        var inside = Path.Combine(repository, "MediaCompany.slnx");
        var repositoryFile = await new InstallationVerifier(settings with { ModelCard = new ExpectedFile(inside, AudioFixtures.Sha256(inside)) }, repository)
            .VerifyAsync(entries: false, CancellationToken.None);
        Assert.Contains(repositoryFile.Findings, f => f.Contains("inside the repository root", StringComparison.Ordinal));

        // A file a record lists changes after the record was written: the entry refuses, naming the file.
        using var second = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(60));
        var module = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(second.RuntimeRecord)!)!, "runtime_fixture", "__init__.py");
        await File.AppendAllTextAsync(module, "# changed after installation\n");
        var entry = await new InstallationVerifier(second.Settings(), repository).VerifyAsync(entries: true, CancellationToken.None);
        Assert.Contains(entry.Findings, f => f.Contains("__init__.py", StringComparison.Ordinal) && f.Contains("changed", StringComparison.Ordinal));
    }

    /// <summary>
    /// ONE READ PER RECORD (the quality review's correction): a runtime record REPLACED BETWEEN ITS HASH AND ITS PARSE by a
    /// forged one — listing a changed module and changed metadata at their new hashes — cannot pass. The entries and the
    /// metadata's expected hash are parsed from the very bytes whose SHA-256 matched, so the changed module and the changed
    /// metadata are each refused, and the forged licence is never read.
    /// </summary>
    [Fact]
    public async Task ARecordReplacedBetweenItsHashAndItsParseIsVerifiedFromTheBytesHashed()
    {
        using var installation = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(60));
        var settings = installation.Settings();
        var packages = Path.GetDirectoryName(Path.GetDirectoryName(installation.RuntimeRecord)!)!;
        var module = Path.Combine(packages, "runtime_fixture", "__init__.py");
        var metadata = Path.Combine(Path.GetDirectoryName(installation.RuntimeRecord)!, "METADATA");
        var original = await File.ReadAllTextAsync(installation.RuntimeRecord);

        static string Listed(string path) =>
            Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var oldModule = Listed(module);
        var oldMetadata = Listed(metadata);
        await File.AppendAllTextAsync(module, "# changed after installation\n");
        await File.WriteAllTextAsync(metadata, (await File.ReadAllTextAsync(metadata)).Replace("a-fixture-code-licence", "a-forged-licence", StringComparison.Ordinal));
        var forged = original.Replace(oldModule, Listed(module), StringComparison.Ordinal).Replace(oldMetadata, Listed(metadata), StringComparison.Ordinal);
        Assert.NotEqual(original, forged);

        var verifier = new InstallationVerifier(settings, MediaTool.RepositoryRoot())
        {
            AfterHash = path =>
            {
                if (string.Equals(path, Path.GetFullPath(installation.RuntimeRecord), StringComparison.OrdinalIgnoreCase))
                {
                    File.WriteAllText(installation.RuntimeRecord, forged);
                }
            },
        };

        var report = await verifier.VerifyAsync(entries: true, CancellationToken.None);

        Assert.Equal(forged, await File.ReadAllTextAsync(installation.RuntimeRecord));
        Assert.True(Assert.Single(report.Files, f => f.Name == "runtime record").Matches);
        Assert.Contains(report.Findings, f => f.Contains("__init__.py", StringComparison.Ordinal) && f.Contains("changed", StringComparison.Ordinal));
        Assert.Contains(report.Findings, f => f.Contains("METADATA", StringComparison.Ordinal));
        Assert.NotEqual("a-forged-licence", report.RuntimeLicence);
        Assert.False(report.Verified);
    }

    /// <summary>
    /// THE HOLD: while the core files are held, the platform refuses replacing, renaming, writing or deleting the model — the
    /// bytes hashed are the bytes the stand-in loads — and the stand-in still opens and reads them. Released, a swapped model
    /// is found by the next hold, naming both hashes.
    /// </summary>
    [Fact]
    public async Task AHeldModelCannotBeSwappedWhileHeldAndASwapAfterwardsIsRefusedByTheNextHold()
    {
        using var installation = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(60));
        var settings = Settings(installation);
        var verifier = new InstallationVerifier(settings.InHouseModel!, settings.RepositoryRoot);
        var impostor = Path.Combine(installation.Root, "impostor.onnx");
        await File.WriteAllTextAsync(impostor, "deterministic\nanother model");
        var writer = new ArtifactWriter(_root, MediaTool.RepositoryRoot());
        var staging = writer.BeginRun("hold");
        await writer.WriteStagingTextAsync(staging, "part-001.txt", "held while loaded", CancellationToken.None);

        await using (var held = await verifier.HoldAsync(CancellationToken.None))
        {
            Assert.Throws<IOException>(() => File.Copy(impostor, installation.Model, overwrite: true));
            Assert.ThrowsAny<IOException>(() => File.Move(installation.Model, installation.Model + ".moved"));
            Assert.ThrowsAny<IOException>(() => File.WriteAllText(installation.Model, "swapped"));
            Assert.ThrowsAny<Exception>(() => File.Delete(installation.Model));

            // The child still loads the held files: the hold denies writing, not reading or starting.
            await new ExternalMediaTool(settings).GenerateAsync(Generation(staging, settings.InHouseModel!, "part-001.txt", "part-001.wav"), CancellationToken.None);
            Assert.True(File.Exists(Path.Combine(staging, "part-001.wav")));
        }

        var expected = settings.InHouseModel!.Model.Sha256;
        File.Copy(impostor, installation.Model, overwrite: true);
        var refused = await Assert.ThrowsAsync<InstallationRefusedException>(() => verifier.HoldAsync(CancellationToken.None));
        var finding = Assert.Single(refused.Findings);
        Assert.Contains(expected, finding, StringComparison.Ordinal);
        Assert.Contains(AudioFixtures.Sha256(installation.Model), finding, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // The package: one part per beat and the script's own rate
    // -----------------------------------------------------------------------

    /// <summary>
    /// Item 001 splits into THIRTEEN parts, one per beat, concatenating to the recorded narration exactly; their whitespace
    /// words sum to 1,929; the script's rate row reads 150 words a minute and its own figure 12 min 51 s, so the whole
    /// expectation is 771.6 s; the loader adds no rate to the material.
    /// </summary>
    [Fact]
    public async Task ItemOneSplitsIntoThirteenBeatPartsAndItsExpectationIsTheScriptsOwn()
    {
        var loaded = await ItemPackageLoader.LoadAsync(MediaTool.RepositoryRoot(), "wave-2/item-001/item-material.json", CancellationToken.None);
        var parts = NarrationSplitter.SplitByBeat(loaded.Narration, loaded.Material.Beats);

        Assert.Equal(13, parts.Count);
        Assert.Equal(Enumerable.Range(1, 13), parts.Select(p => p.Beat));
        Assert.Equal(loaded.Narration, string.Concat(parts.Select(p => p.Text)));
        Assert.Equal(1_929, parts.Sum(p => DurationExpectation.Words(p.Text)));
        Assert.Equal(150, loaded.Rate.WordsPerMinute);
        Assert.Equal("12 min 51 s", loaded.Rate.DurationQuote);
        Assert.Equal(771.6m, DurationExpectation.ExpectedSeconds(1_929, loaded.Rate.WordsPerMinute));
        Assert.DoesNotContain("words per minute", await File.ReadAllTextAsync(loaded.MaterialPath), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private ExternalMediaTool Tool() => new(MediaTool.Settings(_root, MediaTool.RepositoryRoot()));

    /// <summary>Settings for the stand-in: the media tool where it is installed, placeholders that are never started where it is not.</summary>
    private ProductionSettings Settings(FixtureInstallation installation)
    {
        var settings = MediaTool.Settings(_root, MediaTool.RepositoryRoot()) with { InHouseModel = installation.Settings() };
        if (!MediaTool.Present)
        {
            var placeholder = Path.Combine(_root, "never-started.bin");
            File.WriteAllText(placeholder, "never started");
            settings = settings with { RendererPath = placeholder, ProbePath = placeholder, FontFile = placeholder };
        }

        return settings;
    }

    private static VoiceGeneration Generation(string staging, InHouseModelSettings model, string text, string output) =>
        new(staging, text, output, model.Model.Path, model.Configuration.Path, model.Generation, model.PartBound);

    private static DateTime SafeStart(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch (InvalidOperationException)
        {
            return DateTime.MinValue;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return DateTime.MinValue;
        }
    }
}
