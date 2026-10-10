using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace MediaCompany.Architecture.Tests;

/// <summary>
/// The boundary assertions the production change adds (decision D-020 of its design). Each asserts that an
/// alternative does not EXIST in the built output — a second process starter, a second file writer, a hard-coded
/// executable path, a subject term or an item identifier in production code, a reach from the produce path to a
/// publication, dispatch, release-credential or channel-configuration type, a fake constructible by the metered
/// composition — and each is shown to FIND its seeded violation in a probe type, so a scan that found nothing because
/// it could see nothing would fail here.
/// </summary>
public sealed partial class BoundaryTests
{
    private const string Production = "MediaCompany.Production";

    /// <summary>The one type that starts a process, and the one that writes files (decisions D-013 and D-014).</summary>
    private const string TheProcessStarter = "MediaCompany.Production.ExternalMediaTool";
    private const string TheFileWriter = "MediaCompany.Production.ArtifactWriter";

    // -----------------------------------------------------------------------
    // One process starter
    // -----------------------------------------------------------------------

    /// <summary>
    /// EXACTLY ONE TYPE STARTS A PROCESS across every production assembly: the media tool. A type that names a
    /// process start or a start specification anywhere in its compiled bodies, closures included, is a starter.
    /// </summary>
    [Fact]
    public void ExactlyOneTypeAcrossTheProductionAssembliesStartsAProcess()
    {
        var starters = ProductionAssemblies.SelectMany(name => OwnersReaching(Load(name), StartsAProcess)).Distinct().Order().ToArray();
        Assert.Equal(new[] { TheProcessStarter }, starters);
    }

    /// <summary>The process scan applied to a probe that starts a process: it is found and named.</summary>
    [Fact]
    public void TheProcessScanFindsAProbeStartingAProcessAndNamesIt()
    {
        var found = OwnersReaching(typeof(ProbeSecondProcessStarter).Assembly, StartsAProcess);
        Assert.Contains(typeof(ProbeSecondProcessStarter).FullName!, found);
    }

    // -----------------------------------------------------------------------
    // One file writer
    // -----------------------------------------------------------------------

    /// <summary>
    /// EXACTLY ONE TYPE WRITES FILES across every production assembly: the artifact writer. A type naming a member
    /// that creates, writes, moves, copies or deletes a file or a folder, or a writable stream over a path, is a
    /// writer; reading a file is not writing one.
    /// </summary>
    [Fact]
    public void ExactlyOneTypeAcrossTheProductionAssembliesWritesFiles()
    {
        var writers = ProductionAssemblies.SelectMany(name => OwnersReaching(Load(name), WritesAFile)).Distinct().Order().ToArray();
        Assert.Equal(new[] { TheFileWriter }, writers);
    }

    /// <summary>The writer scan applied to a probe that writes a file: it is found and named.</summary>
    [Fact]
    public void TheWriterScanFindsAProbeWritingAFileAndNamesIt()
    {
        var found = OwnersReaching(typeof(ProbeSecondFileWriter).Assembly, WritesAFile);
        Assert.Contains(typeof(ProbeSecondFileWriter).FullName!, found);
    }

    /// <summary>The writer starts no process, and the process starter writes no file: each boundary is one type, alone.</summary>
    [Fact]
    public void TheWriterStartsNoProcessAndTheStarterWritesNoFile()
    {
        var production = Load(Production);
        Assert.DoesNotContain(TheFileWriter, OwnersReaching(production, StartsAProcess));
        Assert.DoesNotContain(TheProcessStarter, OwnersReaching(production, WritesAFile));
    }

    // -----------------------------------------------------------------------
    // No hard-coded executable path
    // -----------------------------------------------------------------------

    /// <summary>
    /// No production assembly holds a string naming an executable: the renderer and the probe are located by
    /// configuration alone (decision D-013).
    /// </summary>
    [Fact]
    public void NoProductionAssemblyHoldsAHardCodedExecutablePath()
    {
        Assert.Empty(ProductionAssemblies.SelectMany(name => ExecutableLiterals(Load(name))));
    }

    /// <summary>The executable scan applied to a probe naming one: it is found.</summary>
    [Fact]
    public void TheExecutableScanFindsAProbeNamingAnExecutable()
    {
        Assert.NotEmpty(ExecutableLiterals(typeof(ProbeSecondProcessStarter).Assembly));
    }

