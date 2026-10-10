# Owner guide — the company's own voice

This guide is for the owner. It says how to record the narration, how the performer's release is
entered, how a recording is registered, how the installed in-house voice model is verified, the
exact commands that produce an item in each mode and store, and what a run will and will not do.
**It holds no secret.** No command of the own mode reads a credential, calls a vendor or reaches a
network.

Every command below is run from the repository's root on this machine, with the connection string
of the store it acts on set in the environment (it names a database, never a vendor secret):

```powershell
$env:MEDIACOMPANY_CONNECTION_STRING = "Host=localhost;Port=55432;Database=<mediacompany or mediacompany_demo>;Username=postgres;Password=mediacompany-dev"
dotnet run --project src/MediaCompany.Host -- <command> <options>
```

`<settings.json>` below is a copy of `config/production-settings.sample.json` kept **outside the
repository** (for example `C:/Users/vuhoangcao/MediaCompanyRun/settings.own.json`) with every
`<...>` value set. Its `inHouseModel` section already names the model installed on 2026-10-10 and
the SHA-256 of each of its files.

---

## 1. How to record the narration

- **One file per beat.** Item 001 has 13 beats; record each beat as its own file. The text of each
  beat is the narration in `wave-2/item-001/narration.txt` from that beat's opening sentence up to
  the next beat's opening (the openings are listed in `wave-2/item-001/item-material.json`).
- **Name each file by its two-digit beat number**, with any extension: `01.wav`, `02.wav`, …,
  `13.wav`. Put the 13 files, and nothing else, in one folder. A file for the whole narration is
  not accepted; a missing beat, an extra number, two files for one beat or any other file is
  refused, each named.
- **WAV is recommended.** Any file is accepted that decodes from start to end with zero errors and
  holds exactly one audio stream and no video stream.
- **Record every beat with the same settings** (the same sample rate, the same number of channels
  and the same sample format). A set whose beats differ in these is refused before anything is
  recorded, naming each beat's format: nothing is converted, because no format target is set.
- **Nothing is required of the sound.** There is no loudness target, no duration target or
  tolerance and no format target. Every property is **measured and recorded**: the container, the
  codec, the sample rate, the channels, the sample format, the duration (decoded samples over the
  sample rate, never a header) and the loudness (EBU R128 integrated loudness, in LUFS). Nothing is
  adjusted, trimmed, padded, stretched or re-levelled.
- Each beat's measured duration is later printed beside the script's own expectation (150 words a
  minute, as the script records it; 771.6 s for the whole narration, which the script prints as
  12 min 51 s) with the signed difference. That is a reported reference, never a target.

## 2. How the release is entered

Your release (decided on 2026-10-10): a dated statement signed by the performer naming the item,
granting the company the right to edit, publish and monetise the recording, and **not** permitting
its use to train a model. **Keep the signed document outside the repository.** The store keeps only
these five fields, each entered as an option of the registration command:

| Field | Option | What is stored |
|---|---|---|
| the release document's reference | `--release-reference "<reference>"` | the text you give |
| its date | `--release-date yyyy-mm-dd` | the date |
| the performer's name | `--performer "<name>"` | the name you give |
| the document itself | `--release-document "<path outside the repository>"` | **only its SHA-256**; the document is read once to hash and copied nowhere; a path inside the repository is refused |
| the model-training term | `--training-term "model training not permitted"` | the text you give |

A field you do not give is recorded **ABSENT** and printed ABSENT; nothing is filled in. A
production uses a recording only when all five are present; otherwise it passes the recording
over, naming each absent field.

## 3. How to register a recording

```powershell
dotnet run --project src/MediaCompany.Host -- register-recording --settings <settings.json> --recordings "<folder of 01..13>" `
  --performer "<name>" --release-reference "<reference>" --release-date 2026-10-10 `
  --release-document "<path of the signed release, outside the repository>" --training-term "model training not permitted"
```

Each file is copied into the output root, its stored copy hashed, probed, decoded end to end and
measured, then hashed again; only when every file passes is the registration recorded, on the
datastore's instant. Exit codes: **0** registered (each beat printed with its stored path, SHA-256,
format, duration and loudness); **2** refused before anything was recorded, every finding named;
**3** the media tool or the store failed, named.

After registration, **do not edit the stored copies** under the output root: every plan and every
part re-hashes them, and a changed or missing file stops the production, naming the file and both
hashes. To replace a recording, register a new folder; the latest registration is the one used.

## 4. The installed in-house model, and how it is verified

