"""The headless dashboard -- one DashboardState, two renderings.

The runtime is a CLI that exits between commands, so there is no live screen to capture.
Instead the Recorder's projection (phases, gates, current agent, counters, event log) is
rendered on demand:

    render_text(proj, marker, title=...)          -> str      ASCII dashboard, ~110 columns.
                                                              Stored on every marker as
                                                              `visual_snapshot_text`; also the
                                                              HTML deck's frame when Pillow is
                                                              absent.
    render_frame(proj, marker, scene, t, opts)    -> Image    one 16:9 PNG frame at scene
                                                              progress t in [0, 1], with the
                                                              animated "Agent Thinking..." bar,
                                                              the title overlay, spinner, and
                                                              token meter. Requires Pillow.

Both draw from the same projection, so the text snapshot on a marker and the video frame
built from it always agree. The text renderer is ASCII-only on purpose: snapshots are printed
by `demo --status` on consoles whose code page may not carry box-drawing glyphs.
"""

from __future__ import annotations

import math
import textwrap
from pathlib import Path

try:  # Pillow is optional; every caller checks `has_pil()` before asking for a frame.
    from PIL import Image, ImageDraw, ImageFont
    _PIL = True
except Exception:  # noqa: BLE001
    Image = ImageDraw = ImageFont = None  # type: ignore
    _PIL = False


def has_pil() -> bool:
    return _PIL


# ------------------------------------------------------------------ theme

THEMES = {
    "midnight": {
        "bg": (11, 15, 25), "panel": (18, 26, 43), "panel_edge": (36, 48, 72),
        "text": (229, 231, 235), "muted": (148, 163, 184), "dim": (71, 85, 105),
        "accent": (56, 189, 248), "green": (34, 197, 94), "amber": (245, 158, 11),
        "red": (239, 68, 68), "purple": (167, 139, 250), "pink": (244, 114, 182),
        "white": (255, 255, 255), "black": (0, 0, 0),
    },
}

STATUS_COLOR = {
    "pending": "dim", "leased": "accent", "running": "accent", "reported": "purple",
    "completed": "green", "rejected": "amber", "retrying": "amber", "blocked": "amber",
    "superseded": "muted", "failed": "red",
}
GATE_COLOR = {"undecided": "dim", "awaiting": "amber", "approved": "green",
              "rejected": "red", "decided": "green"}

STATUS_ICON_TEXT = {
    "pending": "[ ]", "leased": "[>]", "running": "[>]", "reported": "[~]",
    "completed": "[=]", "rejected": "[!]", "retrying": "[~]", "blocked": "[!]",
    "superseded": "[<]", "failed": "[x]",
}
GATE_ICON_TEXT = {"undecided": "< >", "awaiting": "<?>", "approved": "<*>",
                  "rejected": "<!>", "decided": "<*>"}

ACTION_LABEL = {
    "run_initialized": "RUN ACCEPTED", "work_item_enqueued": "ROUTING",
    "context_hydrated": "HYDRATING", "dispatch": "DISPATCHING", "prompt_exchange": "THINKING",
    "agent_response": "RESPONDED", "validation": "VALIDATING", "retry": "RETRYING",
    "rollback": "ROLLING BACK", "gate_awaiting": "AWAITING GATE", "gate_decision": "GATE",
    "phase_blocked": "BLOCKED", "phase_unblocked": "UNBLOCKED", "tool_invocation": "TOOL",
    "aggregation": "AGGREGATING", "run_completed": "COMPLETED", "run_aborted": "ABORTED",
    "note": "NOTE",
}


def _clock(ms: int) -> str:
    s = max(0, ms) // 1000
    h, rem = divmod(s, 3600)
    m, sec = divmod(rem, 60)
    return f"{h:d}:{m:02d}:{sec:02d}"


def _k(n) -> str:
    if not n:
        return "0"
    n = int(n)
    return f"{n / 1000:.1f}k" if n >= 1000 else str(n)


