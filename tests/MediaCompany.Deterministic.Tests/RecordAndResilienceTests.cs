using MediaCompany.Deterministic.Accounting;
using MediaCompany.Deterministic.Record;
using MediaCompany.Deterministic.Reporting;
using MediaCompany.Deterministic.Resilience;
using MediaCompany.Domain;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>The tamper-evident chain over the append-only record (acceptance criterion AC-009).</summary>
public sealed class AuditChainTests
{
    private static AuditEntry Entry(string previousHash, string subject = "item:one")
    {
        var id = AuditEntryId.New();
        var occurredAt = DateTimeOffset.Parse("2026-10-01T10:00:00Z");
        var hash = AuditChain.ComputeHash(
            id, "producer", "asset.recorded", subject, occurredAt, "a reason",
            "inputs", "outputs", "recorded", "none", "none", RetentionClass.RightsRecord, previousHash);

        return new AuditEntry
        {
            Id = id,
            Actor = "producer",
            Action = "asset.recorded",
            Subject = subject,
            OccurredAt = occurredAt,
            Reason = "a reason",
            InputsReference = "inputs",
            OutputsReference = "outputs",
            Decision = "recorded",
            CostReference = "none",
            Risk = "none",
            RetentionClass = RetentionClass.RightsRecord,
            PreviousEntryHash = previousHash,
            EntryHash = hash,
        };
    }

    [Fact]
    public void AnIntactChainVerifies()
    {
        var first = Entry(AuditChain.Genesis);
        var second = Entry(first.EntryHash, "item:two");

        Assert.Equal(-1, AuditChain.FirstBrokenLink([first, second]));
    }

    /// <summary>
    /// An altered entry is detectable. The datastore refuses the amendment outright, and this is
    /// the second, independent check: if a row were altered by any other means, the chain says so.
    /// </summary>
    [Fact]
    public void AnAlteredEntryBreaksTheChainAtItsOwnIndex()
    {
        var first = Entry(AuditChain.Genesis);
        var second = Entry(first.EntryHash, "item:two");
        var tampered = second with { Reason = "a different reason" };

        Assert.Equal(1, AuditChain.FirstBrokenLink([first, tampered]));
    }

    /// <summary>Altering an earlier entry invalidates it and everything after it.</summary>
    [Fact]
    public void AlteringAnEarlierEntryBreaksTheChainThere()
    {
        var first = Entry(AuditChain.Genesis);
        var second = Entry(first.EntryHash, "item:two");
        var tamperedFirst = first with { Decision = "changed" };

        Assert.Equal(0, AuditChain.FirstBrokenLink([tamperedFirst, second]));
    }

    /// <summary>A removed entry breaks the chain at the entry that followed it.</summary>
    [Fact]
    public void ARemovedEntryBreaksTheChain()
    {
        var first = Entry(AuditChain.Genesis);
        var second = Entry(first.EntryHash, "item:two");
        var third = Entry(second.EntryHash, "item:three");

        Assert.Equal(1, AuditChain.FirstBrokenLink([first, third]));
    }

    /// <summary>The hash is a function of the content: the same content yields the same hash.</summary>
    [Fact]
    public void TheHashIsDeterministic()
    {
        var entry = Entry(AuditChain.Genesis);
        Assert.Equal(entry.EntryHash, AuditChain.ComputeHash(entry));
    }
}

/// <summary>
/// The failure policy, against acceptance criterion AC-023: no failure ends in neither a retry nor
/// an escalation.
/// </summary>
public sealed class FailureHandlingTests
{
    private static readonly FailurePolicy Policy = FailurePolicy.Default;

    /// <summary>
    /// Every failure class yields a disposition. The union has three members and none of them is
    /// "do nothing", so the count acceptance criterion AC-023 measures is zero by construction.
    /// </summary>
    [Theory]
    [InlineData(FailureClass.Transient)]
    [InlineData(FailureClass.RateLimited)]
    [InlineData(FailureClass.QuotaExhausted)]
    [InlineData(FailureClass.Outage)]
    [InlineData(FailureClass.QualityDegraded)]
    [InlineData(FailureClass.Refusal)]
    public void EveryFailureClassEndsRetriedReResolvedOrEscalated(FailureClass failureClass)
    {
        var disposition = FailureHandling.Decide(
            failureClass, attemptsSoFar: 1, Policy, TimeSpan.Zero, TimeSpan.FromMinutes(30));

        Assert.True(disposition
            is FailureDisposition.RetrySameRoute
            or FailureDisposition.ReResolve
            or FailureDisposition.Escalate);
    }

