using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace MediaCompany.Architecture.Tests;

/// <summary>
/// The boundary assertions the own-voice change adds (decision D-016 of its design). Each asserts that an alternative does
/// not EXIST — a fifth narration source, a network client or the capability boundary on the own-source path, a model or
/// runtime named in production code, a model, voice or runtime file in the repository tree, a binary column in the tenth
/// resource — and each is shown to FIND its seeded violation in a probe. The delivered one-starter and one-writer
/// assertions are kept unchanged and already cover every type this change adds.
/// </summary>
public sealed partial class BoundaryTests
{
    /// <summary>The own-source path: whole types, and the members of the produce run and the one starter that serve the own sources.</summary>
    private static readonly string[] OwnSourceTypes =
    [
        "MediaCompany.Deterministic.Production.NarrationSourceRule",
        "MediaCompany.Deterministic.Production.RecordingSetCheck",
        "MediaCompany.Deterministic.Production.DurationExpectation",
        "MediaCompany.Production.RecordingRegistrar",
        "MediaCompany.Production.InstallationVerifier",
        "MediaCompany.Production.HeldInstallation",
        "MediaCompany.Production.FileHashes",
        "MediaCompany.Production.MediaToolProbing",
    ];

    private static readonly (string Type, string[] Members)[] OwnSourceMembers =
    [
        ("MediaCompany.Production.ProduceItemService+ProductionRun",
            ["RecordingPartsAsync", "ModelPartsAsync", "RederiveAsync", "Generate", "Provenance", "MeasureAsync", "Measured", "Unmeasured"]),
        ("MediaCompany.Production.ProduceItemService", ["SelectOwnAsync"]),
        ("MediaCompany.Production.ExternalMediaTool", ["GenerateAsync", "MeasureAudioAsync"]),
    ];

    /// <summary>The narration source is a CLOSED set of exactly four members (decision D-001).</summary>
    [Fact]
    public void TheNarrationSourceHasExactlyFourMembers()
    {
        Assert.Equal(new[] { "Recording", "InHouseModel", "Fake", "Vendor" }, Enum.GetNames(typeof(MediaCompany.Domain.Production.NarrationSource)));
        Assert.Equal(new[] { 1, 2, 3, 4 }, Enum.GetValues<MediaCompany.Domain.Production.NarrationSource>().Select(v => (int)v));
    }

    /// <summary>
    /// NO TYPE OF THE OWN-SOURCE PATH constructs or names a network client, a socket, a credential or the capability boundary,
    /// in any field, parameter or compiled body, closures and state machines included: the rule, the set check, the expectation,
    /// the registrar, the verifier and its hold, and the produce run's and the starter's own-source members.
    /// </summary>
    [Fact]
    public void NoTypeOfTheOwnSourcePathNamesANetworkClientACredentialOrTheCapabilityBoundary()
    {
        var types = OwnSourceTypes.Select(name => ProductionAssemblies.Select(a => Load(a).GetType(name)).SingleOrDefault(t => t is not null)).ToArray();
        Assert.All(types.Zip(OwnSourceTypes), t => Assert.True(t.First is not null, $"{t.Second} is named on the own-source path and no production assembly declares it"));

        var reach = types.SelectMany(t => NetworkReach(t!, null)).ToList();
        foreach (var (name, members) in OwnSourceMembers)
        {
            var type = Load(Production).GetType(name);
            Assert.True(type is not null, $"{name} is not declared");
            foreach (var member in members)
            {
                Assert.True(type!.GetMethods(Declared).Any(m => m.Name == member), $"{name}.{member} is named on the own-source path and is not declared");
            }

            reach.AddRange(NetworkReach(type!, members));
        }

        Assert.Empty(reach);
    }

    /// <summary>The network scan applied to a probe naming a network client: it is found and named.</summary>
    [Fact]
    public void TheNetworkScanFindsAProbeNamingANetworkClientAndNamesIt()
    {
        Assert.Contains(NetworkReach(typeof(ProbeOwnSourceReachingTheNetwork), null), r => r.Contains(nameof(ProbeOwnSourceReachingTheNetwork), StringComparison.Ordinal));
    }

