using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Configuration;
using MediaCompany.Domain.Publication;
using Xunit;

namespace MediaCompany.Domain.Tests;

/// <summary>
/// The invariants the technical design makes structural rather than guarded. Each test asserts
/// the absence of an alternative path, not the rejection of one.
/// </summary>
public sealed class ActionSetTests
{
    /// <summary>
    /// Acceptance criterion AC-010: the copyright responsibility can block a release and holds no
    /// permission that releases.
    /// </summary>
    [Fact]
    public void CopyrightRoleHoldsNoReleasingAction()
    {
        var copyright = ActionSet.For(WorkforceRole.Copyright);

        Assert.Contains(ActionKind.BlockPlace, copyright);
        Assert.Contains(ActionKind.BlockClear, copyright);
        Assert.DoesNotContain(ActionKind.Publish, copyright);
        Assert.DoesNotContain(ActionKind.ReleaseCredentialRequest, copyright);
    }

    /// <summary>
    /// Acceptance criterion AC-011: the publishing responsibility has no route that overrides a
    /// standing block. Its action set is disjoint from every Block action, so the override is not
    /// an expressible action rather than a forbidden one.
    /// </summary>
    [Fact]
    public void PublishingRoleIsDisjointFromEveryBlockAction()
    {
        var publisher = ActionSet.For(WorkforceRole.Publisher);
        var blockActions = new[] { ActionKind.BlockPlace, ActionKind.BlockClear };

        Assert.Empty(publisher.Intersect(blockActions));
    }

    /// <summary>Only the owner decides an approval; no other role holds ApprovalDecide.</summary>
    [Fact]
    public void OnlyTheOwnerDecidesAnApproval()
    {
        var holders = ActionSet.Roles.Where(r => ActionSet.Holds(r, ActionKind.ApprovalDecide)).ToArray();
        Assert.Equal([WorkforceRole.Owner], holders);
    }

    /// <summary>Only the publishing role may request a release credential (acceptance criterion AC-015).</summary>
    [Fact]
    public void OnlyThePublishingRoleRequestsAReleaseCredential()
    {
        var holders = ActionSet.Roles.Where(r => ActionSet.Holds(r, ActionKind.ReleaseCredentialRequest)).ToArray();
        Assert.Equal([WorkforceRole.Publisher], holders);
    }

    /// <summary>A role outside the closed set holds nothing, rather than defaulting to anything.</summary>
    [Fact]
    public void AnUnknownRoleHoldsNoAction()
    {
        Assert.Empty(ActionSet.For((WorkforceRole)9999));
    }
}

public sealed class GateTransitionTableTests
{
    /// <summary>
    /// Decision D-007: owner approval is a state in the transition table. Every path from Draft to
    /// Published passes through AwaitingOwnerApproval, so there is no bypass to configure away.
    /// </summary>
    [Fact]
    public void EveryPathToPublishedPassesOwnerApproval()
    {
        var paths = GateTransitionTable.PathsToPublished();

        Assert.NotEmpty(paths);
        Assert.All(paths, path => Assert.Contains(GateState.AwaitingOwnerApproval, path));
    }

    /// <summary>Published is reachable only from Approved (decision D-003, layer three).</summary>
    [Theory]
    [InlineData(GateState.Draft)]
    [InlineData(GateState.AwaitingRightsCheck)]
    [InlineData(GateState.AwaitingOwnerApproval)]
    [InlineData(GateState.SentBack)]
    [InlineData(GateState.Withdrawn)]
    public void PublishedIsUnreachableFromAnythingButApproved(GateState from)
    {
        Assert.False(GateTransitionTable.IsAllowed(from, GateState.Published));
    }

    [Fact]
    public void PublishedIsReachableFromApproved()
    {
        Assert.True(GateTransitionTable.IsAllowed(GateState.Approved, GateState.Published));
    }

    /// <summary>Published and Withdrawn are terminal; nothing leaves them.</summary>
    [Fact]
    public void TerminalStatesHaveNoOutwardTransition()
    {
        Assert.Empty(GateTransitionTable.From(GateState.Published));
        Assert.Empty(GateTransitionTable.From(GateState.Withdrawn));
    }
}

