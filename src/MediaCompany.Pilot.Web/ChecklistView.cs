namespace MediaCompany.Pilot.Web;

/// <summary>One kit 04 item as the package's checklist file shows it, and whether the operator ticks it.</summary>
public sealed record ChecklistRow(string Id, string Wording, string Who, bool ToolTicked, string Note, bool OperatorItem);

/// <summary>The package's checklist, and whether its identifiers equal the held list (approval is offered only then).</summary>
public sealed record ChecklistReading(IReadOnlyList<ChecklistRow> Rows, bool MatchesHeld, string? Problem);

/// <summary>
/// THE CHECKLIST (design decision D-012). Read from the package's checklist.md, which the toolchain wrote: the 27 kit 04 items in held
/// order, B8 decided by the toolchain, B3 and B5 noted but left unticked. Approval is not offered when the file's identifiers differ
/// from the held list. The operator items are every held item the toolchain does not tick: 26.
/// </summary>
public static class ChecklistView
{
    public static IReadOnlyList<string> OperatorItems(HeldData held)
    {
        ArgumentNullException.ThrowIfNull(held);
        return held.Checklist.Select(i => i.Id).Where(id => !held.ChecklistTicked.Contains(id, StringComparer.Ordinal)).ToList();
    }

    public static ChecklistReading Read(string packageFolder, HeldData held)
    {
        ArgumentNullException.ThrowIfNull(held);
        var path = Path.Combine(packageFolder, PackageFiles.Checklist);
        if (!File.Exists(path))
        {
            return new ChecklistReading([], false, $"Gói không có tệp {PackageFiles.Checklist}.");
        }

        var operatorItems = OperatorItems(held);
        var rows = new List<ChecklistRow>();
        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("| ", StringComparison.Ordinal) || trimmed.StartsWith("| # ", StringComparison.Ordinal) || trimmed.StartsWith("|---", StringComparison.Ordinal))
            {
                continue;
            }

            var cells = trimmed.Trim('|').Split(" | ").Select(c => c.Trim()).ToArray();
            if (cells.Length != 4)
            {
                return new ChecklistReading(rows, false, $"Dòng \"{trimmed}\" của {PackageFiles.Checklist} không đúng dạng bốn cột.");
            }

            var result = cells[3];
            var ticked = result.StartsWith('☑');
            var note = result.TrimStart('☑', '☐').Trim();
            rows.Add(new ChecklistRow(cells[0], cells[1], cells[2], ticked, note, operatorItems.Contains(cells[0], StringComparer.Ordinal)));
        }

        var held_ = held.Checklist.Select(i => i.Id).ToList();
        var same = rows.Select(r => r.Id).SequenceEqual(held_, StringComparer.Ordinal);
        return same
            ? new ChecklistReading(rows, true, null)
            : new ChecklistReading(rows, false,
                $"Danh sách duyệt của gói có {rows.Count} mục ({string.Join(", ", rows.Select(r => r.Id))}), khác {held_.Count} mục của mẫu 04. Không duyệt gói này; tạo video lại ở bước 6.");
    }
}
