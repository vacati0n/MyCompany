using System.Globalization;
using System.Text;

namespace MediaCompany.Pilot.Web;

/// <summary>
/// THE LOCAL LOG (design decision D-014): one line per request with the time, method, route, status, refusal code, toolchain exit status
/// and duration. Never the query, a body, a file's contents, a typed name or the per-start token. Before setup the log sits in the app
/// folder; after setup in the pilot folder's web folder.
/// </summary>
public sealed class WebLog(Func<string> folder)
{
    public const string FileName = "nhat-ky.log";
    private readonly object _gate = new();

    public string FilePath => Path.Combine(folder(), FileName);

    public void Write(string method, string route, int status, string? code, int? exitCode, double milliseconds)
    {
        var line = string.Create(CultureInfo.InvariantCulture,
            $"{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\t{method}\t{route}\t{status}\t{code ?? "-"}\t{(exitCode is { } e ? e.ToString(CultureInfo.InvariantCulture) : "-")}\t{milliseconds:0}ms\n");
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(folder());
                File.AppendAllText(FilePath, line, new UTF8Encoding(false));
            }
            catch (IOException)
            {
                // A log line that cannot be written does not stop the operator's work.
            }
            catch (UnauthorizedAccessException)
            {
                // As above.
            }
        }
    }
}
