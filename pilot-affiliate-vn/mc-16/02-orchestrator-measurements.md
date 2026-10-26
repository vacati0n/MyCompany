# MC-16: the orchestrator's measurements for the design phase (2026-10-26)

All MEASURED on this machine, nothing downloaded or installed.

| # | Question | Finding |
|---|---|---|
| M1 | Does the installed face cover Vietnamese? | Yes: `01-rk-016-glyph-measurement.md`, 0 of 204 code points missing in Inter 4.1 Regular, Italic, SemiBold and Bold. |
| M2 | Can the installed Remotion play a video clip inside a composition? | The installed set (`C:/Users/vuhoangcao/MediaCompanyRun/graphics/remotion/node_modules`, Remotion 4.0.534) holds `@remotion/compositor-win32-x64-msvc` (with `remotion.exe`, `ffmpeg.exe`, `ffprobe.exe`), which `OffthreadVideo` needs; its files are in the Wave 11 manifest `manifests/remotion.sha256` (14,789 lines). Whether a render with an `OffthreadVideo` succeeds under the delivered render script's job and module-resolution hook is NOT yet measured: the design's vertical spike must show it. |
| M3 | Which installed Remotion packages must the toolchain never load? | The same `node_modules/@remotion` holds `openai-whisper` (speech recognition) and `elevenlabs` (text-to-speech), present as part of the installed set but never loaded by the delivered drawing program. The delivered render script writes the bundle's module list (`moduleList`); MC-16 can prove from it that neither package, nor `@remotion/captions` transcription helpers, is in the bundle. |
| M4 | Does the installed ffmpeg detect pauses? | Yes. The WinGet ffmpeg has `silencedetect`. On a generated file (1.5 s tone, 0.6 s digital silence, 2.0 s tone), `silencedetect=noise=-35dB:d=0.35` reported `silence_start: 1.499909`, `silence_end: 2.100091`, `silence_duration: 0.600181`. A real room recording has a noise floor; the threshold is a design parameter to be checked against the operator's first real recording. |
| M5 | Other ffmpeg filters present | `zoompan`, `drawtext`, `subtitles`, `ass` are present in the WinGet build. |
