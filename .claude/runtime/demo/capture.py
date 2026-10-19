"""Screen capture of the Claude Desktop window, and the clock that makes it editable.

The synthetic dashboard the renderer draws is a diagram of a run. This module records the
real thing: the actual application window, while the run happens in it. What turns raw
screen capture into an editable demo is not the pixels but the **time base** -- every
execution marker carries a wall-clock timestamp, so once the recording's own start instant
is known, every marker has a frame number, and `editor.py` can cut, condense, and caption
the footage against what the runtime was actually doing at that moment.

Two backends, picked in this order:

    obs       OBS Studio driven over obs-websocket. True window capture (Windows Graphics
              Capture), so another window in front of Claude Desktop does not appear in the
              recording, and OBS's own encoder does the work. Requires the WebSocket server
              to be enabled once, in OBS: Tools -> WebSocket Server Settings -> Enable.
    ffmpeg    ffmpeg's gdigrab, capturing exactly the target window's rectangle. No setup at
              all, but it records what is *on screen* in that rectangle, so the window is
              raised first and occlusion is checked before and after.

Window-level gdigrab (`-i title=...`) is deliberately not used: a GPU-composited Electron
window returns black frames through BitBlt, which is why the rectangle is captured instead.

Only the target window's rectangle is ever recorded. The session refuses to start when the
target is not the foreground window, so an unrelated window is not captured by accident.
"""

from __future__ import annotations

import ctypes
import json
import os
import shutil
import subprocess
import sys
import time
from ctypes import wintypes
from datetime import datetime, timezone
from pathlib import Path

from . import DEMO_DIR, log
from .obs_ws import OBSClient, OBSError, probe as obs_probe

CAPTURE_DIR = "capture"
MANIFEST = "capture-manifest.json"
DEFAULT_FPS = 30
DEFAULT_TITLE = "Claude"
DEFAULT_EXE = "Claude.exe"

# OBS provisioning names. Kept separate from anything the user owns: the demo never edits
# the operator's own scene collection or profile.
OBS_COLLECTION = "Framework Demo Capture"
OBS_PROFILE = "Framework Demo Capture"
OBS_SCENE = "Claude Desktop"
OBS_SOURCE = "Claude Desktop Window"
WGC_METHOD = 2          # window_capture: 0 auto, 1 BitBlt, 2 Windows Graphics Capture


def now_ms() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def _iso(ts: float) -> str:
    return datetime.fromtimestamp(ts, timezone.utc).isoformat(
        timespec="milliseconds").replace("+00:00", "Z")


# --------------------------------------------------------------------- window discovery

if sys.platform == "win32":
    _user32 = ctypes.WinDLL("user32", use_last_error=True)
    _kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    _dwmapi = ctypes.WinDLL("dwmapi", use_last_error=True)
    _ENUM_PROC = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
    # Without this the window rectangles below come back in the virtualised coordinates
    # Windows hands a DPI-unaware process, while gdigrab captures in physical pixels. On a
    # scaled display the two disagree and the recording is offset from the window.
    try:
        ctypes.windll.shcore.SetProcessDpiAwareness(2)       # per-monitor aware
    except (AttributeError, OSError):
        try:
            _user32.SetProcessDPIAware()
        except (AttributeError, OSError):
            pass
else:                                            # pragma: no cover -- capture is Windows-only
    _user32 = _kernel32 = _dwmapi = None
    _ENUM_PROC = None

DWMWA_EXTENDED_FRAME_BOUNDS = 9
PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
SW_RESTORE = 9


class CaptureError(RuntimeError):
    """Capture could not be set up or run."""


def _require_windows():
    if sys.platform != "win32":
        raise CaptureError("window capture is implemented for Windows only; record with OBS "
                           "by hand on this platform and pass --capture <file> to the editor")


def _process_name(pid: int) -> str:
    h = _kernel32.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, False, pid)
    if not h:
        return ""
    try:
        size = wintypes.DWORD(260)
        buf = ctypes.create_unicode_buffer(size.value)
        if _kernel32.QueryFullProcessImageNameW(h, 0, buf, ctypes.byref(size)):
            return Path(buf.value).name
        return ""
    finally:
        _kernel32.CloseHandle(h)


