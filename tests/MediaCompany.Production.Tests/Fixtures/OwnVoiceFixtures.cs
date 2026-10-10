using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using MediaCompany.Application.Ports;
using MediaCompany.Production;

namespace MediaCompany.OwnVoice.Fixtures;

/// <summary>
/// Audio fixtures written by the test itself (the own-voice change): PCM waves of a known tone, level and length, and
/// files rendered by the installed media tool. Nothing is downloaded and no recording of anyone is used. Shared by the
/// production tests and the datastore demonstrations, which link this file.
/// </summary>
internal static class AudioFixtures
{
    /// <summary>A PCM wave of a sine at the frequency and peak amplitude named (1.0 is full scale), in 16 or 24 bits.</summary>
    public static byte[] Wave(int sampleRate, int channels, int bits, double seconds, double frequency, double amplitude)
    {
        var frames = (int)Math.Round(seconds * sampleRate);
        var bytesPerSample = bits / 8;
        var data = new byte[frames * channels * bytesPerSample];
        var full = bits == 16 ? short.MaxValue : 8_388_607;
        for (var n = 0; n < frames; n++)
        {
            var value = (int)Math.Round(Math.Sin(2 * Math.PI * frequency * n / sampleRate) * amplitude * full);
            for (var c = 0; c < channels; c++)
            {
                var at = ((n * channels) + c) * bytesPerSample;
                if (bits == 16)
                {
                    BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(at), (short)value);
                }
                else
                {
                    data[at] = (byte)(value & 0xFF);
                    data[at + 1] = (byte)((value >> 8) & 0xFF);
                    data[at + 2] = (byte)((value >> 16) & 0xFF);
                }
            }
        }

        var header = new byte[44];
        Encoding.ASCII.GetBytes("RIFF").CopyTo(header, 0);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), 36 + data.Length);
        Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(header, 8);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(20), 1);
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(22), (short)channels);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(24), sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(28), sampleRate * channels * bytesPerSample);
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(32), (short)(channels * bytesPerSample));
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(34), (short)bits);
        Encoding.ASCII.GetBytes("data").CopyTo(header, 36);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(40), data.Length);
        return [.. header, .. data];
    }

    /// <summary>Runs the installed media tool, from the test, to render a fixture; the test fails naming the tool's own words.</summary>
    public static void Render(string renderer, params string[] arguments)
    {
        var start = new ProcessStartInfo(renderer) { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-hide_banner", "-nostdin", "-loglevel", "error", "-y" }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var errors = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"the media tool could not render the fixture: {errors}");
        }
    }

    /// <summary>
    /// One tone recording per beat, rendered by the installed media tool as WAV (16-bit, 22,050 Hz, mono), each a different
    /// frequency and a length the test chooses: the stand-in for the CEO's thirteen recordings.
    /// </summary>
    public static IReadOnlyList<string> RenderBeats(string renderer, string folder, IReadOnlyList<int> beats, Func<int, double> seconds)
    {
        Directory.CreateDirectory(folder);
        var files = new List<string>();
        foreach (var beat in beats)
        {
            var file = Path.Combine(folder, $"{beat:00}.wav");
            Render(renderer, "-f", "lavfi", "-i",
                FormattableString.Invariant($"sine=frequency={200 + (beat * 20)}:sample_rate=22050:duration={seconds(beat)}"),
                "-ac", "1", "-c:a", "pcm_s16le", file);
            files.Add(file);
        }

        return files;
    }

    public static string Sha256(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}