    // -----------------------------------------------------------------------
    // No subject term and no item identifier in production code
    // -----------------------------------------------------------------------

    /// <summary>
    /// Nothing in the production path is specific to an item (decision D-020; criterion on item-agnostic source):
    /// no subject term of the item material and no item identifier appears in any production assembly's string
    /// literals, type or member names, or in any production source file. The terms are read from the item material
    /// itself, so the check follows the material.
    /// </summary>
    [Fact]
    public void NoSubjectTermOrItemIdentifierIsInProductionCode()
    {
        var (terms, item) = MaterialTerms();
        Assert.NotEmpty(terms);
        Assert.Empty(SubjectHits(ProductionAssemblies.Select(Load), SourceFiles(), terms, item));
    }

    /// <summary>The subject scan applied to a probe holding a subject term: it is found and named.</summary>
    [Fact]
    public void TheSubjectScanFindsAProbeHoldingASubjectTermAndNamesIt()
    {
        var (terms, item) = MaterialTerms();
        var hits = SubjectHits([typeof(ProbeSubjectSpecificStage).Assembly], [], terms, item);
        Assert.Contains(hits, h => h.Contains(nameof(ProbeSubjectSpecificStage), StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // Nothing publishes: the produce path reaches no publication path
    // -----------------------------------------------------------------------

    /// <summary>
    /// The produce path reaches no publication, dispatch, release-credential or channel-configuration type, in any
    /// declared member or compiled body of any type of the production assembly; and its closure holds no publishing
    /// assembly beyond the rule-determined one it shares with every caller.
    /// </summary>
    [Fact]
    public void TheProducePathReachesNoPublicationPath()
    {
        Assert.Empty(PublicationReach(Load(Production)));
    }

    /// <summary>The publication scan applied to a probe reaching a dispatch: it is found and named.</summary>
    [Fact]
    public void ThePublicationScanFindsAProbeReachingADispatchAndNamesIt()
    {
        Assert.Contains(PublicationReach(typeof(ProbeProducerThatDispatches).Assembly), f => f.Contains(nameof(ProbeProducerThatDispatches), StringComparison.Ordinal));
    }

    /// <summary>
    /// The production assembly references the domain, the ports, the rule-determined assembly and the capability
    /// boundary, and nothing else first-party (decision D-012): not the persistence adapters, not the host.
    /// </summary>
    [Fact]
    public void TheProductionAssemblyReferencesOnlyItsDeclaredFirstPartyAssemblies()
    {
        Assert.Equal(
            new[] { Application, Capability, Deterministic, Domain }.Order(),
            References(Production).Order());
    }

    // -----------------------------------------------------------------------
    // Fakes: built only by the demonstration composition
    // -----------------------------------------------------------------------

    /// <summary>
    /// A fake provider is constructed ONLY in the demonstration factory member, and the metered member names no fake
    /// type (decision D-004): a metered composition cannot build one, so it can never fall back to one.
    /// </summary>
    [Fact]
    public void OnlyTheDemonstrationFactoryMemberConstructsAFake()
    {
        var capability = Load(Capability);
        var fakes = capability.GetTypes().Where(t => t.Name.StartsWith("Fake", StringComparison.Ordinal)).ToArray();
        Assert.Equal(3, fakes.Length);
        Assert.All(fakes, f => Assert.True(f is { IsVisible: false, IsSealed: true }, $"{f.Name} is internal and sealed"));

        var constructingSites = capability.GetTypes()
            .SelectMany(t => t.GetMethods(Declared).Where(m => m.DeclaringType == t).Cast<MethodBase>().Concat(t.GetConstructors(Declared)))
            .Where(m => IlOperands(m).OfType<ConstructorInfo>().Any(c => fakes.Contains(c.DeclaringType)))
            .Select(m => Owner(m.DeclaringType!).FullName + "." + m.Name)
            .Distinct()
            .ToArray();

        Assert.NotEmpty(constructingSites);
        Assert.All(constructingSites, site => Assert.Contains("MediaCompany.Capability.CapabilityGatewayFactory", site, StringComparison.Ordinal));
        Assert.DoesNotContain(constructingSites, site => site.EndsWith(".Create", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // The scans
    // -----------------------------------------------------------------------

    private static bool StartsAProcess(MemberInfo member) =>
        (member.DeclaringType == typeof(Process) && member.Name is "Start")
        || (member is ConstructorInfo constructor && constructor.DeclaringType == typeof(ProcessStartInfo));

    private static readonly string[] FileWritingMembers =
    [
        "WriteAllText", "WriteAllTextAsync", "WriteAllBytes", "WriteAllBytesAsync", "WriteAllLines", "WriteAllLinesAsync",
        "AppendAllText", "AppendAllTextAsync", "AppendAllLines", "AppendAllLinesAsync", "AppendText", "Create", "CreateText",
        "Move", "Copy", "Delete", "Replace", "OpenWrite", "CreateDirectory", "CreateSymbolicLink", "SetAttributes",
    ];

    private static bool WritesAFile(MemberInfo member)
    {
        var declaring = member.DeclaringType;
        if (declaring is null)
        {
            return false;
        }

        if ((declaring == typeof(File) || declaring == typeof(Directory) || declaring == typeof(FileInfo) || declaring == typeof(DirectoryInfo))
            && FileWritingMembers.Contains(member.Name, StringComparer.Ordinal))
        {
            return true;
        }

        // A file stream or a stream writer over a path is a write site wherever it is constructed.
        return member is ConstructorInfo && (declaring == typeof(FileStream) || declaring == typeof(StreamWriter));
    }

    /// <summary>The outermost declared type of every compiled body in an assembly that names a member matching the rule.</summary>
    private static IReadOnlyList<string> OwnersReaching(Assembly assembly, Func<MemberInfo, bool> rule)
    {
        var owners = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in assembly.GetTypes())
        {
            var bodies = type.GetMethods(Declared).Where(m => m.DeclaringType == type).Cast<MethodBase>()
                .Concat(type.GetConstructors(Declared));
            foreach (var method in bodies)
            {
                if (IlOperands(method).OfType<MemberInfo>().Any(rule))
                {
                    owners.Add(Owner(type).FullName ?? type.Name);
                }
            }
        }

        return owners.ToArray();
    }

    /// <summary>A compiler-generated nested type (a closure, a state machine) is reported as the type that declares it.</summary>
    private static Type Owner(Type type)
    {
        var owner = type;
        while (owner.IsNested && (owner.Name.Contains('<', StringComparison.Ordinal)
                                  || owner.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)))
        {
            owner = owner.DeclaringType!;
        }

        return owner;
    }

    private static IEnumerable<string> ExecutableLiterals(Assembly assembly) =>
        assembly.GetTypes()
            .SelectMany(t => t.GetMethods(Declared).Where(m => m.DeclaringType == t).Cast<MethodBase>().Concat(t.GetConstructors(Declared)))
            .SelectMany(m => IlOperands(m).OfType<string>().Select(s => (Owner: m.DeclaringType!, Literal: s)))
            .Where(x => x.Literal.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        || Regex.IsMatch(x.Literal, @"\bff(mpeg|probe)\b", RegexOptions.IgnoreCase))
            .Select(x => $"{Owner(x.Owner).FullName}: \"{x.Literal}\"");

    /// <summary>Every place a subject term (on word boundaries) or the item identifier appears in assemblies or source files.</summary>
    private static IReadOnlyList<string> SubjectHits(IEnumerable<Assembly> assemblies, IEnumerable<string> sourceFiles, IReadOnlyList<string> terms, string item)
    {
        var patterns = terms.Select(t => new Regex($@"\b{Regex.Escape(t)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .Append(new Regex(Regex.Escape(item), RegexOptions.IgnoreCase))
            .ToArray();
        bool Hit(string text) => patterns.Any(p => p.IsMatch(text));

        var hits = new List<string>();
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (Hit(type.Name))
                {
                    hits.Add($"{assembly.GetName().Name}: type {type.FullName}");
                }

                foreach (var member in type.GetMembers(Declared).Where(m => m.DeclaringType == type && Hit(m.Name)))
                {
                    hits.Add($"{assembly.GetName().Name}: member {type.FullName}.{member.Name}");
                }

                var bodies = type.GetMethods(Declared).Where(m => m.DeclaringType == type).Cast<MethodBase>().Concat(type.GetConstructors(Declared));
                foreach (var literal in bodies.SelectMany(m => IlOperands(m).OfType<string>()).Where(Hit))
                {
                    hits.Add($"{assembly.GetName().Name}: {Owner(type).FullName} holds \"{literal}\"");
                }
            }
        }

        foreach (var file in sourceFiles)
        {
            var text = File.ReadAllText(file);
            if (Hit(text))
            {
                hits.Add($"source {file}");
            }
        }

        return hits;
    }

    /// <summary>The repository's root, found by walking up from the built test assembly to the solution file.</summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MediaCompany.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root holding MediaCompany.slnx was not found.");
    }

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    /// <summary>The subject terms and the item identifier, read from the item material held beside the package.</summary>
    private static (IReadOnlyList<string> Terms, string Item) MaterialTerms()
    {
        using var material = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "wave-2", "item-001", "item-material.json")));
        var terms = material.RootElement.GetProperty("subjectTerms").GetProperty("terms").EnumerateArray().Select(t => t.GetString()!).ToArray();
        return (terms, material.RootElement.GetProperty("item").GetString()!);
    }

