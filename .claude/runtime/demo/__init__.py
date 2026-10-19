"""Demo mode -- autonomous end-to-end demonstration capture for a framework run.

`/implement --demo` records the run it drives and, when the run completes, compiles a
distribution-ready presentation from the recording with no further command. The
presentation is an edit of the real application on screen; the drawn dashboard is what it
falls back to when nothing was filmed.

    context.py        DemoContext: the per-run switch and its persisted configuration
    recorder.py       the Recorder: turns canonical runtime events, on-disk envelopes, and
                      host tool hooks into millisecond-stamped JSON execution markers
    timeline.py       marker stream -> presentation scenes (time compression, titles,
                      narration text, the "Agent Thinking..." state per scene)
    narration.py      text-to-speech over the scenes' narration, engine chain with fallbacks

    -- filming the real application (the primary path) --
    obs_ws.py         minimal obs-websocket v5 client, standard library only
    capture.py        screen capture of the Claude Desktop window (OBS, else ffmpeg), and
                      the capture manifest that maps wall clock to video time
    capture_helper.py the detached process that owns a recording while the run proceeds
    overlays.py       the presentation frame drawn around the footage: brand bar, workflow
                      rail, lower thirds, opening and closing cards
    editor.py         capture + markers -> an edit decision list -> presentation_demo.mp4

    -- drawing the run instead, when nothing was filmed --
    renderer.py       the headless dashboard: one DashboardState rendered as text (for the
                      marker's visual_snapshot_text) and as PNG frames (Pillow, optional)
    video_builder.py  frames + narration -> presentation_demo.mp4 (ffmpeg), else an
                      animated GIF, always an HTML deck; writes build-manifest.json
    deck.py           the self-contained HTML presentation deck

Zero-bloat contract
-------------------
`framework_runtime.py` touches this package at exactly three points, and every one of them
is guarded by a single `Path.exists()` on `runs/<run>/demo/demo-context.json`:

    plan --demo          -> `enable(run_dir, ...)`
    RunLedger.emit(...)  -> `on_event(run_dir, event)` after the canonical event is appended
    demo subcommand      -> `status / build / backfill / hook`

When `--demo` was never given the file does not exist, nothing here is imported, and the
main execution path pays one `stat()` per event. Nothing in this package writes outside
`runs/<run>/demo/` (plus the `runs/.active-demo` pointer the host hook reads) and nothing
here may raise into the runtime: every entry point below catches and logs to
`runs/<run>/demo/recorder.log`, because a demo that breaks the run it records is worse
than no demo.
"""

from __future__ import annotations

from pathlib import Path

DEMO_DIR = "demo"
CONTEXT_FILE = "demo-context.json"
MARKERS_FILE = "markers.jsonl"
LOG_FILE = "recorder.log"
ACTIVE_POINTER = ".active-demo"          # runs/.active-demo -> run_id of the recording run
VERSION = "1.0.0"


def demo_dir(run_dir: Path) -> Path:
    return run_dir / DEMO_DIR


def is_enabled(run_dir: Path) -> bool:
    """The one check the hot path performs. A directory listing is not needed: the context
    file is written last by `enable`, so its presence means the demo is fully configured."""
    return (run_dir / DEMO_DIR / CONTEXT_FILE).exists()


def log(run_dir: Path, message: str) -> None:
    """Append one line to the recorder log. Never raises."""
    try:
        from datetime import datetime, timezone
        d = demo_dir(run_dir)
        d.mkdir(parents=True, exist_ok=True)
        stamp = datetime.now(timezone.utc).isoformat(timespec="milliseconds")
        with (d / LOG_FILE).open("a", encoding="utf-8") as fh:
            fh.write(f"{stamp} {message}\n")
    except Exception:  # noqa: BLE001 -- logging must never propagate
        pass


def enable(run_dir: Path, *, requester: str = "operator", options: dict | None = None) -> dict:
    """Create (or re-enter) the DemoContext for a run. Idempotent."""
    from . import context as _ctx
    return _ctx.DemoContext.enable(run_dir, requester=requester, options=options or {}).to_dict()


def on_event(run_dir: Path, event: dict) -> None:
    """Hot-path tap. Called by `RunLedger.emit` after the canonical event is persisted.

    Guarded by `is_enabled` in the caller as well, so a run without a demo never reaches
    here; the guard is repeated because the tap must stay safe if a caller forgets it.
    """
    if not is_enabled(run_dir):
        return
    try:
        from . import recorder as _rec
        _rec.Recorder(run_dir).record_event(event)
        et = event.get("event_type")
        if et in ("run_completed", "run_aborted", "escalation_resolved") \
                and (et == "run_aborted" or run_is_finished(run_dir)) \
                and claim_autobuild(run_dir):
            # A run that is still filming stops its own camera before the film is cut, so
            # the last beat is in the footage and the file is finalised.
            #
            # `run_completed` alone is not the moment to do it. The runtime emits that when
            # the last *phase* completes, which is before the gate closing that phase has
            # been decided -- so stopping there would cut the film just short of the final
            # human approval, which is the beat the whole demonstration is building towards.
            # The camera therefore stops on whichever event leaves nothing undecided.
            from . import capture as _cap
            m = _cap.load_manifest(run_dir)
            if m and m.get("state") == "recording":
                try:
                    _cap.stop_detached(run_dir)
                except Exception as exc:  # noqa: BLE001
                    log(run_dir, f"could not stop the capture automatically: {exc!r}")
            build(run_dir, reason=et)
    except Exception as exc:  # noqa: BLE001 -- the run must never fail because of its demo
        log(run_dir, f"on_event failed for {event.get('event_id')}: {exc!r}")


