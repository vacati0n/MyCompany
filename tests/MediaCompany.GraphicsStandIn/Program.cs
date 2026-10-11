using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// The stand-in for Node running the render script. Expected arguments, exactly: <staged script name> <bundle|render> <request name>.
// Expected environment, exactly: SystemRoot, windir, PATH (the system folder alone), TEMP, TMP, LOCALAPPDATA, APPDATA, USERPROFILE and
// HOME (all one folder inside the working directory), CHROME_LOG_FILE (inside it too), TZ=UTC and NODE_ENV=production. It reads its start line before anything else.
// Exit codes follow the render script: 0 done, 3 refused before the renderer was called, 4 the renderer failed; 9 a contract breach.

if (args.Length == 1 && args[0] == "--sleeper")
{
    Thread.Sleep(TimeSpan.FromMinutes(10));
    return 0;
}

if (args.Length != 3 || args[1] is not ("bundle" or "render"))
{
    return Breach($"the arguments are not a script, a mode and a request: [{string.Join(", ", args)}]");
}

if (args[0].IndexOfAny(['/', '\\', ':']) >= 0 || args[2].IndexOfAny(['/', '\\', ':']) >= 0 || !File.Exists(args[0]) || !File.Exists(args[2]))
{
    return Breach("the script and the request are not plain names of files in the working directory");
}

var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SystemRoot", "windir", "PATH", "TEMP", "TMP", "LOCALAPPDATA", "APPDATA", "USERPROFILE", "HOME", "TZ", "NODE_ENV", "CHROME_LOG_FILE" };
var environment = Environment.GetEnvironmentVariables().Keys.Cast<string>().ToArray();
if (environment.FirstOrDefault(k => !allowed.Contains(k)) is { } extra)
{
    return Breach($"the environment holds {extra}, which the starter does not pass");
}