    /// <summary>Every way a type of the assembly reaches a publication, dispatch, release-credential or channel-configuration type.</summary>
    private static IReadOnlyList<string> PublicationReach(Assembly assembly)
    {
        static bool Forbidden(Type? type) =>
            type is not null
            && ((type.Namespace ?? string.Empty).StartsWith("MediaCompany.Deterministic.Publication", StringComparison.Ordinal)
                || (type.Namespace ?? string.Empty).StartsWith("MediaCompany.Domain.Publication", StringComparison.Ordinal)
                || type == typeof(MediaCompany.Deterministic.Services.PublicationDispatchService)
                || type == typeof(MediaCompany.Deterministic.Services.PublicationGateService)
                || type == typeof(MediaCompany.Deterministic.Services.PublishingSequenceService)
                || type == typeof(MediaCompany.Deterministic.Services.ChannelProfileService)
                || type == typeof(MediaCompany.Domain.Configuration.ChannelConfigurationKeys)
                || type == typeof(MediaCompany.Application.Ports.IDispatchWriter)
                || type == typeof(MediaCompany.Application.Ports.IGateWriter)
                || type == typeof(MediaCompany.Credentials.CredentialRequest)
                || type == typeof(MediaCompany.Credentials.ICredentialBroker));

        var reach = new List<string>();
        foreach (var type in assembly.GetTypes())
        {
            foreach (var field in type.GetFields(Declared).Where(f => f.DeclaringType == type && Flatten(f.FieldType).Any(Forbidden)))
            {
                reach.Add($"{Owner(type).FullName}.{field.Name} holds {field.FieldType.Name}");
            }

            var bodies = type.GetMethods(Declared).Where(m => m.DeclaringType == type).Cast<MethodBase>().Concat(type.GetConstructors(Declared));
            foreach (var method in bodies)
            {
                foreach (var parameter in method.GetParameters().Where(p => Flatten(p.ParameterType).Any(Forbidden)))
                {
                    reach.Add($"{Owner(type).FullName}.{method.Name} takes {parameter.ParameterType.Name}");
                }

                foreach (var member in IlOperands(method).OfType<MemberInfo>())
                {
                    if (Forbidden(member.DeclaringType) || (member is Type t && Forbidden(t)))
                    {
                        reach.Add($"{Owner(type).FullName}.{method.Name} names {member.DeclaringType?.Name}.{member.Name}");
                    }
                }
            }
        }

        return reach;
    }
}

// ---------------------------------------------------------------------------
// The probes: each holds exactly the violation its scan must find. They live in this test assembly only.
// ---------------------------------------------------------------------------

/// <summary>A probe that starts a second process from a hard-coded executable path.</summary>
internal static class ProbeSecondProcessStarter
{
    public static void Start() => Process.Start("C:/tools/ffmpeg.exe")?.Dispose();
}

/// <summary>A probe that writes a file outside the one writer.</summary>
internal static class ProbeSecondFileWriter
{
    public static void Write(string path) => File.WriteAllText(path, "written outside the one writer");
}

/// <summary>A probe stage that holds a subject term, as a production stage written for one item would.</summary>
internal static class ProbeSubjectSpecificStage
{
    public static string Caption() => "the waggle dance of the honey bee";
}

/// <summary>A probe producer that composes a dispatch.</summary>
internal static class ProbeProducerThatDispatches
{
    public static Type Dispatch() => typeof(MediaCompany.Deterministic.Services.PublicationDispatchService);
}
