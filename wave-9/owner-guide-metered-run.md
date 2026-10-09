# Owner guide — the one metered production run of item 001

This guide is for the owner. It says exactly which credential variables to set, how to set them,
which commands exist, and what the run will and will not do. **It holds no secret, and no secret
is ever written into a file of this repository, a settings file or a command line.**

The run itself is executed by the orchestrator, outside every phase, **only on the owner's explicit
go**, under the hard cap of **USD 5.95** for the video (the owner's decision of 2026-10-09).

---

## 1. The credential variables — exact names

The company is one legal entity, so each provider account is **company-level** and its credential
variable uses the `GLOBAL` form, which needs no channel identifier. The names are fixed by the
account identifiers recorded in `config/preparation-item-001.json`:

| Variable | Account | Used by item 001's run? |
|---|---|---|
| `MEDIACOMPANY_SECRET_OPENAI__GLOBAL` | the speech-and-image vendor's pay-per-use API key | **Yes** — the narration requests |
| `MEDIACOMPANY_SECRET_ANTHROPIC__GLOBAL` | the reasoning vendor's pay-per-use API key | **No** — no reasoning call is planned; set it only if you want it ready for a later wave |

These are the same names the metered mode prints when one is missing
(`the credential variable MEDIACOMPANY_SECRET_OPENAI__GLOBAL is not set`). The value is read by the
credential broker at the moment a request is issued and attached only to the outgoing request; it
is never printed, logged or recorded.

### How to set them (Windows, for your user account)

Open PowerShell **on this machine** and type, replacing the placeholder with your key **in your own
session only** (do not paste the key into any file, chat or ticket):

```powershell
[Environment]::SetEnvironmentVariable("MEDIACOMPANY_SECRET_OPENAI__GLOBAL", "<paste your key here>", "User")
```

Then open a **new** terminal so the variable is visible. To withdraw it after the run:

```powershell
[Environment]::SetEnvironmentVariable("MEDIACOMPANY_SECRET_OPENAI__GLOBAL", $null, "User")
```

The connection string of the company store is set the same way, as `MEDIACOMPANY_CONNECTION_STRING`
(it names the database, never a secret of a vendor).

---

## 2. What the run will do, and what it will not do

**It will:**

- read item 001's recorded package and its item material (`wave-2/item-001/`) — the script,
  research and claims are **not** regenerated;
- send the recorded narration text, all 11,096 characters, to the speech vendor in **14 requests**
  of at most 1,500 characters each (the configured maximum), each admitted against the cap at its
  **worst case before it is made** and that worst case reserved durably before the call;
- draw the 22 graphics, the 5 thumbnail candidates and 18 labelled clip placeholders with the local
  media tool (no capability call), render **one video file** under the output root you configure
  (outside the repository), decode it end to end and measure its runtime from the file;
- record every stage outcome, every operation and every file in the company store, and print the
  plan, the estimate and the cap **before** the first call.

**It will not:** publish, upload, create or configure a channel, create an account, buy anything or
start a subscription; use stock footage or music; generate the macro-frame thumbnail; make a
reasoning or image call; retry a failed narration request automatically; ever pass the cap.

**The cap.** The plan's estimate is **USD 0.17** (11,096 characters at the configured price of USD
15.00 per million characters — an **estimate** from the price the implementing role recorded, to be
re-fetched before the go). Every attempt counts, failed and timed-out ones included, each at its
worst case when the vendor's charge is not known. A call whose worst case would take the total past
USD 5.95 is **not made**: the Audio stage is recorded held, naming the cap and the counted total,
and the command exits with code 4.

---

## 3. The commands

All three need `MEDIACOMPANY_CONNECTION_STRING` and a settings file (copy
`config/production-settings.sample.json` **outside** the repository and fill in every `<...>`).

```text
dotnet run --project src/MediaCompany.Host -- produce --mode plan-only --settings <settings.json>
dotnet run --project src/MediaCompany.Host -- produce --mode fake      --settings <settings.json>
dotnet run --project src/MediaCompany.Host -- produce --mode metered   --settings <settings.json>
```

| Mode | Store it accepts | What it does |
|---|---|---|
| `plan-only` | any designated store | prints the plan, every estimate, the total labelled ESTIMATE, the cap and the bounds; **calls nothing and writes no file** |
| `fake` | a **demonstration** store only | the whole production with fake providers at **USD 0.00**, no network, no credential read; every figure labelled demonstration |
| `metered` | the **company store** only (`mediacompany`, as configured) | the real run; refuses **before any call**, naming every unmet precondition |

Metered mode refuses, naming each, when: the store is not the configured company store or is not
designated company; no cap is recorded; the narration route has no price in force; the cost
controller refuses or defers; a credential variable above is not set (printed by its exact name);
a fake is configured for the planned account; no vendor endpoint is configured; or no narration
voice is configured. **The narration voice is your choice** (it is part of the channel's voice,
still an open question): set `narrationVoice` in the settings file to the vendor voice you choose;
none is invented.

Exit codes: `0` finished; `2` refused before any call; `3` a stage failed (named in its record); `4`
stopped at the cap; `5` an incurred attempt could not be recorded (its facts are printed and its
worst-case reservation keeps counting).

---

## 4. The orchestrator's preconditions of the go

Before your go, and in this order, the orchestrator:

1. **Verifies first hand** the speech vendor's endpoint, request and response shapes, the
   authentication scheme and the price per character, and the terms positions recorded for the
   narration route (automated access permitted, no licence over the company's content). The values
   in `config/preparation-item-001.json` are the implementing role's recorded knowledge of
   2026-10-09 and are **not** verified first hand. A price that differs is recorded as a new price
   row with its own source, date and validity, never by editing the recorded one.
2. **Installs** the nine schema resources into the company store `mediacompany` (it holds no table
   today): `dotnet run --project src/MediaCompany.Host -- install`.
3. **Prepares** it from the recorded configuration:
   `dotnet run --project src/MediaCompany.Host -- prepare --settings <settings.json> --designation company`
   (refused unless the connected database is the configured company store).
4. Runs **plan-only** against the company store and shows you the plan, the estimate and the cap.
5. On your explicit go, runs **metered** once.

Nothing in any phase, test or demonstration has touched the company store.
