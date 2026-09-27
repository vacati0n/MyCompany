using System.Reflection;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Configuration;
using MediaCompany.Domain.Publication;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain.Work;
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

    // -----------------------------------------------------------------------
    // The publishing capability — the five structural absences, asserted by the build
    // -----------------------------------------------------------------------

    /// <summary>
    /// STRUCTURAL ABSENCE FOUR, the whole of it. No transport component exists in the build, and
    /// no production assembly references one.
    ///
    /// This is the assertion the design asks the BUILD to make rather than an inspection to make.
    /// The other four absences are properties of closed sets that a reader can enumerate; this one
    /// is a property of the whole compiled output, and only reflection over it can establish that
    /// the type a violation would have to name is not there.
    ///
    /// A transport component, for this purpose, is a type that could carry a publication to a
    /// destination: anything naming an upload, a publish-to, a channel or account creation, or a
    /// destination client. The check reads TYPE names as well as method names, because a class
    /// called DestinationUploader with a method called SendAsync would pass a method-name check.
    /// </summary>
    [Theory]
    [InlineData(Domain)]
    [InlineData(Application)]
    [InlineData(Deterministic)]
    [InlineData(Credentials)]
    [InlineData(Capability)]
    [InlineData(Persistence)]
    [InlineData(Host)]
    public void NoProductionAssemblyDeclaresADestinationTransportComponent(string assembly)
    {
        var forbidden = new[]
        {
            "Upload", "PublishTo", "PostTo", "CreateChannel", "CreateAccount",
            "Transport", "DestinationClient", "PlatformClient", "ChannelClient",
            "Uploader", "Publisher" + "Client", "Egress",
        };

        var offenders = Load(assembly).GetTypes()
            .Where(t => forbidden.Any(f => (t.FullName ?? t.Name).Contains(f, StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.FullName ?? t.Name)
            .ToArray();

        Assert.Empty(offenders);
    }

    /// <summary>
    /// Absence four, second half: the assembly that composes a dispatch holds no reference by which
    /// a network call is expressible.
    ///
    /// This asserts REFERENCES, which is what makes it sound where a signature scan is not. A
    /// network call written inside a method body names no type in any signature and so is invisible
    /// to a member scan — but it still forces an assembly reference on the assembly that contains
    /// it. Asserting the composing assembly's whole reference closure therefore reaches inside
    /// method bodies, which the signature scan alone does not.
    /// </summary>
    [Fact]
    public void TheAssemblyThatComposesADispatchHoldsNoReferenceReachingANetworkEgress()
    {
        var composer = Load(Deterministic).GetType(
            "MediaCompany.Deterministic.Publication.PublicationDispatchComposer", throwOnError: true)!;

        Assert.Equal(Deterministic, composer.Assembly.GetName().Name);

        // Its own first-party references, and the transitive closure of them.
        Assert.Equal(new List<string> { Application, Domain }, References(Deterministic).Order().ToList());
        Assert.Equal(
            new List<string> { Application, Deterministic, Domain },
            FirstPartyClosure(Deterministic).Order().ToList());

        // And no networking assembly anywhere in that closure, which a body-only call could not
        // avoid appearing in.
        foreach (var assembly in FirstPartyClosure(Deterministic))
        {
            Assert.Empty(NetworkingReferences(assembly));
        }
    }

    /// <summary>
    /// Only the capability assembly may hold a networking reference at all.
    ///
    /// The delivered signature scan catches a network type named in a field or a signature. This
    /// catches one called inside a method body, because the call still forces the reference. The
    /// two together are what the no-egress claim rests on.
    /// </summary>
    [Theory]
    [InlineData(Domain)]
    [InlineData(Application)]
    [InlineData(Deterministic)]
    [InlineData(Persistence)]
    public void NoAssemblyOutsideTheCapabilityBoundaryReferencesANetworkingAssembly(string assembly)
    {
        Assert.Empty(NetworkingReferences(assembly));
    }

    /// <summary>The first-party assemblies reachable from one, including itself.</summary>
    private static HashSet<string> FirstPartyClosure(string root)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>([root]);

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

        return seen;
    }

    /// <summary>The networking assemblies one assembly references by name.</summary>
    private static string[] NetworkingReferences(string name)
    {
        string[] networking =
        [
            "System.Net.Http", "System.Net.Sockets", "System.Net.Primitives",
            "System.Net.Requests", "System.Net.WebClient", "System.Net.Mail",
            "System.Net.NameResolution", "System.Net.Security",
        ];

        return Load(name).GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => networking.Contains(n, StringComparer.Ordinal))
            .ToArray();
    }

    /// <summary>
    /// STRUCTURAL ABSENCE ONE. The dispatch outcome union is closed and carries no effected case,
    /// so no caller can obtain a value meaning uploaded.
    ///
    /// Asserted over the built output rather than over the source: the union is closed by a private
    /// constructor, so this enumerates every nested case type the runtime can see and fails if a
    /// third appears or if any of them names an effect.
    /// </summary>
    [Fact]
    public void TheDispatchOutcomeUnionCarriesNoEffectedCase()
    {
        var union = Load(Domain).GetType(
            "MediaCompany.Domain.Publication.DispatchOutcome", throwOnError: true)!;

        // Every production assembly is scanned, not only the one that declares the union, so a
        // third case added anywhere in the build would be found here.
        var cases = new[] { Domain, Application, Deterministic, Credentials, Capability, Persistence, Host }
            .SelectMany(a => Load(a).GetTypes())
            .Where(t => t.BaseType == union)
            .Select(t => t.Name)
            .Order()
            .ToArray();

        Assert.Equal(new[] { "Composed", "Refused" }, cases);

        // The union cannot be extended from outside. Every constructor the base declares is
        // private, apart from the record copy constructor the compiler emits, which takes the
        // union's own type and cannot bring a new case into being.
        var declared = union
            .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(c =>
            {
                var parameters = c.GetParameters();
                return !(parameters.Length == 1 && parameters[0].ParameterType == union);
            })
            .ToArray();

        Assert.NotEmpty(declared);
        Assert.All(declared, c => Assert.True(c.IsPrivate));

        foreach (var forbidden in new[] { "Effected", "Uploaded", "Sent", "Delivered", "Published" })
        {
            Assert.DoesNotContain(cases, c => c.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// STRUCTURAL ABSENCE TWO, asserted over the REAL WORKFLOW ENGINE rather than over an
    /// enumeration declared beside it.
    ///
    /// The engine advances a unit to the position the workflow definition it is given declares
    /// next, through <see cref="WorkflowDefinition.Next"/>. The publishing workflow is a real
    /// definition over the delivered lifecycle, and composition is its last stage, so the engine's
    /// own successor lookup returns none.
    ///
    /// The second half is what makes this a structure rather than an accident: EVERY workflow
    /// definition the production build declares is examined, and any that contains the composition
    /// position must end there. A publishing workflow with a stage after composition fails here,
    /// which is the violation the accepted design names and the previous shape of this test could
    /// not have caught.
    /// </summary>
    [Fact]
    public void NoWorkflowTheBuildDeclaresHasAPositionAfterComposition()
    {
        Assert.True(PublishingWorkflow.CompositionIsTerminal());
        Assert.Null(PublishingWorkflow.Definition.Next(LifecyclePosition.PublishingComposed));
        Assert.Equal(LifecyclePosition.PublishingComposed, PublishingWorkflow.Definition.Stages[^1]);

        var declared = DeclaredWorkflows();
        Assert.NotEmpty(declared);

        foreach (var (owner, workflow) in declared)
        {
            if (!workflow.Stages.Contains(LifecyclePosition.PublishingComposed))
            {
                continue;
            }

            Assert.True(
                workflow.IsTerminal(LifecyclePosition.PublishingComposed),
                $"the workflow '{workflow.Name}' declared by {owner} has a stage after composition: "
                + $"{workflow.Next(LifecyclePosition.PublishingComposed)}");
        }
    }

    /// <summary>
    /// Absence two, behaviourally: the delivered engine, given the real publishing workflow, moves
    /// a unit that finishes composition to a TERMINAL CLAIM STATE rather than to a next position.
    ///
    /// This drives the engine itself, so it asserts the consequence rather than the declaration.
    /// </summary>
    [Fact]
    public async Task TheEngineLeavesAUnitTerminalWhenItFinishesComposition()
    {
        var work = new TerminalStateProbe();
        var service = new WorkLifecycleService(work, new ProbeClock());

        var job = new Job
        {
            Id = JobId.New(),
            Item = ItemId.New(),
            Channel = ChannelId.New(),
            Workflow = PublishingWorkflow.Name,
            Position = LifecyclePosition.PublishingComposed,
            ClaimState = ClaimState.Claimed,
            AvailableAt = ProbeClock.Instant,
        };

        await service.RecordStageOutcomeAsync(
            job, PublishingWorkflow.Definition, LifecyclePosition.PublishingComposed,
            ProbeClock.Instant, succeeded: true, failureClass: null, attemptsSoFar: 1,
            FailurePolicy.Default, TimeSpan.Zero, TimeSpan.FromHours(1), null,
            CancellationToken.None);

        var advance = Assert.Single(work.Advances);
        Assert.Equal(LifecyclePosition.Completed, advance.Position);
        Assert.Equal(ClaimState.Done, advance.State);
    }

    /// <summary>Every workflow definition any production assembly declares as a static member.</summary>
    private static IReadOnlyList<(string Owner, WorkflowDefinition Workflow)> DeclaredWorkflows()
    {
        var found = new List<(string, WorkflowDefinition)>();

        foreach (var assembly in new[] { Domain, Application, Deterministic, Credentials, Capability, Persistence, Host })
        {
            foreach (var type in Load(assembly).GetTypes())
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (property.PropertyType == typeof(WorkflowDefinition) && property.GetMethod is not null
                        && property.GetValue(null) is WorkflowDefinition fromProperty)
                    {
                        found.Add(($"{type.FullName}.{property.Name}", fromProperty));
                    }
                }

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (field.FieldType == typeof(WorkflowDefinition)
                        && field.GetValue(null) is WorkflowDefinition fromField)
                    {
                        found.Add(($"{type.FullName}.{field.Name}", fromField));
                    }
                }
            }
        }

        return found;
    }

    /// <summary>
    /// STRUCTURAL ABSENCE THREE. The closed action set holds a publication-dispatch action and no
    /// egress action, so an authority evaluation permitting a destination egress is not
    /// expressible: there is no action value for a role to hold or a check to consult.
    /// </summary>
    [Fact]
    public void TheClosedActionSetHoldsNoEgressAction()
    {
        var actions = Enum.GetNames<ActionKind>();

        Assert.Contains(nameof(ActionKind.PublicationDispatch), actions);

        foreach (var forbidden in new[] { "Upload", "Send", "Transmit", "Egress", "Transport", "Deliver" })
        {
            Assert.DoesNotContain(actions, a => a.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }

        // No role holds anything beyond the closed set, so widening authority is a code change.
        Assert.All(
            ActionSet.Roles.SelectMany(ActionSet.For),
            a => Assert.Contains(a, Enum.GetValues<ActionKind>()));
    }

    /// <summary>
    /// The publishing configuration keys are pure value slots.
    ///
    /// The prevention is not a setting, and this is the assertion that says so mechanically: every
    /// admitted publishing key passes the forbidden-fragment rule, and a proposed key naming a
    /// step, a refusal, a condition or an egress is inadmissible rather than merely absent.
    /// </summary>
    [Fact]
    public void NoAdmittedPublishingKeyReachesAControl()
    {
        Assert.All(
            PublishingConfigurationKeys.Admitted,
            key => Assert.False(
                PublishingConfigurationKeys.ReachesAControl(key),
                $"the admitted key {key} reaches a control"));

        foreach (var proposed in new[]
                 {
                     "publishing.upload-enabled",
                     "publishing.egress-endpoint",
                     "publishing.dispatch-effect",
                     "publishing.condition-override",
                     "publishing.gate-bypass",
                     "publishing.approval-default",
                 })
        {
            Assert.True(
                PublishingConfigurationKeys.ReachesAControl(proposed),
                $"the proposed key {proposed} should be inadmissible");
            Assert.False(PublishingConfigurationKeys.IsAdmitted(proposed));
        }
    }

    /// <summary>
    /// Every deterministic step the publishing path adds is registered in the rule-determined set.
    ///
    /// The expected set is DISCOVERED FROM THE CODE, not restated beside it. Every public static
    /// class in the publishing namespace of the rule-determined assembly is a step, and each must
    /// declare the task names it realizes, every one of which the registry must contain. A step
    /// added there and registered nowhere fails this check, which a comparison against a
    /// hand-maintained second list could not do: a step missing from both lists would have
    /// satisfied it.
    /// </summary>
    [Fact]
    public void EveryPublishingDeterministicStepIsRegistered()
    {
        const string PublishingNamespace = "MediaCompany.Deterministic.Publication";

        // A static class is abstract and sealed in metadata.
        var steps = Load(Deterministic).GetTypes()
            .Where(t => t.Namespace == PublishingNamespace && t is { IsAbstract: true, IsSealed: true, IsPublic: true })
            .ToArray();

        Assert.NotEmpty(steps);

        foreach (var step in steps)
        {
            var declared = step.GetField("TaskNames", BindingFlags.Public | BindingFlags.Static);

            Assert.True(
                declared is not null,
                $"{step.Name} is a publishing step and declares no TaskNames, so it cannot be "
                + "checked against the rule-determined set");

            var names = (IReadOnlyList<string>)declared!.GetValue(null)!;

            Assert.NotEmpty(names);

            foreach (var name in names)
            {
                Assert.True(
                    DeterministicTaskRegistry.Contains(name),
                    $"{step.Name} declares the step '{name}', which is not in the rule-determined set");
            }
        }

        // The effecting of a dispatch is not a member, because no such step exists to register.
        foreach (var absent in new[] { "idempotent-upload", "destination-egress", "publication-effect" })
        {
            Assert.False(DeterministicTaskRegistry.Contains(absent));
        }
    }

    // -----------------------------------------------------------------------
    // Decision D-002 — where the analytics surface is declared, and what it may carry
    // -----------------------------------------------------------------------

    /// <summary>
    /// The analytics namespaces, so an assembly that declares one is named rather than guessed.
    /// </summary>
    private const string DomainAnalytics = "MediaCompany.Domain.Analytics";
    private const string DeterministicAnalytics = "MediaCompany.Deterministic.Analytics";

    /// <summary>
    /// D-002, the membership half.
    ///
    /// The delivered closure assertions decide which assemblies the rule-determined module may
    /// reach; they decide nothing about WHERE an analytics unit is declared. This one does, and it
    /// is what makes the model-call closure hold for the analytics surface by construction: every
    /// analytics composer and every analytics read model is declared in one of the two named
    /// assemblies, so it inherits, unchanged, the exact reference equality the delivered assertion
    /// above asserts — which means an analytics unit may add no project reference at all, and the
    /// types a reasoning-model call would have to name are not referenceable from it.
    ///
    /// Two layers, and the difference between them matters. At the COMPILER, analytics code in the
    /// rule-determined module with no new project reference cannot call a reasoning capability at
    /// all, because the types are not visible to it; that is structural incapability and needs
    /// nobody to run anything. Restoring reachability by adding the reference compiles cleanly, and
    /// is caught HERE — which is a test run that something must choose to start, not a compile.
    /// </summary>
    [Fact]
    public void EveryAnalyticsTypeIsDeclaredInOneOfTheTwoNamedAssemblies()
    {
        var misplaced = new List<string>();

        foreach (var assembly in new[] { Domain, Application, Deterministic, Credentials, Capability, Persistence, Host })
        {
            foreach (var type in Load(assembly).GetTypes())
            {
                var space = type.Namespace ?? string.Empty;

                if (space.StartsWith(DomainAnalytics, StringComparison.Ordinal) && assembly != Domain)
                {
                    misplaced.Add($"{type.FullName} is declared in {assembly}, not in {Domain}");
                }

                if (space.StartsWith(DeterministicAnalytics, StringComparison.Ordinal) && assembly != Deterministic)
                {
                    misplaced.Add($"{type.FullName} is declared in {assembly}, not in {Deterministic}");
                }
            }
        }

        Assert.Empty(misplaced);

        // Both namespaces are populated, so the check is over something rather than over nothing.
        Assert.NotEmpty(AnalyticsTypes(Domain, DomainAnalytics));
        Assert.NotEmpty(AnalyticsTypes(Deterministic, DeterministicAnalytics));
    }

    /// <summary>
    /// D-001 and D-002, the representation half.
    ///
    /// No type in the analytics namespaces carries a bare numeric member or a free-form value
    /// member. The one exception is the measurement quantity union itself and its three cases,
    /// which ARE the representation: the amount lives on the observed-value case and nowhere else,
    /// so the only way to reach a number on the analytics surface is to resolve the case first.
    ///
    /// The refusal names the offending type and the offending member, because a check that says
    /// only that something is wrong is one somebody disables.
    /// </summary>
    [Fact]
    public void NoAnalyticsTypeCarriesABareNumericOrFreeFormValueMember()
    {
        var offenders = new List<string>();

        foreach (var (assembly, space) in new[] { (Domain, DomainAnalytics), (Deterministic, DeterministicAnalytics) })
        {
            foreach (var type in AnalyticsTypes(assembly, space))
            {
                if (IsTheMeasurementRepresentation(type))
                {
                    continue;
                }

                offenders.AddRange(BareValueMembers(type));
            }
        }

        Assert.Empty(offenders);
    }

    /// <summary>
    /// The same rule, applied to types that deliberately break it.
    ///
    /// Without this the check above would pass on an empty scan, or on a rule that matched
    /// nothing, and nobody would know. The probes are declared in the test assembly, so they are
    /// not in the analytics namespaces and the check above does not see them.
    /// </summary>
    [Fact]
    public void TheAnalyticsMemberRuleRefusesABareNumericAndAFreeFormValueAndNamesTheOffender()
    {
        var numeric = Assert.Single(BareValueMembers(typeof(ProbeCarryingABareNumericMember)));
        Assert.Contains(nameof(ProbeCarryingABareNumericMember), numeric, StringComparison.Ordinal);
        Assert.Contains("Minutes", numeric, StringComparison.Ordinal);
        Assert.Contains("bare Decimal", numeric, StringComparison.Ordinal);

        var freeForm = Assert.Single(BareValueMembers(typeof(ProbeCarryingAFreeFormValueMember)));
        Assert.Contains(nameof(ProbeCarryingAFreeFormValueMember), freeForm, StringComparison.Ordinal);
        Assert.Contains("free-form value slot", freeForm, StringComparison.Ordinal);

        // And a read model built from the measurement quantity alone passes the same rule.
        Assert.Empty(BareValueMembers(typeof(ProbeCarryingOnlyAMeasurementQuantity)));
    }

    /// <summary>
    /// Every member of one type that would let an unobserved quantity reach a reader as a number
    /// or as unclassified text. The message names the type and the member, because a refusal that
    /// says only that something is wrong is one somebody disables.
    /// </summary>
    private static IReadOnlyList<string> BareValueMembers(Type type)
    {
        Type[] numeric =
        [
            typeof(decimal), typeof(double), typeof(float),
            typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
        ];

        if (type.IsEnum)
        {
            return [];
        }

        const BindingFlags Declared =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        var members = type.GetProperties(Declared)
            .Where(m => m.DeclaringType == type)
            .Select(m => (m.Name, Type: m.PropertyType))
            .Concat(type.GetFields(Declared)
                .Where(m => m.DeclaringType == type)
                // A compiler-emitted backing field mirrors the property that declares it, so
                // reporting both would name one offence twice.
                .Where(m => !m.Name.Contains('<', StringComparison.Ordinal))
                .Select(m => (m.Name, Type: m.FieldType)));

        var offenders = new List<string>();

        foreach (var (name, memberType) in members)
        {
            var bare = Nullable.GetUnderlyingType(memberType) ?? memberType;

            if (numeric.Contains(bare))
            {
                offenders.Add(
                    $"{type.FullName}.{name} is a bare {bare.Name}; an analytics quantity is a "
                    + "MeasurementQuantity, so that an unobserved quantity has no number to print");
            }

            if (bare == typeof(string) && name.EndsWith("Value", StringComparison.Ordinal))
            {
                offenders.Add(
                    $"{type.FullName}.{name} is a free-form value slot; record content reaching a "
                    + "reader through one arrives unclassified");
            }
        }

        return offenders;
    }

    /// <summary>The types one assembly declares in one analytics namespace, compiler artefacts aside.</summary>
    private static Type[] AnalyticsTypes(string assembly, string space) =>
        Load(assembly).GetTypes()
            .Where(t => (t.Namespace ?? string.Empty).StartsWith(space, StringComparison.Ordinal))
            .Where(t => !(t.FullName ?? t.Name).Contains('<', StringComparison.Ordinal))
            .ToArray();

    /// <summary>
    /// The measurement quantity union and its three cases. They are the declared representation,
    /// and the observed-value case is the one place on the whole surface an amount is allowed to
    /// live.
    /// </summary>
    private static bool IsTheMeasurementRepresentation(Type type)
    {
        var union = Load(Domain).GetType("MediaCompany.Domain.Analytics.MeasurementQuantity", throwOnError: true)!;
        return type == union || type.BaseType == union;
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

/// <summary>
/// The narrowest possible unit of work: it records the advances the engine makes and nothing else.
///
/// It exists so the absence-two demonstration can drive the DELIVERED engine and observe where a
/// unit ends up, rather than reading a declaration and trusting the engine agrees with it.
/// </summary>
internal sealed class TerminalStateProbe : IUnitOfWork
{
    public List<(LifecyclePosition Position, ClaimState State)> Advances { get; } = [];

    public Task<IWorkTransaction> BeginAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IWorkTransaction>(new Txn(this));

    private sealed class Txn(TerminalStateProbe owner) : IWorkTransaction
    {
        public IAuditAppender Audit { get; } = new NoAudit();
        public IOperationRecorder Operations { get; } = new NoOperations();
        public IJobWriter Jobs { get; } = new Writer(owner);
        public IBudgetEvaluator Budgets { get; } = new NoBudgets();
        public IGateWriter Gates { get; } = new NoGates();
        public IRouteAvailabilityWriter Availability { get; } = new NoAvailability();
        public IDispatchWriter Dispatches { get; } = new NoDispatches();

        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class Writer(TerminalStateProbe owner) : IJobWriter
    {
        public Task EnqueueAsync(Job job, CancellationToken ct) => Task.CompletedTask;

        public Task RecordStageAsync(JobStage stage, CancellationToken ct) => Task.CompletedTask;

        public Task<Job?> ClaimNextAsync(string workerId, TimeSpan lease, CancellationToken ct) =>
            Task.FromResult<Job?>(null);

        public Task ReleaseAsync(JobId job, ClaimState state, DateTimeOffset availableAt, CancellationToken ct) =>
            Task.CompletedTask;

        public Task AdvanceAsync(
            JobId job, LifecyclePosition position, ClaimState state,
            DateTimeOffset availableAt, CancellationToken ct)
        {
            owner.Advances.Add((position, state));
            return Task.CompletedTask;
        }
    }

    private sealed class NoAudit : IAuditAppender
    {
        public Task<MediaCompany.Domain.Audit.AuditEntry> AppendAsync(
            AuditEntryDraft draft, CancellationToken ct) =>
            Task.FromResult<MediaCompany.Domain.Audit.AuditEntry>(null!);

        public Task<MediaCompany.Domain.Audit.AuditEntry> AppendRefusalAsync(
            MediaCompany.Domain.AuditEntryId targetEntry,
            MediaCompany.Domain.Audit.AuditRefusalReason reason, string actor, CancellationToken ct) =>
            Task.FromResult<MediaCompany.Domain.Audit.AuditEntry>(null!);
    }

    private sealed class NoOperations : IOperationRecorder
    {
        public Task<MediaCompany.Domain.Accounting.OperationRecord> RecordAsync(
            OperationDraft draft, CancellationToken ct) =>
            throw new NotSupportedException("the lifecycle engine records no operation");
    }

    private sealed class NoBudgets : IBudgetEvaluator
    {
        public Task<IReadOnlyList<MediaCompany.Domain.Accounting.BudgetAlert>> EvaluateAsync(
            MediaCompany.Domain.Capabilities.Attribution attribution, DateOnly period,
            DateTimeOffset raisedAt, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MediaCompany.Domain.Accounting.BudgetAlert>>([]);
    }

    private sealed class NoGates : IGateWriter
    {
        public Task RecordApprovalAsync(MediaCompany.Domain.Publication.Approval approval, CancellationToken ct) =>
            Task.CompletedTask;

        public Task RecordBlockAsync(MediaCompany.Domain.Publication.Block block, CancellationToken ct) =>
            Task.CompletedTask;

        public Task RecordTransitionAsync(
            MediaCompany.Domain.ItemId item, MediaCompany.Domain.ItemVersion version,
            MediaCompany.Domain.Publication.GateState from, MediaCompany.Domain.Publication.GateState to,
            string reason, DateTimeOffset at, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NoAvailability : IRouteAvailabilityWriter
    {
        public Task RecordAsync(MediaCompany.Domain.Capabilities.RouteAvailability availability, CancellationToken ct) =>
            Task.CompletedTask;
    }

    private sealed class NoDispatches : IDispatchWriter
    {
        public Task<DispatchWriteOutcome> RecordDispatchAsync(
            MediaCompany.Domain.Publication.DispatchRecord record, CancellationToken ct) =>
            throw new NotSupportedException("the lifecycle engine writes no dispatch");

        public Task RecordAttemptAsync(
            MediaCompany.Domain.Publication.AttemptRecord attempt, CancellationToken ct) =>
            throw new NotSupportedException("the lifecycle engine records no attempt");
    }
}

/// <summary>A fixed clock for the probe.</summary>
internal sealed class ProbeClock : IClock
{
    internal static readonly DateTimeOffset Instant = DateTimeOffset.Parse("2026-10-01T12:00:00Z");

    public DateTimeOffset UtcNow => Instant;
}

/// <summary>An analytics read model as it must NOT be written: the quantity is a bare number.</summary>
internal sealed record ProbeCarryingABareNumericMember(decimal Minutes);

/// <summary>An analytics read model as it must NOT be written: the value is free-form text.</summary>
internal sealed record ProbeCarryingAFreeFormValueMember(string ReportedValue);

/// <summary>An analytics read model as it must be written.</summary>
internal sealed record ProbeCarryingOnlyAMeasurementQuantity(
    MediaCompany.Domain.Analytics.MeasurementQuantity Review);
