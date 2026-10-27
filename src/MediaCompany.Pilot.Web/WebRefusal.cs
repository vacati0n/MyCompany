namespace MediaCompany.Pilot.Web;

/// <summary>
/// A step of the app refused the operator's request before any toolchain call (technical design, error handling: a step pre-check
/// failure is shown in Vietnamese and stops before the toolchain). <see cref="Code"/> is a stable English name for the log and the
/// tests; the message is the Vietnamese sentence the operator reads, with the reason and what to do next.
/// </summary>
public sealed class WebRefusal(string code, string message, int status = 400) : Exception(message)
{
    public string Code { get; } = code;

    public int Status { get; } = status;
}
