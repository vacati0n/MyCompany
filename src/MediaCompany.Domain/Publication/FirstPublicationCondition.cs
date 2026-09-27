using System.Collections.Frozen;

namespace MediaCompany.Domain.Publication;

/// <summary>
/// The three coded conditions of first publication. The set is CLOSED and has exactly three
/// members, so the predicate over it is total: a condition cannot be forgotten, because there is
/// no way to enumerate fewer than all three.
///
/// None of the three is discharged by this change. Each needs the owner to sign in, verify an
/// identity, or open an account, so this code encodes and verifies them and cannot satisfy them.
/// </summary>
public enum FirstPublicationCondition
{
    /// <summary>The channel is registered on every music and stock library its assets come from.</summary>
    LibraryRegistration = 1,

    /// <summary>The payee position is settled and the payment account exists.</summary>
    PaymentAccount = 2,

    /// <summary>Two-step verification is enabled and no community-guidelines strike stands.</summary>
    TwoStepVerification = 3,
}

/// <summary>
/// The three-valued state of one condition.
///
/// <see cref="Unknown"/> is a RECORDED value, distinct from the absence of a record and distinct
/// from <see cref="NotSatisfied"/>. Keeping the three apart is the whole point: an unverifiable
/// condition and a condition observed to be false both refuse, but they refuse for different
/// reasons and a reader must be able to tell which.
/// </summary>
public enum ConditionState
{
    /// <summary>Observed, and the observation could not establish the answer.</summary>
    Unknown = 0,

    /// <summary>Observed, and the answer is no.</summary>
    NotSatisfied = 1,

    /// <summary>Observed, and the answer is yes.</summary>
    Satisfied = 2,
}

/// <summary>
/// How a condition resolved at evaluation time, which is a different question from what was
/// recorded about it. <see cref="Absent"/> is never stored; it is what the register yields when
/// no observation exists, and it folds to not-satisfied exactly as <see cref="ConditionState.Unknown"/> does.
/// </summary>
public enum ConditionResolution
{
    /// <summary>No observation exists. Folds to not satisfied, and stays visible as absent.</summary>
    Absent = 0,

    /// <summary>An observation exists and records unknown. Folds to not satisfied, and stays visible as unknown.</summary>
    Unknown = 1,

    /// <summary>An observation exists and records no.</summary>
    NotSatisfied = 2,

    /// <summary>An observation exists and records yes. The only resolution that satisfies.</summary>
    Satisfied = 3,
}

/// <summary>
/// One recorded observation of one condition, carrying its value, the evidence behind it, and the
/// date it was taken. An observation without evidence is refused at construction, because a
/// condition of first publication resolved from an unevidenced assertion is the same failure as
/// resolving it from a default.
/// </summary>
public sealed record ConditionObservation
{
    public ConditionObservation(
        FirstPublicationCondition condition,
        ConditionState state,
        string evidence,
        DateOnly observedOn)
    {
        if (string.IsNullOrWhiteSpace(evidence))
        {
            throw new ArgumentException(
                "An observation carries the evidence it was taken from; an unevidenced observation is not one.",
                nameof(evidence));
        }

        Condition = condition;
        State = state;
        Evidence = evidence;
        ObservedOn = observedOn;
    }

    public FirstPublicationCondition Condition { get; }
    public ConditionState State { get; }
    public string Evidence { get; }
    public DateOnly ObservedOn { get; }
}

/// <summary>
/// How one condition stood at the moment it was evaluated: the resolution, and the observation it
/// came from when there was one. The observation is carried so the refusal can name the date and
/// the evidence rather than only the verdict.
/// </summary>
public sealed record ConditionStanding(
    FirstPublicationCondition Condition,
    ConditionResolution Resolution,
    ConditionObservation? Observation)
{
    /// <summary>
    /// The fold. Only an observation recording <see cref="ConditionState.Satisfied"/> satisfies;
    /// unknown and absent both do not. The fold happens HERE, at evaluation, rather than at
    /// storage, which is what keeps the difference between unknown and absent visible in the
    /// refusal after it has ceased to matter to the verdict.
    /// </summary>
    public bool IsSatisfied => Resolution == ConditionResolution.Satisfied;

    /// <summary>The refusal text for this condition, naming which of the three ways it failed.</summary>
    public string Describe() => Resolution switch
    {
        ConditionResolution.Satisfied => $"{Condition} is satisfied (observed {Observation?.ObservedOn:O}).",
        ConditionResolution.NotSatisfied =>
            $"{Condition} is NOT satisfied: observed false on {Observation?.ObservedOn:O} ({Observation?.Evidence}).",
        ConditionResolution.Unknown =>
            $"{Condition} is NOT satisfied: recorded UNKNOWN on {Observation?.ObservedOn:O} ({Observation?.Evidence}). "
            + "An unverifiable condition resolves as not satisfied.",
        _ =>
            $"{Condition} is NOT satisfied: NO observation is recorded. "
            + "An absent record folds to unknown and therefore to not satisfied; it is never read as nothing to check.",
    };
}

/// <summary>
/// The closed three-member condition register (module M-024).
///
/// The register is total over <see cref="FirstPublicationCondition"/>: <see cref="Evaluate"/>
/// returns exactly three standings whatever the observation set contains, so a missing observation
/// produces an ABSENT standing that refuses, rather than producing nothing to check. That is the
/// difference between a condition set that refuses by omission and one that cannot.
/// </summary>
public sealed class FirstPublicationConditionRegister
{
    /// <summary>The closed set, in declaration order. Exactly three, and a fourth is a code change.</summary>
    public static readonly FrozenSet<FirstPublicationCondition> All =
        Enum.GetValues<FirstPublicationCondition>().ToFrozenSet();

    private readonly IReadOnlyDictionary<FirstPublicationCondition, ConditionObservation> _observations;

    public FirstPublicationConditionRegister(IEnumerable<ConditionObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        // The latest observation per condition wins, so a re-observation supersedes without the
        // register holding a history it would have to choose from at evaluation time.
        _observations = observations
            .GroupBy(o => o.Condition)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.ObservedOn).Last());
    }

    /// <summary>An empty register. Every condition resolves absent, so every one refuses.</summary>
    public static FirstPublicationConditionRegister Empty { get; } = new([]);

    /// <summary>
    /// Every condition's standing, always three of them, in declaration order. Total by
    /// construction: the enumeration drives the result, not the observation set.
    /// </summary>
    public IReadOnlyList<ConditionStanding> Evaluate()
    {
        var standings = new List<ConditionStanding>(All.Count);

        foreach (var condition in All)
        {
            if (!_observations.TryGetValue(condition, out var observation))
            {
                standings.Add(new ConditionStanding(condition, ConditionResolution.Absent, null));
                continue;
            }

            var resolution = observation.State switch
            {
                ConditionState.Satisfied => ConditionResolution.Satisfied,
                ConditionState.NotSatisfied => ConditionResolution.NotSatisfied,
                _ => ConditionResolution.Unknown,
            };

            standings.Add(new ConditionStanding(condition, resolution, observation));
        }

        return standings;
    }

    /// <summary>The standings that do not satisfy, in declaration order. Empty means all three hold.</summary>
    public IReadOnlyList<ConditionStanding> Unsatisfied() =>
        Evaluate().Where(s => !s.IsSatisfied).ToArray();

    /// <summary>Whether all three conditions are satisfied. False whenever any is unknown or absent.</summary>
    public bool AllSatisfied() => Evaluate().All(s => s.IsSatisfied);
}
