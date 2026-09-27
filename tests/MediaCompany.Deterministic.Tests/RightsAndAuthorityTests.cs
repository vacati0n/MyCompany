using MediaCompany.Deterministic.Authority;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Deterministic.Rights;
using MediaCompany.Domain;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Rights;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>Demonstrations for the rights record, against acceptance criteria AC-005 to AC-008.</summary>
public sealed class PermissionBasisTests
{
    private static readonly ChannelId Channel = ChannelId.New();
    private static readonly ItemId Item = ItemId.New();
    private static readonly DateOnly Today = new(2026, 10, 1);

    private static readonly LibraryRegistration[] Registered =
        [new(Channel, "stock-library-a", new DateOnly(2026, 9, 1))];

    private static Asset Complete() => new()
    {
        Id = AssetId.New(),
        Item = Item,
        Library = "stock-library-a",
        Source = "stock-library-a catalogue",
        Creator = "A. Photographer",
        LicenceType = LicenceType.RoyaltyFreeStock,
        LicenceReference = "LIC-2026-0001",
        CommercialUsePermitted = true,
        ModificationPermitted = true,
        AttributionRequirement = "none required under this licence",
        PlatformRestrictions = "none",
        Expiry = null,
        ProofOfLicenceReference = "receipt-2026-0001.pdf",
        AssessedRisk = AssessedRisk.Low,
        VerifiedBy = WorkforceRole.Copyright,
        VerifiedOn = new DateOnly(2026, 9, 20),
    };

    /// <summary>AC-005: a complete record resolves as permitted and states its basis.</summary>
    [Fact]
    public void ACompleteRecordResolvesAsPermitted()
    {
        var verdict = PermissionBasis.Resolve(Complete(), Registered, Today);

        var permitted = Assert.IsType<PermissionVerdict.Permitted>(verdict);
        Assert.Contains("LIC-2026-0001", permitted.Basis);
        Assert.Equal(nameof(WorkforceRole.Copyright), permitted.VerifiedByRole);
        Assert.Equal(new DateOnly(2026, 9, 20), permitted.VerifiedOn);
    }

