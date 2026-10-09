using System.Net;
using System.Text;
using System.Text.Json;
using MediaCompany.Capability.Providers;
using MediaCompany.Credentials;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Capability.Tests;

/// <summary>
/// The three vendor paths and their fakes (the production change, decisions D-002 and D-004 of its design), proven
/// against the dated fixtures the implementing role recorded and a STUB HANDLER that answers every request itself:
/// no test here reaches a network, and the secret every test uses is a fake value the test sets.
/// </summary>
public sealed class VendorPathTests
{
    /// <summary>A fake secret: never a real credential, and searched for in every output.</summary>
    internal const string FakeSecret = "fake-test-secret-0f4c2a9e-never-a-credential";

    private static readonly ProviderAccountId SpeechAccount = new("speech-vendor-fixture");
    private static readonly ProviderAccountId ReasoningAccount = new("reasoning-vendor-fixture");
    private static readonly Uri Endpoint = new("https://vendor.stub.invalid/");

    // -----------------------------------------------------------------------
    // The speech path
    // -----------------------------------------------------------------------

    /// <summary>The request matches the fixture: path, bearer scheme, the four body members, the exact text.</summary>
    [Fact]
    public async Task TheSpeechRequestMatchesTheDatedFixture()
    {
        var fixture = Fixture("speech");
        var stub = new StubHandler(_ => Audio());
        var (adapter, handle) = await SpeechAsync(stub);

        var attempt = await adapter.InvokeAsync(Route(SpeechAccount, "tts-fixture"), Narration("The exact text.\n\n"), handle, CancellationToken.None);

        Assert.True(attempt.Succeeded);
        var sent = Assert.Single(stub.Requests);
        Assert.Equal(fixture.GetProperty("request").GetProperty("method").GetString(), sent.Method);
        Assert.Equal(fixture.GetProperty("request").GetProperty("path").GetString(), sent.Path);
        Assert.Equal($"Bearer {FakeSecret}", sent.Authorization);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.Equal(
            fixture.GetProperty("request").GetProperty("bodyMembers").EnumerateArray().Select(m => m.GetString()).Order(),
            body.RootElement.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal("The exact text.\n\n", body.RootElement.GetProperty("input").GetString());
        Assert.Equal("2026-10-09", fixture.GetProperty("recordedOn").GetString());
    }

    /// <summary>
    /// The speech response states no usage, so the charge is UNKNOWN: the attempt carries the audio and no units,
    /// and the boundary books it at the admitted worst case; no header is read for usage.
    /// </summary>
    [Fact]
    public async Task ASpeechSuccessCarriesItsAudioAndAnUnknownChargeAndReadsNoUsageFromHeaders()
    {
        var stub = new StubHandler(_ =>
        {
            var response = Audio();
            response.Headers.Add("x-usage-input-units", "999");
            return response;
        });
        var (adapter, handle) = await SpeechAsync(stub);

        var attempt = await adapter.InvokeAsync(Route(SpeechAccount, "tts-fixture"), Narration("text"), handle, CancellationToken.None);

        Assert.True(attempt.Succeeded);
        Assert.False(attempt.ChargeKnown);
        Assert.Equal(UnitCounts.None, attempt.Units);
        Assert.Equal(AudioBytes, attempt.Content!.Bytes.ToArray());
    }

    /// <summary>
    /// A TIMEOUT, a TRANSPORT FAILURE and a NON-SUCCESS STATUS each leave the charge unknown: none is recorded as a
    /// zero measurement; each carries no content.
    /// </summary>
    [Theory]
    [InlineData("timeout")]
    [InlineData("transport")]
    [InlineData("status")]
    public async Task AFailedSpeechCallLeavesItsChargeUnknownAndIsNeverAZeroMeasurement(string failure)
    {
        var stub = new StubHandler(_ => failure switch
        {
            "timeout" => throw new TaskCanceledException("the client's timeout"),
            "transport" => throw new HttpRequestException(HttpRequestError.ConnectionError, "the connection dropped"),
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
        });
        var (adapter, handle) = await SpeechAsync(stub);

        var attempt = await adapter.InvokeAsync(Route(SpeechAccount, "tts-fixture"), Narration("text"), handle, CancellationToken.None);

        Assert.False(attempt.Succeeded);
        Assert.False(attempt.ChargeKnown);
        Assert.NotEqual(CostBasis.Measurement, attempt.CostBasis);
        Assert.Null(attempt.Content);
        Assert.Contains("the charge is not known", attempt.FailureReason, StringComparison.Ordinal);
    }

    /// <summary>A credential refusal before sending sends nothing, so its charge is known to be none.</summary>
    [Fact]
    public async Task ACredentialRefusedBeforeSendingSendsNothingAndKnowsItsChargeIsNone()
    {
        var stub = new StubHandler(_ => Audio());
        var (adapter, handle) = await SpeechAsync(stub);
        await adapter.InvokeAsync(Route(SpeechAccount, "tts-fixture"), Narration("first"), handle, CancellationToken.None);

        // The handle's one use is spent: the second presentation is refused and nothing is sent.
        var attempt = await adapter.InvokeAsync(Route(SpeechAccount, "tts-fixture"), Narration("second"), handle, CancellationToken.None);

        Assert.False(attempt.Succeeded);
        Assert.True(attempt.ChargeKnown);
        Assert.Single(stub.Requests);
    }

    // -----------------------------------------------------------------------
    // The messages and image paths
    // -----------------------------------------------------------------------

    /// <summary>
    /// The messages request matches its fixture — path, the vendor's KEY HEADER carrying the secret, the non-secret
    /// version header, the body — and the billed units are read from the response BODY.
    /// </summary>
    [Fact]
    public async Task TheMessagesPathMatchesItsFixtureAndReadsUsageFromTheBody()
    {
        var fixture = Fixture("messages");
        var stub = new StubHandler(_ => Json(fixture.GetProperty("response").GetProperty("body").GetRawText()));
        var adapter = new MessagesAdapter(ReasoningAccount, Endpoint, new HttpClient(stub), Broker(out var broker));
        var handle = await IssueAsync(broker, ReasoningAccount, AuthenticationScheme.KeyHeader, adapter.KeyHeaderName);

        var attempt = await adapter.InvokeAsync(
            Route(ReasoningAccount, "reasoning-fixture"), Request(CapabilityClass.EditorialReasoning, new CapabilityPayload.Messages("a prompt", 64)), handle, CancellationToken.None);

        var sent = Assert.Single(stub.Requests);
        Assert.Equal(fixture.GetProperty("request").GetProperty("path").GetString(), sent.Path);
        Assert.Equal(FakeSecret, sent.Headers["x-api-key"]);
        Assert.Equal("2023-06-01", sent.Headers["anthropic-version"]);
        Assert.Null(sent.Authorization);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.Equal(
            fixture.GetProperty("request").GetProperty("bodyMembers").EnumerateArray().Select(m => m.GetString()).Order(),
            body.RootElement.EnumerateObject().Select(p => p.Name).Order());

        Assert.True(attempt.ChargeKnown);
        Assert.Equal(CostBasis.Measurement, attempt.CostBasis);
        Assert.Equal(new UnitCounts(12, 7, 0, 0), attempt.Units);
        Assert.Equal("fixture reply", Encoding.UTF8.GetString(attempt.Content!.Bytes.Span));
    }

    /// <summary>The image path decodes the image, hashes the decoded bytes, and reads the units from the body.</summary>
    [Fact]
    public async Task TheImagePathMatchesItsFixtureDecodesTheImageAndReadsUsageFromTheBody()
    {
        var fixture = Fixture("image");
        var png = FakeImageGeneration.Png("fixture");
        var stub = new StubHandler(_ => Json($$$"""{"created":1760000000,"data":[{"b64_json":"{{{Convert.ToBase64String(png)}}}"}],"usage":{"input_tokens":20,"output_tokens":4160}}"""));
        var adapter = new ImageGenerationAdapter(SpeechAccount, Endpoint, new HttpClient(stub), Broker(out var broker));
        var handle = await IssueAsync(broker, SpeechAccount, AuthenticationScheme.BearerAuthorization, null);

        var attempt = await adapter.InvokeAsync(
            Route(SpeechAccount, "image-fixture"), Request(CapabilityClass.StillImages, new CapabilityPayload.StillImage("a prompt", "1024x1024")), handle, CancellationToken.None);

        var sent = Assert.Single(stub.Requests);
        Assert.Equal(fixture.GetProperty("request").GetProperty("path").GetString(), sent.Path);
        Assert.Equal($"Bearer {FakeSecret}", sent.Authorization);
        Assert.Equal(png, attempt.Content!.Bytes.ToArray());
        Assert.Equal(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(png)), attempt.Content.Sha256);
        Assert.Equal(new UnitCounts(20, 4160, 0, 0) { ImageUnits = 1 }, attempt.Units);
        Assert.True(attempt.ChargeKnown);
    }

