"""The editor: a raw screen recording plus the marker stream, cut into a demo.

A recording of a real run is mostly waiting. An agent thinks for four minutes, a gate waits
for a human, a validator runs; none of that is watchable at 1x, and none of it can be cut
blind either, because the moment something *does* happen is exactly the moment worth showing.
The marker stream solves that: every marker carries a wall-clock timestamp, the capture
manifest carries the instant the recording's own clock read zero, so every beat of the run
has a frame number in the footage.

From that the editor builds an edit decision list:

    beat i  ---------------------------------------------  beat i+1
    |<- head 1x ->|<----------- condensed Nx ----------->|<- tail 1x ->|
       show the        the waiting, played fast with a       land on the
       action start    badge saying how fast                 result

Short spans play untouched. Long ones keep their head and tail at real speed and condense the
middle, so nothing is fabricated and nothing is hidden: the badge states the factor.

Around that it composes the presentation frame from `overlays.py` -- the captured window
inset, the workflow rail, a lower third naming the beat, a progress bar -- renders an
opening and closing card, lays the narration under it, and encodes one MP4.

The captured audio is never used. Only the generated narration reaches the output, so nothing
that happened to be playing on the machine during the recording is published.
"""

from __future__ import annotations

import json
import shutil
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

from . import DEMO_DIR, VERSION, log
from . import captions
from . import narration as tts
from . import overlays
from .capture import load_manifest
from .context import DEFAULT_OPTIONS, DemoContext
from .recorder import Recorder, parse_ts
from .timeline import build_scenes, choose_narration, speech_seconds
from .video_builder import MANIFEST as BUILD_MANIFEST, BASENAME, find_ffmpeg

# --- edit rhythm ---------------------------------------------------------------------
#
# The module constants are the brisk profile, which is what the tests pin and what a short
# highlight reel wants. `PACE` overrides them per build. The relaxed profile exists for a
# different audience: someone who has never seen the framework, is being shown it once, and
# is listening rather than reading the small print on screen. It holds every beat far longer,
# condenses far less, and lets the narration decide the length of the film.
KEEP_WHOLE_S = 7.0        # a span at or under this plays untouched
HEAD_S = 3.0              # real-speed lead-in kept at the start of a condensed span
TAIL_S = 2.5              # real-speed landing kept at the end of a condensed span
CONDENSED_S = 2.5         # what the condensed middle is squeezed to
MAX_SPEED = 60.0
MIN_SEGMENT_S = 1.2       # the least time a caption may be on screen and still be read
MIN_SPEED = 0.25          # how far a very brief beat may be slowed to reach that
TRAILING_S = 6.0          # how long the last beat holds when the recording runs on
INTRO_S = 5.0
OUTRO_S = 6.5
THINK_FRAMES = 8          # phase-shifted overlays that animate the "working" dots
THINK_FRAME_S = 0.25
FADE_S = 0.6
NARRATION_PAD_S = 1.2     # quiet held after a sentence before the picture moves on
OPENING_VOICE_AT_S = 1.5  # the narrator starts over the title card, not after it

PACE = {
    "brisk": {},
    "relaxed": {
        "KEEP_WHOLE_S": 14.0,
        "HEAD_S": 5.0,
        "TAIL_S": 4.0,
        "CONDENSED_S": 5.0,
        "MAX_SPEED": 30.0,
        "MIN_SEGMENT_S": 3.5,
        # a screen recording at a third speed still reads as work happening; below
        # that it stops looking deliberate, which is where holding a frame takes over
        "MIN_SPEED": 0.33,
        "TRAILING_S": 8.0,
        "INTRO_S": 9.0,
        "OUTRO_S": 13.0,
        "NARRATION_PAD_S": 1.8,
    },
}
DEFAULT_PACE = "relaxed"


class Pace(dict):
    """The rhythm constants for one build: module defaults under a named profile."""

    def __init__(self, name: str | None = None):
        super().__init__()
        base = {k: v for k, v in globals().items()
                if k.isupper() and isinstance(v, (int, float))}
        base.update(PACE.get(name or DEFAULT_PACE, {}))
        self.name = name or DEFAULT_PACE
        self.update(base)

    def __getattr__(self, item):
        try:
            return self[item]
        except KeyError as exc:
            raise AttributeError(item) from exc


def _now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def _k(n) -> str:
    if not n:
        return "0"
    n = int(n)
    return f"{n / 1000:.1f}k" if n >= 1000 else str(n)


def _fmt_clock(seconds: float) -> str:
    s = int(max(0, seconds))
    return f"{s // 60}:{s % 60:02d}"


