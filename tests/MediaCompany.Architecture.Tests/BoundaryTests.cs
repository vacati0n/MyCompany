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

    /// <summary>
    /// The publishing entry point and the dossier recorder sit in the RULE-DETERMINED assembly, so
    /// the no-model closure asserted above covers them by the same proof that covers every other
    /// deterministic step: that assembly references the domain and the ports and nothing else, so
    /// neither service can name the capability boundary or the credential broker, and a model call
    /// from either does not compile.
    ///
    /// The second half holds them to it from the other side. Every dependency either takes is
    /// declared in the domain, the ports or the rule-determined assembly itself, so neither can be
    /// handed a capability, a credential or a persistence adapter by the composition root either.
    /// </summary>
    [Fact]
    public void TheEntryPointAndTheDossierRecorderSitInTheRuleDeterminedAssembly()
    {
        var permitted = new[] { Domain, Application, Deterministic };

        // Wave 6 adds the channel profile service, the copyright-check stage handler and the
        // rights-check step, the dossier recorder's first production caller, to the same proof.
        foreach (var service in new[]
                 {
                     typeof(PublishingSequenceService), typeof(ItemDossierRecorder), typeof(ChannelProfileService),
                     typeof(MediaCompany.Deterministic.Production.CopyrightCheckStageHandler), typeof(RightsCheckStep),
                 })
        {
            Assert.Equal(Deterministic, service.Assembly.GetName().Name);

            var dependencies = service.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType)
                .Concat(service
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Where(f => f.DeclaringType == service)
                    .Select(f => f.FieldType))
                .ToArray();

            Assert.NotEmpty(dependencies);

            foreach (var dependency in dependencies)
            {
                var owner = dependency.Assembly.GetName().Name ?? string.Empty;
                Assert.True(
                    permitted.Contains(owner) || owner.StartsWith("System", StringComparison.Ordinal),
                    $"{service.Name} depends on {dependency.FullName}, declared in {owner}, outside the domain, the ports "
                    + "and the rule-determined assembly");
            }
        }
    }

    // -----------------------------------------------------------------------
    // The multi-channel capability — channel keys reach no control, and one component reads them
    // -----------------------------------------------------------------------

    /// <summary>
    /// The channel configuration keys are pure value slots (the multi-channel design, decision D-001).
    /// Every admitted channel key passes the forbidden-fragment rule, and a proposed key naming the
    /// payee, a payment account, a registration, a condition, the approval step, a gate, a refusal, a
    /// block, publication, dispatch, an upload, a route, a trigger, a due time, liveness or an
    /// enablement is inadmissible rather than merely absent.
    /// </summary>
    [Fact]
    public void NoAdmittedChannelKeyReachesAControl()
    {
        Assert.All(
            ChannelConfigurationKeys.Admitted,
            key => Assert.False(ChannelConfigurationKeys.ReachesAControl(key), $"the admitted channel key {key} reaches a control"));
        Assert.Equal(8, ChannelConfigurationKeys.Admitted.Count);

        foreach (var proposed in new[]
                 {
                     "channel.payee", "channel.payment-account", "channel.account-holder", "channel.library-registration",
                     "channel.condition-override", "channel.approval-default", "channel.gate-skip", "channel.refusal-text",
                     "channel.block-clear", "channel.publish-enabled", "channel.dispatch-target", "channel.upload-endpoint",
                     "channel.route", "channel.routing-target", "channel.trigger", "channel.due-at", "channel.live",
                     "channel.enable",
                 })
        {
            Assert.True(ChannelConfigurationKeys.ReachesAControl(proposed), $"the proposed channel key {proposed} should be inadmissible");
            Assert.False(ChannelConfigurationKeys.IsAdmitted(proposed));
        }

        // No channel key is a base key, a production key or a publishing key, so none of those
        // surfaces can be reached through a channel scope.
        Assert.Empty(ChannelConfigurationKeys.Admitted.Intersect(ConfigurationKeys.Admitted));
        Assert.Empty(ChannelConfigurationKeys.Admitted.Intersect(PublishingConfigurationKeys.Admitted));
        Assert.Empty(ChannelConfigurationKeys.Admitted.Intersect(MediaCompany.Application.Production.ProductionConfigurationKeys.Admitted));
    }

    /// <summary>
    /// ONE COMPONENT READS A CHANNEL KEY (decision D-001): the channel profile service. Beside it only
    /// the key set's own declaration and the configuration store's admission check name the key set,
    /// so no schedule, content strategy or any other per-channel value can be read as a due time, a
    /// trigger, a routing target, a gate state, a refusal, a precondition or a payee.
    ///
    /// The check reads the COMPILED METHOD BODIES of every production assembly: a channel key used
    /// anywhere is a string literal the compiler inlines, and a use of the key set's members is a
    /// member reference, and both are found there whatever the source spelled.
    /// </summary>
    [Fact]
    public void OnlyTheChannelProfileServiceReadsAChannelKey()
    {
        var offenders = ChannelKeyReaders()
            .Where(owner => !PermittedChannelKeyReaders.Contains(owner))
            .ToArray();

        Assert.Empty(offenders);

        // The check is over something: the profile service is found reading the keys.
        Assert.Contains("MediaCompany.Deterministic.Services.ChannelProfileService", ChannelKeyReaders());
    }

    /// <summary>
    /// The scan the single-reader rule rests on, applied to a probe that reads a channel key outside
    /// the profile service: it is found and named, so the rule cannot pass by matching nothing.
    /// </summary>
    [Fact]
    public void TheChannelKeyScanFindsAReaderOutsideTheProfileServiceAndNamesIt()
    {
        var found = ChannelKeyReadersIn(typeof(ProbeReadingAChannelKey).Assembly);
        Assert.Contains(typeof(ProbeReadingAChannelKey).FullName!, found);
    }

    /// <summary>
    /// The approval queue is a VIEW (decision D-005): its port, the channel partition port and the
    /// item register port have READ MEMBERS ONLY. The member set of each is declared here, so a member
    /// added to any of them — a write, a presentation, a decision, a send-back, a submission or a
    /// transition included — fails the build until it is named here, where a reader sees it; and every
    /// member returns a read.
    /// </summary>
    [Fact]
    public void TheApprovalQueueAndTheChannelPortsHaveReadMembersOnly()
    {
        var declared = new Dictionary<Type, string[]>
        {
            [typeof(IApprovalQueueReader)] = ["AwaitingOwnerApprovalAsync", "DecidedAsync"],
            [typeof(IChannelPartitionReader)] =
                ["OperationsAsync", "CloseMonthAsync", "DossiersAsync", "ThroughputAsync", "BudgetsAsync", "TierDistributionAsync"],
            [typeof(IItemRegister)] = ["RecordedChannelAsync"],

            // The AI-economics change: the benchmark record's reader has read members only.
            [typeof(IBenchmarkReader)] = ["ObservationsAsync", "RecordAsync"],
        };

        foreach (var (port, reads) in declared)
        {
            var members = port.GetMethods();
            Assert.Equal(reads.Order(StringComparer.Ordinal), members.Select(m => m.Name).Distinct().Order(StringComparer.Ordinal));

            foreach (var member in members)
            {
                Assert.True(
                    member.ReturnType.IsGenericType && member.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
                    $"{port.Name}.{member.Name} does not return a read");
            }

            Assert.Empty(port.GetProperties());
            Assert.Empty(port.GetEvents());
        }
    }

    /// <summary>
    /// The repository declares ONE version, in the shared build properties, and every production
    /// assembly carries it (the multi-channel change, as decided at its Design Gate).
    /// </summary>
    [Fact]
    public void EveryProductionAssemblyCarriesTheDeclaredVersion()
    {
        foreach (var name in ProductionAssemblies)
        {
            var assembly = Load(name);
            // 1.4.0, decided at the Design Gate of the AI-economics change.
            Assert.Equal(new Version(1, 4, 0, 0), assembly.GetName().Version);

            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            Assert.NotNull(informational);
            Assert.StartsWith("1.4.0", informational, StringComparison.Ordinal);
        }
    }

    /// <summary>The types allowed to name the channel key set, each for the reason its summary states.</summary>
    private static readonly string[] PermittedChannelKeyReaders =
    [
        // The one composing site of a channel's profile.
        "MediaCompany.Deterministic.Services.ChannelProfileService",

        // The key set's own declaration.
        "MediaCompany.Domain.Configuration.ChannelConfigurationKeys",

        // The configuration store's admission check, which admits a channel key only under a
        // channel scope and reads no value.
        "MediaCompany.Persistence.NpgsqlConfigurationStore",
    ];

    private static IReadOnlyList<string> ChannelKeyReaders() =>
        ProductionAssemblies.SelectMany(name => ChannelKeyReadersIn(Load(name))).Distinct().ToArray();

    /// <summary>
    /// Every type in an assembly whose compiled method bodies load a channel key literal or reference
    /// a member of the channel key set. A compiler-generated nested type is reported as the type that
    /// declares it.
    /// </summary>
    private static IReadOnlyList<string> ChannelKeyReadersIn(Assembly assembly)
    {
        var keySet = typeof(ChannelConfigurationKeys);
        var keys = ChannelConfigurationKeys.Admitted;
        var readers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in assembly.GetTypes())
        {
            var owner = type;
            while (owner.IsNested && (owner.Name.Contains('<', StringComparison.Ordinal) || owner.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)))
            {
                owner = owner.DeclaringType!;
            }

            var bodies = type.GetMethods(Declared).Where(m => m.DeclaringType == type).Cast<MethodBase>()
                .Concat(type.GetConstructors(Declared));

            foreach (var method in bodies)
            {
                foreach (var operand in IlOperands(method))
                {
                    var hit = operand switch
                    {
                        string literal => keys.Contains(literal),
                        MemberInfo member => member.DeclaringType == keySet,
                        _ => false,
                    };

                    if (hit)
                    {
                        readers.Add(owner.FullName ?? owner.Name);
                    }
                }
            }
        }

        return readers.ToArray();
    }

    private static readonly IReadOnlyDictionary<short, System.Reflection.Emit.OpCode> OpCodesByValue =
        typeof(System.Reflection.Emit.OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (System.Reflection.Emit.OpCode)f.GetValue(null)!)
            .ToDictionary(o => o.Value);

    /// <summary>
    /// The string literals and member references a compiled method body names, decoded opcode by
    /// opcode, so no operand is mistaken for another.
    /// </summary>
    private static IEnumerable<object> IlOperands(MethodBase method)
    {
        byte[]? il;
        try
        {
            il = method.GetMethodBody()?.GetILAsByteArray();
        }
        catch (InvalidOperationException)
        {
            yield break;
        }

        if (il is null)
        {
            yield break;
        }

        var module = method.Module;
        var genericTypes = method.DeclaringType?.IsGenericType == true ? method.DeclaringType.GetGenericArguments() : null;
        var genericMethods = method.IsGenericMethod ? method.GetGenericArguments() : null;

        for (var at = 0; at < il.Length;)
        {
            short value = il[at] == 0xFE ? (short)(0xFE00 | il[at + 1]) : il[at];
            at += il[at] == 0xFE ? 2 : 1;

            if (!OpCodesByValue.TryGetValue(value, out var code))
            {
                yield break;
            }

            switch (code.OperandType)
            {
                case System.Reflection.Emit.OperandType.InlineNone:
                    break;
                case System.Reflection.Emit.OperandType.ShortInlineBrTarget:
                case System.Reflection.Emit.OperandType.ShortInlineI:
                case System.Reflection.Emit.OperandType.ShortInlineVar:
                    at += 1;
                    break;
                case System.Reflection.Emit.OperandType.InlineVar:
                    at += 2;
                    break;
                case System.Reflection.Emit.OperandType.InlineI8:
                case System.Reflection.Emit.OperandType.InlineR:
                    at += 8;
                    break;
                case System.Reflection.Emit.OperandType.InlineSwitch:
                    var cases = BitConverter.ToInt32(il, at);
                    at += 4 + (4 * cases);
                    break;
                case System.Reflection.Emit.OperandType.InlineString:
                {
                    var token = BitConverter.ToInt32(il, at);
                    at += 4;
                    yield return module.ResolveString(token);
                    break;
                }

                case System.Reflection.Emit.OperandType.InlineMethod:
                case System.Reflection.Emit.OperandType.InlineField:
                case System.Reflection.Emit.OperandType.InlineTok:
                case System.Reflection.Emit.OperandType.InlineType:
                {
                    var token = BitConverter.ToInt32(il, at);
                    at += 4;
                    MemberInfo? member = null;
                    try
                    {
                        member = module.ResolveMember(token, genericTypes, genericMethods);
                    }
                    catch (ArgumentException)
                    {
                    }

                    if (member is not null)
                    {
                        yield return member;
                    }

                    break;
                }

                default:
                    at += 4;
                    break;
            }
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

    /// <summary>Every production assembly, in the order the other assertions name them.</summary>
    private static readonly string[] ProductionAssemblies =
        [Domain, Application, Deterministic, Credentials, Capability, Persistence, Host];

    /// <summary>
    /// The types outside the two analytics namespaces that are allowed to carry an analytics type
    /// on a declared member, each because it is a boundary the surface is reached through rather
    /// than a part of the surface.
    ///
    /// The list is what makes membership DECIDED rather than assumed. An analytics type declared
    /// anywhere else, or carried by any other type, fails the assertion below until someone adds
    /// it here — which is a line in a diff a reviewer reads, rather than a namespace nobody
    /// noticed was skipped.
    /// </summary>
    private static readonly string[] PermittedCarriersOutsideTheAnalyticsNamespaces =
    [
        // The read ports. Declared with the other ports so the rule-determined assembly reaches
        // them without a reference of its own.
        "MediaCompany.Application.Ports.ICostRollupReader",
        "MediaCompany.Application.Ports.IServedTierReader",
        "MediaCompany.Application.Ports.IRevenueParameterRegister",

        // The reporting surface's line type, which carries the measurement quantity in the
        // position its free-form string value used to occupy.
        "MediaCompany.Deterministic.Services.ReportedMeasure",

        // The adapters that implement the ports, which sit below the boundary and read upward.
        "MediaCompany.Persistence.NpgsqlCostReader",
        "MediaCompany.Persistence.NpgsqlRevenueParameterRegister",

        // The measure catalogue, which is where the six revenue-derived figures are declared as
        // deferred measures and therefore names the closed figure set.
        "MediaCompany.Deterministic.Reporting.MeasureCatalogue",

        // Wave 7, the AI-economics capability. The ports and drafts the benchmark record and the admission
        // ledger are reached through, declared with the other ports.
        "MediaCompany.Application.Ports.IAdmissionLedger",
        "MediaCompany.Application.Ports.AdmissionDecisionDraft",
        "MediaCompany.Application.Ports.IBenchmarkWriter",
        "MediaCompany.Application.Ports.BenchmarkObservationDraft",
        "MediaCompany.Application.Ports.IBenchmarkReader",
        "MediaCompany.Application.Ports.BenchmarkRecordSummary",

        // The rule-determined rules that read the controller decision and the evidence table: the
        // resolution function and its inputs and record, the evidence selection and its outcome, and the
        // cost controller, which is the one composing site of every governing reading's cases.
        "MediaCompany.Deterministic.Routing.ResolutionInputs",
        "MediaCompany.Deterministic.Routing.ResolutionRecord",
        "MediaCompany.Deterministic.Routing.RouteResolver",
        "MediaCompany.Deterministic.Routing.EvidenceSelection",
        "MediaCompany.Deterministic.Routing.SelectionOutcome",
        "MediaCompany.Deterministic.Accounting.CostController",

        // The adapters that implement the added ports, below the boundary, and their one row encoding.
        "MediaCompany.Persistence.NpgsqlAdmissionLedger",
        "MediaCompany.Persistence.NpgsqlBenchmarkWriter",
        "MediaCompany.Persistence.NpgsqlBenchmarkReader",
        "MediaCompany.Persistence.MeasurementColumns",
        "MediaCompany.Persistence.BenchmarkRows",
    ];

    /// <summary>
    /// The string members the analytics surface and its permitted carriers are allowed to declare,
    /// each an AUTHORED LABEL rather than a value slot: a statement this code composes, never
    /// record content copied through.
    ///
    /// The list is declared rather than pattern-matched, because a pattern over member names
    /// decides nothing — the delivered defect was a member called Value, and the next one will be
    /// called something else. A new string member on an analytics type is refused until it is
    /// named here, where the question "is this an authored label or a value slot?" is asked once,
    /// by a person.
    /// </summary>
    private static readonly string[] PermittedAuthoredLabels =
    [
        "MediaCompany.Domain.Analytics.SingleRecordCaveat.Statement",
        "MediaCompany.Domain.Analytics.RefusalReadModel.Precondition",
        "MediaCompany.Domain.Analytics.RefusalReadModel.RecordedState",
        "MediaCompany.Domain.Analytics.RefusalReadModel.Detail",
        "MediaCompany.Domain.Analytics.CostReadModel.PriceBasis",
        "MediaCompany.Domain.Analytics.StageOutcomeReadModel.Statement",
        "MediaCompany.Domain.Analytics.CompositionStepReadModel.DispatchRecordEvidence",
        "MediaCompany.Domain.Analytics.CompositionStepReadModel.AuditEntryEvidence",
        "MediaCompany.Domain.Analytics.CompositionStepReadModel.Statement",
        "MediaCompany.Domain.Analytics.SupplyAuditReadModel.Subject",
        "MediaCompany.Domain.Analytics.SupplyAuditReadModel.Library",
        "MediaCompany.Domain.Analytics.SupplyAuditReadModel.WhatWouldObtainIt",
        "MediaCompany.Domain.Analytics.DeterminationOutcomeReadModel.Statement",
        "MediaCompany.Domain.Analytics.ServedTierReadModel.ServedStatement",
        "MediaCompany.Domain.Analytics.TierRatioReadModel.AssumptionLabel",
        "MediaCompany.Domain.Analytics.RevenueDerivedFigureReadModel.RecordedSource",
        "MediaCompany.Domain.Analytics.ObservedRevenueParameter.SourceObservation",
        "MediaCompany.Domain.Analytics.RevenueParameterRecord.SourceObservation",
        "MediaCompany.Deterministic.Analytics.AnalyticsComposers.PriceBasis",
        "MediaCompany.Deterministic.Analytics.AnalyticsComposers.SplitAssumption",
        "MediaCompany.Deterministic.Services.ReportedMeasure.Name",
        "MediaCompany.Deterministic.Services.ReportedMeasure.Source",
        "MediaCompany.Domain.Analytics.SingleRecordCaveat.Standard",
        "MediaCompany.Deterministic.Reporting.MeasureCatalogue.RevenueDerivedNames",

        // Wave 5. The tier ratio's required definition, and the required statement each reading
        // carries of what its record set does and does not establish; each is composed by the
        // surface, never copied out of a record.
        "MediaCompany.Domain.Analytics.TierRatioReadModel.Definition",
        "MediaCompany.Domain.Analytics.ThroughputReadModel.Statement",
        "MediaCompany.Domain.Analytics.ItemDossierReadModel.Statement",
        "MediaCompany.Domain.Analytics.SupplyAuditReading.Statement",
        "MediaCompany.Domain.Analytics.DeterminationReading.Statement",
        "MediaCompany.Deterministic.Analytics.AnalyticsComposers.TierRatioDefinition",
        "MediaCompany.Deterministic.Analytics.AnalyticsComposers.ThroughputStatement",
        "MediaCompany.Deterministic.Analytics.AnalyticsComposers.DossierStatement",
        "MediaCompany.Deterministic.Analytics.AnalyticsComposers.SupplyAuditStatement",
        "MediaCompany.Deterministic.Analytics.AnalyticsComposers.DeterminationStatement",

        // Wave 6, the multi-channel capability. The finality statement every month reading carries,
        // the statement each channel-partitioned reading carries of what it does and does not
        // establish, the company ceiling's coverage statement, and the two-clock limit the deferred
        // cycle time states; each is composed by the surface, never copied out of a record.
        "MediaCompany.Domain.Analytics.MonthFinality.Statement",
        "MediaCompany.Domain.Analytics.OperationPartitionReading.Statement",
        "MediaCompany.Domain.Analytics.OperationPartitionReading.RevenueStatement",
        "MediaCompany.Domain.Analytics.DossierPartitionReading.Statement",
        "MediaCompany.Domain.Analytics.ThroughputPartitionReading.Statement",
        "MediaCompany.Domain.Analytics.CompanyCeilingReading.CoverageStatement",
        "MediaCompany.Domain.Analytics.ChannelApprovalListing.Statement",
        "MediaCompany.Domain.Analytics.ChannelApprovalWorkload.Statement",
        "MediaCompany.Deterministic.Analytics.ChannelAnalyticsComposers.OperationPartitionStatement",
        "MediaCompany.Deterministic.Analytics.ChannelAnalyticsComposers.RevenueStatement",
        "MediaCompany.Deterministic.Analytics.ChannelAnalyticsComposers.DossierPartitionStatement",
        "MediaCompany.Deterministic.Analytics.ChannelAnalyticsComposers.ThroughputPartitionStatement",
        "MediaCompany.Deterministic.Analytics.ChannelAnalyticsComposers.CeilingCoverageStatement",
        "MediaCompany.Deterministic.Analytics.ChannelAnalyticsComposers.ApprovalListingStatement",
        "MediaCompany.Deterministic.Analytics.ChannelAnalyticsComposers.ApprovalWorkloadStatement",
        "MediaCompany.Deterministic.Reporting.MeasureCatalogue.CycleTimeClockLimit",

        // Wave 7, the AI-economics capability. The units an observation is stated in, the statement each
        // benchmark, selection, controller and tier-distribution reading carries, the assumed split and
        // the relation statement, the registers a recorded amount names, and the task names, the tier and
        // reservation statements and the company basis the rules compose; each is authored by the code,
        // never copied out of a record.
        "MediaCompany.Domain.Analytics.BenchmarkObservation.QualityRatingUnit",
        "MediaCompany.Domain.Analytics.BenchmarkObservation.LatencyUnit",
        "MediaCompany.Domain.Analytics.BenchmarkRecordReading.Statement",
        "MediaCompany.Domain.Analytics.SelectionRecord.Statement",
        "MediaCompany.Domain.Analytics.GoverningReading.Statement",
        "MediaCompany.Domain.Analytics.ControllerDecision.CompanyBasisStatement",
        "MediaCompany.Domain.Analytics.TierDistributionReading.AssumedSplit",
        "MediaCompany.Domain.Analytics.TierDistributionReading.RelationStatement",
        "MediaCompany.Deterministic.Analytics.EconomicsComposers.BenchmarkStatement",
        "MediaCompany.Deterministic.Analytics.EconomicsComposers.AssumedSplit",
        "MediaCompany.Deterministic.Analytics.EconomicsComposers.RelationStatement",
        "MediaCompany.Deterministic.Analytics.ChannelAnalyticsComposers.BudgetRegister",
        "MediaCompany.Deterministic.Analytics.ChannelAnalyticsComposers.EnvelopeConstant",
        "MediaCompany.Application.Ports.AdmissionDecisionDraft.TierStatement",
        "MediaCompany.Application.Ports.AdmissionDecisionDraft.ReservationStatement",
        "MediaCompany.Deterministic.Routing.ResolutionRecord.TierStatement",
        "MediaCompany.Deterministic.Routing.RouteResolver.TaskName",
        "MediaCompany.Deterministic.Routing.EvidenceSelection.TaskName",
        "MediaCompany.Deterministic.Accounting.CostController.TaskName",
        "MediaCompany.Deterministic.Accounting.CostController.CompanyBasisRecordedIn",
        "MediaCompany.Deterministic.Accounting.CostController.ChannelBudgetRecordedIn",
        "MediaCompany.Deterministic.Accounting.CostController.CompanyBasisStatement",
    ];

    /// <summary>
    /// D-002, the membership half, and the half that decides membership rather than assuming it.
    ///
    /// The delivered closure assertions decide which assemblies the rule-determined module may
    /// reach; they decide nothing about WHERE an analytics unit is declared. This one does, in
    /// both directions. Every type in the two analytics namespaces must be declared in the two
    /// named assemblies, AND every type anywhere in the production build that carries an analytics
    /// type on a declared member must itself be in those namespaces or on the permitted-carrier
    /// list. A new analytics type declared in some third namespace is therefore refused, which is
    /// the gap the earlier shape of this assertion left open: it scanned two namespaces and
    /// required nothing to be in them.
    ///
    /// Two layers, and the difference between them matters. At the COMPILER, analytics code in the
    /// rule-determined module with no new project reference cannot call a reasoning capability at
    /// all, because the types are not visible to it; a project reference to the capability boundary
    /// is stronger still and does not restore, the capability assembly already referencing the
    /// rule-determined one. At the SUITE, a used reference to the credential broker compiles
    /// cleanly and is refused here.
    /// </summary>
    [Fact]
    public void EveryAnalyticsTypeAndEveryCarrierOfOneIsDeclaredWhereTheDesignPutsIt()
    {
        var misplaced = new List<string>();

        foreach (var assembly in ProductionAssemblies)
        {
            foreach (var type in Load(assembly).GetTypes())
            {
                var name = type.FullName ?? type.Name;
                if (name.Contains('<', StringComparison.Ordinal))
                {
                    continue;
                }

                var space = type.Namespace ?? string.Empty;
                var inDomainAnalytics = space.StartsWith(DomainAnalytics, StringComparison.Ordinal);
                var inDeterministicAnalytics = space.StartsWith(DeterministicAnalytics, StringComparison.Ordinal);

                if (inDomainAnalytics && assembly != Domain)
                {
                    misplaced.Add($"{name} is declared in {assembly}, not in {Domain}");
                }

                if (inDeterministicAnalytics && assembly != Deterministic)
                {
                    misplaced.Add($"{name} is declared in {assembly}, not in {Deterministic}");
                }

                if (inDomainAnalytics || inDeterministicAnalytics || IsNested(type))
                {
                    continue;
                }

                if (CarriesAnAnalyticsType(type) && !PermittedCarriersOutsideTheAnalyticsNamespaces.Contains(name))
                {
                    misplaced.Add(
                        $"{name} carries an analytics type but is declared outside {DomainAnalytics} and "
                        + $"{DeterministicAnalytics} and is not a permitted carrier; declare it inside the "
                        + "analytics surface, or name it as a boundary the surface is reached through");
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
    /// No type on the analytics surface carries a bare numeric member or a free-form value member.
    /// The one exception is the measurement quantity union itself and its three cases, which ARE
    /// the representation: the amount lives on the observed-value case and nowhere else, so the
    /// only way to reach a number on the analytics surface is to resolve the case first.
    ///
    /// The rule reaches a numeric declared directly, a numeric reached through any generic
    /// argument or array element at any depth, and a numeric returned by a method; and it refuses
    /// any string member not declared as an authored label. The earlier shape reached only a
    /// directly declared primitive and a string whose name ended in Value, which was narrower than
    /// the property it was offered as enforcing.
    /// </summary>
    [Fact]
    public void NoAnalyticsTypeCarriesABareNumericOrAnUndeclaredStringMember()
    {
        var offenders = new List<string>();

        foreach (var (type, publicOnly) in ScannedAnalyticsTypes())
        {
            offenders.AddRange(BareValueMembers(type, publicOnly));
        }

        Assert.Empty(offenders);
    }

    /// <summary>
    /// The same rule, applied to types that deliberately break it, one per way of breaking it.
    ///
    /// Without this the check above would pass on an empty scan, or on a rule that matched
    /// nothing, and nobody would know. The probes are declared in the test assembly, so they are
    /// not on the analytics surface and the check above does not see them.
    /// </summary>
    [Fact]
    public void TheAnalyticsMemberRuleRefusesEveryWayAValueCanBeCarriedAndNamesTheOffender()
    {
        var direct = Assert.Single(BareValueMembers(typeof(ProbeCarryingABareNumericMember)));
        Assert.Contains(nameof(ProbeCarryingABareNumericMember), direct, StringComparison.Ordinal);
        Assert.Contains("Minutes", direct, StringComparison.Ordinal);
        Assert.Contains("bare Decimal", direct, StringComparison.Ordinal);

        var inCollection = Assert.Single(BareValueMembers(typeof(ProbeCarryingANumericInsideACollection)));
        Assert.Contains("Amounts", inCollection, StringComparison.Ordinal);
        Assert.Contains("bare Decimal", inCollection, StringComparison.Ordinal);

        var nested = Assert.Single(BareValueMembers(typeof(ProbeCarryingANumericInsideANestedGeneric)));
        Assert.Contains("ByPeriod", nested, StringComparison.Ordinal);
        Assert.Contains("bare Int64", nested, StringComparison.Ordinal);

        var behindMethod = Assert.Single(BareValueMembers(typeof(ProbeReturningANumericFromAMethod)));
        Assert.Contains("Total", behindMethod, StringComparison.Ordinal);
        Assert.Contains("bare Decimal", behindMethod, StringComparison.Ordinal);

        var unnamedString = Assert.Single(BareValueMembers(typeof(ProbeCarryingAnUndeclaredStringMember)));
        Assert.Contains("RenderedFigure", unnamedString, StringComparison.Ordinal);
        Assert.Contains("authored label", unnamedString, StringComparison.Ordinal);

        // And a read model built from the measurement quantity alone passes the same rule.
        Assert.Empty(BareValueMembers(typeof(ProbeCarryingOnlyAMeasurementQuantity)));
    }

    /// <summary>
    /// Every type the member rule is applied to: the two analytics namespaces and the permitted
    /// carriers, minus the measurement representation itself.
    /// </summary>
    private static IReadOnlyList<(Type Type, bool PublicOnly)> ScannedAnalyticsTypes()
    {
        var scanned = new List<(Type, bool)>();

        // On the surface itself, every declared member is scanned: a private numeric is a value a
        // later member can expose.
        foreach (var (assembly, space) in new[] { (Domain, DomainAnalytics), (Deterministic, DeterministicAnalytics) })
        {
            scanned.AddRange(AnalyticsTypes(assembly, space)
                .Where(t => !IsTheMeasurementRepresentation(t))
                .Select(t => (t, false)));
        }

        // A permitted carrier is a BOUNDARY: it turns a measurement into a datastore column or a
        // report line, so it converts internally by definition. It is held to its public surface,
        // which is what a reader can reach.
        foreach (var assembly in ProductionAssemblies)
        {
            scanned.AddRange(Load(assembly).GetTypes()
                .Where(t => PermittedCarriersOutsideTheAnalyticsNamespaces.Contains(t.FullName ?? t.Name))
                .Select(t => (t, true)));
        }

        return scanned;
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

    private static bool IsNested(Type type) => type.IsNested;

    /// <summary>Whether any declared member of a type mentions a type from the analytics namespaces.</summary>
    private static bool CarriesAnAnalyticsType(Type type) =>
        SignatureTypes(type).Any(t =>
        {
            var space = t.Namespace ?? string.Empty;
            return space.StartsWith(DomainAnalytics, StringComparison.Ordinal)
                || space.StartsWith(DeterministicAnalytics, StringComparison.Ordinal);
        });

    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>
    /// The synthesized members a record carries. They are the compiler's, not the author's, and
    /// one of them returns an integer, so scanning them would refuse every record ever written.
    /// </summary>
    private static readonly string[] SynthesizedRecordMembers =
        ["GetHashCode", "Equals", "PrintMembers", "ToString", "<Clone>$", "EqualityContract"];

    /// <summary>Every type named anywhere on a type's own declared members, flattened.</summary>
    private static IEnumerable<Type> SignatureTypes(Type type)
    {
        foreach (var property in type.GetProperties(Declared).Where(m => m.DeclaringType == type))
        {
            foreach (var flattened in Flatten(property.PropertyType))
            {
                yield return flattened;
            }
        }

        foreach (var field in type.GetFields(Declared).Where(m => m.DeclaringType == type))
        {
            foreach (var flattened in Flatten(field.FieldType))
            {
                yield return flattened;
            }
        }

        foreach (var method in type.GetMethods(Declared).Where(m => m.DeclaringType == type && !m.IsSpecialName))
        {
            if (SynthesizedRecordMembers.Contains(method.Name))
            {
                continue;
            }

            foreach (var flattened in Flatten(method.ReturnType).Concat(
                         method.GetParameters().SelectMany(p => Flatten(p.ParameterType))))
            {
                yield return flattened;
            }
        }
    }

    /// <summary>
    /// One signature type and every type reachable inside it: array elements, by-ref and pointer
    /// targets, and EVERY generic argument at EVERY depth. The earlier shape unwrapped only the
    /// first generic argument once, so a numeric behind a dictionary value or a nested collection
    /// was invisible to it.
    /// </summary>
    private static IEnumerable<Type> Flatten(Type type)
    {
        if (type.IsByRef || type.IsArray || type.IsPointer)
        {
            var element = type.GetElementType();
            if (element is not null)
            {
                foreach (var inner in Flatten(element))
                {
                    yield return inner;
                }
            }

            yield break;
        }

        var bare = Nullable.GetUnderlyingType(type) ?? type;
        yield return bare;

        if (bare.IsGenericType)
        {
            foreach (var argument in bare.GetGenericArguments().SelectMany(Flatten))
            {
                yield return argument;
            }
        }
    }

    /// <summary>
    /// Every member of one type that would let an unobserved quantity reach a reader as a number,
    /// or let record content reach one as unclassified text. The message names the type and the
    /// member, because a refusal that says only that something is wrong is one somebody disables.
    /// </summary>
    private static IReadOnlyList<string> BareValueMembers(Type type, bool publicOnly = false)
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

        var offenders = new List<string>();
        var owner = type.FullName ?? type.Name;
        var flags = publicOnly
            ? BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
            : Declared;

        void Numerics(string member, Type signature)
        {
            foreach (var reached in Flatten(signature).Where(numeric.Contains).Distinct())
            {
                offenders.Add(
                    $"{owner}.{member} reaches a bare {reached.Name}; an analytics quantity is a "
                    + "MeasurementQuantity, so that an unobserved quantity has no number to print");
            }
        }

        foreach (var property in type.GetProperties(flags).Where(m => m.DeclaringType == type))
        {
            if (SynthesizedRecordMembers.Contains(property.Name))
            {
                continue;
            }

            Numerics(property.Name, property.PropertyType);
            Strings(offenders, owner, property.Name, property.PropertyType);
        }

        foreach (var field in type.GetFields(flags).Where(m => m.DeclaringType == type))
        {
            // A compiler-emitted backing field mirrors the property that declares it, so reporting
            // both would name one offence twice.
            if (field.Name.Contains('<', StringComparison.Ordinal))
            {
                continue;
            }

            Numerics(field.Name, field.FieldType);
            Strings(offenders, owner, field.Name, field.FieldType);
        }

        foreach (var method in type.GetMethods(flags)
                     .Where(m => m.DeclaringType == type && !m.IsSpecialName)
                     .Where(m => !SynthesizedRecordMembers.Contains(m.Name)))
        {
            // The return type only. A numeric PARAMETER is an input a composer is given, not a
            // value a reader can reach, and refusing one would refuse every composer that counts.
            Numerics(method.Name + "()", method.ReturnType);
        }

        return offenders;
    }

    /// <summary>
    /// A string member is refused unless it is declared as an authored label. A name-suffix rule
    /// decides nothing here: the delivered defect was a member called Value, and the next one is
    /// called something else.
    /// </summary>
    private static void Strings(List<string> offenders, string owner, string member, Type signature)
    {
        if (!Flatten(signature).Contains(typeof(string)))
        {
            return;
        }

        if (PermittedAuthoredLabels.Contains($"{owner}.{member}"))
        {
            return;
        }

        offenders.Add(
            $"{owner}.{member} is a string member that is not declared as an authored label; a value slot "
            + "lets record content reach a reader unclassified, so the member is refused until it is named");
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
        public IDossierWriter Dossiers { get; } = new NoDossiers();
        public IAdmissionLedger Admission { get; } = new NoAdmission();
        public IBenchmarkWriter Benchmarks { get; } = new NoBenchmarks();

        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class Writer(TerminalStateProbe owner) : IJobWriter
    {
        public Task<DateTimeOffset> EnqueueAsync(Job job, TimeSpan claimableAfter, CancellationToken ct) =>
            Task.FromResult(ProbeClock.Instant);

        public Task RecordStageAsync(JobStage stage, CancellationToken ct) => Task.CompletedTask;

        public Task<Job?> ClaimNextAsync(string workerId, TimeSpan lease, CancellationToken ct) =>
            Task.FromResult<Job?>(null);

        public Task<Job?> ClaimAsync(JobId job, string workerId, TimeSpan lease, CancellationToken ct) =>
            Task.FromResult<Job?>(null);

        public Task<DateTimeOffset> ReleaseAsync(JobId job, ClaimState state, TimeSpan claimableAfter, CancellationToken ct) =>
            Task.FromResult(ProbeClock.Instant + claimableAfter);

        public Task<DateTimeOffset> AdvanceAsync(
            JobId job, LifecyclePosition position, ClaimState state,
            TimeSpan claimableAfter, CancellationToken ct)
        {
            owner.Advances.Add((position, state));
            return Task.FromResult(ProbeClock.Instant + claimableAfter);
        }
    }

    /// <summary>The engine admits no capability request; reaching the admission ledger fails the probe.</summary>
    private sealed class NoAdmission : IAdmissionLedger
    {
        public Task<BookingReservation> ReserveAsync(CancellationToken ct) => throw new NotSupportedException();

        public Task<IReadOnlyList<MediaCompany.Domain.Registry.ModelPrice>> PricesInForceAsync(
            IReadOnlyCollection<ModelId> models, CancellationToken ct) => throw new NotSupportedException();

        public Task<GoverningReadingsSummary> GoverningReadingsAsync(
            MediaCompany.Domain.Capabilities.Attribution attribution, MediaCompany.Domain.Accounting.Money allotment, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MediaCompany.Domain.Analytics.BenchmarkObservation>> EvidenceAsync(
            IReadOnlyCollection<RouteId> routes, MediaCompany.Domain.Capabilities.TaskClass taskClass,
            CancellationToken ct) => throw new NotSupportedException();

        public Task RecordDecisionAsync(AdmissionDecisionDraft decision, CancellationToken ct) => throw new NotSupportedException();
    }

    /// <summary>The engine records no observation; reaching the benchmark writer fails the probe.</summary>
    private sealed class NoBenchmarks : IBenchmarkWriter
    {
        public Task<DateTimeOffset> RegisterEntryAsync(
            CorpusEntryId entry, MediaCompany.Domain.Capabilities.TaskClass taskClass, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<MediaCompany.Domain.Analytics.BenchmarkObservation> RecordObservationAsync(
            BenchmarkObservationDraft draft, CancellationToken ct) => throw new NotSupportedException();
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

    private sealed class NoDossiers : IDossierWriter
    {
        public Task OpenAsync(MediaCompany.Domain.ItemId item, MediaCompany.Domain.ItemVersion version,
            DateTimeOffset openedAt, CancellationToken ct) =>
            throw new NotSupportedException("the lifecycle engine opens no dossier");

        public Task RecordStageAsync(MediaCompany.Domain.ItemId item, MediaCompany.Domain.ItemVersion version,
            MediaCompany.Domain.Dossier.StageEvidence evidence, CancellationToken ct) =>
            throw new NotSupportedException("the lifecycle engine records no stage evidence");

        public Task RecordSupplyAuditAsync(MediaCompany.Domain.ItemId item, MediaCompany.Domain.ItemVersion version,
            MediaCompany.Domain.Dossier.SupplyAuditEntry entry, CancellationToken ct) =>
            throw new NotSupportedException("the lifecycle engine records no supply audit");

        public Task RecordDeterminationAsync(MediaCompany.Domain.ItemId item, MediaCompany.Domain.ItemVersion version,
            MediaCompany.Domain.Dossier.DeterminationResolution resolution, CancellationToken ct) =>
            throw new NotSupportedException("the lifecycle engine records no determination");

        public Task RecordComponentAsync(MediaCompany.Domain.ItemId item, MediaCompany.Domain.ItemVersion version,
            DossierComponent component, DateTimeOffset recordedAt, CancellationToken ct) =>
            throw new NotSupportedException("the lifecycle engine records no dossier component");
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

/// <summary>Nor like this: the number is one level down, inside a collection.</summary>
internal sealed record ProbeCarryingANumericInsideACollection(IReadOnlyList<decimal> Amounts);

/// <summary>Nor like this: the number is two levels down, inside a nested collection.</summary>
internal sealed record ProbeCarryingANumericInsideANestedGeneric(
    IReadOnlyList<IReadOnlyList<long>> ByPeriod);

/// <summary>Nor like this: the number is behind a method rather than on a member.</summary>
internal sealed class ProbeReturningANumericFromAMethod
{
    public decimal Total() => 0m;
}

/// <summary>
/// Nor like this: a free-form string slot named anything at all. The delivered defect was a
/// member called Value; a rule that looked for that name would pass this one.
/// </summary>
internal sealed record ProbeCarryingAnUndeclaredStringMember(string RenderedFigure);

/// <summary>An analytics read model as it must be written.</summary>
internal sealed record ProbeCarryingOnlyAMeasurementQuantity(
    MediaCompany.Domain.Analytics.MeasurementQuantity Review);

/// <summary>A type that reads a channel key outside the profile service, as the single-reader rule must find.</summary>
internal static class ProbeReadingAChannelKey
{
    public static string Schedule() => ChannelConfigurationKeys.Schedule;
}