    /// <summary>A messages or image body WITHOUT usage leaves the charge unknown, never a zero measurement.</summary>
    [Fact]
    public async Task AUsageLessBodyLeavesTheChargeUnknown()
    {
        var stub = new StubHandler(_ => Json("""{"content":[{"type":"text","text":"no usage"}]}"""));
        var adapter = new MessagesAdapter(ReasoningAccount, Endpoint, new HttpClient(stub), Broker(out var broker));
        var handle = await IssueAsync(broker, ReasoningAccount, AuthenticationScheme.KeyHeader, adapter.KeyHeaderName);

        var attempt = await adapter.InvokeAsync(
            Route(ReasoningAccount, "reasoning-fixture"), Request(CapabilityClass.EditorialReasoning, new CapabilityPayload.Messages("p", 8)), handle, CancellationToken.None);

        Assert.True(attempt.Succeeded);
        Assert.False(attempt.ChargeKnown);
        Assert.Equal(CostBasis.Estimate, attempt.CostBasis);
    }

    // -----------------------------------------------------------------------
    // The contract suite: every vendor path and every fake
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE CONTRACT every path keeps, vendor and fake alike: a completed attempt's content length and hash are those
    /// of its bytes; the same input gives the same content; a request carrying another path's payload sends nothing
    /// and knows its charge is none; the payload's text never reaches a content description.
    /// </summary>
    [Theory]
    [InlineData("speech-vendor")]
    [InlineData("speech-fake")]
    [InlineData("messages-vendor")]
    [InlineData("messages-fake")]
    [InlineData("image-vendor")]
    [InlineData("image-fake")]
    public async Task EveryPathKeepsTheContract(string path)
    {
        var (adapter, request, wrong, issue) = Path(path);

        var first = await adapter.InvokeAsync(Route(adapter.ProviderAccount, "contract-model"), request, await issue(), CancellationToken.None);
        var second = await adapter.InvokeAsync(Route(adapter.ProviderAccount, "contract-model"), request, await issue(), CancellationToken.None);
        var refused = await adapter.InvokeAsync(Route(adapter.ProviderAccount, "contract-model"), wrong, await issue(), CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.Equal(first.Content!.Bytes.Length, first.Content.Length);
        Assert.Equal(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(first.Content.Bytes.Span)), first.Content.Sha256);
        Assert.Equal(first.Content.Sha256, second.Content!.Sha256);
        Assert.False(refused.Succeeded);
        Assert.True(refused.ChargeKnown);
        Assert.Null(refused.Content);
        Assert.DoesNotContain("contract input", first.Content.ToString(), StringComparison.Ordinal);
    }

