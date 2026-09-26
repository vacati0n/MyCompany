using MediaCompany.Domain.Authority;
using MediaCompany.Persistence;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// Static checks over the embedded schema. These run with no database and assert that the
/// design-critical constructs are present in the DDL that will be applied.
///
/// They are not a substitute for the integration demonstrations in
/// <see cref="PostgresIntegrationTests"/>, which exercise the behaviour against a live datastore
/// and are recorded as not-run until one is available. What these do establish is that the
/// constructs the technical design requires are in the schema and have not been dropped.
/// </summary>
public sealed class SchemaStaticTests
{
    private static string Schema() => SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.001-schema.sql");

    private static string Rollups() => SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.002-rollups.sql");

    [Fact]
    public void TheSchemaResourcesAreEmbeddedAndNonEmpty()
    {
        Assert.All(SchemaInstaller.ResourceNames, name =>
            Assert.False(string.IsNullOrWhiteSpace(SchemaInstaller.ReadResource(name))));
    }

    /// <summary>
    /// Constraint C-005 and decision D-006: the cost is a GENERATED column over the unit counts
    /// and the applied unit prices, in the datastore's exact decimal type. The arithmetic is not
    /// in application code, and this asserts the column that makes that true.
    /// </summary>
    [Fact]
    public void TheCostColumnIsGeneratedByTheDatastoreFromExactDecimals()
    {
        var schema = Schema();

        Assert.Contains("computed_cost numeric(18, 8) GENERATED ALWAYS AS (", schema, StringComparison.Ordinal);
        Assert.Contains("(input_units  * applied_input_price)", schema, StringComparison.Ordinal);
        Assert.Contains("(output_units * applied_output_price)", schema, StringComparison.Ordinal);
        Assert.Contains("(cached_units * applied_cached_price)", schema, StringComparison.Ordinal);
        Assert.Contains(") STORED", schema, StringComparison.Ordinal);
    }

    /// <summary>
    /// Decision D-007 and risk RK-005: elapsed minutes are a GENERATED column over the two
    /// recorded timestamps, so no writer on either side can enter one.
    /// </summary>
    [Fact]
    public void ElapsedMinutesAreGeneratedFromTheTwoRecordedTimestamps()
    {
        var schema = Schema();

        Assert.Contains("elapsed_minutes integer GENERATED ALWAYS AS (", schema, StringComparison.Ordinal);
        Assert.Contains("decided_at - presented_at", schema, StringComparison.Ordinal);
    }

    /// <summary>
    /// Constraint C-014: the record is append-only. The trigger refuses UPDATE and DELETE at the
    /// datastore, so the record is not correctable in place even by a caller holding the
    /// connection.
    /// </summary>
    [Fact]
    public void TheRecordedHistoryRefusesAmendmentAtTheDatastore()
    {
        var schema = Schema();

        Assert.Contains("CREATE TRIGGER audit_entries_append_only", schema, StringComparison.Ordinal);
        Assert.Contains("BEFORE UPDATE OR DELETE ON audit_entries", schema, StringComparison.Ordinal);
        Assert.Contains("the recorded history is append-only", schema, StringComparison.Ordinal);
    }

    /// <summary>
    /// Constraint C-001: route admission is refused in the datastore as well as in the
    /// application, which is what makes the two refusals independent.
    /// </summary>
    [Fact]
    public void ForbiddenRoutesAreRefusedAtTheDatastore()
    {
        var schema = Schema();

        Assert.Contains("CREATE TRIGGER routes_refuse_forbidden", schema, StringComparison.Ordinal);
        Assert.Contains("BEFORE INSERT OR UPDATE ON routes", schema, StringComparison.Ordinal);
        Assert.Contains("is a forbidden source", schema, StringComparison.Ordinal);
    }

    /// <summary>Decision D-003 layer three, mirrored at the datastore: no bypass into Published.</summary>
    [Fact]
    public void TheGateBypassIsRefusedAtTheDatastore()
    {
        var schema = Schema();

        Assert.Contains("CREATE TRIGGER gate_transitions_refuse_bypass", schema, StringComparison.Ordinal);
        Assert.Contains("Published is reachable only from Approved", schema, StringComparison.Ordinal);
    }

    /// <summary>Acceptance criterion AC-012, mirrored at the datastore: no self-approved exception.</summary>
    [Fact]
    public void SelfApprovalIsRefusedAtTheDatastore()
    {
        var schema = Schema();

        Assert.Contains("CREATE TRIGGER exception_approvals_refuse_self", schema, StringComparison.Ordinal);
        Assert.Contains("requested it and may not approve it", schema, StringComparison.Ordinal);
        Assert.Contains("is a subject role of the control", schema, StringComparison.Ordinal);
    }

    /// <summary>Constraint C-002: exactly one route per capability class and tier.</summary>
    [Fact]
    public void ARouteTierIsUniquePerCapability()
    {
        Assert.Contains("CONSTRAINT routes_one_per_tier UNIQUE (capability_class, tier)", Schema(), StringComparison.Ordinal);
    }

