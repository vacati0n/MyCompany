using MediaCompany.Credentials;
using MediaCompany.Domain;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Capability.Tests;

/// <summary>
/// STRUCTURAL ABSENCE FIVE, demonstrated independently of the other four.
///
/// The release-credential issuance predicate additionally requires all three conditions of first
/// publication. None of them is discharged, and none can be discharged by this system, so no
/// release credential is issuable and a hypothetical transport would hold nothing to present.
///
/// This absence stands on its own: it holds even if the outcome union gained a case, even if the
/// workflow gained a position, even if the action set gained an egress action, and even if a
/// transport appeared in the build.
/// </summary>
public sealed class ReleaseCredentialTests
{
    private const string Secret = "sk-test-DO-NOT-LEAK-7f3a9c2e";

    private static readonly ProviderAccountId Account = new("provider-alpha");
    private static readonly ChannelId Channel = ChannelId.New();
    private static readonly JobId Job = JobId.New();
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(3);
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
    private static readonly DateOnly Observed = new(2026, 9, 27);

    private static CredentialBroker Build()
    {
        var store = new FakeSecretStore();
        var clock = new TestClock(Now);
        store.Publish(new CredentialHolderKey(Account, Channel), Secret);
        return new CredentialBroker(store, () => clock.UtcNow, TimeSpan.FromMinutes(5));
    }

    private static CredentialRequest Release(FirstPublicationConditionRegister? conditions) => new()
    {
        CredentialClass = CredentialClass.Release,
        ProviderAccount = Account,
        Job = Job,
        Stage = LifecyclePosition.OwnerReview,
        Actor = WorkforceRole.Publisher,
        Channel = Channel,
        GatePass = new GatePassToken(Item, Version, "publication", Now),
        OpenBlockCount = 0,
        FirstPublicationConditions = conditions,
    };

    private static FirstPublicationConditionRegister Register(
        params (FirstPublicationCondition Condition, ConditionState State)[] entries) =>
        new(entries.Select(e => new ConditionObservation(
            e.Condition, e.State, $"observation of {e.Condition}", Observed)));

    private static FirstPublicationConditionRegister AllSatisfied =>
        Register(Enum.GetValues<FirstPublicationCondition>().Select(c => (c, ConditionState.Satisfied)).ToArray());

    /// <summary>
    /// The real recorded position. Library registration is not done, the payment account does not
    /// exist, and two-step verification is unconfirmed, so no release credential is issuable.
    /// </summary>
    [Fact]
    public async Task NoReleaseCredentialIsIssuableUnderTheRealRecordedConditions()
    {
        var outcome = await Build().IssueAsync(
            Release(Register(
                (FirstPublicationCondition.LibraryRegistration, ConditionState.NotSatisfied),
                (FirstPublicationCondition.PaymentAccount, ConditionState.NotSatisfied),
                (FirstPublicationCondition.TwoStepVerification, ConditionState.Unknown))),
            CancellationToken.None);

        var refused = Assert.IsType<CredentialOutcome.Refused>(outcome);
        Assert.Equal(CredentialRefusal.FirstPublicationConditionUnmet, refused.Reason);
        Assert.Contains("LibraryRegistration", refused.Detail);
        Assert.Contains("PaymentAccount", refused.Detail);
        Assert.Contains("TwoStepVerification", refused.Detail);
    }

    /// <summary>
    /// A request carrying NO register is refused exactly as one carrying an unsatisfied register.
    /// Omitting the conditions is not a way past the check.
    /// </summary>
    [Fact]
    public async Task AReleaseRequestCarryingNoConditionRegisterIsRefused()
    {
        var outcome = await Build().IssueAsync(Release(conditions: null), CancellationToken.None);

        var refused = Assert.IsType<CredentialOutcome.Refused>(outcome);
        Assert.Equal(CredentialRefusal.FirstPublicationConditionUnmet, refused.Reason);
        Assert.Contains("NO observation is recorded", refused.Detail);
    }

    /// <summary>Any ONE unsatisfied condition is enough to refuse, whichever of the three it is.</summary>
    [Theory]
    [InlineData(FirstPublicationCondition.LibraryRegistration)]
    [InlineData(FirstPublicationCondition.PaymentAccount)]
    [InlineData(FirstPublicationCondition.TwoStepVerification)]
    public async Task AnySingleUnsatisfiedConditionRefusesTheReleaseCredential(
        FirstPublicationCondition unsatisfied)
    {
        var entries = Enum.GetValues<FirstPublicationCondition>()
            .Select(c => (c, c == unsatisfied ? ConditionState.Unknown : ConditionState.Satisfied))
            .ToArray();

        var outcome = await Build().IssueAsync(Release(Register(entries)), CancellationToken.None);

        var refused = Assert.IsType<CredentialOutcome.Refused>(outcome);
        Assert.Equal(CredentialRefusal.FirstPublicationConditionUnmet, refused.Reason);
        Assert.Contains(unsatisfied.ToString(), refused.Detail);
    }

    /// <summary>
    /// The predicate is an ADDITIONAL requirement, not a replacement: the delivered refusals still
    /// fire, and they fire first, so the gate and the blocks are not bypassed by the new check.
    /// </summary>
    [Fact]
    public async Task TheDeliveredReleaseRefusalsStillFireAhead()
    {
        var broker = Build();

        var wrongRole = await broker.IssueAsync(
            Release(AllSatisfied) with { Actor = WorkforceRole.Producer }, CancellationToken.None);
        Assert.Equal(
            CredentialRefusal.NotPublishingRole,
            Assert.IsType<CredentialOutcome.Refused>(wrongRole).Reason);

        var noGatePass = await broker.IssueAsync(
            Release(AllSatisfied) with { GatePass = null }, CancellationToken.None);
        Assert.Equal(
            CredentialRefusal.GateNotPassed,
            Assert.IsType<CredentialOutcome.Refused>(noGatePass).Reason);

        var blocked = await broker.IssueAsync(
            Release(AllSatisfied) with { OpenBlockCount = 2 }, CancellationToken.None);
        Assert.Equal(
            CredentialRefusal.BlockStanding,
            Assert.IsType<CredentialOutcome.Refused>(blocked).Reason);
    }

    /// <summary>
    /// The check is a real predicate and not a blanket refusal: with all three satisfied the
    /// issuance proceeds past it. Nothing in this change satisfies them, so this is the shape of
    /// the later wave rather than a state this one can reach.
    /// </summary>
    [Fact]
    public async Task WithAllThreeSatisfiedTheIssuanceProceedsPastTheConditionCheck()
    {
        var outcome = await Build().IssueAsync(Release(AllSatisfied), CancellationToken.None);

        Assert.IsType<CredentialOutcome.Issued>(outcome);
    }

    /// <summary>A provider-access credential is unaffected: the conditions gate release alone.</summary>
    [Fact]
    public async Task AProviderAccessCredentialIsUnaffectedByTheConditions()
    {
        var outcome = await Build().IssueAsync(
            new CredentialRequest
            {
                CredentialClass = CredentialClass.ProviderAccess,
                ProviderAccount = Account,
                Job = Job,
                Stage = LifecyclePosition.Script,
                Actor = WorkforceRole.Producer,
                Channel = Channel,
                Capability = CapabilityClass.EditorialReasoning,
            },
            CancellationToken.None);

        Assert.IsType<CredentialOutcome.Issued>(outcome);
    }
}
