using System.Diagnostics;
using System.Reflection;
using MediaCompany.Application.Ports;
using MediaCompany.Capability;
using MediaCompany.Capability.Providers;
using MediaCompany.Credentials;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Management;
using MediaCompany.Domain.Work;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The AI-management capability against the record store (the eighth schema resource and the change that comes
/// with it): the one read, the register, the held-outcome record, the re-verification record, the reports, the
/// rules, the brief and the dashboard over a live store.
///
/// EVERY DEMONSTRATION RUNS AGAINST A THROWAWAY STORE THAT IT DROPS AND RECREATES, in the separate demonstration
/// database. Every company, channel, budget amount, price row, route, operation, policy statement, re-verification
/// result, register entry beyond the resource's transcription, held request and gate state below is a
/// DEMONSTRATION FIXTURE that exists only there; nothing in production code, seed or migration records any of them.
///
/// NO METERED CALL IS MADE. Where a demonstration drives the admission path it does so through a STAND-IN provider
/// declared here, which returns a fixture attempt and reaches nothing, with a stand-in broker that holds no secret.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class ManagementIntegrationTests : IAsyncLifetime
{
    private NpgsqlDataSource? _dataSource;

    private NpgsqlDataSource Source => _dataSource
        ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    private static readonly CompanyId Company = CompanyId.New();
    private static readonly ChannelId ChannelA = ChannelId.New();
    private static readonly ChannelId ChannelB = ChannelId.New();
    private static readonly DepartmentId Department = DepartmentId.New();
    private static readonly AgentId Agent = AgentId.New();
    private static readonly ItemId ItemA = ItemId.New();
    private static readonly ItemId ItemB = ItemId.New();
    private static readonly ProviderAccountId Alpha = new("provider-alpha");
    private static readonly ModelId AlphaModel = new("alpha-reasoning");

    /// <summary>The default command timeout of the data source: thirty seconds.</summary>
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);

    /// <summary>How long a demonstration holder keeps its hold: past the read's own command timeout.</summary>
    private static readonly TimeSpan HeldPastTheReadsTimeout = TimeSpan.FromSeconds(35);

    public async Task InitializeAsync()
    {
        if (PostgresIntegrationTests.ConnectionString is null)
        {
            return;
        }

        _dataSource = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString);

        try
        {
            await ThrowawayStore.DropAsync(_dataSource);
            await SchemaInstaller.InstallAsync(_dataSource, CancellationToken.None);
            await SeedAsync();
        }
        catch
        {
            await ThrowawayStore.DropAsync(_dataSource);
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_dataSource is not null)
        {
            try
            {
                await ThrowawayStore.DropAsync(_dataSource);
            }
            finally
            {
                await _dataSource.DisposeAsync();
            }
        }
    }

    private NpgsqlCompanyRecordReader Reader() => new(Source);

    private async Task<ManagementRead> ReadAsync(NpgsqlCompanyRecordReader? reader = null) =>
        await new ManagementReportService(reader ?? Reader()).ReadAsync(null, CancellationToken.None);

    // -----------------------------------------------------------------------
    // The eighth schema resource and the register
    // -----------------------------------------------------------------------

    /// <summary>
    /// The eighth resource installs last, creates the policy records and the held-outcome record EMPTY, seeds the
    /// register with exactly its transcription — three decided owner decisions and eleven open questions, each citing
    /// the record it is transcribed from — widens the reading columns, and applies a second time without changing an
    /// entry.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheEighthResourceSeedsTheRegisterRecordsNothingElseAndReappliesCleanly()
    {
        // The ninth resource (the production change) now installs last; the eighth is still the eighth, and its
        // transcription is asserted below over the entries IT seeded, REG-001 to REG-021, exactly as delivered.
        // The ninth resource's five decided entries, superseding five of these, are asserted by its own test.
        Assert.Equal("MediaCompany.Persistence.Schema.008-management.sql", SchemaInstaller.ResourceNames[^3]);
        Assert.Equal("MediaCompany.Persistence.Schema.008-management.sql", SchemaInstaller.ResourceNames[7]);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM platform_policy_statements"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM platform_policy_reverifications"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM admission_held_outcomes"));

        var read = await Reader().ReadAsync(null, CancellationToken.None);
        var eighth = Enumerable.Range(1, 21).Select(n => $"REG-{n:D3}").ToHashSet(StringComparer.Ordinal);
        var seededByTheEighth = read.RegisterEntries.Where(e => eighth.Contains(e.Identifier)).ToArray();
        var entries = seededByTheEighth.OrderBy(e => e.Identifier, StringComparer.Ordinal).ToArray();
        Assert.Equal(Enumerable.Range(1, 21).Select(n => $"REG-{n:D3}"), entries.Select(e => e.Identifier));
        Assert.Equal(["REG-001", "REG-002", "REG-003"], entries.Where(e => e.Status == RegisterEntryStatus.Decided).Select(e => e.Identifier));

        // The owner's decisions of 2026-10-09 answered the Wave 7 observation-count and ceiling questions: the
        // questions are recorded as they were and superseded by the decisions, never deleted (correction cycle).
        Assert.Equal("REG-015", entries[1].Supersedes);
        Assert.Equal("REG-016", entries[2].Supersedes);
        Assert.Contains("wave-7/bao-cao-ceo.md section 3.1, question 1", entries[14].Statement, StringComparison.Ordinal);
        Assert.Contains("wave-7/bao-cao-ceo.md section 3.1, question 2", entries[15].Statement, StringComparison.Ordinal);

        // The five owner questions this wave opened, each citing the record that holds them; the price-capture
        // question's entry supersedes the one the transcription first recorded and stays open.
        for (var item = 1; item <= 5; item++)
        {
            Assert.Contains($"CEO-Q-700 item {item}, research/ceo-decision-record.md", entries[15 + item].Statement, StringComparison.Ordinal);
            Assert.Equal(RegisterEntryStatus.Open, entries[15 + item].Status);
        }

        Assert.Equal("REG-014", entries[19].Supersedes);
        var open = seededByTheEighth.Where(e => RuleCatalogue.IsOpen(e, seededByTheEighth)).Select(e => e.Identifier).Order(StringComparer.Ordinal);
        Assert.Equal(
            Enumerable.Range(4, 10).Concat(Enumerable.Range(17, 5)).Select(n => $"REG-{n:D3}"),
            open);
        Assert.All(entries.Where(e => e.Status == RegisterEntryStatus.Decided), e =>
        {
            Assert.Equal("the owner", e.DecidedBy);
            Assert.Equal(new DateOnly(2026, 10, 9), e.DecidedOn);
        });
        Assert.All(entries.Where(e => e.Status == RegisterEntryStatus.Open), e => Assert.False(string.IsNullOrWhiteSpace(e.InterimRuling)));

        // Each entry cites the record it is transcribed from, as the two records number them.
        Assert.Contains("CEO-D-700", entries[0].Statement, StringComparison.Ordinal);
        Assert.Contains("CEO-D-701", entries[1].Statement, StringComparison.Ordinal);
        Assert.Contains("CEO-D-702", entries[2].Statement, StringComparison.Ordinal);
        for (var question = 3; question <= 7; question++)
        {
            Assert.Contains($"wave-7/bao-cao-ceo.md section 3.1, question {question}", entries[question].Statement, StringComparison.Ordinal);
        }

        Assert.All(entries.Skip(8).Take(5), e => Assert.Contains("wave-7/bao-cao-ceo.md section", e.Statement, StringComparison.Ordinal));
        Assert.Contains("re-admitted", entries[7].Statement, StringComparison.Ordinal);
        Assert.Contains("Not re-admitted", entries[7].InterimRuling, StringComparison.Ordinal);
        Assert.Contains("the tech lead prepares the ruling", entries[13].Owner, StringComparison.Ordinal);
        Assert.Contains("no record states the re-fetch rule honoured", entries[13].InterimRuling, StringComparison.Ordinal);
        Assert.Contains("the tech lead prepares the ruling", entries[19].Owner, StringComparison.Ordinal);
        Assert.DoesNotContain(entries, e => e.Statement.Contains("34.42", StringComparison.Ordinal) && e.Statement.Contains("interim", StringComparison.OrdinalIgnoreCase));

        // The reading columns lose their precision bound; every other column keeps its type.
        Assert.Equal(3L, await ScalarAsync<long>(
            """
            SELECT count(*) FROM information_schema.columns
            WHERE table_name = 'admission_decision_readings' AND column_name IN ('amount', 'spend_amount', 'utilisation_amount')
              AND data_type = 'numeric' AND numeric_precision IS NULL
            """));

        // Re-applied: nothing is re-inserted and no entry changes, its instant included.
        var instants = entries.Select(e => e.EnteredAt).ToArray();
        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.008-management.sql"));
        var again = (await Reader().ReadAsync(null, CancellationToken.None)).RegisterEntries
            .Where(e => eighth.Contains(e.Identifier)).OrderBy(e => e.Identifier, StringComparer.Ordinal).ToArray();
        Assert.Equal(instants, again.Select(e => e.EnteredAt));
        Assert.Equal(entries, again);
    }

    /// <summary>
    /// A register entry is WRITE-ONCE for every writer: an in-place edit and a delete are refused; a supersession
    /// naming no entry is refused under its own name; a change is a NEW entry naming the one it supersedes, stamped
    /// by the datastore whatever instant the writer supplies; and recording it changes no budget, configuration
    /// value, approval, gate state, route, price, condition or admission record. The superseded entry leaves the
    /// decisions the brief requires, read in the same snapshot.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ARegisterEntryIsWriteOnceAndAChangeIsANewEntryThatChangesNoControl()
    {
        var controls = await ControlCountsAsync();

        await AssertRefusedAsync("UPDATE owner_decision_register SET statement = 'edited' WHERE entry_id = 'REG-003'", "written once");
        await AssertRefusedAsync("UPDATE owner_decision_register SET status = 'Decided', decided_by = 'x', decided_on = CURRENT_DATE WHERE entry_id = 'REG-008'", "written once");
        await AssertRefusedAsync("DELETE FROM owner_decision_register WHERE entry_id = 'REG-004'", "written once");
        await AssertRefusedAsync(
            """
            INSERT INTO owner_decision_register (entry_id, statement, kind, status, owner, recorded_on, supersedes, entered_at)
            VALUES ('REG-900', 'fixture: supersedes nothing', 'Question', 'Open', 'the owner', CURRENT_DATE, 'REG-999', now())
            """,
            "owner_decision_register_supersedes_existing");

        await ExecuteAsync(
            """
            INSERT INTO owner_decision_register
                (entry_id, statement, kind, status, owner, recorded_on, decided_by, decided_on, supersedes, entered_at)
            VALUES ('REG-100', 'fixture: an answer to the re-admission question, a demonstration row', 'Decision', 'Decided',
                    'the owner', CURRENT_DATE, 'the owner', CURRENT_DATE, 'REG-008', '2000-01-01T00:00:00Z')
            """);

        Assert.Equal(controls, await ControlCountsAsync());
        Assert.True(await ScalarAsync<bool>("SELECT entered_at > now() - interval '1 minute' FROM owner_decision_register WHERE entry_id = 'REG-100'"));
        await AssertRefusedAsync(
            """
            INSERT INTO owner_decision_register (entry_id, statement, kind, status, owner, recorded_on, supersedes, entered_at)
            VALUES ('REG-101', 'fixture: supersedes REG-008 a second time', 'Question', 'Open', 'the owner', CURRENT_DATE, 'REG-008', now())
            """,
            "owner_decision_register_superseded_once");

        var read = await ReadAsync();
        var required = read.Brief.Sections.Single(s => s.Section == BriefSection.DecisionsRequired).Items
            .Cast<BriefItem.DecisionItem>().Select(d => d.Entry.Identifier).ToArray();
        Assert.DoesNotContain("REG-008", required);
        Assert.DoesNotContain("REG-100", required);
        Assert.Equal(read.Reports.Register.Where(e => RuleCatalogue.IsOpen(e, read.Reports.Register)).Select(e => e.Identifier), required);
        // The ninth resource (the production change) records the owner's answers of 2026-10-09 to REG-004, REG-007,
        // REG-017, REG-018 and REG-019 as decided entries superseding them, so those five are no longer open; every
        // other entry the delivered assertion named stays open, REG-008 is answered by this demonstration's own row.
        Assert.Equal(
            Enumerable.Range(4, 10).Where(n => n is not (8 or 4 or 7)).Concat(Enumerable.Range(20, 2)).Select(n => $"REG-{n:D3}"),
            required.Order(StringComparer.Ordinal));
    }

    // -----------------------------------------------------------------------
    // One read at one datastore instant
    // -----------------------------------------------------------------------

    /// <summary>
    /// EVERY LINE OF ONE READ CARRIES ONE INSTANT, AND A ROW RECORDED AFTER IT IS NOT READ. An operation stamped after
    /// the report instant — visible in the snapshot because it was committed before the read began, as a writer
    /// committing between the instant and the snapshot would leave it — is read by no line: neither the week's cost,
    /// nor the month's spend, nor the channel's.
    /// </summary>
    [RequiresPostgresFact]
    public async Task EveryLineCarriesOneInstantAndARowStampedAfterTheInstantIsReadByNoLine()
    {
        await RecordOperationAsync(ChannelA, 1_000, 500);                 // 0.0105 USD, booked now
        await InsertOperationStampedAheadAsync(ChannelA, TimeSpan.FromMinutes(10)); // 0.5 USD, stamped ten minutes ahead

        var read = await ReadAsync();
        var instant = read.Reports.Instant;
        Assert.All(read.Reports.Lines, l => Assert.Equal(instant, l.Instant));
        Assert.All(read.Reports.Outcomes, o => Assert.Equal(instant, o.Instant));
        Assert.All(read.Tiles, t => Assert.Equal(instant, t.Instant));

        Assert.Equal(0.0105m, Amount(read, ReportKind.Coo, ReportLineKey.WeekCost));
        Assert.Equal(0.0105m, Amount(read, ReportKind.Cfo, ReportLineKey.CompanySpend));
        Assert.Equal(0.0105m, Amount(read, ReportKind.Cfo, ReportLineKey.ChannelSpend, ChannelA));
        Assert.Equal(2L, await ScalarAsync<long>("SELECT count(*) FROM agent_costs"));
    }

    /// <summary>
    /// A COMMIT DURING COMPOSITION REACHES EVERY AFFECTED LINE OR NONE. An operation committed on another session after
    /// the read's first statement is in no line of that read; the next read has it in every affected line — the
    /// week's cost, the month's company spend and the channel's spend — together.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ACommitDuringCompositionReachesEveryAffectedLineOrNone()
    {
        await RecordOperationAsync(ChannelA, 1_000, 500);

        var committed = false;
        var during = new NpgsqlCompanyRecordReader(Source)
        {
            AfterFirstStatement = async _ =>
            {
                await RecordOperationAsync(ChannelA, 2_000, 1_000);
                committed = true;
            },
        };

        var first = await ReadAsync(during);
        Assert.True(committed);
        Assert.Equal(0.0105m, Amount(first, ReportKind.Coo, ReportLineKey.WeekCost));
        Assert.Equal(0.0105m, Amount(first, ReportKind.Cfo, ReportLineKey.CompanySpend));
        Assert.Equal(0.0105m, Amount(first, ReportKind.Cfo, ReportLineKey.ChannelSpend, ChannelA));

        var second = await ReadAsync();
        Assert.Equal(0.0315m, Amount(second, ReportKind.Coo, ReportLineKey.WeekCost));
        Assert.Equal(0.0315m, Amount(second, ReportKind.Cfo, ReportLineKey.CompanySpend));
        Assert.Equal(0.0315m, Amount(second, ReportKind.Cfo, ReportLineKey.ChannelSpend, ChannelA));
    }

    /// <summary>
    /// A GATE TRANSITION COMMITTED DURING COMPOSITION REACHES EVERY AFFECTED LINE OR NONE (correction cycle). A
    /// presentation for owner approval — a gate transition, read snapshot-bound in the awaiting listing, and its audit
    /// entry, read instant-filtered in the owner send-back rate's denominator — committed on another session after the
    /// first statement is in neither; the next read has it in both, together. A presentation made BEFORE the read under a
    /// process clock a day ahead, its transition carrying that caller instant, is read snapshot-bound all the same.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AGateTransitionCommittedDuringCompositionReachesEveryAffectedLineOrNone()
    {
        await ChannelTestKit.RecordRightsFixtureAsync(Source, ItemB, ChannelB);

        var during = new NpgsqlCompanyRecordReader(Source)
        {
            AfterFirstStatement = async _ =>
            {
                var clock = SteppingClock.HoursAgo(1);
                await ChannelTestKit.PresentAsync(ChannelTestKit.Gate(Source, new NpgsqlUnitOfWork(Source, clock), clock), ItemB, new ItemVersion(1));
            },
        };

        // None: the snapshot-bound listing and the instant-filtered rate both read no presentation.
        var first = await ReadAsync(during);
        Assert.Equal(FigureCase.ObservedZero, Figure(first, ReportKind.Coo, ReportLineKey.AwaitingApproval).Case);
        Assert.Equal(FigureCase.ObservedZero, Figure(first, ReportKind.Coo, ReportLineKey.ChannelAwaitingApproval, ChannelB).Case);
        Assert.Equal(FigureCase.Unmeasured, Figure(first, ReportKind.Coo, ReportLineKey.OwnerSendBackRate).Case);

        // All: the next read has the presentation in the listing, the channel's listing and the rate's denominator.
        var second = await ReadAsync();
        Assert.Equal("1 item versions awaiting owner approval", Figure(second, ReportKind.Coo, ReportLineKey.AwaitingApproval).Describe());
        Assert.Equal("1 item versions awaiting owner approval", Figure(second, ReportKind.Coo, ReportLineKey.ChannelAwaitingApproval, ChannelB).Describe());
        Assert.Equal(FigureCase.ObservedZero, Figure(second, ReportKind.Coo, ReportLineKey.OwnerSendBackRate).Case);
        var snapshot = await Reader().ReadAsync(null, CancellationToken.None);
        Assert.Equal(1L, snapshot.AuditActions.Where(a => a.Action == PublicationGateService.PresentedAction).Sum(a => a.Count));

        // Snapshot-bound: a presentation made under a process clock a day ahead carries that caller instant on its
        // transition and is read all the same; its audit entry is the datastore's and is counted in the week.
        var ahead = new SteppingClock(DateTimeOffset.UtcNow.AddDays(1));
        await ChannelTestKit.RecordRightsFixtureAsync(Source, ItemA, ChannelA);
        await ChannelTestKit.PresentAsync(ChannelTestKit.Gate(Source, new NpgsqlUnitOfWork(Source, ahead), ahead), ItemA, new ItemVersion(1));
        var third = await ReadAsync();
        Assert.Equal("2 item versions awaiting owner approval", Figure(third, ReportKind.Coo, ReportLineKey.AwaitingApproval).Describe());
        Assert.Equal(2L, (await Reader().ReadAsync(null, CancellationToken.None)).AuditActions
            .Where(a => a.Action == PublicationGateService.PresentedAction).Sum(a => a.Count));
    }

    /// <summary>
    /// A BUDGET ALERT COMMITTED DURING COMPOSITION REACHES EVERY AFFECTED LINE OR NONE (correction cycle). An admission
    /// that books an operation and raises the channel's 50 percent alert, committed on another session after the first
    /// statement, is in no line: neither the channel's spend nor its alert count; the next read has both. An alert
    /// row stamped after the report instant — visible to the snapshot — is counted by no line (instant-filtered).
    /// </summary>
    [RequiresPostgresFact]
    public async Task ABudgetAlertCommittedDuringCompositionReachesEveryAffectedLineOrNone()
    {
        var month = await BookingMonthAsync();
        await InsertBudgetAsync(ChannelA, month, 0.0200m);
        await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", 90);

        var during = new NpgsqlCompanyRecordReader(Source)
        {
            AfterFirstStatement = async _ =>
                Assert.IsType<CapabilityOutcome.Completed>(await Gateway(new StandInProvider(Alpha))
                    .ExecuteAsync(Request(CapabilityClass.BulkClassification, 60), Context(), CancellationToken.None)),
        };

        var first = await ReadAsync(during);
        Assert.Equal(1L, await ScalarAsync<long>("SELECT count(*) FROM budget_alerts"));
        Assert.Equal(FigureCase.Unmeasured, Figure(first, ReportKind.Cfo, ReportLineKey.ChannelSpend, ChannelA).Case);
        Assert.Equal(FigureCase.ObservedZero, Figure(first, ReportKind.Cfo, ReportLineKey.ChannelAlerts, ChannelA).Case);

        var second = await ReadAsync();
        Assert.Equal(0.0105m, Amount(second, ReportKind.Cfo, ReportLineKey.ChannelSpend, ChannelA));
        Assert.Equal("1 alerts recorded", Figure(second, ReportKind.Cfo, ReportLineKey.ChannelAlerts, ChannelA).Describe());

        // A DEMONSTRATION FIXTURE written past the delivered tracking: an alert of the 75 percent threshold stamped ten
        // minutes ahead of the datastore's clock, visible to the next snapshot and recorded after its instant.
        await ExecuteAsync(
            """
            INSERT INTO budget_alerts (budget_id, period, threshold, utilization, utilized, budget_amount, raised_at)
            SELECT budget_id, period, 75, 80, 0.016, amount, clock_timestamp() + interval '10 minutes' FROM budgets
            """);
        var third = await ReadAsync();
        Assert.Equal("1 alerts recorded", Figure(third, ReportKind.Cfo, ReportLineKey.ChannelAlerts, ChannelA).Describe());
        Assert.Equal(2L, await ScalarAsync<long>("SELECT count(*) FROM budget_alerts"));
    }

    /// <summary>
    /// A WELL-FORMED WEEK THAT DOES NOT EXIST IS REFUSED BY NAME (correction cycle): 2027-W53 ends the weekly and the
    /// dashboard command with the week refused and the malformed-week exit, as 2026-W99 does, and nothing is printed
    /// but the refusal; an existing week prints.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AWeekThatDoesNotExistEndsTheConsoleCommandsInANamedRefusal()
    {
        var service = new ManagementReportService(Reader());
        foreach (var command in new[] { "weekly", "dashboard" })
        {
            foreach (var (week, named) in new[] { ("2027-W53", "ISO week 2027-W53 does not exist"), ("2026-W99", "does not name an ISO week") })
            {
                var output = new StringWriter();
                var error = new StringWriter();
                var exit = await MediaCompany.Deterministic.Services.ManagementConsole.RunAsync(service, command, week, output, error, CancellationToken.None);
                Assert.Equal(MediaCompany.Deterministic.Services.ConsoleExit.WeekRefused, exit);
                Assert.Equal(2, (int)exit);
                Assert.Contains(named, error.ToString(), StringComparison.Ordinal);
                Assert.Equal(string.Empty, output.ToString());
            }

            var printed = new StringWriter();
            Assert.Equal(
                MediaCompany.Deterministic.Services.ConsoleExit.Printed,
                await MediaCompany.Deterministic.Services.ManagementConsole.RunAsync(service, command, "2026-W40", printed, new StringWriter(), CancellationToken.None));
            Assert.Contains("the UTC week 2026-W40", printed.ToString(), StringComparison.Ordinal);
        }

        var missing = await Assert.ThrowsAsync<ReportWeekNotFoundException>(() => Reader().ReadAsync(new ReportWeek(2027, 53), CancellationToken.None));
        Assert.Equal(new ReportWeek(2027, 53), missing.Named);
    }

    /// <summary>
    /// THE TWO BOUNDS ARE NAMED APART (correction cycle): the lock bound reports the store being changed, the statement
    /// bound a read too slow, any other failure its class.
    /// </summary>
    [Fact]
    public void TheLockBoundAndTheStatementBoundAreNamedApart()
    {
        var locked = NpgsqlCompanyRecordReader.Named(new PostgresException("lock timeout", "ERROR", "ERROR", PostgresErrorCodes.LockNotAvailable));
        Assert.Equal(CompanyReadFailure.StoreBeingChanged, locked.Failure);
        Assert.Contains("the store is being changed", locked.Detail, StringComparison.Ordinal);

        var slow = NpgsqlCompanyRecordReader.Named(new PostgresException("statement timeout", "ERROR", "ERROR", PostgresErrorCodes.QueryCanceled));
        Assert.Equal(CompanyReadFailure.ReadTooSlow, slow.Failure);
        Assert.Contains("the read was too slow", slow.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain("the store is being changed", slow.Detail, StringComparison.Ordinal);

        var other = NpgsqlCompanyRecordReader.Named(new PostgresException("conflict", "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure));
        Assert.Equal(CompanyReadFailure.ReadFailed, other.Failure);
        Assert.Contains(PostgresErrorCodes.SerializationFailure, other.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE READ IS ON THE DATASTORE'S CLOCK. Gate presentations made through the delivered gate service under a process
    /// clock a day AHEAD and a day BEHIND are both read — in the awaiting-approval listing and in the week's
    /// datastore-stamped presentation count — and the instant, the week, its bounds and the datastore's date are the
    /// datastore's, bracketed by its own clock read either side; the measurable-now month is the datastore's too.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ASkewedProcessClockMovesNoInstantNoBoundAndNoCount()
    {
        var ahead = new SteppingClock(DateTimeOffset.UtcNow.AddDays(1));
        var behind = new SteppingClock(DateTimeOffset.UtcNow.AddDays(-1));

        foreach (var (item, clock) in new[] { (ItemA, (IClock)ahead), (ItemB, behind) })
        {
            var channel = item == ItemA ? ChannelA : ChannelB;
            await ChannelTestKit.RecordRightsFixtureAsync(Source, item, channel);
            await ChannelTestKit.PresentAsync(ChannelTestKit.Gate(Source, new NpgsqlUnitOfWork(Source, clock), clock), item, new ItemVersion(1));
        }

        var before = await DatastoreNowAsync();
        var read = await ReadAsync();
        var after = await DatastoreNowAsync();
        var reports = read.Reports;

        Assert.InRange(reports.Instant, before, after);
        var monday = reports.Instant.UtcDateTime.Date.AddDays(-(((int)reports.Instant.UtcDateTime.DayOfWeek + 6) % 7));
        Assert.Equal(new DateTimeOffset(monday, TimeSpan.Zero), reports.PeriodStart);
        Assert.Equal(reports.PeriodStart.AddDays(7), reports.PeriodEnd);

        Assert.Equal("2 item versions awaiting owner approval", Figure(read, ReportKind.Coo, ReportLineKey.AwaitingApproval).Describe());
        Assert.Equal(2, (await new NpgsqlApprovalQueueReader(Source).AwaitingOwnerApprovalAsync(CancellationToken.None)).Count);

        var snapshot = await Reader().ReadAsync(null, CancellationToken.None);
        Assert.Equal(DateOnly.FromDateTime(snapshot.Instant.UtcDateTime), snapshot.DatastoreDate);
        Assert.Equal(2L, snapshot.AuditActions.Where(a => a.Action == PublicationGateService.PresentedAction).Sum(a => a.Count));

        var month = await new ReportingService(new NpgsqlChannelPartitionReader(Source), Reader()).CurrentPeriodAsync(CancellationToken.None);
        Assert.Equal(new DateOnly(after.UtcDateTime.Year, after.UtcDateTime.Month, 1), month);
    }

    /// <summary>
    /// THE READ NEVER WAITS ON ANY HOLD, WHATEVER ITS LENGTH. Another session holds the record horizon shared, as an
    /// admission across a provider call does, a row of the operation record for update, the company admission scope's
    /// advisory hold and the audit chain head, and keeps all four past the read's own thirty-second command timeout:
    /// the read completes in seconds, reading what was committed, and no holder waits on it.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheReadNeverWaitsOnARowHoldAnAdvisoryHoldOrTheHorizonKeptPastItsOwnTimeout()
    {
        var operation = await RecordOperationAsync(ChannelA, 1_000, 500);
        var holder = await HoldAsync(
            """
            SELECT horizon FROM audit_record_horizon WHERE only_row FOR SHARE;
            SELECT operation_id FROM agent_costs WHERE operation_id = @operation FOR UPDATE;
            SELECT pg_advisory_xact_lock(hashtextextended('mediacompany.admission.company', 0));
            SELECT only_row FROM audit_chain_head WHERE only_row FOR UPDATE;
            """,
            HeldPastTheReadsTimeout,
            c => c.Parameters.AddWithValue("operation", operation.Id.Value));

        var since = Stopwatch.StartNew();
        var read = await ReadAsync();
        Assert.True(since.Elapsed < TimeSpan.FromSeconds(5), $"the read took {since.Elapsed} with holds in place");
        Assert.Equal(0.0105m, Amount(read, ReportKind.Coo, ReportLineKey.WeekCost));

        await Task.Delay(CommandTimeout + TimeSpan.FromSeconds(1) - since.Elapsed);
        Assert.False(holder.IsCompleted, "the holder released its holds before the read's command timeout passed");
        var late = Stopwatch.StartNew();
        await ReadAsync();
        Assert.True(late.Elapsed < TimeSpan.FromSeconds(5), $"the later read took {late.Elapsed} with holds in place");
        Assert.False(holder.IsCompleted, "the holder released its holds before the later read completed");
        await holder;
    }

    /// <summary>
    /// A read taken while an admission is held open across a stand-in provider call completes in seconds and reads
    /// nothing of the admission in flight; the read after the admission commits reads its booking.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AReadDuringAnAdmissionHeldAcrossAProviderCallCompletesAndReadsNothingInFlight()
    {
        var month = await BookingMonthAsync();
        await InsertBudgetAsync(ChannelA, month, 1.00m);
        await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", 90);

        ManagementRead? during = null;
        TimeSpan took = default;
        var standIn = new StandInProvider(Alpha, async () =>
        {
            var since = Stopwatch.StartNew();
            during = await ReadAsync();
            took = since.Elapsed;
        });

        Assert.IsType<CapabilityOutcome.Completed>(await Gateway(standIn)
            .ExecuteAsync(Request(CapabilityClass.BulkClassification, floor: 60), Context(), CancellationToken.None));

        Assert.NotNull(during);
        Assert.True(took < TimeSpan.FromSeconds(5), $"the read during the admission took {took}");
        Assert.Equal(FigureCase.Unmeasured, Figure(during!, ReportKind.Coo, ReportLineKey.WeekCost).Case);

        var afterwards = await ReadAsync();
        Assert.Equal(0.0105m, Amount(afterwards, ReportKind.Coo, ReportLineKey.WeekCost));
    }

    /// <summary>
    /// An exclusive structural hold — the only hold a plain read can meet, as while a resource is applied — kept past
    /// the read's command timeout ends the read inside its own bound with its NAMED outcome, never an unnamed timeout.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AStructuralHoldEndsTheReadInItsNamedOutcomeInsideItsOwnBound()
    {
        var holder = await HoldAsync("LOCK TABLE agent_costs IN ACCESS EXCLUSIVE MODE", HeldPastTheReadsTimeout);

        var since = Stopwatch.StartNew();
        var ended = await Assert.ThrowsAsync<CompanyReadException>(() => Reader().ReadAsync(null, CancellationToken.None));
        Assert.True(since.Elapsed < TimeSpan.FromSeconds(10), $"the read ended after {since.Elapsed}");
        Assert.Equal(CompanyReadFailure.StoreBeingChanged, ended.Failure);
        Assert.Contains("lock bound of 2 seconds", ended.Detail, StringComparison.Ordinal);
        Assert.False(holder.IsCompleted);
        await holder;
    }

    /// <summary>A store without the eighth resource ends the read in its named outcome, naming the resource.</summary>
    [RequiresPostgresFact]
    public async Task AStoreWithoutTheEighthResourceEndsTheReadNamingIt()
    {
        await ExecuteAsync("DROP TABLE admission_held_outcomes");
        var ended = await Assert.ThrowsAsync<CompanyReadException>(() => Reader().ReadAsync(null, CancellationToken.None));
        Assert.Equal(CompanyReadFailure.EighthResourceMissing, ended.Failure);
        Assert.Contains("008-management.sql", ended.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE READ WRITES NOTHING AND NEVER ADVANCES THE HORIZON: the stored horizon and every table's row count are the
    /// same after a read as before it, and the read's own transaction refuses any write the datastore receives.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheReadWritesNothingAndNeverAdvancesTheHorizon()
    {
        await RecordOperationAsync(ChannelA, 1_000, 500);
        var horizon = await HorizonAsync();
        var counts = await TableCountsAsync();

        var read = await ReadAsync();

        Assert.Equal(horizon, await HorizonAsync());
        Assert.Equal(horizon, read.Reports.StoredHorizon);
        Assert.Equal(counts, await TableCountsAsync());
    }

    /// <summary>
    /// FINALITY IS READ, NEVER MADE. Before any closure after the week the read states the week NOT FINAL, naming the
    /// stored horizon; once the horizon stands at or after the week's end, the week reads final, and a later commit —
    /// booked after the horizon, so outside the week — leaves every line of the week identical.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AWeekStatedFinalReReadsIdenticalAfterALaterCommit()
    {
        await RecordOperationAsync(ChannelA, 1_000, 500);

        var open = await ReadAsync();
        Assert.False(open.Reports.Final);
        Assert.Contains($"record horizon as stored, {open.Reports.StoredHorizon:O}", open.Reports.FinalityStatement, StringComparison.Ordinal);

        // A DEMONSTRATION FIXTURE: the horizon moved forward to just past the week's end, as a delivered closure run
        // after the week would; the horizon only moves forward, and this store is dropped afterwards.
        await ExecuteAsync(
            "UPDATE audit_record_horizon SET horizon = @end WHERE only_row",
            c => c.Parameters.AddWithValue("end", open.Reports.PeriodEnd.AddSeconds(1)));

        var week = new ReportWeek(int.Parse(open.Reports.Week[..4]), int.Parse(open.Reports.Week[^2..]));
        var final = await new ManagementReportService(Reader()).ReadAsync(week, CancellationToken.None);
        Assert.True(final.Reports.Final);

        await RecordOperationAsync(ChannelA, 5_000, 5_000);
        var again = await new ManagementReportService(Reader()).ReadAsync(week, CancellationToken.None);

        static string Body(ReportLine l) => $"{l.Label}|{l.Figure.Describe()}|{l.Figure.Case}";
        var weekLines = new[] { ReportLineKey.WeekCost, ReportLineKey.UnitsCompleted, ReportLineKey.OperationsFailed, ReportLineKey.ChannelWeekCost };
        Assert.Equal(
            final.Reports.Lines.Where(l => weekLines.Contains(l.Key)).Select(Body),
            again.Reports.Lines.Where(l => weekLines.Contains(l.Key)).Select(Body));
        Assert.Equal(0.0105m, Amount(again, ReportKind.Coo, ReportLineKey.WeekCost));
    }

    // -----------------------------------------------------------------------
    // Held outcomes, controller decisions and the carried items
    // -----------------------------------------------------------------------

    /// <summary>
    /// EVERY DEFERRAL IS RECORDED AND LISTED. A deferral at the 90 percent threshold and a deferral behind an
    /// admission in progress each write a held-outcome row in the admission transaction — reason, escalation instant,
    /// hold timeout, escalation target — and the COO report lists both with reason, hold age on the datastore's clock
    /// and escalation state, none re-admitted; a deferral past its recorded escalation instant raises the COO rule
    /// keyed on that recorded instant.
    /// </summary>
    [RequiresPostgresFact]
    public async Task EveryDeferralIsRecordedWithItsReasonAndListedWithItsHoldAgeAndEscalation()
    {
        var month = await BookingMonthAsync();
        await InsertBudgetAsync(ChannelA, month, 0.0115m);
        await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", 90);
        var standIn = new StandInProvider(Alpha);

        Assert.IsType<CapabilityOutcome.Completed>(await Gateway(standIn).ExecuteAsync(Request(CapabilityClass.BulkClassification, 60), Context(), CancellationToken.None));
        var atThreshold = Assert.IsType<CapabilityOutcome.Held>(await Gateway(standIn)
            .ExecuteAsync(Request(CapabilityClass.BulkClassification, 60, TimeSpan.FromSeconds(1)), Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.DeferredAtThreshold, atThreshold.Reason);

        var holder = await HoldAsync("SELECT pg_advisory_xact_lock(hashtextextended('mediacompany.admission.company', 0))", TimeSpan.FromSeconds(3));
        await InsertBudgetAsync(ChannelB, month, 1.00m);
        var inProgress = Assert.IsType<CapabilityOutcome.Held>(await Gateway(standIn)
            .ExecuteAsync(Request(CapabilityClass.BulkClassification, 60, channel: ChannelB, item: ItemB), Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.MeteredAdmissionInProgress, inProgress.Reason);
        await holder;

        Assert.Equal(2L, await ScalarAsync<long>("SELECT count(*) FROM admission_held_outcomes"));
        Assert.Equal(1L, await ScalarAsync<long>(
            "SELECT count(*) FROM admission_held_outcomes WHERE reason = 'DeferredAtThreshold' AND hold_timeout = interval '1 second' AND escalates_at = held_at + interval '1 second'"));
        Assert.Equal(1L, await ScalarAsync<long>(
            "SELECT count(*) FROM admission_held_outcomes WHERE reason = 'MeteredAdmissionInProgress' AND hold_timeout = interval '4 hours' AND escalates_at = held_at + interval '4 hours'"));
        Assert.Equal(0L, await ScalarAsync<long>(
            "SELECT count(*) FROM admission_held_outcomes h JOIN agent_costs c USING (operation_id) WHERE c.occurred_at <> h.held_at OR c.outcome <> 'Held'"));
        await AssertRefusedAsync("UPDATE admission_held_outcomes SET escalates_to_owner = true", "written once");

        await Task.Delay(TimeSpan.FromSeconds(1.5));
        var read = await ReadAsync();
        var held = read.Reports.Lines.Where(l => l.Key == ReportLineKey.HeldRequest).ToArray();
        Assert.Equal(2, held.Length);
        Assert.All(held, l =>
        {
            Assert.StartsWith("deferred metered request", l.Label, StringComparison.Ordinal);
            Assert.Equal("minutes held", Assert.IsType<MeasurementQuantity.ObservedValue>(Assert.IsType<LineFigure.Measured>(l.Figure).Quantity).Unit);
            Assert.Contains("not re-admitted", l.RecordedText, StringComparison.Ordinal);
        });
        Assert.Contains(held, l => l.RecordedText!.Contains("reason DeferredAtThreshold; escalation PAST", StringComparison.Ordinal));
        Assert.Contains(held, l => l.RecordedText!.Contains("reason MeteredAdmissionInProgress; escalation not yet reached", StringComparison.Ordinal));

        var escalated = Assert.IsType<RuleOutcome.Issued>(Assert.Single(read.Reports.Outcomes, o => o.Rule == RecommendationRule.CooEscalatedDeferral));
        Assert.Contains("REG-008", escalated.AnchorStatement, StringComparison.Ordinal);
        Assert.StartsWith("recorded 1 seconds, the request's recorded hold timeout", escalated.Anchor!.Describe(), StringComparison.Ordinal);
    }

    /// <summary>
    /// THE CONTROLLER'S RECORDED DECISIONS READ BACK AS RECORDED. Every decision booked inside the week is read in the
    /// one snapshot with its action, booking month, datastore instant, readings and stored statements identical to the
    /// rows the admission ledger wrote, and the CTO report prints the stored statements verbatim as recorded text.
    /// </summary>
    [RequiresPostgresFact]
    public async Task EveryControllerDecisionOfTheWeekReadsBackExactlyAsRecorded()
    {
        var month = await BookingMonthAsync();
        await InsertBudgetAsync(ChannelA, month, 0.0115m);
        await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", 90);
        var standIn = new StandInProvider(Alpha);
        await Gateway(standIn).ExecuteAsync(Request(CapabilityClass.BulkClassification, 60), Context(), CancellationToken.None);
        await Gateway(standIn).ExecuteAsync(Request(CapabilityClass.BulkClassification, 60), Context(), CancellationToken.None);

        var snapshot = await Reader().ReadAsync(null, CancellationToken.None);
        Assert.Equal(2, snapshot.Decisions.Count);

        foreach (var decision in snapshot.Decisions)
        {
            await using var command = Source.CreateCommand(
                """
                SELECT decided_at, booking_month, action, basis, basis_statement, tier_statement, company_basis_statement,
                       reservation_statement, (SELECT count(*) FROM admission_decision_readings r WHERE r.operation_id = d.operation_id)
                FROM admission_decisions d WHERE operation_id = @o
                """);
            command.Parameters.AddWithValue("o", decision.Operation.Value);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(reader.GetFieldValue<DateTimeOffset>(0), decision.DecidedAt);
            Assert.Equal(reader.GetFieldValue<DateOnly>(1), decision.BookingMonth);
            Assert.Equal(reader.GetString(2), decision.Action.ToString());
            Assert.Equal(reader.GetString(3), decision.Basis.ToString());
            Assert.Equal(reader.GetString(4), decision.BasisStatement);
            Assert.Equal(reader.GetString(5), decision.TierStatement);
            Assert.Equal(reader.GetString(6), decision.CompanyBasisStatement);
            Assert.Equal(reader.GetString(7), decision.ReservationStatement);
            Assert.Equal(reader.GetInt64(8), decision.Readings.Count);
            Assert.DoesNotContain("interim", decision.CompanyBasisStatement, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("decided on 2026-10-09 is the company ceiling", decision.CompanyBasisStatement, StringComparison.Ordinal);
        }

        var deferred = Assert.Single(snapshot.Decisions, d => d.Action == ControllerAction.Defer);
        var channelReading = Assert.Single(deferred.Readings, r => r.Scope == GoverningScope.Channel);
        Assert.Equal(BudgetThreshold.Ninety, channelReading.Threshold);
        Assert.IsType<MeasurementQuantity.ObservedValue>(channelReading.Utilisation);

        var read = ManagementReportService.Compose(snapshot);
        Assert.Contains(read.Reports.Lines, l => l.Key == ReportLineKey.ControllerDecision
            && l.RecordedText!.Contains($"company basis statement as recorded at that time: {deferred.CompanyBasisStatement}", StringComparison.Ordinal)
            && l.RecordedAt == deferred.DecidedAt);
        Assert.Contains(read.Reports.Outcomes, o => o is RuleOutcome.Issued { Rule: RecommendationRule.CtoControllerAction });
    }

    /// <summary>
    /// AN ADMISSION PAST THE READING COLUMNS' OLD BOUND HAS A NAMED OUTCOME. With a channel's booked spend at more than
    /// ten to the tenth percent of a tiny budget, the next metered admission is refused at the 100 percent threshold
    /// under its own reason, its readings recorded with the unbounded utilisation, and no datastore error reaches the
    /// caller; the controller's thresholds and mapping are unchanged.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnAdmissionPastTheOldColumnBoundIsRefusedUnderItsNamedOutcome()
    {
        var month = await BookingMonthAsync();
        await InsertBudgetAsync(ChannelA, month, 0.00000001m);
        await InsertProviderRouteAsync(CapabilityClass.BulkClassification, "Primary", 90);
        await RecordOperationAsync(ChannelA, 100_000, 100_000); // 1.8 USD: 1.8e10 percent of the budget

        var refused = Assert.IsType<CapabilityOutcome.Refused>(await Gateway(new StandInProvider(Alpha))
            .ExecuteAsync(Request(CapabilityClass.BulkClassification, 60), Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.RefusedAtThreshold, refused.Reason);
        Assert.True(await ScalarAsync<decimal>(
            "SELECT utilisation_amount FROM admission_decision_readings WHERE scope_kind = 'Channel' AND action = 'Refuse'") > 10_000_000_000m);
        Assert.Equal(MediaCompany.Domain.Analytics.ControllerAction.Refuse, MediaCompany.Deterministic.Accounting.CostController.ActionFor(100));
    }

    /// <summary>
    /// THE RE-VERIFICATION RECORD. With nothing recorded the CTO line reads unmeasured naming the missing record and
    /// the rule abstains; a recorded statement and result (demonstration fixtures) read back identical and are
    /// counted, stamped on the datastore's clock whatever a writer supplies; a result is write-once; and the overdue
    /// part stays unmeasured naming the missing cadence.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheReverificationRecordReadsBackAndTheLineStaysUnmeasuredWithoutACadence()
    {
        var empty = await ReadAsync();
        Assert.Equal(FigureCase.Unmeasured, Figure(empty, ReportKind.Cto, ReportLineKey.ReverificationRechecked).Case);
        Assert.IsType<RuleOutcome.Abstained>(Assert.Single(empty.Reports.Outcomes, o => o.Rule == RecommendationRule.CtoReverification));

        await ExecuteAsync(
            """
            INSERT INTO platform_policy_statements (statement_id, platform, label, source_reference, effective_on, registered_at)
            VALUES ('fixture-statement', 'video-platform', 'a demonstration fixture statement', 'a fixture source', NULL, '2000-01-01T00:00:00Z');
            INSERT INTO platform_policy_reverifications (reverification_id, statement_id, verified_on, changed, recorded_at)
            VALUES (gen_random_uuid(), 'fixture-statement', CURRENT_DATE, true, '2000-01-01T00:00:00Z');
            """);

        var snapshot = await Reader().ReadAsync(null, CancellationToken.None);
        var result = Assert.Single(snapshot.Reverifications);
        Assert.Equal("fixture-statement", result.Statement);
        Assert.True(result.Changed);
        Assert.True(result.RecordedAt > DateTimeOffset.UtcNow.AddMinutes(-5));
        Assert.Equal("a demonstration fixture statement", Assert.Single(snapshot.PolicyStatements).Label);

        var read = ManagementReportService.Compose(snapshot);
        Assert.Equal("1 statements re-checked", Figure(read, ReportKind.Cto, ReportLineKey.ReverificationRechecked).Describe());
        Assert.Equal(FigureCase.Unmeasured, Figure(read, ReportKind.Cto, ReportLineKey.ReverificationOverdue).Case);
        await AssertRefusedAsync("UPDATE platform_policy_reverifications SET changed = false", "written once");
        await AssertRefusedAsync("DELETE FROM platform_policy_statements", "written once");
    }

    /// <summary>
    /// THE DASHBOARD IS THE BRIEF OF THE SAME READ. Over a live store with an item awaiting approval, every tile is a
    /// brief item's own rendering with the read's instant; the awaiting-approval tile equals the per-channel approval
    /// view over gate state; and the brief carries its seven sections in order.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheDashboardTilesAreTheBriefOfTheSameReadAndTheAwaitingFigureIsTheApprovalView()
    {
        var clock = SteppingClock.HoursAgo(1);
        await ChannelTestKit.RecordRightsFixtureAsync(Source, ItemA, ChannelA);
        await ChannelTestKit.PresentAsync(ChannelTestKit.Gate(Source, new NpgsqlUnitOfWork(Source, clock), clock), ItemA, new ItemVersion(1));

        var read = await ReadAsync();
        Assert.Equal(Enum.GetValues<BriefSection>(), read.Brief.Sections.Select(s => s.Section));
        var items = read.Brief.Sections.SelectMany(s => s.Items).ToArray();
        Assert.Equal(items.Select(ManagementRendering.Render), read.Tiles.Select(t => t.Rendering));
        Assert.All(read.Tiles, t => Assert.Equal(read.Reports.Instant, t.Instant));

        var view = await new NpgsqlApprovalQueueReader(Source).AwaitingOwnerApprovalAsync(ChannelA, CancellationToken.None);
        var tile = Assert.Single(read.Tiles, t => t.Rendering.StartsWith($"channel {ChannelA}: awaiting owner approval:", StringComparison.Ordinal));
        Assert.Contains($"{view.Count} item versions awaiting owner approval [observed]", tile.Rendering, StringComparison.Ordinal);
        Assert.DoesNotContain(read.Tiles, t => t.Rendering.Contains(PostgresIntegrationTests.ConnectionString!, StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // The harness
    // -----------------------------------------------------------------------

    private static decimal Amount(ManagementRead read, ReportKind report, ReportLineKey key, ChannelId? channel = null) =>
        Assert.IsType<MeasurementQuantity.ObservedValue>(Assert.IsType<LineFigure.Measured>(Figure(read, report, key, channel)).Quantity).Amount;

    private static LineFigure Figure(ManagementRead read, ReportKind report, ReportLineKey key, ChannelId? channel = null) =>
        Assert.Single(read.Reports.Lines, l => l.Report == report && l.Key == key
            && (channel is null || l.Label.Contains(channel.Value.ToString(), StringComparison.Ordinal))).Figure;

    private async Task<OperationRecord> RecordOperationAsync(ChannelId channel, long input, long output)
    {
        await using var transaction = await new NpgsqlUnitOfWork(Source, new FixedClock(DateTimeOffset.UnixEpoch)).BeginAsync(CancellationToken.None);
        var record = await transaction.Operations.RecordAsync(
            new OperationDraft
            {
                Id = OperationId.New(),
                Run = RunId.New(),
                Attribution = new Attribution(channel == ChannelA ? ItemA : ItemB, channel, Department, Agent),
                Capability = CapabilityClass.EditorialReasoning,
                Route = null,
                Model = AlphaModel,
                Units = new UnitCounts(input, output, 0, 0),
                CostBasis = CostBasis.Measurement,
                Duration = TimeSpan.FromMilliseconds(1_000),
                Outcome = OperationOutcome.Succeeded,
                OccurredAt = DateTimeOffset.UnixEpoch,
                Attempt = 1,
            },
            CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return record;
    }

    /// <summary>
    /// A DEMONSTRATION FIXTURE written past the recorder: an operation booked at an instant AHEAD of the datastore's
    /// clock, as a writer committing between a report's instant and its snapshot would leave one — stamped after the
    /// instant, visible to the snapshot. It costs 0.5 USD at the fixture's price.
    /// </summary>
    private Task InsertOperationStampedAheadAsync(ChannelId channel, TimeSpan ahead) =>
        ExecuteAsync(
            """
            INSERT INTO agent_costs (operation_id, run_id, occurred_at, attempt, item_id, channel_id, department_id, agent_id,
                                     capability_class, period, model_id, input_units, output_units, applied_input_price,
                                     applied_output_price, cost_basis, duration_ms, outcome, cost_stated)
            SELECT gen_random_uuid(), gen_random_uuid(), at, 1, @item, @channel, @department, @agent, 'EditorialReasoning',
                   (date_trunc('month', at AT TIME ZONE 'UTC'))::date, 'alpha-reasoning', 100000, 0, 0.000005, 0, 'Measurement',
                   1000, 'Succeeded', true
            FROM (SELECT GREATEST(clock_timestamp(), horizon) + @ahead AS at FROM audit_record_horizon WHERE only_row) s
            """,
            c =>
            {
                c.Parameters.AddWithValue("item", (channel == ChannelA ? ItemA : ItemB).Value);
                c.Parameters.AddWithValue("channel", channel.Value);
                c.Parameters.AddWithValue("department", Department.Value);
                c.Parameters.AddWithValue("agent", Agent.Value);
                c.Parameters.AddWithValue("ahead", ahead);
            });

    private async Task<IReadOnlyDictionary<string, long>> ControlCountsAsync()
    {
        var counts = new Dictionary<string, long>();
        foreach (var table in new[]
                 {
                     "budgets", "budget_alerts", "configuration", "approvals", "gate_transitions", "routes", "model_prices",
                     "first_publication_conditions", "company_payment_account_observations", "admission_decisions", "agent_costs",
                     "audit_entries", "channels",
                 })
        {
            counts[table] = await ScalarAsync<long>($"SELECT count(*) FROM {table}");
        }

        counts["horizon"] = (await HorizonAsync()).UtcTicks;
        return counts;
    }

    private async Task<IReadOnlyDictionary<string, long>> TableCountsAsync()
    {
        var tables = new List<string>();
        await using (var command = Source.CreateCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name"))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        var counts = new Dictionary<string, long>();
        foreach (var table in tables)
        {
            counts[table] = await ScalarAsync<long>($"SELECT count(*) FROM \"{table}\"");
        }

        return counts;
    }

    private async Task AssertRefusedAsync(string sql, string named)
    {
        var refused = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(sql));
        Assert.True(
            refused.MessageText.Contains(named, StringComparison.Ordinal) || refused.ConstraintName == named,
            $"refused as '{refused.MessageText}' ({refused.ConstraintName}), not naming '{named}'");
    }

    /// <summary>
    /// Takes holds on a session of its own and keeps them for the given time, waiting client-side so no command of
    /// the holder runs that long; the returned task ends when the holds are released.
    /// </summary>
    private async Task<Task> HoldAsync(string sql, TimeSpan keep, Action<NpgsqlCommand>? bind = null)
    {
        var connection = await Source.OpenConnectionAsync();
        var transaction = await connection.BeginTransactionAsync();
        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            bind?.Invoke(command);
            await command.ExecuteNonQueryAsync();
        }

        return Task.Run(async () =>
        {
            try
            {
                await Task.Delay(keep);
                await transaction.CommitAsync();
            }
            finally
            {
                await transaction.DisposeAsync();
                await connection.DisposeAsync();
            }
        });
    }

    private CapabilityGateway Gateway(StandInProvider standIn) =>
        new(new NpgsqlUnitOfWork(Source, new FixedClock(DateTimeOffset.UnixEpoch)),
            new StandInBroker(),
            new Dictionary<ProviderAccountId, IProviderAdapter> { [standIn.ProviderAccount] = standIn },
            TimeSpan.FromSeconds(60));

    private static CapabilityInvocationContext Context() => new()
    {
        Run = RunId.New(),
        Job = JobId.New(),
        Stage = LifecyclePosition.Script,
        Actor = WorkforceRole.Producer,
        Attempt = 1,
    };

    private static CapabilityRequest Request(
        CapabilityClass capability, int floor, TimeSpan? hold = null, ChannelId? channel = null, ItemId? item = null) =>
        new(capability,
            ReasoningTier.Standard,
            new QualityRating(floor),
            new ContextCapacity(8_000),
            new Money(1.00m),
            Criticality.Routine,
            new Attribution(item ?? ItemA, channel ?? ChannelA, Department, Agent),
            new EstimatedUnits(1_000, 500, 0),
            hold ?? TimeSpan.FromHours(4),
            ReducedFloorPolicy.Forbidden);

    private async Task InsertProviderRouteAsync(CapabilityClass capability, string tier, int rated) =>
        await ExecuteAsync(
            """
            INSERT INTO routes (route_id, capability_class, tier, target_kind, provider_account_id, model_id,
                                rated_quality, context_capacity, terms_basis, terms_verified_on)
            VALUES (@route, @capability, @tier, 'ProviderRoute', 'provider-alpha', 'alpha-reasoning', @rated, 200000,
                    'paid tier under commercial terms, a demonstration fixture', CURRENT_DATE);
            INSERT INTO route_availability (route_id, effective_from, state, reason)
            VALUES (@route, now() - interval '1 day', 'Serving', 'demonstration fixture: serving');
            """,
            c =>
            {
                c.Parameters.AddWithValue("route", Guid.NewGuid());
                c.Parameters.AddWithValue("capability", capability.ToString());
                c.Parameters.AddWithValue("tier", tier);
                c.Parameters.AddWithValue("rated", rated);
            });

    private Task InsertBudgetAsync(ChannelId channel, DateOnly month, decimal amount) =>
        ExecuteAsync(
            "INSERT INTO budgets (budget_id, scope_kind, scope_id, period, amount, currency) VALUES (gen_random_uuid(), 'Channel', @c, @p, @a, 'USD')",
            c =>
            {
                c.Parameters.AddWithValue("c", channel.Value);
                c.Parameters.Add("p", NpgsqlDbType.Date).Value = month;
                c.Parameters.AddWithValue("a", amount);
            });

    private Task<DateOnly> BookingMonthAsync() => ChannelTestKit.BookingMonthAsync(Source);

    private async Task<DateTimeOffset> HorizonAsync()
    {
        await using var command = Source.CreateCommand("SELECT horizon FROM audit_record_horizon WHERE only_row");
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.GetFieldValue<DateTimeOffset>(0);
    }

    private async Task<DateTimeOffset> DatastoreNowAsync()
    {
        await using var command = Source.CreateCommand("SELECT clock_timestamp()");
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.GetFieldValue<DateTimeOffset>(0);
    }

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
        var result = await command.ExecuteScalarAsync();
        return result is T typed ? typed : (T)Convert.ChangeType(result!, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task SeedAsync() => await ExecuteAsync(
        """
        INSERT INTO companies (company_id, name, operating_state) VALUES (@company, 'Fixture Company', 'building');
        INSERT INTO channels (channel_id, company_id, platform, language, registered) VALUES
            (@a, @company, 'video-platform', 'en', false),
            (@b, @company, 'video-platform', 'vi', false);
        INSERT INTO departments (department_id, company_id, name, budget_holder) VALUES (@department, @company, 'production', 'Owner');
        INSERT INTO workforce_agents (agent_id, department_id, name, role) VALUES (@agent, @department, 'fixture agent', 'Producer');
        INSERT INTO items (item_id, channel_id, item_version, title) VALUES
            (@itemA, @a, 1, 'fixture item of channel A'),
            (@itemB, @b, 1, 'fixture item of channel B');
        INSERT INTO provider_accounts (provider_account_id, provider, commercial_terms_basis, verified_on, status) VALUES
            ('provider-alpha', 'alpha', 'paid tier, a demonstration fixture', '2026-09-01', 'Active');
        INSERT INTO models (model_id, provider_account_id, rated_quality, context_capacity, modality) VALUES
            ('alpha-reasoning', 'provider-alpha', 90, 200000, 'text');
        INSERT INTO model_prices (model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from, valid_to) VALUES
            (gen_random_uuid(), 'alpha-reasoning', 'InputUnit',  0.000003,  'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL),
            (gen_random_uuid(), 'alpha-reasoning', 'OutputUnit', 0.000015,  'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL),
            (gen_random_uuid(), 'alpha-reasoning', 'CachedUnit', 0.0000003, 'USD', 'ESTIMATE, a demonstration parameter', '2026-09-01', '2020-01-01T00:00:00Z', NULL);
        """,
        c =>
        {
            c.Parameters.AddWithValue("company", Company.Value);
            c.Parameters.AddWithValue("a", ChannelA.Value);
            c.Parameters.AddWithValue("b", ChannelB.Value);
            c.Parameters.AddWithValue("department", Department.Value);
            c.Parameters.AddWithValue("agent", Agent.Value);
            c.Parameters.AddWithValue("itemA", ItemA.Value);
            c.Parameters.AddWithValue("itemB", ItemB.Value);
        });

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    /// <summary>
    /// The STAND-IN PROVIDER. It implements the boundary's internal adapter port, reaches NOTHING — no network, no
    /// account, no credential — and returns a fixture attempt: 1,000 input and 500 output units, measured. A
    /// demonstration may run a step during the call, which is when the admission transaction is open.
    /// </summary>
    private sealed class StandInProvider(ProviderAccountId account, Func<Task>? during = null) : IProviderAdapter
    {
        public ProviderAccountId ProviderAccount { get; } = account;

        public async Task<ProviderAttempt> InvokeAsync(
            RouteTarget.ProviderRoute route, CapabilityRequest request, ScopedHandle handle, CancellationToken cancellationToken)
        {
            if (during is not null)
            {
                await during();
            }

            return new ProviderAttempt(true, new UnitCounts(1_000, 500, 0, 0), CostBasis.Measurement, TimeSpan.FromMilliseconds(1_200), null, null);
        }
    }

    /// <summary>A stand-in credential broker. It holds NO SECRET and issues a scoped handle that attaches to nothing.</summary>
    private sealed class StandInBroker : ICredentialBroker
    {
        public Task<CredentialOutcome> IssueAsync(CredentialRequest request, CancellationToken cancellationToken)
        {
            var constructor = typeof(ScopedHandle).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(c => c.GetParameters().Length == 10);
            var handle = (ScopedHandle)constructor.Invoke(
            [
                Guid.NewGuid(), request.CredentialClass, request.ProviderAccount, request.Channel, request.Capability,
                request.Job, request.Stage, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), 1,
            ]);
            return Task.FromResult<CredentialOutcome>(new CredentialOutcome.Issued(handle));
        }
    }
}
