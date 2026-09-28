using System.Reflection;
using MediaCompany.Domain.Analytics;
using Xunit;

namespace MediaCompany.Domain.Tests;

/// <summary>
/// The measurement representation itself (decision D-001).
///
/// These assert over the SHAPE of the union, not over one composer's use of it. The property the
/// whole wave rests on is that the wrong rendering is inexpressible rather than forbidden, and a
/// property of that kind is demonstrable only against the type.
/// </summary>
public sealed class AnalyticsRepresentationTests
{
    private static Type[] Cases() =>
        typeof(MeasurementQuantity).Assembly.GetTypes()
            .Where(t => t.BaseType == typeof(MeasurementQuantity))
            .ToArray();

    /// <summary>The union is closed at exactly three cases, and a fourth is not expressible.</summary>
    [Fact]
    public void TheUnionIsClosedAtThreeCases()
    {
        var cases = Cases().Select(t => t.Name).Order().ToArray();

        Assert.Equal(new[] { "ObservedValue", "ObservedZero", "Unmeasured" }, cases);

        // Every constructor the base declares is private, apart from the record copy constructor
        // the compiler emits, which takes the union's own type and brings no new case into being.
        var declared = typeof(MeasurementQuantity)
            .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(c =>
            {
                var parameters = c.GetParameters();
                return !(parameters.Length == 1 && parameters[0].ParameterType == typeof(MeasurementQuantity));
            })
            .ToArray();

        Assert.NotEmpty(declared);
        Assert.All(declared, c => Assert.True(c.IsPrivate));
    }

    /// <summary>
    /// The heart of it: the unmeasured case carries NO VALUE FIELD OF ANY KIND.
    ///
    /// There is no amount, no unit and no value member on it, so there is nothing a renderer could
    /// print as a number and no slot a caller could default. The rejected wrapper shape keeps such
    /// a slot alive in every state, which is exactly the difference.
    /// </summary>
    [Fact]
    public void TheUnmeasuredCaseCarriesNoValueFieldOfAnyKind()
    {
        var members = typeof(MeasurementQuantity.Unmeasured)
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.DeclaringType == typeof(MeasurementQuantity.Unmeasured))
            .Select(p => p.Name)
            .Where(name => name != "EqualityContract")
            .ToArray();

        Assert.Equal(new[] { "Detail", "Reason" }, members.Order().ToArray());

        Assert.DoesNotContain("Amount", members);
        Assert.DoesNotContain("Unit", members);
        Assert.DoesNotContain("Value", members);