def _short_agent(name: str | None, width: int = 18) -> str:
    if not name:
        return ""
    name = str(name)
    for prefix in ("runtime:", "human:", "host:"):
        if name.startswith(prefix):
            name = name[len(prefix):]
    return name[:width]


def _phase_progress(proj: dict) -> tuple[int, int]:
    phases = proj.get("phases") or []
    done = sum(1 for p in phases if p["status"] == "completed")
    return done, len(phases)


# ------------------------------------------------------------------ text rendering


def render_text(proj: dict, marker: dict | None = None, *, title: str = "",
                width: int = 110) -> str:
    """ASCII dashboard. Deterministic for a given projection; no clock is read."""
    width = max(96, width)
    left_w = 50
    right_w = width - left_w - 3
    cur = proj.get("current") or {}
    t_ms = (marker or {}).get("t_ms", 0)
    done, total = _phase_progress(proj)

    def clip(s: str, w: int) -> str:
        s = "" if s is None else str(s)
        return s if len(s) <= w else s[: w - 1] + "~"

    def row(l: str, r: str) -> str:
        return f"| {clip(l, left_w - 2):<{left_w - 2}} | {clip(r, right_w - 2):<{right_w - 2}} |"

    bar = "+" + "-" * (width - 2) + "+"
    split = "+" + "-" * left_w + "+" + "-" * (right_w + 1) + "+"
    head_l = f"AI ENGINEERING FRAMEWORK  /{proj.get('command_id') or 'implement'} --demo   " \
             f"{proj.get('run_id', '')}  {proj.get('workflow_id') or ''}"
    head_r = f"REC  T+{_clock(t_ms)}"
    header = f"| {clip(head_l, width - 4 - len(head_r) - 2):<{width - 4 - len(head_r) - 2}}" \
             f"  {head_r} |"

    # left column: pipeline
    left = ["WORKFLOW PIPELINE" + (f"   {clip(title, left_w - 22)}" if title else "")]
    for p in proj.get("phases") or []:
        icon = STATUS_ICON_TEXT.get(p["status"], "[ ]")
        owner = _short_agent(p.get("owner"), 16)
        line = f"{icon} {p['index']} {clip(p['state_id'], 27):<27} {owner}"
        left.append(line)
        for gname in p.get("gates") or []:
            g = (proj.get("gates") or {}).get(gname) or {}
            gi = GATE_ICON_TEXT.get(g.get("status", "undecided"), "< >")
            note = ""
            if g.get("decision"):
                note = f"{g['decision']} by {_short_agent(g.get('owner_role'), 14)}"
            elif g.get("status") == "awaiting":
                note = "awaiting decision"
            left.append(f"    {gi} {clip(gname, 22):<22} {note}")

    # right column: activity + event stream
    right = ["AGENT ACTIVITY"]
    label = ACTION_LABEL.get((marker or {}).get("action_type", ""), str(cur.get("action", "")).upper())
    agent = _short_agent(cur.get("agent"), 24)
    right.append(f"{agent:<24} {label:<14} {('phase ' + str(cur.get('phase'))) if cur.get('phase') else ''}")
    detail = str(cur.get("detail") or "")
    for ln in textwrap.wrap(detail, right_w - 2)[:5]:
        right.append(ln)
    while len(right) < 7:
        right.append("")
    right.append("-" * (right_w - 2))
    right.append("EVENT STREAM")
    for ln in (proj.get("log") or [])[-8:]:
        right.append(ln)

    n = max(len(left), len(right))
    left += [""] * (n - len(left))
    right += [""] * (n - len(right))

    thinking = bool(cur.get("thinking"))
    spinner = "(*)" if thinking else ("(v)" if proj.get("finished") == "run_completed" else "( )")
    status = "Agent Thinking..." if thinking else \
        ("Run completed" if proj.get("finished") == "run_completed" else str(cur.get("action", "idle")))
    filled = int(round(20 * done / total)) if total else 0
    prog = "[" + "#" * filled + "-" * (20 - filled) + "]"
    tok = proj.get("tokens") or {}
    ctx = f"ctx ~{_k(tok.get('prompt_est'))} tok est"
    if tok.get("legacy_est"):
        ctx += f" (legacy ~{_k(tok.get('legacy_est'))})"
    cnt = proj.get("counters") or {}
    foot = f"{spinner} {status:<22} phases {done}/{total} {prog}   {ctx}   " \
           f"tools {cnt.get('tool_calls', 0)}  retries {cnt.get('retries', 0)}"
    footer = f"| {clip(foot, width - 4):<{width - 4}} |"

    lines = [bar, header, split]
    lines += [row(l, r) for l, r in zip(left, right)]
    lines += [split, footer, bar]
    return "\n".join(lines)