def run_is_finished(run_dir: Path) -> bool:
    """Is there genuinely nothing left to decide?

    Every phase committed *and* every gate decided. Read from the run's own state store
    rather than from the ledger's summary, because the summary counts phases and a gate is
    not a phase.
    """
    import json
    path = Path(run_dir) / "state.json"
    if not path.exists():
        return False
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return False
    def values(container):
        if isinstance(container, dict):
            return list(container.values())
        return list(container or [])

    phases = [i for i in values(data.get("work_items")) if i.get("work_type") == "state"]
    if not phases or any(i.get("status") != "completed" for i in phases):
        return False
    gates = values(data.get("gates"))
    return bool(gates) and all(g.get("decision") for g in gates)


AUTOBUILD_CLAIM = ".autobuilt"


def claim_autobuild(run_dir: Path) -> bool:
    """Claim the right to compile this run's film, once.

    Finishing a run emits more than one event that leaves nothing undecided: the last gate's
    resolution, and then the aggregation's `run_completed`. Both are genuinely "the run is
    done", and without a claim each would compile the whole film -- the same minutes of
    encoding, twice, for the same result. The claim is a file so it survives the process,
    and it is written before the build rather than after, because a build that crashes
    should not be retried automatically on the next event either.
    """
    claim = demo_dir(run_dir) / AUTOBUILD_CLAIM
    try:
        claim.parent.mkdir(parents=True, exist_ok=True)
        with claim.open("x", encoding="utf-8") as fh:
            from datetime import datetime, timezone
            fh.write(datetime.now(timezone.utc).isoformat(timespec="seconds"))
        return True
    except FileExistsError:
        log(run_dir, "the film was already compiled for this run; not compiling it again")
        return False
    except OSError:
        return True          # cannot claim, so do the useful thing rather than nothing


def has_capture(run_dir: Path) -> bool:
    """Did this run record the real application? Decides which builder makes the film."""
    from . import capture as _cap
    m = _cap.load_manifest(run_dir)
    return bool(m and m.get("state") == "recorded" and Path(m.get("video", "")).exists())


def build(run_dir: Path, **kw) -> dict:
    """Compile the presentation.

    A run that filmed the real application is edited from that footage; a run that did not
    falls back to the drawn dashboard, which needs nothing installed and always produces at
    least the HTML deck.
    """
    if has_capture(run_dir) and not kw.pop("drawn", False):
        from . import editor as _ed
        allowed = ("out_dir", "options", "quiet", "keep_work", "capture_offset")
        result = _ed.edit(run_dir, **{k: v for k, v in kw.items() if k in allowed})
        if result.get("outputs"):
            return result
        log(run_dir, "capture edit produced nothing; falling back to the drawn dashboard")
        kw = {k: v for k, v in kw.items() if k not in ("keep_work", "capture_offset",
                                                       "options")}
    kw.pop("drawn", None)
    from . import video_builder as _vb
    return _vb.build(run_dir, **{k: v for k, v in kw.items()
                                 if k not in ("keep_work", "capture_offset", "options")})


def record_start(run_dir: Path, **kw) -> dict:
    from . import capture as _cap
    return _cap.start_detached(run_dir, **kw)


def record_stop(run_dir: Path, **kw) -> dict:
    from . import capture as _cap
    return _cap.stop_detached(run_dir, **kw)


def capture_status(run_dir: Path) -> dict:
    from . import capture as _cap
    return _cap.status(run_dir)


def backfill(run_dir: Path) -> dict:
    from . import recorder as _rec
    return _rec.Recorder(run_dir).backfill_from_events()


def status(run_dir: Path) -> dict:
    from . import context as _ctx
    from . import recorder as _rec
    ctx = _ctx.DemoContext.load(run_dir)
    if ctx is None:
        return {"enabled": False, "run_id": run_dir.name}
    markers = _rec.Recorder(run_dir).markers()
    manifest_path = demo_dir(run_dir) / "build-manifest.json"
    manifest = None
    if manifest_path.exists():
        import json
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    from . import capture as _cap
    return {"enabled": True, "run_id": run_dir.name, "context": ctx.to_dict(),
            "markers": len(markers),
            "last_marker": markers[-1] if markers else None,
            "build": manifest,
            "capture": _cap.load_manifest(run_dir)}


def ingest_host_hook(runs_root: Path, payload: dict) -> dict | None:
    """Host tool-hook entry point (Claude Code `PostToolUse` and friends).

    Resolves the active demo run through `runs/.active-demo` (or `FRAMEWORK_DEMO_RUN_ID`) and
    records one `tool_invocation` marker. Returns the marker, or None when no demo run is
    active -- which is the normal case and costs one file read.
    """
    import os
    run_id = os.environ.get("FRAMEWORK_DEMO_RUN_ID")
    pointer = runs_root / ACTIVE_POINTER
    if not run_id and pointer.exists():
        run_id = pointer.read_text(encoding="utf-8").strip()
    if not run_id:
        return None
    run_dir = runs_root / run_id
    if not is_enabled(run_dir):
        return None
    try:
        from . import recorder as _rec
        return _rec.Recorder(run_dir).record_tool_invocation(payload)
    except Exception as exc:  # noqa: BLE001
        log(run_dir, f"host hook ingestion failed: {exc!r}")
        return None
