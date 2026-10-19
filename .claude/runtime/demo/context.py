"""DemoContext -- the per-run switch that wraps the standard orchestration pipeline.

The context is a small JSON document at `runs/<run>/demo/demo-context.json`. Its presence
is what turns recording on; its content is the configuration the recorder, the renderer,
and the video builder read. Nothing about the run's own state lives here: the run is the
authority on itself, the context only says how the run is to be shown.

Options (every one has a default and every one can be overridden per build):

    title            overlay title on the opening card; defaults to the feature request's
                     first heading, else the run id
    target_seconds   the presentation length the timeline compresses towards (narration
                     may lengthen it, compression never shortens a narrated scene)
    fps              frame rate of the MP4; the GIF fallback derives its own
    width, height    frame geometry; 1280x720 renders crisply in a slide deck
    narration        "auto" (best available engine), "off", or an engine name
    voice            engine-specific voice hint, e.g. "Microsoft Zira Desktop"
    formats          preference order the builder honours: ["mp4", "gif", "html"]
"""

from __future__ import annotations

import json
import re
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path

from . import ACTIVE_POINTER, CONTEXT_FILE, DEMO_DIR, VERSION

DEFAULT_OPTIONS = {
    "title": None,
    "target_seconds": 90,
    "fps": 10,
    "width": 1280,
    "height": 720,
    "narration": "auto",
    "voice": None,
    "formats": ["mp4", "gif", "html"],
    "theme": "midnight",
    # How the film is paced, and how much it explains. `relaxed` plus `explain` is the
    # default because the usual viewer of one of these is seeing the framework for the first
    # time: every beat is held long enough to take in, waits are condensed far less, and the
    # narration explains what is happening in plain language rather than naming it. In that
    # mode the narration decides the length of the film, so a demo runs to minutes, not to a
    # target. `brisk` with `explain` off is the short highlight reel.
    "pace": "relaxed",
    "explain": True,
    "speech_rate": -1,
    # Every spoken line is shown on screen as it is said. A demo that can only be followed
    # with the sound on is a demo half the room cannot follow.
    "captions": True,
}

SCHEMA = "framework.runtime/demo-context.v1"


def _now_ms() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def _title_from_inputs(run_dir: Path) -> str | None:
    """The first Markdown heading of the first supplied input, when the request is on disk."""
    req = run_dir / "execution-request.json"
    if not req.exists():
        return None
    try:
        data = json.loads(req.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None
    claude_root = run_dir.parent.parent
    for inp in data.get("inputs") or []:
        ref = inp.get("reference")
        if not ref:
            continue
        p = claude_root / ref
        if not p.exists():
            continue
        for line in p.read_text(encoding="utf-8", errors="replace").splitlines():
            m = re.match(r"^#\s+(.+?)\s*$", line)
            if m:
                return m.group(1)[:120]
    return None


@dataclass
class DemoContext:
    run_id: str
    enabled_at: str
    requester: str
    options: dict = field(default_factory=lambda: dict(DEFAULT_OPTIONS))
    schema: str = SCHEMA
    version: str = VERSION

    # ---------------------------------------------------------------- persistence

    @classmethod
    def path(cls, run_dir: Path) -> Path:
        return run_dir / DEMO_DIR / CONTEXT_FILE

    @classmethod
    def load(cls, run_dir: Path) -> "DemoContext | None":
        p = cls.path(run_dir)
        if not p.exists():
            return None
        data = json.loads(p.read_text(encoding="utf-8"))
        opts = dict(DEFAULT_OPTIONS)
        opts.update(data.get("options") or {})
        return cls(run_id=data["run_id"], enabled_at=data["enabled_at"],
                   requester=data.get("requester", "operator"), options=opts,
                   schema=data.get("schema", SCHEMA), version=data.get("version", VERSION))

    @classmethod
    def enable(cls, run_dir: Path, *, requester: str = "operator",
               options: dict | None = None) -> "DemoContext":
        """Create the context, or merge new options into an existing one. Idempotent.

        Enabling is deliberately the last write: the recorder treats the context file as the
        switch, so the directory and the pointer exist before the switch is thrown.
        """
        existing = cls.load(run_dir)
        merged = dict(existing.options if existing else DEFAULT_OPTIONS)
        for k, v in (options or {}).items():
            if v is not None:
                merged[k] = v
        if not merged.get("title"):
            merged["title"] = _title_from_inputs(run_dir) or run_dir.name
        ctx = existing or cls(run_id=run_dir.name, enabled_at=_now_ms(), requester=requester)
        ctx.options = merged
        (run_dir / DEMO_DIR).mkdir(parents=True, exist_ok=True)
        # The active-run pointer lets a host tool hook find the recording run without an
        # argument. One recording run per runs root at a time; the newest wins.
        pointer = run_dir.parent / ACTIVE_POINTER
        pointer.write_text(run_dir.name, encoding="utf-8")
        cls.path(run_dir).write_text(json.dumps(ctx.to_dict(), indent=2), encoding="utf-8")
        return ctx

    @classmethod
    def release_pointer(cls, run_dir: Path) -> None:
        """Drop the active-run pointer when it names this run. Called after the final build."""
        pointer = run_dir.parent / ACTIVE_POINTER
        try:
            if pointer.exists() and pointer.read_text(encoding="utf-8").strip() == run_dir.name:
                pointer.unlink()
        except OSError:
            pass

    def to_dict(self) -> dict:
        return asdict(self)

    # ---------------------------------------------------------------- convenience

    @property
    def dir(self) -> str:
        return DEMO_DIR

    def opt(self, key: str, default=None):
        v = self.options.get(key)
        return default if v is None else v