def _probe(ffmpeg: str, video: Path) -> dict:
    """Duration and frame size of the capture, read from ffmpeg's own report."""
    out = subprocess.run([ffmpeg, "-hide_banner", "-i", str(video)],
                         capture_output=True, text=True).stderr
    info = {"width": None, "height": None, "duration": None}
    import re
    m = re.search(r"Stream .*Video:.*?, (\d+)x(\d+)", out)
    if m:
        info["width"], info["height"] = int(m.group(1)), int(m.group(2))
    m = re.search(r"Duration: (\d+):(\d\d):(\d\d\.\d+)", out)
    if m:
        info["duration"] = int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))
    return info


# --------------------------------------------------------------------- edit decisions


def _overlaps(a: float, b: float, spans: list) -> bool:
    return any(a < end and b > start for start, end in spans or [])


def scene_spans(scenes: list, markers: list, t0: str, capture_duration: float,
                trailing: float = TRAILING_S, occluded: list | None = None) -> list:
    """Each scene's real span inside the capture, in seconds from the recording's start."""
    by_id = {m["marker_id"]: m for m in markers}
    base = parse_ts(t0)
    if base is None:
        raise ValueError("the capture manifest carries no usable start time")
    spans = []
    for scene in scenes:
        ids = scene.get("marker_ids") or []
        first = by_id.get(ids[0]) if ids else None
        if first is None:
            continue
        ts = parse_ts(first["timestamp"])
        if ts is None:
            continue
        spans.append({"scene": scene, "start": (ts - base).total_seconds()})
    spans.sort(key=lambda s: s["start"])
    for i, s in enumerate(spans):
        s["end"] = spans[i + 1]["start"] if i + 1 < len(spans) \
            else min(capture_duration, s["start"] + trailing)
    # Keep only what the recording actually contains. A run whose first phases happened
    # before the camera was rolling still films: those beats are simply not in the footage,
    # and the opening card carries the run's identity instead. A beat that *is* in the
    # footage is never dropped for being brief -- `plan_edit` slows it instead, because two
    # events a tenth of a second apart are still two things the run did.
    kept = []
    for s in spans:
        if s["end"] <= 0 or s["start"] >= capture_duration:
            continue
        s["start"] = max(0.0, s["start"])
        s["end"] = min(capture_duration, s["end"])
        # Footage filmed while something else was on screen is not this run and is not ours
        # to publish. A span that runs into an occluded stretch is cut back to where the
        # window was still in front; one that lies wholly inside it is dropped.
        for start, end in occluded or []:
            if s["start"] >= start and s["end"] <= end:
                s["end"] = s["start"]
                break
            if s["start"] < start < s["end"]:
                s["end"] = start
            elif start <= s["start"] < end:
                s["start"] = min(end, s["end"])
        if s["end"] - s["start"] >= 0.2:
            kept.append(s)
    return kept


def rebalance_spans(spans: list, min_span: float) -> dict:
    """Give a beat that owns no footage the seconds immediately after it.

    Some beats are instantaneous. A dispatch and the response to it are two markers a
    fraction of a second apart, so the span between them holds almost no film, and a
    narrator explaining what a dispatch *is* would otherwise be talking over a frozen
    frame. The seconds that follow are the honest thing to show: they are the same
    recording, they are what happened next, and the caption still names the beat that
    opened them. So a starved span takes from the one after it, and only as much as that
    one can spare -- a beat is never robbed to feed its predecessor.
    """
    moved = 0
    for i, s in enumerate(spans[:-1]):
        have = s["end"] - s["start"]
        if have >= min_span:
            continue
        nxt = spans[i + 1]
        spare = (nxt["end"] - nxt["start"]) - min_span
        take = min(min_span - have, max(0.0, spare))
        if take <= 0.05:
            continue
        s["end"] += take
        nxt["start"] += take
        moved += 1
    return {"spans_rebalanced": moved}