    /// <summary>A refusal never retries.</summary>
    [Fact]
    public void ARefusalNeverRetries()
    {
        var disposition = FailureHandling.Decide(
            FailureClass.Refusal, attemptsSoFar: 0, Policy, TimeSpan.Zero, TimeSpan.FromHours(1));

        Assert.IsType<FailureDisposition.Escalate>(disposition);
    }

    /// <summary>Quota and outage failures do not retry on the same route; resolution restarts.</summary>
    [Theory]
    [InlineData(FailureClass.QuotaExhausted)]
    [InlineData(FailureClass.Outage)]
    [InlineData(FailureClass.QualityDegraded)]
    public void QuotaAndOutageFailuresReResolveRatherThanRetry(FailureClass failureClass)
    {
        Assert.IsType<FailureDisposition.ReResolve>(FailureHandling.Decide(
            failureClass, attemptsSoFar: 0, Policy, TimeSpan.Zero, TimeSpan.FromHours(1)));
    }

    /// <summary>An exhausted policy escalates rather than retrying forever.</summary>
    [Fact]
    public void AnExhaustedPolicyEscalates()
    {
        Assert.IsType<FailureDisposition.Escalate>(FailureHandling.Decide(
            FailureClass.Transient, attemptsSoFar: Policy.MaxAttempts, Policy, TimeSpan.Zero, TimeSpan.FromHours(1)));
    }

    /// <summary>
    /// A wait that would take the request past its deadline escalates instead. The design forbids
    /// waiting past the deadline; it re-resolves or escalates.
    /// </summary>
    [Fact]
    public void ABackoffThatWouldPassTheDeadlineEscalates()
    {
        Assert.IsType<FailureDisposition.Escalate>(FailureHandling.Decide(
            FailureClass.RateLimited, attemptsSoFar: 1, Policy, TimeSpan.FromSeconds(59), TimeSpan.FromSeconds(60)));
    }

    /// <summary>Backoff grows and is capped, deterministically.</summary>
    [Fact]
    public void BackoffGrowsAndIsCapped()
    {
        var first = FailureHandling.Backoff(1, Policy);
        var second = FailureHandling.Backoff(2, Policy);
        var far = FailureHandling.Backoff(50, Policy);

        Assert.True(second > first);
        Assert.Equal(Policy.MaxBackoff, far);
        Assert.Equal(first, FailureHandling.Backoff(1, Policy));
    }

    /// <summary>A failure class maps to an explicit recorded availability state (decision D-008).</summary>
    [Fact]
    public void FailureClassesMapToRecordedAvailabilityStates()
    {
        Assert.NotNull(FailureHandling.StateFor(FailureClass.QuotaExhausted));
        Assert.NotNull(FailureHandling.StateFor(FailureClass.Outage));
        Assert.NotNull(FailureHandling.StateFor(FailureClass.QualityDegraded));
        Assert.Null(FailureHandling.StateFor(FailureClass.Transient));
    }
}

/// <summary>Budget threshold crossings, against acceptance criterion AC-027.</summary>
public sealed class BudgetThresholdTests
{
    /// <summary>Driving utilization past each threshold in turn raises one crossing at each.</summary>
    [Fact]
    public void EachThresholdIsCrossedExactlyOnceAsUtilizationRises()
    {
        decimal[] steps = [0m, 55m, 78m, 93m, 104m];
        var crossings = new List<int>();

        for (var i = 1; i < steps.Length; i++)
        {
            crossings.AddRange(BudgetThresholds.Crossed(steps[i - 1], steps[i]));
        }

        Assert.Equal([50, 75, 90, 100], crossings);
    }

    /// <summary>A threshold already crossed is not crossed again: the alert is idempotent.</summary>
    [Fact]
    public void AThresholdAlreadyCrossedIsNotCrossedAgain()
    {
        Assert.Equal([50], BudgetThresholds.Crossed(10m, 60m));
        Assert.Empty(BudgetThresholds.Crossed(60m, 70m));
        Assert.Empty(BudgetThresholds.Crossed(60m, 60m));
    }

    /// <summary>A single jump past several thresholds crosses each of them.</summary>
    [Fact]
    public void ASingleJumpCrossesEveryThresholdItPasses()
    {
        Assert.Equal([50, 75, 90, 100], BudgetThresholds.Crossed(0m, 120m));
    }

    /// <summary>Falling utilization crosses nothing.</summary>
    [Fact]
    public void FallingUtilizationCrossesNothing()
    {
        Assert.Empty(BudgetThresholds.Crossed(95m, 40m));
    }

    [Fact]
    public void TheHighestThresholdReachedIsReported()
    {
        Assert.Null(BudgetThresholds.HighestReached(49m));
        Assert.Equal(50, BudgetThresholds.HighestReached(50m));
        Assert.Equal(100, BudgetThresholds.HighestReached(250m));
    }
}

