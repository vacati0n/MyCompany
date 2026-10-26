using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text.Json;

namespace MediaCompany.Pilot;

/// <summary>The static half of the network guard: what the toolchain's own assemblies may reference and implement.</summary>
public sealed record GuardResult(bool Passed, IReadOnlyList<string> Closure, IReadOnlyList<string> Findings);

/// <summary>
/// THE NETWORK GUARD, STATIC PART (design D-005). The toolchain's closure of its own assemblies is held to the allowed set
/// (MediaCompany.Pilot, MediaCompany.Deterministic, MediaCompany.Application, MediaCompany.Domain); no assembly in that closure
/// references a networking assembly; no type in it implements the voice-model port; the dependency list beside the toolchain names
/// only those projects. The guard runs at the start of every produce and its result is written to the run record.
/// </summary>
public static class NetworkGuard
{
    public static readonly IReadOnlyList<string> AllowedOwn = ["MediaCompany.Pilot", "MediaCompany.Deterministic", "MediaCompany.Application", "MediaCompany.Domain"];

    public static readonly IReadOnlyList<string> Networking =
    [
        "System.Net.Http", "System.Net.Sockets", "System.Net.Primitives", "System.Net.Requests", "System.Net.WebClient",
        "System.Net.Mail", "System.Net.NameResolution", "System.Net.Security",
    ];

    public const string VoiceModelPort = "MediaCompany.Application.Ports.IVoiceModel";

    public static GuardResult Check() => Check(typeof(NetworkGuard).Assembly, AllowedOwn);

    /// <summary>Checks the closure of the root's MediaCompany assemblies against the allowed set and the rules above.</summary>
    public static GuardResult Check(Assembly root, IReadOnlyList<string> allowed)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(allowed);
        var findings = new List<string>();
        var seen = new SortedSet<string>(StringComparer.Ordinal);
        var queue = new Queue<Assembly>([root]);
        while (queue.Count > 0)
        {
            var assembly = queue.Dequeue();
            var name = assembly.GetName().Name!;
            if (!seen.Add(name))
            {
                continue;
            }

            if (!allowed.Contains(name, StringComparer.Ordinal))
            {
                findings.Add($"the assembly {name} is outside the allowed set");
            }

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (Networking.Contains(reference.Name, StringComparer.Ordinal))
                {
                    findings.Add($"{name} references the networking assembly {reference.Name}");
                }
                else if (reference.Name is { } referenced && referenced.StartsWith("MediaCompany.", StringComparison.Ordinal))
                {
                    queue.Enqueue(Assembly.Load(reference));
                }
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException partial)
            {
                types = [.. partial.Types.OfType<Type>()];
            }

            foreach (var type in types)
            {
                if (type.GetInterfaces().Any(i => i.FullName == VoiceModelPort))
                {
                    findings.Add($"the type {type.FullName} in {name} implements the voice-model port");
                }
            }
        }

        findings.AddRange(DependencyListFindings(root, allowed));
        return new GuardResult(findings.Count == 0, [.. seen], findings);
    }

    /// <summary>The dependency list beside the toolchain: every library it names must be one of the allowed projects.</summary>
    private static IEnumerable<string> DependencyListFindings(Assembly root, IReadOnlyList<string> allowed)
    {
        var location = root.Location;
        if (string.IsNullOrEmpty(location))
        {
            yield break;
        }

        var deps = Path.ChangeExtension(location, ".deps.json");
        if (!File.Exists(deps))
        {
            yield break;
        }

        using var document = JsonDocument.Parse(File.ReadAllBytes(deps));
        if (!document.RootElement.TryGetProperty("libraries", out var libraries))
        {
            yield break;
        }

        foreach (var library in libraries.EnumerateObject())
        {
            var name = library.Name.Split('/')[0];
            if (!allowed.Contains(name, StringComparer.Ordinal))
            {
                yield return $"the dependency list names {library.Name}, outside the allowed set";
            }
        }
    }
}

/// <summary>
/// THE NETWORK GUARD, RUNNING PART (design D-005). Counts every event the process's socket and name-resolution event sources
/// raise while a produce runs. The pilot opens no socket, so any count above zero holds the package.
/// </summary>
public sealed class SocketTelemetry : EventListener
{
    private static readonly string[] Watched = ["System.Net.Sockets", "System.Net.NameResolution"];
    private int _events;

    public int Events => Volatile.Read(ref _events);

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        ArgumentNullException.ThrowIfNull(eventSource);
        if (Watched.Contains(eventSource.Name, StringComparer.Ordinal))
        {
            EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (Watched.Contains(eventData.EventSource.Name, StringComparer.Ordinal))
        {
            Interlocked.Increment(ref _events);
        }
    }
}