def plan_edit(spans: list, pace: "Pace | None" = None) -> list:
    """The edit decision list: ordered output segments, each a slice of the capture."""
    p = pace or Pace("brisk")
    segments = []
    for span in spans:
        scene, a, b = span["scene"], span["start"], span["end"]
        dur = b - a
        if dur < p.MIN_SEGMENT_S:
            # Two events a fraction of a second apart are still two things the run did.
            # Slowing the moment keeps both captions readable without inventing footage.
            segments.append({"scene": scene, "in": a, "out": b,
                             "speed": max(p.MIN_SPEED, dur / p.MIN_SEGMENT_S),
                             "role": "held"})
            continue
        if dur <= p.KEEP_WHOLE_S:
            segments.append({"scene": scene, "in": a, "out": b, "speed": 1.0,
                             "role": "beat"})
            continue
        middle_start, middle_end = a + p.HEAD_S, b - p.TAIL_S
        middle = middle_end - middle_start
        if middle < 3.0:
            segments.append({"scene": scene, "in": a, "out": b, "speed": 1.0, "role": "beat"})
            continue
        speed = min(p.MAX_SPEED, middle / p.CONDENSED_S)
        segments.append({"scene": scene, "in": a, "out": middle_start, "speed": 1.0,
                         "role": "head"})
        segments.append({"scene": scene, "in": middle_start, "out": middle_end,
                         "speed": speed, "role": "condensed"})
        segments.append({"scene": scene, "in": middle_end, "out": b, "speed": 1.0,
                         "role": "tail"})
    return relayout(segments)


def relayout(segments: list) -> list:
    """Recompute each segment's on-screen length and its offset in the finished film."""
    t = 0.0
    for seg in segments:
        seg["duration"] = (seg["out"] - seg["in"]) / seg["speed"] + seg.get("hold", 0.0)
        seg["at"] = t
        t += seg["duration"]
    return segments


def body_duration(segments: list) -> float:
    return sum(s["duration"] for s in segments)


def fit_narration(segments: list, scenes: list, need: dict, pace: "Pace") -> dict:
    """Give every beat enough screen time for the sentence spoken over it.

    Two levers, in this order. First the condensing is *relaxed*: a beat whose middle was
    being played at twenty times speed is slowed back towards real time, so the extra seconds
    are filled with more of the work actually happening rather than with padding. Only when
    a beat has no more real footage left to give is its last frame held still, which is what
    a presenter does anyway when they pause on a screen to explain it.

    `need` maps a scene's index to the seconds its narration occupies. Returns a report of
    what it had to do, for the build manifest.
    """
    by_scene: dict = {}
    for seg in segments:
        by_scene.setdefault(id(seg["scene"]), []).append(seg)
    relaxed = held = slowed = 0
    for idx, scene in enumerate(scenes):
        want = need.get(idx)
        segs = by_scene.get(id(scene))
        if not want or not segs:
            continue
        want += pace.NARRATION_PAD_S

        def total() -> float:
            for y in segs:
                y["duration"] = (y["out"] - y["in"]) / y["speed"] + y.get("hold", 0.0)
            return sum(y["duration"] for y in segs)

        have = total()
        if have >= want:
            continue
        # 1. un-condense, never below real time
        for s in segs:
            if s["speed"] <= 1.0 or have >= want:
                continue
            others = have - s["duration"]
            allowance = want - others                 # seconds this segment may now occupy
            s["speed"] = max(1.0, (s["out"] - s["in"]) / max(0.04, allowance))
            have = total()
            relaxed += 1
        # 2. slow what is left, down to the floor for this pace. Gentle slow motion over a
        #    screen recording still reads as the work happening; a frozen frame does not.
        if have < want:
            for s in segs:
                if have >= want:
                    break
                others = have - s["duration"]
                allowance = want - others
                s["speed"] = max(pace.MIN_SPEED, (s["out"] - s["in"]) / max(0.04, allowance))
                have = total()
                slowed += 1
        # 3. only now hold the last frame, for whatever is still missing
        if have < want:
            segs[-1]["hold"] = segs[-1].get("hold", 0.0) + (want - have)
            held += 1
            total()
    relayout(segments)
    return {"beats_uncondensed": relaxed, "beats_slowed": slowed, "beats_held": held}


# --------------------------------------------------------------------- narration


