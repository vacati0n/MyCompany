namespace MediaCompany.Domain.Analytics;

/// <summary>
/// Why a quantity is unmeasured. The set is CLOSED at two members, so a third reason is a code
/// change a reviewer sees rather than a string somebody invented at a call site.
/// </summary>
public enum UnmeasuredReason
{
    /// <summary>No observation of this quantity exists in the company's own records.</summary>
    NoObservationExists = 1,

    /// <summary>An observation exists, and the source it came from cannot state this quantity.</summary>
    SourceCannotStateOne = 2,
}

/// <summary>
/// Every analytics quantity, in exactly one of three cases (decision D-001).
///
/// The union is closed by a private constructor, so a fourth case is not expressible from outside
/// this file. Two properties of the shape are load-bearing, and neither is a convention.
///
/// First, <see cref="Unmeasured"/> carries NO VALUE FIELD OF ANY KIND. There is nothing on it a
/// renderer could print as a number, nothing a caller could default, and no slot against which
/// a null-coalescing zero is even expressible. The rejected alternative — a wrapper carrying a
/// measurement state beside an optional value — keeps the value slot alive in every state, and a
/// slot that exists in every state is one a caller eventually fills.
///
/// Second, <see cref="ObservedZero"/> is its OWN CASE rather than an <see cref="ObservedValue"/>
/// whose amount is zero. A reader resolves the three apart without inspecting a number, so the
/// difference between "the company measured this and it was zero" and "the company never measured
/// this" survives every rendering. <see cref="Observed"/> routes a zero amount to that case, which
/// is why no observed value carrying zero can be constructed at all.
/// </summary>
public abstract record MeasurementQuantity
{
    private MeasurementQuantity()
    {
    }

    /// <summary>A unit is required on every observed case; a figure with no unit is not one.</summary>
    private static string RequireUnit(string unit) =>
        string.IsNullOrWhiteSpace(unit)
            ? throw new ArgumentException("An observed quantity carries the unit it was observed in.", nameof(unit))
            : unit;

    /// <summary>An observation with a non-zero amount, in the unit it was observed in.</summary>
    public sealed record ObservedValue : MeasurementQuantity
    {
        internal ObservedValue(decimal amount, string unit)
        {
            Amount = amount;
            Unit = unit;
        }

        /// <summary>The observed amount. Never zero: a zero observation is <see cref="ObservedZero"/>.</summary>
        public decimal Amount { get; }

        public string Unit { get; }
    }

    /// <summary>
    /// An observation whose value was zero. It carries the unit and NO AMOUNT FIELD, because the
    /// amount is not what distinguishes it — the case is.
    /// </summary>
    public sealed record ObservedZero : MeasurementQuantity
    {
        internal ObservedZero(string unit) => Unit = unit;

        public string Unit { get; }
    }

    /// <summary>
    /// No measurement. Carries the closed reason and a required detail naming what was looked for
    /// and where, and carries no value field, no unit and no amount.
    /// </summary>
    public sealed record Unmeasured : MeasurementQuantity
    {
        internal Unmeasured(UnmeasuredReason reason, string detail)
        {
            Reason = reason;
            Detail = detail;
        }

        public UnmeasuredReason Reason { get; }

        /// <summary>What was looked for and where. Blank is refused at construction.</summary>
        public string Detail { get; }
    }

    /// <summary>
    /// An observed quantity. An amount of zero yields <see cref="ObservedZero"/>, which is what
    /// makes an observed value carrying zero unconstructible rather than merely discouraged.
    /// </summary>
    public static MeasurementQuantity Observed(decimal amount, string unit) =>
        amount == 0m ? new ObservedZero(RequireUnit(unit)) : new ObservedValue(amount, RequireUnit(unit));

    /// <summary>An observation of zero, stated as such.</summary>
    public static MeasurementQuantity Zero(string unit) => new ObservedZero(RequireUnit(unit));

    /// <summary>
    /// No measurement, with the reason and the detail. A blank detail is refused, following the
    /// delivered precedent that refuses a blank statement of what a recorded set does and does not
    /// establish: an unmeasured quantity that cannot say what was looked for is not diagnosable.
    /// </summary>
    public static MeasurementQuantity NotMeasured(UnmeasuredReason reason, string detail) =>
        string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException(
                "An unmeasured quantity states what was looked for and where; without it the absence is not diagnosable.",
                nameof(detail))
            : new Unmeasured(reason, detail);

    /// <summary>
    /// A count of records, which is an observation whenever the counting happened. Zero recorded
    /// rows is an observed zero; a count that was never taken is <see cref="NotMeasured"/>.
    /// </summary>
    public static MeasurementQuantity Count(long records, string unit = "records") =>
        Observed(records, unit);

    /// <summary>
    /// The rendering. The three cases render differently from one another by construction, and the
    /// unmeasured rendering names which of the two reasons applies.
    /// </summary>
    public string Describe() => this switch
    {
        ObservedValue observed => $"{observed.Amount} {observed.Unit}",
        ObservedZero zero => $"observed zero {zero.Unit}",
        Unmeasured unmeasured => $"unmeasured ({Describe(unmeasured.Reason)}): {unmeasured.Detail}",
        _ => throw new InvalidOperationException("Unreachable: the measurement union has three cases."),
    };

    private static string Describe(UnmeasuredReason reason) => reason switch
    {
        UnmeasuredReason.NoObservationExists => "no observation exists",
        UnmeasuredReason.SourceCannotStateOne => "the source cannot state one",
        _ => throw new InvalidOperationException("Unreachable: the reason set has two members."),
    };
}
