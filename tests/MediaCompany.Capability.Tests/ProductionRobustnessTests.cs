using System.Net;
using System.Net.Sockets;
using System.Text;
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
/// The vendor call's robustness (corrections CR-003, CR-006 and CR-009): a call that never answers is cut by the REAL
/// client timeout, against a loopback listener that accepts and never replies; a redirect is never followed; and an
/// operator's cancellation during the call books the attempt at its worst case. Every listener here is on the loopback
/// address and opened by the test itself; no request leaves this machine, and the secret is a fake value the test sets.
/// </summary>
public sealed class ProductionRobustnessTests
{
    private static readonly ProviderAccountId Account = new("speech-robustness-fixture");

    /// <summary>
    /// A STALLED CALL: the loopback listener accepts the connection and never answers. The vendor client the metered
    /// composition builds, bounded at one second here, cuts the call at its own timeout; the attempt is failed, its
    /// charge not known, and never a zero measurement.
    /// </summary>
    [Fact]
    public async Task AStalledCallIsCutByTheRealClientTimeoutAndItsChargeIsNotKnown()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var held = new List<TcpClient>();
        var accepting = Task.Run(async () =>
        {
            while (true)
            {
                held.Add(await listener.AcceptTcpClientAsync());
            }
        });

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var client = CapabilityGatewayFactory.VendorClient(TimeSpan.FromSeconds(1));
        var (adapter, handle) = await SpeechAsync(new Uri($"http://127.0.0.1:{port}/"), client);
        var clock = System.Diagnostics.Stopwatch.StartNew();

        var attempt = await adapter.InvokeAsync(new RouteTarget.ProviderRoute(Account, new ModelId("tts-fixture")), Narration("a stalled line"), handle, CancellationToken.None);