def _window_title(hwnd) -> str:
    n = _user32.GetWindowTextLengthW(hwnd)
    if not n:
        return ""
    buf = ctypes.create_unicode_buffer(n + 1)
    _user32.GetWindowTextW(hwnd, buf, n + 1)
    return buf.value


def _class_name(hwnd) -> str:
    buf = ctypes.create_unicode_buffer(256)
    _user32.GetClassNameW(hwnd, buf, 256)
    return buf.value


def window_rect(hwnd) -> tuple:
    """The window's visible frame (x, y, w, h), excluding the drop shadow.

    `GetWindowRect` includes the invisible resize border, which on Windows 10 and 11 is
    about eleven pixels of desktop on every side -- recording it would put a strip of
    whatever is behind the window into the demo.
    """
    r = wintypes.RECT()
    hr = _dwmapi.DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS,
                                       ctypes.byref(r), ctypes.sizeof(r))
    if hr != 0:
        _user32.GetWindowRect(hwnd, ctypes.byref(r))
    return r.left, r.top, r.right - r.left, r.bottom - r.top


def find_window(title: str = DEFAULT_TITLE, exe: str = DEFAULT_EXE) -> dict:
    """The visible top-level window of `exe` whose title contains `title`.

    Electron applications run a process tree; only one process owns a titled top-level
    window, so the search is over windows rather than over processes.
    """
    _require_windows()
    found = []

    def visit(hwnd, _lparam):
        if not _user32.IsWindowVisible(hwnd):
            return True
        t = _window_title(hwnd)
        if not t:
            return True
        pid = wintypes.DWORD()
        _user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
        name = _process_name(pid.value)
        if exe and name.lower() != exe.lower():
            return True
        if title and title.lower() not in t.lower():
            return True
        x, y, w, h = window_rect(hwnd)
        if w < 200 or h < 200:
            return True
        found.append({"hwnd": int(hwnd), "title": t, "class": _class_name(hwnd),
                      "exe": name, "pid": pid.value, "rect": [x, y, w, h]})
        return True

    _user32.EnumWindows(_ENUM_PROC(visit), 0)
    if not found:
        raise CaptureError(
            f"no visible window of {exe!r} with {title!r} in its title. Open Claude Desktop, "
            f"or pass --window-title / --window-exe to target a different application")
    found.sort(key=lambda w: w["rect"][2] * w["rect"][3], reverse=True)
    return found[0]


def foreground(win: dict, settle: float = 0.6) -> bool:
    """Raise the target window and report whether it actually came to the front."""
    hwnd = win["hwnd"]
    # Only a minimised window is restored. `SW_RESTORE` on a *maximised* window un-maximises
    # it, which would resize the operator's window and shrink the frame being filmed.
    if _user32.IsIconic(hwnd):
        _user32.ShowWindow(hwnd, SW_RESTORE)
    _user32.SetForegroundWindow(hwnd)
    # SetForegroundWindow is refused when the calling process is not itself foreground;
    # attaching to the foreground thread's input queue lifts that restriction.
    if _user32.GetForegroundWindow() != hwnd:
        cur = _user32.GetForegroundWindow()
        tid_cur = _user32.GetWindowThreadProcessId(cur, None)
        tid_me = _kernel32.GetCurrentThreadId()
        _user32.AttachThreadInput(tid_cur, tid_me, True)
        _user32.BringWindowToTop(hwnd)
        _user32.SetForegroundWindow(hwnd)
        _user32.AttachThreadInput(tid_cur, tid_me, False)
    time.sleep(settle)
    return _user32.GetForegroundWindow() == hwnd


def is_foreground(win: dict) -> bool:
    return bool(_user32) and _user32.GetForegroundWindow() == win["hwnd"]


# --------------------------------------------------------------------- ffmpeg discovery


def find_ffmpeg() -> tuple:
    from .video_builder import find_ffmpeg as _find
    return _find()


# --------------------------------------------------------------------- backends


class _Backend:
    name = "?"

    def start(self, session: "CaptureSession"):
        raise NotImplementedError

    def stop(self, session: "CaptureSession") -> Path:
        raise NotImplementedError


