using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaCompany.Pilot.Web;

/// <summary>
/// The app record (technical design module M-017), in the user's local application-data folder: the pilot folder, the settings file,
/// the toolchain executable and its SHA-256, the port and the CEO's name. It carries a form version so a later reader refuses a form
/// it does not know.
/// </summary>
public sealed record AppRecord
{
    public const int KnownFormVersion = 1;

    public required int FormVersion { get; init; }
    public required string PilotFolder { get; init; }
    public required string SettingsPath { get; init; }
    public required string ToolchainPath { get; init; }
    public required string ToolchainSha256 { get; init; }
    public required int Port { get; init; }
    public required string CeoName { get; init; }
}

public static class WebJson
{
    /// <summary>Files the app writes: camel case, unknown members refused, indented, Vietnamese kept readable.</summary>
    public static readonly JsonSerializerOptions Files = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Request and response bodies: camel case, unknown members refused.</summary>
    public static readonly JsonSerializerOptions Requests = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value, Files).ReplaceLineEndings("\n") + "\n", new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }

    public static T? Read<T>(string path)
        where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(File.ReadAllBytes(path), Files);
    }
}

public static class AppRecordStore
{
    public const string FileName = "app-record.json";
    public const string RunningFileName = "running-address.txt";

    /// <summary>The default app folder: the user's local application-data folder.</summary>
    public static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MediaCompany", "PilotWeb");

    public static AppRecord? Read(string appFolder)
    {
        AppRecord? record;
        try
        {
            record = WebJson.Read<AppRecord>(Path.Combine(appFolder, FileName));
        }
        catch (JsonException)
        {
            return null;
        }

        return record is { FormVersion: AppRecord.KnownFormVersion } ? record : null;
    }

    public static void Write(string appFolder, AppRecord record) => WebJson.Write(Path.Combine(appFolder, FileName), record);
}
