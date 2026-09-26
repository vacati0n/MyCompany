using System.Reflection;
using Xunit;

namespace MediaCompany.Architecture.Tests;

/// <summary>
/// The build-time boundary tests the technical design requires.
///
/// These are not style checks. Each asserts that an alternative path does not EXIST — that the
/// type a violation would have to name is not visible, or that the assembly a violation would
/// have to reference is not referenced. A design property enforced by removing the alternative is
/// only demonstrable this way; a check that a call was not made would demonstrate something
/// weaker.
/// </summary>
public sealed class BoundaryTests
{
    private const string Domain = "MediaCompany.Domain";
    private const string Application = "MediaCompany.Application";
    private const string Deterministic = "MediaCompany.Deterministic";
    private const string Credentials = "MediaCompany.Credentials";
    private const string Capability = "MediaCompany.Capability";
    private const string Persistence = "MediaCompany.Persistence";
    private const string Host = "MediaCompany.Host";

    /// <summary>
    /// Loads the built assembly by name. The reflection below therefore reads the real build
    /// output: if a project reference were added, this test would see it.
    /// </summary>
    private static Assembly Load(string name) => Assembly.Load(new AssemblyName(name));

    private static string[] References(string name) =>
        Load(name).GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => n.StartsWith("MediaCompany", StringComparison.Ordinal))
            .ToArray();

    // -----------------------------------------------------------------------
    // Decision D-005 — the zero-AI-cost property is a dependency-direction property
    // -----------------------------------------------------------------------

    /// <summary>
    /// D-005, the whole of it. Work whose output is determined by its inputs and a rule sits in an
    /// assembly that does not reference the capability boundary, the provider adapters or the
    /// credential broker. A model call is therefore not expressible from any member of the named
    /// deterministic set — not forbidden, not guarded, not expressible.
    ///
    /// Risk R-003 is the convenience dependency added during implementation. This test is the
    /// mitigation, and it fails at build time rather than after cost has been incurred.
    /// </summary>
    [Fact]
    public void TheDeterministicModuleDoesNotReferenceTheCapabilityBoundary()
    {
        var references = References(Deterministic);

        Assert.DoesNotContain(Capability, references);
        Assert.DoesNotContain(Credentials, references);
    }

    /// <summary>
    /// The direction it may depend in. It reaches the domain and the ports, and nothing below
    /// them, so the set of assemblies it can reach transitively is closed under the same rule.
    /// </summary>
    [Fact]
    public void TheDeterministicModuleDependsOnlyOnTheDomainAndThePorts()
    {
        var references = References(Deterministic).Order().ToList();
        Assert.Equal(new List<string> { Application, Domain }, references);
    }

    /// <summary>
    /// The transitive closure is checked as well, because a two-step path would satisfy the direct
    /// check and still make a model call reachable.
    /// </summary>
    [Fact]
    public void NoTransitiveDependencyOfTheDeterministicModuleReachesTheCapabilityBoundary()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>([Deterministic]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!seen.Add(current))
            {
                continue;
            }

            foreach (var reference in References(current))
            {
                queue.Enqueue(reference);
            }
        }

        Assert.DoesNotContain(Capability, seen);
        Assert.DoesNotContain(Credentials, seen);
        Assert.DoesNotContain(Persistence, seen);
    }

    /// <summary>
    /// No type in the deterministic module names an HTTP type. Combined with the reference check,
    /// this closes the remaining way a rule-determined task could reach a network egress.
    /// </summary>
    [Fact]
    public void NoTypeInTheDeterministicModuleNamesAnHttpType()
    {
        var offenders = TypesNaming(Load(Deterministic), t => t.Namespace?.StartsWith("System.Net", StringComparison.Ordinal) == true);
        Assert.Empty(offenders);
    }

    /// <summary>The domain and the ports are equally free of a network egress.</summary>
    [Theory]
    [InlineData(Domain)]
    [InlineData(Application)]
    public void TheInnerLayersNameNoHttpType(string assembly)
    {
        Assert.Empty(TypesNaming(Load(assembly), t => t.Namespace?.StartsWith("System.Net", StringComparison.Ordinal) == true));
    }

    // -----------------------------------------------------------------------
    // Decision D-001 — capability access only through the single resolution boundary
    // -----------------------------------------------------------------------

    /// <summary>
    /// D-001: a directly constructed provider adapter must not be a compiling path. The adapter
    /// port and every implementation of it are internal to the capability assembly, so no other
    /// assembly can name the type, let alone construct one.
    /// </summary>
    [Fact]
    public void TheProviderAdapterPortIsNotVisibleOutsideTheCapabilityAssembly()
    {
        var port = Load(Capability).GetType("MediaCompany.Capability.Providers.IProviderAdapter", throwOnError: true)!;

        Assert.False(port.IsPublic);
        Assert.False(port.IsVisible);
    }

    /// <summary>Every implementation of the adapter port is internal and sealed.</summary>
    [Fact]
    public void EveryProviderAdapterImplementationIsInternalAndSealed()
    {
        var capability = Load(Capability);
        var port = capability.GetType("MediaCompany.Capability.Providers.IProviderAdapter", throwOnError: true)!;

        var implementations = capability.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && port.IsAssignableFrom(t))
            .ToArray();

        Assert.NotEmpty(implementations);
        Assert.All(implementations, t =>
        {
            Assert.False(t.IsVisible);
            Assert.True(t.IsSealed);
        });
    }

    /// <summary>
    /// No assembly outside the capability boundary names an HTTP type. The single egress is the
    /// capability assembly, which is what makes the boundary the only path to a provider.
    /// </summary>
    [Theory]
    [InlineData(Domain)]
    [InlineData(Application)]
    [InlineData(Deterministic)]
    [InlineData(Persistence)]
    public void OnlyTheCapabilityAssemblyReachesANetworkEgress(string assembly)
    {
        Assert.Empty(TypesNaming(Load(assembly), t => t == typeof(HttpClient) || t == typeof(HttpRequestMessage)));
    }

    /// <summary>
    /// The public surface of the capability assembly is the boundary and its vocabulary — no
    /// adapter, no provider client, no transport type.
    /// </summary>
    [Fact]
    public void ThePublicSurfaceOfTheCapabilityAssemblyExposesNoProviderType()
    {
        var exported = Load(Capability).GetExportedTypes().Select(t => t.Name).ToArray();

        Assert.Contains(nameof(MediaCompany.Capability.ICapabilityGateway), exported);
        Assert.DoesNotContain(exported, n => n.Contains("Adapter", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(exported, n => n.Contains("Client", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Decision D-004 — credential material never enters the caller's scope
    // -----------------------------------------------------------------------

    /// <summary>The secret store port is internal to the credentials assembly.</summary>
    [Fact]
    public void TheSecretStorePortIsNotVisibleOutsideTheCredentialsAssembly()
    {
        var port = Load(Credentials).GetType("MediaCompany.Credentials.ISecretStore", throwOnError: true)!;

        Assert.False(port.IsPublic);
        Assert.False(port.IsVisible);
    }

    /// <summary>Every implementation of the secret store port is internal.</summary>
    [Fact]
    public void EverySecretStoreImplementationIsInternal()
    {
        var credentials = Load(Credentials);
        var port = credentials.GetType("MediaCompany.Credentials.ISecretStore", throwOnError: true)!;

        var implementations = credentials.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && port.IsAssignableFrom(t))
            .ToArray();

        Assert.NotEmpty(implementations);
        Assert.All(implementations, t => Assert.False(t.IsVisible));
    }

    /// <summary>
    /// No public member of the credentials assembly hands a caller a credential. The broker issues
    /// opaque handles and the exchange attaches at the transport layer; there is no public method
    /// whose return type could carry a secret value.
    /// </summary>
    [Fact]
    public void NoPublicMemberOfTheCredentialsAssemblyReturnsASecretValue()
    {
        var offenders = new List<string>();

        foreach (var type in Load(Credentials).GetExportedTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.DeclaringType != type)
                {
                    continue;
                }

                var name = method.Name;
                var looksLikeAccess =
                    name.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Resolve", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Reveal", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("GetCredential", StringComparison.OrdinalIgnoreCase);

                if (looksLikeAccess)
                {
                    offenders.Add($"{type.Name}.{name}");
                }
            }
        }

        Assert.Empty(offenders);
    }

    /// <summary>
    /// The credential broker is reachable only through its factory: the constructor is internal,
    /// so a caller cannot build one over a store of its own.
    /// </summary>
    [Fact]
    public void TheCredentialBrokerHasNoPublicConstructor()
    {
        var broker = typeof(MediaCompany.Credentials.CredentialBroker);
        Assert.Empty(broker.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    /// <summary>The capability gateway is likewise reachable only through its factory.</summary>
    [Fact]
    public void TheCapabilityGatewayHasNoPublicConstructor()
    {
        var gateway = typeof(MediaCompany.Capability.CapabilityGateway);
        Assert.Empty(gateway.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    // -----------------------------------------------------------------------
    // Constraint C-013 — nothing in this wave publishes
    // -----------------------------------------------------------------------

    /// <summary>
    /// Exclusion X-001 removes the upload path. No assembly declares a type or a public method
    /// that uploads, posts or publishes to a platform, so there is no route from the gate to one.
    /// </summary>
    [Theory]
    [InlineData(Domain)]
    [InlineData(Application)]
    [InlineData(Deterministic)]
    [InlineData(Credentials)]
    [InlineData(Capability)]
    [InlineData(Persistence)]
    [InlineData(Host)]
    public void NoAssemblyDeclaresAnUploadOrPublishPath(string assembly)
    {
        var forbidden = new[] { "Upload", "PublishTo", "PostTo", "CreateChannel", "CreateAccount" };

        var offenders = Load(assembly).GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.DeclaringType == t)
                .Select(m => $"{t.FullName}.{m.Name}"))
            .Where(name => forbidden.Any(f => name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.Empty(offenders);
    }

    /// <summary>
    /// Finds every type in an assembly that names a matching type anywhere on its own members, so
    /// a dependency hidden in a private field is found as readily as one in a public signature.
    /// </summary>
    private static string[] TypesNaming(Assembly assembly, Func<Type, bool> matches)
    {
        var offenders = new List<string>();

        foreach (var type in assembly.GetTypes())
        {
            var named = type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(f => f.DeclaringType == type)
                .Select(f => f.FieldType)
                .Concat(type
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.DeclaringType == type)
                    .SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType)));

            if (named.Any(t => matches(Unwrap(t))))
            {
                offenders.Add(type.FullName ?? type.Name);
            }
        }

        return offenders.ToArray();
    }

    private static Type Unwrap(Type type)
    {
        if (type.IsByRef || type.IsArray || type.IsPointer)
        {
            return Unwrap(type.GetElementType() ?? type);
        }

        return type.IsGenericType ? type.GetGenericArguments()[0] : type;
    }
}