    /// <summary>
    /// AC-005: a record missing any field resolves as NOT PERMITTED rather than as permitted, and
    /// names what is missing. The verdict union has two members and no third a reader could
    /// mistake for permission.
    /// </summary>
    [Theory]
    [InlineData("source")]
    [InlineData("creator")]
    [InlineData("licence reference")]
    [InlineData("proof of licence")]
    [InlineData("verifier identity")]
    [InlineData("verification date")]
    public void ARecordMissingAnyFieldResolvesAsNotPermitted(string missingField)
    {
        var asset = Complete();
        asset = missingField switch
        {
            "source" => asset with { Source = null },
            "creator" => asset with { Creator = null },
            "licence reference" => asset with { LicenceReference = null },
            "proof of licence" => asset with { ProofOfLicenceReference = null },
            "verifier identity" => asset with { VerifiedBy = null },
            "verification date" => asset with { VerifiedOn = null },
            _ => asset,
        };

        var refused = Assert.IsType<PermissionVerdict.NotPermitted>(
            PermissionBasis.Resolve(asset, Registered, Today));

        Assert.Contains(refused.MissingOrFailing, m => m.Contains(missingField, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>An expired licence resolves as not permitted and names the expiry.</summary>
    [Fact]
    public void AnExpiredLicenceResolvesAsNotPermitted()
    {
        var expired = Complete() with { Expiry = new DateOnly(2026, 8, 1) };

        var refused = Assert.IsType<PermissionVerdict.NotPermitted>(
            PermissionBasis.Resolve(expired, Registered, Today));

        Assert.Contains(refused.MissingOrFailing, m => m.Contains("expired", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>A licence forbidding commercial use resolves as not permitted.</summary>
    [Fact]
    public void ALicenceForbiddingCommercialUseResolvesAsNotPermitted()
    {
        var noncommercial = Complete() with { CommercialUsePermitted = false };

        var refused = Assert.IsType<PermissionVerdict.NotPermitted>(
            PermissionBasis.Resolve(noncommercial, Registered, Today));

        Assert.Contains(refused.MissingOrFailing, m => m.Contains("commercial use", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>AC-007: an unregistered channel resolves as not permitted, naming the library.</summary>
    [Fact]
    public void AnUnregisteredChannelResolvesAsNotPermitted()
    {
        var refused = Assert.IsType<PermissionVerdict.NotPermitted>(
            PermissionBasis.Resolve(Complete(), [], Today));

        Assert.Contains(refused.MissingOrFailing, m => m.Contains("not registered", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// AC-006: an item holding a deliberately unlicensed asset cannot reach a releasable state,
    /// and the block names the asset and the missing basis.
    /// </summary>
    [Fact]
    public void AnItemHoldingAnUnlicensedAssetIsNotReleasable()
    {
        var good = Complete();
        var bad = Complete() with { LicenceReference = null, ProofOfLicenceReference = null };

        var verdict = ReleasableRightsPrecondition.Evaluate([good, bad], Registered, Today);

        Assert.False(verdict.Releasable);
        var blocking = Assert.Single(verdict.Blocking);
        Assert.Equal(bad.Id, blocking.Asset);
        Assert.Contains(blocking.MissingOrFailing, m => m.Contains("licence reference", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>An item whose assets are all complete and registered is releasable on rights grounds.</summary>
    [Fact]
    public void AnItemWithCompleteAssetsIsReleasableOnRightsGrounds()
    {
        var verdict = ReleasableRightsPrecondition.Evaluate([Complete(), Complete()], Registered, Today);

        Assert.True(verdict.Releasable);
        Assert.Empty(verdict.Blocking);
    }
}

/// <summary>The three least-privilege refusals, against acceptance criteria AC-010 to AC-012.</summary>
public sealed class AuthorityRuleTests
{
    /// <summary>AC-010: an attempt by the copyright responsibility to release is refused.</summary>
    [Fact]
    public void ACopyrightReleaseAttemptIsRefused()
    {
        var refused = Assert.IsType<PermissionEvaluation.Refused>(
            AuthorityRules.Evaluate(WorkforceRole.Copyright, ActionKind.Publish));

        Assert.Equal(RefusalGround.ActionNotHeld, refused.Ground);
        Assert.Contains("closed", refused.Detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-011: the publishing responsibility cannot clear a standing block.</summary>
    [Fact]
    public void APublisherAttemptToClearABlockIsRefused()
    {
        var refused = Assert.IsType<PermissionEvaluation.Refused>(
            AuthorityRules.Evaluate(WorkforceRole.Publisher, ActionKind.BlockClear));

        Assert.Equal(RefusalGround.ActionNotHeld, refused.Ground);
    }

    /// <summary>The copyright role can place a block; the permission it does hold is exercised.</summary>
    [Fact]
    public void TheCopyrightRoleCanPlaceABlock()
    {
        Assert.IsType<PermissionEvaluation.Permitted>(
            AuthorityRules.Evaluate(WorkforceRole.Copyright, ActionKind.BlockPlace));
    }

    /// <summary>AC-012: a self-approved exception is refused for every role subject to a control.</summary>
    [Theory]
    [InlineData(WorkforceRole.Publisher)]
    [InlineData(WorkforceRole.Copyright)]
    [InlineData(WorkforceRole.Producer)]
    [InlineData(WorkforceRole.Operator)]
    [InlineData(WorkforceRole.Accountant)]
    public void ASelfApprovedExceptionIsRefused(WorkforceRole role)
    {
        var control = new Control("CTRL-SELF", "a control", nonCuttable: false, subjectRoles: [role], grantsActions: []);
        var request = new ExceptionRequest("EXC-001", control.Id, role, "convenience");

        var refused = Assert.IsType<PermissionEvaluation.Refused>(
            AuthorityRules.EvaluateExceptionApproval(control, request, role));

        Assert.Equal(RefusalGround.SelfApproval, refused.Ground);
    }

    /// <summary>An approver subject to the control is refused even when it did not request it.</summary>
    [Fact]
    public void AnApproverSubjectToTheControlIsRefused()
    {
        var control = new Control(
            "CTRL-SUBJECT", "a control", nonCuttable: false,
            subjectRoles: [WorkforceRole.Publisher, WorkforceRole.Owner], grantsActions: []);
        var request = new ExceptionRequest("EXC-002", control.Id, WorkforceRole.Publisher, "reason");

        var refused = Assert.IsType<PermissionEvaluation.Refused>(
            AuthorityRules.EvaluateExceptionApproval(control, request, WorkforceRole.Owner));

        Assert.Equal(RefusalGround.ApproverSubjectToControl, refused.Ground);
    }

    /// <summary>An approver that already holds the action the exception would grant is refused.</summary>
    [Fact]
    public void AnApproverWhoWouldGainTheActionIsRefused()
    {
        var control = new Control(
            "CTRL-GRANT", "a control over publishing", nonCuttable: false,
            subjectRoles: [WorkforceRole.Producer], grantsActions: [ActionKind.Publish]);
        var request = new ExceptionRequest("EXC-003", control.Id, WorkforceRole.Producer, "reason");

        var refused = Assert.IsType<PermissionEvaluation.Refused>(
            AuthorityRules.EvaluateExceptionApproval(control, request, WorkforceRole.Publisher));

        Assert.Equal(RefusalGround.ApproverWouldGainAction, refused.Ground);
    }

    /// <summary>A non-cuttable control cannot be excepted at all.</summary>
    [Fact]
    public void ANonCuttableControlCannotBeExcepted()
    {
        var control = new Control(
            "CTRL-HARD", "a non-cuttable control", nonCuttable: true,
            subjectRoles: [WorkforceRole.Producer], grantsActions: []);
        var request = new ExceptionRequest("EXC-004", control.Id, WorkforceRole.Producer, "cost");

        var refused = Assert.IsType<PermissionEvaluation.Refused>(
            AuthorityRules.EvaluateExceptionApproval(control, request, WorkforceRole.Owner));

        Assert.Equal(RefusalGround.ControlIsNonCuttable, refused.Ground);
    }

    /// <summary>An eligible approver is permitted, so the rule refuses the wrong approver and not every one.</summary>
    [Fact]
    public void AnEligibleApproverIsPermitted()
    {
        var control = new Control(
            "CTRL-OK", "a cuttable control", nonCuttable: false,
            subjectRoles: [WorkforceRole.Producer], grantsActions: [ActionKind.AssetRecord]);
        var request = new ExceptionRequest("EXC-005", control.Id, WorkforceRole.Producer, "reason");

        Assert.IsType<PermissionEvaluation.Permitted>(
            AuthorityRules.EvaluateExceptionApproval(control, request, WorkforceRole.Owner));
    }
}

/// <summary>The non-cuttable control protection, against acceptance criterion AC-028.</summary>
public sealed class NonCuttableControlTests
{
    private static readonly Control[] Controls =
    [
        new("CTRL-RIGHTS", "every asset carries a verified permission basis", nonCuttable: true,
            subjectRoles: [WorkforceRole.Producer], grantsActions: []),
        new("CTRL-APPROVAL", "the owner approves every publication", nonCuttable: true,
            subjectRoles: [WorkforceRole.Publisher], grantsActions: []),
        new("CTRL-CACHE", "responses are cached for a week", nonCuttable: false,
            subjectRoles: [WorkforceRole.Operator], grantsActions: []),
    ];

    /// <summary>An attempt to reduce a non-cuttable control by a cost decision is refused.</summary>
    [Fact]
    public void ACostDecisionReducingANonCuttableControlIsRefused()
    {
        var verdict = NonCuttableControlProtection.Apply(
            Controls, ["CTRL-APPROVAL"], "COST-001", WorkforceRole.Accountant,
            "reduce approval overhead", DateTimeOffset.Parse("2026-10-01T10:00:00Z"));

        Assert.False(verdict.Accepted);
        Assert.Equal(["CTRL-APPROVAL"], verdict.RefusedControls);
        Assert.Null(verdict.Recorded);
    }

    /// <summary>An accepted cost decision records the controls it did NOT touch.</summary>
    [Fact]
    public void AnAcceptedCostDecisionRecordsTheControlsItDidNotTouch()
    {
        var verdict = NonCuttableControlProtection.Apply(
            Controls, ["CTRL-CACHE"], "COST-002", WorkforceRole.Accountant,
            "shorten the cache window", DateTimeOffset.Parse("2026-10-01T10:00:00Z"));

        Assert.True(verdict.Accepted);
        Assert.NotNull(verdict.Recorded);
        var decision = verdict.Recorded!;
        Assert.Equal(["CTRL-CACHE"], decision.ControlsReduced);
        Assert.Equal(["CTRL-APPROVAL", "CTRL-RIGHTS"], decision.ControlsUntouched);
    }

    /// <summary>A decision touching nothing still records every control as untouched.</summary>
    [Fact]
    public void ADecisionTouchingNothingRecordsEveryControlAsUntouched()
    {
        var verdict = NonCuttableControlProtection.Apply(
            Controls, [], "COST-003", WorkforceRole.Accountant, "no reduction",
            DateTimeOffset.Parse("2026-10-01T10:00:00Z"));

        Assert.NotNull(verdict.Recorded);
        Assert.Equal(3, verdict.Recorded!.ControlsUntouched.Count);
    }
}

/// <summary>The publication gate refusals, against acceptance criteria AC-011 and AC-019.</summary>
public sealed class GatePredicateTests
{
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(7);
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");

    private static Approval OwnerApproval(ItemVersion version) => new(
        Item, version, GatePredicates.OwnerApprovalGate, WorkforceRole.Owner,
        ApprovalVerdict.Approved, "approved", Now.AddMinutes(-30), Now.AddMinutes(-10));

    /// <summary>
    /// A register in which all three conditions of first publication are satisfied.
    ///
    /// Supplied so each delivered refusal test still isolates the refusal it names: the conditions
    /// are evaluated last, so a satisfied register leaves every earlier refusal exactly as it was.
    /// It is a UNIT-TEST FIXTURE and says nothing about the real channel, where none of the three
    /// is discharged; the tests alongside assert that real position separately.
    /// </summary>
    private static FirstPublicationConditionRegister AllConditionsSatisfied =>
        new(Enum.GetValues<FirstPublicationCondition>().Select(c => new ConditionObservation(
            c, ConditionState.Satisfied, $"fixture: {c} satisfied for this unit test", new DateOnly(2026, 9, 27))));

    /// <summary>All preconditions met: the gate passes and issues a token naming the exact version.</summary>
    [Fact]
    public void TheGatePassesWhenEveryPreconditionIsMet()
    {
        var verdict = GatePredicates.EvaluatePublish(
            Item, Version, GateState.Approved, WorkforceRole.Publisher,
            openBlocks: [], approvals: [OwnerApproval(Version)], rightsPreconditionMet: true, Now, AllConditionsSatisfied);

        var passed = Assert.IsType<GateVerdict.Passed>(verdict);
        Assert.Equal(Version, passed.Token.ItemVersion);
        Assert.Equal(Item, passed.Token.Item);
    }

    /// <summary>AC-011, refusal one: a standing copyright block.</summary>
    [Fact]
    public void AStandingBlockRefusesTheGate()
    {
        var block = new Block(Guid.NewGuid(), Item, WorkforceRole.Copyright, "asset licence unverified", null, open: true);

        var refused = Assert.IsType<GateVerdict.Refused>(GatePredicates.EvaluatePublish(
            Item, Version, GateState.Approved, WorkforceRole.Publisher,
            openBlocks: [block], approvals: [OwnerApproval(Version)], rightsPreconditionMet: true, Now, AllConditionsSatisfied));

        Assert.Equal(GateRefusal.BlockStanding, refused.Reason);
        Assert.Contains("licence unverified", refused.Detail);
    }

    /// <summary>AC-011, refusal two: an incomplete approval set.</summary>
    [Fact]
    public void AnIncompleteApprovalSetRefusesTheGate()
    {
        var refused = Assert.IsType<GateVerdict.Refused>(GatePredicates.EvaluatePublish(
            Item, Version, GateState.Approved, WorkforceRole.Publisher,
            openBlocks: [], approvals: [], rightsPreconditionMet: true, Now, AllConditionsSatisfied));

        Assert.Equal(GateRefusal.ApprovalSetIncomplete, refused.Reason);
    }

    /// <summary>AC-019: an approval bound to another version refuses the gate.</summary>
    [Fact]
    public void AnApprovalBoundToAnotherVersionRefusesTheGate()
    {
        var refused = Assert.IsType<GateVerdict.Refused>(GatePredicates.EvaluatePublish(
            Item, Version, GateState.Approved, WorkforceRole.Publisher,
            openBlocks: [], approvals: [OwnerApproval(new ItemVersion(6))], rightsPreconditionMet: true, Now, AllConditionsSatisfied));

        Assert.Equal(GateRefusal.ApprovalVersionMismatch, refused.Reason);
    }

    /// <summary>A send-back is not an approval, so the gate still refuses.</summary>
    [Fact]
    public void ASendBackDoesNotSatisfyTheApprovalSet()
    {
        var sentBack = new Approval(
            Item, Version, GatePredicates.OwnerApprovalGate, WorkforceRole.Owner,
            ApprovalVerdict.SentBack, "reworked", Now.AddMinutes(-20), Now.AddMinutes(-5));

        var refused = Assert.IsType<GateVerdict.Refused>(GatePredicates.EvaluatePublish(
            Item, Version, GateState.Approved, WorkforceRole.Publisher,
            openBlocks: [], approvals: [sentBack], rightsPreconditionMet: true, Now, AllConditionsSatisfied));

        Assert.Equal(GateRefusal.ApprovalSetIncomplete, refused.Reason);
    }

    /// <summary>The rights precondition is a gate refusal in its own right (AC-006, AC-007).</summary>
    [Fact]
    public void AnUnmetRightsPreconditionRefusesTheGate()
    {
        var refused = Assert.IsType<GateVerdict.Refused>(GatePredicates.EvaluatePublish(
            Item, Version, GateState.Approved, WorkforceRole.Publisher,
            openBlocks: [], approvals: [OwnerApproval(Version)], rightsPreconditionMet: false, Now, AllConditionsSatisfied));

        Assert.Equal(GateRefusal.RightsPreconditionUnmet, refused.Reason);
    }

    /// <summary>A role holding no Publish action cannot pass the gate, whatever else holds.</summary>
    [Fact]
    public void ARoleWithNoPublishActionCannotPassTheGate()
    {
        var refused = Assert.IsType<GateVerdict.Refused>(GatePredicates.EvaluatePublish(
            Item, Version, GateState.Approved, WorkforceRole.Copyright,
            openBlocks: [], approvals: [OwnerApproval(Version)], rightsPreconditionMet: true, Now, AllConditionsSatisfied));

        Assert.Equal(GateRefusal.ActorHoldsNoAction, refused.Reason);
    }

    /// <summary>The transition table refuses a state that is not Approved, independently of the rest.</summary>
    [Fact]
    public void AStateOtherThanApprovedRefusesTheGate()
    {
        var refused = Assert.IsType<GateVerdict.Refused>(GatePredicates.EvaluatePublish(
            Item, Version, GateState.AwaitingOwnerApproval, WorkforceRole.Publisher,
            openBlocks: [], approvals: [OwnerApproval(Version)], rightsPreconditionMet: true, Now, AllConditionsSatisfied));

        Assert.Equal(GateRefusal.TransitionNotInTable, refused.Reason);
    }

    /// <summary>Every path to Published passes owner approval (decision D-007).</summary>
    [Fact]
    public void NoPathToPublishedSkipsOwnerApproval()
    {
        Assert.True(GatePredicates.EveryPathPassesOwnerApproval());
    }

    /// <summary>
    /// Plan task T-014: no route from the gate reaches a publishing platform. The passing verdict
    /// carries a token and nothing else; there is no member of the verdict union that performs a
    /// release, and exclusion X-001 removed the upload path.
    /// </summary>
    [Fact]
    public void ThePassingVerdictCarriesOnlyATokenAndNoReleasePath()
    {
        var carried = typeof(GateVerdict.Passed)
            .GetProperties()
            .Where(p => p.Name != "EqualityContract")
            .Select(p => p.PropertyType)
            .ToArray();

        // The only thing a passing gate hands back is the token. Nothing it returns can publish.
        Assert.Equal([typeof(GatePassToken)], carried);

        // And no member of the gate module names an upload, a send, or a platform.
        var forbiddenNames = new[] { "Upload", "Send", "Post", "Platform" };
        Assert.DoesNotContain(
            typeof(GatePredicates).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
            m => forbiddenNames.Any(n => m.Name.Contains(n, StringComparison.OrdinalIgnoreCase)));
    }
}