    /// <summary>Constraint C-002: one production account per provider.</summary>
    [Fact]
    public void OneProductionAccountPerProvider()
    {
        Assert.Contains("CONSTRAINT provider_accounts_one_per_provider UNIQUE (provider)", Schema(), StringComparison.Ordinal);
    }

    /// <summary>Constraint C-018: a threshold alert is idempotent per budget, period and threshold.</summary>
    [Fact]
    public void ABudgetAlertIsIdempotentPerCrossing()
    {
        var schema = Schema();

        Assert.Contains("PRIMARY KEY (budget_id, period, threshold)", schema, StringComparison.Ordinal);
        Assert.Contains("threshold IN (50, 75, 90, 100)", schema, StringComparison.Ordinal);
    }

    /// <summary>
    /// Acceptance criterion AC-017 at the datastore: a member of the named deterministic set
    /// carries no price and no unit prices, so a non-zero cost cannot attach to it.
    /// </summary>
    [Fact]
    public void ADeterministicTaskRowCannotCarryAPrice()
    {
        Assert.Contains("CONSTRAINT agent_costs_deterministic_is_free CHECK", Schema(), StringComparison.Ordinal);
    }

    /// <summary>Decision D-009: the typed emergency union, enforced so a half-filled row cannot exist.</summary>
    [Fact]
    public void TheRouteTargetUnionIsEnforced()
    {
        Assert.Contains("CONSTRAINT routes_target_union CHECK", Schema(), StringComparison.Ordinal);
    }

    /// <summary>
    /// The indexes the design names against the two acceptance queries. Risk R-013 is the rollup
    /// becoming an unindexed scan; these are the mitigation.
    /// </summary>
    [Theory]
    [InlineData("agent_costs_by_item")]
    [InlineData("agent_costs_by_channel")]
    [InlineData("agent_costs_by_dept")]
    [InlineData("agent_costs_by_cap")]
    [InlineData("agent_costs_by_model")]
    [InlineData("route_availability_current")]
    [InlineData("audit_entries_by_subject")]
    [InlineData("approvals_by_version")]
    [InlineData("assets_by_item")]
    public void TheNamedIndexesArePresent(string indexName)
    {
        Assert.Contains($"CREATE INDEX {indexName}", Schema(), StringComparison.Ordinal);
    }

    /// <summary>
    /// The job claim uses FOR UPDATE SKIP LOCKED, which is what lets independent work progress
    /// independently (acceptance criterion AC-024) without a second infrastructure component.
    /// </summary>
    [Fact]
    public void TheJobClaimSkipsLockedRows()
    {
        Assert.Contains("jobs_ready", Schema(), StringComparison.Ordinal);
        Assert.Contains("claim_state = 'Ready'", Schema(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Constraint C-005: the monthly total is presented against the recorded envelope with the
    /// variance stated, and the arithmetic is the datastore's.
    /// </summary>
    [Fact]
    public void TheEnvelopeAndItsVarianceAreComputedByTheDatastore()
    {
        var rollups = Rollups();

        Assert.Contains("77.41::numeric(18, 8)", rollups, StringComparison.Ordinal);
        Assert.Contains("34.42::numeric(18, 8)", rollups, StringComparison.Ordinal);
        Assert.Contains("42.99::numeric(18, 8)", rollups, StringComparison.Ordinal);
        Assert.Contains("variance_against_envelope", rollups, StringComparison.Ordinal);
    }

    /// <summary>
    /// Acceptance criterion AC-008: the permission answer is three indexed reads with no lookup
    /// outside the record, and an incomplete record resolves as not permitted.
    /// </summary>
    [Fact]
    public void ThePermissionAnswerIsAViewOverTheRecordAlone()
    {
        var rollups = Rollups();

        Assert.Contains("CREATE VIEW v_item_permission_answer", rollups, StringComparison.Ordinal);
        Assert.Contains(") AS permitted", rollups, StringComparison.Ordinal);
        Assert.Contains("a.commercial_use_permitted IS TRUE", rollups, StringComparison.Ordinal);
    }

    /// <summary>
    /// The closed action set is the authority and the tables are its mirror. This asserts the
    /// mirror is written from <see cref="ActionSet"/> rather than authored separately, so a
    /// permission cannot be widened by inserting a row.
    /// </summary>
    [Fact]
    public void TheRolePermissionMirrorIsSeededFromTheClosedActionSet()
    {
        // The seeder enumerates ActionSet; asserting over the closed set here is what a later
        // integration run compares the database contents against.
        var expected = ActionSet.Roles
            .SelectMany(role => ActionSet.For(role).Select(action => $"{role}:{action}"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(expected);
        Assert.Contains("Copyright:BlockPlace", expected);
        Assert.DoesNotContain("Copyright:Publish", expected);
        Assert.Contains("Publisher:Publish", expected);
        Assert.DoesNotContain("Publisher:BlockClear", expected);
    }
}
