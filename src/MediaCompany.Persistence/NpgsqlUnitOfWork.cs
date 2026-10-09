using MediaCompany.Application.Ports;
using Npgsql;

namespace MediaCompany.Persistence;

/// <summary>
/// The PostgreSQL realization of the persistence port (module M-017 under stack O-005).
///
/// One transaction carries every writer, so the state change, the queue entry, the operation
/// record and the audit entry commit together. That is what removes the dual write behind
/// constraint C-004: there is no window in which an operation has happened and its record has
/// not, and no outbox is required because there is no second system.
/// </summary>
public sealed class NpgsqlUnitOfWork : IUnitOfWork
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IClock _clock;

    public NpgsqlUnitOfWork(NpgsqlDataSource dataSource, IClock clock)
    {
        _dataSource = dataSource;
        _clock = clock;
    }

    public async Task<IWorkTransaction> BeginAsync(CancellationToken cancellationToken)
    {
        var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            return new NpgsqlWorkTransaction(connection, transaction, _clock);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}

internal sealed class NpgsqlWorkTransaction : IWorkTransaction
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private bool _committed;

    internal NpgsqlWorkTransaction(NpgsqlConnection connection, NpgsqlTransaction transaction, IClock clock)
    {
        _connection = connection;
        _transaction = transaction;

        Audit = new NpgsqlAuditAppender(connection, transaction);
        Operations = new NpgsqlOperationRecorder(connection, transaction);
        Jobs = new NpgsqlJobWriter(connection, transaction);
        Budgets = new NpgsqlBudgetEvaluator(connection, transaction);
        Gates = new NpgsqlGateWriter(connection, transaction);
        Availability = new NpgsqlRouteAvailabilityWriter(connection, transaction);
        Dispatches = new NpgsqlDispatchWriter(connection, transaction);
        Dossiers = new NpgsqlDossierWriter(connection, transaction);

        // The AI-economics change: the admission ledger, whose reservation lives exactly as long as this
        // transaction, and the benchmark writer.
        Admission = new NpgsqlAdmissionLedger(connection, transaction);
        Benchmarks = new NpgsqlBenchmarkWriter(connection, transaction);
    }

    public IAuditAppender Audit { get; }

    public IOperationRecorder Operations { get; }

    public IJobWriter Jobs { get; }

    public IBudgetEvaluator Budgets { get; }

    public IGateWriter Gates { get; }

    public IRouteAvailabilityWriter Availability { get; }

    public IDispatchWriter Dispatches { get; }

    public IDossierWriter Dossiers { get; }

    public IAdmissionLedger Admission { get; }

    public IBenchmarkWriter Benchmarks { get; }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
        {
            // Nothing this transaction wrote is durable. A crash here leaves the job ready rather
            // than lost, which is the property that makes one record per attempt well defined.
            try
            {
                await _transaction.RollbackAsync().ConfigureAwait(false);
            }
            catch (NpgsqlException)
            {
                // The connection is already gone; there is nothing to roll back.
            }
        }

        await _transaction.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
