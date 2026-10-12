using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediaCompany.Domain.Production;

namespace MediaCompany.Deterministic.Production;

/// <summary>One drawn text: the element, its characters, the face it is set in and its role.</summary>
public sealed record DrawnText(string Element, string Text, bool Italic, string Role);

/// <summary>A figure the check admitted: where it is drawn, its text, the figure tokens in it and what admitted it.</summary>
public sealed record AdmittedFigure(string Position, string Element, string Text, string Role, IReadOnlyList<string> Tokens, string AdmittedBy);

/// <summary>
/// One specification as checked: its file name, the very bytes read once and their SHA-256, and what the produce path needs
/// from it. The drawing program reads the staged copy of these bytes; nothing here is re-serialised.
/// </summary>
public sealed record CheckedSpecification(
    string Position,
    int Beat,
    string FileName,
    byte[] Bytes,
    string Sha256,
    GraphicKind Kind,
    IReadOnlyList<DrawnText> Texts);

/// <summary>
/// THE ITEM'S RECORDED SOURCES, the closed set a specification may quote (decision D-001 of the graphics design; the review's
/// correction CR-001): the claim record, the shot list, the narration and the owner's decision record. They are located from the
/// item package (its material and its folder), never from a path a specification supplies; any other file is refused.
/// </summary>
public sealed record GraphicSources(string ClaimRecord, string ShotList, string Narration, string OwnerRecord)
{
    /// <summary>The claim record's file name in the item package's folder, as the Research stage records it.</summary>
    public const string ClaimRecordName = "claim-to-source.md";

    /// <summary>The four paths, repository-relative.</summary>
    public IReadOnlyList<string> All => [ClaimRecord, ShotList, Narration, OwnerRecord];

    /// <summary>
    /// The recorded sources of an item: the claim record beside the material in <paramref name="itemFolder"/> (the material's
    /// repository-relative folder), the shot list every graphic row of the material quotes, the narration file the material names,
    /// and the owner's decision record the material quotes for its clip placeholders. Refused when the material's graphic rows do
    /// not all quote one shot list.
    /// </summary>
    public static GraphicSources Of(ItemMaterial material, string itemFolder)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(itemFolder);
        var shotLists = material.Graphics.Select(g => g.Source).Distinct(StringComparer.Ordinal).ToArray();
        if (shotLists.Length != 1)
        {
            throw new InvalidOperationException($"the material's graphic rows quote {shotLists.Length} shot lists, and one is recorded");
        }

        var folder = itemFolder.Replace('\\', '/').TrimEnd('/');
        return new GraphicSources($"{folder}/{ClaimRecordName}", shotLists[0], material.NarrationFile, material.ClipPlaceholders.Source);
    }
}

/// <summary>What the check found: every refusal, the specifications that passed, and the item's admitted figure set.</summary>
public sealed record GraphicCheckResult(IReadOnlyList<string> Findings, IReadOnlyList<CheckedSpecification> Specifications, IReadOnlyList<AdmittedFigure> Figures)
{
    public bool Passed => Findings.Count == 0;
}

/// <summary>
/// THE GRAPHIC SPECIFICATION CHECK (the graphics change, decision D-001 of its design), a pure function over the
/// specifications' bytes, the item material and the recorded sources' text. It holds no subject term and no position
/// identifier: every expected name is derived from the material, and every quotation is matched against the item's four recorded
/// sources (<see cref="GraphicSources"/>), the closed set located from the item package; a specification naming any other file,
/// or naming its own claim or owner record, is refused.
///
/// Matching: a source and a quotation are compared after removing the emphasis characters asterisk and backtick and
/// collapsing every run of white space to one space (the recorded decision record wraps its lines); a drawn text must equal
/// its quotation character for character, and the quotation must sit in its source ON TOKEN BOUNDARIES: a quotation whose edge
/// is a letter or a number does not continue a longer word or figure of the source (a figure "5 Hz" is not cut out of
/// "13–15 Hz", nor "2 km" out of "~2 km"). THE NUMBER RULE: a figure is a text holding a number character or a word of the closed
/// list below; any number character other than the ASCII digits (a fraction, a superscript, a digit of another script, a
/// Roman numeral) is refused outright; a figure is admitted only from the claim record, or by a recorded owner answer whose
/// quotation the element cites and which contains the figure's text on token boundaries. A quantity drawn by geometry (a
/// bearing, an angle, a trace, bars or a region) is allowed only in a specification carrying the owner's disclosure, or where
/// the element cites a recorded quotation that fixes it.
/// </summary>
public static partial class GraphicSpecificationCheck
{
    public const string Format = "graphic-specification/1";