# ------------------------------------------------------------------ PNG rendering

_FONT_CANDIDATES = {
    "mono": [
        "C:/Windows/Fonts/CascadiaMono.ttf", "C:/Windows/Fonts/consola.ttf",
        "/System/Library/Fonts/Menlo.ttc", "/System/Library/Fonts/Monaco.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf",
    ],
    "mono_bold": [
        "C:/Windows/Fonts/consolab.ttf", "C:/Windows/Fonts/CascadiaMono.ttf",
        "/System/Library/Fonts/Menlo.ttc",
        "/usr/share/fonts/truetype/dejavu/DejaVuSansMono-Bold.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationMono-Bold.ttf",
    ],
    "sans": [
        "C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/arial.ttf",
        "/System/Library/Fonts/SFNS.ttf", "/System/Library/Fonts/Helvetica.ttc",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
    ],
    "sans_bold": [
        "C:/Windows/Fonts/segoeuib.ttf", "C:/Windows/Fonts/arialbd.ttf",
        "/System/Library/Fonts/SFNS.ttf", "/System/Library/Fonts/Helvetica.ttc",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
    ],
}
_FONT_CACHE: dict = {}


def _font(kind: str, size: int):
    key = (kind, size)
    if key in _FONT_CACHE:
        return _FONT_CACHE[key]
    font = None
    for cand in _FONT_CANDIDATES.get(kind, []):
        if Path(cand).exists():
            try:
                font = ImageFont.truetype(cand, size)
                break
            except OSError:
                continue
    if font is None:
        try:
            font = ImageFont.load_default(size=size)
        except TypeError:          # Pillow < 10.1 has a fixed-size default
            font = ImageFont.load_default()
    _FONT_CACHE[key] = font
    return font


def _text_width(font, text: str) -> float:
    try:
        return font.getlength(text)
    except AttributeError:
        return font.getsize(text)[0]


def _wrap(font, text: str, max_px: int) -> list:
    if not text:
        return []
    unit = max(1.0, _text_width(font, "M"))
    cols = max(8, int(max_px / unit))
    return textwrap.wrap(str(text), cols)


def _ellipsize(font, text: str, max_px: int) -> str:
    text = "" if text is None else str(text)
    if _text_width(font, text) <= max_px:
        return text
    while text and _text_width(font, text + "…") > max_px:
        text = text[:-1]
    return text + "…"