class FFmpegBackend(_Backend):
    """gdigrab over the target window's rectangle.

    Records the desktop inside that rectangle, so the window is raised first.

    Two details make the file survive being stopped. It is written as Matroska rather than
    MP4: MP4 keeps its index in a `moov` atom written only at a clean exit, so a recording
    interrupted for any reason is unplayable, while Matroska stays readable up to the last
    frame written. And ffmpeg is asked to stop with a console break rather than by writing
    `q` to its stdin, because on Windows ffmpeg polls the console for that key and never
    sees it arrive down a pipe -- which is exactly how the first recording made here ended
    up with no index at all.
    """

    name = "ffmpeg-gdigrab"

    def __init__(self, ffmpeg: str):
        self.ffmpeg = ffmpeg
        self.proc = None

    def start(self, session: "CaptureSession"):
        x, y, w, h = session.rect
        w -= w % 2
        h -= h % 2
        session.rect = [x, y, w, h]
        session.video_path = session.video_path.with_suffix(".mkv")
        out = session.video_path
        out.parent.mkdir(parents=True, exist_ok=True)
        cmd = [
            self.ffmpeg, "-hide_banner", "-loglevel", "error", "-y",
            "-f", "gdigrab", "-framerate", str(session.fps),
            "-draw_mouse", "1",
            "-offset_x", str(x), "-offset_y", str(y), "-video_size", f"{w}x{h}",
            "-i", "desktop",
            "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p",
            "-progress", "pipe:1", "-stats_period", "0.1",
            str(out),
        ]
        session.command = cmd
        flags = subprocess.CREATE_NEW_PROCESS_GROUP if sys.platform == "win32" else 0
        self.proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                     stderr=subprocess.PIPE, text=True, bufsize=1,
                                     creationflags=flags)
        # t0 = the wall clock at which the recording's own timeline is zero. ffmpeg's
        # progress stream reports how far in it is; sampling both together removes the
        # process start-up cost from the mapping instead of guessing at it.
        deadline = time.monotonic() + 20
        while time.monotonic() < deadline:
            line = self.proc.stdout.readline()
            if not line:
                if self.proc.poll() is not None:
                    err = (self.proc.stderr.read() or "")[-400:]
                    raise CaptureError(f"ffmpeg exited before capturing: {err}")
                continue
            if line.startswith("out_time_us="):
                try:
                    elapsed = int(line.split("=", 1)[1].strip()) / 1_000_000
                except ValueError:
                    continue
                session.t0 = time.time() - elapsed
                return
        raise CaptureError("ffmpeg produced no progress output within 20s")

    def stop(self, session: "CaptureSession") -> Path:
        if self.proc is None:
            raise CaptureError("capture was never started")
        if sys.platform == "win32":
            import signal
            try:
                os.kill(self.proc.pid, signal.CTRL_BREAK_EVENT)
            except OSError:
                pass
        else:
            self.proc.terminate()
        try:
            self.proc.wait(timeout=30)
        except subprocess.TimeoutExpired:
            self.proc.terminate()
            try:
                self.proc.wait(timeout=10)
            except subprocess.TimeoutExpired:
                self.proc.kill()
        return session.video_path


