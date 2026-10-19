"""Video synthesis engine -- markers to `presentation_demo.mp4`, with graceful fallbacks.

    markers.jsonl --> scenes (timeline) --> narration (optional) --> frames (renderer)
                                                                        |
                                    +-----------------------------------+
                                    v
                       ffmpeg present?  yes -> presentation_demo.mp4  (H.264 + AAC)
                                        no  -> presentation_demo.gif  (Pillow, animated)
                       Pillow present?  no  -> text frames only
                       always           -> presentation_demo.html    (self-contained deck)

Every product is written under `runs/<run>/demo/`, and `build-manifest.json` records what
was produced, what was skipped, and why -- so a presentation that came out as a GIF says
"ffmpeg not found" in its own manifest rather than leaving anyone to guess.

Overlays, transitions, and the "Agent Thinking..." status bar are painted into the frames by
the renderer rather than applied with ffmpeg filters, so the MP4, the GIF, and the deck's
keyframes are the same picture, and no font-configuration of ffmpeg is needed on any host.
"""

from __future__ import annotations

import io
import json
import os
import shutil
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

from . import DEMO_DIR, VERSION, log
from . import narration as tts
from . import renderer
from .context import DEFAULT_OPTIONS, DemoContext
from .recorder import Recorder
from .timeline import build_scenes, choose_narration, compress, total_ms

MANIFEST = "build-manifest.json"
BASENAME = "presentation_demo"
TRANSITION_S = 0.30
GIF_FPS = 5
GIF_WIDTH = 640
KEYFRAME_WIDTH = 880


def _now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


# ------------------------------------------------------------------ ffmpeg discovery


def find_ffmpeg() -> tuple[str | None, str]:
    """(path, how) -- the first ffmpeg the host offers, or (None, reason)."""
    env = os.environ.get("FFMPEG_BINARY")
    if env and Path(env).exists():
        return env, "FFMPEG_BINARY"
    exe = shutil.which("ffmpeg")
    if exe:
        return exe, "PATH"
    try:
        import imageio_ffmpeg  # type: ignore
        exe = imageio_ffmpeg.get_ffmpeg_exe()
        if exe and Path(exe).exists():
            return exe, "imageio-ffmpeg"
    except Exception:  # noqa: BLE001
        pass
    node = shutil.which("node")
    if node:
        for cwd in (Path.cwd(), Path(__file__).resolve().parents[3]):
            try:
                out = subprocess.run([node, "-p", "require('ffmpeg-static')"], cwd=str(cwd),
                                     capture_output=True, text=True, timeout=20)
                cand = (out.stdout or "").strip()
                if out.returncode == 0 and cand and Path(cand).exists():
                    return cand, "ffmpeg-static (node)"
            except (OSError, subprocess.SubprocessError):
                continue
    if os.name == "nt":
        local = os.environ.get("LOCALAPPDATA", "")
        for cand in (Path(local) / "Microsoft" / "WinGet" / "Links" / "ffmpeg.exe",
                     Path("C:/ffmpeg/bin/ffmpeg.exe")):
            if cand.exists():
                return str(cand), "well-known path"
    return None, ("ffmpeg not found: not on PATH, no FFMPEG_BINARY, no imageio-ffmpeg, "
                  "no ffmpeg-static")


# ------------------------------------------------------------------ frame production


def iter_frames(scenes: list, opts: dict, *, title: str, width: int, height: int, fps: int,
                every: int = 1):
    """Yield (image, scene_index) for the whole film, crossfading between scenes.

    `every` > 1 renders only every n-th frame (the GIF samples the film at a lower rate, and
    a frame nobody will see is not worth painting); the scene's last frame is always rendered
    because the next transition blends from it.
    """
    prev_last = None
    trans_n = max(1, int(TRANSITION_S * fps))
    frame_index = 0
    for si, s in enumerate(scenes):
        n = max(1, int(round(s["duration_ms"] * fps / 1000)))
        for k in range(n):
            last = k == n - 1
            if frame_index % every and not last:
                frame_index += 1
                continue
            t = k / n
            img = renderer.render_frame(s["projection"], s["marker"], s, t, title=title,
                                        width=width, height=height,
                                        theme=opts.get("theme", "midnight"),
                                        frame_index=frame_index, fps=fps)
            if prev_last is not None and k < trans_n:
                img = renderer.blend(prev_last, img, (k + 1) / (trans_n + 1))
            frame_index += 1
            if not (last and (frame_index - 1) % every):
                yield img, si
            if last:
                prev_last = img


