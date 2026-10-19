"""Presentation furniture drawn around the real screen recording.

The capture is the product doing the work. Everything here is the frame around it: a brand
bar, the workflow rail that says which phase the run is in and which gates have been
decided, a lower third naming what is happening at this instant, and the cards that open and
close the film. All of it is rendered with Pillow into full-canvas RGBA PNGs with a
transparent window where the footage shows through, so the whole composite is one `overlay`
filter in ffmpeg rather than a dozen drawtext filters that would depend on ffmpeg's own font
configuration.

Layout, at 1920x1080:

    +------------------------------------------------------------------+
    |  brand bar                                          run id · REC  |  0..72
    +--------------------------------------------+---------------------+
    |                                            |  WORKFLOW           |
    |            (transparent: the                |  1 scope ...        |
    |             captured window)               |  2 planning ...     |  84..851
    |                                            |  ...                |
    +--------------------------------------------+---------------------+
    |  DISPATCH                                             12x badge   |
    |  Dispatching execution-planning to the planner                    |  875..1045
    |  ~15.0k tokens of context · attempt 1                             |
    +------------------------------------------------------------------+
    |================================                                   |  progress
    +------------------------------------------------------------------+
"""

from __future__ import annotations

import math
from pathlib import Path

from .renderer import (GATE_COLOR, STATUS_COLOR, THEMES, _ellipsize, _font, _k,
                       _phase_progress, _short_agent, _text_width, _wrap, has_pil)

if has_pil():
    from PIL import Image, ImageDraw, ImageFilter

CANVAS = (1920, 1080)
VIDEO_BOX = (24, 84, 1428, 740)           # x, y, w, h -- where the capture is composited
RAIL_BOX = (1476, 84, 420, 740)
LOWER_BOX = (24, 848, 1872, 222)          # the beat, and the caption being spoken over it
BRAND_H = 72
PROGRESS_H = 6
CAPTION_WIDTH = 52                        # characters per caption line at this size

KIND_LABEL = {
    "opening": "DEMO", "materialized": "ROUTING", "context_hydrated": "CONTEXT",
    "dispatch": "DISPATCH", "prompt_exchange": "THINKING", "agent_response": "RESULT",
    "validation": "VALIDATION", "retry": "RECOVERY", "rollback": "ROLLBACK",
    "gate_awaiting": "GATE", "gate_decision": "GATE", "phase_blocked": "BLOCKED",
    "phase_unblocked": "UNBLOCKED", "tool_group": "TOOLS", "aggregation": "AGGREGATION",
    "run_completed": "COMPLETE", "run_aborted": "ABORTED", "note": "NOTE",
}
KIND_COLOR = {
    "dispatch": "accent", "prompt_exchange": "accent", "agent_response": "purple",
    "validation": "green", "retry": "amber", "rollback": "amber", "gate_decision": "green",
    "gate_awaiting": "amber", "phase_blocked": "amber", "tool_group": "pink",
    "run_completed": "green", "run_aborted": "red", "opening": "accent",
}


def _video_target(capture_size: tuple) -> tuple:
    """Where and how large the capture sits on the canvas, preserving its aspect ratio."""
    bx, by, bw, bh = VIDEO_BOX
    cw, ch = capture_size
    scale = min(bw / cw, bh / ch)
    w = int(cw * scale) // 2 * 2
    h = int(ch * scale) // 2 * 2
    return bx + (bw - w) // 2, by + (bh - h) // 2, w, h


class _Canvas:
    def __init__(self, theme: str = "midnight"):
        self.t = THEMES.get(theme, THEMES["midnight"])
        self.img = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img, "RGBA")

    def c(self, name, alpha: int = 255):
        rgb = self.t[name] if isinstance(name, str) else tuple(name)
        return rgb if len(rgb) == 4 else (*rgb, alpha)

    def panel(self, box, *, radius=14, fill="panel", alpha=232, edge="panel_edge",
              edge_alpha=255, width=1):
        self.d.rounded_rectangle(box, radius=radius, fill=self.c(fill, alpha),
                                 outline=self.c(edge, edge_alpha), width=width)

    def text(self, xy, s, *, font, fill="text", alpha=255, anchor="la"):
        self.d.text(xy, s, font=font, fill=self.c(fill, alpha), anchor=anchor)

    def dot(self, xy, r, fill, alpha=255):
        x, y = xy
        self.d.ellipse((x - r, y - r, x + r, y + r), fill=self.c(fill, alpha))

    def diamond(self, xy, r, fill, alpha=255):
        x, y = xy
        self.d.polygon([(x, y - r), (x + r, y), (x, y + r), (x - r, y)],
                       fill=self.c(fill, alpha))

    def pill(self, xy, text, *, font, color="accent", pad=14, height=30, alpha=48):
        x, y = xy
        w = _text_width(font, text) + pad * 2
        self.d.rounded_rectangle((x, y, x + w, y + height), radius=height // 2,
                                 fill=self.c(color, alpha), outline=self.c(color))
        self.d.text((x + w / 2, y + height / 2), text, font=font, fill=self.c(color),
                    anchor="mm")
        return w