def plan_narration(segments: list, scenes: list, options: dict, work: Path,
                   ffmpeg: str | None, pace: "Pace") -> dict:
    """Record the narration, fit the cut around it, then lay the voice on the timeline.

    The order matters and it is the opposite of the brisk path. In `explain` mode the script
    is written first and the picture is paced to it, because the point of that mode is that
    a viewer who never reads the screen still understands what happened. Elsewhere the film
    is cut to the run and a line too long for its beat is replaced by its short form.
    """
    explaining = bool(options.get("explain"))
    mode = str(options.get("narration") or "auto")
    total = body_duration(segments) + pace.INTRO_S + pace.OUTRO_S
    density = choose_narration(scenes, None if explaining else total,
                               mode="explain" if explaining else None)

    first_seg = {}
    for seg in segments:
        first_seg.setdefault(id(seg["scene"]), seg)

    lines = []
    for idx, scene in enumerate(scenes):
        if id(scene) not in first_seg:
            continue
        spoken = scene.get("spoken") or ""
        if spoken and not explaining:
            room = sum(s["duration"] for s in segments if s["scene"] is scene)
            if speech_seconds(spoken) > room + 1.5:
                spoken = scene.get("narration_short") or ""
        if spoken:
            lines.append((idx, spoken))

    if mode == "off" or not lines:
        return {"engine": None, "segments": [], "density": density, "track": None,
                "fit": {}, "attempts": [{"engine": "off", "reason": "narration disabled"}]}

    nar = tts.synthesize(lines, work / "narration", engine=mode,
                         voice=options.get("voice"), ffmpeg=ffmpeg,
                         rate=options.get("speech_rate"))
    fit = {}
    if nar["segments"] and explaining:
        # The opening line starts over the title card, so that beat has the card's own
        # seconds to play with before its footage has to carry the sentence.
        head_room = max(0.0, pace.INTRO_S - OPENING_VOICE_AT_S)
        need = {}
        for s in nar["segments"]:
            seconds = s["duration_ms"] / 1000.0
            if s["index"] == lines[0][0]:
                seconds = max(0.0, seconds - head_room)
            need[s["index"]] = seconds
        fit = fit_narration(segments, scenes, need, pace)
        total = body_duration(segments) + pace.INTRO_S + pace.OUTRO_S

    placement = {}
    for i, (idx, _) in enumerate(lines):
        seg = first_seg[id(scenes[idx])]
        placement[idx] = (OPENING_VOICE_AT_S if (explaining and i == 0)
                          else pace.INTRO_S + seg["at"] + 0.4)

    track = None
    if nar["segments"]:
        track = work / "narration.wav"
        tts.assemble([{"offset_ms": int(placement[s["index"]] * 1000), "path": s["path"]}
                      for s in nar["segments"] if s["index"] in placement],
                     int(total * 1000) + 1500, track)

    # What was said, when, and in what words -- for the captions burned into the picture,
    # for the subtitle file, and for the script a presenter reads beforehand.
    spoken_by_index, cues = {}, []
    said = {idx: text for idx, text in lines}
    for s in nar["segments"]:
        idx = s["index"]
        if idx not in placement:
            continue
        at = placement[idx]
        spoken_by_index[idx] = {"at": at, "seconds": s["duration_ms"] / 1000.0,
                                "text": said.get(idx, "")}
        cues.extend(captions.time_cues(said.get(idx, ""), at, s["duration_ms"] / 1000.0))
    return {"engine": nar["engine"], "segments": nar["segments"], "attempts": nar["attempts"],
            "density": density, "track": track, "fit": fit, "cues": cues,
            "spoken": spoken_by_index}


# --------------------------------------------------------------------- overlays


def _caption_slices(seg: dict, cues: list, offset: float) -> list:
    """Split one segment where the caption over it changes.

    `cues` are on the finished film's timeline, which starts with the opening card; `offset`
    is how far that card pushes the body along. Returns (duration, text) pairs that sum to
    the segment's own length, with `None` where nothing is being said.
    """
    a, b = seg["at"] + offset, seg["at"] + seg["duration"] + offset
    edges = {a, b}
    for cue in cues:
        for t in (cue["start"], cue["end"]):
            if a < t < b:
                edges.add(t)
    marks = sorted(edges)
    slices = []
    for start, end in zip(marks, marks[1:]):
        if end - start <= 0.02:
            continue
        mid = (start + end) / 2
        text = next((c["text"] for c in cues if c["start"] <= mid < c["end"]), None)
        slices.append((end - start, text))
    return slices or [(seg["duration"], None)]


