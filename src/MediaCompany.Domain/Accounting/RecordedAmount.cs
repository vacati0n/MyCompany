namespace MediaCompany.Domain.Accounting;

/// <summary>
/// An amount somebody RECORDED — a budget amount, the approved envelope, its metered allotment — in
/// exactly one of three shapes (the AI-economics change, decision D-011 of its design; the third, a recorded
/// figure that is not money, added by the AI-management change).
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

    /// <summary>
    /// A recorded figure that is NOT MONEY (the AI-management change, decision D-003 of its design): a
    /// recorded count, percentage or threshold carrying a positive decimal and its unit, or a recorded date
    /// carrying the date and what it is the date of, each naming where it was recorded — the owner's ten
    /// comparable runs, a controller threshold, a programme threshold, a dated platform change.
    ///
    /// It is a third shape of the recorded amount and NOT a fourth case of the measurement union: nobody
    /// observed it, somebody wrote it down, and it renders as recorded, naming its source, never as observed.
    /// Exactly one of <see cref="Value"/> and <see cref="Date"/> is present, decided by its two factories.
    /// </summary>
    public sealed record RecordedFigure : RecordedAmount
    {
        private RecordedFigure(decimal? value, DateOnly? date, string unit, string recordedIn)
        {
            Value = value;
            Date = date;
            Unit = unit;
            RecordedIn = recordedIn;
        }

        /// <summary>The recorded figure, always positive; null on a recorded date.</summary>
        public decimal? Value { get; }

        /// <summary>The recorded date; null on a recorded figure.</summary>
        public DateOnly? Date { get; }

        /// <summary>The unit of the figure, or what the date is the date of.</summary>
        public string Unit { get; }

        /// <summary>The record, decision or document section the figure was recorded in.</summary>
        public string RecordedIn { get; }

        internal static RecordedFigure Create(decimal value, string unit, string recordedIn)
        {
            if (value <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "A recorded figure is positive; a figure nobody recorded is NotRecorded, never zero.");
            }

            return new RecordedFigure(value, null, Require(unit, nameof(unit)), Require(recordedIn, nameof(recordedIn)));
        }

        internal static RecordedFigure Create(DateOnly date, string what, string recordedIn) =>
            new(null, date, Require(what, nameof(what)), Require(recordedIn, nameof(recordedIn)));

        private static string Require(string text, string name) =>
            string.IsNullOrWhiteSpace(text)
                ? throw new ArgumentException("A recorded figure names its unit and where it was recorded.", name)
                : text;
    }

    /// <summary>An amount recorded in <paramref name="recordedIn"/>.</summary>
    public static RecordedAmount Of(Money amount, string recordedIn) => Recorded.Create(amount, recordedIn);

    /// <summary>No amount recorded, naming what was looked for.</summary>
    public static RecordedAmount Missing(string lookedFor) => NotRecorded.Create(lookedFor);

    /// <summary>A recorded figure that is not money, in its unit, naming where it was recorded.</summary>
    public static RecordedAmount Figure(decimal value, string unit, string recordedIn) =>
        RecordedFigure.Create(value, unit, recordedIn);

    /// <summary>A recorded date, naming what it is the date of and where it was recorded.</summary>
    public static RecordedAmount OnDate(DateOnly date, string what, string recordedIn) =>
        RecordedFigure.Create(date, what, recordedIn);

    /// <summary>
    /// The rendering. The three shapes render apart, every recorded one says "recorded" and names its source,
    /// and the not-recorded one carries no amount.
    /// </summary>
    public string Describe() => this switch
    {
        Recorded recorded => $"recorded {recorded.Amount} in {recorded.RecordedIn}",
        RecordedFigure { Value: { } value } figure => $"recorded {value} {figure.Unit} in {figure.RecordedIn}",
        RecordedFigure { Date: { } date } figure => $"recorded {figure.Unit} {date:yyyy-MM-dd} in {figure.RecordedIn}",
        NotRecorded missing => $"not recorded: {missing.LookedFor}",
        _ => throw new InvalidOperationException("Unreachable: a recorded amount has three shapes."),
    };
}