class _Painter:
    """Thin helper over ImageDraw with the theme resolved."""

    def __init__(self, img, theme: dict):
        self.img = img
        self.d = ImageDraw.Draw(img, "RGBA")
        self.t = theme

    def c(self, name, alpha: int = 255):
        rgb = self.t[name] if isinstance(name, str) else tuple(name)
        return rgb if len(rgb) == 4 else (*rgb, alpha)

    def panel(self, box, *, radius: int = 12, fill="panel", edge="panel_edge"):
        self.d.rounded_rectangle(box, radius=radius, fill=self.c(fill), outline=self.c(edge))

    def text(self, xy, s, *, font, fill="text", anchor="la"):
        self.d.text(xy, s, font=font, fill=self.c(fill), anchor=anchor)

    def label(self, xy, s, *, font, fill="accent"):
        """Small uppercase section label with a leading accent bar."""
        x, y = xy
        self.d.rectangle((x, y + 2, x + 3, y + 14), fill=self.c(fill))
        self.text((x + 10, y), s.upper(), font=font, fill="muted")

    def dot(self, xy, r, fill, *, outline=None):
        x, y = xy
        self.d.ellipse((x - r, y - r, x + r, y + r), fill=self.c(fill),
                       outline=self.c(outline) if outline else None)

    def diamond(self, xy, r, fill):
        x, y = xy
        self.d.polygon([(x, y - r), (x + r, y), (x, y + r), (x - r, y)], fill=self.c(fill))

    def bar(self, box, frac, *, fill="accent", track="panel_edge", radius: int = 5):
        x0, y0, x1, y1 = box
        self.d.rounded_rectangle(box, radius=radius, fill=self.c(track))
        w = max(0, min(1.0, frac)) * (x1 - x0)
        if w > 0:
            self.d.rounded_rectangle((x0, y0, x0 + w, y1), radius=radius, fill=self.c(fill))

    def spinner(self, xy, r, angle_deg, *, fill="accent", width: int = 4):
        x, y = xy
        self.d.arc((x - r, y - r, x + r, y + r), start=angle_deg, end=angle_deg + 260,
                   fill=self.c(fill), width=width)