def keyframe(scene: dict, opts: dict, *, title: str, width: int, height: int, fps: int):
    """One representative frame per scene for the deck: after the overlay has faded."""
    t = min(0.95, float(scene.get("overlay_until", 0.38)) + 0.12)
    return renderer.render_frame(scene["projection"], scene["marker"], scene, t, title=title,
                                 width=width, height=height, theme=opts.get("theme", "midnight"),
                                 frame_index=int(t * scene["duration_ms"] * fps / 1000), fps=fps)


def encode_mp4(ffmpeg: str, frames, out: Path, *, width: int, height: int, fps: int,
               audio: Path | None) -> int:
    cmd = [ffmpeg, "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24",
           "-s", f"{width}x{height}", "-r", str(fps), "-i", "pipe:0"]
    if audio:
        cmd += ["-i", str(audio)]
    cmd += ["-c:v", "libx264", "-preset", "medium", "-crf", "20", "-pix_fmt", "yuv420p",
            "-movflags", "+faststart"]
    if audio:
        cmd += ["-c:a", "aac", "-b:a", "128k", "-shortest"]
    cmd += [str(out)]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                            stderr=subprocess.PIPE)
    count = 0
    try:
        for img, _ in frames:
            proc.stdin.write(img.tobytes())
            count += 1
    finally:
        proc.stdin.close()
    _, err = proc.communicate(timeout=1800)
    if proc.returncode != 0:
        raise RuntimeError(f"ffmpeg exited {proc.returncode}: {err.decode(errors='replace')[-400:]}")
    return count


def encode_gif(frames_iter, out: Path, *, gif_fps: int, width: int) -> int:
    """Downscale, quantise, and write the sampled frames as an animated GIF."""
    from PIL import Image
    kept = []
    for img, _ in frames_iter:
        h = int(img.height * width / img.width)
        small = img.resize((width, h), Image.LANCZOS).convert("P", palette=Image.ADAPTIVE,
                                                               colors=96)
        kept.append(small)
    if not kept:
        raise RuntimeError("no frames to encode")
    kept[0].save(str(out), save_all=True, append_images=kept[1:],
                 duration=int(1000 / gif_fps), loop=0, optimize=False, disposal=2)
    return len(kept)


def _png_bytes(img, width: int | None = None) -> bytes:
    from PIL import Image
    if width and img.width > width:
        img = img.resize((width, int(img.height * width / img.width)), Image.LANCZOS)
    buf = io.BytesIO()
    img.convert("P", palette=Image.ADAPTIVE, colors=256).save(buf, format="PNG", optimize=True)
    return buf.getvalue()


# ------------------------------------------------------------------ the build


