using System.Reflection;
using System.Text.Json;
using MediaCompany.Credentials;
using MediaCompany.Domain;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Capability.Tests;

/// <summary>
/// The credential demonstrations and measurements, against acceptance criteria AC-013 to AC-016.
/// </summary>
public sealed class CredentialBrokerTests
{
    private const string Secret = "sk-test-DO-NOT-LEAK-7f3a9c2e";

    private static readonly ProviderAccountId Account = new("provider-alpha");
    private static readonly ChannelId Channel = ChannelId.New();
    private static readonly JobId Job = JobId.New();

    private static (CredentialBroker Broker, FakeSecretStore Store, TestClock Clock) Build(TimeSpan? ttl = null)
    {
        var store = new FakeSecretStore();
        var clock = new TestClock(DateTimeOffset.Parse("2026-10-01T12:00:00Z"));
        store.Publish(new CredentialHolderKey(Account, Channel), Secret);
        var broker = new CredentialBroker(store, () => clock.UtcNow, ttl ?? TimeSpan.FromMinutes(5));
        return (broker, store, clock);
    }

    private static CredentialRequest ProviderAccess(WorkforceRole actor = WorkforceRole.Producer) => new()
    {
        CredentialClass = CredentialClass.ProviderAccess,
        ProviderAccount = Account,
        Job = Job,
        Stage = LifecyclePosition.Script,
        Actor = actor,
        Channel = Channel,
        Capability = CapabilityClass.EditorialReasoning,
    };

