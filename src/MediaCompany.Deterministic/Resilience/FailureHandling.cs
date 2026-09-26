using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Work;

namespace MediaCompany.Deterministic.Resilience;

/// <summary>
/// How a failure at the resolution boundary is classified (module M-001, module M-004). The set
/// is closed so that every failure carries one of these and none ends unclassified.
/// </summary>
public enum FailureClass
{
    Transient = 1,
    RateLimited = 2,
    QuotaExhausted = 3,
    Outage = 4,
    QualityDegraded = 5,
    Refusal = 6,
}

/// <summary>What the policy decided to do about one failure. There is no member meaning "nothing".</summary>
public abstract record FailureDisposition
{
    private FailureDisposition()
    {
    }

    /// <summary>Retry on the same route, after the recorded backoff, inside the deadline.</summary>
    public sealed record RetrySameRoute(int NextAttempt, TimeSpan Backoff) : FailureDisposition;

    /// <summary>Do not retry this route; resolve again from the next tier at or above the floor.</summary>
    public sealed record ReResolve(string Reason) : FailureDisposition;

    /// <summary>Escalate. A refusal never retries, and an exhausted policy escalates.</summary>
    public sealed record Escalate(string Reason) : FailureDisposition;
}

/// <summary>
/// The retry, backoff, failover and circuit-breaking policy, under the names
/// <see cref="DeterministicTaskRegistry.RetryAndBackoff"/>,
/// <see cref="DeterministicTaskRegistry.FailoverSelection"/> and
/// <see cref="DeterministicTaskRegistry.CircuitBreaking"/>.
///
/// Acceptance criterion AC-023 counts failures ending in neither a retry nor an escalation to
/// zero. The return type is what makes that count zero: <see cref="FailureDisposition"/> has
/// three members and none of them is "do nothing", so a failure cannot end undisposed.
/// </summary>
public static class FailureHandling
{
    public static FailureDisposition Decide(
        FailureClass failureClass,
        int attemptsSoFar,
        FailurePolicy policy,
        TimeSpan elapsed,
        TimeSpan deadline)
    {
        ArgumentNullException.ThrowIfNull(policy);

        // A refusal never retries.
        if (failureClass == FailureClass.Refusal)
        {
            return new FailureDisposition.Escalate("A refusal is terminal and never retries.");
        }

        // Quota and outage failures do not retry on the same route; resolution restarts from the
        // next tier at or above the floor.
        if (failureClass is FailureClass.QuotaExhausted or FailureClass.Outage or FailureClass.QualityDegraded)
        {
            return new FailureDisposition.ReResolve(
                $"{failureClass} does not retry on the same route; resolution restarts at or above the floor.");
        }

        if (attemptsSoFar >= policy.MaxAttempts)
        {
            return new FailureDisposition.Escalate(
                $"Attempts exhausted at {attemptsSoFar} of {policy.MaxAttempts}.");
        }

        var backoff = Backoff(attemptsSoFar, policy);
        if (elapsed + backoff > deadline)
        {
            return new FailureDisposition.Escalate(
                "The next backoff would take the request past its deadline; waiting past it is not permitted.");
        }

        return new FailureDisposition.RetrySameRoute(attemptsSoFar + 1, backoff);
    }

    /// <summary>Exponential backoff, capped. Deterministic: the same attempt yields the same wait.</summary>
    public static TimeSpan Backoff(int attemptsSoFar, FailurePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (attemptsSoFar <= 0)
        {
            return policy.InitialBackoff;
        }

        var ticks = policy.InitialBackoff.Ticks * Math.Pow(policy.BackoffMultiplier, attemptsSoFar);
        if (ticks >= policy.MaxBackoff.Ticks || double.IsInfinity(ticks))
        {
            return policy.MaxBackoff;
        }

        return TimeSpan.FromTicks((long)ticks);
    }

    /// <summary>
    /// The availability state a failure class drives the route into. Decision D-008 makes this an
    /// explicit recorded transition rather than a property inferred from recent failures.
    /// </summary>
    public static AvailabilityState? StateFor(FailureClass failureClass) => failureClass switch
    {
        FailureClass.QuotaExhausted => AvailabilityState.QuotaExhausted,
        FailureClass.Outage => AvailabilityState.Outage,
        FailureClass.QualityDegraded => AvailabilityState.QualityDegraded,
        _ => null,
    };
}