    /// <summary>The fakes report explicit zero units MEASURED, so their operation costs zero and is stated.</summary>
    [Fact]
    public async Task AFakeReportsExplicitZeroUnitsMeasured()
    {
        var fake = new FakeSpeechAudio(SpeechAccount);
        var attempt = await fake.InvokeAsync(Route(SpeechAccount, "tts"), Narration("abc"), Handle(SpeechAccount), CancellationToken.None);

        Assert.True(attempt.ChargeKnown);
        Assert.Equal(CostBasis.Measurement, attempt.CostBasis);
        Assert.Equal(UnitCounts.None, attempt.Units);
        Assert.Equal(44 + (3 * FakeSpeechAudio.SamplesPerCharacter * 2), attempt.Content!.Length);
    }

    // -----------------------------------------------------------------------
    // The demonstration composition
    // -----------------------------------------------------------------------

    /// <summary>
    /// The demonstration composition reaches NO NETWORK and reads NO CREDENTIAL VARIABLE: with the speech account's
    /// variable SET TO A FAKE VALUE by this test, a fake narration completes, the boundary holds no network client
    /// anywhere in its adapters, and the fake value appears in no operation, audit entry or content description.
    /// </summary>
    [Fact]
    public async Task TheDemonstrationCompositionReachesNoNetworkAndReadsNoCredentialVariable()
    {
        var variable = CredentialBrokerFactory.VariableName(SpeechAccount, null);
        var before = Environment.GetEnvironmentVariable(variable);
        Environment.SetEnvironmentVariable(variable, FakeSecret);
        try
        {
            var work = ProductionHarness.Work(SpeechAccount, "tts-fixture", CapabilityClass.Narration, out _);
            var gateway = CapabilityGatewayFactory.CreateDemonstration(work, [new DemonstrationProvider(SpeechAccount, VendorContract.SpeechAudio)], () => DateTimeOffset.UtcNow);

            var outcome = Assert.IsType<CapabilityOutcome.Completed>(await gateway.ExecuteAsync(
                ProductionHarness.NarrationRequest("the demonstration text"), ProductionHarness.Context(), CancellationToken.None));

            Assert.NotNull(outcome.Content);
            Assert.Equal(OperationOutcome.Succeeded, outcome.Operation.Outcome);

            var adapters = typeof(CapabilityGateway).GetField("_adapters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(gateway);
            var held = ((IEnumerable<IProviderAdapter>)adapters!).ToArray();
            Assert.All(held, a => Assert.StartsWith("Fake", a.GetType().Name, StringComparison.Ordinal));
            Assert.All(held, a => Assert.DoesNotContain(
                a.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
                f => typeof(HttpClient).IsAssignableFrom(f.FieldType)));

            var everything = string.Join("\n", work.Operations.Select(o => o.ToString()).Concat(work.AuditEntries.Select(e => e.ToString())).Append(outcome.Content!.ToString()));
            Assert.DoesNotContain(FakeSecret, everything, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, before);
        }
    }

    /// <summary>
    /// THE SECRET IS NEVER EXPOSED: through the boundary over the vendor path, a fake secret appears in no
    /// operation record, no audit entry, no decision, no failure reason and no exception message, on success and on
    /// failure alike; it exists only on the outgoing message.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AFakeSecretAppearsInNoOutputOfTheVendorPath(bool succeeds)
    {
        var stub = new StubHandler(_ => succeeds ? Audio() : throw new HttpRequestException(HttpRequestError.ConnectionError, "dropped"));
        var work = ProductionHarness.Work(SpeechAccount, "tts-fixture", CapabilityClass.Narration, out var registers);
        registers.Accounts[0] = registers.Accounts[0] with { Scope = CredentialScope.Company };
        var secrets = new FakeSecretStore();
        secrets.Publish(new CredentialHolderKey(SpeechAccount, null), FakeSecret);
        var broker = new CredentialBroker(secrets, () => DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        var gateway = new CapabilityGateway(work, broker, [new SpeechAudioAdapter(SpeechAccount, Endpoint, new HttpClient(stub), broker)], TimeSpan.FromSeconds(60));

        var outcome = await gateway.ExecuteAsync(ProductionHarness.NarrationRequest("secret-free text"), ProductionHarness.Context(), CancellationToken.None);

        Assert.Contains(FakeSecret, Assert.Single(stub.Requests).Authorization!, StringComparison.Ordinal);
        var everything = string.Join("\n",
            work.Operations.Select(o => $"{o} {o.FailureReason}")
                .Concat(work.AuditEntries.Select(e => $"{e} {e.Reason} {e.Decision}"))
                .Concat(work.Decisions.Select(d => $"{d} {d.TierStatement} {d.ReservationStatement}"))
                .Concat(broker.AuditTrail.Select(a => a.ToString()))
                .Append(outcome.ToString()));
        Assert.DoesNotContain(FakeSecret, everything, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static readonly byte[] AudioBytes = FakeSpeechAudio.Wave("fixture audio");

    private static HttpResponseMessage Audio()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(AudioBytes) };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        return response;
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static JsonElement Fixture(string name) =>
        JsonDocument.Parse(File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".json"))).RootElement;

    private static CredentialBroker Broker(out CredentialBroker broker)
    {
        var secrets = new FakeSecretStore();
        secrets.Publish(new CredentialHolderKey(SpeechAccount, null), FakeSecret);
        secrets.Publish(new CredentialHolderKey(ReasoningAccount, null), FakeSecret);
        broker = new CredentialBroker(secrets, () => DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        return broker;
    }

    private static async Task<(SpeechAudioAdapter Adapter, ScopedHandle Handle)> SpeechAsync(StubHandler stub)
    {
        var adapter = new SpeechAudioAdapter(SpeechAccount, Endpoint, new HttpClient(stub), Broker(out var broker));
        return (adapter, await IssueAsync(broker, SpeechAccount, AuthenticationScheme.BearerAuthorization, null));
    }

    private static async Task<ScopedHandle> IssueAsync(CredentialBroker broker, ProviderAccountId account, AuthenticationScheme scheme, string? header)
    {
        var issued = await broker.IssueAsync(
            new CredentialRequest
            {
                CredentialClass = CredentialClass.ProviderAccess,
                ProviderAccount = account,
                Job = JobId.New(),
                Stage = LifecyclePosition.Assembly,
                Actor = WorkforceRole.Producer,
                Channel = null,
                Scheme = scheme,
                KeyHeaderName = header,
            },
            CancellationToken.None);
        return Assert.IsType<CredentialOutcome.Issued>(issued).Handle;
    }

    private static ScopedHandle Handle(ProviderAccountId account) =>
        new(Guid.NewGuid(), CredentialClass.ProviderAccess, account, null, null, JobId.New(), LifecyclePosition.Assembly,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), 1);

    private static RouteTarget.ProviderRoute Route(ProviderAccountId account, string model) => new(account, new ModelId(model));

    private static CapabilityRequest Narration(string text) =>
        Request(CapabilityClass.Narration, new CapabilityPayload.Narration(text, "fixture-voice", "wav"));

    private static CapabilityRequest Request(CapabilityClass capability, CapabilityPayload payload) => new(
        capability, ReasoningTier.Light, new QualityRating(0), new ContextCapacity(0), new Money(1m), Criticality.Routine,
        new Attribution(ItemId.New(), ChannelId.New(), DepartmentId.New(), AgentId.New()), EstimatedUnits.None, TimeSpan.Zero,
        ReducedFloorPolicy.Forbidden)
    {
        Payload = payload,
    };

    private static (IProviderAdapter Adapter, CapabilityRequest Request, CapabilityRequest Wrong, Func<Task<ScopedHandle>> Issue) Path(string path)
    {
        var broker = Broker(out _);
        var speech = Narration("contract input");
        var messages = Request(CapabilityClass.EditorialReasoning, new CapabilityPayload.Messages("contract input", 16));
        var image = Request(CapabilityClass.StillImages, new CapabilityPayload.StillImage("contract input", "1024x1024"));
        Func<Task<ScopedHandle>> fakeHandle = () => Task.FromResult(Handle(SpeechAccount));

        return path switch
        {
            "speech-vendor" => (new SpeechAudioAdapter(SpeechAccount, Endpoint, new HttpClient(new StubHandler(_ => Audio())), broker), speech, messages,
                () => IssueAsync(broker, SpeechAccount, AuthenticationScheme.BearerAuthorization, null)),
            "speech-fake" => (new FakeSpeechAudio(SpeechAccount), speech, messages, fakeHandle),
            "messages-vendor" => (new MessagesAdapter(ReasoningAccount, Endpoint, new HttpClient(new StubHandler(_ => Json(
                    """{"content":[{"type":"text","text":"contract reply"}],"usage":{"input_tokens":3,"output_tokens":2}}"""))), broker), messages, speech,
                () => IssueAsync(broker, ReasoningAccount, AuthenticationScheme.KeyHeader, MessagesAdapter.KeyHeader)),
            "messages-fake" => (new FakeMessages(ReasoningAccount), messages, speech, fakeHandle),
            "image-vendor" => (new ImageGenerationAdapter(SpeechAccount, Endpoint, new HttpClient(new StubHandler(_ => Json(
                    $$$"""{"data":[{"b64_json":"{{{Convert.ToBase64String(FakeImageGeneration.Png("x"))}}}"}],"usage":{"input_tokens":1,"output_tokens":2}}"""))), broker), image, speech,
                () => IssueAsync(broker, SpeechAccount, AuthenticationScheme.BearerAuthorization, null)),
            "image-fake" => (new FakeImageGeneration(SpeechAccount), image, speech, fakeHandle),
            _ => throw new ArgumentOutOfRangeException(nameof(path)),
        };
    }
}

/// <summary>
/// A STUB HANDLER: it answers every request itself from the function it is given and records what was sent, so no
/// request leaves the process. It is a test double for the network, named as one.
/// </summary>
internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
{
    public List<SentRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new SentRequest(
            request.Method.Method,
            request.RequestUri!.AbsolutePath,
            request.Headers.Authorization?.ToString(),
            request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase),
            body));
        return answer(request);
    }
}

internal sealed record SentRequest(string Method, string Path, string? Authorization, IReadOnlyDictionary<string, string> Headers, string Body);
