using MediaCompany.Application.Ports;
using MediaCompany.Application.Production;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;

namespace MediaCompany.Deterministic.Services;

/// <summary>Where one run of the rights-check step came to rest.</summary>
public enum RightsCheckRest
{
    /// <summary>The rights check was releasable and the item version was presented for owner approval.</summary>
    PresentedForOwnerApproval = 1,

    /// <summary>The rights check was not releasable and the item version was sent back.</summary>
    SentBack = 2,

    /// <summary>A gate step was refused by name; the outcome carries the refusal.</summary>
    Refused = 3,
}

/// <summary>
/// What one run of the rights-check step did, and every gate step it took. <see cref="Evidence"/> is
/// the copyright-check outcome the run decided on, or null where no check ran and none is recorded: a
/// check that never ran reads absent, never as an outcome at an invented instant.
/// </summary>

public sealed record RightsCheckResult(
    RightsCheckRest Rest,
    ChannelId Channel,
    Domain.Dossier.StageEvidence? Evidence,
    bool EvidenceRecordedByThisRun,
    IReadOnlyList<GateStepOutcome> GateSteps);

/// <summary>
/// The rights-check step (the multi-channel change, decision D-009 of its design): the dossier
/// recorder's first production caller, and the production path from Draft through the rights check
/// to owner approval on real recorded state.
///
/// It carries a new item version: it opens the dossier through the delivered recorder where none is
/// opened, submits the version for the rights check through the gate service, runs the copyright-check
/// stage handler, records the handler's evidence through the recorder, and then presents the version
/// for owner approval where the evidence is releasable or sends it back where it is not.
///
/// EVERY STEP READS RECORDED STATE FIRST, so a run interrupted between two of its writes resumes from
/// what was recorded rather than duplicating a write-once row: an opened dossier is not reopened, a
/// version already awaiting the rights check is not resubmitted, and recorded copyright-check
/// evidence is reused rather than re-recorded. The item's channel is read from the recorded dossier,
/// never accepted from a caller.
///
/// It is rule-determined work in the rule-determined assembly. It performs no metered operation,
/// reads no configuration value, and touches nothing in the publishing sequence, so it adds no
/// position after composition.
/// </summary>
public sealed class RightsCheckStep
{
    /// <summary>The role the step records and submits as. It holds the approval-presentation action.</summary>
    public const WorkforceRole Producer = WorkforceRole.Producer;

    /// <summary>The role the step sends back as. It holds the asset-verification action.</summary>
    public const WorkforceRole Copyright = WorkforceRole.Copyright;

    private readonly IItemDossierReader _dossiers;
    private readonly IGateLedger _gates;
    private readonly ItemDossierRecorder _recorder;
    private readonly PublicationGateService _gate;
    private readonly CopyrightCheckStageHandler _handler;

    public RightsCheckStep(
        IItemDossierReader dossiers,
        IGateLedger gates,
        ItemDossierRecorder recorder,
        PublicationGateService gate,
        CopyrightCheckStageHandler handler)
    {
        _dossiers = dossiers;
        _gates = gates;
        _recorder = recorder;
        _gate = gate;
        _handler = handler;
    }

    public async Task<RightsCheckResult> RunAsync(
        ItemId item,
        ItemVersion version,
        DeclaredSubject subject,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);

        // 1. The dossier: opened once, through the recorder, where none is opened yet.
        var recorded = await _dossiers.DossierAsync(item, version, cancellationToken).ConfigureAwait(false);
        if (recorded is null)
        {
            await _recorder.OpenAsync(item, version, Producer, cancellationToken).ConfigureAwait(false);
            recorded = await _dossiers.DossierAsync(item, version, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"The dossier of item {item} version {version} did not read back after it was opened.");
        }

        var steps = new List<GateStepOutcome>();

        // 2. The submission, from the recorded state. A version already awaiting the rights check
        //    resumes without a second submission; any other state is refused by the gate service.
        var state = await _gates.CurrentStateAsync(item, version, cancellationToken).ConfigureAwait(false);
        if (state != GateState.AwaitingRightsCheck)
        {
            var submitted = await _gate.SubmitForRightsCheckAsync(item, version, Producer, cancellationToken)
                .ConfigureAwait(false);
            steps.Add(submitted);

            if (submitted is GateStepOutcome.Refused)
            {
                var existing = recorded.Dossier.StageFor(ProductionStage.CopyrightCheck);
                return new RightsCheckResult(
                    RightsCheckRest.Refused, recorded.Channel,
                    existing, EvidenceRecordedByThisRun: false, steps);
            }
        }

        // 3. The stage: its recorded evidence is reused; otherwise the handler runs and its evidence
        //    is recorded through the recorder, with the entry that records it, on one transaction.
        var evidence = recorded.Dossier.StageFor(ProductionStage.CopyrightCheck);
        var recordedNow = false;
        if (evidence is null)
        {
            var result = await _handler.HandleAsync(item, version, subject, cancellationToken).ConfigureAwait(false);
            await _recorder.RecordStageAsync(item, version, result.Evidence, Copyright, cancellationToken).ConfigureAwait(false);
            evidence = result.Evidence;
            recordedNow = true;
        }

        // 4. The verdict decides the next gate step, each from the recorded state again.
        if (evidence.Outcome == StageOutcome.Succeeded)
        {
            var presented = await _gate.PresentForOwnerApprovalAsync(item, version, cancellationToken).ConfigureAwait(false);
            steps.Add(presented);

            if (presented is GateStepOutcome.Moved)
            {
                return new RightsCheckResult(RightsCheckRest.PresentedForOwnerApproval, recorded.Channel, evidence, recordedNow, steps);
            }

            // RECORDED EVIDENCE AND THE PRESENTATION DISAGREE (decision D-009, as corrected in its
            // review): the evidence read releasable, and the presentation's own rights check, taken now,
            // did not — a basis expired or an asset decision was added since. The version is sent back
            // naming the disagreement, so the step reaches the rest D-009 defines rather than refusing on
            // every later run. Any other refusal is reported as it stands.
            if (presented is GateStepOutcome.Refused { Reason: GateStepRefusal.RightsNotReleasable } disagreement)
            {
                var back = await _gate
                    .SendBackFromRightsCheckAsync(
                        item, version, Copyright,
                        $"the recorded copyright-check evidence read releasable and the rights check at presentation did not: {disagreement.Detail}",
                        cancellationToken)
                    .ConfigureAwait(false);
                steps.Add(back);

                return new RightsCheckResult(
                    back is GateStepOutcome.Moved ? RightsCheckRest.SentBack : RightsCheckRest.Refused,
                    recorded.Channel, evidence, recordedNow, steps);
            }

            return new RightsCheckResult(RightsCheckRest.Refused, recorded.Channel, evidence, recordedNow, steps);
        }

        var sentBack = await _gate
            .SendBackFromRightsCheckAsync(item, version, Copyright, evidence.Summary, cancellationToken)
            .ConfigureAwait(false);
        steps.Add(sentBack);

        return new RightsCheckResult(
            sentBack is GateStepOutcome.Moved ? RightsCheckRest.SentBack : RightsCheckRest.Refused,
            recorded.Channel, evidence, recordedNow, steps);
    }
}
