namespace MediaCompany.Pilot;

/// <summary>
/// The operator's input is refused. <see cref="Code"/> is the stable English name the run record, the tests and the operator
/// guide cite; <see cref="Message"/> is the Vietnamese sentence the operator reads, naming the row, field, term, file,
/// sentence or check that failed.
/// </summary>
public sealed class PilotRefusal(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>An installed tool failed or overran its bound; separated from a refusal by its exit status.</summary>
public sealed class PilotToolFailure(string invocation, string detail) : Exception($"{invocation}: {detail}");

/// <summary>The exit statuses of the five verbs.</summary>
public static class PilotExit
{
    public const int Done = 0;
    public const int Usage = 1;
    public const int Refused = 2;
    public const int ToolFailed = 3;
}
