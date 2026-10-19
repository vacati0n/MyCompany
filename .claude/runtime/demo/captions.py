"""Captions: the spoken narration, broken into readable cues and written out three ways.

A demo that can only be followed with the sound on is a demo half the room cannot follow --
someone watching on a phone, someone hard of hearing, someone in a meeting. So every line the
narrator speaks is also shown on screen as it is spoken, and written out afterwards as a
subtitle file and as a script anyone can read before they stand up to present.

The hard part is the timing, and it is already solved: the narration is synthesised before
the picture is cut, so each line's exact duration is known. A line is split into cues at the
punctuation a reader would pause on, and each cue is given a share of that duration in
proportion to how long it takes to say -- which is a good deal closer than splitting the time
evenly, because "roughly 43 thousand" takes far longer to read aloud than "and".
"""

from __future__ import annotations

import re
from pathlib import Path

MAX_CUE_CHARS = 96          # two comfortable lines at the size the caption band renders
MAX_CUE_WORDS = 16
MIN_CUE_SECONDS = 1.1

# Where a reader would draw breath, strongest first. A cue that breaks at a full stop reads
# far better than one that breaks after exactly nine words. Only punctuation and the two
# conjunctions that genuinely start a new clause: breaking after "which" or "that" leaves a
# caption dangling mid-phrase, which is worse to read than a slightly longer one.
_BREAKS = (". ", "? ", "! ", "; ", ": ", " — ", ", ", " and ", " but ")


def split_cues(text: str) -> list:
    """One line of narration, split into cues a viewer can read in the time it is spoken."""
    text = " ".join((text or "").split())
    if not text:
        return []
    cues, rest = [], text
    while rest:
        if len(rest) <= MAX_CUE_CHARS and len(rest.split()) <= MAX_CUE_WORDS:
            cues.append(rest)
            break
        window = rest[:MAX_CUE_CHARS + 1]
        cut = -1
        for sep in _BREAKS:
            found = window.rfind(sep)
            if found > len(window) // 3:            # never break in the first third
                cut = found + len(sep)
                break
        if cut <= 0:
            words = rest.split()
            cut = len(" ".join(words[:MAX_CUE_WORDS])) + 1
        # a cue that ends on the dash it broke at reads as though something is missing
        cues.append(rest[:cut].strip().rstrip("—-").strip())
        rest = rest[cut:].strip()
    return [c for c in cues if c]


def time_cues(text: str, start: float, duration: float) -> list:
    """Cues placed on the timeline, weighted by how long each takes to say."""
    cues = split_cues(text)
    if not cues:
        return []
    weights = [max(1, len(c.split())) for c in cues]
    total = sum(weights)
    out, t = [], start
    for cue, weight in zip(cues, weights):
        span = max(MIN_CUE_SECONDS, duration * weight / total)
        out.append({"start": t, "end": t + span, "text": cue})
        t += span
    # the last cue ends exactly when the voice does, however the rounding fell
    if out:
        out[-1]["end"] = max(out[-1]["start"] + MIN_CUE_SECONDS, start + duration)
    return out


def wrap_cue(text: str, width: int = 48) -> list:
    """Two balanced lines rather than one long one and one short one."""
    words = text.split()
    if len(text) <= width:
        return [text]
    best, target = None, len(text) / 2
    for i in range(1, len(words)):
        head, tail = " ".join(words[:i]), " ".join(words[i:])
        if len(head) > width + 8:
            break
        score = abs(len(head) - target)
        if best is None or score < best[0]:
            best = (score, [head, tail])
    return best[1] if best else [text]


# --------------------------------------------------------------------- output files


def _srt_clock(seconds: float) -> str:
    ms = int(round(max(0.0, seconds) * 1000))
    h, ms = divmod(ms, 3_600_000)
    m, ms = divmod(ms, 60_000)
    s, ms = divmod(ms, 1000)
    return f"{h:02d}:{m:02d}:{s:02d},{ms:03d}"


def write_srt(cues: list, path: Path) -> Path:
    """A standard subtitle file, for anyone who wants to play the film elsewhere."""
    blocks = []
    for i, cue in enumerate(sorted(cues, key=lambda c: c["start"]), 1):
        blocks.append(f"{i}\n{_srt_clock(cue['start'])} --> {_srt_clock(cue['end'])}\n"
                      + "\n".join(wrap_cue(cue["text"])) + "\n")
    path.write_text("\n".join(blocks), encoding="utf-8")
    return path


def _clock(seconds: float) -> str:
    s = int(max(0, seconds))
    return f"{s // 60}:{s % 60:02d}"


def write_script(scenes: list, spoken: dict, path: Path, *, title: str, run_id: str,
                 total_seconds: float, source: str) -> Path:
    """The script, as a document to read before presenting.

    Written for a person rehearsing, not for a machine: what is on screen at each point, what
    the narrator says over it, and where in the film it falls.
    """
    lines = [
        f"# {title}",
        "",
        f"Narration script for the demonstration film of run `{run_id}`.",
        f"Running time {_clock(total_seconds)}, {len(spoken)} narrated beats, cut from "
        f"{source}.",
        "",
        "Every word below is spoken over the recording, and shown on screen as a caption at "
        "the same moment. Nothing here was written by hand: each line is generated from what "
        "the run actually did.",
        "",
    ]
    for idx, scene in enumerate(scenes):
        entry = spoken.get(idx)
        if not entry:
            continue
        lines.append(f"## {_clock(entry['at'])}  {scene.get('title') or scene.get('kind')}")
        lines.append("")
        if scene.get("subtitle"):
            lines.append(f"*On screen:* {scene['subtitle']}")
            lines.append("")
        lines.append(entry["text"])
        lines.append("")
    path.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")
    return path