    /// <summary>
    /// NO PRODUCTION ASSEMBLY NAMES THE MODEL'S INTERPRETER, ITS RUNTIME, A VOICE FILE OR THE ENVIRONMENT'S FILES: each is located
    /// by configuration alone, as the renderer and the probe are (the delivered executable scan keeps covering ".exe").
    /// </summary>
    [Fact]
    public void NoProductionAssemblyNamesTheModelItsRuntimeOrItsFiles()
    {
        Assert.Empty(ProductionAssemblies.SelectMany(name => ModelLiterals(Load(name))));
    }

    /// <summary>The model-name scan applied to a probe naming one: it is found.</summary>
    [Fact]
    public void TheModelNameScanFindsAProbeNamingTheRuntime()
    {
        Assert.NotEmpty(ModelLiterals(typeof(ProbeOwnSourceReachingTheNetwork).Assembly));
    }

    /// <summary>
    /// NO FILE IN THE REPOSITORY TREE is a voice model, a voice configuration, a model card, a package record, an environment
    /// configuration or a runtime launcher: the model, its runtime and the release document live outside the repository.
    /// </summary>
    [Fact]
    public void NoFileInTheRepositoryTreeIsAModelVoiceRuntimeOrPackageFile()
    {
        Assert.Empty(ModelFiles(RepositoryRoot()));
    }

