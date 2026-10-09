using MediaCompany.Application.Ports;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Deterministic.Accounting;

/// <summary>
/// The cost controller (the AI-economics change, decision D-004 of its design), a member of the
/// zero-AI-cost set under the name <see cref="DeterministicTaskRegistry.CostControl"/>.
///
/// A PURE FIXED RULE. It maps each governing reading of one booking month to one action by the four
/// thresholds the delivered budget tracking already fixes — below 50 percent none, at 50 the delivered
/// alert and nothing else, at 75 a reasoning-tier downgrade, at 90 a deferral, at 100 a refusal — and
/// applies the MOST SEVERE action any reading demands. The thresholds and the mapping are code: this
/// type takes no configuration dependency of any kind, which the build-time suite asserts, so no key
/// can reach a threshold, the mapping or an admission past a refusal.
///
/// A reading nobody can state is NEVER READ AS ZERO OR AS HEADROOM. A channel with no budget amount
/// recorded for the month refuses metered work under its own reason, naming the channel and month; a
/// scope whose booked spend cannot be stated, because an operation booked into the month for it carries
/// an unstated cost, refuses metered work under its own reason, naming the scope, the month and how many
/// operations. Neither is the exceeded reason. Booked spend reads OBSERVED ZERO where nothing is booked,
/// because the operation record is the complete ledger of the company's booked metered operations, so
/// the first metered operation of a month is admissible.
///
/// The company reading is governed against the RECORDED METERED ALLOTMENT, the metered line of the
/// owner-approved envelope, as the interim basis while the standing charge has no recorded home, and
/// every decision states it. Its only effects are the action and the reason it returns, which the
/// resolution function reads to restrict route choice; it reaches no compliance or copyright control.
/// </summary>
public static class CostController
{
    public const string TaskName = DeterministicTaskRegistry.CostControl;

    /// <summary>Where the company basis is recorded. A code constant, owner-approved, never an invention.</summary>
    public const string CompanyBasisRecordedIn =
        "the metered line of the owner-approved monthly envelope, a code constant of the accounting domain";

    /// <summary>Where a channel's budget amount is recorded.</summary>
    public const string ChannelBudgetRecordedIn = "the budget register";

    /// <summary>The interim company basis: the recorded metered allotment, as a recorded amount.</summary>
    public static RecordedAmount CompanyBasis { get; } = RecordedAmount.Of(ApprovedEnvelope.Metered, CompanyBasisRecordedIn);

    /// <summary>What the company reading is governed against, stated on every decision.</summary>
    public static string CompanyBasisStatement { get; } =
        $"the company reading is governed against the recorded metered allotment of {ApprovedEnvelope.Metered}, the "
        + $"metered line of the owner-approved monthly envelope of {ApprovedEnvelope.MonthlyTotal} ({ApprovedEnvelope.Metered} "
        + $"metered and {ApprovedEnvelope.Standing} standing), as the interim basis while the standing charge has no "
        + "recorded home; governing against the whole envelope would admit metered spend the standing charge then "
        + "carries past it. Booked spend covers the metered operations the operation record carries, which is the "
        + "complete ledger of the company's booked metered operations, and nothing else; a budget, the envelope and "
        + "the allotment are recorded amounts, never observations";

    /// <summary>
    /// The action one threshold demands. The mapping is fixed here and nowhere else; the thresholds are
    /// the delivered closed set.
    /// </summary>
    public static ControllerAction ActionFor(int? threshold) => threshold switch
    {
        null => ControllerAction.None,
        50 => ControllerAction.AlertOnly,
        75 => ControllerAction.Downgrade,
        90 => ControllerAction.Defer,
        100 => ControllerAction.Refuse,
        _ => throw new ArgumentOutOfRangeException(
            nameof(threshold), threshold, "The threshold set is closed at 50, 75, 90 and 100."),
    };

    /// <summary>
    /// The decision for one admission: the channel reading and the company reading of the booking
    /// month, from the one statement that read them, and the most severe action either demands.
    /// </summary>
    public static ControllerDecision Decide(GoverningReadingsSummary summary, DateTimeOffset decidedAt)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var channelAmount = summary.ChannelBudgetAmount is { } amount
            ? RecordedAmount.Of(amount, ChannelBudgetRecordedIn)
            : RecordedAmount.Missing(
                $"no budget amount is recorded for channel {summary.Channel} in period {summary.Month:yyyy-MM} in "
                + ChannelBudgetRecordedIn);