def render_frame(proj: dict, marker: dict | None, scene: dict, t: float, *, title: str = "",
                 width: int = 1280, height: int = 720, theme: str = "midnight",
                 frame_index: int = 0, fps: int = 12):
    """One frame of the dashboard at progress `t` of `scene`. Requires Pillow.

    `scene` carries: title, subtitle, kind, thinking, t_ms (wall clock at the scene's start),
    progress (0..1 over the presentation). Animation is a pure function of (frame_index, t),
    so re-rendering a frame produces identical bytes.
    """
    if not _PIL:
        raise RuntimeError("Pillow is not installed; PNG frames are unavailable")
    th = THEMES.get(theme, THEMES["midnight"])
    img = Image.new("RGB", (width, height), th["bg"])
    P = _Painter(img, th)
    sx, sy = width / 1280.0, height / 720.0

    def X(v): return int(v * sx)
    def Y(v): return int(v * sy)
    def F(kind, size): return _font(kind, max(8, int(size * min(sx, sy))))

    cur = proj.get("current") or {}
    thinking = bool(scene.get("thinking", cur.get("thinking")))
    done, total = _phase_progress(proj)
    wall_ms = int(scene.get("t_ms", (marker or {}).get("t_ms", 0)))
    pulse = 0.5 + 0.5 * math.sin(frame_index / max(1, fps) * 2 * math.pi * 0.9)

    # -- header ---------------------------------------------------------------
    P.d.rectangle((0, 0, width, Y(3)), fill=P.c("accent"))
    P.diamond((X(36), Y(34)), X(9), "accent")
    P.text((X(54), Y(20)), "AI ENGINEERING FRAMEWORK", font=F("sans_bold", 17), fill="text")
    P.text((X(54), Y(41)), f"/{proj.get('command_id') or 'implement'} --demo   ·   "
                            f"{proj.get('workflow_id') or ''}   ·   {proj.get('run_id', '')}",
           font=F("mono", 13), fill="muted")
    rec_alpha = int(120 + 135 * pulse)
    P.dot((X(1040), Y(34)), X(6), (*th["red"], rec_alpha))
    P.text((X(1052), Y(24)), "REC", font=F("sans_bold", 15), fill="red")
    P.text((X(1256), Y(34)), f"T+{_clock(wall_ms)}", font=F("mono_bold", 20), fill="text",
           anchor="rm")

    # -- left: pipeline -------------------------------------------------------
    lx0, ly0, lx1, ly1 = X(24), Y(80), X(520), Y(640)
    P.panel((lx0, ly0, lx1, ly1))
    P.label((lx0 + X(16), ly0 + Y(12)), "Workflow pipeline", font=F("sans_bold", 12))
    if title:
        P.text((lx1 - X(16), ly0 + Y(12)), _ellipsize(F("sans", 12), title, X(260)),
               font=F("sans", 12), fill="muted", anchor="ra")
    phases = proj.get("phases") or []
    gates = proj.get("gates") or {}
    n_gates = sum(len(p.get("gates") or []) for p in phases)
    avail = (ly1 - ly0) - Y(48)
    ph_h = Y(40)
    g_h = Y(26)
    need = len(phases) * ph_h + n_gates * g_h
    if need > avail and need:
        scale = avail / need
        ph_h, g_h = int(ph_h * scale), int(g_h * scale)
    y = ly0 + Y(40)
    active_phase = scene.get("phase") or cur.get("phase")
    for p in phases:
        col = STATUS_COLOR.get(p["status"], "dim")
        if p["state_id"] == active_phase:
            P.d.rounded_rectangle((lx0 + X(8), y - Y(3), lx1 - X(8), y + ph_h - Y(5)),
                                  radius=8, fill=P.c("accent", 26))
        cx, cy = lx0 + X(30), y + ph_h // 2 - Y(4)
        if p["status"] in ("running", "leased") and thinking:
            P.dot((cx, cy), X(8), (*th[col], int(90 + 160 * pulse)))
        P.dot((cx, cy), X(5) if p["status"] not in ("completed",) else X(6), col,
              outline=None if p["status"] != "pending" else "muted")
        name_font = F("mono_bold" if p["state_id"] == active_phase else "mono", 14)
        P.text((lx0 + X(48), cy), f"{p['index']}  {_ellipsize(name_font, p['state_id'], X(280))}",
               font=name_font, fill="text" if p["status"] != "pending" else "muted", anchor="lm")
        owner = _short_agent(p.get("owner"), 22)
        P.text((lx1 - X(18), cy), owner, font=F("mono", 12), fill=col if p["status"] != "pending"
               else "dim", anchor="rm")
        if p.get("attempt", 0) > 1:
            P.text((lx1 - X(18), cy + Y(13)), f"attempt {p['attempt']}", font=F("mono", 10),
                   fill="amber", anchor="rm")
        y += ph_h
        for gname in p.get("gates") or []:
            g = gates.get(gname) or {}
            gs = g.get("status", "undecided")
            gcol = GATE_COLOR.get(gs, "dim")
            gy = y + g_h // 2 - Y(3)
            P.diamond((lx0 + X(58), gy), X(5), gcol)
            P.text((lx0 + X(72), gy), _ellipsize(F("mono", 12), gname, X(190)), font=F("mono", 12),
                   fill="muted" if gs == "undecided" else "text", anchor="lm")
            note = ""
            if g.get("decision"):
                note = f"{g['decision']} · {_short_agent(g.get('owner_role'), 18)}"
            elif gs == "awaiting":
                note = "awaiting human decision"
            if note:
                P.text((lx1 - X(18), gy), _ellipsize(F("mono", 11), note, X(210)),
                       font=F("mono", 11), fill=gcol, anchor="rm")
            y += g_h

    # -- right top: agent activity -------------------------------------------
    rx0, ry0, rx1, ry1 = X(544), Y(80), X(1256), Y(400)
    P.panel((rx0, ry0, rx1, ry1))
    P.label((rx0 + X(16), ry0 + Y(12)), "Agent activity", font=F("sans_bold", 12))
    agent = _short_agent(cur.get("agent"), 30) or "runtime"
    P.text((rx0 + X(24), ry0 + Y(40)), agent, font=F("sans_bold", 26), fill="text")
    label = ACTION_LABEL.get((marker or {}).get("action_type", ""),
                             str(cur.get("action", "")).upper())
    if thinking and label not in ("TOOL",):
        label = "THINKING"
    pill_col = {"THINKING": "accent", "RESPONDED": "purple", "VALIDATING": "green",
                "GATE": "green", "RETRYING": "amber", "BLOCKED": "amber", "ROLLING BACK": "amber",
                "COMPLETED": "green", "ABORTED": "red", "TOOL": "pink"}.get(label, "muted")
    lf = F("sans_bold", 12)
    lw = _text_width(lf, label) + X(20)
    px0 = rx0 + X(24) + _text_width(F("sans_bold", 26), agent) + X(18)
    py = ry0 + Y(50)
    P.d.rounded_rectangle((px0, py, px0 + lw, py + Y(22)), radius=11, fill=P.c(pill_col, 40),
                          outline=P.c(pill_col))
    P.text((px0 + lw / 2, py + Y(11)), label, font=lf, fill=pill_col, anchor="mm")
    meta = []
    if cur.get("phase"):
        meta.append(f"phase {cur['phase']}")
    ph = next((p for p in phases if p["state_id"] == cur.get("phase")), None)
    if ph and ph.get("attempt"):
        meta.append(f"attempt {ph['attempt']}")
    pl = (marker or {}).get("payload") or {}
    if pl.get("prompt_tokens_est"):
        meta.append(f"~{_k(pl['prompt_tokens_est'])} tokens in")
    if pl.get("response_tokens_est"):
        meta.append(f"~{_k(pl['response_tokens_est'])} tokens out")
    if pl.get("agent_version"):
        meta.append(f"v{pl['agent_version']}")
    P.text((rx0 + X(24), ry0 + Y(80)), "   ·   ".join(meta), font=F("mono", 12), fill="muted")
    detail = str(cur.get("detail") or "")
    df = F("mono", 13)
    lines = _wrap(df, detail, (rx1 - rx0) - X(48))[:9]
    ty = ry0 + Y(108)
    lh = Y(21)
    # typewriter reveal while the agent thinks: characters land over the first half of the
    # scene; a response or a decision lands at once.
    reveal = 1.0 if not thinking else min(1.0, t / 0.55)
    total_chars = sum(len(ln) for ln in lines) or 1
    budget = int(total_chars * reveal)
    for ln in lines:
        if budget <= 0:
            break
        shown = ln[:budget]
        budget -= len(ln)
        P.text((rx0 + X(24), ty), shown, font=df, fill="text" if not thinking else "accent")
        ty += lh
    if thinking and budget > -1 and lines:
        # caret
        if int(frame_index / max(1, fps) * 3) % 2 == 0:
            cw = _text_width(df, "M")
            last_shown = lines[min(len(lines) - 1, max(0, len(lines) - 1))]
            P.d.rectangle((rx0 + X(24) + _text_width(df, last_shown[:max(0, len(last_shown))]),
                           ty - lh + Y(3), rx0 + X(24) + _text_width(df, last_shown) + cw,
                           ty - Y(3)), fill=P.c("accent", 160))

    # -- right bottom: event stream -------------------------------------------
    ex0, ey0, ex1, ey1 = X(544), Y(416), X(1256), Y(640)
    P.panel((ex0, ey0, ex1, ey1))
    P.label((ex0 + X(16), ey0 + Y(12)), "Event stream", font=F("sans_bold", 12))
    cnt = proj.get("counters") or {}
    P.text((ex1 - X(16), ey0 + Y(12)),
           f"{cnt.get('invocations', 0)} invocations · {cnt.get('validations_passed', 0)} passed"
           f" · {cnt.get('retries', 0)} retries · {cnt.get('tool_calls', 0)} tool calls",
           font=F("mono", 11), fill="muted", anchor="ra")
    ef = F("mono", 12)
    logs = (proj.get("log") or [])[-8:]
    ly = ey0 + Y(40)
    for i, ln in enumerate(logs):
        newest = i == len(logs) - 1
        P.text((ex0 + X(24), ly), _ellipsize(ef, ln, (ex1 - ex0) - X(48)), font=ef,
               fill="text" if newest else "muted")
        ly += Y(22)

    # -- footer: the "Agent Thinking..." status bar ---------------------------
    fx0, fy0, fx1, fy1 = X(24), Y(656), X(1256), Y(708)
    P.panel((fx0, fy0, fx1, fy1), radius=10)
    cy = (fy0 + fy1) // 2
    if thinking:
        angle = (frame_index * (360.0 / max(1, fps)) * 1.4) % 360
        P.spinner((fx0 + X(30), cy), X(10), angle, fill="accent", width=max(2, X(3)))
        dots = "." * (1 + int(frame_index / max(1, fps) * 3) % 3)
        P.text((fx0 + X(52), cy), f"Agent Thinking{dots}", font=F("sans_bold", 17),
               fill="accent", anchor="lm")
        status_w = X(240)
    elif proj.get("finished") == "run_completed":
        P.dot((fx0 + X(30), cy), X(9), "green")
        P.text((fx0 + X(52), cy), "Run completed", font=F("sans_bold", 17), fill="green",
               anchor="lm")
        status_w = X(240)
    else:
        P.dot((fx0 + X(30), cy), X(7), "muted")
        P.text((fx0 + X(52), cy), str(cur.get("action") or "idle").capitalize(),
               font=F("sans_bold", 16), fill="muted", anchor="lm")
        status_w = X(240)
    bx0 = fx0 + status_w + X(40)
    bx1 = bx0 + X(300)
    P.text((bx0, cy - Y(14)), f"phases {done}/{total}", font=F("mono", 11), fill="muted")
    P.bar((bx0, cy + Y(2), bx1, cy + Y(12)), (done / total) if total else 0.0, fill="green")
    tok = proj.get("tokens") or {}
    tx0 = bx1 + X(48)
    tx1 = fx1 - X(24)
    prompt_est, legacy = tok.get("prompt_est") or 0, tok.get("legacy_est") or 0
    frac = (prompt_est / legacy) if legacy else (1.0 if prompt_est else 0.0)
    ctx_label = f"context ~{_k(prompt_est)} tokens est"
    if legacy:
        ctx_label += f"   (legacy rules ~{_k(legacy)}, -{max(0, int(100 - 100 * frac))}%)"
    P.text((tx0, cy - Y(14)), ctx_label, font=F("mono", 11), fill="muted")
    P.bar((tx0, cy + Y(2), tx1, cy + Y(12)), frac, fill="purple")

    # -- title overlay ---------------------------------------------------------
    hold = scene.get("overlay_until", 0.38)
    if scene.get("title") and t <= hold:
        fade_in, fade_out = 0.08, 0.10
        if t < fade_in:
            a = t / fade_in
        elif t > hold - fade_out:
            a = max(0.0, (hold - t) / fade_out)
        else:
            a = 1.0
        if a > 0.01:
            tf, sf = F("sans_bold", 34), F("sans", 16)
            ttl = _ellipsize(tf, scene["title"], width - X(160))
            sub = _ellipsize(sf, scene.get("subtitle") or "", width - X(160))
            tw = _text_width(tf, ttl)
            sw = _text_width(sf, sub) if sub else 0
            bw = int(max(tw, sw) + X(80))
            bh = Y(110) if sub else Y(80)
            # Composite only the card's region: a full-frame RGBA pass per frame is the
            # single most expensive thing a renderer can do, and the card is a tenth of it.
            ov = Image.new("RGBA", (bw + 4, bh + 4), (0, 0, 0, 0))
            od = ImageDraw.Draw(ov)
            od.rounded_rectangle((1, 1, bw + 1, bh + 1), radius=16,
                                 fill=(*th["panel"], int(232 * a)),
                                 outline=(*th["accent"], int(255 * a)), width=2)
            od.text((bw // 2 + 1, Y(40) + 1), ttl, font=tf, fill=(*th["white"], int(255 * a)),
                    anchor="mm")
            if sub:
                od.text((bw // 2 + 1, Y(80) + 1), sub, font=sf,
                        fill=(*th["muted"], int(255 * a)), anchor="mm")
            img.paste(ov, ((width - bw) // 2 - 2, Y(250) - 2), ov)

    # progress ticker along the bottom edge
    d2 = ImageDraw.Draw(img)
    prog = float(scene.get("progress", 0.0))
    d2.rectangle((0, height - Y(4), int(width * prog), height), fill=th["accent"])
    return img


def blend(a, b, alpha: float):
    """Crossfade helper for scene transitions. Requires Pillow."""
    return Image.blend(a, b, max(0.0, min(1.0, alpha)))