class OBSBackend(_Backend):
    """OBS Studio over obs-websocket, capturing the window itself rather than the screen."""

    name = "obs-websocket"

    def __init__(self):
        self.obs = None

    # -- provisioning ---------------------------------------------------------

    def _provision(self, obs: OBSClient, session: "CaptureSession"):
        """Create the demo's own scene collection, profile, scene, and window source.

        Everything is namespaced under `Framework Demo Capture`, and every call is idempotent, so
        the operator's own OBS setup is neither read nor modified.
        """
        collections = (obs.request("GetSceneCollectionList") or {}).get("sceneCollections", [])
        if OBS_COLLECTION not in collections:
            obs.request("CreateSceneCollection", {"sceneCollectionName": OBS_COLLECTION})
            time.sleep(1.5)                      # OBS reloads the whole scene graph
        elif (obs.request("GetSceneCollectionList") or {}).get(
                "currentSceneCollectionName") != OBS_COLLECTION:
            obs.request("SetCurrentSceneCollection", {"sceneCollectionName": OBS_COLLECTION})
            time.sleep(1.5)

        profiles = (obs.request("GetProfileList") or {}).get("profiles", [])
        if OBS_PROFILE not in profiles:
            obs.request("CreateProfile", {"profileName": OBS_PROFILE})
            time.sleep(1.0)
        elif (obs.request("GetProfileList") or {}).get("currentProfileName") != OBS_PROFILE:
            obs.request("SetCurrentProfile", {"profileName": OBS_PROFILE})
            time.sleep(1.0)

        x, y, w, h = session.rect
        w -= w % 2
        h -= h % 2
        session.rect = [x, y, w, h]
        for category, name, value in (
            ("Video", "BaseCX", str(w)), ("Video", "BaseCY", str(h)),
            ("Video", "OutputCX", str(w)), ("Video", "OutputCY", str(h)),
            ("Video", "FPSCommon", str(session.fps)),
            ("Output", "Mode", "Simple"),
            ("SimpleOutput", "RecFormat2", "mp4"),
            ("SimpleOutput", "RecQuality", "HQ"),
            ("SimpleOutput", "FilePath", str(session.dir)),
            ("Output", "FilenameFormatting", session.stem),
        ):
            obs.try_request("SetProfileParameter", {"parameterCategory": category,
                                                    "parameterName": name,
                                                    "parameterValue": value})

        scenes = [s["sceneName"] for s in (obs.request("GetSceneList") or {}).get("scenes", [])]
        if OBS_SCENE not in scenes:
            obs.request("CreateScene", {"sceneName": OBS_SCENE})
        obs.request("SetCurrentProgramScene", {"sceneName": OBS_SCENE})

        win = session.window
        # OBS identifies a window as title:class:executable, with ':' escaped as '#3A'.
        spec = ":".join(part.replace(":", "#3A") for part in
                        (win["title"], win["class"], win["exe"]))
        settings = {"window": spec, "method": WGC_METHOD, "priority": 2,
                    "cursor": True, "client_area": True, "capture_audio": False}
        inputs = [i["inputName"] for i in (obs.request("GetInputList") or {}).get("inputs", [])]
        if OBS_SOURCE in inputs:
            obs.request("SetInputSettings", {"inputName": OBS_SOURCE, "overlay": True,
                                             "inputSettings": settings})
            items = (obs.request("GetSceneItemList", {"sceneName": OBS_SCENE})
                     or {}).get("sceneItems", [])
            if not any(i["sourceName"] == OBS_SOURCE for i in items):
                obs.request("CreateSceneItem", {"sceneName": OBS_SCENE,
                                                "sourceName": OBS_SOURCE})
        else:
            obs.request("CreateInput", {"sceneName": OBS_SCENE, "inputName": OBS_SOURCE,
                                        "inputKind": "window_capture",
                                        "inputSettings": settings})
        session.obs_source = spec

    # -- lifecycle ------------------------------------------------------------

    def start(self, session: "CaptureSession"):
        self.obs = OBSClient.connect()
        v = self.obs.request("GetVersion")
        session.backend_version = (f"OBS {v.get('obsVersion')} / websocket "
                                   f"{v.get('obsWebSocketVersion')}")
        self._provision(self.obs, session)
        status = self.obs.request("GetRecordStatus")
        if status.get("outputActive"):
            raise CaptureError("OBS is already recording; stop that recording first")
        self.obs.request("StartRecord")
        deadline = time.monotonic() + 15
        while time.monotonic() < deadline:
            st = self.obs.request("GetRecordStatus")
            if st.get("outputActive") and (st.get("outputDuration") or 0) > 0:
                session.t0 = time.time() - (st["outputDuration"] / 1000.0)
                return
            time.sleep(0.1)
        raise CaptureError("OBS did not start recording within 15s")

    def stop(self, session: "CaptureSession") -> Path:
        if self.obs is None:
            raise CaptureError("capture was never started")
        out = self.obs.request("StopRecord", timeout=60)
        path = out.get("outputPath")
        self.obs.close()
        if not path:
            raise CaptureError("OBS stopped recording but reported no output path")
        return Path(path)


# --------------------------------------------------------------------- the session


