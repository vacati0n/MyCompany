"""Narration -- text-to-speech over the scenes' narration lines, with an engine chain.

Every engine below produces one 16-bit PCM WAV per scene; the builder then lays them on a
single narration track at each scene's start offset. The chain is tried in order and the
first engine that produces audio wins; an engine that is not installed, has no voice, or
fails on the first sentence is skipped with its reason recorded in the build manifest.
When no engine works the presentation is silent, and the HTML deck says so rather than
pretending.

    edge      `edge-tts` (Microsoft neural voices, needs network and ffmpeg to decode mp3)
    pyttsx3   the cross-platform offline wrapper (SAPI5 / NSSpeechSynthesizer / espeak)
    sapi      Windows System.Speech through PowerShell -- no Python package needed
    say       macOS `say` writing WAVE directly
    espeak    `espeak-ng` / `espeak` on Linux

Select with the context option `narration` or the environment variable `FRAMEWORK_DEMO_TTS`:
`auto` (default), `off`, or an engine name. `FRAMEWORK_DEMO_VOICE` names a voice.
"""

from __future__ import annotations

import array
import json
import os
import shutil
import subprocess
import sys
import wave
from pathlib import Path

TARGET_RATE = 22050
ENGINES = ("edge", "pyttsx3", "sapi", "say", "espeak")
DEFAULT_EDGE_VOICE = "en-US-AriaNeural"

# Delivery speed, as a small signed number every engine maps onto its own scale: 0 is the
# engine's natural pace, negative is slower. The default is deliberately below the engine
# default -- a synthetic voice at its own idea of normal is noticeably brisk for a listener
# hearing unfamiliar material once, and the whole point of the narration is to be followed.
DEFAULT_SPEECH_RATE = -1
BASE_WPM = 185


# ------------------------------------------------------------------ WAV utilities


def read_wav(path: Path) -> tuple[int, array.array]:
    """(rate, mono 16-bit samples). Stereo is averaged, 8/24/32-bit widths are converted."""
    with wave.open(str(path), "rb") as w:
        rate, ch, width, n = w.getframerate(), w.getnchannels(), w.getsampwidth(), w.getnframes()
        raw = w.readframes(n)
    if width == 2:
        samples = array.array("h")
        samples.frombytes(raw)
        if sys.byteorder == "big":
            samples.byteswap()
    elif width == 1:
        samples = array.array("h", ((b - 128) << 8 for b in raw))
    else:
        step = width
        samples = array.array("h", (int.from_bytes(raw[i + step - 2:i + step], "little",
                                                   signed=True)
                                    for i in range(0, len(raw) - step + 1, step)))
    if ch > 1:
        mono = array.array("h", (int(sum(samples[i:i + ch]) / ch)
                                 for i in range(0, len(samples) - ch + 1, ch)))
        samples = mono
    return rate, samples


def resample(samples: array.array, src: int, dst: int) -> array.array:
    if src == dst or not samples:
        return samples
    ratio = src / dst
    out_len = int(len(samples) / ratio)
    out = array.array("h", bytes(out_len * 2))
    last = len(samples) - 1
    for i in range(out_len):
        pos = i * ratio
        j = int(pos)
        frac = pos - j
        a = samples[j]
        b = samples[j + 1] if j < last else a
        out[i] = int(a + (b - a) * frac)
    return out


def write_wav(path: Path, samples: array.array, rate: int = TARGET_RATE) -> None:
    if sys.byteorder == "big":
        samples = array.array("h", samples)
        samples.byteswap()
    data = samples.tobytes()
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(data)


def duration_ms(path: Path) -> int:
    with wave.open(str(path), "rb") as w:
        return int(w.getnframes() * 1000 / w.getframerate())


def assemble(segments: list, total_ms: int, out_path: Path, rate: int = TARGET_RATE) -> Path:
    """Lay `segments` ([{offset_ms, path}]) onto one silent track of `total_ms`."""
    total = max(1, int(total_ms * rate / 1000))
    track = array.array("h", bytes(total * 2))
    for seg in segments:
        src_rate, samples = read_wav(Path(seg["path"]))
        samples = resample(samples, src_rate, rate)
        start = int(seg["offset_ms"] * rate / 1000)
        end = min(total, start + len(samples))
        if end > start:
            track[start:end] = samples[: end - start]
    write_wav(out_path, track, rate)
    return out_path


# ------------------------------------------------------------------ engines

def _texts_to_files(lines: list, work: Path) -> list:
    work.mkdir(parents=True, exist_ok=True)
    items = []
    for idx, text in lines:
        tf = work / f"scene-{idx:03d}.txt"
        tf.write_text(text, encoding="utf-8")
        items.append({"index": idx, "text": tf, "wav": work / f"scene-{idx:03d}.wav"})
    return items


def _ok(items: list) -> list:
    """Keep the items whose WAV exists and is non-trivial."""
    out = []
    for it in items:
        p = Path(it["wav"])
        if p.exists() and p.stat().st_size > 1000:
            try:
                out.append({"index": it["index"], "path": str(p), "duration_ms": duration_ms(p)})
            except (wave.Error, EOFError):
                continue
    return out


def _rate(rate) -> int:
    try:
        return max(-6, min(6, int(rate)))
    except (TypeError, ValueError):
        return DEFAULT_SPEECH_RATE


def _wpm(rate) -> int:
    return max(90, BASE_WPM + _rate(rate) * 22)