        clock.Stop();
        Assert.False(attempt.Succeeded);
        Assert.False(attempt.ChargeKnown);
        Assert.NotEqual(CostBasis.Measurement, attempt.CostBasis);
        Assert.Contains("the provider-call bound passed", attempt.FailureReason, StringComparison.Ordinal);
        Assert.InRange(clock.Elapsed, TimeSpan.FromSeconds(0.9), TimeSpan.FromSeconds(30));
        Assert.NotEmpty(held);
        listener.Stop();
        held.ForEach(c => c.Dispose());
    }

    /// <summary>
    /// A REDIRECT IS NEVER FOLLOWED (correction CR-006): the loopback listener answers a redirect to a second loopback
    /// listener. The vendor client does not follow it, so the second listener is never reached and the request with its
    /// credential goes nowhere else; the attempt is a non-success status, its charge not known.
    /// </summary>
    [Fact]
    public async Task ARedirectIsNeverFollowed()
    {
        using var elsewhere = new TcpListener(IPAddress.Loopback, 0);
        elsewhere.Start();
        var reachedElsewhere = 0;
        var watching = Task.Run(async () =>
        {
            using var reached = await elsewhere.AcceptTcpClientAsync();
            Interlocked.Increment(ref reachedElsewhere);
        });
        var elsewherePort = ((IPEndPoint)elsewhere.LocalEndpoint).Port;

        using var redirecting = new TcpListener(IPAddress.Loopback, 0);
        redirecting.Start();
        var answering = Task.Run(async () =>
        {
            using var connection = await redirecting.AcceptTcpClientAsync();
            var stream = connection.GetStream();
            var buffer = new byte[8192];
            var read = new StringBuilder();
            while (!read.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
            {
                var count = await stream.ReadAsync(buffer);
                if (count == 0)
                {
                    break;
                }

                read.Append(Encoding.ASCII.GetString(buffer, 0, count));
            }

            var answer = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 307 Temporary Redirect\r\nLocation: http://127.0.0.1:{elsewherePort}/elsewhere\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(answer);
            await stream.FlushAsync();
        });

        var port = ((IPEndPoint)redirecting.LocalEndpoint).Port;
        using var client = CapabilityGatewayFactory.VendorClient(TimeSpan.FromSeconds(10));
        var (adapter, handle) = await SpeechAsync(new Uri($"http://127.0.0.1:{port}/"), client);

        var attempt = await adapter.InvokeAsync(new RouteTarget.ProviderRoute(Account, new ModelId("tts-fixture")), Narration("a redirected line"), handle, CancellationToken.None);
        await answering.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(200);

        Assert.False(attempt.Succeeded);
        Assert.False(attempt.ChargeKnown);
        Assert.Contains("status 307", attempt.FailureReason, StringComparison.Ordinal);
        Assert.Equal(0, Volatile.Read(ref reachedElsewhere));
        elsewhere.Stop();
        redirecting.Stop();
    }

    /// <summary>
    /// AN OPERATOR'S CANCELLATION DURING THE VENDOR CALL (correction CR-003): the attempt is not dropped. It is booked
    /// as a failed attempt at the admitted worst case, labelled estimate, under the reservation committed before the
    /// call, and the outcome is returned to the caller, which ends its own stage.
    /// </summary>
    [Fact]
    public async Task ACancellationDuringTheVendorCallIsBookedAtTheWorstCase()
    {
        var work = ProductionHarness.Work(Account, "tts", CapabilityClass.Narration, out _, cap: 5.95m);
        var adapter = new WaitingAdapter(Account);
        using var cancellation = new CancellationTokenSource();
        var text = new string('a', 1_200);

        var running = ProductionHarness.Gateway(work, adapter).ExecuteAsync(ProductionHarness.NarrationRequest(text), ProductionHarness.Context(), cancellation.Token);
        await adapter.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await cancellation.CancelAsync();
        var outcome = await running.WaitAsync(TimeSpan.FromSeconds(30));

        var booked = Assert.IsType<CapabilityOutcome.Completed>(outcome);
        Assert.Equal(OperationOutcome.Failed, booked.Operation.Outcome);
        Assert.Equal(CostBasis.Estimate, booked.Operation.CostBasis);
        Assert.Equal(1_200, booked.Operation.Units.CharacterUnits);
        Assert.Contains("cancelled by the operator during the vendor call", booked.Operation.FailureReason, StringComparison.Ordinal);
        Assert.Null(booked.Content);
        Assert.Single(work.Admission.CommittedReservations);
        Assert.Single(work.Operations);
    }

    private static async Task<(SpeechAudioAdapter Adapter, ScopedHandle Handle)> SpeechAsync(Uri endpoint, HttpClient client)
    {
        var secrets = new FakeSecretStore();
        secrets.Publish(new CredentialHolderKey(Account, null), VendorPathTests.FakeSecret);
        var broker = new CredentialBroker(secrets, () => DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        var issued = await broker.IssueAsync(
            new CredentialRequest
            {
                CredentialClass = CredentialClass.ProviderAccess,
                ProviderAccount = Account,
                Job = JobId.New(),
                Stage = LifecyclePosition.Assembly,
                Actor = WorkforceRole.Producer,
                Channel = null,
                Scheme = AuthenticationScheme.BearerAuthorization,
            },
            CancellationToken.None);
        return (new SpeechAudioAdapter(Account, endpoint, client, broker), Assert.IsType<CredentialOutcome.Issued>(issued).Handle);
    }

    private static CapabilityRequest Narration(string text) => new(
        CapabilityClass.Narration, ReasoningTier.Light, new QualityRating(0), new ContextCapacity(0), new Money(1m), Criticality.Routine,
        new Attribution(ItemId.New(), ChannelId.New(), DepartmentId.New(), AgentId.New()), EstimatedUnits.None, TimeSpan.Zero,
        ReducedFloorPolicy.Forbidden)
    {
        Payload = new CapabilityPayload.Narration(text, "fixture-voice", "wav"),
    };

    /// <summary>A stand-in vendor that never answers: it waits until the caller's token is cancelled.</summary>
    private sealed class WaitingAdapter(ProviderAccountId account) : IProviderAdapter
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ProviderAccountId ProviderAccount { get; } = account;

        public async Task<ProviderAttempt> InvokeAsync(RouteTarget.ProviderRoute route, CapabilityRequest request, ScopedHandle handle, CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("unreachable: the wait ends only by cancellation");
        }
    }
}
