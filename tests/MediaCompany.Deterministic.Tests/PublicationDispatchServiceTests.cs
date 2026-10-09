using MediaCompany.Application.Ports;
using MediaCompany.Application.Production;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Publication;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The service that persists a composed dispatch.
///
/// What is asserted here is the SHAPE of the write: which rows are produced, which are not, and
/// that the whole of it is one transaction. That the transaction is atomic under interruption is a
/// datastore property and is demonstrated against a real instance instead, because an in-memory
/// double would demonstrate the double.
/// </summary>
public sealed class PublicationDispatchServiceTests
{
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(3);

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private static (PublicationDispatchService Service, InMemoryWork Work) Build()
    {
        var work = new InMemoryWork();
        return (new PublicationDispatchService(work, new FixedClock(PublishingFixture.Now)), work);
    }

    private static DispatchOutcome Composed() => PublicationDispatchComposer.Compose(
        WorkforceRole.Publisher,
        PublishingFixture.Passed(Item, Version),
        Item, Version,
        PublishingFixture.Destination,
        PublishingFixture.Settings,
        PublishingFixture.Surfaces(Item, Version),
        PublishingFixture.Now.AddDays(3),
        PublishingFixture.Now);

    private static DispatchOutcome Refused() => PublicationDispatchComposer.Compose(
        WorkforceRole.Researcher,
        PublishingFixture.Passed(Item, Version),
        Item, Version,
        PublishingFixture.Destination,
        PublishingFixture.Settings,
        PublishingFixture.Surfaces(Item, Version),
        PublishingFixture.Now.AddDays(3),
        PublishingFixture.Now);

    /// <summary>A composed dispatch writes exactly one record and one attempt, and commits once.</summary>
    [Fact]
    public async Task AComposedDispatchWritesOneRecordAndOneAttempt()
    {
        var (service, work) = Build();

        var result = await service.PersistAsync(
            Composed(), Item, Version, PublishingFixture.Destination, "Owner",
            new string('b', 64), job: null, gateStateChange: null, CancellationToken.None);

        Assert.Equal(DispatchWriteOutcome.Created, result.Write);
        Assert.False(result.Refused);
        Assert.Single(work.Dispatches);
        Assert.Single(work.Attempts);
        Assert.Equal(1, work.Commits);
    }

    /// <summary>
    /// A retry writes a second ATTEMPT and no second dispatch record. Two attempts, one record,
    /// which is what exactly-once means at this edge.
    /// </summary>
    [Fact]
    public async Task ARetryAddsAnAttemptAndNoSecondRecord()
    {
        var (service, work) = Build();

        await service.PersistAsync(
            Composed(), Item, Version, PublishingFixture.Destination, "Owner",
            new string('b', 64), null, null, CancellationToken.None);

        var retry = await service.PersistAsync(
            Composed(), Item, Version, PublishingFixture.Destination, "Owner",
            new string('b', 64), null, null, CancellationToken.None);

        Assert.Equal(DispatchWriteOutcome.AlreadyRecorded, retry.Write);
        Assert.Single(work.Dispatches);
        Assert.Equal(2, work.Attempts.Count);
    }

    /// <summary>A refused attempt is recorded, and leaves no dispatch record behind.</summary>
    [Fact]
    public async Task ARefusedAttemptIsRecordedAndLeavesNoDispatchRecord()
    {
        var (service, work) = Build();

        var result = await service.PersistAsync(
            Refused(), Item, Version, PublishingFixture.Destination, string.Empty,
            new string('b', 64), null, null, CancellationToken.None);

        Assert.True(result.Refused);

        // Not "already recorded": no dispatch record exists for this item version, and saying one
        // does would tell a reader the opposite of the truth.
        Assert.Equal(DispatchWriteOutcome.NoRecordWritten, result.Write);
        Assert.Empty(work.Dispatches);
        Assert.Single(work.Attempts);
        Assert.True(work.Attempts[0].Refused);
        Assert.True(work.Attempts[0].AnswersAllFive);
    }