class CaptureSession:
    """One screen recording, with the clock mapping that makes it editable."""

    def __init__(self, run_dir: Path, *, fps: int = DEFAULT_FPS, backend: str = "auto",
                 title: str = DEFAULT_TITLE, exe: str = DEFAULT_EXE):
        self.run_dir = Path(run_dir)
        self.dir = self.run_dir / DEMO_DIR / CAPTURE_DIR
        self.fps = int(fps)
        self.stem = "session"
        self.requested_backend = backend
        self.window = find_window(title, exe)
        self.rect = list(self.window["rect"])
        self.t0 = None
        self.command = None
        self.backend_version = None
        self.obs_source = None
        self.warnings: list = []
        # Spans, in this recording's own seconds, during which the target window was not the
        # one on screen. Filled by the capture helper while it waits; the editor refuses to
        # publish any footage inside them.
        self.occluded: list = []
        self.backend = None
        self.video_path = self.dir / f"{self.stem}.mp4"

    # -- selection ------------------------------------------------------------

    def _select_backend(self) -> _Backend:
        want = self.requested_backend
        obs_state = obs_probe()
        if want in ("auto", "obs"):
            if obs_state["reachable"]:
                return OBSBackend()
            if want == "obs":
                raise CaptureError(f"OBS backend requested but unavailable: "
                                   f"{obs_state['reason']}")
            self.warnings.append({"stage": "backend", "reason":
                                  f"OBS not used: {obs_state['reason']}"})
        ffmpeg, how = find_ffmpeg()
        if not ffmpeg:
            raise CaptureError(f"no capture backend: OBS is unavailable and {how}")
        self.ffmpeg_how = how
        return FFmpegBackend(ffmpeg)

    # -- lifecycle ------------------------------------------------------------

    def start(self) -> dict:
        self.dir.mkdir(parents=True, exist_ok=True)
        self.backend = self._select_backend()
        raised = foreground(self.window)
        # The rectangle is re-read after raising: a restored-from-minimised window moves.
        self.window["rect"] = list(window_rect(self.window["hwnd"]))
        self.rect = list(self.window["rect"])
        if not raised:
            if isinstance(self.backend, FFmpegBackend):
                raise CaptureError(
                    f"{self.window['title']!r} could not be brought to the front, and the "
                    f"ffmpeg backend records whatever is on screen in its rectangle. Click "
                    f"the window and start again, or enable the OBS WebSocket server to "
                    f"capture the window itself")
            self.warnings.append({"stage": "foreground", "reason":
                                  "the target window was not raised; OBS window capture is "
                                  "unaffected by that"})
        self.backend.start(self)
        self.started_at = _iso(self.t0)
        self.write_manifest(state="recording")
        return self.manifest()

    def stop(self) -> dict:
        stopped = time.time()
        path = self.backend.stop(self)
        if path != self.video_path and path.exists():
            self.dir.mkdir(parents=True, exist_ok=True)
            target = self.dir / f"{self.stem}{path.suffix}"
            if path.resolve() != target.resolve():
                shutil.move(str(path), str(target))
            self.video_path = target
        self.stopped_at = _iso(stopped)
        self.duration = round(stopped - self.t0, 3)
        if self.occluded:
            hidden = sum(b - a for a, b in self.occluded)
            self.warnings.append({
                "stage": "occlusion",
                "reason": f"the target window was not the one on screen for "
                          f"{hidden:.1f}s of {self.duration:.0f}s, across "
                          f"{len(self.occluded)} span(s). That footage shows whatever was "
                          f"in front instead and is never published"})
        return self.write_manifest(state="recorded")

    # -- persistence ----------------------------------------------------------

    def manifest(self) -> dict:
        return {
            "schema": "framework.runtime/demo-capture-manifest.v1",
            "run_id": self.run_dir.name,
            "backend": self.backend.name if self.backend else None,
            "backend_version": self.backend_version,
            "obs_source": self.obs_source,
            "video": str(self.video_path),
            "fps": self.fps,
            "rect": self.rect,
            "window": {k: self.window[k] for k in ("title", "class", "exe", "pid")},
            # The instant the recording's own timeline reads zero. Every marker's video time
            # is `marker.timestamp - started_at`, which is what the editor cuts against.
            "started_at": getattr(self, "started_at", None),
            "stopped_at": getattr(self, "stopped_at", None),
            "duration_seconds": getattr(self, "duration", None),
            "command": self.command,
            "occluded": self.occluded,
            "warnings": self.warnings,
            "written_at": now_ms(),
        }

    def write_manifest(self, state: str) -> dict:
        data = self.manifest()
        data["state"] = state
        self.dir.mkdir(parents=True, exist_ok=True)
        (self.dir / MANIFEST).write_text(json.dumps(data, indent=2), encoding="utf-8")
        return data