def _brand(cv: _Canvas, run_id: str, command: str, subtitle: str | None = None):
    cv.d.rectangle((0, 0, CANVAS[0], BRAND_H), fill=cv.c("bg", 236))
    cv.d.rectangle((0, BRAND_H - 1, CANVAS[0], BRAND_H), fill=cv.c("panel_edge"))
    cv.diamond((40, BRAND_H // 2), 10, "accent")
    cv.text((62, 20), "AI ENGINEERING FRAMEWORK", font=_font("sans_bold", 18), fill="text")
    cv.text((62, 44), subtitle or f"/{command} --demo", font=_font("mono", 13), fill="muted")
    cv.dot((1780, BRAND_H // 2), 6, "red")
    cv.text((1796, BRAND_H // 2), "REC", font=_font("sans_bold", 14), fill="red", anchor="lm")
    cv.text((1760, BRAND_H // 2), run_id, font=_font("mono", 13), fill="muted", anchor="rm")


def _video_frame(cv: _Canvas, rect: tuple):
    """A hairline and a soft glow *around* the hole the footage shows through.

    The glow is stroked, never filled: a filled rounded rectangle behind the window would
    lay a translucent wash over the whole capture, which is the one part of the frame that
    must reach the viewer untouched.
    """
    x, y, w, h = rect
    glow = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.rounded_rectangle((x - 9, y - 9, x + w + 9, y + h + 9), radius=18,
                         outline=(*cv.t["accent"], 90), width=10)
    blurred = glow.filter(ImageFilter.GaussianBlur(11))
    # Anything the blur spread back over the footage is erased, so the hole stays empty.
    ImageDraw.Draw(blurred).rectangle((x, y, x + w - 1, y + h - 1), fill=(0, 0, 0, 0))
    cv.img.alpha_composite(blurred)
    cv.d.rectangle((x - 1, y - 1, x + w, y + h), outline=cv.c("panel_edge"), width=2)


def _rail(cv: _Canvas, proj: dict, active_phase: str | None):
    x, y, w, h = RAIL_BOX
    cv.panel((x, y, x + w, y + h), alpha=222)
    cv.d.rectangle((x + 16, y + 18, x + 19, y + 30), fill=cv.c("accent"))
    cv.text((x + 28, y + 16), "WORKFLOW", font=_font("sans_bold", 14), fill="muted")
    done, total = _phase_progress(proj)
    cv.text((x + w - 16, y + 16), f"{done}/{total}", font=_font("mono", 12), fill="muted",
            anchor="ra")

    phases = proj.get("phases") or []
    gates = proj.get("gates") or {}
    n_gates = sum(len(p.get("gates") or []) for p in phases)
    avail = h - 56
    ph_h, g_h = 52, 32
    need = len(phases) * ph_h + n_gates * g_h
    if need > avail and need:
        k = avail / need
        ph_h, g_h = max(26, int(ph_h * k)), max(18, int(g_h * k))
    cy = y + 48
    for p in phases:
        col = STATUS_COLOR.get(p["status"], "dim")
        active = p["state_id"] == active_phase
        if active:
            cv.d.rounded_rectangle((x + 8, cy - 4, x + w - 8, cy + ph_h - 10), radius=8,
                                   fill=cv.c("accent", 30))
            cv.d.rectangle((x + 8, cy - 4, x + 11, cy + ph_h - 10), fill=cv.c("accent"))
        mid = cy + (ph_h - 14) // 2
        cv.dot((x + 30, mid), 6 if p["status"] == "completed" else 5, col,
               alpha=255 if p["status"] != "pending" else 150)
        f = _font("mono_bold" if active else "mono", 16)
        cv.text((x + 46, mid), f"{p['index']}  {_ellipsize(f, p['state_id'], w - 80)}",
                font=f, fill="text" if p["status"] != "pending" else "muted", anchor="lm")
        owner = _short_agent(p.get("owner"), 24)
        if owner:
            cv.text((x + 46, mid + 15), owner, font=_font("mono", 12),
                    fill=col if p["status"] != "pending" else "dim")
        if (p.get("attempt") or 0) > 1:
            cv.text((x + w - 16, mid + 13), f"attempt {p['attempt']}", font=_font("mono", 10),
                    fill="amber", anchor="ra")
        cy += ph_h
        for gname in p.get("gates") or []:
            g = gates.get(gname) or {}
            gs = g.get("status", "undecided")
            gcol = GATE_COLOR.get(gs, "dim")
            gm = cy + (g_h - 10) // 2
            cv.diamond((x + 58, gm), 5, gcol, alpha=255 if gs != "undecided" else 150)
            cv.text((x + 72, gm), _ellipsize(_font("mono", 14), gname, w - 150),
                    font=_font("mono", 14), fill="muted" if gs == "undecided" else "text",
                    anchor="lm")
            if g.get("decision"):
                cv.text((x + w - 16, gm), g["decision"], font=_font("mono", 11), fill=gcol,
                        anchor="rm")
            elif gs == "awaiting":
                cv.text((x + w - 16, gm), "awaiting", font=_font("mono", 11), fill=gcol,
                        anchor="rm")
            cy += g_h


def _lower_third(cv: _Canvas, scene: dict, speed: float | None, thinking_phase: float | None,
                 caption: str | None = None):
    x, y, w, h = LOWER_BOX
    cv.panel((x, y, x + w, y + h), alpha=236)
    kind = scene.get("kind", "note")
    col = KIND_COLOR.get(kind, "muted")
    label = KIND_LABEL.get(kind, kind.upper())
    cv.d.rounded_rectangle((x, y + 18, x + 4, y + h - 18), radius=2, fill=cv.c(col))
    lf = _font("sans_bold", 14)
    pill_w = cv.pill((x + 24, y + 16), label, font=lf, color=col)

    if thinking_phase is not None:
        # three dots breathing in sequence: the film's own "the agent is working" tell
        bx = x + 24 + pill_w + 16
        for i in range(3):
            a = 0.35 + 0.65 * (0.5 + 0.5 * math.sin(thinking_phase * 2 * math.pi - i * 0.9))
            cv.dot((bx + i * 14, y + 35), 4, "accent", alpha=int(255 * a))

    if speed and speed > 1.2:
        sf = _font("sans_bold", 15)
        txt = f"{speed:.0f}x  ·  condensed"
        sw = _text_width(sf, txt) + 62
        bx0 = x + w - 24 - sw
        cv.d.rounded_rectangle((bx0, y + 20, x + w - 24, y + 50), radius=15,
                               fill=cv.c("amber", 46), outline=cv.c("amber"))
        # Drawn rather than typed: the fast-forward glyph is missing from most UI fonts and
        # renders as two empty boxes, which is worse than no icon at all.
        for i in range(2):
            px = bx0 + 20 + i * 11
            cv.d.polygon([(px, y + 28), (px + 8, y + 35), (px, y + 42)], fill=cv.c("amber"))
        cv.d.text((bx0 + 44 + (sw - 62) / 2, y + 35), txt, font=sf, fill=cv.c("amber"),
                  anchor="mm")

    title = scene.get("title") or ""
    if caption:
        # With a caption on screen the beat's name becomes the smaller of the two: the
        # sentence being spoken is what the viewer is actually following, so it gets the
        # size, the contrast, and the room for two lines.
        tf = _font("sans_bold", 27)
        cv.text((x + 24, y + 56), _ellipsize(tf, title, w - 60), font=tf, fill="muted")
        cf = _font("sans", 31)
        cy = y + 100
        for line in wrap_caption(caption)[:2]:
            cv.text((x + 24, cy), _ellipsize(cf, line, w - 60), font=cf, fill="white")
            cy += 42
        return
    tf = _font("sans_bold", 40)
    cv.text((x + 24, y + 76), _ellipsize(tf, title, w - 60), font=tf, fill="text")
    sub = scene.get("subtitle") or ""
    if sub:
        sf = _font("mono", 19)
        cv.text((x + 24, y + 132), _ellipsize(sf, sub, w - 60), font=sf, fill="muted")


def _progress(cv: _Canvas, fraction: float):
    y = CANVAS[1] - PROGRESS_H
    cv.d.rectangle((0, y, CANVAS[0], CANVAS[1]), fill=cv.c("panel_edge", 180))
    w = max(0, min(1.0, fraction)) * CANVAS[0]
    if w > 0:
        cv.d.rectangle((0, y, w, CANVAS[1]), fill=cv.c("accent"))


def wrap_caption(text: str) -> list:
    from .captions import wrap_cue
    return wrap_cue(text, CAPTION_WIDTH)


def beat_overlay(scene: dict, *, run_id: str, command: str, capture_size: tuple,
                 fraction: float, speed: float | None = None,
                 thinking_phase: float | None = None, theme: str = "midnight",
                 brand_subtitle: str | None = None, caption: str | None = None):
    """The full-canvas RGBA overlay for one edited segment."""
    cv = _Canvas(theme)
    _brand(cv, run_id, command, brand_subtitle)
    _video_frame(cv, _video_target(capture_size))
    _rail(cv, scene.get("projection") or {}, scene.get("phase"))
    _lower_third(cv, scene, speed, thinking_phase, caption)
    _progress(cv, fraction)
    return cv.img


def title_card(title: str, subtitle: str, bullets: list, *, theme: str = "midnight",
               kicker: str = "AI ENGINEERING FRAMEWORK"):
    """The opening card: an opaque full frame, used as its own clip."""
    cv = _Canvas(theme)
    cv.d.rectangle((0, 0, *CANVAS), fill=cv.c("bg"))
    glow = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((260, -420, 1660, 620), fill=(*cv.t["accent"], 30))
    cv.img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(150)))
    cv.diamond((300, 330), 13, "accent")
    cv.text((326, 318), kicker, font=_font("sans_bold", 20), fill="muted")
    # The title is never cut: the type size steps down until the whole of it fits in three
    # lines. A demo whose own title card is truncated undermines everything after it.
    for size in (66, 58, 50, 44, 38, 32):
        tf = _font("sans_bold", size)
        lines = _wrap(tf, title, 1340)
        if len(lines) <= 3:
            break
    lines = lines[:3]
    y = 388 - (len(lines) - 2) * 20
    for ln in lines:
        cv.text((300, y), ln, font=tf, fill="white")
        y += int(size * 1.18)
    cv.d.rectangle((300, y + 16, 380, y + 20), fill=cv.c("accent"))
    cv.text((300, y + 46), subtitle, font=_font("mono", 20), fill="accent")
    by = y + 110
    for b in bullets[:4]:
        cv.dot((308, by + 10), 4, "muted")
        cv.text((328, by), b, font=_font("sans", 23), fill="muted")
        by += 44
    return cv.img


def outro_card(title: str, stats: list, footer: str, *, theme: str = "midnight"):
    """The closing card: the run's own numbers, in a grid."""
    cv = _Canvas(theme)
    cv.d.rectangle((0, 0, *CANVAS), fill=cv.c("bg"))
    glow = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((360, 520, 1560, 1360), fill=(*cv.t["green"], 26))
    cv.img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(150)))
    cv.diamond((300, 286), 13, "green")
    cv.text((326, 274), "RUN COMPLETE", font=_font("sans_bold", 20), fill="green")
    cv.text((300, 330), title, font=_font("sans_bold", 54), fill="white")
    x, y = 300, 470
    for i, (value, label) in enumerate(stats[:6]):
        col = x + (i % 3) * 448
        row = y + (i // 3) * 170
        cv.panel((col, row, col + 400, row + 138), alpha=200)
        cv.text((col + 28, row + 28), str(value), font=_font("sans_bold", 42), fill="accent")
        cv.text((col + 28, row + 90), label, font=_font("mono", 15), fill="muted")
    cv.text((300, 900), footer, font=_font("mono", 17), fill="muted")
    return cv.img


def save(img, path: Path) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(str(path), format="PNG", optimize=False)
    return path
