namespace MediaCompany.Domain.Capabilities;

/// <summary>
/// A rated quality level on a closed 0..100 scale. A request carries a floor and a route
/// carries a rating; the floor filter of design step 2 runs before tier ordering, so a route
/// below the floor is never a candidate (constraint C-003).
/// </summary>
public readonly record struct QualityRating : IComparable<QualityRating>
{
    public int Value { get; }

    public QualityRating(int value)
    {
        if (value is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Quality rating is a 0..100 scale.");
        }

        Value = value;
    }

    public int CompareTo(QualityRating other) => Value.CompareTo(other.Value);

    public static bool operator <(QualityRating left, QualityRating right) => left.Value < right.Value;
    public static bool operator >(QualityRating left, QualityRating right) => left.Value > right.Value;
    public static bool operator <=(QualityRating left, QualityRating right) => left.Value <= right.Value;
    public static bool operator >=(QualityRating left, QualityRating right) => left.Value >= right.Value;

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Context capacity a model offers, or a request requires, counted in units the provider meters.
/// </summary>
public readonly record struct ContextCapacity(int Units) : IComparable<ContextCapacity>
{
    public int CompareTo(ContextCapacity other) => Units.CompareTo(other.Units);

    public static bool operator <(ContextCapacity left, ContextCapacity right) => left.Units < right.Units;
    public static bool operator >(ContextCapacity left, ContextCapacity right) => left.Units > right.Units;
    public static bool operator <=(ContextCapacity left, ContextCapacity right) => left.Units <= right.Units;
    public static bool operator >=(ContextCapacity left, ContextCapacity right) => left.Units >= right.Units;

    public override string ToString() => Units.ToString();
}