        var readings = new[]
        {
            Reading(
                GoverningScope.Channel,
                summary.Channel.Value,
                $"channel {summary.Channel}",
                summary.Month,
                channelAmount,
                summary.ChannelSpend,
                summary.ChannelUtilisationPercent),
            Reading(
                GoverningScope.Company,
                summary.Company?.Value,
                "the company",
                summary.Month,
                CompanyBasis,
                summary.CompanySpend,
                summary.CompanyUtilisationPercent),
        };

        // The most severe action any reading demands; between two readings demanding the same action the
        // channel's is named first, because it is the narrower scope.
        var deciding = readings.OrderByDescending(r => r.Action).First();
        var action = deciding.Action;

        return new ControllerDecision
        {
            BookingMonth = summary.Month,
            DecidedAt = decidedAt,
            Readings = readings,
            Action = action,
            Reason = action switch
            {
                ControllerAction.Refuse => deciding.Reason ?? RefusalReason.RefusedAtThreshold,
                ControllerAction.Defer => RefusalReason.DeferredAtThreshold,
                _ => null,
            },
            DecidedBy = action == ControllerAction.None ? null : deciding.Scope,
            CompanyBasisStatement = CompanyBasisStatement,
        };
    }

    /// <summary>Whether an action restricts resolution to the targets that cost nothing.</summary>
    public static bool RestrictsToZeroCost(ControllerAction action) =>
        action is ControllerAction.Defer or ControllerAction.Refuse;

    private static GoverningReading Reading(
        GoverningScope scope,
        Guid? scopeId,
        string label,
        DateOnly month,
        RecordedAmount amount,
        ScopeSpend spend,
        decimal? percent)
    {
        var bookedSpend = spend.UnstatedOperations > 0
            ? MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"{spend.UnstatedOperations} of the {spend.Operations} operations booked into {month:yyyy-MM} for {label} "
                + "carry a cost that is not stated, so the booked spend of the scope cannot be stated")
            : MeasurementQuantity.Observed(spend.Booked.Amount, spend.Booked.Currency);

        MeasurementQuantity utilisation;
        int? threshold = null;
        ControllerAction action;
        RefusalReason? reason = null;

        if (amount is RecordedAmount.NotRecorded missing)
        {
            utilisation = MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"no utilisation of {label} is computed for {month:yyyy-MM} against an amount nobody recorded: {missing.LookedFor}");
            action = ControllerAction.Refuse;
            reason = RefusalReason.BudgetAmountNotRecorded;
        }
        else if (bookedSpend is MeasurementQuantity.Unmeasured)
        {
            utilisation = MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"the utilisation of {label} for {month:yyyy-MM} cannot be stated, because its booked spend cannot");
            action = ControllerAction.Refuse;
            reason = RefusalReason.SpendUnmeasured;
        }
        else if (percent is not { } observed)
        {
            // A recorded amount and a stated spend with no utilisation computed is a read that failed to
            // compute one; it is refused rather than read as headroom.
            utilisation = MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"the datastore returned no utilisation of {label} for {month:yyyy-MM}");
            action = ControllerAction.Refuse;
            reason = RefusalReason.SpendUnmeasured;
        }
        else
        {
            utilisation = MeasurementQuantity.Observed(observed, "percent");
            threshold = BudgetThresholds.HighestReached(observed);
            action = ActionFor(threshold);
        }

        var demands = action switch
        {
            ControllerAction.None => "no action: below every threshold",
            ControllerAction.AlertOnly => "the delivered alert only, at the 50 percent threshold",
            ControllerAction.Downgrade => "a reasoning-tier downgrade, at the 75 percent threshold",
            ControllerAction.Defer => "a deferral of metered work, at the 90 percent threshold",
            ControllerAction.Refuse when reason == RefusalReason.BudgetAmountNotRecorded =>
                "a refusal of metered work, because no budget amount is recorded; this is not the budget exceeded",
            ControllerAction.Refuse when reason == RefusalReason.SpendUnmeasured =>
                "a refusal of metered work, because the booked spend cannot be stated; it is read neither as zero nor as headroom",
            _ => "a refusal of metered work, at the 100 percent threshold",
        };

        return new GoverningReading
        {
            Scope = scope,
            ScopeId = scopeId,
            Month = month,
            Amount = amount,
            BookedSpend = bookedSpend,
            Utilisation = utilisation,
            Threshold = threshold is { } t ? (BudgetThreshold)t : null,
            Action = action,
            Reason = reason,
            Statement = $"{label}, booking month {month:yyyy-MM}: amount {amount.Describe()}; booked spend "
                + $"{bookedSpend.Describe()}; utilisation {utilisation.Describe()}; demands {demands}",
        };
    }
}
