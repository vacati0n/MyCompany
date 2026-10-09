namespace MediaCompany.Domain.Accounting;

/// <summary>
/// An amount somebody RECORDED — a budget amount, the approved envelope, its metered allotment — in
/// exactly one of two shapes (the AI-economics change, decision D-011 of its design).
///
/// A recorded amount is not a measurement. Nobody observed a budget; somebody wrote it down. The
/// delivered budget readings composed these amounts as observed measurement quantities, which labelled
/// a recorded figure as an observation. This type is the label that is true: <see cref="Recorded"/>
/// carries the amount, its currency and the register or constant it was recorded in, and
/// <see cref="NotRecorded"/> carries what was looked for and NO VALUE FIELD OF ANY KIND, so a missing
/// budget has no number a renderer could print as zero or as headroom.
///
/// It is deliberately NOT a fourth case of the measurement union, which stays closed at three cases.
/// Each case's constructor is private, and the only way to reach one is its factory.
/// </summary>
public abstract record RecordedAmount
{
    private RecordedAmount()
    {
    }

    /// <summary>An amount recorded in a named register or constant.</summary>
    public sealed record Recorded : RecordedAmount
    {
        private Recorded(Money amount, string recordedIn)
        {
            Amount = amount;
            RecordedIn = recordedIn;
        }

        /// <summary>The recorded amount, in its currency. Always positive.</summary>
        public Money Amount { get; }

        /// <summary>The register or constant the amount was recorded in.</summary>
        public string RecordedIn { get; }

        internal static Recorded Create(Money amount, string recordedIn)
        {
            if (amount.Amount <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "A recorded amount is positive; an amount nobody recorded is NotRecorded, never zero.");
            }

            if (string.IsNullOrWhiteSpace(recordedIn))
            {
                throw new ArgumentException("A recorded amount names the register or constant it was recorded in.", nameof(recordedIn));
            }

            return new Recorded(amount, recordedIn);
        }
    }

    /// <summary>No amount was recorded. Carries what was looked for, and no value, no currency and no amount.</summary>
    public sealed record NotRecorded : RecordedAmount
    {
        private NotRecorded(string lookedFor) => LookedFor = lookedFor;

        /// <summary>What was looked for and where. Blank is refused at construction.</summary>
        public string LookedFor { get; }

        internal static NotRecorded Create(string lookedFor) =>
            string.IsNullOrWhiteSpace(lookedFor)
                ? throw new ArgumentException(
                    "An amount that was not recorded states what was looked for and where; without it the absence is not diagnosable.",
                    nameof(lookedFor))
                : new NotRecorded(lookedFor);
    }

    /// <summary>An amount recorded in <paramref name="recordedIn"/>.</summary>
    public static RecordedAmount Of(Money amount, string recordedIn) => Recorded.Create(amount, recordedIn);

    /// <summary>No amount recorded, naming what was looked for.</summary>
    public static RecordedAmount Missing(string lookedFor) => NotRecorded.Create(lookedFor);

    /// <summary>The rendering. The two shapes render apart, and the not-recorded one carries no amount.</summary>
    public string Describe() => this switch
    {
        Recorded recorded => $"recorded {recorded.Amount} in {recorded.RecordedIn}",
        NotRecorded missing => $"not recorded: {missing.LookedFor}",
        _ => throw new InvalidOperationException("Unreachable: a recorded amount has two shapes."),
    };
}
