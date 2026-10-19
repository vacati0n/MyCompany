# Command Specification: /implement

## Purpose
Start end-to-end feature delivery for new functionality.

## Inputs
- Feature summary.
- Acceptance criteria.
- Priority and deadline.

## Parameters

Required parameters are the inputs above, supplied as `--input feature-request=<path>`. The
optional parameters below refine output format only; none of them changes routing, the Phase
Model, a gate, or what any agent is asked to do.

| Parameter | Type | Default | Effect |
|---|---|---|---|
| `--demo` | flag | off | Record the run for an autonomous end-to-end demonstration. The runtime throws the run's demo switch (`runs/<run>/demo/demo-context.json`) before the first event; from then on every canonical event, dispatch prompt, agent result, validation verdict, gate decision, and host tool call becomes a millisecond-stamped JSON marker in `runs/<run>/demo/markers.jsonl`. With `demo --record-start` the runtime also films the Claude Desktop window, and at run completion it cuts that footage into `runs/<run>/demo/presentation_demo.mp4` using the markers as the edit decision list, with no further command. Without footage it draws a dashboard instead; `build-manifest.json` records which. Idempotent, and may be added to a run already in flight, in which case the events already persisted are backfilled. |
| `--demo-title <text>` | string | first heading of the feature request | Opening card title. |
| `--demo-seconds <n>` | number | 90 | Presentation length the timeline compresses towards. Ignored under `--demo-explain`, where the narration sets the length instead. |
| `--demo-pace <mode>` | `relaxed` / `brisk` | `relaxed` | How the film is paced. `relaxed` holds every beat long enough to take in and condenses waiting far less, for a viewer meeting the framework once; `brisk` is the short highlight reel. |
| `--demo-explain` / `--demo-no-explain` | flag | on | Narrate in full, in plain language, explaining what each step means rather than naming it. A concept is explained the first time it appears and named thereafter. In this mode the script is recorded first and the picture is paced to it: a beat short of footage is un-condensed, then slowed, and only then held on its last frame. |
| `--demo-speech-rate <n>` | -6 to 6 | -1 | Delivery speed of the voice, where 0 is the engine's own pace. |
| `--demo-no-captions` | flag | captions on | Hide the spoken line on screen. Captions are timed from the recorded narration, not guessed, and the subtitle file (`presentation_demo.srt`) and script (`presentation_demo-script.md`) are written either way. |
| `--demo-narration <mode>` | `auto` / `off` / engine | `auto` | Voice-over engine: `auto` tries edge-tts, pyttsx3, Windows SAPI, macOS `say`, espeak-ng in that order and records which one produced audio. |
| `--demo-record` | flag | off | Start filming the Claude Desktop window as part of this command, before the run's first event is emitted, so the opening beats are on camera. Uses OBS Studio when its WebSocket server is enabled, otherwise ffmpeg over the window's rectangle. A failure to start is reported and never stops the run. |

Operator entry points carrying the flag. `<framework-dir>` below is this framework
directory's real name, `.claude` in the framework's own checkout:

```bash
python <framework-dir>/runtime/framework_runtime.py plan --demo \
    --input feature-request=<framework-dir>/runs/inputs/<request>.md
python <framework-dir>/runtime/framework_runtime.py demo --run-id <run> --record-start
python <framework-dir>/runtime/framework_runtime.py demo --run-id <run> --record-stop --build
python <framework-dir>/runtime/framework_runtime.py demo --run-id <run> [--snapshot|--build|--backfill]
python <framework-dir>/runtime/framework_runtime.py demo --print-hook
```

Host tool calls reach the film only when the wiring block from
`runtime/demo/hooks.settings.json` is merged into the host's settings; `demo --print-hook`
prints that block with the framework directory already resolved. Without it the markers
carry the runtime's own events and nothing the host did.

Zero-bloat rule: without `--demo` the demo package is never imported; the main execution path
pays one `stat()` per event on the switch file and nothing else.

## Workflow Triggered
Implement Feature (`workflows/implement-feature.md`).

## Expected Outputs
- Scoped implementation plan.
- Code changes with tests.
- Review findings resolution and release note draft.

## Success Criteria
- Acceptance criteria validated by QA.
- No unresolved critical review findings.
- Delivery artifacts are complete.

## Failure Handling
- Route ambiguity to clarification and requirement refinement.
- Return failed validation to implementation stage.
- Escalate blockers to orchestrator and tech lead.
