using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using MediaCompany.Credentials;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Capability.Providers;

/// <summary>
/// The fake speech path (the production change, decision D-004 of its design). DETERMINISTIC, at zero cost,
/// reaching nothing: it returns a mono 16-bit PCM wave whose sample count is a fixed function of the text's length
/// (960 samples, 60 milliseconds at 16,000 samples a second, per character) and whose samples are a fixed integer
/// function of the characters, so one text yields one byte sequence. It reports explicit zero units MEASURED, so
/// its operation costs zero and is stated; every reader labels it demonstration by the store's designation.
/// Constructible only by the demonstration factory member, which holds no network client.
/// </summary>
internal sealed class FakeSpeechAudio(ProviderAccountId account) : IProviderAdapter
{
    internal const int SampleRate = 16_000;
    internal const int SamplesPerCharacter = 960;

    public ProviderAccountId ProviderAccount { get; } = account;

    public bool Serves(CapabilityClass capability) => capability == CapabilityClass.Narration;

    public Task<ProviderAttempt> InvokeAsync(
        RouteTarget.ProviderRoute route, CapabilityRequest request, ScopedHandle handle, CancellationToken cancellationToken)
    {
        if (request.Payload is not CapabilityPayload.Narration narration)
        {
            return Task.FromResult(VendorCalls.WrongPayload("fake speech"));
        }

        return Task.FromResult(new ProviderAttempt(true, UnitCounts.None, CostBasis.Measurement, TimeSpan.Zero, null, null)
        {
            Content = new ProducedContent(ContentKind.Audio, "audio/wav", Wave(narration.Text)),
        });
    }

    /// <summary>The wave for a text: a header, then one integer sawtooth block per character, silence for white space.</summary>
    internal static byte[] Wave(string text)
    {
        var samples = (long)text.Length * SamplesPerCharacter;
        var dataBytes = checked((int)(samples * 2));
        var wave = new byte[44 + dataBytes];
        var span = wave.AsSpan();

        Encoding.ASCII.GetBytes("RIFF").CopyTo(span);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], 36 + dataBytes);
        Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(span[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[16..], 16);
        BinaryPrimitives.WriteInt16LittleEndian(span[20..], 1);
        BinaryPrimitives.WriteInt16LittleEndian(span[22..], 1);
        BinaryPrimitives.WriteInt32LittleEndian(span[24..], SampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..], SampleRate * 2);
        BinaryPrimitives.WriteInt16LittleEndian(span[32..], 2);
        BinaryPrimitives.WriteInt16LittleEndian(span[34..], 16);
        Encoding.ASCII.GetBytes("data").CopyTo(span[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[40..], dataBytes);

        var at = 44;
        foreach (var c in text)
        {
            var period = 40 + (c % 64);
            for (var i = 0; i < SamplesPerCharacter; i++)
            {
                var value = char.IsWhiteSpace(c) ? 0 : ((i % period) * 3000 / period) - 1500;
                BinaryPrimitives.WriteInt16LittleEndian(span[at..], (short)value);
                at += 2;
            }
        }

        return wave;
    }
}

/// <summary>
/// The fake image path (the production change, decision D-004 of its design): a fixed-size PNG drawn from a fixed
/// palette as a pure function of the prompt, at zero cost, reaching nothing. Item 001's run never calls it.
/// </summary>
internal sealed class FakeImageGeneration(ProviderAccountId account) : IProviderAdapter
{
    internal const int Side = 64;

    private static readonly byte[][] Palette =
    [
        [0x1F, 0x2A, 0x44], [0x3C, 0x6E, 0x71], [0xD9, 0xD9, 0xD9], [0x28, 0x4B, 0x63], [0xF4, 0xF4, 0xF9],
    ];

    public ProviderAccountId ProviderAccount { get; } = account;

    public bool Serves(CapabilityClass capability) => capability == CapabilityClass.StillImages;

    public Task<ProviderAttempt> InvokeAsync(
        RouteTarget.ProviderRoute route, CapabilityRequest request, ScopedHandle handle, CancellationToken cancellationToken)
    {
        if (request.Payload is not CapabilityPayload.StillImage image)
        {
            return Task.FromResult(VendorCalls.WrongPayload("fake image"));
        }

        return Task.FromResult(new ProviderAttempt(true, UnitCounts.None, CostBasis.Measurement, TimeSpan.Zero, null, null)
        {
            Content = new ProducedContent(ContentKind.Image, "image/png", Png(image.Prompt)),
        });
    }

    internal static byte[] Png(string prompt)
    {
        var seed = 0;
        foreach (var c in prompt)
        {
            seed = unchecked((seed * 31) + c);
        }

        var raw = new byte[Side * ((Side * 3) + 1)];
        for (var y = 0; y < Side; y++)
        {
            var row = y * ((Side * 3) + 1);
            raw[row] = 0;
            for (var x = 0; x < Side; x++)
            {
                var colour = Palette[(int)((uint)(seed + (x / 16) + (y / 16)) % (uint)Palette.Length)];
                colour.CopyTo(raw, row + 1 + (x * 3));
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, Side);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), Side);
        header[8] = 8;
        header[9] = 2;
        Chunk(png, "IHDR", header);
        Chunk(png, "IDAT", compressed.ToArray());
        Chunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void Chunk(Stream png, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        png.Write(length);
        var typed = new byte[4 + data.Length];
        Encoding.ASCII.GetBytes(type).CopyTo(typed, 0);
        data.CopyTo(typed, 4);
        png.Write(typed);
        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typed));
        png.Write(crc);
    }

    private static uint Crc32(byte[] bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return crc ^ 0xFFFFFFFFu;
    }
}

/// <summary>
/// The fake messages path (the production change, decision D-004 of its design): a fixed text naming the prompt's
/// length, at zero cost, reaching nothing. Item 001's run and every report never call it.
/// </summary>
internal sealed class FakeMessages(ProviderAccountId account) : IProviderAdapter
{
    public ProviderAccountId ProviderAccount { get; } = account;

    public bool Serves(CapabilityClass capability) =>
        capability is CapabilityClass.EditorialReasoning or CapabilityClass.HighStakesReview or CapabilityClass.BulkClassification;

    public Task<ProviderAttempt> InvokeAsync(
        RouteTarget.ProviderRoute route, CapabilityRequest request, ScopedHandle handle, CancellationToken cancellationToken)
    {
        if (request.Payload is not CapabilityPayload.Messages messages)
        {
            return Task.FromResult(VendorCalls.WrongPayload("fake messages"));
        }

        var text = $"demonstration reply to a prompt of {messages.Prompt.Length} characters; no model was called";
        return Task.FromResult(new ProviderAttempt(true, UnitCounts.None, CostBasis.Measurement, TimeSpan.Zero, null, null)
        {
            Content = new ProducedContent(ContentKind.Text, "text/plain; charset=utf-8", VendorCalls.Utf8(text)),
        });
    }
}