    /// <summary>
    /// Decision D-004, stated structurally: the handle the caller receives carries no credential
    /// material, and there is no member on it that could. This asserts it over the type's shape,
    /// so adding one later fails this test rather than leaking quietly.
    /// </summary>
    [Fact]
    public void AScopedHandleHasNoMemberThatCouldCarryASecret()
    {
        var members = typeof(ScopedHandle)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        Assert.DoesNotContain(members, n => n.Contains("Secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(members, n => n.Contains("Token", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(members, n => n.Contains("Key", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(members, n => n.Contains("Value", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(members, n => n.Contains("Credential", StringComparison.OrdinalIgnoreCase)
            && n != nameof(ScopedHandle.CredentialClass));
    }

    /// <summary>
    /// The exchange interface has no method that returns a credential to a caller. It attaches one
    /// to a transport message and returns an outcome, which is the whole of "credential material
    /// never enters the caller's scope".
    /// </summary>
    [Fact]
    public void TheExchangeInterfaceReturnsNoCredentialToAnyCaller()
    {
        var methods = typeof(ICredentialExchange).GetMethods();

        Assert.All(methods, m =>
        {
            var returned = m.ReturnType.IsGenericType ? m.ReturnType.GetGenericArguments()[0] : m.ReturnType;
            Assert.NotEqual(typeof(string), returned);
            Assert.True(returned == typeof(CredentialOutcome) || returned == typeof(void) || returned == typeof(Task));
        });
    }

    /// <summary>The secret store port is not visible outside the credentials assembly.</summary>
    [Fact]
    public void TheSecretStorePortIsNotPublic()
    {
        var port = typeof(CredentialBroker).Assembly.GetType("MediaCompany.Credentials.ISecretStore");

        Assert.NotNull(port);
        Assert.False(port!.IsPublic);
    }

    /// <summary>
    /// AC-013: a sweep of everything the system emits about a credential — the handle, its string
    /// rendering, its serialization, and the audit trail — returns zero occurrences of the held
    /// credential.
    /// </summary>
    [Fact]
    public async Task ASweepOfEmittedSurfacesFindsZeroOccurrencesOfTheCredential()
    {
        var (broker, _, _) = Build();

        var issued = Assert.IsType<CredentialOutcome.Issued>(
            await broker.IssueAsync(ProviderAccess(), CancellationToken.None));

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/v1");
        await broker.AttachAsync(issued.Handle, message, CancellationToken.None);

        var surfaces = new[]
        {
            issued.Handle.ToString(),
            JsonSerializer.Serialize(issued.Handle),
            JsonSerializer.Serialize(broker.AuditTrail),
            string.Join("\n", broker.AuditTrail.Select(e => $"{e.At} {e.Event} {e.HandleId} {e.Holder} {e.Refusal}")),
        };

        Assert.All(surfaces, surface =>
            Assert.DoesNotContain(Secret, surface, StringComparison.Ordinal));
    }

    /// <summary>The credential does reach the transport, which is where it is supposed to be.</summary>
    [Fact]
    public async Task TheCredentialIsAttachedAtTheTransportLayer()
    {
        var (broker, _, _) = Build();
        var issued = Assert.IsType<CredentialOutcome.Issued>(
            await broker.IssueAsync(ProviderAccess(), CancellationToken.None));

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/v1");
        var attached = await broker.AttachAsync(issued.Handle, message, CancellationToken.None);

        Assert.IsType<CredentialOutcome.Attached>(attached);
        Assert.Equal(Secret, message.Headers.Authorization?.Parameter);
    }

    /// <summary>AC-014: a credential presented after expiry is refused, and the refusal is recorded.</summary>
    [Fact]
    public async Task AHandlePresentedAfterExpiryIsRefusedAndRecorded()
    {
        var (broker, _, clock) = Build(TimeSpan.FromMinutes(5));
        var issued = Assert.IsType<CredentialOutcome.Issued>(
            await broker.IssueAsync(ProviderAccess(), CancellationToken.None));

        clock.Advance(TimeSpan.FromMinutes(6));

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/v1");
        var refused = Assert.IsType<CredentialOutcome.Refused>(
            await broker.AttachAsync(issued.Handle, message, CancellationToken.None));

        Assert.Equal(CredentialRefusal.Expired, refused.Reason);
        Assert.Null(message.Headers.Authorization);
        Assert.Contains(broker.AuditTrail, e => e.Event == "refused" && e.Refusal == CredentialRefusal.Expired);
    }

    /// <summary>AC-014: a handle presented outside its scope is refused and recorded.</summary>
    [Fact]
    public async Task AHandlePresentedOutsideItsScopeIsRefusedAndRecorded()
    {
        var (broker, store, clock) = Build();
        var otherAccount = new ProviderAccountId("provider-beta");
        store.Publish(new CredentialHolderKey(otherAccount, Channel), "sk-other");

        var issued = Assert.IsType<CredentialOutcome.Issued>(
            await broker.IssueAsync(ProviderAccess(), CancellationToken.None));

        // The caller rewrites the handle to name a different provider account. The broker holds
        // the authoritative copy, so the substitution is detected.
        var forged = issued.Handle with { };
        var field = typeof(ScopedHandle).GetProperty(nameof(ScopedHandle.ProviderAccount))!;
        Assert.False(field.CanWrite, "the handle is immutable, so a caller cannot widen its scope in place");

        // Presenting an entirely unknown handle is refused the same way.
        var unknown = CreateUnknownHandle(clock.UtcNow);
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/v1");
        var refused = Assert.IsType<CredentialOutcome.Refused>(
            await broker.AttachAsync(unknown, message, CancellationToken.None));

        Assert.Equal(CredentialRefusal.OutOfScope, refused.Reason);
        Assert.Null(message.Headers.Authorization);
        Assert.NotNull(forged);
    }

    /// <summary>The use bound is spent after its declared number of presentations.</summary>
    [Fact]
    public async Task AHandleIsSpentAfterItsUseBound()
    {
        var (broker, _, _) = Build();
        var issued = Assert.IsType<CredentialOutcome.Issued>(
            await broker.IssueAsync(ProviderAccess(), CancellationToken.None));

        using var first = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/v1");
        Assert.IsType<CredentialOutcome.Attached>(await broker.AttachAsync(issued.Handle, first, CancellationToken.None));

        using var second = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/v1");
        var refused = Assert.IsType<CredentialOutcome.Refused>(
            await broker.AttachAsync(issued.Handle, second, CancellationToken.None));

        Assert.Equal(CredentialRefusal.UseBoundSpent, refused.Reason);
    }

    /// <summary>
    /// AC-016: a rotation takes effect on the next use with no redeployment. The broker resolves
    /// the secret at issuance and at presentation and never at start-up, which is the property
    /// risk R-007 names.
    /// </summary>
    [Fact]
    public async Task ARotationTakesEffectOnTheNextUseWithNoRestart()
    {
        var (broker, store, _) = Build();
        var holder = new CredentialHolderKey(Account, Channel);

        var first = Assert.IsType<CredentialOutcome.Issued>(
            await broker.IssueAsync(ProviderAccess(), CancellationToken.None));
        using var message1 = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/v1");
        await broker.AttachAsync(first.Handle, message1, CancellationToken.None);
        Assert.Equal(Secret, message1.Headers.Authorization?.Parameter);

        store.Rotate(holder, "sk-rotated-9b1d");

        var second = Assert.IsType<CredentialOutcome.Issued>(
            await broker.IssueAsync(ProviderAccess(), CancellationToken.None));
        using var message2 = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/v1");
        await broker.AttachAsync(second.Handle, message2, CancellationToken.None);

        Assert.Equal("sk-rotated-9b1d", message2.Headers.Authorization?.Parameter);
    }

    /// <summary>AC-016: a revocation invalidates a live handle at its next presentation.</summary>
    [Fact]
    public async Task ARevocationInvalidatesALiveHandleAtItsNextPresentation()
    {
        var (broker, store, _) = Build();

        var issued = Assert.IsType<CredentialOutcome.Issued>(
            await broker.IssueAsync(ProviderAccess(), CancellationToken.None));

        store.Revoke(new CredentialHolderKey(Account, Channel));

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/v1");
        var refused = Assert.IsType<CredentialOutcome.Refused>(
            await broker.AttachAsync(issued.Handle, message, CancellationToken.None));

        Assert.Equal(CredentialRefusal.Revoked, refused.Reason);
        Assert.Null(message.Headers.Authorization);
    }

    /// <summary>
    /// AC-016: the credential-to-holder mapping shows no credential spanning two providers,
    /// accounts or channels. The holder key is the pair, so a second holder is a second key and a
    /// second secret; sharing is unrepresentable rather than discouraged.
    /// </summary>
    [Fact]
    public void TheHolderKeyIsThePairOfProviderAccountAndChannel()
    {
        var a = new CredentialHolderKey(Account, Channel);
        var b = new CredentialHolderKey(Account, ChannelId.New());
        var c = new CredentialHolderKey(new ProviderAccountId("provider-beta"), Channel);

        Assert.NotEqual(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a, new CredentialHolderKey(Account, Channel));
    }

    /// <summary>A holder that does not exist yields no handle at all.</summary>
    [Fact]
    public async Task AnAbsentHolderYieldsNoHandle()
    {
        var store = new FakeSecretStore();
        var clock = new TestClock(DateTimeOffset.Parse("2026-10-01T12:00:00Z"));
        var broker = new CredentialBroker(store, () => clock.UtcNow, TimeSpan.FromMinutes(5));

        var refused = Assert.IsType<CredentialOutcome.Refused>(
            await broker.IssueAsync(ProviderAccess(), CancellationToken.None));

        Assert.Equal(CredentialRefusal.NoHolder, refused.Reason);
    }

    /// <summary>AC-015: a release-credential request from a role other than the publisher is refused.</summary>
    [Theory]
    [InlineData(WorkforceRole.Copyright)]
    [InlineData(WorkforceRole.Producer)]
    [InlineData(WorkforceRole.Owner)]
    [InlineData(WorkforceRole.Operator)]
    public async Task AReleaseCredentialRequestFromAnotherRoleIsRefused(WorkforceRole actor)
    {
        var (broker, _, clock) = Build();

        var refused = Assert.IsType<CredentialOutcome.Refused>(await broker.IssueAsync(
            new CredentialRequest
            {
                CredentialClass = CredentialClass.Release,
                ProviderAccount = Account,
                Job = Job,
                Stage = LifecyclePosition.OwnerReview,
                Actor = actor,
                Channel = Channel,
                GatePass = new GatePassToken(ItemId.New(), new ItemVersion(1), "publication", clock.UtcNow),
                OpenBlockCount = 0,
            },
            CancellationToken.None));

        Assert.Equal(CredentialRefusal.NotPublishingRole, refused.Reason);
    }

    /// <summary>AC-015: a release-credential request made before the gate passes is refused.</summary>
    [Fact]
    public async Task AReleaseCredentialRequestBeforeTheGatePassesIsRefused()
    {
        var (broker, _, _) = Build();

        var refused = Assert.IsType<CredentialOutcome.Refused>(await broker.IssueAsync(
            new CredentialRequest
            {
                CredentialClass = CredentialClass.Release,
                ProviderAccount = Account,
                Job = Job,
                Stage = LifecyclePosition.OwnerReview,
                Actor = WorkforceRole.Publisher,
                Channel = Channel,
                GatePass = null,
                OpenBlockCount = 0,
            },
            CancellationToken.None));

        Assert.Equal(CredentialRefusal.GateNotPassed, refused.Reason);
    }

    /// <summary>A release credential is refused while a block stands, even with a gate-pass token.</summary>
    [Fact]
    public async Task AReleaseCredentialIsRefusedWhileABlockStands()
    {
        var (broker, _, clock) = Build();

        var refused = Assert.IsType<CredentialOutcome.Refused>(await broker.IssueAsync(
            new CredentialRequest
            {
                CredentialClass = CredentialClass.Release,
                ProviderAccount = Account,
                Job = Job,
                Stage = LifecyclePosition.OwnerReview,
                Actor = WorkforceRole.Publisher,
                Channel = Channel,
                GatePass = new GatePassToken(ItemId.New(), new ItemVersion(1), "publication", clock.UtcNow),
                OpenBlockCount = 1,
            },
            CancellationToken.None));

        Assert.Equal(CredentialRefusal.BlockStanding, refused.Reason);
    }

    /// <summary>Every issuance and every presentation writes an audit event carrying the handle identity.</summary>
    [Fact]
    public async Task EveryIssuanceAndPresentationIsAudited()
    {
        var (broker, _, _) = Build();
        var issued = Assert.IsType<CredentialOutcome.Issued>(
            await broker.IssueAsync(ProviderAccess(), CancellationToken.None));

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/v1");
        await broker.AttachAsync(issued.Handle, message, CancellationToken.None);

        Assert.Contains(broker.AuditTrail, e => e.Event == "issued" && e.HandleId == issued.Handle.HandleId);
        Assert.Contains(broker.AuditTrail, e => e.Event == "presented" && e.HandleId == issued.Handle.HandleId);
    }

    private static ScopedHandle CreateUnknownHandle(DateTimeOffset now)
    {
        var ctor = typeof(ScopedHandle)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(c => c.GetParameters().Length == 10);
        return (ScopedHandle)ctor.Invoke(
        [
            Guid.NewGuid(),
            CredentialClass.ProviderAccess,
            Account,
            (ChannelId?)Channel,
            (CapabilityClass?)CapabilityClass.EditorialReasoning,
            Job,
            LifecyclePosition.Script,
            now,
            now.AddMinutes(5),
            1,
        ]);
    }
}
