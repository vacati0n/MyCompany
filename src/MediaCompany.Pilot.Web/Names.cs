namespace MediaCompany.Pilot.Web;

/// <summary>
/// THE APPROVER TEXT (design decision D-005, Design Gate option O-015). The toolchain stores the approver text as given; the app composes
/// it in one of exactly two forms. The CEO presses approve: "[CEO name] (CEO)". An employee presses approve on the CEO's written
/// instruction: "[CEO name] (CEO; nhân viên [employee name] bấm duyệt theo chỉ thị bằng văn bản của CEO)". Names are NFC-normalised
/// and trimmed; a blank name, or a name with a control character, "(", ")" or ";", is refused.
/// </summary>
public static class ApproverText
{
    public static string Compose(string ceoName, string? employeeName)
    {
        var ceo = CleanName(ceoName, "Tên CEO");
        if (employeeName is null)
        {
            return $"{ceo} (CEO)";
        }

        var employee = CleanName(employeeName, "Tên nhân viên bấm duyệt");
        return $"{ceo} (CEO; nhân viên {employee} bấm duyệt theo chỉ thị bằng văn bản của CEO)";
    }

    /// <summary>The name NFC-normalised and trimmed, refused when blank or when it holds a control character, "(", ")" or ";".</summary>
    public static string CleanName(string? raw, string what)
    {
        var name = VietnameseText.Nfc((raw ?? string.Empty).Trim());
        if (name.Length == 0)
        {
            throw new WebRefusal("name-missing", $"Thiếu {what.ToLowerInvariant()}. Gõ tên rồi bấm lại.");
        }

        if (VietnameseText.FirstControl(name) is { } control)
        {
            throw new WebRefusal("name-invalid", $"{what} có ký tự điều khiển U+{control.Value:X4}. Gõ lại tên chỉ bằng chữ và khoảng trắng.");
        }

        if (name.IndexOfAny(['(', ')', ';']) >= 0)
        {
            throw new WebRefusal("name-invalid", $"{what} không được có dấu \"(\", \")\" hoặc \";\". Gõ lại tên không có các dấu đó.");
        }

        return name;
    }
}

/// <summary>
/// Plain file names (technical design, security: an uploaded or typed name is never used as a path). The toolchain's own rule decides:
/// <see cref="InputFiles.Resolve"/> refuses a name that is not a plain file name before it looks for the file, so a "file-missing"
/// answer means the name is plain.
/// </summary>
public static class PlainName
{
    public static string Require(string? raw, string where, string folder)
    {
        var name = VietnameseText.Nfc((raw ?? string.Empty).Trim());
        try
        {
            InputFiles.Resolve(folder, name, where);
        }
        catch (PilotRefusal refusal) when (refusal.Code == "file-missing")
        {
            // The rule passed; the file simply is not there yet.
        }

        return name;
    }
}