/// <summary>
/// A FIXTURE INSTALLATION of the in-house model, laid out like the real one but built by the test outside the repository:
/// an environment whose "interpreter" is the in-test stand-in executable, an environment configuration naming a fixture base
/// interpreter, a voice model whose first line chooses the stand-in's behaviour, its configuration at the model path plus
/// ".json", a model card, and a runtime and a dependency package each with its metadata and an installed-files record whose
/// entries carry their own SHA-256. Every expected hash in its settings is computed by the test over the fixture files —
/// never the real installation's values.
/// </summary>
internal sealed class FixtureInstallation : IDisposable
{
    public FixtureInstallation(string behaviour, TimeSpan partBound, string? root = null)
    {
        Root = root ?? Path.Combine(Path.GetTempPath(), "mediacompany-voice-fixture-" + Guid.NewGuid().ToString("N"));
        var scripts = Path.Combine(Root, "venv", "Scripts");
        var packages = Path.Combine(Root, "venv", "Lib", "site-packages");
        var basePython = Path.Combine(Root, "base", "interpreter.bin");
        var model = Path.Combine(Root, "model", "fixture-voice.onnx");
        Directory.CreateDirectory(scripts);
        Directory.CreateDirectory(packages);
        Directory.CreateDirectory(Path.GetDirectoryName(basePython)!);
        Directory.CreateDirectory(Path.GetDirectoryName(model)!);

        // The stand-in executable and the files its launcher needs, copied OUT of the repository's build output.
        var built = Path.GetDirectoryName(typeof(FixtureInstallation).Assembly.Location)!;
        foreach (var file in Directory.GetFiles(built, "MediaCompany.VoiceStandIn.*"))
        {
            File.Copy(file, Path.Combine(scripts, Path.GetFileName(file)), overwrite: true);
        }

        Interpreter = Path.Combine(scripts, "MediaCompany.VoiceStandIn.exe");
        File.WriteAllBytes(basePython, Encoding.UTF8.GetBytes("a fixture base interpreter: hashed, never started"));
        EnvironmentConfiguration = Path.Combine(Root, "venv", "fixture-environment.cfg");
        File.WriteAllText(EnvironmentConfiguration, $"home = {Path.GetDirectoryName(basePython)}\ninclude-system-site-packages = false\nexecutable = {basePython}\n");

        File.WriteAllText(model, behaviour + "\n" + new string('w', 4096));
        Model = model;
        Configuration = model + ".json";
        File.WriteAllText(Configuration,
            """{"dataset":"fixture","audio":{"sample_rate":22050,"quality":"test"},"language":{"code":"xx_XX"},"inference":{"noise_scale":0.667,"length_scale":1,"noise_w":0.8}}""");
        ModelCard = Path.Combine(Root, "model", "fixture-card.md");
        File.WriteAllText(ModelCard, "# Model card for a fixture voice\n\n## Dataset\n\n* URL: none\n* License: a fixture dataset licence\n");

        RuntimeRecord = Package(packages, "runtime_fixture", "1.0", "runtime-fixture", "a-fixture-code-licence");
        DependencyRecord = Package(packages, "dependency_fixture", "2.0", "dependency-fixture", "a-fixture-dependency-licence");
        PartBound = partBound;
    }

    public string Root { get; }

    public string Interpreter { get; }

    public string EnvironmentConfiguration { get; }

    public string Model { get; }

    public string Configuration { get; }

    public string ModelCard { get; }

    public string RuntimeRecord { get; }

    public string DependencyRecord { get; }

    public TimeSpan PartBound { get; }

    /// <summary>The model section, every expected hash computed now over the fixture files.</summary>
    public InHouseModelSettings Settings(Func<string, string>? expected = null)
    {
        string Hash(string path) => expected?.Invoke(path) ?? AudioFixtures.Sha256(path);
        var basePython = Path.Combine(Root, "base", "interpreter.bin");
        return new InHouseModelSettings
        {
            Interpreter = new ExpectedFile(Interpreter, Hash(Interpreter)),
            EnvironmentConfiguration = new ExpectedFile(EnvironmentConfiguration, Hash(EnvironmentConfiguration)),
            BaseInterpreterSha256 = Hash(basePython),
            RuntimeModule = "runtime_fixture",
            Model = new ExpectedFile(Model, Hash(Model)),
            Configuration = new ExpectedFile(Configuration, Hash(Configuration)),
            ModelCard = new ExpectedFile(ModelCard, Hash(ModelCard)),
            RuntimeRecord = new ExpectedFile(RuntimeRecord, Hash(RuntimeRecord)),
            DependencyRecords = [new ExpectedFile(DependencyRecord, Hash(DependencyRecord))],
            Generation = new VoiceGenerationSettings(1m, 0.667m, 0.8m, 0m, 1m, true),
            PartBound = PartBound,
            WeightsLicence = new ConfiguredLicence("a configured weights licence", "a fixture source"),
        };
    }

    /// <summary>A package folder with one module file, its metadata and an installed-files record listing both with their hashes.</summary>
    private static string Package(string packages, string module, string version, string name, string licence)
    {
        var folder = Path.Combine(packages, module);
        var info = Path.Combine(packages, $"{module}-{version}.dist-info");
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(info);
        File.WriteAllText(Path.Combine(folder, "__init__.py"), $"# {name} fixture module\n");
        File.WriteAllText(Path.Combine(info, "METADATA"), $"Metadata-Version: 2.4\nName: {name}\nVersion: {version}\nLicense: {licence}\n\nA fixture package.\n");
        string Entry(string relative) =>
            $"{relative},sha256={Convert.ToBase64String(SHA256.HashData(File.ReadAllBytes(Path.Combine(packages, relative)))).TrimEnd('=').Replace('+', '-').Replace('/', '_')},{new FileInfo(Path.Combine(packages, relative)).Length}";
        var record = Path.Combine(info, "RECORD");
        File.WriteAllText(record, string.Join("\n",
            Entry($"{module}/__init__.py"),
            Entry($"{module}-{version}.dist-info/METADATA"),
            $"{module}/__pycache__/__init__.cpython-314.pyc,,",
            $"{module}-{version}.dist-info/RECORD,,") + "\n");
        return record;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // A stand-in terminated a moment ago can still hold its image briefly; the folder is under the temporary root.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