def render_overlays(segments: list, out_dir: Path, *, run_id: str, command: str,
                    capture_size: tuple, theme: str, brand_subtitle: str,
                    cues: list | None = None, caption_offset: float = 0.0) -> list:
    """The overlay track: one image per segment, cut again wherever the caption changes.

    Returns concat entries of (path, duration) covering the body exactly.
    """
    out_dir.mkdir(parents=True, exist_ok=True)
    total = body_duration(segments) or 1.0
    cues = cues or []
    entries = []
    for i, seg in enumerate(segments):
        scene = seg["scene"]
        speed = seg["speed"] if seg["role"] == "condensed" else None
        slices = _caption_slices(seg, cues, caption_offset)
        if len(slices) == 1 and slices[0][1] is None and scene.get("thinking") \
                and seg["duration"] >= THINK_FRAME_S * 3:
            # nothing is being said over this beat, so the working dots carry it instead
            frames = []
            for f in range(THINK_FRAMES):
                img = overlays.beat_overlay(
                    scene, run_id=run_id, command=command, capture_size=capture_size,
                    fraction=(seg["at"] + seg["duration"] * f / THINK_FRAMES) / total,
                    speed=speed, thinking_phase=f / THINK_FRAMES, theme=theme,
                    brand_subtitle=brand_subtitle)
                frames.append(overlays.save(img, out_dir / f"seg-{i:03d}-{f}.png"))
            left, f = seg["duration"], 0
            while left > 0.01:
                d = min(THINK_FRAME_S, left)
                entries.append((frames[f % THINK_FRAMES], d))
                left -= d
                f += 1
            continue
        elapsed = 0.0
        for j, (duration, text) in enumerate(slices):
            img = overlays.beat_overlay(
                scene, run_id=run_id, command=command, capture_size=capture_size,
                fraction=(seg["at"] + elapsed + duration / 2) / total, speed=speed,
                theme=theme, brand_subtitle=brand_subtitle, caption=text)
            p = overlays.save(img, out_dir / f"seg-{i:03d}-c{j:02d}.png")
            entries.append((p, duration))
            elapsed += duration
    return entries


def write_concat(entries: list, path: Path) -> Path:
    """An ffconcat script for the overlay track. The last image is repeated without a
    duration, which is how the concat demuxer is told to hold the final frame."""
    lines = ["ffconcat version 1.0"]
    for p, d in entries:
        lines.append(f"file '{p.as_posix()}'")
        lines.append(f"duration {d:.3f}")
    if entries:
        lines.append(f"file '{entries[-1][0].as_posix()}'")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return path


# --------------------------------------------------------------------- filtergraph


def build_filtergraph(segments: list, *, capture_size: tuple, fps: int,
                      has_intro: bool, has_outro: bool, intro_idx: int, outro_idx: int,
                      overlay_idx: int, intro_s: float = INTRO_S,
                      outro_s: float = OUTRO_S) -> str:
    """The whole edit as one filter_complex, written to a script file.

    Input 0 is the capture, `overlay_idx` the overlay track, and the cards their own image
    inputs. Everything is normalised to the canvas, the same frame rate, and the same pixel
    format before `concat`, which refuses mismatched inputs.
    """
    W, H = overlays.CANVAS
    vx, vy, vw, vh = overlays._video_target(capture_size)
    g = []
    for i, seg in enumerate(segments):
        pts = "PTS-STARTPTS" if seg["speed"] == 1.0 else f"(PTS-STARTPTS)/{seg['speed']:.6f}"
        # `fps` before anything else time-based, and it is not optional. A screen capture is
        # variable frame rate -- gdigrab emits a frame when the screen changes -- and both of
        # the filters below need a known cadence: `tpad` cannot work out how many frames to
        # clone without one and silently pads nothing, and slow motion on a variable-rate
        # source judders. Resampling to a constant rate after the stretch fixes both.
        chain = (f"[0:v]trim=start={seg['in']:.3f}:end={seg['out']:.3f},"
                 f"setpts={pts},fps={fps}")
        hold = seg.get("hold", 0.0)
        if hold > 0.01:
            # Hold the last frame rather than invent footage: the picture stops while the
            # narrator finishes the sentence, which is what a presenter does anyway.
            chain += f",tpad=stop_mode=clone:stop_duration={hold:.3f}"
        g.append(chain + f"[s{i}];")
    g.append("".join(f"[s{i}]" for i in range(len(segments)))
             + f"concat=n={len(segments)}:v=1:a=0[raw];")
    g.append(f"[raw]fps={fps},scale={vw}:{vh}:flags=lanczos,format=rgba[fg];")
    g.append(f"color=c=0x0B0F19:s={W}x{H}:r={fps},format=rgba[bg];")
    g.append(f"[bg][fg]overlay={vx}:{vy}:shortest=1[comp];")
    g.append(f"[{overlay_idx}:v]fps={fps},scale={W}:{H},format=rgba[ov];")
    g.append("[comp][ov]overlay=0:0:shortest=1,format=yuv420p[body];")

    parts = []
    if has_intro:
        g.append(f"[{intro_idx}:v]scale={W}:{H},fps={fps},format=yuv420p,"
                 f"fade=t=in:st=0:d={FADE_S},fade=t=out:st={intro_s - FADE_S:.2f}:"
                 f"d={FADE_S}[intro];")
        parts.append("[intro]")
    parts.append("[body]")
    if has_outro:
        g.append(f"[{outro_idx}:v]scale={W}:{H},fps={fps},format=yuv420p,"
                 f"fade=t=in:st=0:d={FADE_S},fade=t=out:st={outro_s - FADE_S:.2f}:"
                 f"d={FADE_S}[outro];")
        parts.append("[outro]")
    if len(parts) > 1:
        g.append("".join(parts) + f"concat=n={len(parts)}:v=1:a=0[outv]")
    else:
        g.append("[body]null[outv]")
    return "\n".join(g)