def engine_sapi(lines: list, work: Path, voice: str | None, ffmpeg: str | None,
                rate=None) -> list:
    if os.name != "nt" or not shutil.which("powershell"):
        raise RuntimeError("Windows PowerShell is not available")
    items = _texts_to_files(lines, work)
    manifest = work / "sapi-batch.json"
    manifest.write_text(json.dumps([{"text": str(i["text"]), "wav": str(i["wav"])}
                                    for i in items]), encoding="utf-8")
    script = work / "sapi-batch.ps1"
    voice_line = f"try {{ $s.SelectVoice('{voice}') }} catch {{ }}" if voice else ""
    script.write_text(f"""
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$s = New-Object System.Speech.Synthesis.SpeechSynthesizer
{voice_line}
$s.Rate = {_rate(rate)}
$fmt = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo({TARGET_RATE}, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
$items = Get-Content -Raw -Encoding UTF8 '{manifest}' | ConvertFrom-Json
foreach ($it in $items) {{
  $text = [IO.File]::ReadAllText($it.text, [Text.Encoding]::UTF8)
  $s.SetOutputToWaveFile($it.wav, $fmt)
  $s.Speak($text)
  $s.SetOutputToNull()
}}
$s.Dispose()
""", encoding="utf-8")
    subprocess.run(["powershell", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass",
                    "-File", str(script)], check=True, capture_output=True, timeout=600)
    return _ok(items)


def engine_pyttsx3(lines: list, work: Path, voice: str | None, ffmpeg: str | None,
                   rate=None) -> list:
    import pyttsx3  # type: ignore
    items = _texts_to_files(lines, work)
    eng = pyttsx3.init()
    eng.setProperty("rate", _wpm(rate))
    if voice:
        for v in eng.getProperty("voices"):
            if voice.lower() in (v.name or "").lower() or voice == v.id:
                eng.setProperty("voice", v.id)
                break
    for it in items:
        eng.save_to_file(it["text"].read_text(encoding="utf-8"), str(it["wav"]))
    eng.runAndWait()
    return _ok(items)


def engine_say(lines: list, work: Path, voice: str | None, ffmpeg: str | None,
               rate=None) -> list:
    if not shutil.which("say"):
        raise RuntimeError("macOS `say` is not available")
    items = _texts_to_files(lines, work)
    for it in items:
        cmd = ["say", "-r", str(_wpm(rate)), "-o", str(it["wav"]), "--file-format=WAVE",
               f"--data-format=LEI16@{TARGET_RATE}", "-f", str(it["text"])]
        if voice:
            cmd[1:1] = ["-v", voice]
        subprocess.run(cmd, check=True, capture_output=True, timeout=120)
    return _ok(items)


def engine_espeak(lines: list, work: Path, voice: str | None, ffmpeg: str | None,
                  rate=None) -> list:
    exe = shutil.which("espeak-ng") or shutil.which("espeak")
    if not exe:
        raise RuntimeError("espeak-ng is not available")
    items = _texts_to_files(lines, work)
    for it in items:
        cmd = [exe, "-s", str(_wpm(rate)), "-w", str(it["wav"]), "-f", str(it["text"])]
        if voice:
            cmd += ["-v", voice]
        subprocess.run(cmd, check=True, capture_output=True, timeout=120)
    return _ok(items)


def engine_edge(lines: list, work: Path, voice: str | None, ffmpeg: str | None,
                rate=None) -> list:
    import asyncio
    import edge_tts  # type: ignore
    if not ffmpeg:
        raise RuntimeError("edge-tts writes mp3 and no ffmpeg is available to decode it")
    items = _texts_to_files(lines, work)
    pct = _rate(rate) * 8
    speed = f"{pct:+d}%"

    async def run():
        for it in items:
            mp3 = Path(it["wav"]).with_suffix(".mp3")
            await edge_tts.Communicate(it["text"].read_text(encoding="utf-8"),
                                       voice or DEFAULT_EDGE_VOICE,
                                       rate=speed).save(str(mp3))
            subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-i", str(mp3), "-ac", "1",
                            "-ar", str(TARGET_RATE), str(it["wav"])], check=True,
                           capture_output=True, timeout=120)
    asyncio.run(run())
    return _ok(items)


ENGINE_FN = {"edge": engine_edge, "pyttsx3": engine_pyttsx3, "sapi": engine_sapi,
             "say": engine_say, "espeak": engine_espeak}


def synthesize(lines: list, work: Path, *, engine: str = "auto", voice: str | None = None,
               ffmpeg: str | None = None, rate=None) -> dict:
    """Try the engine chain over `lines` ([(scene_index, text)]).

    Returns {"engine": name|None, "segments": [{index, path, duration_ms}], "attempts": [...]}.
    Never raises: a narration that cannot be made is a recorded fallback, not a failure.
    """
    engine = (os.environ.get("FRAMEWORK_DEMO_TTS") or engine or "auto").lower()
    voice = os.environ.get("FRAMEWORK_DEMO_VOICE") or voice
    if rate is None:
        rate = os.environ.get("FRAMEWORK_DEMO_SPEECH_RATE")
    attempts = []
    if engine == "off" or not lines:
        return {"engine": None, "segments": [], "attempts": [{"engine": "off",
                                                              "reason": "narration disabled"}]}
    order = list(ENGINES) if engine == "auto" else [engine]
    for name in order:
        fn = ENGINE_FN.get(name)
        if fn is None:
            attempts.append({"engine": name, "reason": "unknown engine"})
            continue
        try:
            segments = fn(lines, work / name, voice, ffmpeg, rate)
        except Exception as exc:  # noqa: BLE001 -- every engine failure is a recorded fallback
            attempts.append({"engine": name, "reason": f"{type(exc).__name__}: {str(exc)[:200]}"})
            continue
        if segments:
            attempts.append({"engine": name, "reason": "ok", "segments": len(segments)})
            return {"engine": name, "segments": segments, "attempts": attempts}
        attempts.append({"engine": name, "reason": "produced no audio"})
    return {"engine": None, "segments": [], "attempts": attempts}
