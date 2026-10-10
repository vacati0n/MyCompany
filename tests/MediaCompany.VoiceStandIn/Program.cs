using System.Buffers.Binary;
using System.Globalization;
using System.Text;

// The stand-in for the in-house model's interpreter. Expected arguments, exactly in this order:
//   -I -B -m <module> -m <model> -c <config> -i <text file> -f <output file>
//   --length-scale <x> --noise-scale <x> --noise-w-scale <x> --sentence-silence <x> --volume <x> [--no-normalize]
// Any other shape exits 9 naming it, so a test sees a voice name, a data directory, a speaker or a device flag refused.

const int SampleRate = 22_050;

var expected = new[] { "-I", "-B", "-m" };
if (args.Length < 4 || !args.Take(3).SequenceEqual(expected))
{
    return Refuse("the interpreter options are not -I -B -m <module>");
}

var named = new Dictionary<string, string>(StringComparer.Ordinal);
var normalise = true;
var order = new List<string>();
for (var i = 4; i < args.Length; i++)
{
    var flag = args[i];
    if (flag == "--no-normalize")
    {
        normalise = false;
        order.Add(flag);
        continue;
    }

    if (flag is not ("-m" or "-c" or "-i" or "-f" or "--length-scale" or "--noise-scale" or "--noise-w-scale" or "--sentence-silence" or "--volume"))
    {
        return Refuse($"the argument {flag} is not one the runtime is given");
    }

    if (i + 1 >= args.Length || named.ContainsKey(flag))
    {
        return Refuse($"the argument {flag} has no value or is repeated");
    }

    named[flag] = args[++i];
    order.Add(flag);
}

var required = new[] { "-m", "-c", "-i", "-f", "--length-scale", "--noise-scale", "--noise-w-scale", "--sentence-silence", "--volume" };
if (!order.Where(o => o != "--no-normalize").SequenceEqual(required))
{
    return Refuse($"the arguments are not in the runtime's order: {string.Join(" ", order)}");
}

if (!Path.IsPathRooted(named["-m"]) || !Path.IsPathRooted(named["-c"]))
{
    return Refuse("the model and its configuration are not given as full paths");
}

if (named["-i"].IndexOfAny(['/', '\\', ':']) >= 0 || named["-f"].IndexOfAny(['/', '\\', ':']) >= 0)
{
    return Refuse("the text and output files are not plain names in the working directory");
}

// Read the model and its configuration in full, as the runtime loads them, while the starter's process holds them read-only.
var model = File.ReadAllBytes(named["-m"]);
_ = File.ReadAllBytes(named["-c"]);
var behaviour = Encoding.UTF8.GetString(model).Split('\n')[0].Trim();
var text = File.ReadAllText(named["-i"], Encoding.UTF8);
var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
var lengthScale = double.Parse(named["--length-scale"], CultureInfo.InvariantCulture);

switch (behaviour)
{
    case "fail":
        Console.Error.WriteLine("stand-in failure: the model could not run (Authorization: Bearer sk-standin0123456789abcdefghij)");
        return 1;
    case "slow":
        Thread.Sleep(TimeSpan.FromMinutes(10));
        break;
}

// A tone whose length follows the words: 0.3 s a word at length scale 1, so a part's duration is known to the test.
var samples = (int)Math.Round(words * 0.3 * lengthScale * SampleRate);
if (behaviour == "random")
{
    samples += Random.Shared.Next(1, SampleRate / 10);
}

var data = new byte[samples * 2];
for (var n = 0; n < samples; n++)
{
    var value = Math.Sin(2 * Math.PI * 220 * n / SampleRate) * 8_000;
    if (behaviour == "random")
    {
        value += Random.Shared.Next(-200, 200);
    }

    BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(n * 2), (short)value);
}

using (var output = File.Create(named["-f"]))
{
    var header = new byte[44];
    Encoding.ASCII.GetBytes("RIFF").CopyTo(header, 0);
    BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), 36 + data.Length);
    Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(header, 8);
    BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(16), 16);
    BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(20), 1);
    BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(22), 1);
    BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(24), SampleRate);
    BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(28), SampleRate * 2);
    BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(32), 2);
    BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(34), 16);
    Encoding.ASCII.GetBytes("data").CopyTo(header, 36);
    BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(40), data.Length);
    output.Write(header);
    output.Write(data);
}

_ = normalise;
return 0;

static int Refuse(string why)
{
    Console.Error.WriteLine($"stand-in refused its arguments: {why}");
    return 9;
}