# --------------------------------------------------------------------- the build


def edit(run_dir: Path, *, capture: dict | None = None, out_dir: Path | None = None,
         options: dict | None = None, quiet: bool = False, keep_work: bool = False,
         capture_offset: float = 0.0) -> dict:
    """Cut the run's capture into `presentation_demo.mp4`. Never raises."""
    run_dir = Path(run_dir)
    started = time.monotonic()
    ctx = DemoContext.load(run_dir)
    opts = dict(ctx.options) if ctx else dict(DEFAULT_OPTIONS)
    opts.update({k: v for k, v in (options or {}).items() if v is not None})
    pace = Pace(opts.get("pace"))
    out_dir = Path(out_dir) if out_dir else run_dir / DEMO_DIR
    out_dir.mkdir(parents=True, exist_ok=True)
    work = out_dir / "edit"
    manifest = {
        "schema": "framework.runtime/demo-build-manifest.v1", "version": VERSION,
        "run_id": run_dir.name, "built_at": _now(), "reason": "capture-edit",
        "source": "screen-capture", "title": opts.get("title") or run_dir.name,
        "options": opts, "pace": pace.name, "explain": bool(opts.get("explain")),
        "outputs": {}, "fallbacks": [], "narration": {}, "errors": [],
        "segments": 0, "scenes": 0, "duration_ms": 0,
    }

    def say(line: str):
        if quiet:
            return
        enc = sys.stdout.encoding or "utf-8"
        print(line.encode(enc, "replace").decode(enc))

    try:
        if not overlays.has_pil():
            raise RuntimeError("Pillow is required to compose the presentation frame; "
                               "install it with `pip install pillow`")
        ffmpeg, how = find_ffmpeg()
        if not ffmpeg:
            raise RuntimeError(f"ffmpeg is required to edit a screen capture: {how}. "
                               f"Install it, or `pip install imageio-ffmpeg`")
        manifest["ffmpeg"] = {"path": ffmpeg, "resolved_by": how}

        cap = capture or load_manifest(run_dir)
        if not cap:
            raise RuntimeError(f"no capture manifest for {run_dir.name}; record first with "
                               f"`demo --run-id {run_dir.name} --record-start`")
        video = Path(cap["video"])
        if not video.exists():
            raise RuntimeError(f"the recorded file is missing: {video}")
        probe = _probe(ffmpeg, video)
        cap_size = (probe["width"] or cap["rect"][2], probe["height"] or cap["rect"][3])
        cap_dur = probe["duration"] or cap.get("duration_seconds") or 0
        if cap_dur <= 0:
            raise RuntimeError(f"the recording at {video} has no duration")
        manifest["capture"] = {"video": str(video), "seconds": round(cap_dur, 2),
                               "size": list(cap_size), "backend": cap.get("backend"),
                               "started_at": cap.get("started_at")}

        markers = Recorder(run_dir).markers()
        if not markers:
            raise RuntimeError("no execution markers; the run was not recorded with --demo")
        scenes = build_scenes(markers, opts)
        t0 = cap.get("started_at")
        if capture_offset:
            # A recording made by hand carries no start instant: the operator states how far
            # into the file the run's first marker falls, and the rest follows from that.
            base = parse_ts(markers[0]["timestamp"])
            t0 = (base.timestamp() - capture_offset)
            t0 = datetime.fromtimestamp(t0, timezone.utc).isoformat(
                timespec="milliseconds").replace("+00:00", "Z")
        occluded = cap.get("occluded") or []
        if occluded:
            hidden = sum(b - a for a, b in occluded)
            manifest["fallbacks"].append({
                "stage": "occlusion",
                "reason": f"{hidden:.0f}s of the recording filmed something other than the "
                          f"target window and is excluded from the film; enable OBS window "
                          f"capture to make a recording immune to this"})
            manifest["occluded"] = occluded
        spans = scene_spans(scenes, markers, t0, cap_dur, pace.TRAILING_S, occluded)
        if not spans:
            raise RuntimeError(
                "no beat of this run falls inside the recording. Check that the capture was "
                "running while the run executed, or pass --capture-offset")
        covered = [s["scene"] for s in spans]
        if opts.get("explain"):
            manifest["rebalance"] = rebalance_spans(spans, pace.MIN_SEGMENT_S * 2)
        segments = plan_edit(spans, pace)
        manifest["scenes"] = len(covered)
        manifest["segments"] = len(segments)
        manifest["condensed"] = sum(1 for s in segments if s["role"] == "condensed")
        if len(covered) < len(scenes):
            missed = len(scenes) - len(covered)
            manifest["fallbacks"].append({
                "stage": "coverage",
                "reason": f"{missed} of {len(scenes)} beats happened outside the recording "
                          f"and are not shown; start the camera with `plan --demo "
                          f"--demo-record` to film a run from its first event"})

        nar = plan_narration(segments, covered, opts, work, ffmpeg, pace)
        manifest["narration"] = {"engine": nar["engine"], "attempts": nar["attempts"],
                                 "segments": len(nar["segments"]), "density": nar["density"],
                                 "speech_rate": opts.get("speech_rate"),
                                 "fit": nar.get("fit") or {}}
        manifest["condensed"] = sum(1 for s in segments if s["role"] == "condensed"
                                    and s["speed"] > 1.05)
        if not nar["segments"] and str(opts.get("narration") or "auto") != "off":
            manifest["fallbacks"].append({"stage": "narration", "reason":
                                          "no text-to-speech engine produced audio"})

        proj = (covered[-1].get("projection") or {}) if covered else {}
        done, total_phases = overlays._phase_progress(proj)
        all_gates = len(proj.get("gates") or {})
        gates = sum(1 for g in (proj.get("gates") or {}).values() if g.get("decision"))
        tokens = (proj.get("tokens") or {}).get("prompt_est")
        body = body_duration(segments)
        raw_span = sum(s["end"] - s["start"] for s in spans)

        show_captions = opts.get("captions", True)
        entries = render_overlays(
            segments, work / "overlay", run_id=run_dir.name,
            command=str(proj.get("command_id") or "implement"), capture_size=cap_size,
            theme=str(opts.get("theme") or "midnight"),
            brand_subtitle=f"/{proj.get('command_id') or 'implement'} --demo   ·   "
                           f"{proj.get('workflow_id') or ''}",
            cues=(nar.get("cues") or []) if show_captions else [],
            caption_offset=pace.INTRO_S)
        manifest["captions"] = {"shown": bool(show_captions and nar.get("cues")),
                                "cues": len(nar.get("cues") or [])}
        concat = write_concat(entries, work / "overlay.txt")

        intro = overlays.save(overlays.title_card(
            manifest["title"],
            f"/{proj.get('command_id') or 'implement'} --demo   ·   "
            f"{proj.get('workflow_id') or 'implement-feature'}",
            [f"{total_phases} phases, each owned by a specialist agent",
             f"{all_gates} human gates no agent can bypass",
             "recorded live from the run, condensed by its own telemetry"]),
            work / "intro.png")
        outro = overlays.save(overlays.outro_card(
            manifest["title"],
            [(f"{done}/{total_phases}", "phases delivered"),
             (str(gates), "gates decided by a human"),
             (f"~{_k(tokens)}", "tokens of context (est.)"),
             (str(sum(1 for m in markers if m["action_type"] == "prompt_exchange")),
              "agent invocations"),
             (_fmt_clock(raw_span), "of run, real time"),
             (_fmt_clock(body + pace.INTRO_S + pace.OUTRO_S), "of demo")],
            f"Every frame is the real run. {manifest['segments']} segments cut from "
            f"{_fmt_clock(cap_dur)} of screen recording by {len(markers)} execution markers."),
            work / "outro.png")

        fps = int(cap.get("fps") or 30)
        inputs = ["-i", str(video),
                  "-f", "concat", "-safe", "0", "-i", str(concat),
                  "-loop", "1", "-t", str(pace.INTRO_S), "-i", str(intro),
                  "-loop", "1", "-t", str(pace.OUTRO_S), "-i", str(outro)]
        graph = build_filtergraph(segments, capture_size=cap_size, fps=fps,
                                  has_intro=True, has_outro=True, intro_idx=2, outro_idx=3,
                                  overlay_idx=1, intro_s=pace.INTRO_S,
                                  outro_s=pace.OUTRO_S)
        graph_path = work / "filtergraph.txt"
        graph_path.write_text(graph, encoding="utf-8")

        out = out_dir / f"{BASENAME}.mp4"
        cmd = [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", *inputs]
        if nar["track"]:
            cmd += ["-i", str(nar["track"])]
        cmd += ["-filter_complex_script", str(graph_path), "-map", "[outv]"]
        if nar["track"]:
            cmd += ["-map", "4:a", "-c:a", "aac", "-b:a", "160k"]
        else:
            cmd += ["-an"]          # the capture's own audio is never published
        cmd += ["-c:v", "libx264", "-preset", "medium", "-crf", "19", "-pix_fmt", "yuv420p",
                "-movflags", "+faststart"]
        if nar["track"]:
            # the narration track is padded past the last scene; the picture decides the end
            cmd += ["-shortest"]
        cmd += [str(out)]
        say(f"editing        : {len(segments)} segments "
            f"({manifest['condensed']} condensed) from {_fmt_clock(cap_dur)} of capture")
        proc = subprocess.run(cmd, capture_output=True, text=True, timeout=3600)
        if proc.returncode != 0:
            raise RuntimeError(f"ffmpeg failed: {(proc.stderr or '')[-600:]}")
        manifest["outputs"]["mp4"] = str(out)
        manifest["duration_ms"] = int((body + pace.INTRO_S + pace.OUTRO_S) * 1000)
        manifest["engine"] = f"ffmpeg ({how})"
        if nar["track"]:
            manifest["outputs"]["narration_wav"] = str(nar["track"])

        # The same words three ways: burned into the picture above, as a subtitle file for
        # anyone playing the film elsewhere, and as a script to read before presenting.
        if nar.get("cues"):
            srt = captions.write_srt(nar["cues"], out_dir / f"{BASENAME}.srt")
            manifest["outputs"]["subtitles"] = str(srt)
        if nar.get("spoken"):
            script = captions.write_script(
                covered, nar["spoken"], out_dir / f"{BASENAME}-script.md",
                title=manifest["title"], run_id=run_dir.name,
                total_seconds=body + pace.INTRO_S + pace.OUTRO_S,
                source=f"{_fmt_clock(cap_dur)} of screen recording")
            manifest["outputs"]["script"] = str(script)

        poster = out_dir / f"{BASENAME}-poster.png"
        subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-ss",
                        str(pace.INTRO_S + min(8.0, body / 3)), "-i", str(out),
                        "-vframes", "1",
                        str(poster)], capture_output=True, timeout=120)
        if poster.exists():
            manifest["outputs"]["poster"] = str(poster)

        manifest["edl"] = [
            {"at": round(s["at"], 3), "duration": round(s["duration"], 3),
             "in": round(s["in"], 3), "out": round(s["out"], 3), "speed": round(s["speed"], 2),
             "role": s["role"], "beat": s["scene"].get("kind"),
             "title": s["scene"].get("title")}
            for s in segments]
        if not keep_work:
            shutil.rmtree(work / "overlay", ignore_errors=True)
    except Exception as exc:                     # noqa: BLE001 -- reported, never raised
        import traceback
        manifest["errors"].append(f"{type(exc).__name__}: {exc}")
        log(run_dir, "capture edit failed:\n" + traceback.format_exc())

    manifest["elapsed_seconds"] = round(time.monotonic() - started, 2)
    (out_dir / BUILD_MANIFEST).write_text(json.dumps(manifest, indent=2, default=str),
                                          encoding="utf-8")
    prefix = run_dir.parent.parent.name
    say("")
    ok = manifest["outputs"] and not manifest["errors"]
    say(f"demo           : {'compiled' if ok else 'INCOMPLETE'}  "
        f"({manifest['segments']} segments, {manifest['duration_ms'] / 1000:.1f}s, "
        f"{manifest['elapsed_seconds']}s to build)")
    for key, label in (("mp4", "video"), ("script", "script"), ("subtitles", "subtitles"),
                       ("poster", "poster"), ("narration_wav", "narration")):
        if key in manifest["outputs"]:
            p = Path(manifest["outputs"][key])
            try:
                shown = f"{prefix}/{p.resolve().relative_to(run_dir.parent.parent.resolve()).as_posix()}"
            except ValueError:
                shown = str(p)
            say(f"  {label:<13}: {shown}  ({p.stat().st_size / 1024 / 1024:.1f} MB)")
    if manifest["narration"].get("engine"):
        say(f"  voice        : {manifest['narration']['engine']} "
            f"({manifest['narration']['segments']} lines, "
            f"{manifest['narration']['density']} density)")
    for fb in manifest["fallbacks"]:
        say(f"  fallback     : {fb['stage']} -> {fb['reason']}")
    for err in manifest["errors"]:
        say(f"  error        : {err}")
    return manifest
