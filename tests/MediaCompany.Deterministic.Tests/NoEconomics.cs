using MediaCompany.Application.Ports;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The admission ledger as a double for the services these demonstrations drive, none of which admits
/// a capability request: every member refuses, so a service that reached it would fail its demonstration
/// rather than pass on a reading nobody took (the AI-economics change).
/// </summary>
internal sealed class NoAdmission : IAdmissionLedger
{
    public Task<BookingReservation> ReserveAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException("No service under this demonstration admits a capability request.");

    public Task<bool> TryHoldScopesAsync(ChannelId channel, CancellationToken cancellationToken) =>
        throw new NotSupportedException("No service under this demonstration holds an admission scope.");

    public Task<AdmissionSnapshot> ReadAsync(
        CapabilityClass capability, Attribution attribution, Money companyAllotment, TaskClass? taskClass, CancellationToken cancellationToken) =>
        throw new NotSupportedException("No service under this demonstration reads an admission snapshot.");

    public Task RestorePricesAsync(IReadOnlyList<ModelPrice> prices, CancellationToken cancellationToken) =>
        throw new NotSupportedException("No service under this demonstration restores admission prices.");

    public Task<GoverningReadingsSummary> GoverningReadingsAsync(Attribution attribution, Money companyAllotment, CancellationToken cancellationToken) =>
        throw new NotSupportedException("No service under this demonstration reads governing readings.");

    public Task RecordDecisionAsync(AdmissionDecisionDraft decision, CancellationToken cancellationToken) =>
        throw new NotSupportedException("No service under this demonstration records an admission decision.");
}

/// <summary>The benchmark writer as a double: no service under these demonstrations records an observation.</summary>
internal sealed class NoBenchmarks : IBenchmarkWriter
{
    public Task<DateTimeOffset> RegisterEntryAsync(CorpusEntryId entry, TaskClass taskClass, CancellationToken cancellationToken) =>
        throw new NotSupportedException("No service under this demonstration registers a corpus entry.");

    public Task<BenchmarkObservation> RecordObservationAsync(BenchmarkObservationDraft draft, CancellationToken cancellationToken) =>
        throw new NotSupportedException("No service under this demonstration records an observation.");
}