def build(run_dir: Path, *, reason: str | None = None, out_dir: Path | None = None,
          target_seconds: float | None = None, formats: list | None = None,
          narration: str | None = None, fps: int | None = None, width: int | None = None,
          height: int | None = None, quiet: bool = False, keep_frames: bool = False) -> dict:
    """Compile the presentation for one run. Never raises; the manifest records the outcome."""
    run_dir = Path(run_dir)
    started = time.monotonic()
    ctx = DemoContext.load(run_dir)
    opts = dict(ctx.options) if ctx else dict(DEFAULT_OPTIONS)
    for k, v in (("target_seconds", target_seconds), ("formats", formats),
                 ("narration", narration), ("fps", fps), ("width", width), ("height", height)):
        if v is not None:
            opts[k] = v
    out_dir = Path(out_dir) if out_dir else run_dir / DEMO_DIR
    out_dir.mkdir(parents=True, exist_ok=True)
    title = opts.get("title") or run_dir.name
    W, H, FPS = int(opts["width"]), int(opts["height"]), int(opts["fps"])
    manifest = {
        "schema": "framework.runtime/demo-build-manifest.v1", "version": VERSION,
        "run_id": run_dir.name, "built_at": _now(), "reason": reason or "manual",
        "title": title, "options": opts, "outputs": {}, "fallbacks": [], "narration": {},
        "scenes": 0, "duration_ms": 0, "frames": 0, "engine": None, "errors": [],
    }

    def say(line: str):
        if quiet:
            return
        enc = sys.stdout.encoding or "utf-8"
        print(line.encode(enc, "replace").decode(enc))

    try:
        rec = Recorder(run_dir)
        markers = rec.markers()
        if not markers:
            rec.backfill_from_events()
            markers = rec.markers()
            manifest["fallbacks"].append({"stage": "markers", "reason":
                                          "no live markers; reconstructed from events.jsonl"})
        if not markers:
            raise RuntimeError("no markers and no events to build from")
        manifest["outputs"]["markers"] = str(rec.path)

        scenes = build_scenes(markers, opts)
        ffmpeg, how = find_ffmpeg()
        manifest["ffmpeg"] = {"path": ffmpeg, "resolved_by": how}

        # narration first: its durations decide how long each scene must hold
        nar_mode = str(opts.get("narration") or "auto")
        density = choose_narration(scenes, float(opts.get("target_seconds") or 0) or None)
        manifest["narration_density"] = density
        lines = [(i, s["spoken"]) for i, s in enumerate(scenes) if s.get("spoken")]
        nar = tts.synthesize(lines, out_dir / "narration", engine=nar_mode,
                             voice=opts.get("voice"), ffmpeg=ffmpeg) \
            if nar_mode != "off" else {"engine": None, "segments": [], "attempts": [
                {"engine": "off", "reason": "narration disabled"}]}
        manifest["narration"] = {"engine": nar["engine"], "attempts": nar["attempts"],
                                 "segments": len(nar["segments"])}
        durations = {seg["index"]: seg["duration_ms"] for seg in nar["segments"]}
        compress(scenes, float(opts.get("target_seconds") or 0) or None, durations)
        film_ms = total_ms(scenes)
        manifest["scenes"], manifest["duration_ms"] = len(scenes), film_ms
        audio_path = None
        if nar["segments"]:
            audio_path = out_dir / "narration.wav"
            tts.assemble([{"offset_ms": scenes[s["index"]]["start_ms"], "path": s["path"]}
                          for s in nar["segments"]], film_ms + 500, audio_path)
            manifest["outputs"]["narration_wav"] = str(audio_path)
        elif nar_mode != "off":
            manifest["fallbacks"].append({"stage": "narration", "reason":
                                          "no text-to-speech engine produced audio; silent film"})

        wanted = [f.lower() for f in (opts.get("formats") or ["mp4", "gif", "html"])]
        pil = renderer.has_pil()
        if not pil:
            manifest["fallbacks"].append({"stage": "frames", "reason":
                                          "Pillow not installed; text frames only"})

        keyframes: dict = {}
        if pil:
            for i, s in enumerate(scenes):
                keyframes[i] = _png_bytes(keyframe(s, opts, title=title, width=W, height=H,
                                                   fps=FPS), KEYFRAME_WIDTH)
            if keep_frames:
                kf_dir = out_dir / "frames"
                kf_dir.mkdir(exist_ok=True)
                for i, png in keyframes.items():
                    (kf_dir / f"scene-{i:03d}.png").write_bytes(png)
                manifest["outputs"]["keyframes_dir"] = str(kf_dir)

        primary_done = False
        for fmt in wanted:
            if fmt == "mp4" and not primary_done:
                if not pil:
                    manifest["fallbacks"].append({"stage": "mp4", "reason": "Pillow missing"})
                    continue
                if not ffmpeg:
                    manifest["fallbacks"].append({"stage": "mp4", "reason": how})
                    continue
                out = out_dir / f"{BASENAME}.mp4"
                try:
                    n = encode_mp4(ffmpeg, iter_frames(scenes, opts, title=title, width=W,
                                                       height=H, fps=FPS), out,
                                   width=W, height=H, fps=FPS, audio=audio_path)
                    manifest["outputs"]["mp4"] = str(out)
                    manifest["frames"] = n
                    manifest["engine"] = f"ffmpeg ({how})"
                    primary_done = True
                except Exception as exc:  # noqa: BLE001
                    manifest["fallbacks"].append({"stage": "mp4", "reason": str(exc)[:300]})
            elif fmt == "gif" and not primary_done:
                if not pil:
                    manifest["fallbacks"].append({"stage": "gif", "reason": "Pillow missing"})
                    continue
                out = out_dir / f"{BASENAME}.gif"
                try:
                    # a long film gets a thinner GIF: the format has no inter-frame
                    # compression, so size grows linearly with frames
                    gif_fps = GIF_FPS if film_ms <= 150_000 else 3
                    gif_w = GIF_WIDTH if film_ms <= 150_000 else 560
                    step = max(1, int(round(FPS / gif_fps)))
                    n = encode_gif(iter_frames(scenes, opts, title=title, width=W, height=H,
                                               fps=FPS, every=step), out,
                                   gif_fps=max(1, FPS // step), width=gif_w)
                    manifest["outputs"]["gif"] = str(out)
                    manifest["frames"] = n
                    manifest["engine"] = "Pillow animated GIF"
                    primary_done = True
                except Exception as exc:  # noqa: BLE001
                    manifest["fallbacks"].append({"stage": "gif", "reason": str(exc)[:300]})
            elif fmt == "html":
                from . import deck
                out = out_dir / f"{BASENAME}.html"
                audio_segments = {s["index"]: Path(s["path"]).read_bytes()
                                  for s in nar["segments"]}
                deck.build_deck(out, title=title, scenes=scenes, keyframes=keyframes,
                                narration=audio_segments, markers=markers, manifest=manifest)
                manifest["outputs"]["html"] = str(out)
                if not primary_done:
                    manifest["engine"] = manifest["engine"] or "HTML deck"
        if not primary_done and "html" not in wanted:
            manifest["fallbacks"].append({"stage": "output", "reason":
                                          "no video produced and html not requested"})
    except Exception as exc:  # noqa: BLE001 -- a build failure is reported, never raised
        import traceback
        manifest["errors"].append(f"{type(exc).__name__}: {exc}")
        log(run_dir, "build failed:\n" + traceback.format_exc())

    manifest["elapsed_seconds"] = round(time.monotonic() - started, 2)
    (out_dir / MANIFEST).write_text(json.dumps(manifest, indent=2, default=str), encoding="utf-8")
    if reason in ("run_completed", "run_aborted"):
        DemoContext.release_pointer(run_dir)

    prefix = run_dir.parent.parent.name
    say("")
    say(f"demo           : {'compiled' if manifest['outputs'] and not manifest['errors'] else 'INCOMPLETE'}"
        f"  ({manifest['scenes']} scenes, {manifest['duration_ms'] / 1000:.1f}s, "
        f"{manifest['frames']} frames, {manifest['elapsed_seconds']}s to build)")
    for key, label in (("mp4", "video"), ("gif", "animation"), ("html", "deck"),
                       ("narration_wav", "narration")):
        if key in manifest["outputs"]:
            p = Path(manifest["outputs"][key])
            try:
                rel = p.resolve().relative_to(run_dir.parent.parent.resolve())
                shown = f"{prefix}/{rel.as_posix()}"
            except ValueError:
                shown = str(p)
            say(f"  {label:<13}: {shown}  ({p.stat().st_size / 1024:.0f} KB)")
    if manifest["narration"].get("engine"):
        say(f"  voice        : {manifest['narration']['engine']} "
            f"({manifest['narration']['segments']} segments, "
            f"{manifest.get('narration_density', 'full')} density)")
    for fb in manifest["fallbacks"]:
        say(f"  fallback     : {fb['stage']} -> {fb['reason']}")
    for err in manifest["errors"]:
        say(f"  error        : {err}")
    try:
        mrel = (out_dir / MANIFEST).resolve().relative_to(run_dir.parent.parent.resolve())
        say(f"  manifest     : {prefix}/{mrel.as_posix()}")
    except ValueError:
        say(f"  manifest     : {out_dir / MANIFEST}")
    return manifest
