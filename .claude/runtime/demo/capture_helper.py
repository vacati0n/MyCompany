"""The detached process that owns one screen recording.

A recording has to outlive the command that starts it: the run being filmed happens in the
window afterwards, driven by whoever is driving it, and only then is the recording stopped.
So `capture.start_detached` spawns this helper, which holds the ffmpeg process or the OBS
connection, reports itself through the capture manifest, and waits for a `stop.request` file
to appear beside it.

Run directly only for debugging:

    python -m demo.capture_helper --run-dir <framework-dir>/runs/<run-id>

where `<framework-dir>` is this framework directory's real name, `.claude` in the
framework's own checkout.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
import traceback
from pathlib import Path

if __package__ in (None, ""):                    # started as a script by `start_detached`
    sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
    from demo import DEMO_DIR                                       # noqa: E402
    from demo.capture import (CAPTURE_DIR, MANIFEST, CaptureError,  # noqa: E402
                              CaptureSession, DEFAULT_EXE, DEFAULT_FPS, DEFAULT_TITLE,
                              foreground as capture_foreground,
                              is_foreground as capture_is_foreground)
else:
    from . import DEMO_DIR
    from .capture import (CAPTURE_DIR, MANIFEST, CaptureError, CaptureSession,
                          DEFAULT_EXE, DEFAULT_FPS, DEFAULT_TITLE,
                          foreground as capture_foreground,
                          is_foreground as capture_is_foreground)

POLL_SECONDS = 0.25
MAX_SECONDS = 4 * 60 * 60
OCCLUSION_GAP_S = 1.0        # spans closer than this are one interruption, not two
RAISE_EVERY_S = 2.0          # how often to try taking the window back when it is covered


def _merge(spans: list) -> list:
    """Merge spans that all but touch, and drop ones too brief to have filmed anything."""
    out: list = []
    for start, end in sorted(spans):
        if end - start < 0.4:
            continue
        if out and start - out[-1][1] <= OCCLUSION_GAP_S:
            out[-1][1] = max(out[-1][1], end)
        else:
            out.append([start, end])
    return out


def _fail(run_dir: Path, message: str):
    d = run_dir / DEMO_DIR / CAPTURE_DIR
    d.mkdir(parents=True, exist_ok=True)
    path = d / MANIFEST
    data = {}
    if path.exists():
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except ValueError:
            data = {}
    data.update(state="failed", error=message)
    path.write_text(json.dumps(data, indent=2), encoding="utf-8")
    print(message, file=sys.stderr)


def main() -> int:
    ap = argparse.ArgumentParser(description="demo capture helper")
    ap.add_argument("--run-dir", required=True)
    ap.add_argument("--fps", type=int, default=DEFAULT_FPS)
    ap.add_argument("--backend", default="auto", choices=["auto", "obs", "ffmpeg"])
    ap.add_argument("--window-title", default=DEFAULT_TITLE)
    ap.add_argument("--window-exe", default=DEFAULT_EXE)
    ap.add_argument("--max-seconds", type=float, default=MAX_SECONDS)
    args = ap.parse_args()

    run_dir = Path(args.run_dir).resolve()
    stop_file = run_dir / DEMO_DIR / CAPTURE_DIR / "stop.request"
    if stop_file.exists():
        stop_file.unlink()

    try:
        session = CaptureSession(run_dir, fps=args.fps, backend=args.backend,
                                 title=args.window_title, exe=args.window_exe)
        data = session.start()
    except (CaptureError, Exception) as exc:     # noqa: BLE001 -- the manifest is the report
        _fail(run_dir, f"{type(exc).__name__}: {exc}\n{traceback.format_exc()}")
        return 2

    print(f"recording: backend={data['backend']} t0={data['started_at']} "
          f"rect={data['rect']} -> {data['video']}", flush=True)

    # While the ffmpeg backend is recording it captures the *screen* inside the window's
    # rectangle, so anything that comes to the front -- or the bare desktop, if the window is
    # minimised -- is filmed instead. That is a privacy problem, not a cosmetic one: a demo
    # is meant to be published, and whatever else was on that screen would go with it. So the
    # helper watches which window is actually in front and records every span during which
    # the target was not, relative to the recording's own clock. `editor.py` then refuses to
    # publish those spans. OBS window capture is immune and reports nothing here.
    deadline = time.monotonic() + args.max_seconds
    watch = session.backend.name.startswith("ffmpeg")
    occluded, away_since, last_raise = [], None, -RAISE_EVERY_S
    try:
        while time.monotonic() < deadline:
            if stop_file.exists():
                break
            if watch:
                now = time.time() - session.t0
                if capture_is_foreground(session.window):
                    if away_since is not None:
                        occluded.append([round(away_since, 2), round(now, 2)])
                        away_since = None
                else:
                    if away_since is None:
                        away_since = now
                    # Try to take the window back rather than film the desktop for the rest
                    # of the take. A recording the operator deliberately started is allowed
                    # to keep its subject in front; the interruption is recorded either way.
                    if now - last_raise > RAISE_EVERY_S:
                        last_raise = now
                        try:
                            capture_foreground(session.window, settle=0.1)
                        except Exception:  # noqa: BLE001
                            pass
            time.sleep(POLL_SECONDS)
        else:
            print(f"stopping: reached the {args.max_seconds}s ceiling", flush=True)
        if away_since is not None:
            occluded.append([round(away_since, 2), round(time.time() - session.t0, 2)])
        session.occluded = _merge(occluded)
        data = session.stop()
        if session.occluded:
            hidden = sum(b - a for a, b in session.occluded)
            print(f"occluded: the target window was not in front for {hidden:.1f}s across "
                  f"{len(session.occluded)} span(s); that footage will not be published",
                  flush=True)
        if stop_file.exists():
            stop_file.unlink()
        print(f"recorded: {data['duration_seconds']}s -> {data['video']}", flush=True)
        return 0
    except Exception as exc:                     # noqa: BLE001
        _fail(run_dir, f"{type(exc).__name__}: {exc}\n{traceback.format_exc()}")
        return 2


if __name__ == "__main__":
    sys.exit(main())
