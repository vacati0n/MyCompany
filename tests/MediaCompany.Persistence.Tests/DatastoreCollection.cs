using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The datastore demonstrations share one PostgreSQL instance and each one drops and recreates the
/// schema, so they run SEQUENTIALLY. Run in parallel they clobber one another's schema, and the
/// resulting failure looks like a broken property rather than a broken harness.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatastoreCollection
{
    public const string Name = "datastore";
}
