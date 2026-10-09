namespace MediaCompany.Domain.Accounting;

/// <summary>
/// The item's recorded cap and what counts against it, as one admission snapshot read them at its reserved
/// instant (the production change, decision D-006 of its design).
///
/// THE COUNTED TOTAL is the stated cost of every operation booked for the item, over all time, plus the WORST
/// CASE of every reservation of the item that no booked operation has reconciled. An attempt lost with its
/// transaction, or one whose recording failed, therefore keeps counting at its worst case: the reservation was
/// committed before the call. Every figure is the datastore's; nothing here computes one from a clock.
/// </summary>
public sealed record ItemCapReading
{
    /// <summary>The recorded cap: a configured amount, never an observation.</summary>
    public required Money Cap { get; init; }

    /// <summary>The recorded statement the cap rests on (the owner's decision).</summary>
    public required string Source { get; init; }

    /// <summary>The stated cost of the item's booked operations.</summary>
    public required Money Booked { get; init; }

    /// <summary>How many of the item's booked operations carry a cost that is not stated.</summary>
    public required long UnstatedOperations { get; init; }

    /// <summary>The worst case of every reservation of the item that no booked operation has reconciled.</summary>
    public required Money OpenReservations { get; init; }

    /// <summary>How many reservations of the item are open.</summary>
    public required long OpenReservationCount { get; init; }

    /// <summary>What counts against the cap.</summary>
    public Money CountedTotal => Booked + OpenReservations;

    /// <summary>What remains of the cap; zero or negative where nothing remains.</summary>
    public Money Remaining => new(Cap.Amount - CountedTotal.Amount, Cap.Currency);

    /// <summary>States the reading, every figure named for what it is.</summary>
    public string Describe() =>
        $"item cap {Cap} (configured, {Source}); counted total {CountedTotal} = booked {Booked} over its operations"
        + $"{(UnstatedOperations > 0 ? $", {UnstatedOperations} of them with a cost not stated" : string.Empty)}"
        + $" plus {OpenReservationCount} open reservation(s) at their worst case {OpenReservations}; remaining {Remaining}";
}
