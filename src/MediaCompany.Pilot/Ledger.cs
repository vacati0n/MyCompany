using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaCompany.Pilot;

/// <summary>One ledger entry (design D-003): one produced package, chained to the entry before it by its hash.</summary>
public sealed record LedgerEntry
{
    public required int Sequence { get; init; }
    public required string VideoCode { get; init; }
    public required string PagePath { get; init; }
    public required int ProductVideoNumber { get; init; }
    public required string Focus { get; init; }
    public required string RecordingSha256 { get; init; }
    public required string VideoSha256 { get; init; }
    public required string PackageFolder { get; init; }
    public required string RecordedAt { get; init; }
    public required string PreviousHash { get; init; }
    public required string EntryHash { get; init; }

    /// <summary>The entry's hash over every field but its own, each on its own line in a fixed order.</summary>
    public string ComputeHash() => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",
        Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture), VideoCode, PagePath,
        ProductVideoNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), Focus, RecordingSha256, VideoSha256, PackageFolder,
        RecordedAt, PreviousHash))));
}

/// <summary>
/// THE PILOT LEDGER (design D-003). A hash-chained JSON file in the configured pilot folder, outside the repository. The chain is
/// verified and reconciled with the previous generation and the packages' entry copies before every read; the previous generation is kept beside it; a write goes to a temporary file that then replaces the
/// ledger. A re-run of the same video code supersedes its earlier entry, so the current view holds the last entry per code. The
/// held limits it enforces: 12 videos in the pilot; 2 videos per product page; a different focus for the second video of a
/// product; a recording digest used by no other video.
/// </summary>
public sealed class Ledger
{
    public const string FileName = "pilot-ledger.json";
    public const string PreviousFileName = "pilot-ledger.previous.json";
    public static readonly string Genesis = new('0', 64);

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _folder;
    private readonly HeldData _held;
    private readonly List<LedgerEntry> _entries;

    private Ledger(string folder, HeldData held, List<LedgerEntry> entries)
    {
        _folder = folder;
        _held = held;
        _entries = entries;
    }

    public IReadOnlyList<LedgerEntry> Entries => _entries;

    public string FilePath => Path.Combine(_folder, FileName);

    /// <summary>The last entry of each video code, in the order the codes first appear.</summary>
    public IReadOnlyList<LedgerEntry> Current =>
        _entries.GroupBy(e => e.VideoCode, StringComparer.Ordinal).Select(g => g.Last()).ToList();

    /// <summary>
    /// Opens the ledger of the pilot folder, creating the folder when it is new, and reconciles it before anything is judged
    /// (correction request 7). The read is refused, naming the cause, in each of these cases: the ledger or its previous
    /// generation unreadable; a broken chain in either; the ledger missing while its previous generation exists (deleted); the
    /// chain shorter than the previous generation, or not starting with it (truncated or replaced); a package folder under
    /// runs holding a ledger-entry copy the chain lacks, or one that cannot be read.
    /// </summary>
    public static Ledger Open(string pilotFolder, HeldData held)
    {
        ArgumentNullException.ThrowIfNull(held);
        Directory.CreateDirectory(pilotFolder);
        var path = Path.Combine(pilotFolder, FileName);
        var previousPath = Path.Combine(pilotFolder, PreviousFileName);
        var entries = File.Exists(path) ? ReadChain(path) : [];
        Verify(entries, path);
        if (File.Exists(previousPath))
        {
            var previous = ReadChain(previousPath);
            Verify(previous, previousPath);
            if (!File.Exists(path))
            {
                throw new PilotRefusal("ledger-truncated",
                    $"Không thấy sổ theo dõi thí điểm {path}, nhưng bản trước {PreviousFileName} có {previous.Count} mục: sổ đã bị xoá. Không chạy tiếp; khôi phục sổ hoặc báo người phụ trách.");
            }

            var prefix = previous.Count <= entries.Count
                && previous.Select(e => e.EntryHash).SequenceEqual(entries.Take(previous.Count).Select(e => e.EntryHash), StringComparer.Ordinal);
            if (!prefix)
            {
                throw new PilotRefusal("ledger-truncated",
                    $"Sổ theo dõi thí điểm {path} có {entries.Count} mục, bản trước {PreviousFileName} có {previous.Count} mục và sổ không còn bắt đầu bằng các mục đó: sổ đã bị cắt bớt hoặc bị thay. Không chạy tiếp; khôi phục sổ hoặc báo người phụ trách.");
            }
        }

        var known = entries.Select(e => e.EntryHash).ToHashSet(StringComparer.Ordinal);
        var runs = Path.Combine(pilotFolder, "runs");
        var copies = Directory.Exists(runs)
            ? Directory.EnumerateDirectories(runs).Select(d => Path.Combine(d, PackageFiles.LedgerEntry)).Where(File.Exists).Order(StringComparer.Ordinal).ToList()
            : [];
        foreach (var copy in copies)
        {
            LedgerEntry? entry;
            try
            {
                entry = JsonSerializer.Deserialize<LedgerEntry>(File.ReadAllBytes(copy), Json);
            }
            catch (JsonException)
            {
                entry = null;
            }

            if (entry is null || !known.Contains(entry.EntryHash))
            {
                throw new PilotRefusal("ledger-entry-missing",
                    $"Gói {Path.GetDirectoryName(copy)} có bản sao mục sổ{(entry is null ? " không đọc được" : $" (video {entry.VideoCode}, mục số {entry.Sequence})")} mà sổ theo dõi {path} không có: sổ đã mất mục. Không chạy tiếp; khôi phục sổ hoặc báo người phụ trách.");
            }
        }

        return new Ledger(pilotFolder, held, entries);
    }

