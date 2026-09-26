namespace MediaCompany.Domain;

/// <summary>
/// Identity types for the entities of the technical design's data model. Each is a distinct
/// type so that an identifier of one kind cannot be passed where another is expected.
/// </summary>
public readonly record struct CompanyId(Guid Value)
{
    public static CompanyId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct ChannelId(Guid Value)
{
    public static ChannelId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct DepartmentId(Guid Value)
{
    public static DepartmentId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct AgentId(Guid Value)
{
    public static AgentId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct ItemId(Guid Value)
{
    public static ItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct AssetId(Guid Value)
{
    public static AssetId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct JobId(Guid Value)
{
    public static JobId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct RunId(Guid Value)
{
    public static RunId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct OperationId(Guid Value)
{
    public static OperationId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct AuditEntryId(Guid Value)
{
    public static AuditEntryId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct BudgetId(Guid Value)
{
    public static BudgetId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct ModelId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct ProviderAccountId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct ModelPriceId(Guid Value)
{
    public static ModelPriceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct RouteId(Guid Value)
{
    public static RouteId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// An item version. An approval binds to one of these exactly, per constraint C-016.
/// </summary>
public readonly record struct ItemVersion(int Value)
{
    public override string ToString() => Value.ToString();
}