The model is **Piper, `piper-tts` 1.8.0** (code licence GPL-3.0-or-later, used in house and not
distributed) with the voice **`en_GB-cori-high`** (dataset: LibriVox recordings, public domain, as
its model card states; weights and voice licence MIT as declared in the hub repository's card
metadata, recorded as configured values with that source). The orchestrator installed it on
2026-10-10 **outside the repository**, under `C:/Users/vuhoangcao/MediaCompanyRun/voice/` (`venv/`
and `model/`). The `inHouseModel` section of the settings names each file and the SHA-256 the
orchestrator measured first hand: the environment's interpreter and configuration, the base
interpreter it names, the voice model, its configuration (the model path plus `.json`, the one the
runtime loads), the model card, and the installed-files record of the runtime and of each of its six
dependencies. Every hashed entry of those records is checked too. The narration settings are the
voice's defaults, passed explicitly: length scale 1, noise scale 0.667, noise width 0.8, sentence
silence 0 s, volume 1, the runtime's own output normalisation on. The model part bound is 350 s
per beat.

```powershell
dotnet run --project src/MediaCompany.Host -- verify-model --settings <settings.json>
```

It **reads files only and starts no process**. Exit codes: **0** verified, every file printed with
the hash computed now beside the one configured, and the runtime's name, version and licence read
from its installed metadata; **2** refused, every finding named (a missing file, a changed file with
both hashes, a file inside the repository, a configuration that is not the model path plus `.json`).

## 5. How to produce an item

| Mode | Store | Command |
|---|---|---|
| plan only — prints every part's source and why, the verified files and the bounds; starts nothing, writes nothing | demonstration or company | `produce --mode plan-only --settings <settings.json>` |
| own — the company's own narration | demonstration (`mediacompany_demo`) or company (`mediacompany`, on the owner's go) | `produce --mode own --settings <settings.json>` |
| fake — the demonstration composition | demonstration only | `produce --mode fake --settings <settings.json>` |
| metered — the speech vendor | **refused**, naming the owner's decision of 2026-10-10 | `produce --mode metered --settings <settings.json>` exits 2 |

Which source narrates is decided by one fixed rule, printed per part before anything runs: **your
registered recording**, when its release is complete and every stored file still matches;
otherwise **the in-house model**, when every configured file verifies; otherwise the run is refused,
naming why. The speech vendor is never chosen. One source narrates a whole production: before each
part the rule is applied again, and if the answer changed (a recording registered meanwhile, a
model file changed), the run stops before that part, naming both sources.

Exit codes of `produce`: **0** rendered, the item held short of publish-ready; **2** refused before
anything was recorded or called, every reason named; **3** a stage failed or was held, named (a
part past the model part bound, a changed file, a source that changed); **4** stopped at the item
cap (boundary sources only); **5** an incurred attempt could not be recorded (boundary sources
only); **6** cancelled by you (Ctrl+C), the interrupted stage recorded failed.

### The company store, on the owner's go (the orchestrator runs these)

1. A fresh backup of the company store.
2. The tenth schema resource applied **alone** to the store holding nine, and applied again to show
   it changes nothing: `install --from 10`. (A plain `install` over a store that already holds a
   designation is refused, naming this option.)
3. `verify-model --settings <settings.json>`.
4. Recommended first: `produce --mode own` against the demonstration database with the real model.
5. `produce --mode plan-only` against the company store: every part named `InHouseModel`, every
   hash verified, no metered spend planned.
6. On your go: `produce --mode own` against the company store.

## 6. What a run will and will not do

**It will:** narrate the recorded text locally, one part per beat (13 for item 001), from your
recording or the in-house model on this machine; measure every part and the joined narration from
decoded audio and record the measurements beside the script's expectation; record each part's
provenance (for the model: its name, version and licences, the voice, every setting passed, the
SHA-256 of every file loaded, the exact command line, and whether the first part, generated twice,
repeated — with the default settings it is expected to read **not observed to repeat**; for a
recording: the registration, the beat, the performer and the release); join the parts with no
inserted silence; render, decode end to end and measure **one video file** under the output root;
book **no operation — USD 0.00 metered spend** — leaving the item cap's counted total at its
recorded USD 0.007695 estimate; print local compute (and recording time) as **unmeasured**.

**It will not:** call the speech vendor or any network service; download anything; read or need a
credential; store the release document, a recording outside the output root, the model or its
runtime in the repository or the store; change any loudness, duration, format or sample rate;
claim a repeatability it did not observe; publish, upload or configure anything — the item stays
held short of publish-ready.