    /// <summary>
    /// THE CLOSED LIST OF NUMBER WORDS that make a text a figure, in one place (the review's correction 1): every English cardinal
    /// from zero to nineteen and the tens, hundred, thousand, million, billion and trillion, dozen and score, the fractions half
    /// and quarter, the multiplicatives once, twice, thrice, double and triple, each with its plural where English has one, and
    /// the further counting and time-of-day words the drawings could carry. Matched whole-word and case-insensitively.
    /// </summary>
    public static readonly IReadOnlyList<string> NumberWords =
    [
        "zero", "zeros", "zeroes", "one", "ones", "two", "twos", "three", "threes", "four", "fours", "five", "fives", "six", "sixes",
        "seven", "sevens", "eight", "eights", "nine", "nines", "ten", "tens", "eleven", "elevens", "twelve", "twelves",
        "thirteen", "thirteens", "fourteen", "fourteens", "fifteen", "fifteens", "sixteen", "sixteens", "seventeen", "seventeens",
        "eighteen", "eighteens", "nineteen", "nineteens",
        "twenty", "twenties", "thirty", "thirties", "forty", "forties", "fifty", "fifties", "sixty", "sixties", "seventy",
        "seventies", "eighty", "eighties", "ninety", "nineties",
        "hundred", "hundreds", "thousand", "thousands", "million", "millions", "billion", "billions", "trillion", "trillions",
        "dozen", "dozens", "score", "scores", "half", "halves", "quarter", "quarters",
        "once", "twice", "thrice", "double", "doubles", "triple", "triples",
        "doubled", "tripled", "treble", "quadruple", "halve", "halved", "percent", "twofold", "threefold", "fourfold", "fivefold",
        "tenfold", "hundredfold", "thousandfold", "o'clock", "noon", "midnight",
    ];

    private static readonly string[] TopFields =
        ["format", "position", "beat", "row", "claims", "records", "kind", "kindSource", "disclosure", "alsoCarries", "shared", "elements", "motion"];

    private static readonly string[] OptionalTop = ["disclosure", "alsoCarries", "shared", "motion"];

    /// <summary>Per element type: its required fields and its optional fields, beyond id and type.</summary>
    private static readonly Dictionary<string, (string[] Required, string[] Optional)> ElementFields = new(StringComparer.Ordinal)
    {
        ["text"] = (["role", "text", "source", "quote", "x", "y", "anchor", "size"], ["face", "focus", "admittedBy"]),
        ["point"] = (["x", "y", "r", "filled"], ["focus"]),
        ["bearing"] = (["x", "y", "angle", "length", "arrow", "dashed"], ["focus", "fixedBy", "weight"]),
        ["path"] = (["form", "points", "arrow", "dashed"], ["focus", "weight"]),
        ["axis"] = (["x1", "y1", "x2", "y2"], ["focus"]),
        ["trace"] = (["form", "x", "y", "width", "height"], ["focus", "fixedBy", "cycles", "spread"]),
        ["angle"] = (["x", "y", "from", "to", "radius"], ["focus", "fixedBy"]),
        ["region"] = (["form", "x", "y", "a", "b", "angle", "shading"], ["focus", "fixedBy"]),
        ["panel"] = (["x", "y", "w", "h"], ["focus"]),
        ["blocks"] = (["x", "y", "boxWidth", "boxHeight", "gap", "count"], ["focus"]),
        ["bars"] = (["x", "y", "width", "height", "extents"], ["focus", "fixedBy"]),
    };

    /// <summary>The element types whose geometry can depict a quantity.</summary>
    private static readonly HashSet<string> QuantityTypes = new(StringComparer.Ordinal) { "bearing", "trace", "angle", "region", "bars" };

    private static readonly Dictionary<string, string[]> MotionFields = new(StringComparer.Ordinal)
    {
        ["reveal"] = ["type", "targets", "from", "to"],
        ["rotate"] = ["type", "targets", "fromAngle", "toAngle", "from", "to"],
        ["sequence"] = ["type", "sets", "accumulate", "from", "to"],
        ["grow"] = ["type", "target", "property", "fromValue", "toValue", "from", "to"],
        ["follow"] = ["type", "target", "path", "from", "to"],
    };

    private static readonly HashSet<string> Roles = new(StringComparer.Ordinal) { "title", "label", "caption", "citation", "figure" };
    private static readonly HashSet<string> Sizes = new(StringComparer.Ordinal) { "title", "label", "caption", "small" };
    private static readonly HashSet<string> Anchors = new(StringComparer.Ordinal) { "start", "middle", "end" };
    private static readonly HashSet<string> GrowProperties = new(StringComparer.Ordinal) { "a", "b", "length", "radius", "width", "height" };