public sealed class ConfigurationSurfaceTests
{
    /// <summary>
    /// Decision D-007: the owner-approval step is absent from the configuration surface. The
    /// admitted key set is closed and contains no key that names approval, so there is nothing to
    /// switch off.
    /// </summary>
    [Fact]
    public void NoAdmittedConfigurationKeyTouchesApproval()
    {
        Assert.DoesNotContain(
            ConfigurationKeys.Admitted,
            key => key.Contains("approval", StringComparison.OrdinalIgnoreCase)
                || key.Contains("gate", StringComparison.OrdinalIgnoreCase)
                || key.Contains("owner", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnUnlistedKeyIsNotAdmitted()
    {
        Assert.False(ConfigurationKeys.IsAdmitted("gate.owner-approval-required"));
        Assert.True(ConfigurationKeys.IsAdmitted(ConfigurationKeys.ModelPrice));
    }
}

public sealed class ControlConstructionTests
{
    /// <summary>
    /// Risk R-008: a control with no subject role cannot be admitted, because without the relation
    /// no rule could establish that an approver is not subject to it.
    /// </summary>
    [Fact]
    public void AControlWithNoSubjectRoleCannotBeConstructed()
    {
        var thrown = Assert.Throws<ArgumentException>(() => new Control(
            "CTRL-001", "a control", nonCuttable: false, subjectRoles: [], grantsActions: [ActionKind.Publish]));

        Assert.Contains("subject role", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AControlWithASubjectRoleIsAdmitted()
    {
        var control = new Control(
            "CTRL-002", "a control", nonCuttable: true, subjectRoles: [WorkforceRole.Publisher], grantsActions: []);

        Assert.True(control.NonCuttable);
        Assert.Single(control.SubjectRoles);
    }
}

public sealed class ApprovalTests
{
    private static readonly ItemId Item = ItemId.New();

    /// <summary>
    /// Acceptance criterion AC-018 and risk RK-005: elapsed minutes are derived from the two
    /// recorded timestamps, never entered. There is no constructor parameter that accepts one.
    /// </summary>
    [Fact]
    public void ElapsedMinutesAreDerivedFromTheRecordedTimestamps()
    {
        var presented = DateTimeOffset.Parse("2026-10-01T09:00:00Z");
        var decided = DateTimeOffset.Parse("2026-10-01T09:47:00Z");

        var approval = new Approval(
            Item, new ItemVersion(3), "publication", WorkforceRole.Owner,
            ApprovalVerdict.Approved, "looks right", presented, decided);

        Assert.Equal(47, approval.ElapsedMinutes);
    }

    [Fact]
    public void ASendBackCarriesItsReasonAndItsElapsedMinutes()
    {
        var presented = DateTimeOffset.Parse("2026-10-01T09:00:00Z");
        var approval = new Approval(
            Item, new ItemVersion(1), "publication", WorkforceRole.Owner,
            ApprovalVerdict.SentBack, "the third claim is unsourced", presented, presented.AddMinutes(12));

        Assert.Equal(ApprovalVerdict.SentBack, approval.Verdict);
        Assert.Equal(12, approval.ElapsedMinutes);
        Assert.NotEmpty(approval.Reason);
    }

    [Fact]
    public void AnApprovalCannotBeDecidedBeforeItWasPresented()
    {
        var presented = DateTimeOffset.Parse("2026-10-01T09:00:00Z");
        Assert.Throws<ArgumentException>(() => new Approval(
            Item, new ItemVersion(1), "publication", WorkforceRole.Owner,
            ApprovalVerdict.Approved, "reason", presented, presented.AddMinutes(-1)));
    }

    /// <summary>Constraint C-016: the version is part of the approval's identity.</summary>
    [Fact]
    public void ApprovalsForDifferentVersionsAreDifferentApprovals()
    {
        var presented = DateTimeOffset.Parse("2026-10-01T09:00:00Z");
        var v1 = new Approval(Item, new ItemVersion(1), "publication", WorkforceRole.Owner,
            ApprovalVerdict.Approved, "ok", presented, presented.AddMinutes(5));
        var v2 = new Approval(Item, new ItemVersion(2), "publication", WorkforceRole.Owner,
            ApprovalVerdict.Approved, "ok", presented, presented.AddMinutes(5));

        Assert.NotEqual(v1, v2);
        Assert.NotEqual(v1.ItemVersion, v2.ItemVersion);
    }
}

public sealed class CapabilityRequestTests
{
    private static Attribution Attribution() =>
        new(ItemId.New(), ChannelId.New(), DepartmentId.New(), AgentId.New());

    /// <summary>
    /// Decision D-012: the request declares what is needed and never who provides it. The type has
    /// no provider and no model member, which this test asserts over its public surface.
    /// </summary>
    [Fact]
    public void ACapabilityRequestNamesNoProviderAndNoModel()
    {
        var members = typeof(CapabilityRequest)
            .GetProperties()
            .Select(p => p.Name)
            .ToArray();

        Assert.DoesNotContain(members, name => name.Contains("Provider", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(members, name => name.Contains("Model", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(members, name => name.Contains("Route", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Resolution step 6: a critical request is held rather than downgraded, so it may not carry a
    /// reduced-floor policy at all.
    /// </summary>
    [Fact]
    public void ACriticalRequestCannotCarryAReducedFloorPolicy()
    {
        Assert.Throws<ArgumentException>(() => new CapabilityRequest(
            CapabilityClass.HighStakesReview,
            ReasoningTier.Deep,
            new QualityRating(90),
            new ContextCapacity(8_000),
            new Money(0.50m),
            Criticality.Critical,
            Attribution(),
            EstimatedUnits.None,
            TimeSpan.FromHours(4),
            ReducedFloorPolicy.AllowDownTo(new QualityRating(60))));
    }

    [Fact]
    public void AQualityRatingIsBoundedToTheDeclaredScale()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new QualityRating(101));
        Assert.Throws<ArgumentOutOfRangeException>(() => new QualityRating(-1));
    }
}

public sealed class ForbiddenSourceTests
{
    /// <summary>
    /// The five kinds design fact F-005 records. The set being closed is what keeps the
    /// consumer-subscription account pool out: adding it back would be a code change.
    /// </summary>
    [Fact]
    public void TheForbiddenKindSetIsTheOneTheDesignRecords()
    {
        var kinds = Enum.GetValues<ForbiddenSourceKind>();

        Assert.Equal(5, kinds.Length);
        Assert.Contains(ForbiddenSourceKind.ConsumerChatSubscription, kinds);
        Assert.Contains(ForbiddenSourceKind.MultipliedPersonalAccount, kinds);
        Assert.Contains(ForbiddenSourceKind.SharedCredential, kinds);
        Assert.Contains(ForbiddenSourceKind.AutomatedAccessProhibited, kinds);
        Assert.Contains(ForbiddenSourceKind.WithdrawnInterface, kinds);
    }

    [Fact]
    public void AForbiddenSourceEntryCarriesItsReasonAndEvidence()
    {
        Assert.Throws<ArgumentException>(() => new ForbiddenSource(
            ForbiddenSourceKind.SharedCredential, "acct-x", reason: " ", evidenceReference: "policy"));

        Assert.Throws<ArgumentException>(() => new ForbiddenSource(
            ForbiddenSourceKind.SharedCredential, "acct-x", reason: "prohibited", evidenceReference: " "));
    }
}

public sealed class EnvelopeTests
{
    /// <summary>
    /// Design fact F-009: the approved envelope is USD 77.41 a month, split 34.42 metered and
    /// 42.99 standing. Every figure compared against it is an ESTIMATE until a unit price has been
    /// verified first-hand, which risk RK-002 requires before any spend.
    /// </summary>
    [Fact]
    public void TheEnvelopeSplitsIntoItsRecordedParts()
    {
        Assert.Equal(77.41m, ApprovedEnvelope.MonthlyTotal.Amount);
        Assert.Equal(34.42m, ApprovedEnvelope.Metered.Amount);
        Assert.Equal(42.99m, ApprovedEnvelope.Standing.Amount);
        Assert.Equal(
            ApprovedEnvelope.MonthlyTotal.Amount,
            ApprovedEnvelope.Metered.Amount + ApprovedEnvelope.Standing.Amount);
    }

    /// <summary>The four budget thresholds constraint C-018 fixes.</summary>
    [Fact]
    public void TheBudgetThresholdSetIsFixed()
    {
        Assert.Equal([50, 75, 90, 100], Budget.Thresholds);
    }
}
