namespace MediaCompany.Domain.Analytics;

/// <summary>
/// The six revenue-derived figures (decision D-003). The set is closed, and every member is
/// present in the delivered surface and visible in none of its views while no revenue parameter
/// is observed.
/// </summary>
public enum RevenueDerivedFigure
{
    Revenue = 1,
    RevenuePerMille = 2,
    ProfitPerItem = 3,
    ProfitPerChannel = 4,
    ReturnOnInvestment = 5,
    CostPerDollarOfRevenue = 6,
}

/// <summary>
/// One row of the revenue-parameter register, as recorded. A row may be recorded WITHOUT a source
/// observation, and such a row is an assumed parameter: it is admitted to nothing.
///
/// The recorded amount is carried as a <see cref="MeasurementQuantity"/> rather than as a bare
/// number, so a register row is subject to the same three states as every other analytics
/// quantity and no renderer can reach a number on it without resolving the case first.
/// </summary>
public sealed record RevenueParameterRecord
{
    public required MeasurementQuantity Observation { get; init; }

    /// <summary>The observation the parameter came from. Null means it was recorded without one.</summary>
    public string? SourceObservation { get; init; }

    /// <summary>The date of that source observation. Null means none was recorded.</summary>
    public DateOnly? ObservedOn { get; init; }
}

/// <summary>
/// A revenue parameter that COUNTS AS OBSERVED (decision D-003, assumption A-002 of the accepted
/// design): one recorded together with the source observation it came from and the date of that
/// observation.
///
/// The constructor is private and the only way in is <see cref="Admit"/>, which returns null for
/// a row missing either. A parameter recorded without a source is therefore not this type, and a
/// caller holding one has no argument it can pass to the admission function that lights any of
/// the six figures. With nothing recorded there is no instance at all, so the six are unlit by
/// default rather than filtered out of a view.
/// </summary>
public sealed record ObservedRevenueParameter
{
    private ObservedRevenueParameter(
        MeasurementQuantity observation,
        string sourceObservation,
        DateOnly observedOn)
    {
        Observation = observation;
        SourceObservation = sourceObservation;
        ObservedOn = observedOn;
    }

    public MeasurementQuantity Observation { get; }

    /// <summary>The source observation. Never blank: a blank one does not admit.</summary>
    public string SourceObservation { get; }

    public DateOnly ObservedOn { get; }

    /// <summary>
    /// Admits a recorded row, or returns null. Null is the whole mechanism: there is no other
    /// constructor, so a row without a source observation and its date yields no instance and
    /// therefore no argument for the admission function.
    /// </summary>
    public static ObservedRevenueParameter? Admit(RevenueParameterRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.SourceObservation) || record.ObservedOn is null)
        {
            return null;
        }

        // An unmeasured or zero row states no revenue parameter to divide by, so it admits
        // nothing either. Only an observed non-zero amount is a parameter.
        if (record.Observation is not MeasurementQuantity.ObservedValue)
        {
            return null;
        }

        return new ObservedRevenueParameter(record.Observation, record.SourceObservation, record.ObservedOn.Value);
    }
}
