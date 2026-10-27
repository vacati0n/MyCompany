using System.Diagnostics;
using System.Text;

namespace MediaCompany.Pilot.Web.Tests;

internal sealed class CaseFailure(string message) : Exception(message);

/// <summary>The checks a case makes; each failure stops the case with what was expected and what was found.</summary>
internal static class Expect
{
    public static void True(bool condition, string what)
    {
        if (!condition)
        {
            throw new CaseFailure(what);
        }
    }

    public static void Equal<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new CaseFailure($"{what}: expected <{expected}>, found <{actual}>");
        }
    }

    public static void Contains(string needle, string? haystack, string what)
    {
        if (haystack is null || !haystack.Contains(needle, StringComparison.Ordinal))
        {
            throw new CaseFailure($"{what}: expected to contain <{needle}>, found <{haystack}>");
        }
    }

    public static T Throws<T>(Action action, string what)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T error)
        {
            return error;
        }
        catch (Exception other)
        {
            throw new CaseFailure($"{what}: expected {typeof(T).Name}, got {other.GetType().Name}: {other.Message}");
        }

        throw new CaseFailure($"{what}: expected {typeof(T).Name}, nothing was thrown");
    }
}

/// <summary>Runs the cases in order, prints PASS or FAIL with the case's evidence lines, and returns the number of failures.</summary>
internal sealed class Runner
{
    private readonly List<(string Id, string Name, Action Body)> _cases = [];
    private readonly List<string> _notes = [];

    public void Add(string id, string name, Action body) => _cases.Add((id, name, body));

    /// <summary>An evidence line printed under the running case.</summary>
    public void Note(string line)
    {
        lock (_notes)
        {
            _notes.Add(line);
        }
    }

    public int Run(string? only)
    {
        var failed = 0;
        var run = 0;
        var total = Stopwatch.StartNew();
        foreach (var (id, name, body) in _cases)
        {
            if (only is not null && !id.StartsWith(only, StringComparison.Ordinal))
            {
                continue;
            }

            run++;
            var watch = Stopwatch.StartNew();
            string? failure = null;
            try
            {
                body();
            }
            catch (Exception error)
            {
                failure = error is CaseFailure ? error.Message : $"{error.GetType().Name}: {error.Message}\n{error.StackTrace}";
            }

            var seconds = watch.Elapsed.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            var output = new StringBuilder();
            output.AppendLine(failure is null ? $"PASS {id} {name} ({seconds} s)" : $"FAIL {id} {name} ({seconds} s): {failure}");
            lock (_notes)
            {
                foreach (var note in _notes)
                {
                    output.AppendLine("     " + note);
                }

                _notes.Clear();
            }

            Console.Out.Write(output.ToString());
            Console.Out.Flush();
            if (failure is not null)
            {
                failed++;
            }
        }

        Console.Out.WriteLine($"RESULT: {run - failed} passed, {failed} failed, {run} run ({total.Elapsed.TotalSeconds:0} s)");
        return failed;
    }
}

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        var runner = new Runner();
        UnitCases.Add(runner);
        var flow = new FlowCases(runner);
        flow.Add();
        BrowserCase.Add(runner);
        flow.AddClosing();
        try
        {
            return runner.Run(args.Length > 0 ? args[0] : null);
        }
        finally
        {
            flow.Dispose();
        }
    }
}
