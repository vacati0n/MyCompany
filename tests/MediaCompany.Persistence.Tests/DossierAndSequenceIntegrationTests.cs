using System.Text.Json;
using System.Text.Json.Serialization;
using MediaCompany.Application.Ports;
using MediaCompany.Application.Production;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Production;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;
using Npgsql;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The four record sources and the publishing entry point against the record store.
///
/// EVERY DEMONSTRATION RUNS AGAINST A THROWAWAY STORE THAT IT DROPS AND RECREATES, and every row a
/// demonstration writes exists only there. The recorded state each test runs over is stated in its
/// own summary, so a composition reached over demonstration state is never read as a discharged
/// precondition: the first-publication conditions recorded satisfied here are FIXTURE ROWS written
/// by the demonstration, carrying the evidence the schema requires, and nothing in production code,
/// configuration, seed data or migration records any of them. Every subject term, count and total
/// below is a DEMONSTRATION PARAMETER; none is a clip count of any audited subject.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class DossierAndSequenceIntegrationTests : IAsyncLifetime
{
    private NpgsqlDataSource? _dataSource;
    private readonly SteppingClock _clock = SteppingClock.HoursAgo(6);

    private NpgsqlDataSource Source => _dataSource
        ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    private static readonly CompanyId Company = CompanyId.New();
    private static readonly ChannelId Channel = ChannelId.New();
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(1);
    private static readonly DateTimeOffset At = DateTimeOffset.Parse("2026-09-01T09:00:00Z");

    private static readonly JsonSerializerOptions Comparable = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false,
    };

    public async Task InitializeAsync()
    {
        if (PostgresIntegrationTests.ConnectionString is null)
        {
            return;
        }

        _dataSource = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString);
        await ExecuteAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        await SchemaInstaller.InstallAsync(_dataSource, CancellationToken.None);
        await ExecuteAsync(
            """
            INSERT INTO companies (company_id, name, operating_state) VALUES (@company, 'Media Company', 'building');
            INSERT INTO channels (channel_id, company_id, platform, language, registered)
                VALUES (@channel, @company, 'video-platform', 'en', false);
            INSERT INTO items (item_id, channel_id, item_version, title) VALUES (@item, @channel, 1, 'demonstration item');
            """,
            c =>
            {
                c.Parameters.AddWithValue("company", Company.Value);
                c.Parameters.AddWithValue("channel", Channel.Value);
                c.Parameters.AddWithValue("item", Item.Value);
            });
    }

    public async Task DisposeAsync()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }
    }

    // -----------------------------------------------------------------------
    // The four sources over an empty store and an opened, empty dossier
    // -----------------------------------------------------------------------

    /// <summary>
    /// Recorded state: the installed store, with no dossier opened. Each of the four sources, read
    /// through the composed analytics service, reads UNMEASURED naming the item dossier register
    /// and the item version it looked for, with no count, zero or placeholder in its place, and the
    /// stage reading evaluates no predicate and lists no refusal.
    /// </summary>
    [RequiresPostgresFact]
    public async Task OverAStoreHoldingNoDossierEachSourceReadsUnmeasuredNamingWhereItLooked()
    {
        var analytics = Analytics();

        var dossier = await analytics.ItemDossierAsync(Item, Version, CancellationToken.None);
        var stages = await analytics.StageOutcomesAsync(Item, Version, CancellationToken.None);
        var audit = await analytics.SupplyAuditAsync(Item, Version, CancellationToken.None);
        var determinations = await analytics.DeterminationsAsync(Item, Version, CancellationToken.None);

        foreach (var quantity in new[]
                 {
                     dossier.Stages, dossier.SupplyAuditEntries, dossier.DeterminationResolutions,
                     stages.Stages[0].RecordedOutcomes, audit.RecordedEntries, determinations.RecordedResolutions,
                 })
        {
            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(quantity);
            Assert.Contains("item dossier register", unmeasured.Detail, StringComparison.Ordinal);
            Assert.Contains($"item {Item} version {Version}", unmeasured.Detail, StringComparison.Ordinal);
        }

        Assert.All(stages.Stages, s => Assert.IsType<MeasurementQuantity.Unmeasured>(s.RecordedOutcomes));
        Assert.Empty(stages.Refusals);
        Assert.Empty(audit.Entries);
    }

    /// <summary>
    /// Recorded state: a dossier opened through the recorder, holding no component. Every source
    /// now reads an OBSERVED ZERO, which renders differently from the unmeasured reading above.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnOpenedDossierHoldingNoRowReadsAsAnObservedZero()
    {
        await Recorder().OpenAsync(Item, Version, WorkforceRole.Producer, CancellationToken.None);

        var analytics = Analytics();
        var dossier = await analytics.ItemDossierAsync(Item, Version, CancellationToken.None);
        var audit = await analytics.SupplyAuditAsync(Item, Version, CancellationToken.None);
        var determinations = await analytics.DeterminationsAsync(Item, Version, CancellationToken.None);

        Assert.IsType<MeasurementQuantity.ObservedZero>(dossier.Stages);
        Assert.IsType<MeasurementQuantity.ObservedZero>(dossier.TreatmentVerdicts);
        Assert.IsType<MeasurementQuantity.ObservedZero>(audit.RecordedEntries);
        Assert.IsType<MeasurementQuantity.ObservedZero>(determinations.RecordedResolutions);
        Assert.Equal(1L, await ScalarAsync<long>(
            "SELECT COUNT(*) FROM audit_entries WHERE action = @action",
            c => c.Parameters.AddWithValue("action", ItemDossierRecorder.OpenedAction)));
    }

    // -----------------------------------------------------------------------
    // The full round trip
    // -----------------------------------------------------------------------

    /// <summary>
    /// Recorded state: a dossier opened and every one of its components recorded through the
    /// recorder over the record store. The reconstituted dossier equals the recorded one in every
    /// component — including a clip count of zero, an absent count and an admissible count, a
    /// determination the delivered composition resolved not-evidenceable with its produced
    /// evidence named, and a stage recorded as failed — and the delivered publish-ready predicate
    /// gives the same verdict over both. The readings count every recorded row.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AFullDossierRoundTripsThroughTheRecordStoreUnchanged()
    {
        var recorded = FullDossier(Item, Version);
        await RecordAsync(recorded);

        var reader = new NpgsqlItemDossierReader(Source);
        var back = await reader.DossierAsync(Item, Version, CancellationToken.None);

        Assert.NotNull(back);
        Assert.Equal(Channel, back!.Channel);
        Assert.Equal(
            JsonSerializer.Serialize(recorded, Comparable),
            JsonSerializer.Serialize(back.Dossier, Comparable));

        var registrations = Array.Empty<Domain.Rights.LibraryRegistration>();
        var original = PublishReadyPredicate.Evaluate(recorded, GateState.Draft, registrations, Channel, At.AddHours(1));
        var reconstituted = PublishReadyPredicate.Evaluate(back.Dossier, GateState.Draft, registrations, Channel, At.AddHours(1));
        Assert.Equal(
            original.Refusals.Select(r => (r.Refusal, r.Detail)),
            reconstituted.Refusals.Select(r => (r.Refusal, r.Detail)));

        var reading = await Analytics().ItemDossierAsync(Item, Version, CancellationToken.None);
        Assert.Equal("3 stage outcomes", reading.Stages.Describe());
        Assert.Equal("9 treatment verdicts", reading.TreatmentVerdicts.Describe());
        Assert.Equal("2 visual provenance records", reading.Visuals.Describe());
        Assert.Equal("3 supply audit entries", reading.SupplyAuditEntries.Describe());
        Assert.Equal("4 determination resolutions", reading.DeterminationResolutions.Describe());
        Assert.Equal("1 runtime records", reading.RuntimeRecords.Describe());
    }

    /// <summary>
    /// The supply audit read back through the composed service keeps every entry in the case it
    /// was recorded in: a count of ZERO as an observed zero, an ABSENT count as unmeasured with no
    /// number in its place, and an admissible count as the count it was. The subject terms are
    /// fixture terms and the admissible count is a demonstration parameter.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ASupplyAuditCountOfZeroReadsBackAsZeroAndAnAbsentCountAsNoNumber()
    {
        await RecordAsync(FullDossier(Item, Version));

        var audit = await Analytics().SupplyAuditAsync(Item, Version, CancellationToken.None);

        Assert.Equal("3 supply audit entries", audit.RecordedEntries.Describe());
        Assert.IsType<MeasurementQuantity.ObservedZero>(audit.Entries[0].ClipCount);
        var absent = Assert.IsType<MeasurementQuantity.Unmeasured>(audit.Entries[1].ClipCount);
        Assert.DoesNotMatch("[0-9]", MeasurementQuantity.UnmeasuredPrefix(absent.Reason));
        Assert.Equal("7 clips", audit.Entries[2].ClipCount.Describe());

        Assert.Equal(0, await ScalarAsync<int>(
            "SELECT clip_count FROM dossier_supply_audit_entries WHERE requested_term = 'fixture-term-literal-none'"));
        Assert.True(await ScalarAsync<bool>(
            "SELECT clip_count IS NULL FROM dossier_supply_audit_entries WHERE requested_term = 'fixture-term-unanswered'"));
    }

    /// <summary>
    /// The determinations and the stage outcomes read back through the composed service. Each
    /// recorded determination is its own entry and an unrecorded one reads unmeasured; the stage
    /// reading runs the delivered predicate over the RECONSTITUTED dossier and lists its refusals,
    /// including the three first-publication conditions ABSENT on this store.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheDeterminationsAndTheStageOutcomesAnswerFromRecordedState()
    {
        await RecordAsync(FullDossier(Item, Version));
        var analytics = Analytics();

        var determinations = await analytics.DeterminationsAsync(Item, Version, CancellationToken.None);
        Assert.Equal("4 determination resolutions", determinations.RecordedResolutions.Describe());
        var unrecorded = determinations.Determinations.Single(d => d.Determination == ComplianceDetermination.AdvertiserSuitability);
        Assert.Null(unrecorded.RecordedOutcome);
        Assert.IsType<MeasurementQuantity.Unmeasured>(unrecorded.RecordedResolutions);
        Assert.Equal(
            DeterminationOutcome.RecordedNotEvidenceable,
            determinations.Determinations.Single(d => d.Determination == ComplianceDetermination.SyntheticMediaDisclosure).RecordedOutcome);

        var stages = await analytics.StageOutcomesAsync(Item, Version, CancellationToken.None);
        Assert.Equal(StageOutcome.Failed, stages.Stages.Single(s => s.Stage == ProductionStage.CopyrightCheck).RecordedOutcome);
        Assert.IsType<MeasurementQuantity.Unmeasured>(stages.Stages.Single(s => s.Stage == ProductionStage.Idea).RecordedOutcomes);
        Assert.Contains(stages.Refusals, r => r.Precondition == nameof(PublishReadyRefusal.StageOutcomeMissing));
        foreach (var condition in Enum.GetNames<FirstPublicationCondition>())
        {
            Assert.Contains(stages.Refusals, r => r.Precondition == condition && r.RecordedState == "Absent");
        }
    }

    /// <summary>A dossier recorded for one item version answers for that version only.</summary>
    [RequiresPostgresFact]
    public async Task ADossierAnswersForItsOwnVersionOnly()
    {
        await RecordAsync(FullDossier(Item, Version));

        var other = await Analytics().ItemDossierAsync(Item, new ItemVersion(2), CancellationToken.None);

        var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(other.Stages);
        Assert.Contains($"item {Item} version 2", unmeasured.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// The dossier reading surfaces counts and authored labels only: its members are the item
    /// version, a measurement quantity per component and the statement, so no recorded free text —
    /// and therefore no credential, secret or identifying content a component might carry — reaches
    /// a reader through it.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheDossierReadingSurfacesCountsAndAuthoredLabelsOnly()
    {
        await RecordAsync(FullDossier(Item, Version));

        var reading = await Analytics().ItemDossierAsync(Item, Version, CancellationToken.None);

        foreach (var property in typeof(ItemDossierReadModel).GetProperties())
        {
            var type = property.PropertyType;
            Assert.True(
                type == typeof(MeasurementQuantity) || type == typeof(ItemId) || type == typeof(ItemVersion)
                || property.Name == nameof(ItemDossierReadModel.Statement),
                $"{property.Name} is a {type.Name}, which is neither a quantity nor an authored label");
        }

        Assert.Equal(AnalyticsComposers.DossierStatement, reading.Statement);
    }

    // -----------------------------------------------------------------------
    // What the dossier register refuses
    // -----------------------------------------------------------------------

    /// <summary>
    /// A component recorded against a dossier never opened is refused by the datastore, and the
    /// transaction leaves NEITHER the row NOR its audit entry.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ARecordingIntoAnUnopenedDossierLeavesNeitherRowNorEntry()
    {
        var refused = await Assert.ThrowsAsync<PostgresException>(() => Recorder().RecordComponentAsync(
            Item, Version, new DossierComponent.Runtime(TimeSpan.FromMinutes(12)), WorkforceRole.Producer, CancellationToken.None));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, refused.SqlState);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM dossier_components"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM audit_entries"));
    }

    /// <summary>
    /// The table checks refuse every row shape the domain rules do not admit, and each refusal
    /// leaves no audit entry: a supply entry carrying a count AND an unobtained reason, a prior
    /// observation recorded in part, an evidenced determination with no evidence, and a
    /// not-evidenceable reason on an outcome that is not not-evidenceable.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheRegisterRefusesEveryRowShapeTheDomainRulesDoNotAdmit()
    {
        var recorder = Recorder();
        await recorder.OpenAsync(Item, Version, WorkforceRole.Producer, CancellationToken.None);
        var entriesAfterOpening = await ScalarAsync<long>("SELECT COUNT(*) FROM audit_entries");

        var countAndReason = new SupplyAuditEntry
        {
            RequestedTerm = "fixture-term-contradictory",
            Library = "library-one",
            Fidelity = TermFidelity.LiteralSingleToken,
            AuditedAt = At,
            Count = 2,
            UnobtainedReason = CountUnobtainedReason.LibraryReportsNone,
        };
        var wholePrior = new SupplyAuditEntry
        {
            RequestedTerm = "fixture-term-partial-prior",
            Library = "library-one",
            Fidelity = TermFidelity.Substituted,
            AuditedAt = At,
            UnobtainedReason = CountUnobtainedReason.TermNotAnsweredLiterally,
            WhatWouldObtainIt = "a literal query",
            Prior = new PriorObservation { ReportedTotal = 5, ObservedOn = new DateOnly(2026, 8, 1), Source = " ", FidelityAtObservation = TermFidelity.Unknown },
        };

        await AssertRefused(() => recorder.RecordSupplyAuditAsync(Item, Version, countAndReason, WorkforceRole.Researcher, CancellationToken.None));

        // A prior recorded whole is admitted; the check refuses one recorded in part, which the
        // writer cannot produce from the domain type, so it is asserted against the table directly.
        await AssertRefused(() => ExecuteAsync(
            """
            INSERT INTO dossier_supply_audit_entries
                (item_id, item_version, requested_term, library, fidelity, audited_at, unobtained_reason,
                 prior_reported_total, recorded_unavailable)
            VALUES (@item, 1, 'fixture-term-partial', 'library-one', 'Substituted', now(), 'TermNotAnsweredLiterally', 5, false)
            """,
            c => c.Parameters.AddWithValue("item", Item.Value)));
        await recorder.RecordSupplyAuditAsync(Item, Version, wholePrior, WorkforceRole.Researcher, CancellationToken.None);

        await AssertRefused(() => recorder.RecordDeterminationAsync(Item, Version, new DeterminationResolution
        {
            Determination = ComplianceDetermination.DuplicateDetection,
            Outcome = DeterminationOutcome.Evidenced,
            ResolvedAt = At,
        }, WorkforceRole.Producer, CancellationToken.None));

        await AssertRefused(() => recorder.RecordDeterminationAsync(Item, Version, new DeterminationResolution
        {
            Determination = ComplianceDetermination.DuplicateDetection,
            Outcome = DeterminationOutcome.Unresolved,
            ResolvedAt = At,
            NotEvidenceableReason = "a reason carried by an outcome that takes none",
        }, WorkforceRole.Producer, CancellationToken.None));

        // One opening entry and one entry for the one admitted row; nothing for any refusal.
        Assert.Equal(entriesAfterOpening + 1, await ScalarAsync<long>("SELECT COUNT(*) FROM audit_entries"));
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM dossier_supply_audit_entries"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM dossier_determinations"));
    }

    /// <summary>
    /// Every dossier row is written once: a second singleton component, a second outcome for one
    /// stage and a second opening are refused, and an update in place is refused by the datastore.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheDossierIsWrittenOnce()
    {
        var recorder = Recorder();
        await recorder.OpenAsync(Item, Version, WorkforceRole.Producer, CancellationToken.None);
        await recorder.RecordComponentAsync(Item, Version, new DossierComponent.Runtime(TimeSpan.FromMinutes(12)), WorkforceRole.Producer, CancellationToken.None);
        var stage = new StageEvidence { Stage = ProductionStage.Script, Outcome = StageOutcome.Succeeded, Summary = "committed", RecordedAt = At };
        await recorder.RecordStageAsync(Item, Version, stage, WorkforceRole.Producer, CancellationToken.None);

        var second = await Assert.ThrowsAsync<PostgresException>(() => recorder.RecordComponentAsync(
            Item, Version, new DossierComponent.Runtime(TimeSpan.FromMinutes(13)), WorkforceRole.Producer, CancellationToken.None));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, second.SqlState);

        var again = await Assert.ThrowsAsync<PostgresException>(() =>
            recorder.RecordStageAsync(Item, Version, stage with { Outcome = StageOutcome.Failed }, WorkforceRole.Producer, CancellationToken.None));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, again.SqlState);

        await Assert.ThrowsAsync<PostgresException>(() => recorder.OpenAsync(Item, Version, WorkforceRole.Producer, CancellationToken.None));

        var amended = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteAsync("UPDATE dossier_stage_evidence SET outcome = 'Failed'"));
        Assert.Contains("written once", amended.MessageText, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // The publishing entry point
    // -----------------------------------------------------------------------

    /// <summary>
    /// Recorded state: the COMPANY'S REAL POSITION — no owner approval, no gate transition and no
    /// first-publication condition recorded. The drive comes to rest refused at gate evaluation:
    /// one refused attempt naming the composer's reason and the gate's detail, no dispatch record,
    /// the unit escalated to the dead claim state with every stage row it opened closed, and
    /// nothing recorded about approval or any condition.
    /// </summary>
    [RequiresPostgresFact]
    public async Task OverTheCompanysRealPositionEveryDriveIsRefusedAtGateEvaluation()
    {
        var result = await Sequence().DriveAsync(Request(), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.RefusedAtGateEvaluation, result.Rest);
        Assert.Equal("GateRefused", await ScalarAsync<string>("SELECT refusal_reason FROM publication_attempts WHERE refused"));
        Assert.Contains("TransitionNotInTable", await ScalarAsync<string>("SELECT refusal_detail FROM publication_attempts"), StringComparison.Ordinal);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM publication_dispatches"));
        Assert.Equal("Dead", await JobStateAsync(result.Unit));
        Assert.Equal("PublishingGateEvaluation", await ScalarAsync<string>(
            "SELECT position FROM jobs WHERE job_id = @job", c => c.Parameters.AddWithValue("job", result.Unit.Value)));
        Assert.Equal(0L, await PendingStageRowsAsync(result.Unit));
        Assert.Equal(4, await StageRowsAsync(result.Unit));

        foreach (var table in new[] { "gate_transitions", "approvals", "first_publication_conditions" })
        {
            Assert.Equal(0L, await ScalarAsync<long>($"SELECT COUNT(*) FROM {table}"));
        }
    }

    /// <summary>
    /// THE KNOWN ISSUE, OBSERVED. Presenting a Draft item for owner approval asks for a transition
    /// the table does not hold, and on the record store the delivered gate writer refuses it, so
    /// nothing is recorded. The entry point depends on neither this edge nor the awaiting-rights-
    /// check state no delivered code writes; the demonstrations below write that state as a
    /// fixture row instead.
    /// </summary>
    [RequiresPostgresFact]
    public async Task PresentingADraftItemIsRefusedByTheDeliveredGateWriter()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Gate().PresentForOwnerApprovalAsync(Item, Version, CancellationToken.None));

        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM gate_transitions"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM audit_entries"));
    }

    /// <summary>
    /// Recorded state: the owner verdict recorded through the delivered gate service over a fixture
    /// gate state, and NO condition observed. The refusal names all three conditions, each ABSENT.
    /// </summary>
    [RequiresPostgresFact]
    public async Task WithTheOwnerVerdictRecordedAndNoConditionTheRefusalNamesAllThree()
    {
        await RecordOwnerApprovalFixtureAsync();

        var result = await Sequence().DriveAsync(Request(), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.RefusedAtGateEvaluation, result.Rest);
        Assert.Equal("FirstPublicationConditionUnmet", await ScalarAsync<string>("SELECT refusal_reason FROM publication_attempts"));
        var detail = await ScalarAsync<string>("SELECT refusal_detail FROM publication_attempts");
        foreach (var condition in Enum.GetNames<FirstPublicationCondition>())
        {
            Assert.Contains($"{condition} is NOT satisfied: NO observation is recorded", detail, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Recorded state: the owner verdict, and fixture condition rows with the named condition
    /// recorded NOT SATISFIED and the other two satisfied. The refusal names that condition
    /// individually and no other.
    /// </summary>
    [RequiresPostgresTheory]
    [InlineData(FirstPublicationCondition.LibraryRegistration)]
    [InlineData(FirstPublicationCondition.PaymentAccount)]
    [InlineData(FirstPublicationCondition.TwoStepVerification)]
    public async Task EachConditionUnsetInTurnIsNamedIndividually(FirstPublicationCondition unset)
    {
        await RecordOwnerApprovalFixtureAsync();
        foreach (var condition in Enum.GetValues<FirstPublicationCondition>())
        {
            await RecordConditionFixtureAsync(condition, condition == unset ? ConditionState.NotSatisfied : ConditionState.Satisfied);
        }

        var result = await Sequence().DriveAsync(Request(), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.RefusedAtGateEvaluation, result.Rest);
        var detail = await ScalarAsync<string>("SELECT refusal_detail FROM publication_attempts");
        Assert.Contains($"{unset} is NOT satisfied: observed false", detail, StringComparison.Ordinal);
        foreach (var other in Enum.GetValues<FirstPublicationCondition>().Where(c => c != unset))
        {
            Assert.DoesNotContain($"{other} is NOT satisfied", detail, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Recorded state: the owner verdict, and a fixture condition row recording UNKNOWN beside two
    /// satisfied ones. Unknown folds to not-satisfied and stays visible as unknown in the refusal.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AConditionRecordedUnknownRefusesAndStaysVisible()
    {
        await RecordOwnerApprovalFixtureAsync();
        await RecordConditionFixtureAsync(FirstPublicationCondition.LibraryRegistration, ConditionState.Satisfied);
        await RecordConditionFixtureAsync(FirstPublicationCondition.PaymentAccount, ConditionState.Satisfied);
        await RecordConditionFixtureAsync(FirstPublicationCondition.TwoStepVerification, ConditionState.Unknown);

        var result = await Sequence().DriveAsync(Request(), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.RefusedAtGateEvaluation, result.Rest);
        Assert.Contains("TwoStepVerification is NOT satisfied: recorded UNKNOWN", result.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// Recorded state: THE DEMONSTRATION FIXTURE STORE the Planning Gate admits — a store this test
    /// drops, the owner verdict recorded through the delivered gate service, the three conditions
    /// recorded satisfied only as fixture rows carrying evidence, and an item with no asset rows.
    /// The drive passes every declared position in order and comes to rest at composition: one
    /// dispatch record, the unit at the terminal claim state, every stage row closed, the
    /// composition row succeeded with its leaving instant, one completion entry, and no gate
    /// transition written by the drive. Nothing is published: the descriptor is at rest and no
    /// position follows composition.
    /// </summary>
    [RequiresPostgresFact]
    public async Task OverTheFixtureStoreTheDriveComposesAndStopsAtTheTerminalPosition()
    {
        await RecordOwnerApprovalFixtureAsync();
        foreach (var condition in Enum.GetValues<FirstPublicationCondition>())
        {
            await RecordConditionFixtureAsync(condition, ConditionState.Satisfied);
        }

        var transitionsBefore = await ScalarAsync<long>("SELECT COUNT(*) FROM gate_transitions");
        var result = await Sequence().DriveAsync(Request(), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.Composed, result.Rest);
        Assert.Equal(PublishingWorkflow.Definition.Stages, result.PositionsDriven);
        Assert.Null(PublishingWorkflow.Definition.Next(result.RestingPosition));

        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM publication_dispatches"));
        Assert.Equal("Done", await JobStateAsync(result.Unit));
        Assert.Equal("Completed", await ScalarAsync<string>(
            "SELECT position FROM jobs WHERE job_id = @job", c => c.Parameters.AddWithValue("job", result.Unit.Value)));
        Assert.Equal(0L, await PendingStageRowsAsync(result.Unit));
        Assert.Equal(PublishingWorkflow.Definition.Stages.Count, await StageRowsAsync(result.Unit));
        Assert.True(await ScalarAsync<bool>(
            "SELECT outcome = 'Succeeded' AND left_at IS NOT NULL FROM job_stages WHERE job_id = @job AND position = 'PublishingComposed'",
            c => c.Parameters.AddWithValue("job", result.Unit.Value)));
        Assert.Equal(1L, await ScalarAsync<long>(
            "SELECT COUNT(*) FROM audit_entries WHERE action = @action AND subject LIKE @subject",
            c =>
            {
                c.Parameters.AddWithValue("action", LifecycleActions.Completed);
                c.Parameters.AddWithValue("subject", $"job:{result.Unit}%");
            }));

        Assert.Equal(transitionsBefore, await ScalarAsync<long>("SELECT COUNT(*) FROM gate_transitions"));
        Assert.Equal(GateState.Approved, await new NpgsqlGateLedger(Source).CurrentStateAsync(Item, Version, CancellationToken.None));
    }

    /// <summary>
    /// Recorded state: the fixture store after one composed drive. A second drive of the same item
    /// version meets the record already there: no second dispatch record, the second unit at rest
    /// escalated at composition naming the existing record, and no second completion entry.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ASecondDriveMeetsTheRecordAlreadyThereAndWritesNoSecond()
    {
        await RecordOwnerApprovalFixtureAsync();
        foreach (var condition in Enum.GetValues<FirstPublicationCondition>())
        {
            await RecordConditionFixtureAsync(condition, ConditionState.Satisfied);
        }

        await Sequence().DriveAsync(Request(), CancellationToken.None);
        var second = await Sequence().DriveAsync(Request(), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.DispatchAlreadyRecorded, second.Rest);
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM publication_dispatches"));
        Assert.Equal("Dead", await JobStateAsync(second.Unit));
        Assert.Equal(0L, await PendingStageRowsAsync(second.Unit));
        Assert.Equal(1L, await ScalarAsync<long>(
            "SELECT COUNT(*) FROM audit_entries WHERE action = @action",
            c => c.Parameters.AddWithValue("action", LifecycleActions.Completed)));
    }

    // -----------------------------------------------------------------------
    // Harness
    // -----------------------------------------------------------------------

    private ItemDossierRecorder Recorder() => new(new NpgsqlUnitOfWork(Source, _clock), _clock);

    private PublicationGateService Gate() =>
        new(new NpgsqlGateLedger(Source), new NpgsqlAssetLedger(Source), new NpgsqlUnitOfWork(Source, _clock), _clock);

    private PublishingSequenceService Sequence()
    {
        var unitOfWork = new NpgsqlUnitOfWork(Source, _clock);
        return new PublishingSequenceService(
            new WorkLifecycleService(unitOfWork, _clock),
            Gate(),
            new PublicationDispatchService(unitOfWork, _clock),
            _clock);
    }

    private AnalyticsReportService Analytics()
    {
        var costs = new NpgsqlCostReader(Source);
        return new AnalyticsReportService(
            costs, costs, new NpgsqlRevenueParameterRegister(Source), new NpgsqlThroughputReader(Source),
            new NpgsqlItemDossierReader(Source), new NpgsqlGateLedger(Source), new NpgsqlAssetLedger(Source), _clock);
    }

    private static PublishingSequenceRequest Request() => new()
    {
        Item = Item,
        Version = Version,
        Channel = Channel,
        Subject = new DeclaredSubject
        {
            Subject = "deep-sea cephalopod physiology",
            Pillar = "physiology",
            Format = "long-form documentary",
            SubjectTerms = ["Architeuthis dux"],
            Libraries = ["library-one"],
        },
        Destination = new DestinationDescriptor("a-video-platform", "the-company-channel", "en-GB"),
        TreatmentConditions = Enum.GetValues<TreatmentCondition>(),
        SurfaceDraft = new SurfaceDraft
        {
            Title = "Chromatophore density in Architeuthis dux",
            Description = "A quantified account of chromatophore density, with named attribution.",
            Tags = ["cephalopod", "physiology", "marine biology"],
            ThumbnailReference = "thumbnail-variant-3",
            Captions = "Chromatophore density varies with depth.",
            AdultSignallingElement = "on-screen mortality statistics at 04:12",
            AudienceReasoning = "not made for kids: the item carries mortality statistics and a scientific register",
            SubjectTermsUsed = ["Architeuthis dux"],
        },
        Settings = new PublicationSettings
        {
            Visibility = "unlisted",
            MadeForKids = false,
            CommentPolicy = "held-for-review",
            CategoryName = "science-and-technology",
        },
        TimingPolicy = new PublicationTiming.TimingPolicy { LeadDays = 3, PreferredTimeOfDay = new TimeOnly(14, 0) },
    };

    /// <summary>
    /// The owner verdict, recorded through the DELIVERED gate service as the state the transition
    /// table holds. The awaiting-rights-check state it is presented from is written first as a
    /// demonstration fixture row, because no delivered code writes that state; the fixture goes
    /// through the delivered gate writer, which admits only transitions the table holds.
    /// </summary>
    private async Task RecordOwnerApprovalFixtureAsync()
    {
        await using (var transaction = await new NpgsqlUnitOfWork(Source, _clock).BeginAsync(CancellationToken.None))
        {
            await transaction.Gates.RecordTransitionAsync(
                Item, Version, GateState.Draft, GateState.AwaitingRightsCheck,
                "demonstration fixture: the rights check is recorded as reached in this throwaway store",
                _clock.UtcNow, CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var gate = Gate();
        var presentedAt = await gate.PresentForOwnerApprovalAsync(Item, Version, CancellationToken.None);
        await gate.RecordOwnerVerdictAsync(
            Item, Version, ApprovalVerdict.Approved, "demonstration fixture verdict", presentedAt, CancellationToken.None);
    }

    /// <summary>
    /// One fixture condition row, carrying the evidence the schema requires and naming itself a
    /// fixture. It exists only in this throwaway store.
    /// </summary>
    private async Task RecordConditionFixtureAsync(FirstPublicationCondition condition, ConditionState state) =>
        await ExecuteAsync(
            """
            INSERT INTO first_publication_conditions (channel_id, condition, state, evidence, observed_on)
            VALUES (@channel, @condition, @state, 'demonstration fixture row in a throwaway store; discharges nothing', CURRENT_DATE)
            """,
            c =>
            {
                c.Parameters.AddWithValue("channel", Channel.Value);
                c.Parameters.AddWithValue("condition", condition.ToString());
                c.Parameters.AddWithValue("state", state.ToString());
            });

    private async Task RecordAsync(ItemDossier dossier)
    {
        var recorder = Recorder();
        var ct = CancellationToken.None;
        await recorder.OpenAsync(dossier.Item, dossier.Version, WorkforceRole.Producer, ct);

        foreach (var stage in dossier.Stages)
        {
            await recorder.RecordStageAsync(dossier.Item, dossier.Version, stage, WorkforceRole.Producer, ct);
        }

        foreach (var verdict in dossier.Treatment)
        {
            await recorder.RecordComponentAsync(dossier.Item, dossier.Version, new DossierComponent.Treatment(verdict), WorkforceRole.Producer, ct);
        }

        await recorder.RecordComponentAsync(dossier.Item, dossier.Version, new DossierComponent.Audience(dossier.Audience!), WorkforceRole.Producer, ct);

        foreach (var visual in dossier.Visuals)
        {
            await recorder.RecordComponentAsync(dossier.Item, dossier.Version, new DossierComponent.Visual(visual), WorkforceRole.Producer, ct);
        }

        foreach (var clip in dossier.ClipOrigins)
        {
            await recorder.RecordComponentAsync(dossier.Item, dossier.Version, new DossierComponent.ClipOrigin(clip), WorkforceRole.Producer, ct);
        }

        foreach (var claim in dossier.Claims)
        {
            await recorder.RecordComponentAsync(dossier.Item, dossier.Version, new DossierComponent.Claim(claim), WorkforceRole.Researcher, ct);
        }

        foreach (var entry in dossier.SupplyAudit)
        {
            await recorder.RecordSupplyAuditAsync(dossier.Item, dossier.Version, entry, WorkforceRole.Researcher, ct);
        }

        foreach (var resolution in dossier.Determinations)
        {
            await recorder.RecordDeterminationAsync(dossier.Item, dossier.Version, resolution, WorkforceRole.Producer, ct);
        }

        await recorder.RecordComponentAsync(dossier.Item, dossier.Version, new DossierComponent.Runtime(dossier.Runtime!.Value), WorkforceRole.Producer, ct);
        await recorder.RecordComponentAsync(dossier.Item, dossier.Version, new DossierComponent.Metadata(dossier.Metadata!), WorkforceRole.Producer, ct);
        await recorder.RecordComponentAsync(dossier.Item, dossier.Version, new DossierComponent.Originality(dossier.Originality!), WorkforceRole.Producer, ct);
    }

    /// <summary>
    /// A dossier with every component recorded. The subject terms are fixture terms and every count
    /// and total is a demonstration parameter.
    /// </summary>
    private static ItemDossier FullDossier(ItemId item, ItemVersion version)
    {
        var notEvidenceable = EvidenceComposition.Resolve(
            ComplianceDetermination.SyntheticMediaDisclosure,
            [
                new EvidenceComponent("internal assessment", DeterminationOutcome.Evidenced, "every generated element assessed", string.Empty, string.Empty),
                new EvidenceComponent("platform disclosure", DeterminationOutcome.RecordedNotEvidenceable, string.Empty,
                    "the disclosure surface opens only at upload", "an upload, which nothing in this build performs"),
            ],
            At,
            "policy text reference",
            new DateOnly(2026, 8, 20));

        return new ItemDossier
        {
            Item = item,
            Version = version,
            Stages =
            [
                new StageEvidence { Stage = ProductionStage.Research, Outcome = StageOutcome.Succeeded, Summary = "research recorded", RecordedAt = At },
                new StageEvidence { Stage = ProductionStage.Script, Outcome = StageOutcome.Succeeded, Summary = "script committed", RecordedAt = At.AddMinutes(30) },
                new StageEvidence
                {
                    Stage = ProductionStage.CopyrightCheck,
                    Outcome = StageOutcome.Failed,
                    Summary = "a visual lacked a recorded licence",
                    RecordedAt = At.AddHours(2),
                    EvidenceReference = "copyright-check-log-1",
                },
            ],
            Treatment = Enum.GetValues<TreatmentCondition>().Select(condition => new TreatmentVerdict
            {
                Condition = condition,
                Kind = condition == TreatmentCondition.AudienceShareMonitoring
                    ? TreatmentVerdictKind.StandingObligation
                    : TreatmentVerdictKind.Passed,
                Evidence = $"resisted by the recorded treatment of {condition}",
                RecordedAt = At.AddMinutes(40),
                AwaitedParameter = condition == TreatmentCondition.AudienceShareMonitoring ? "the age-demographic report" : null,
            }).ToArray(),
            Audience = new AudienceDesignation { MadeForKids = false, Reasoning = "a scientific register for adults", RecordedAt = At.AddMinutes(41) },
            Visuals =
            [
                new VisualProvenance
                {
                    VisualReference = "visual-1",
                    Source = VisualSource.LicensedStock,
                    RecordedAt = At.AddMinutes(50),
                    Asset = new AssetId(Guid.Parse("0f0e0d0c-0b0a-0908-0706-050403020100")),
                    LicenceReference = "licence-1",
                },
                new VisualProvenance
                {
                    VisualReference = "visual-2",
                    Source = VisualSource.GeneratedCutaway,
                    RecordedAt = At.AddMinutes(51),
                    CutawayReason = new GeneratedCutawayReason
                    {
                        WhyNoLicensedAssetServed = "no licensed asset depicts the process",
                        AlternativesConsidered = ["an original diagram", "a held still"],
                        RecordedAt = At.AddMinutes(51),
                    },
                },
            ],
            ClipOrigins =
            [
                new ClipOriginAssessment
                {
                    ClipReference = "clip-1",
                    Origin = ClipOrigin.RecordedFootage,
                    Basis = "the library's provenance record",
                    AssessedAt = At.AddMinutes(55),
                    UsedAt = At.AddMinutes(90),
                },
            ],
            Claims =
            [
                new ClaimAttribution { ClaimId = "claim-1", Claim = "chromatophore density varies with depth", Source = "a named paper", RecordedAt = At.AddMinutes(20) },
            ],
            SupplyAudit =
            [
                new SupplyAuditEntry
                {
                    RequestedTerm = "fixture-term-literal-none",
                    Library = "library-one",
                    Fidelity = TermFidelity.LiteralSingleToken,
                    AuditedAt = At.AddMinutes(5),
                    TermAnswered = "fixture-term-literal-none",
                    Count = 0,
                },
                new SupplyAuditEntry
                {
                    RequestedTerm = "fixture-term-unanswered",
                    Library = "library-one",
                    Fidelity = TermFidelity.Substituted,
                    AuditedAt = At.AddMinutes(6),
                    TermAnswered = "a different term",
                    UnobtainedReason = CountUnobtainedReason.TermNotAnsweredLiterally,
                    WhatWouldObtainIt = "a literal single-token query",
                    Prior = new PriorObservation
                    {
                        ReportedTotal = 4,
                        ObservedOn = new DateOnly(2026, 8, 1),
                        Source = "an earlier recorded read",
                        FidelityAtObservation = TermFidelity.Substituted,
                    },
                },
                new SupplyAuditEntry
                {
                    RequestedTerm = "fixture-term-literal-count",
                    Library = "library-one",
                    Fidelity = TermFidelity.LiteralSingleToken,
                    AuditedAt = At.AddMinutes(7),
                    TermAnswered = "fixture-term-literal-count",
                    Count = 7,
                },
            ],
            Determinations =
            [
                new DeterminationResolution
                {
                    Determination = ComplianceDetermination.DuplicateDetection,
                    Outcome = DeterminationOutcome.Evidenced,
                    ResolvedAt = At.AddHours(1),
                    Evidence = "distinct at the recorded distance",
                },
                notEvidenceable,
                new DeterminationResolution
                {
                    Determination = ComplianceDetermination.AffiliateAndPaidPromotion,
                    Outcome = DeterminationOutcome.Failed,
                    ResolvedAt = At.AddHours(1),
                    Evidence = "a paid placement was found undisclosed",
                },
                new DeterminationResolution
                {
                    Determination = ComplianceDetermination.OriginalitySelfAssessment,
                    Outcome = DeterminationOutcome.Unresolved,
                    ResolvedAt = At.AddHours(1),
                },
            ],
            Runtime = new TimeSpan(0, 12, 30),
            Metadata = new ItemMetadata
            {
                Title = "Chromatophore density in Architeuthis dux",
                Description = "A quantified account.",
                Tags = ["cephalopod", "physiology"],
            },
            Originality = new FootageRemovalAssessment
            {
                ClaimsTotal = 1,
                ClaimsCarriedWithoutFootage = 1,
                Judgement = "the argument survives with the licensed footage set aside",
                AssessedAt = At.AddHours(1),
            },
        };
    }

    private async Task AssertRefused(Func<Task> write)
    {
        var refused = await Assert.ThrowsAsync<PostgresException>(write);
        Assert.Equal(PostgresErrorCodes.CheckViolation, refused.SqlState);
    }

    private Task<string> JobStateAsync(JobId job) =>
        ScalarAsync<string>("SELECT claim_state FROM jobs WHERE job_id = @job", c => c.Parameters.AddWithValue("job", job.Value));

    private Task<long> PendingStageRowsAsync(JobId job) =>
        ScalarAsync<long>(
            "SELECT COUNT(*) FROM job_stages WHERE job_id = @job AND outcome = 'Pending'",
            c => c.Parameters.AddWithValue("job", job.Value));

    private async Task<int> StageRowsAsync(JobId job) =>
        (int)await ScalarAsync<long>(
            "SELECT COUNT(*) FROM job_stages WHERE job_id = @job",
            c => c.Parameters.AddWithValue("job", job.Value));

    private async Task ExecuteAsync(string sql, Action<NpgsqlCommand>? configure = null)
    {
        await using var command = Source.CreateCommand(sql);
        configure?.Invoke(command);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<T> ScalarAsync<T>(string sql, Action<NpgsqlCommand>? configure = null)
    {
        await using var command = Source.CreateCommand(sql);
        configure?.Invoke(command);
        return (T)(await command.ExecuteScalarAsync())!;
    }
}

/// <summary>A theory that is recorded as not-run, rather than predicted, when no datastore is reachable.</summary>
public sealed class RequiresPostgresTheoryAttribute : TheoryAttribute
{
    public RequiresPostgresTheoryAttribute()
    {
        if (PostgresIntegrationTests.ConnectionString is null)
        {
            Skip = "not-run: no PostgreSQL datastore is reachable. Set MEDIACOMPANY_TEST_CONNECTION_STRING to run.";
        }
    }
}