/// <summary>The reported measure set, against acceptance criteria AC-025 and AC-026.</summary>
public sealed class MeasureCatalogueTests
{
    /// <summary>AC-025: the measurable-now set carries no revenue-denominated measure.</summary>
    [Fact]
    public void TheMeasurableNowSetCarriesNoRevenueMeasure()
    {
        var revenueWords = new[] { "revenue", "profit", "rpm", "watch-hours", "subscriber", "return-on" };

        Assert.DoesNotContain(
            MeasureCatalogue.MeasurableNow,
            m => revenueWords.Any(w => m.Name.Contains(w, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// AC-026: a deferred measure names the parameter it waits on, and carries no value at all, so
    /// nothing can render it as zero, a dash, or any placeholder a reader could mistake for a
    /// measurement. The type has no value member; this test asserts that over its shape.
    /// </summary>
    [Fact]
    public void ADeferredMeasureHasNoValueMemberAndNamesItsAwaitedParameter()
    {
        var members = typeof(Measure.Deferred).GetProperties().Select(p => p.Name).ToArray();

        Assert.DoesNotContain("Value", members);
        Assert.Contains("AwaitingParameter", members);

        Assert.All(MeasureCatalogue.Deferred, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.AwaitingParameter));
            Assert.Contains("not yet available", d.Display, StringComparison.Ordinal);
            Assert.Contains(d.AwaitingParameter, d.Display, StringComparison.Ordinal);
        });
    }

    /// <summary>Every deferred measure waits on an observed parameter, and every one is named.</summary>
    [Fact]
    public void EveryRevenueDenominatedMeasureIsDeferredAndNamed()
    {
        var deferredNames = MeasureCatalogue.Deferred.Select(d => d.Name).ToArray();

        Assert.Contains("revenue-per-mille", deferredNames);
        Assert.Contains("profit", deferredNames);
        Assert.Contains("watch-hours", deferredNames);
        Assert.Contains("partner-programme-threshold-progress", deferredNames);
    }

    /// <summary>The two sets partition the catalogue; no measure is in both or in neither.</summary>
    [Fact]
    public void TheTwoSetsPartitionTheCatalogue()
    {
        Assert.Equal(MeasureCatalogue.All.Count, MeasureCatalogue.MeasurableNow.Count + MeasureCatalogue.Deferred.Count);
        Assert.Empty(MeasureCatalogue.MeasurableNow.Select(m => m.Name)
            .Intersect(MeasureCatalogue.Deferred.Select(d => d.Name)));
    }

    /// <summary>Every measurable-now measure states where its figure comes from.</summary>
    [Fact]
    public void EveryMeasurableNowMeasureNamesItsSource()
    {
        Assert.All(MeasureCatalogue.MeasurableNow, m => Assert.False(string.IsNullOrWhiteSpace(m.Source)));
    }
}

/// <summary>The named zero-AI-cost set, against acceptance criterion AC-017.</summary>
public sealed class DeterministicTaskRegistryTests
{
    /// <summary>The set the upstream recommendation records, restricted by this wave's boundary.</summary>
    [Fact]
    public void TheNamedSetCarriesTheRecordedMembers()
    {
        Assert.Contains(DeterministicTaskRegistry.Render, (IEnumerable<string>)DeterministicTaskRegistry.Names);
        Assert.Contains(DeterministicTaskRegistry.SubtitleTimingForcedAlignment, (IEnumerable<string>)DeterministicTaskRegistry.Names);
        Assert.Contains(DeterministicTaskRegistry.ProviderRouting, (IEnumerable<string>)DeterministicTaskRegistry.Names);
        Assert.Contains(DeterministicTaskRegistry.CostMetering, (IEnumerable<string>)DeterministicTaskRegistry.Names);
        Assert.Contains(DeterministicTaskRegistry.AuditLogging, (IEnumerable<string>)DeterministicTaskRegistry.Names);
        Assert.Contains(DeterministicTaskRegistry.PerceptualDuplicateDetection, (IEnumerable<string>)DeterministicTaskRegistry.Names);
    }

    /// <summary>
    /// Constraint C-013 restricts the set: the idempotent upload the upstream recommendation also
    /// names is excluded, because this wave publishes nothing and no upload path exists.
    /// </summary>
    [Fact]
    public void TheIdempotentUploadIsExcludedByThisWavesBoundary()
    {
        Assert.DoesNotContain(
            (IEnumerable<string>)DeterministicTaskRegistry.Names,
            name => name.Contains("upload", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MembershipIsExactAndCaseSensitive()
    {
        Assert.True(DeterministicTaskRegistry.Contains("render"));
        Assert.False(DeterministicTaskRegistry.Contains("Render"));
        Assert.False(DeterministicTaskRegistry.Contains("call-a-model"));
    }
}