var workingDirectory = Path.GetFullPath(Directory.GetCurrentDirectory()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
foreach (var redirected in new[] { "TEMP", "TMP", "LOCALAPPDATA", "APPDATA", "USERPROFILE", "HOME", "CHROME_LOG_FILE" })
{
    var value = Environment.GetEnvironmentVariable(redirected);
    if (value is null || !Path.GetFullPath(value).StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
    {
        return Breach($"{redirected} does not point inside the staging folder");
    }
}

if (Environment.GetEnvironmentVariable("TZ") != "UTC" || Environment.GetEnvironmentVariable("NODE_ENV") != "production"
    || !string.Equals(Environment.GetEnvironmentVariable("PATH"), Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase))
{
    return Breach("the time zone, the production flag or the search path is not the one the starter sets");
}

var start = Console.In.ReadLine();
if (start != "start")
{
    Console.Error.WriteLine("refused: no start line was read");
    return 3;
}

using var request = JsonDocument.Parse(File.ReadAllBytes(args[2]));
var root = request.RootElement;
var anchor = root.GetProperty("anchor").GetString()!;
var installed = Path.GetDirectoryName(anchor)!;
var behaviourFile = Path.Combine(installed, "node_modules", "standin-behaviour.txt");
var behaviour = File.Exists(behaviourFile) ? File.ReadAllText(behaviourFile).Trim() : "deterministic";

switch (behaviour)
{
    case "fail":
        Console.Error.WriteLine("renderer failed: the stand-in was told to fail");
        return 4;
    case "slow":
        Thread.Sleep(TimeSpan.FromMinutes(10));
        break;
    case "grandchild":
        Sleeper();
        Thread.Sleep(TimeSpan.FromMinutes(10));
        break;
    case "orphan":
        Sleeper();
        break;
}

if (args[1] == "bundle")
{
    var entry = Path.GetFullPath(root.GetProperty("entry").GetString()!);
    var publicDir = Path.GetFullPath(root.GetProperty("publicDir").GetString()!);
    var outDir = Path.GetFullPath(root.GetProperty("outDir").GetString()!);
    Directory.CreateDirectory(Path.Combine(outDir, "public"));
    File.WriteAllBytes(Path.Combine(outDir, "bundle.js"), SHA256.HashData(File.ReadAllBytes(entry)));
    File.WriteAllText(Path.Combine(outDir, "index.html"), "fixture bundle");
    foreach (var file in Directory.GetFiles(publicDir))
    {
        File.Copy(file, Path.Combine(outDir, "public", Path.GetFileName(file)));
    }

    var modules = new List<string> { entry, Path.Combine(root.GetProperty("modules").GetString()!, "remotion", "index.js") };
    if (behaviour == "excluded")
    {
        modules.Add(Path.Combine(root.GetProperty("modules").GetString()!, "@remotion", "elevenlabs", "dist", "index.js"));
    }

    if (behaviour == "cache")
    {
        // As a bundler with caching on would: a file left under the installed folder.
        Directory.CreateDirectory(Path.Combine(installed, "node_modules", ".cache"));
        File.WriteAllText(Path.Combine(installed, "node_modules", ".cache", "left-behind"), "a cache the starter must never allow");
    }

    File.WriteAllText(root.GetProperty("moduleList").GetString()!, JsonSerializer.Serialize(modules));
    return 0;
}

// Render: exactly the frames requested, each a truecolour PNG of the profile's size whose colour follows the motion progress.
var specificationBytes = File.ReadAllBytes(root.GetProperty("specification").GetString()!);
using var style = JsonDocument.Parse(File.ReadAllBytes(root.GetProperty("style").GetString()!));
var timing = root.GetProperty("timing");
var frames = timing.GetProperty("frames").GetInt64();
var width = timing.GetProperty("width").GetInt32();
var height = timing.GetProperty("height").GetInt32();
var still = timing.GetProperty("kind").GetString() == "still";
var motionEnd = style.RootElement.GetProperty("motionEnd").GetDouble();
var output = Path.GetFullPath(root.GetProperty("output").GetString()!);
Directory.CreateDirectory(output);
var seed = SHA256.HashData(specificationBytes);
var images = new Dictionary<(byte, byte, byte), byte[]>();
var window = (long)Math.Floor(frames * motionEnd);
for (long k = 0; k < frames; k++)
{
    var progress = still || window <= 1 ? 1.0 : Math.Min(1.0, k / (double)(window - 1));
    var red = (byte)(seed[0] ^ (byte)Math.Round(progress * 200));
    var green = seed[1];
    var blue = behaviour == "vary" ? (byte)RandomNumberGenerator.GetInt32(256) : seed[2];
    File.WriteAllBytes(Path.Combine(output, string.Create(CultureInfo.InvariantCulture, $"element-{k}.png")), Png(images, width, height, red, green, blue));
}

return 0;

static void Sleeper()
{
    var child = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
    child.ArgumentList.Add("--sleeper");
    Process.Start(child);
}

static int Breach(string why)
{
    Console.Error.WriteLine($"stand-in refused its contract: {why}");
    return 9;
}

// A truecolour image of one colour, as the renderer's frames are (three channels, eight bits each): its compressed data is
// computed once per colour, since a frame's colour changes only while its motion runs.
static byte[] Png(Dictionary<(byte, byte, byte), byte[]> images, int width, int height, byte red, byte green, byte blue)
{
    if (!images.TryGetValue((red, green, blue), out var image))
    {
        var row = new byte[1 + (width * 3)];
        for (var x = 0; x < width; x++)
        {
            row[1 + (x * 3)] = red;
            row[2 + (x * 3)] = green;
            row[3 + (x * 3)] = blue;
        }

        using var buffer = new MemoryStream();
        using (var zlib = new ZLibStream(buffer, CompressionLevel.Fastest, leaveOpen: true))
        {
            for (var y = 0; y < height; y++)
            {
                zlib.Write(row);
            }
        }

        image = buffer.ToArray();
        images[(red, green, blue)] = image;
    }

    using var png = new MemoryStream();
    png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
    var header = new byte[13];
    BinaryPrimitives.WriteInt32BigEndian(header, width);
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
    header[8] = 8;
    header[9] = 2;
    Chunk(png, "IHDR", header);
    Chunk(png, "IDAT", image);
    Chunk(png, "IEND", []);
    return png.ToArray();
}

static void Chunk(Stream png, string type, byte[] data)
{
    var length = new byte[4];
    BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
    png.Write(length);
    var typed = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
    png.Write(typed);
    var crc = new byte[4];
    BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typed));
    png.Write(crc);
}

static uint Crc32(byte[] bytes)
{
    var crc = 0xFFFFFFFFu;
    foreach (var b in bytes)
    {
        crc ^= b;
        for (var i = 0; i < 8; i++)
        {
            crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
    }

    return crc ^ 0xFFFFFFFFu;
}
