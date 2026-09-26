namespace MediaCompany.Domain.Capabilities;

/// <summary>
/// The attribution tuple every operation carries, per constraint C-004. An operation recorded
/// without the full tuple cannot be re-attributed afterwards (sequencing constraint P-003),
/// so every member is required at construction.
/// </summary>
public sealed record Attribution
{
    public Attribution(ItemId item, ChannelId channel, DepartmentId department, AgentId agent)
    {
        Item = item;
        Channel = channel;
        Department = department;
        Agent = agent;
    }

    public ItemId Item { get; }
    public ChannelId Channel { get; }
    public DepartmentId Department { get; }
    public AgentId Agent { get; }
}