    /// <summary>
    /// Checks every specification of an item. <paramref name="specifications"/> maps each file name found in the item's
    /// graphics folder to its bytes, read once; <paramref name="recorded"/> names the item's four recorded sources and
    /// <paramref name="read"/> maps each of them to its text with line endings normalised (any other entry is ignored, so no
    /// other file can admit a quotation). Every refusal names the file and the element.
    /// </summary>
    public static GraphicCheckResult Check(ItemMaterial material, GraphicSources recorded, IReadOnlyDictionary<string, byte[]> specifications, IReadOnlyDictionary<string, string> read)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(recorded);
        ArgumentNullException.ThrowIfNull(specifications);
        ArgumentNullException.ThrowIfNull(read);

        var findings = new List<string>();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in recorded.All.Distinct(StringComparer.Ordinal))
        {
            if (read.TryGetValue(path, out var text))
            {
                sources[path] = text;
            }
            else
            {
                findings.Add($"the item's recorded source {path} was not read");
            }
        }

        var closedList = string.Join(", ", recorded.All.Distinct(StringComparer.Ordinal));
        var checkedSpecs = new List<CheckedSpecification>();
        var figures = new List<AdmittedFigure>();
        var shared = new Dictionary<string, (decimal Value, string File)>(StringComparer.Ordinal);

        var expected = material.Graphics.ToDictionary(g => FileName(g.Id), g => g, StringComparer.Ordinal);
        foreach (var missing in expected.Keys.Where(k => !specifications.ContainsKey(k)).Order(StringComparer.Ordinal))
        {
            findings.Add($"{missing}: missing; the material names {expected[missing].Id} and no specification for it is recorded");
        }

        foreach (var extra in specifications.Keys.Where(k => !expected.ContainsKey(k)).Order(StringComparer.Ordinal))
        {
            findings.Add($"{extra}: extra; the material names no graphic position this file is for");
        }

        var positionsSeen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (file, bytes) in specifications.Where(s => expected.ContainsKey(s.Key)).OrderBy(s => s.Key, StringComparer.Ordinal))
        {
            var graphic = expected[file];
            var sha = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var before = findings.Count;
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(bytes);
            }
            catch (JsonException invalid)
            {
                findings.Add($"{file}: not a JSON document ({invalid.Message})");
                continue;
            }

            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    findings.Add($"{file}: not a JSON object");
                    continue;
                }

                Fields(root, TopFields, OptionalTop, file, findings);
                if (Str(root, "format") != Format)
                {
                    findings.Add($"{file}: its format is not {Format}");
                }

                var position = Str(root, "position");
                if (position != graphic.Id)
                {
                    findings.Add($"{file}: its position {position ?? "(none)"} is not the material's {graphic.Id}");
                }
                else if (!positionsSeen.TryAdd(position, file))
                {
                    findings.Add($"{file}: duplicate; {positionsSeen[position]} already specifies {position}");
                }

                var beat = root.TryGetProperty("beat", out var b) && b.ValueKind == JsonValueKind.Number ? b.GetInt32() : -1;
                if (beat != graphic.Beat)
                {
                    findings.Add($"{file}: its beat {beat} is not the material's beat {graphic.Beat}");
                }

                // The row, quoted from the shot list exactly as the material quotes it.
                if (!root.TryGetProperty("row", out var row) || Str(row, "source") != graphic.Source || Str(row, "quote") != graphic.Quote)
                {
                    findings.Add($"{file}: its row differs from the material's quotation of {graphic.Source}");
                }

                // The records it reads are the item's own claim record and owner's decision record, located from the item
                // package: a specification restates them and never chooses them.
                var records = root.TryGetProperty("records", out var r) ? r : default;
                var claimsNamed = records.ValueKind == JsonValueKind.Object ? Str(records, "claims") : null;
                var ownerNamed = records.ValueKind == JsonValueKind.Object ? Str(records, "owner") : null;
                if (claimsNamed != recorded.ClaimRecord || ownerNamed != recorded.OwnerRecord)
                {
                    findings.Add($"{file}: its records name {claimsNamed ?? "(none)"} and {ownerNamed ?? "(none)"}, and the item's recorded claim record and owner's decision record are {recorded.ClaimRecord} and {recorded.OwnerRecord}; a specification does not name its own records");
                    continue;
                }

                if (!sources.ContainsKey(recorded.ClaimRecord) || !sources.ContainsKey(recorded.OwnerRecord))
                {
                    continue;
                }

                var claimsPath = recorded.ClaimRecord;
                var ownerPath = recorded.OwnerRecord;
                var positions = material.Graphics.Select(g => g.Id).ToArray();
                var claimRows = ClaimRows(sources[claimsPath], positions);

                // The claims its row cites, and every claim the claim record says this graphic carries.
                var cited = claimRows.Keys.Where(k => Names(graphic.Quote, k)).ToHashSet(StringComparer.Ordinal);
                var listed = root.TryGetProperty("claims", out var c) && c.ValueKind == JsonValueKind.Array
                    ? c.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray()
                    : [];
                if (!cited.SetEquals(listed) || listed.Length != cited.Count)
                {
                    findings.Add($"{file}: its claims [{string.Join(", ", listed)}] differ from the claims its row cites [{string.Join(", ", cited.Order(StringComparer.Ordinal))}]");
                }

                var also = root.TryGetProperty("alsoCarries", out var a) && a.ValueKind == JsonValueKind.Array ? a.EnumerateArray().ToArray() : [];
                var resolved = new HashSet<string>(StringComparer.Ordinal);
                foreach (var entry in also)
                {
                    Fields(entry, ["claim", "source", "quote"], [], $"{file} alsoCarries", findings);
                    var claim = Str(entry, "claim") ?? string.Empty;
                    if (Str(entry, "source") != ownerPath || !Found(sources, ownerPath, Str(entry, "quote")))
                    {
                        findings.Add($"{file}: alsoCarries {claim}: its owner answer is not quoted verbatim from {ownerPath}");
                    }

                    resolved.Add(claim);
                }

                var carriedHere = claimRows.Where(row => row.Value.Carried.Contains(graphic.Id)).Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
                foreach (var claim in carriedHere.Where(k => !cited.Contains(k) && !resolved.Contains(k)).Order(StringComparer.Ordinal))
                {
                    findings.Add($"{file}: the claim record says {claim} is carried by {graphic.Id}, its row does not cite it, and no owner answer resolving it is cited");
                }

                foreach (var claim in resolved.Where(k => !carriedHere.Contains(k) || cited.Contains(k)).Order(StringComparer.Ordinal))
                {
                    findings.Add($"{file}: alsoCarries {claim}, which the claim record does not give to {graphic.Id} beyond its row");
                }

                // The kind, decided by the owner's motion list the specification quotes.
                var kindText = Str(root, "kind");
                var kindSource = root.TryGetProperty("kindSource", out var ks) ? ks : default;
                var kind = kindText == "motion" ? GraphicKind.Motion : GraphicKind.Still;
                if (kindText is not ("still" or "motion"))
                {
                    findings.Add($"{file}: its kind {kindText ?? "(none)"} is neither still nor motion");
                }

                if (kindSource.ValueKind != JsonValueKind.Object || Str(kindSource, "source") != ownerPath || !Found(sources, ownerPath, Str(kindSource, "quote")))
                {
                    findings.Add($"{file}: its kind does not quote the owner's motion list verbatim from {ownerPath}");
                }
                else
                {
                    var moving = positions.Where(p => Names(Str(kindSource, "quote")!, p)).ToHashSet(StringComparer.Ordinal);
                    if (moving.Contains(graphic.Id) != (kind == GraphicKind.Motion))
                    {
                        findings.Add($"{file}: its kind is {kind}, and the owner's motion list it quotes {(moving.Contains(graphic.Id) ? "names" : "does not name")} {graphic.Id}");
                    }
                }

                // The disclosure: the owner's wording, quoted.
                var disclosed = false;
                if (root.TryGetProperty("disclosure", out var disclosure))
                {
                    Fields(disclosure, ["text", "source", "quote"], [], $"{file} disclosure", findings);
                    var text = Str(disclosure, "text") ?? string.Empty;
                    var quote = Str(disclosure, "quote") ?? string.Empty;
                    if (Str(disclosure, "source") != ownerPath || !Found(sources, ownerPath, quote) || text.Length == 0
                        || !Collapse(quote).Contains("\"" + text + "\"", StringComparison.Ordinal))
                    {
                        findings.Add($"{file}: its disclosure \"{text}\" is not the owner's quoted wording in {ownerPath}");
                    }
                    else
                    {
                        disclosed = true;
                    }
                }

                // Shared layout values.
                var sharedHere = new Dictionary<string, decimal>(StringComparer.Ordinal);
                if (root.TryGetProperty("shared", out var sharedNode) && sharedNode.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in sharedNode.EnumerateObject())
                    {
                        if (property.Value.ValueKind != JsonValueKind.Number)
                        {
                            findings.Add($"{file}: its shared value {property.Name} is not a number");
                            continue;
                        }

                        var value = property.Value.GetDecimal();
                        sharedHere[property.Name] = value;
                        if (shared.TryGetValue(property.Name, out var earlier) && earlier.Value != value)
                        {
                            findings.Add($"{file}: its shared value {property.Name} = {value.ToString(CultureInfo.InvariantCulture)} differs from {earlier.Value.ToString(CultureInfo.InvariantCulture)} in {earlier.File}");
                        }

                        shared.TryAdd(property.Name, (value, file));
                    }
                }

                // The elements.
                var ids = new Dictionary<string, string>(StringComparer.Ordinal);
                var texts = new List<DrawnText>();
                var elements = root.TryGetProperty("elements", out var e) && e.ValueKind == JsonValueKind.Array ? e.EnumerateArray().ToArray() : [];
                if (elements.Length == 0)
                {
                    findings.Add($"{file}: it draws no element");
                }

                foreach (var element in elements)
                {
                    var id = Str(element, "id") ?? "(no id)";
                    var type = Str(element, "type") ?? "(no type)";
                    var where = $"{file} element {id}";
                    if (!ElementFields.TryGetValue(type, out var fields))
                    {
                        findings.Add($"{where}: unknown element type {type}");
                        continue;
                    }

                    if (!ids.TryAdd(id, type))
                    {
                        findings.Add($"{where}: duplicate element id");
                    }

                    Fields(element, ["id", "type", .. fields.Required], fields.Optional, where, findings);
                    Numbers(element, sharedHere, where, findings);
                    Addresses(element, where, findings);

                    if (element.TryGetProperty("fixedBy", out var fixing) && Str(fixing, "source") is { } fixingSource && !sources.ContainsKey(fixingSource))
                    {
                        findings.Add($"{where}: its fixing quotation's source {fixingSource} is not one of the item's recorded sources ({closedList})");
                    }

                    if (QuantityTypes.Contains(type) && !disclosed
                        && (!element.TryGetProperty("fixedBy", out var fixedBy) || !Found(sources, Str(fixedBy, "source"), Str(fixedBy, "quote"))))
                    {
                        findings.Add($"{where}: a {type} draws a quantity, and the specification carries no disclosure and the element cites no recorded quotation fixing it");
                    }

                    switch (type)
                    {
                        case "text":
                            CheckText(element, where, graphic.Id, id, claimsPath, ownerPath, closedList, sources, texts, figures, findings);
                            break;
                        case "path" when Str(element, "form") is not ("polyline" or "smooth" or "loop"):
                            findings.Add($"{where}: unknown path form {Str(element, "form")}");
                            break;
                        case "trace" when Str(element, "form") is not ("oscillation" or "linear" or "band"):
                            findings.Add($"{where}: unknown trace form {Str(element, "form")}");
                            break;
                        case "trace" when Str(element, "form") == "oscillation" && !element.TryGetProperty("cycles", out _):
                            findings.Add($"{where}: missing field cycles, which an oscillation needs; the drawing program fills no default");
                            break;
                        case "trace" when Str(element, "form") == "band" && !element.TryGetProperty("spread", out _):
                            findings.Add($"{where}: missing field spread, which a band needs; the drawing program fills no default");
                            break;
                        case "region" when Str(element, "form") is not ("sector" or "spread" or "cone" or "ellipse" or "box"):
                            findings.Add($"{where}: unknown region form {Str(element, "form")}");
                            break;
                        case "region" when Str(element, "shading") is not ("shade" or "outline"):
                            findings.Add($"{where}: unknown region shading {Str(element, "shading")}");
                            break;
                    }
                }

                // The motion.
                var motions = root.TryGetProperty("motion", out var m) && m.ValueKind == JsonValueKind.Array ? m.EnumerateArray().ToArray() : [];
                if (kind == GraphicKind.Still && root.TryGetProperty("motion", out _))
                {
                    findings.Add($"{file}: a still graphic carries motion");
                }

                if (kind == GraphicKind.Motion && motions.Length == 0)
                {
                    findings.Add($"{file}: a motion graphic carries no motion");
                }

                for (var i = 0; i < motions.Length; i++)
                {
                    CheckMotion(motions[i], string.Create(CultureInfo.InvariantCulture, $"{file} motion {i + 1}"), ids, findings);
                }

                if (findings.Count == before)
                {
                    checkedSpecs.Add(new CheckedSpecification(graphic.Id, graphic.Beat, file, bytes, sha, kind, texts));
                }
            }
        }

        return new GraphicCheckResult(findings, checkedSpecs.OrderBy(s => s.Position, StringComparer.Ordinal).ToArray(), figures);
    }

    /// <summary>The specification file name of a graphic position: its identifier lower-cased, as the material records it.</summary>
    public static string FileName(string position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return position.ToLowerInvariant() + ".json";
    }

    /// <summary>The matching form of a text: emphasis characters removed and white space runs collapsed to one space.</summary>
    public static string Collapse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return WhiteSpace().Replace(text.Replace("*", string.Empty, StringComparison.Ordinal).Replace("`", string.Empty, StringComparison.Ordinal), " ");
    }

    /// <summary>
    /// The figure tokens of a text, by the one figure tokenizer that also reads every source (<see cref="Within"/>): each written
    /// figure as one token with its approximation, sign or range mark, its internal separators and its unit mark as written
    /// ("~2", "13–15", "15,000", "60°"); each run of other number characters (another script's digits, fractions, superscripts,
    /// numerals); and every listed number word.
    /// </summary>
    public static IReadOnlyList<string> FigureTokens(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var tokens = Figure().Matches(text).Select(x => x.Value).ToList();
        tokens.AddRange(OtherNumerals().Matches(text).Select(x => x.Value));
        foreach (Match word in Word().Matches(text))
        {
            if (NumberWords.Contains(word.Value.ToLowerInvariant(), StringComparer.Ordinal))
            {
                tokens.Add(word.Value);
            }
        }

        return tokens;
    }

    private static void CheckText(
        JsonElement element, string where, string position, string id, string claimsPath, string ownerPath, string closedList,
        IReadOnlyDictionary<string, string> sources, List<DrawnText> texts, List<AdmittedFigure> figures, List<string> findings)
    {
        var text = Str(element, "text") ?? string.Empty;
        var quote = Str(element, "quote") ?? string.Empty;
        var source = Str(element, "source");
        var role = Str(element, "role") ?? string.Empty;
        if (!Roles.Contains(role))
        {
            findings.Add($"{where}: unknown text role {role}");
        }

        if (!Sizes.Contains(Str(element, "size") ?? string.Empty))
        {
            findings.Add($"{where}: unknown text size {Str(element, "size")}");
        }

        if (!Anchors.Contains(Str(element, "anchor") ?? string.Empty))
        {
            findings.Add($"{where}: unknown text anchor {Str(element, "anchor")}");
        }

        var face = Str(element, "face");
        if (face is not (null or "italic"))
        {
            findings.Add($"{where}: unknown face {face}; only the regular and the italic declared faces exist");
        }

        if (text.Length == 0)
        {
            findings.Add($"{where}: draws no text");
        }

        if (!string.Equals(text, quote, StringComparison.Ordinal))
        {
            findings.Add($"{where}: its text differs from its quotation by at least one character");
        }

        if (source is null || !sources.ContainsKey(source))
        {
            findings.Add($"{where}: its source {source ?? "(none)"} is not one of the item's recorded sources ({closedList})");
        }
        else if (!Found(sources, source, quote))
        {
            findings.Add($"{where}: its quotation is not found in {source}: \"{quote}\"");
        }

        foreach (var numeral in text.Where(ch => char.IsNumber(ch) && ch is not (>= '0' and <= '9')).Distinct())
        {
            findings.Add(string.Create(CultureInfo.InvariantCulture, $"{where}: it draws the number character '{numeral}' (U+{(int)numeral:X4}), which is not an ASCII digit; a figure is written in ASCII digits or a listed number word"));
        }

        var tokens = FigureTokens(text);
        if (tokens.Count > 0)
        {
            if (source == claimsPath && Found(sources, source, quote))
            {
                figures.Add(new AdmittedFigure(position, id, text, role, tokens, $"the claim record {claimsPath}"));
            }
            else if (element.TryGetProperty("admittedBy", out var admitted)
                     && Str(admitted, "source") == ownerPath
                     && Found(sources, ownerPath, Str(admitted, "quote"))
                     && Within(Collapse(Str(admitted, "quote")!), Collapse(text)))
            {
                figures.Add(new AdmittedFigure(position, id, text, role, tokens, $"the owner answer quoted from {ownerPath}"));
            }
            else
            {
                findings.Add($"{where}: the figure \"{text}\" (tokens {string.Join(", ", tokens)}) is not admitted: it is not quoted from the claim record and no owner answer containing it is cited");
            }
        }

        texts.Add(new DrawnText(id, text, face == "italic", role));
    }

    private static void CheckMotion(JsonElement motion, string where, IReadOnlyDictionary<string, string> ids, List<string> findings)
    {
        var type = Str(motion, "type") ?? "(no type)";
        if (!MotionFields.TryGetValue(type, out var fields))
        {
            findings.Add($"{where}: unknown motion type {type}");
            return;
        }

        Fields(motion, fields, [], where, findings);
        var from = motion.TryGetProperty("from", out var f) && f.ValueKind == JsonValueKind.Number ? f.GetDecimal() : -1m;
        var to = motion.TryGetProperty("to", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetDecimal() : -1m;
        if (from < 0m || to > 1m || from >= to)
        {
            findings.Add($"{where}: its window {from.ToString(CultureInfo.InvariantCulture)} to {to.ToString(CultureInfo.InvariantCulture)} is not inside 0 to 1, in order");
        }

        foreach (var reference in References(motion).Where(r => !ids.ContainsKey(r)))
        {
            findings.Add($"{where}: it names the undefined element {reference}");
        }

        if (type == "rotate" && motion.TryGetProperty("targets", out var rotated) && rotated.ValueKind == JsonValueKind.Array)
        {
            foreach (var target in rotated.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(ids.ContainsKey))
            {
                if (ids[target] is not ("bearing" or "angle" or "region"))
                {
                    findings.Add($"{where}: rotate names {target}, a {ids[target]}, which has no direction to rotate");
                }
            }
        }

        if (type == "grow" && !GrowProperties.Contains(Str(motion, "property") ?? string.Empty))
        {
            findings.Add($"{where}: grow names the unknown property {Str(motion, "property")}");
        }

        if (type == "follow")
        {
            if (Str(motion, "target") is { } target && ids.TryGetValue(target, out var targetType) && targetType != "point")
            {
                findings.Add($"{where}: follow moves {target}, a {targetType}, and only a point follows a path");
            }

            if (Str(motion, "path") is { } path && ids.TryGetValue(path, out var pathType) && pathType != "path")
            {
                findings.Add($"{where}: follow runs along {path}, a {pathType}, and not a path");
            }
        }
    }

    private static IEnumerable<string> References(JsonElement motion)
    {
        if (motion.TryGetProperty("targets", out var targets) && targets.ValueKind == JsonValueKind.Array)
        {
            foreach (var target in targets.EnumerateArray())
            {
                yield return target.GetString() ?? string.Empty;
            }
        }

        if (motion.TryGetProperty("sets", out var sets) && sets.ValueKind == JsonValueKind.Array)
        {
            foreach (var set in sets.EnumerateArray().Where(s => s.ValueKind == JsonValueKind.Array))
            {
                foreach (var target in set.EnumerateArray())
                {
                    yield return target.GetString() ?? string.Empty;
                }
            }
        }

        foreach (var single in new[] { "target", "path" })
        {
            if (motion.TryGetProperty(single, out var value) && value.ValueKind == JsonValueKind.String)
            {
                yield return value.GetString()!;
            }
        }
    }

    /// <summary>Refuses an unknown field and a missing required one, naming each.</summary>
    private static void Fields(JsonElement node, IReadOnlyList<string> allowed, IReadOnlyList<string> optional, string where, List<string> findings)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            findings.Add($"{where}: not an object");
            return;
        }

        foreach (var property in node.EnumerateObject().Where(p => !allowed.Contains(p.Name, StringComparer.Ordinal) && !optional.Contains(p.Name, StringComparer.Ordinal)))
        {
            findings.Add($"{where}: unknown field {property.Name}");
        }

        foreach (var required in allowed.Where(f => !optional.Contains(f, StringComparer.Ordinal) && !node.TryGetProperty(f, out _)))
        {
            findings.Add($"{where}: missing field {required}; the drawing program fills no default");
        }
    }

    /// <summary>A numeric field is a number, or a reference to a shared value the specification defines.</summary>
    private static void Numbers(JsonElement element, IReadOnlyDictionary<string, decimal> shared, string where, List<string> findings)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name is "fixedBy" or "admittedBy")
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.Object && property.Value.TryGetProperty("shared", out var name)
                && (name.ValueKind != JsonValueKind.String || !shared.ContainsKey(name.GetString()!)))
            {
                findings.Add($"{where}: its field {property.Name} names a shared value the specification does not define");
            }
        }
    }

    /// <summary>No text anywhere in an element is address-like.</summary>
    private static void Addresses(JsonElement element, string where, List<string> findings)
    {
        if (Strings(element).Any(value => value.Contains("://", StringComparison.Ordinal) || value.StartsWith("//", StringComparison.Ordinal)))
        {
            findings.Add($"{where}: it holds an address-like text, and a graphic reaches no address");
        }
    }

    private static IEnumerable<string> Strings(JsonElement node) => node.ValueKind switch
    {
        JsonValueKind.String => [node.GetString()!],
        JsonValueKind.Object => node.EnumerateObject().SelectMany(p => Strings(p.Value)).ToArray(),
        JsonValueKind.Array => node.EnumerateArray().SelectMany(Strings).ToArray(),
        _ => [],
    };

    private static bool Found(IReadOnlyDictionary<string, string> sources, string? source, string? quote) =>
        source is not null && quote is { Length: > 0 } && sources.TryGetValue(source, out var text)
        && Within(Collapse(text), Collapse(quote));

    /// <summary>
    /// Whether <paramref name="quote"/> occurs in <paramref name="text"/> ON WHOLE-TOKEN BOUNDARIES (the review's correction 1).
    /// Both are read by the same tokenizer: words (letters with in-word apostrophes), written figures with their marks and
    /// separators ("~2", "13–15", "60°") and other numerals. At one occurrence at least, (i) no token of the text is cut by the
    /// quotation's edges, whatever character the edge is (a punctuation-led "–15 Hz" or a trailing "13–" cuts "13–15"), and
    /// (ii) every token of the quotation is a complete token of the text at that position, never a part of one.
    /// </summary>
    public static bool Within(string text, string quote)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(quote);
        if (quote.Length == 0)
        {
            return false;
        }

        var textTokens = TokenSpans(text);
        var quoteTokens = TokenSpans(quote);
        for (var at = text.IndexOf(quote, StringComparison.Ordinal); at >= 0; at = text.IndexOf(quote, at + 1, StringComparison.Ordinal))
        {
            var end = at + quote.Length;
            var uncut = textTokens.All(t => t.End <= at || t.Start >= end || (t.Start >= at && t.End <= end));
            var whole = quoteTokens.All(q => textTokens.Contains((at + q.Start, at + q.End)));
            if (uncut && whole)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The token spans of a text: its words, its written figures and its other numerals, each as start and end.</summary>
    private static HashSet<(int Start, int End)> TokenSpans(string text)
    {
        var spans = new HashSet<(int Start, int End)>();
        foreach (var match in Figure().Matches(text).Concat(OtherNumerals().Matches(text)).Concat(Letters().Matches(text)))
        {
            spans.Add((match.Index, match.Index + match.Length));
        }

        return spans;
    }

    private static string? Str(JsonElement node, string name) =>
        node.ValueKind == JsonValueKind.Object && node.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>
    /// The claim record's rows: each claim, identified by its first cell set as code, with the positions of the material its
    /// carried-by column (the last) names. No identifier scheme is written here: claims and positions are what the records name.
    /// </summary>
    private static Dictionary<string, (string Claim, IReadOnlySet<string> Carried)> ClaimRows(string record, IReadOnlyList<string> positions)
    {
        var rows = new Dictionary<string, (string, IReadOnlySet<string>)>(StringComparer.Ordinal);
        foreach (var line in record.Split('\n'))
        {
            var cells = line.Split('|');
            if (cells.Length < 6)
            {
                continue;
            }

            var first = cells[1].Trim();
            if (first.Length < 3 || first[0] != '`' || first[^1] != '`' || first.Contains(' ', StringComparison.Ordinal))
            {
                continue;
            }

            var carried = cells[^2];
            rows[first.Trim('`')] = (cells[2].Trim(), positions.Where(p => Names(carried, p)).ToHashSet(StringComparer.Ordinal));
        }

        return rows;
    }

    /// <summary>Whether a text names an identifier as a whole word.</summary>
    private static bool Names(string text, string identifier) =>
        Regex.IsMatch(text, $@"(?<![\w-]){Regex.Escape(identifier)}(?![\w-])", RegexOptions.CultureInvariant);

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhiteSpace();

    /// <summary>A written figure: an optional approximation, sign or range mark, ASCII digits with internal separators, an optional unit mark.</summary>
    [GeneratedRegex(@"[~≈±+\-−–]?[0-9]+(?:[.,:/–\-][0-9]+)*[°%‰′″]?", RegexOptions.CultureInvariant)]
    private static partial Regex Figure();

    /// <summary>A run of number characters other than the ASCII digits (refused where drawn).</summary>
    [GeneratedRegex(@"[\p{N}-[0-9]]+", RegexOptions.CultureInvariant)]
    private static partial Regex OtherNumerals();

    /// <summary>A word: letters joined by in-word apostrophes.</summary>
    [GeneratedRegex(@"\p{L}+(?:['’]\p{L}+)*", RegexOptions.CultureInvariant)]
    private static partial Regex Letters();

    [GeneratedRegex(@"[A-Za-z][A-Za-z']*", RegexOptions.CultureInvariant)]
    private static partial Regex Word();
}