    private static List<LedgerEntry> ReadChain(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<LedgerFile>(File.ReadAllBytes(path), Json)?.Entries.ToList()
                ?? throw new PilotRefusal("ledger-chain-broken", $"Sổ theo dõi thí điểm {path} trống hoặc hỏng.");
        }
        catch (JsonException error)
        {
            throw new PilotRefusal("ledger-chain-broken", $"Sổ theo dõi thí điểm {path} không đọc được: {error.Message}. Bản trước còn ở {PreviousFileName}.");
        }
    }

    /// <summary>Refuses a chain whose first link is not the genesis, a link that does not name its predecessor, or a changed entry.</summary>
    public static void Verify(IReadOnlyList<LedgerEntry> entries, string where)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var previous = Genesis;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.Sequence != i + 1 || !string.Equals(entry.PreviousHash, previous, StringComparison.Ordinal)
                || !string.Equals(entry.EntryHash, entry.ComputeHash(), StringComparison.Ordinal))
            {
                throw new PilotRefusal("ledger-chain-broken",
                    $"Sổ theo dõi thí điểm {where} bị sửa hoặc đứt chuỗi ở mục số {i + 1} (mã video {entry.VideoCode}). Không chạy tiếp; khôi phục từ {PreviousFileName} hoặc báo người phụ trách.");
            }

            previous = entry.EntryHash;
        }
    }

    /// <summary>
    /// Refuses a recording digest recorded in any entry of another video code, superseded entries included (decision record entry
    /// 1305: every video needs a fresh recording).
    /// </summary>
    public void RefuseReusedRecording(string recordingSha256, string videoCode)
    {
        var other = _entries.FirstOrDefault(e => !string.Equals(e.VideoCode, videoCode, StringComparison.Ordinal)
            && string.Equals(e.RecordingSha256, recordingSha256, StringComparison.OrdinalIgnoreCase));
        if (other is not null)
        {
            throw new PilotRefusal("recording-reused",
                $"Bản ghi này (SHA-256 {recordingSha256}) đã dùng cho video {other.VideoCode}. Mỗi video cần một bản ghi mới.");
        }
    }

    /// <summary>Refuses a video the held limits do not allow; a re-run of a code already in the ledger is not counted twice.</summary>
    public void RefuseOverLimits(FactSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var others = Current.Where(e => !string.Equals(e.VideoCode, sheet.VideoCode, StringComparison.Ordinal)).ToList();
        if (others.Count >= _held.PilotVideoLimit)
        {
            throw new PilotRefusal("pilot-limit-reached",
                $"Sổ theo dõi đã có {others.Count} video; thí điểm cho tối đa {_held.PilotVideoLimit} video. Không tạo thêm.");
        }

        var sameProduct = others.Where(e => string.Equals(e.PagePath, sheet.PagePath, StringComparison.OrdinalIgnoreCase)).ToList();
        if (sameProduct.Count >= _held.VideosPerProductLimit)
        {
            throw new PilotRefusal("product-limit-reached",
                $"Sản phẩm {sheet.PagePath} đã có {sameProduct.Count} video ({string.Join(", ", sameProduct.Select(e => e.VideoCode))}); tối đa {_held.VideosPerProductLimit}.");
        }

        var focus = VietnameseText.Nfc(sheet.VideoFocus).ToLowerInvariant();
        var repeated = sameProduct.FirstOrDefault(e => string.Equals(VietnameseText.Nfc(e.Focus).ToLowerInvariant(), focus, StringComparison.Ordinal));
        if (repeated is not null)
        {
            throw new PilotRefusal("focus-repeated",
                $"Video {repeated.VideoCode} của cùng sản phẩm đã có trọng tâm \"{repeated.Focus}\"; video thứ hai cần trọng tâm khác.");
        }
    }

    /// <summary>Appends an entry and writes the ledger: the previous generation kept, then an atomic replace.</summary>
    public LedgerEntry Append(FactSheet sheet, string recordingSha256, string videoSha256, string packageFolder, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var draft = new LedgerEntry
        {
            Sequence = _entries.Count + 1,
            VideoCode = sheet.VideoCode,
            PagePath = sheet.PagePath,
            ProductVideoNumber = sheet.ProductVideoNumber,
            Focus = VietnameseText.Nfc(sheet.VideoFocus),
            RecordingSha256 = recordingSha256,
            VideoSha256 = videoSha256,
            PackageFolder = packageFolder,
            RecordedAt = at.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
            PreviousHash = _entries.Count == 0 ? Genesis : _entries[^1].EntryHash,
            EntryHash = string.Empty,
        };
        var entry = draft with { EntryHash = draft.ComputeHash() };
        var next = new List<LedgerEntry>(_entries) { entry };
        Verify(next, FilePath);
        var temporary = FilePath + ".tmp";
        File.WriteAllBytes(temporary, Serialize(next));
        if (File.Exists(FilePath))
        {
            File.Replace(temporary, FilePath, Path.Combine(_folder, PreviousFileName));
        }
        else
        {
            File.Move(temporary, FilePath);
        }

        _entries.Add(entry);
        return entry;
    }

    public static byte[] SerializeEntry(LedgerEntry entry) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry, Json).ReplaceLineEndings("\n") + "\n");

    private static byte[] Serialize(IReadOnlyList<LedgerEntry> entries) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new LedgerFile(1, entries), Json).ReplaceLineEndings("\n") + "\n");

    private sealed record LedgerFile(int FormVersion, IReadOnlyList<LedgerEntry> Entries);
}