    /// <summary>The file scan applied to a seeded probe folder: every seeded name is found.</summary>
    [Fact]
    public void TheModelFileScanFindsEverySeededFile()
    {
        var probe = Path.Combine(Path.GetTempPath(), "mediacompany-model-file-probe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(probe, "pkg-1.0.dist-info"));
        try
        {
            foreach (var name in new[] { "voice.onnx", "voice.onnx.json", "MODEL_CARD", "pkg-1.0.dist-info/RECORD", "pyvenv.cfg", "piper.exe" })
            {
                File.WriteAllText(Path.Combine(probe, name), "a seeded probe file");
            }

            Assert.Equal(7, ModelFiles(probe).Count);
        }
        finally
        {
            Directory.Delete(probe, recursive: true);
        }
    }

    /// <summary>THE TENTH RESOURCE DECLARES NO BINARY COLUMN: no document, recording or model file can be stored by it.</summary>
    [Fact]
    public void TheTenthResourceDeclaresNoBinaryColumn()
    {
        var resource = File.ReadAllText(Path.Combine(RepositoryRoot(), "db", "010-own-voice.sql"));
        Assert.Empty(BinaryColumns(resource));
        Assert.NotEmpty(BinaryColumns("CREATE TABLE seeded_probe (document bytea NOT NULL);"));
    }

    // -----------------------------------------------------------------------
    // The scans
    // -----------------------------------------------------------------------

    private static bool Networked(Type? type) =>
        type is not null
        && ((type.Namespace ?? string.Empty).StartsWith("System.Net", StringComparison.Ordinal)
            || (type.Namespace ?? string.Empty).StartsWith("MediaCompany.Capability", StringComparison.Ordinal)
            || (type.Namespace ?? string.Empty).StartsWith("MediaCompany.Credentials", StringComparison.Ordinal));

    /// <summary>Every way a type, or the named members of it with their closures and state machines, reaches a network, credential or boundary type.</summary>
    private static IReadOnlyList<string> NetworkReach(Type type, IReadOnlyList<string>? members)
    {
        static IEnumerable<Type> Flat(Type t) => t.IsGenericType ? t.GetGenericArguments().SelectMany(Flat).Prepend(t) : t.HasElementType ? Flat(t.GetElementType()!) : [t];

        var reach = new List<string>();
        var bodies = new List<MethodBase>();
        if (members is null)
        {
            foreach (var field in type.GetFields(Declared).Where(f => Flat(f.FieldType).Any(Networked)))
            {
                reach.Add($"{type.FullName}.{field.Name} holds {field.FieldType.Name}");
            }

            bodies.AddRange(type.GetMethods(Declared).Where(m => m.DeclaringType == type));
            bodies.AddRange(type.GetConstructors(Declared));
            bodies.AddRange(type.GetNestedTypes(Declared).SelectMany(n => n.GetMethods(Declared).Where(m => m.DeclaringType == n).Cast<MethodBase>().Concat(n.GetConstructors(Declared))));
        }
        else
        {
            bodies.AddRange(type.GetMethods(Declared).Where(m => m.DeclaringType == type && members.Contains(m.Name)));
            foreach (var nested in type.GetNestedTypes(Declared).Where(n => members.Any(m => n.Name.StartsWith($"<{m}>", StringComparison.Ordinal))))
            {
                bodies.AddRange(nested.GetMethods(Declared).Where(m => m.DeclaringType == nested));
            }

            // Closures declared by those members carry the member's name in their method names.
            foreach (var nested in type.GetNestedTypes(Declared).Where(n => n.Name.Contains("DisplayClass", StringComparison.Ordinal) || n.Name == "<>c"))
            {
                bodies.AddRange(nested.GetMethods(Declared).Where(m => m.DeclaringType == nested && members.Any(x => m.Name.StartsWith($"<{x}>", StringComparison.Ordinal))));
            }
        }

        foreach (var body in bodies)
        {
            foreach (var parameter in body.GetParameters().Where(p => Flat(p.ParameterType).Any(Networked)))
            {
                reach.Add($"{type.FullName}.{body.Name} takes {parameter.ParameterType.Name}");
            }

            foreach (var member in IlOperands(body).OfType<MemberInfo>())
            {
                if (Networked(member.DeclaringType) || (member is Type t && Networked(t)))
                {
                    reach.Add($"{type.FullName}.{body.Name} names {member.DeclaringType?.Name}.{member.Name}");
                }
            }
        }

        return reach;
    }

    private static IEnumerable<string> ModelLiterals(Assembly assembly) =>
        assembly.GetTypes()
            .SelectMany(t => t.GetMethods(Declared).Where(m => m.DeclaringType == t).Cast<MethodBase>().Concat(t.GetConstructors(Declared)))
            .SelectMany(m => IlOperands(m).OfType<string>().Select(s => (Owner: m.DeclaringType!, Literal: s)))
            .Where(x => Regex.IsMatch(x.Literal, @"\bpython(w)?(\d+(\.\d+)?)?(\.exe)?\b|\bpiper\b|\.onnx\b|\bpyvenv\b|\ben_GB\b|\bcori\b", RegexOptions.IgnoreCase))
            .Select(x => $"{x.Owner.FullName}: \"{x.Literal}\"");

    /// <summary>Every file under a root (build output and version control excluded) named as a model, voice, runtime or package file.</summary>
    private static IReadOnlyList<string> ModelFiles(string root)
    {
        var separator = Path.DirectorySeparatorChar;
        bool Skipped(string path) =>
            path.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
            || path.Contains($"{separator}obj{separator}", StringComparison.Ordinal)
            || path.Contains($"{separator}.git{separator}", StringComparison.Ordinal);

        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => !Skipped(f))
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                return name.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".onnx.json", StringComparison.OrdinalIgnoreCase)
                    || name is "MODEL_CARD" or "RECORD" or "pyvenv.cfg"
                    || name.Equals("piper.exe", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("python.exe", StringComparison.OrdinalIgnoreCase);
            });
        var folders = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .Where(d => !Skipped(d + separator) && d.EndsWith(".dist-info", StringComparison.OrdinalIgnoreCase));
        return files.Concat(folders).ToArray();
    }

    private static IReadOnlyList<string> BinaryColumns(string sql) =>
        Regex.Matches(sql, @"\b(bytea|oid|lo|blob)\b(?![_a-z])", RegexOptions.IgnoreCase)
            .Select(m => m.Value)
            .ToArray();
}

/// <summary>A probe on an own-source path that reaches a network client and names the runtime: each scan must find it.</summary>
internal static class ProbeOwnSourceReachingTheNetwork
{
    public static Task<string> Fetch() => new HttpClient().GetStringAsync(new Uri("https://probe.invalid/"));

    public static string Runtime() => "python.exe -m piper";
}