        // And no member of it carries a number at all.
        Assert.DoesNotContain(
            typeof(MeasurementQuantity.Unmeasured)
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => p.DeclaringType == typeof(MeasurementQuantity.Unmeasured)),
            p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?));
    }

    /// <summary>
    /// An observed zero is its own case, and an observed value carrying zero cannot be built.
    ///
    /// The factory routes a zero amount to the zero case, and the case constructors are internal,
    /// so there is no path from outside the assembly to an observed value whose amount is zero.
    /// </summary>
    [Fact]
    public void AnObservedZeroIsItsOwnCaseAndAnObservedValueOfZeroIsNotConstructible()
    {
        Assert.IsType<MeasurementQuantity.ObservedZero>(MeasurementQuantity.Observed(0m, "USD"));
        Assert.IsType<MeasurementQuantity.ObservedZero>(MeasurementQuantity.Zero("USD"));
        Assert.IsType<MeasurementQuantity.ObservedValue>(MeasurementQuantity.Observed(0.01m, "USD"));

        // A negative observation is a real observation: the variance against the envelope is one.
        Assert.IsType<MeasurementQuantity.ObservedValue>(MeasurementQuantity.Observed(-77.41m, "USD"));

        // Every case constructor is PRIVATE, so no caller anywhere -- outside this assembly or
        // inside it -- can reach one. The only path in is the case's own factory.
        Assert.All(Cases(), c =>
        {
            Assert.Empty(c.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            Assert.All(
                c.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(ctor => ctor.GetParameters() is not [{ } only] || only.ParameterType != c),
                ctor => Assert.True(ctor.IsPrivate, $"{c.Name} exposes a constructor that is not private"));
        });

        // And the factory the observed-value case does expose refuses a zero amount outright,
        // which is what makes "an observed value carrying zero is unconstructible" true rather
        // than true-in-practice. The factory is internal, so it is exercised through the union.
        var of = typeof(MeasurementQuantity.ObservedValue)
            .GetMethod("Of", BindingFlags.NonPublic | BindingFlags.Static)!;

        var thrown = Assert.Throws<TargetInvocationException>(() => of.Invoke(null, [0m, "USD"]));
        Assert.IsType<ArgumentOutOfRangeException>(thrown.InnerException);
    }

    /// <summary>The three render differently from one another, everywhere either can appear.</summary>
    [Fact]
    public void TheThreeCasesRenderDifferentlyFromOneAnother()
    {
        var value = MeasurementQuantity.Observed(12.5m, "USD").Describe();
        var zero = MeasurementQuantity.Zero("USD").Describe();
        var unmeasured = MeasurementQuantity
            .NotMeasured(UnmeasuredReason.NoObservationExists, "no operation is recorded")
            .Describe();

        Assert.NotEqual(value, zero);
        Assert.NotEqual(value, unmeasured);
        Assert.NotEqual(zero, unmeasured);

        Assert.Contains("observed zero", zero, StringComparison.Ordinal);
        Assert.Contains("unmeasured", unmeasured, StringComparison.Ordinal);
        Assert.Contains("no observation exists", unmeasured, StringComparison.Ordinal);
        Assert.DoesNotContain("0", zero.Replace("observed zero", string.Empty, StringComparison.Ordinal));
    }

    /// <summary>Both unmeasured reasons are namable, and the set is closed at two.</summary>
    [Fact]
    public void TheUnmeasuredReasonSetIsClosedAtTwoAndEachIsNamedInTheRendering()
    {
        Assert.Equal(2, Enum.GetValues<UnmeasuredReason>().Length);

        Assert.Contains(
            "the source cannot state one",
            MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne, "the library answered another term").Describe(),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// An unmeasured quantity that cannot say what was looked for is refused at construction,
    /// following the delivered label that refuses a blank statement of class limits.
    /// </summary>
    [Fact]
    public void AnUnmeasuredQuantityWithoutADetailIsRefused()
    {
        Assert.Throws<ArgumentException>(
            () => MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, "  "));

        Assert.Throws<ArgumentException>(() => MeasurementQuantity.Observed(1m, " "));
        Assert.Throws<ArgumentException>(() => MeasurementQuantity.Zero(string.Empty));
    }

    // -----------------------------------------------------------------------
    // The admission rule for the six revenue-derived figures (decision D-003)
    // -----------------------------------------------------------------------

    /// <summary>The closed set of six.</summary>
    [Fact]
    public void TheRevenueDerivedFigureSetIsClosedAtSix()
    {
        Assert.Equal(6, Enum.GetValues<RevenueDerivedFigure>().Length);
    }

    /// <summary>
    /// An observed revenue parameter is constructible ONLY from a row carrying the source
    /// observation it came from and that observation's date. There is no public constructor, so a
    /// caller holding a row without both has no instance and therefore no argument to pass.
    /// </summary>
    [Fact]
    public void ARevenueParameterWithoutItsSourceObservationAdmitsNothing()
    {
        Assert.Empty(typeof(ObservedRevenueParameter)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        var amount = MeasurementQuantity.Observed(2.50m, "USD");

        Assert.Null(ObservedRevenueParameter.Admit(new RevenueParameterRecord { Observation = amount }));

        Assert.Null(ObservedRevenueParameter.Admit(new RevenueParameterRecord
        {
            Observation = amount,
            SourceObservation = "a figure somebody assumed",
        }));

        Assert.Null(ObservedRevenueParameter.Admit(new RevenueParameterRecord
        {
            Observation = amount,
            ObservedOn = new DateOnly(2026, 10, 1),
        }));

        // An unmeasured or zero row states no parameter to divide by, so neither admits either.
        Assert.Null(ObservedRevenueParameter.Admit(new RevenueParameterRecord
        {
            Observation = MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, "no amount recorded"),
            SourceObservation = "a platform statement",
            ObservedOn = new DateOnly(2026, 10, 1),
        }));

        Assert.Null(ObservedRevenueParameter.Admit(new RevenueParameterRecord
        {
            Observation = MeasurementQuantity.Zero("USD"),
            SourceObservation = "a platform statement",
            ObservedOn = new DateOnly(2026, 10, 1),
        }));
    }

    /// <summary>A row carrying both admits, and the admitted parameter carries its recorded source.</summary>
    [Fact]
    public void ARevenueParameterRecordedWithItsSourceAndDateAdmits()
    {
        var admitted = ObservedRevenueParameter.Admit(new RevenueParameterRecord
        {
            Observation = MeasurementQuantity.Observed(2.50m, "USD"),
            SourceObservation = "a platform revenue statement",
            ObservedOn = new DateOnly(2026, 10, 1),
        });

        Assert.NotNull(admitted);
        Assert.Equal("a platform revenue statement", admitted!.SourceObservation);
        Assert.Equal(new DateOnly(2026, 10, 1), admitted.ObservedOn);
        Assert.IsType<MeasurementQuantity.ObservedValue>(admitted.Observation);
    }

    // -----------------------------------------------------------------------
    // The caveat and the split-assumption label as construction invariants (D-006, D-007)
    // -----------------------------------------------------------------------

    /// <summary>
    /// The caveat is a construction argument that refuses a blank statement, so no served-tier
    /// output exists without one and no configuration value or display option can suppress it.
    /// </summary>
    [Fact]
    public void TheSingleRecordCaveatRefusesABlankStatement()
    {
        var records = MeasurementQuantity.Count(1, "served-tier records");

        Assert.Throws<ArgumentException>(() => new SingleRecordCaveat(records, "   "));

        var caveat = SingleRecordCaveat.For(records);
        Assert.Contains("does not settle", caveat.Statement, StringComparison.Ordinal);
        Assert.Contains("assumption", caveat.Statement, StringComparison.Ordinal);
    }

    /// <summary>A split-derived figure refuses to exist without its assumption label.</summary>
    [Fact]
    public void ASplitDerivedFigureRefusesABlankAssumptionLabel()
    {
        var caveat = SingleRecordCaveat.For(MeasurementQuantity.Count(0, "served-tier records"));
        var unmeasured = MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, "none recorded");

        Assert.Throws<ArgumentException>(
            () => new TierRatioReadModel(new DateOnly(2026, 10, 1), unmeasured, caveat, " ", "a stated definition"));
    }
}
