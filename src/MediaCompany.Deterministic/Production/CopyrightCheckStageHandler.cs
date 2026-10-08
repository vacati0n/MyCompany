using MediaCompany.Application.Ports;
using MediaCompany.Application.Production;
using MediaCompany.Deterministic.Rights;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Work;

namespace MediaCompany.Deterministic.Production;

/// <summary>
/// The copyright-check stage handler (the multi-channel change, decision D-009 of its design): the
/// first implementation of the delivered stage handler contract.
///
/// It is RULE-DETERMINED WORK. It reads the item's recorded assets and the registrations of the
/// item's RECORDED channel, never a channel a caller supplies, and returns stage evidence of the
/// recorded rights check's verdict. It performs no metered operation: the rule is the asset rights
/// ledger and the licence-join proof the deterministic task registry already names as zero-cost
/// work, and this assembly cannot name the capability boundary at all. It writes nothing; the
/// evidence it returns is recorded by the caller through the dossier recorder.
/// </summary>
public sealed class CopyrightCheckStageHandler : IStageHandler
{
    /// <summary>The rule-determined tasks this stage realises, by their registered names.</summary>
    public static readonly IReadOnlyList<string> TaskNames =
        [DeterministicTaskRegistry.AssetRightsLedger, DeterministicTaskRegistry.LicenceJoinProof];

    private readonly IItemDossierReader _dossiers;
    private readonly IAssetLedger _assets;
    private readonly IClock _clock;

    public CopyrightCheckStageHandler(IItemDossierReader dossiers, IAssetLedger assets, IClock clock)
    {
        _dossiers = dossiers;
        _assets = assets;
        _clock = clock;
    }

    public ProductionStage Stage => ProductionStage.CopyrightCheck;

    /// <summary>
    /// Runs the copyright check over recorded state. The declared subject is accepted as the contract
    /// requires and read for nothing: a rights verdict depends on the recorded assets and the
    /// channel's registrations alone. An item version with no opened dossier has no recorded channel
    /// to check against, which is refused rather than guessed.
    /// </summary>
    public async Task<StageResult> HandleAsync(
        ItemId item,
        ItemVersion version,
        DeclaredSubject subject,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var recorded = await _dossiers.DossierAsync(item, version, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No dossier is opened for item {item} version {version}, so it has no recorded channel to check its rights against.");

        var now = _clock.UtcNow;
        var assets = await _assets.ForItemAsync(item, cancellationToken).ConfigureAwait(false);
        var registrations = await _assets.RegistrationsAsync(recorded.Channel, cancellationToken).ConfigureAwait(false);
        var verdict = RecordedRightsCheck.Evaluate(assets, registrations, DateOnly.FromDateTime(now.UtcDateTime));

        return new StageResult
        {
            Stage = Stage,
            Evidence = new StageEvidence
            {
                Stage = Stage,
                Outcome = verdict.Releasable ? StageOutcome.Succeeded : StageOutcome.Failed,
                Summary = verdict.Releasable
                    ? $"rights releasable for channel {recorded.Channel}: {verdict.Detail}"
                    : $"rights not releasable for channel {recorded.Channel}: {verdict.Detail}",
                RecordedAt = now,
                EvidenceReference = $"assets:{assets.Count} registrations:{registrations.Count} channel:{recorded.Channel}",
            },
        };
    }
}