def manifest_path(run_dir: Path) -> Path:
    return Path(run_dir) / DEMO_DIR / CAPTURE_DIR / MANIFEST


def load_manifest(run_dir: Path) -> dict | None:
    p = manifest_path(run_dir)
    if not p.exists():
        return None
    try:
        return json.loads(p.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None


# --------------------------------------------------------------------- process control
#
# A recording outlives the command that starts it: the run being filmed happens in the
# window, driven by whoever is driving it, and only then is the recording stopped. The
# session is therefore detached into a helper process whose pid and state live in the
# manifest, rather than held open by the runtime.


def start_detached(run_dir: Path, *, fps: int = DEFAULT_FPS, backend: str = "auto",
                   title: str = DEFAULT_TITLE, exe: str = DEFAULT_EXE) -> dict:
    """Start a recording that keeps running after this command returns."""
    run_dir = Path(run_dir)
    existing = load_manifest(run_dir)
    if existing and existing.get("state") == "recording":
        raise CaptureError(
            f"a recording is already in progress for {run_dir.name} (started "
            f"{existing.get('started_at')}); stop it before starting another")
    helper = Path(__file__).resolve().parent / "capture_helper.py"
    args = [sys.executable, str(helper), "--run-dir", str(run_dir), "--fps", str(fps),
            "--backend", backend, "--window-title", title, "--window-exe", exe]
    flags = 0
    if sys.platform == "win32":
        # A hidden console rather than no console at all: stopping ffmpeg cleanly means
        # sending it a console break, and a process detached from every console cannot
        # generate one. `CREATE_NO_WINDOW` gives the helper a console nothing ever sees.
        flags = (subprocess.CREATE_NEW_PROCESS_GROUP
                 | getattr(subprocess, "CREATE_NO_WINDOW", 0x08000000))
    log_path = run_dir / DEMO_DIR / CAPTURE_DIR / "capture.log"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    fh = log_path.open("a", encoding="utf-8")
    proc = subprocess.Popen(args, stdout=fh, stderr=subprocess.STDOUT, stdin=subprocess.DEVNULL,
                            creationflags=flags)
    deadline = time.monotonic() + 45
    while time.monotonic() < deadline:
        data = load_manifest(run_dir)
        if data and data.get("state") == "recording":
            data["helper_pid"] = proc.pid
            (run_dir / DEMO_DIR / CAPTURE_DIR / MANIFEST).write_text(
                json.dumps(data, indent=2), encoding="utf-8")
            return data
        if data and data.get("state") == "failed":
            raise CaptureError(data.get("error") or "capture helper failed")
        if proc.poll() is not None:
            raise CaptureError(f"capture helper exited: "
                               f"{log_path.read_text(encoding='utf-8')[-500:]}")
        time.sleep(0.3)
    raise CaptureError("capture helper did not report a started recording within 45s")


def stop_detached(run_dir: Path, timeout: float = 120) -> dict:
    """Ask the running helper to stop, and wait for the finalised file."""
    run_dir = Path(run_dir)
    data = load_manifest(run_dir)
    if not data:
        raise CaptureError(f"no capture manifest for {run_dir.name}")
    if data.get("state") != "recording":
        return data
    (run_dir / DEMO_DIR / CAPTURE_DIR / "stop.request").write_text(now_ms(), encoding="utf-8")
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        data = load_manifest(run_dir)
        if data and data.get("state") in ("recorded", "failed"):
            if data.get("state") == "failed":
                raise CaptureError(data.get("error") or "capture failed")
            return data
        time.sleep(0.3)
    raise CaptureError(f"the capture helper did not finalise within {timeout}s")


def status(run_dir: Path) -> dict:
    data = load_manifest(run_dir) or {"state": "none"}
    data.setdefault("obs", obs_probe())
    return data