    /// <summary>
    /// A transition to Published is REFUSED by the service.
    ///
    /// Nothing in this change publishes, and the recorded history has no in-place correction, so a
    /// false transition written here could not be taken back.
    /// </summary>
    [Fact]
    public async Task ATransitionToPublishedIsRefused()
    {
        var (service, work) = Build();

        await Assert.ThrowsAsync<ArgumentException>(() => service.PersistAsync(
            Composed(), Item, Version, PublishingFixture.Destination, "Owner", new string('b', 64),
            job: null,
            gateStateChange: new GateStateChange(
                GateState.Approved, GateState.Published, "would be a false record"),
            CancellationToken.None));

        Assert.Empty(work.Transitions);
    }

    /// <summary>A permitted gate state change is written inside the same transaction.</summary>
    [Fact]
    public async Task APermittedGateStateChangeIsWrittenWithTheDispatch()
    {
        var (service, work) = Build();

        // The change starts from the state the record holds, recorded first as a fixture: the double
        // refuses a from-state the record does not hold, exactly as the record store does.
        work.WithRecordedGateState(Item, Version, GateState.AwaitingRightsCheck);

        await service.PersistAsync(
            Composed(), Item, Version, PublishingFixture.Destination, "Owner", new string('b', 64),
            job: null,
            gateStateChange: new GateStateChange(
                GateState.AwaitingRightsCheck, GateState.PublishReady, "held short of dispatch"),
            CancellationToken.None);

        Assert.Single(work.Dispatches);
        Assert.Contains(work.Transitions, t => t == (GateState.AwaitingRightsCheck, GateState.PublishReady));

        // One commit: the dispatch, the attempt, the transition and the audit entry are one write.
        Assert.Equal(1, work.Commits);
    }

    /// <summary>Every persist appends an audit entry, refused attempts included.</summary>
    [Fact]
    public async Task EveryPersistAppendsAnAuditEntry()
    {
        var (service, work) = Build();

        await service.PersistAsync(
            Composed(), Item, Version, PublishingFixture.Destination, "Owner",
            new string('b', 64), null, null, CancellationToken.None);
        await service.PersistAsync(
            Refused(), Item, Version, PublishingFixture.Destination, string.Empty,
            new string('b', 64), null, null, CancellationToken.None);

        Assert.Equal(2, work.AuditEntries.Count);
        Assert.Contains(work.AuditEntries, e => e.Action == "publication.dispatch-composed");
        Assert.Contains(work.AuditEntries, e => e.Action == "publication.dispatch-refused");
    }
}

/// <summary>The production configuration surface, tightened by the publishing vocabulary.</summary>
public sealed class ProductionConfigurationSurfaceTests
{
    /// <summary>
    /// The production forbidden-fragment rule gains the egress vocabulary, and the tightening
    /// invalidates no key the surface already admits.
    /// </summary>
    [Fact]
    public void TheTighteningInvalidatesNoAdmittedProductionKey()
    {
        Assert.All(
            ProductionConfigurationKeys.Admitted,
            key => Assert.False(
                ProductionConfigurationKeys.ReachesAControl(key),
                $"the admitted production key {key} would now be inadmissible"));
    }

    /// <summary>A production key naming an egress, an upload, a dispatch or a transport is refused.</summary>
    [Theory]
    [InlineData("production.egress-endpoint")]
    [InlineData("production.upload-target")]
    [InlineData("production.dispatch-mode")]
    [InlineData("production.transport-timeout")]
    public void AProductionKeyNamingAnEgressIsInadmissible(string key)
    {
        Assert.True(ProductionConfigurationKeys.ReachesAControl(key));
        Assert.False(ProductionConfigurationKeys.IsAdmitted(key));
    }

    /// <summary>
    /// The treatment-conditions key stays admitted. It legitimately carries the conditions as a
    /// VALUE, which is why the stricter fragment barring "condition" is placed on the publishing
    /// surface, where no key has that business, rather than here.
    /// </summary>
    [Fact]
    public void TheTreatmentConditionsKeyStaysAdmitted()
    {
        Assert.True(ProductionConfigurationKeys.IsAdmitted(ProductionConfigurationKeys.TreatmentConditions));
        Assert.False(ProductionConfigurationKeys.ReachesAControl(ProductionConfigurationKeys.TreatmentConditions));
    }
}
