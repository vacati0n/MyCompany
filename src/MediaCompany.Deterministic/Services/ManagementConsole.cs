using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Analytics;

namespace MediaCompany.Deterministic.Services;

/// <summary>How a management console command ended: the exit the host returns for it.</summary>
public enum ConsoleExit
{
    /// <summary>The read was composed and printed.</summary>
    Printed = 0,

    /// <summary>The week was refused: malformed, or well formed and not existing. Nothing was read.</summary>
    WeekRefused = 2,

    /// <summary>The read ended in one of its named outcomes; nothing was printed but the outcome.</summary>
    ReadEnded = 3,
}

/// <summary>
/// The weekly and dashboard console commands (the AI-management change, decision D-012 of its design, with the
/// correction cycle's named refusal of a week that does not exist). One read, printed as text to the writer the
/// host gives it; no file, no listener, no action. A malformed week and a well-formed week that does not exist
/// (such as 2027-W53) are both refused by name with the same exit; a read that ends prints its named outcome.
/// </summary>
public static class ManagementConsole
{
    public static async Task<ConsoleExit> RunAsync(
        ManagementReportService service,
        string command,
        string? weekArgument,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        ManagementRead read;
        try
        {
            read = await service.ReadAsync(ReportWeek.Parse(weekArgument), cancellationToken).ConfigureAwait(false);
        }
        catch (FormatException malformed)
        {
            await error.WriteLineAsync($"Week refused: {malformed.Message}").ConfigureAwait(false);
            return ConsoleExit.WeekRefused;
        }
        catch (ReportWeekNotFoundException missing)
        {
            await error.WriteLineAsync($"Week refused: {missing.Message}").ConfigureAwait(false);
            return ConsoleExit.WeekRefused;
        }
        catch (CompanyReadException ended)
        {
            await error.WriteLineAsync($"The read ended without a snapshot ({ended.Failure}): {ended.Detail}").ConfigureAwait(false);
            return ConsoleExit.ReadEnded;
        }

        var reports = read.Reports;
        foreach (var line in ManagementRendering.Header(
                     reports.Instant, reports.StoredHorizon, reports.Week, reports.PeriodStart, reports.PeriodEnd, reports.FinalityStatement))
        {
            await output.WriteLineAsync(line).ConfigureAwait(false);
        }

        if (command == "weekly")
        {
            foreach (var report in reports.Lines.GroupBy(l => l.Report))
            {
                await output.WriteLineAsync().ConfigureAwait(false);
                await output.WriteLineAsync($"=== {report.Key} report ===").ConfigureAwait(false);
                foreach (var section in report.GroupBy(l => l.Heading))
                {
                    await output.WriteLineAsync($"--- {section.Key} ---").ConfigureAwait(false);
                    foreach (var line in section)
                    {
                        await output.WriteLineAsync($"  {ManagementRendering.Render(line)}").ConfigureAwait(false);
                    }
                }
            }

            await output.WriteLineAsync().ConfigureAwait(false);
            await output.WriteLineAsync("=== CEO brief ===").ConfigureAwait(false);
            foreach (var section in read.Brief.Sections)
            {
                await output.WriteLineAsync($"--- {section.Section} ---").ConfigureAwait(false);
                foreach (var item in section.Items)
                {
                    await output.WriteLineAsync($"  {ManagementRendering.Render(item)}").ConfigureAwait(false);
                }
            }
        }
        else
        {
            await output.WriteLineAsync().ConfigureAwait(false);
            await output.WriteLineAsync("=== CEO dashboard (read-only; one tile per brief line, from the same read) ===").ConfigureAwait(false);
            foreach (var tile in read.Tiles)
            {
                await output.WriteLineAsync($"[{tile.Section}] {tile.Rendering}").ConfigureAwait(false);
            }
        }

        return ConsoleExit.Printed;
    }
}
