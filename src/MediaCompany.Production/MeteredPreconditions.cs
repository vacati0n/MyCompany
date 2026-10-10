using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;

namespace MediaCompany.Production;

/// <summary>What the metered preconditions read: every value, gathered before any call.</summary>
public sealed record MeteredReadiness
{
    public required ProductionPlan Plan { get; init; }

    /// <summary>The store guard's refusals for metered mode; empty where the store is the configured company store.</summary>
    public required IReadOnlyList<string> StoreRefusals { get; init; }

    /// <summary>
    /// The cost controller's refusal or deferral of metered work for the item's channel, read at the datastore's
    /// reserved instant and stated in words, or null where the controller restricts nothing.
    /// </summary>
    public string? ControllerRestriction { get; init; }

    /// <summary>The exact credential variable each planned account's secret is published under, and whether it is set.</summary>
    public required IReadOnlyList<(string Variable, bool Published)> Credentials { get; init; }

    /// <summary>The planned accounts a fake is configured for.</summary>
    public required IReadOnlyList<ProviderAccountId> FakesConfigured { get; init; }

    /// <summary>The planned accounts no vendor endpoint is configured for.</summary>
    public required IReadOnlyList<ProviderAccountId> EndpointsMissing { get; init; }

    public string? NarrationVoice { get; init; }
}

/// <summary>
/// The metered mode's refusal (the production change, decision D-015 of its design), a pure function: every unmet
/// precondition is NAMED, all at once, before any call — a demonstration, undesignated or misidentified store; no
/// recorded cap; an unpriced planned route; a controller refusal or deferral; a missing credential variable,
/// printed by its exact name; a fake configured for a planned account; a missing endpoint; no narration voice.
/// </summary>
public static class MeteredPreconditions
{
    public static IReadOnlyList<string> Unmet(MeteredReadiness readiness)
    {
        ArgumentNullException.ThrowIfNull(readiness);
        var unmet = new List<string>(readiness.StoreRefusals);
        var plan = readiness.Plan;

        if (plan.Package.Cap is null)
        {
            unmet.Add("no cap is recorded for the item");
        }

        if (plan.Route is null)
        {
            unmet.Add("no narration route is admitted");
        }
        else if (plan.Prices is null)
        {
            unmet.Add($"the planned route {plan.Route.Id} has no price in force for a unit kind it is billed by");
        }

        if (readiness.ControllerRestriction is { } restriction)
        {
            unmet.Add($"the cost controller's reading refuses or defers metered work: {restriction}");
        }

        foreach (var (variable, published) in readiness.Credentials)
        {
            if (!published)
            {
                unmet.Add($"the credential variable {variable} is not set");
            }
        }

        foreach (var account in readiness.FakesConfigured)
        {
            unmet.Add($"a fake provider is configured for the planned account {account}; metered mode never composes a fake");
        }

        foreach (var account in readiness.EndpointsMissing)
        {
            unmet.Add($"no vendor endpoint is configured for the planned account {account}");
        }

        if (string.IsNullOrWhiteSpace(readiness.NarrationVoice))
        {
            unmet.Add("no narration voice is configured (setting narrationVoice): the channel's voice is an open owner question, and none is invented");
        }

        return unmet;
    }
}
