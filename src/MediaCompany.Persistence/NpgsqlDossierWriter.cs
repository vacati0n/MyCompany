using System.Text.Json;
using System.Text.Json.Serialization;
using MediaCompany.Application.Ports;
using MediaCompany.Domain.Dossier;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// The item dossier's writer, on the calling transaction's own connection (decision D-004 of the
/// accepted design).
///
/// Every write is an INSERT into a write-once table and nothing else: a second opening, a second
/// outcome for one stage, a second singleton component, a component against a dossier that was
/// never opened and a row the table checks refuse all fail the transaction, so the component and
/// its audit entry either both commit or neither does.
/// </summary>
internal sealed class NpgsqlDossierWriter : IDossierWriter
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    internal NpgsqlDossierWriter(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task OpenAsync(ItemId item, ItemVersion version, DateTimeOffset openedAt, CancellationToken cancellationToken)
    {
        await using var command = Command(
            """
            INSERT INTO item_dossiers (item_id, item_version, opened_at)
            VALUES (@item_id, @item_version, @opened_at)
            """,
            item,
            version);

        command.Parameters.AddWithValue("opened_at", openedAt);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordStageAsync(
        ItemId item,
        ItemVersion version,
        StageEvidence evidence,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        await using var command = Command(
            """
            INSERT INTO dossier_stage_evidence
                (item_id, item_version, stage, outcome, summary, recorded_at, evidence_reference)
            VALUES (@item_id, @item_version, @stage, @outcome, @summary, @recorded_at, @evidence_reference)
            """,
            item,
            version);

        command.Parameters.AddWithValue("stage", evidence.Stage.ToString());
        command.Parameters.AddWithValue("outcome", evidence.Outcome.ToString());
        command.Parameters.AddWithValue("summary", evidence.Summary);
        command.Parameters.AddWithValue("recorded_at", evidence.RecordedAt);
        Text(command, "evidence_reference", evidence.EvidenceReference);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordSupplyAuditAsync(
        ItemId item,
        ItemVersion version,
        SupplyAuditEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using var command = Command(
            """
            INSERT INTO dossier_supply_audit_entries
                (item_id, item_version, requested_term, library, fidelity, audited_at, term_answered,
                 clip_count, unobtained_reason, what_would_obtain_it,
                 prior_reported_total, prior_observed_on, prior_source, prior_fidelity, recorded_unavailable)
            VALUES (@item_id, @item_version, @requested_term, @library, @fidelity, @audited_at, @term_answered,
                    @clip_count, @unobtained_reason, @what_would_obtain_it,
                    @prior_reported_total, @prior_observed_on, @prior_source, @prior_fidelity, @recorded_unavailable)
            """,
            item,
            version);

        command.Parameters.AddWithValue("requested_term", entry.RequestedTerm);
        command.Parameters.AddWithValue("library", entry.Library);
        command.Parameters.AddWithValue("fidelity", entry.Fidelity.ToString());
        command.Parameters.AddWithValue("audited_at", entry.AuditedAt);
        Text(command, "term_answered", entry.TermAnswered);

        // The count EXACTLY as the entry holds it. A zero is written as zero and an absent count as
        // absent: the two are different facts, and writing either as the other is the collapse the
        // previous change had to correct in the revenue register.
        command.Parameters.Add("clip_count", NpgsqlDbType.Integer).Value = (object?)entry.Count ?? DBNull.Value;

        command.Parameters.AddWithValue("unobtained_reason", entry.UnobtainedReason.ToString());
        Text(command, "what_would_obtain_it", entry.WhatWouldObtainIt);
        command.Parameters.Add("prior_reported_total", NpgsqlDbType.Integer).Value =
            (object?)entry.Prior?.ReportedTotal ?? DBNull.Value;
        command.Parameters.Add("prior_observed_on", NpgsqlDbType.Date).Value =
            (object?)entry.Prior?.ObservedOn ?? DBNull.Value;
        Text(command, "prior_source", entry.Prior?.Source);
        Text(command, "prior_fidelity", entry.Prior?.FidelityAtObservation.ToString());
        command.Parameters.AddWithValue("recorded_unavailable", entry.RecordedUnavailable);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordDeterminationAsync(
        ItemId item,
        ItemVersion version,
        DeterminationResolution resolution,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        await using var command = Command(
            """
            INSERT INTO dossier_determinations
                (item_id, item_version, determination, outcome, resolved_at, evidence,
                 not_evidenceable_reason, what_would_make_it_evidenceable, policy_reference, policy_verified_on)
            VALUES (@item_id, @item_version, @determination, @outcome, @resolved_at, @evidence,
                    @not_evidenceable_reason, @what_would_make_it_evidenceable, @policy_reference, @policy_verified_on)
            """,
            item,
            version);

        command.Parameters.AddWithValue("determination", resolution.Determination.ToString());
        command.Parameters.AddWithValue("outcome", resolution.Outcome.ToString());
        command.Parameters.AddWithValue("resolved_at", resolution.ResolvedAt);
        Text(command, "evidence", resolution.Evidence);
        Text(command, "not_evidenceable_reason", resolution.NotEvidenceableReason);
        Text(command, "what_would_make_it_evidenceable", resolution.WhatWouldMakeItEvidenceable);
        Text(command, "policy_reference", resolution.PolicyReference);
        command.Parameters.Add("policy_verified_on", NpgsqlDbType.Date).Value =
            (object?)resolution.PolicyVerifiedOn ?? DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordComponentAsync(
        ItemId item,
        ItemVersion version,
        DossierComponent component,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(component);

        await using var command = Command(
            """
            INSERT INTO dossier_components (item_id, item_version, kind, recorded_at, payload)
            VALUES (@item_id, @item_version, @kind, @recorded_at, @payload)
            """,
            item,
            version);

        command.Parameters.AddWithValue("kind", component.Kind.ToString());
        command.Parameters.AddWithValue("recorded_at", recordedAt);
        command.Parameters.Add("payload", NpgsqlDbType.Jsonb).Value = DossierPayload.Serialize(component);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private NpgsqlCommand Command(string sql, ItemId item, ItemVersion version)
    {
        var command = new NpgsqlCommand(sql, _connection, _transaction);
        command.Parameters.AddWithValue("item_id", item.Value);
        command.Parameters.AddWithValue("item_version", version.Value);
        return command;
    }

    private static void Text(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Text).Value = (object?)value ?? DBNull.Value;
}

/// <summary>
/// The one structured payload rule, for writing and reading alike, so the two cannot disagree.
///
/// Closed values are held BY NAME rather than by ordinal, so a reordered enumeration does not
/// silently change what a recorded row means, and a renamed value fails to read rather than
/// reading as another. Only the record's own settable members are held; members computed from
/// them are recomputed on the way back rather than stored beside them.
/// </summary>
internal static class DossierPayload
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
        IgnoreReadOnlyProperties = true,
    };

    /// <summary>The runtime payload, which is a bare duration and needs an object to be one.</summary>
    internal sealed record RuntimePayload(TimeSpan Runtime);

    internal static string Serialize(DossierComponent component) => component switch
    {
        DossierComponent.Treatment c => JsonSerializer.Serialize(c.Verdict, Options),
        DossierComponent.Audience c => JsonSerializer.Serialize(c.Designation, Options),
        DossierComponent.Visual c => JsonSerializer.Serialize(c.Provenance, Options),
        DossierComponent.ClipOrigin c => JsonSerializer.Serialize(c.Assessment, Options),
        DossierComponent.Claim c => JsonSerializer.Serialize(c.Attribution, Options),
        DossierComponent.Metadata c => JsonSerializer.Serialize(c.Surfaces, Options),
        DossierComponent.Originality c => JsonSerializer.Serialize(c.Assessment, Options),
        DossierComponent.Runtime c => JsonSerializer.Serialize(new RuntimePayload(c.Duration), Options),
        _ => throw new InvalidOperationException("Unreachable: the dossier component union has eight cases."),
    };

    internal static DossierComponent Deserialize(DossierComponentKind kind, string payload) => kind switch
    {
        DossierComponentKind.TreatmentVerdict => new DossierComponent.Treatment(Read<TreatmentVerdict>(payload)),
        DossierComponentKind.AudienceDesignation => new DossierComponent.Audience(Read<AudienceDesignation>(payload)),
        DossierComponentKind.VisualProvenance => new DossierComponent.Visual(Read<VisualProvenance>(payload)),
        DossierComponentKind.ClipOriginAssessment => new DossierComponent.ClipOrigin(Read<ClipOriginAssessment>(payload)),
        DossierComponentKind.ClaimAttribution => new DossierComponent.Claim(Read<ClaimAttribution>(payload)),
        DossierComponentKind.ItemMetadata => new DossierComponent.Metadata(Read<ItemMetadata>(payload)),
        DossierComponentKind.OriginalityAssessment => new DossierComponent.Originality(Read<FootageRemovalAssessment>(payload)),
        DossierComponentKind.Runtime => new DossierComponent.Runtime(Read<RuntimePayload>(payload).Runtime),
        _ => throw new InvalidOperationException($"Unreachable: '{kind}' is not a dossier component kind."),
    };

    private static T Read<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, Options)
        ?? throw new InvalidOperationException($"A recorded {typeof(T).Name} payload read back as nothing.");
}
