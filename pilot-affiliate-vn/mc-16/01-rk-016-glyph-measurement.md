# RK-016, part 1: Vietnamese glyph coverage of the installed face (orchestrator's measurement, 2026-10-26)

MEASURED on this machine with the repository's own `FontCoverage.Read`
(`src/MediaCompany.Deterministic/Production/FontCoverage.cs`), run through a .NET file-based program that references
`MediaCompany.Deterministic.csproj`. Nothing was downloaded; the faces are the Wave 11 installation verified by
`C:/Users/vuhoangcao/MediaCompanyRun/graphics/manifests/font.sha256` (`CEO-D-1001`).

Character set tested (204 code points):
- the 29 Vietnamese base letters, lower and upper case (a ă â b c d đ e ê g h i k l m n o ô ơ p q r s t u ư v x y);
- every toned vowel, precomposed (NFC), lower and upper case: à á ả ã ạ, ằ ắ ẳ ẵ ặ, ầ ấ ẩ ẫ ậ, è é ẻ ẽ ẹ, ề ế ể ễ ệ, ì í ỉ ĩ ị,
  ò ó ỏ õ ọ, ồ ố ổ ỗ ộ, ờ ớ ở ỡ ợ, ù ú ủ ũ ụ, ừ ứ ử ữ ự, ỳ ý ỷ ỹ ỵ;
- the combining marks a decomposed (NFD) text would carry: U+0300, U+0301, U+0303, U+0309, U+0323, U+0306, U+0302, U+031B;
- digits 0-9 and the punctuation the script and overlay draw: . , : ; ! ? · (U+00B7, the overlay separator) - – " ' ( ) % / … and space.

| Face file | Family / subfamily | Version | Missing code points |
|---|---|---|---|
| `Inter-4.1/extras/ttf/Inter-Regular.ttf` | Inter / Regular | 4.001;git-9221beed3 | 0 |
| `Inter-4.1/extras/ttf/Inter-Italic.ttf` | Inter / Italic | 4.001;git-9221beed3 | 0 |
| `Inter-4.1/extras/ttf/Inter-SemiBold.ttf` | Inter / SemiBold | 4.001;git-9221beed3 | 0 |
| `Inter-4.1/extras/ttf/Inter-Bold.ttf` | Inter / Bold | 4.001;git-9221beed3 | 0 |

Result: the installed Inter 4.1 faces map every code point listed above. Coverage of the character map is not the
same as correct stacking of a second diacritic over a circumflex or breve when drawn; that needs a rendered frame,
which the composition test must show (part 2 of RK-016, vertical output, is the architect's).

Program used:

```csharp
#:project D:/Project/MyCompany/.claude/worktrees/mc-16-pilot-pipeline-a4df61/src/MediaCompany.Deterministic/MediaCompany.Deterministic.csproj
using MediaCompany.Deterministic.Production;
var vn = "aăâbcdđeêghiklmnoôơpqrstuưvxyAĂÂBCDĐEÊGHIKLMNOÔƠPQRSTUƯVXY" +
 "àáảãạằắẳẵặầấẩẫậèéẻẽẹềếểễệìíỉĩịòóỏõọồốổỗộờớởỡợùúủũụừứửữựỳýỷỹỵ" +
 "ÀÁẢÃẠẰẮẲẴẶẦẤẨẪẬÈÉẺẼẸỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌỒỐỔỖỘỜỚỞỠỢÙÚỦŨỤỪỨỬỮỰỲÝỶỸỴ0123456789.,:;!?·-–\"'()%/ ";
foreach (var f in args) {
  var r = FontCoverage.Read(File.ReadAllBytes(f));
  var miss = (vn + "0300030103030309032303060302031B00B72026").EnumerateRunes().Where(c => !r.Covers(c.Value)).Select(c => $"{c} U+{c.Value:X4}").ToList();
  Console.WriteLine($"{Path.GetFileName(f)} {r.Family}/{r.Subfamily} v{r.Version}: {vn.Length} chars, missing {miss.Count}: {string.Join(", ", miss)}");
}
```
