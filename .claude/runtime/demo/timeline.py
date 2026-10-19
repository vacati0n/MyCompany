"""Timeline -- marker stream to presentation scenes.

A run takes minutes to hours, most of it waiting on humans at gates; a presentation takes
about ninety seconds. The timeline compresses the marker stream into scenes, each holding
the projection as it stood after the scene's last marker, a title and subtitle for the
overlay, whether the "Agent Thinking..." bar is lit, a narration sentence, and a visual
duration. Durations are proportional to what the scene shows rather than to how long it
took: an agent's four-second think is a four-second scene whether the agent took forty
seconds or forty minutes.

Markers that are bookkeeping rather than story are folded into their neighbours: the burst
of routing events joins the opening card, a frozen context slice joins the dispatch it
precedes, a gate arming or an aggregation joins the scene before it. What remains is one
scene per thing an audience would want said aloud.

Nothing here reads a clock or a file. `build_scenes(markers, options)` is a pure function of
its inputs, so the same recording always cuts to the same film.
"""

from __future__ import annotations

import copy

from .recorder import apply_marker, new_projection

# visual duration per scene kind, milliseconds, before compression towards the target
BASE_MS = {
    "opening": 5200, "materialized": 3600, "context_hydrated": 1500,
    "dispatch": 2600, "prompt_exchange": 4600, "agent_response": 3800, "validation": 3000,
    "retry": 3000, "rollback": 3600, "gate_awaiting": 2000, "gate_decision": 3200,
    "phase_blocked": 2200, "phase_unblocked": 1200, "tool_group": 2600, "aggregation": 2200,
    "run_completed": 6000, "run_aborted": 4200, "note": 1500,
}
MIN_SCENE_MS = 1400            # long enough to read a title card
TOOL_GROUP_SIZE = 5
NOTE_CHARS = 220
ABSORBED = ("gate_awaiting", "phase_blocked", "aggregation")   # fold into the previous scene


def _k(n) -> str:
    if not n:
        return "0"
    n = int(n)
    return f"{n / 1000:.1f}k" if n >= 1000 else str(n)


def _spoken_agent(name: str | None) -> str:
    if not name:
        return "the runtime"
    n = str(name)
    for prefix in ("runtime:", "human:", "host:"):
        if n.startswith(prefix):
            n = n[len(prefix):]
    n = n.replace("-", " ")
    words = {"dev 1 implement": "implementation developer", "dev 2 reviewer": "reviewer",
             "dev 1 bug analyst": "bug analyst", "qa": "Q A"}
    return words.get(n, n)


def _spoken_phase(phase: str | None) -> str:
    return (phase or "").replace("-", " ")


def _first_sentence(text: str | None, limit: int = NOTE_CHARS) -> str:
    if not text:
        return ""
    text = " ".join(str(text).split())
    for sep in (". ", "; ", ": "):
        if sep in text[:limit]:
            head = text[: text.index(sep)]
            if len(head) > 30:
                # Ends the clause as a sentence. Keeping the separator leaves a clipped
                # quotation ending in a semicolon, which a caller then punctuates again --
                # the closing line of a film should not read "behaviour;."
                return head.rstrip(" ,;:") + "."
    text = text if len(text) <= limit else text[: limit - 1].rsplit(" ", 1)[0]
    return text.rstrip(" ,;:") + ("" if text.endswith((".", "!", "?")) else ".")


def _wall(ms: int) -> str:
    s = ms // 1000
    h, rem = divmod(s, 3600)
    m, sec = divmod(rem, 60)
    if h:
        return f"{h} hour{'s' if h != 1 else ''} {m} minute{'s' if m != 1 else ''}"
    if m:
        return f"{m} minute{'s' if m != 1 else ''} {sec} second{'s' if sec != 1 else ''}"
    return f"{sec} second{'s' if sec != 1 else ''}"


def _scene(kind: str, markers: list, proj: dict, **fields) -> dict:
    last = markers[-1]
    s = {
        "kind": kind,
        "marker_ids": [m["marker_id"] for m in markers],
        "marker": last,
        "phase": last.get("phase"),
        "agent": last.get("agent_name"),
        "thinking": False,
        "t_ms": markers[0].get("t_ms", 0),
        "duration_ms": BASE_MS.get(kind, 1500),
        "title": "",
        "subtitle": "",
        "narration": "",        # the full line, spoken when the film has room for it
        "narration_short": "",  # the clipped line, spoken for repeats when it has not
        # The explanatory line: what this moment means, in plain language, for a viewer who
        # has never seen the framework and is not going to read the small print on screen.
        # It is the longest of the three on purpose -- in `explain` mode the narration is
        # the demonstration and the picture is the evidence, so the film is paced to it.
        "narration_long": "",
        "projection": copy.deepcopy(proj),
    }
    s.update(fields)
    if not s["narration_short"]:
        s["narration_short"] = _first_sentence(s["narration"], 90)
    if not s["narration_long"]:
        s["narration_long"] = s["narration"]
    return s


def _spoken_count(n) -> str:
    """A token estimate a listener can follow. "43.4k" is read aloud as gibberish."""
    if not n:
        return "a small amount of"
    n = int(n)
    if n >= 1000:
        return f"roughly {round(n / 1000)} thousand"
    return f"about {n}"


WORDS_PER_SECOND = 2.6          # a measured desktop voice at rate +1; used only to choose
WORDS_PER_SECOND_SLOW = 2.0     # the same voice at the relaxed rate


def speech_seconds(text: str, slow: bool = False) -> float:
    rate = WORDS_PER_SECOND_SLOW if slow else WORDS_PER_SECOND
    return len((text or "").split()) / rate


def choose_narration(scenes: list, target_seconds: float | None,
                     mode: str | None = None) -> str:
    """Decide, per scene, which line is spoken, and write it to `scene["spoken"]`.

    Five modes. `explain` speaks the long, plain-language line at every beat and is never
    trimmed to fit a target, because in that mode the narration *is* the demonstration and
    the film is paced to it rather than the other way round. The other four are tried in
    order until the estimated speech fits about 1.3 times the target: `full` speaks every
    full line; `thinned` keeps the full line for the opening, the close, and the first scene
    of each kind, and the short line for every repeat; `minimal` speaks the short line
    everywhere but the bookends; `sparse` speaks the bookends and the first scene of each
    kind and lets every repeat play silent. Deterministic, and recorded in the manifest.
    """
    budget = (target_seconds or 0) * 1.3
    bookends = {"opening", "run_completed", "run_aborted"}

    def apply(m: str) -> float:
        seen: set = set()
        total = 0.0
        for s in scenes:
            kind = s["kind"]
            if m == "explain":
                # Explain a thing the first time it happens, then simply say what is
                # happening. A viewer told three times over what a dispatch is stops
                # listening, and the film doubles in length for no extra understanding.
                line = (s.get("narration_long") or s["narration"]) \
                    if (kind not in seen or kind in bookends) else s["narration"]
            elif m == "full" or kind in bookends:
                line = s["narration"]
            elif m == "thinned":
                line = s["narration"] if kind not in seen else s["narration_short"]
            elif m == "minimal":
                line = s["narration_short"]
            else:
                line = s["narration"] if kind not in seen else ""
            seen.add(kind)
            s["spoken"] = line
            total += speech_seconds(line, slow=(m == "explain"))
        return total

    if mode == "explain":
        apply("explain")
        return "explain"
    for m in ("full", "thinned", "minimal", "sparse"):
        total = apply(m)
        if not budget or total <= budget:
            return m
    return "sparse"


def _absorb(scene: dict, marker: dict, proj: dict, subtitle_note: str | None = None):
    """Fold a bookkeeping marker into `scene` without changing what the scene is about."""
    scene["marker_ids"].append(marker["marker_id"])
    scene["projection"] = copy.deepcopy(proj)
    if subtitle_note:
        scene["subtitle"] = (scene["subtitle"] + " · " if scene["subtitle"] else "") + subtitle_note


def _attempt(proj: dict, phase: str | None) -> int:
    return next((x["attempt"] for x in proj["phases"] if x["state_id"] == phase), 1) or 1


def build_scenes(markers: list, options: dict | None = None) -> list:
    options = options or {}
    title = options.get("title") or (markers[0]["run_id"] if markers else "demo")
    proj = new_projection(markers[0]["run_id"] if markers else "run")
    scenes: list = []
    hydration: dict | None = None      # a context_hydrated marker waiting for its dispatch
    i, n = 0, len(markers)

    def prev() -> dict | None:
        return scenes[-1] if scenes else None

    while i < n:
        m = markers[i]
        a = m["action_type"]
        p = m.get("payload") or {}

        # -- opening card: run accepted + the routing burst ------------------------------
        if a == "run_initialized":
            group = [m]
            apply_marker(proj, m)
            i += 1
            while i < n and markers[i]["action_type"] == "work_item_enqueued":
                apply_marker(proj, markers[i])
                group.append(markers[i])
                i += 1
            phases = [g for g in group[1:] if (g.get("payload") or {}).get("work_type") != "gate"]
            gates = [g for g in group[1:] if (g.get("payload") or {}).get("work_type") == "gate"]
            owners = []
            for g in phases:
                o = (g.get("payload") or {}).get("owner_agent_id")
                if o and o not in owners:
                    owners.append(o)
            wf = _spoken_phase(p.get("workflow_id"))
            s = _scene("opening", group, proj, phase=None, title=title,
                       subtitle=f"/{p.get('command_id') or 'implement'} --demo  ·  "
                                f"{p.get('workflow_id') or ''}  ·  {len(phases)} phases  ·  "
                                f"{len(gates)} human gates",
                       narration=f"{title}. The framework routes the request through the {wf} "
                                 f"workflow: {len(phases)} phases owned by specialist agents, "
                                 f"and {len(gates)} human gates no agent can bypass. Everything "
                                 f"here was recorded live from the run itself.",
                       narration_long=(
                           f"Welcome. This is a real recording of our A I engineering "
                           f"framework doing a piece of software work by itself. "
                           f"The request it was given is: {title}. "
                           f"It does not hand that to a single assistant. It breaks the work "
                           f"into {len(phases)} stages and gives each one to a different "
                           f"specialist, the way a real team would. You can see them listed "
                           f"down the right hand side. Between those stages there are "
                           f"{len(gates)} checkpoints, and at each one a person has to "
                           f"approve before the work may continue."))
            s["overlay_until"] = 0.72
            scenes.append(s)
            continue

        # -- a routing burst without an opening (demo enabled mid-run) --------------------
        if a == "work_item_enqueued":
            group = []
            while i < n and markers[i]["action_type"] == "work_item_enqueued":
                apply_marker(proj, markers[i])
                group.append(markers[i])
                i += 1
            phases = [g for g in group if (g.get("payload") or {}).get("work_type") != "gate"]
            gates = [g for g in group if (g.get("payload") or {}).get("work_type") == "gate"]
            scenes.append(_scene(
                "materialized", group, proj, phase=None, title="Workflow materialized",
                subtitle=f"{len(phases)} phases · {len(gates)} human gates",
                narration=f"The task router materialises the workflow: {len(phases)} phases "
                          f"and {len(gates)} human gates.",
                narration_long=(
                    f"The framework has laid out the whole plan of work before starting any "
                    f"of it. You can see it down the right hand side: {len(phases)} stages, "
                    f"each one with the name of the specialist that owns it, and "
                    f"{len(gates)} checkpoints in between where a person has to say yes.")))
            continue

        # -- tool calls from the host, grouped ---------------------------------------------
        if a == "tool_invocation":
            group = []
            while i < n and markers[i]["action_type"] == "tool_invocation" \
                    and len(group) < TOOL_GROUP_SIZE:
                apply_marker(proj, markers[i])
                group.append(markers[i])
                i += 1
            tools = []
            for g in group:
                t = (g.get("payload") or {}).get("tool_name")
                if t and t not in tools:
                    tools.append(t)
            scenes.append(_scene(
                "tool_group", group, proj, thinking=True,
                title=f"{_spoken_agent(group[-1].get('agent_name')).title()} works its tools",
                subtitle=" · ".join(tools[:5]) + f"   ({len(group)} call"
                         f"{'s' if len(group) != 1 else ''})",
                narration=f"The agent works through its tools: {len(group)} call"
                          f"{'s' if len(group) != 1 else ''}, using {', '.join(tools[:4])}.",
                narration_short=f"{len(group)} tool call{'s' if len(group) != 1 else ''}.",
                narration_long=(
                    f"While it works, the assistant reaches for its tools. It is reading "
                    f"files, searching through the project, and making changes, "
                    f"{len(group)} action{'s' if len(group) != 1 else ''} in all. "
                    f"Every one of them is written down as it happens, so afterwards you "
                    f"can see exactly what the assistant touched and when.")))
            continue

        apply_marker(proj, m)
        i += 1

        # -- bookkeeping folded into the previous scene -----------------------------------
        if a in ABSORBED and prev() is not None:
            note = None
            if a == "gate_awaiting":
                note = f"{p.get('gate') or 'gate'} awaits a human decision"
            elif a == "aggregation" and p.get("completed") is not None:
                note = f"package {p.get('completed')}/{p.get('phases')}"
            _absorb(prev(), m, proj, note)
            continue

        if a == "context_hydrated":
            if i < n and markers[i]["action_type"] == "dispatch" \
                    and markers[i].get("phase") == m.get("phase"):
                hydration = m          # joins the dispatch scene that follows
                continue
            scenes.append(_scene(a, [m], proj,
                                 title=f"Context frozen: {_spoken_phase(m.get('phase'))}",
                                 subtitle=f"{p.get('members') or '?'} files digested",
                                 narration=f"The context loader freezes a slice of "
                                           f"{p.get('members') or 'the'} files for "
                                           f"{_spoken_phase(m.get('phase'))}, each with a "
                                           f"content digest.",
                                 narration_long=(
                                     f"Before anyone starts work on the "
                                     f"{_spoken_phase(m.get('phase'))} stage, the framework "
                                     f"writes down which documents the specialist is allowed "
                                     f"to read, and takes a fingerprint of each one, so "
                                     f"there is a permanent record of what it was looking "
                                     f"at.")))
            continue

        if a == "dispatch":
            agent = p.get("agent_id") or "the agent"
            group = ([hydration] if hydration else []) + [m]
            hp = (hydration or {}).get("payload") or {}
            hydration = None
            att = _attempt(proj, m.get("phase"))
            sub = [agent]
            if att > 1:
                sub.append(f"attempt {att} · repair pass")
            if hp.get("members"):
                sub.append(f"{hp['members']} files frozen")
            if p.get("prompt_tokens_est"):
                sub.append(f"~{_k(p['prompt_tokens_est'])} tokens of context")
                if p.get("savings_pct"):
                    sub.append(f"{p['savings_pct']}% below the legacy load")
            if p.get("skills_required"):
                sub.append("skills " + ", ".join(p["skills_required"][:4]))
            nar = f"The runtime freezes " \
                  f"{str(hp['members']) + ' context files' if hp.get('members') else 'the context'}" \
                  f" with digests and leases {_spoken_phase(m.get('phase'))} to the " \
                  f"{_spoken_agent(agent)} agent"
            if att > 1:
                nar += f", attempt {att}, as a repair pass over the rejected checks"
            if p.get("prompt_tokens_est"):
                nar += f", about {_k(p['prompt_tokens_est'])} tokens of context"
            short = f"Dispatching {_spoken_phase(m.get('phase'))} to the {_spoken_agent(agent)}" \
                    + (f", attempt {att}." if att > 1 else ".")
            files = hp.get("members")
            long = (f"The framework is now handing the {_spoken_phase(m.get('phase'))} stage "
                    f"to the {_spoken_agent(agent)}. "
                    f"Before it does, it writes down exactly which documents that specialist "
                    f"is allowed to read")
            long += (f" — {files} of them — " if files else " ") + \
                    ("and takes a fingerprint of each one, so there is a record of what it "
                     "was looking at that cannot be changed afterwards. ")
            if p.get("prompt_tokens_est"):
                long += (f"It is given {_spoken_count(p['prompt_tokens_est'])} words to work "
                         f"from, and deliberately nothing more. ")
            if att > 1:
                long += (f"This is attempt number {att}. The first attempt was rejected, and "
                         f"the specialist is being given the list of what was wrong with it "
                         f"so it can repair its own work. ")
            scenes.append(_scene(a, group, proj,
                                 title=f"Handing the work to the {_spoken_agent(agent)}",
                                 subtitle="  ·  ".join(sub), narration=nar + ".",
                                 narration_short=short, narration_long=long.strip()))
            continue

        if a == "prompt_exchange":
            agent = m.get("agent_name")
            sub = [f"attempt {_attempt(proj, m.get('phase'))}"]
            if p.get("prompt_bytes"):
                sub.append(f"dispatch prompt {p['prompt_bytes'] / 1024:.1f} KB")
            scenes.append(_scene(a, [m], proj, thinking=True,
                                 title=f"{_spoken_agent(agent).title()} is thinking",
                                 subtitle=" · ".join(sub),
                                 narration=f"The {_spoken_agent(agent)} agent reads its dispatch "
                                           f"prompt and reasons over "
                                           f"{_spoken_phase(m.get('phase'))}, loading its "
                                           f"operating modules in declared order first.",
                                 narration_short=f"The {_spoken_agent(agent)} is thinking.",
                                 narration_long=(
                                     f"The {_spoken_agent(agent)} is now working, and this "
                                     f"is the part that takes real time. It reads its "
                                     f"standing instructions, then the documents it was just "
                                     f"given, and only then writes its answer. You can watch "
                                     f"it happen on the screen.")))
            continue

        if a == "agent_response":
            agent = m.get("agent_name")
            refs = p.get("artifact_refs") or []
            # An artifact reference is normally a path string, but an agent may declare it
            # as an object carrying the path alongside its type. Adopted from a fix made
            # against a live installation, where the object form crashed the cut.
            first = refs[0] if refs else None
            if isinstance(first, dict):
                first = first.get("path") or first.get("reference") or first.get("type") or ""
            art = str(first).rsplit("/", 1)[-1] if refs else "no artifact"
            sub = [f"status {p.get('status') or 'reported'}", art]
            if p.get("response_tokens_est"):
                sub.append(f"~{_k(p['response_tokens_est'])} tokens out")
            note = _first_sentence(p.get("agent_note"))
            nar = f"The {_spoken_agent(agent)} returns {p.get('status') or 'a result'} and " \
                  f"writes {art.replace('-', ' ').replace('.md', '')}."
            if note:
                nar += f" In its own words: {_first_sentence(note, 140)}"
            spoken_art = art.replace("-", " ").replace(".md", "")
            long = (f"The {_spoken_agent(agent)} has finished and handed its work back. "
                    f"What it produced is a document called {spoken_art}. "
                    f"Along with it, the specialist has to declare what it changed and how "
                    f"confident it is. It cannot simply say it is done. ")
            if note:
                long += f"In its own words: {_first_sentence(note, 200)} "
            scenes.append(_scene(a, [m], proj,
                                 title=f"The {_spoken_agent(agent)} hands back its work",
                                 subtitle=" · ".join(sub), narration=nar,
                                 narration_short=f"The {_spoken_agent(agent)} returns "
                                                 f"{p.get('status') or 'a result'}.",
                                 narration_long=long.strip()))
            continue

        if a == "validation":
            if p.get("result") == "pass":
                sub = f"{p.get('checks_passed')}/{p.get('checks_run')} checks passed" \
                    if p.get("checks_run") else "artifact conforms"
                nar = "The validation engine accepts the artifact"
                if p.get("checks_run"):
                    nar += f": {p['checks_passed']} of {p['checks_run']} checks pass"
                nar += f". {_spoken_phase(m.get('phase')).capitalize()} is complete."
                short = f"Validation passed, {p['checks_passed']} of {p['checks_run']} checks." \
                    if p.get("checks_run") else "Validation passed."
                long = ("Now the framework checks the work that was just handed in. "
                        "This is not a person skim reading it. It is an automatic "
                        "inspection")
                if p.get("checks_run"):
                    long += (f" against {p['checks_run']} specific rules: is every required "
                             f"section actually there, does every claim point at real "
                             f"evidence, do the numbers add up. ")
                    long += (f"All {p['checks_passed']} of them pass. ")
                else:
                    long += " against a fixed set of rules, and the work passes. "
                long += (f"So the {_spoken_phase(m.get('phase'))} stage is accepted, and only "
                         f"now is it allowed to count as finished.")
                ttl = "The framework checks the work"
            else:
                fails = p.get("failures") or []
                sub = f"{len(fails)} failing check{'s' if len(fails) != 1 else ''}"
                if fails:
                    sub += ": " + ", ".join(fails[:6])
                nar = f"The validation engine rejects the artifact, naming {len(fails)} " \
                      f"failing check{'s' if len(fails) != 1 else ''}. Nothing is patched by " \
                      f"hand: the rejection is classified and the agent repairs it."
                short = f"Validation rejected: {len(fails)} failing check" \
                        f"{'s' if len(fails) != 1 else ''}."
                long = (f"The check has found a problem. "
                        f"{len(fails)} of the rules did not pass. "
                        f"Now watch what the framework does about it, because this is the "
                        f"part people usually ask about. Nobody quietly patches it by hand, "
                        f"and nobody waves it through. The framework writes down exactly "
                        f"which rule failed and why, and sends the work straight back to the "
                        f"same specialist to put right. The rejected version is kept as "
                        f"well, so there is a record that it happened.")
                ttl = "The check finds a problem"
            scenes.append(_scene(a, [m], proj, title=ttl, subtitle=sub, narration=nar,
                                 narration_short=short, narration_long=long))
            continue

        if a == "retry":
            scenes.append(_scene(a, [m], proj, title="Bounded retry scheduled",
                                 subtitle=f"{p.get('failure_class') or 'classified failure'}"
                                          f" → {p.get('action') or 'retry'}",
                                 narration=f"The recovery controller classifies the failure as "
                                           f"{(p.get('failure_class') or 'retryable').replace('-', ' ')}"
                                           f" and schedules a bounded retry. No operator is "
                                           f"needed.",
                                 narration_short="A bounded retry is scheduled.",
                                 narration_long=(
                                     "The framework has worked out for itself what kind of "
                                     "failure this is, and has scheduled another attempt, "
                                     "waiting a little longer before each one. Nobody had to "
                                     "notice the problem and nobody had to step in. There is "
                                     "a limit, though: it will not keep trying forever. "
                                     "After a set number of attempts it stops and asks a "
                                     "person.")))
            continue

        if a == "rollback":
            sup = p.get("superseded") or []
            scenes.append(_scene(a, [m], proj, title="Rollback authorised",
                                 subtitle=f"{p.get('gate') or ''} · {len(sup)} phase(s) superseded",
                                 narration=f"A human authorises the rollback the gate rejection "
                                           f"classified: {len(sup)} completed phase"
                                           f"{'s are' if len(sup) != 1 else ' is'} superseded, "
                                           f"their evidence kept immutable, and the gate is "
                                           f"re-armed for a fresh decision.",
                                 narration_long=(
                                     f"Someone has rejected the work at a checkpoint, and a "
                                     f"person has authorised going back to redo it. Notice "
                                     f"that the work already done is not thrown away. It is "
                                     f"kept exactly as it was, as a record of what happened, "
                                     f"and the stage is simply reopened so it can be done "
                                     f"again properly.")))
            continue

        if a == "gate_awaiting":       # only reached when it is the very first scene
            roles = p.get("owner_roles") or []
            scenes.append(_scene(a, [m], proj, title=f"{p.get('gate') or 'Gate'} awaits a decision",
                                 subtitle="owner: " + ", ".join(_spoken_agent(r) for r in roles)
                                 if roles else "human decision required",
                                 narration=f"{p.get('gate') or 'The gate'} holds the run until a "
                                           f"listed owner who did not produce the evidence "
                                           f"decides it.",
                                 narration_long=(
                                     f"The work now stops and waits. This is one of the "
                                     f"checkpoints, and no assistant can get past it. Only a "
                                     f"person can clear it, and not just any person: it has "
                                     f"to be someone holding one of the roles named for this "
                                     f"checkpoint, and it cannot be whoever produced the "
                                     f"work being checked.")))
            continue

        if a == "gate_decision":
            who = _spoken_agent(p.get("owner_role"))
            by = p.get("decided_by") or ""
            sub = f"{p.get('decision')} by {who}"
            if by and by != p.get("owner_role"):
                sub += f" · recorded by {str(by)[:40]}"
            nar = f"{p.get('gate') or 'The gate'} is {p.get('decision') or 'decided'} by the {who}"
            if p.get("auto_policy"):
                nar += " under the clean-evidence auto policy"
            rat = _first_sentence(p.get("rationale"), 160)
            nar += f". Rationale: {rat}" if rat else "."
            long = (f"A person has now decided this checkpoint, and the answer is "
                    f"{p.get('decision') or 'recorded'}. The role that decided is the {who}. "
                    f"That matters: the framework will not take a decision from just "
                    f"anybody. It must come from a role named for this checkpoint, and never "
                    f"from whoever produced the work being judged. ")
            if rat:
                long += f"The reason given was: {rat} "
            long += "The name and the reason are kept beside the decision."
            s = _scene(a, [m], proj,
                       title=f"A person decides: {p.get('gate') or 'checkpoint'} "
                             f"{p.get('decision') or ''}".strip(),
                       subtitle=sub, narration=nar if nar.endswith(".") else nar + ".",
                       narration_short=f"{p.get('gate') or 'The gate'} "
                                       f"{p.get('decision') or 'decided'} by the {who}.",
                       narration_long=long.strip())
            scenes.append(s)
            if i < n and markers[i]["action_type"] == "phase_unblocked":
                apply_marker(proj, markers[i])
                _absorb(s, markers[i], proj, f"{markers[i].get('phase')} unblocked")
                i += 1
            continue

        if a == "phase_blocked":
            scenes.append(_scene(a, [m], proj, title=f"{_spoken_phase(m.get('phase'))} blocked",
                                 subtitle=str(p.get("reason") or "")[:90],
                                 narration=f"{_spoken_phase(m.get('phase')).capitalize()} is "
                                           f"blocked with a recorded reason: "
                                           f"{str(p.get('reason') or '').replace('_', ' ')}.",
                                 narration_long=(
                                     f"This stage cannot start yet, and the framework says "
                                     f"plainly why rather than failing quietly: "
                                     f"{str(p.get('reason') or 'a condition is not met').replace('_', ' ')}"
                                     f". It will wait here until that is dealt with.")))
            continue

        if a == "phase_unblocked":
            scenes.append(_scene(a, [m], proj, title=f"{_spoken_phase(m.get('phase'))} unblocked",
                                 subtitle="guards pass; the phase is dispatchable",
                                 narration=f"{_spoken_phase(m.get('phase')).capitalize()} is "
                                           f"unblocked and becomes dispatchable.",
                                 narration_long=(
                                     f"Whatever was holding this stage up has cleared, so "
                                     f"the {_spoken_phase(m.get('phase'))} stage is free to "
                                     f"go ahead now.")))
            continue

        if a == "aggregation":
            scenes.append(_scene(a, [m], proj, title="Outputs aggregated",
                                 subtitle=f"{p.get('completed') or '?'}/{p.get('phases') or '?'}"
                                          f" phases in the completion package",
                                 narration="The output aggregator rebuilds the completion "
                                           "package and the final report from persisted "
                                           "evidence.",
                                 narration_long=(
                                     "The framework gathers everything produced so far into "
                                     "a single package, along with a report of who did what "
                                     "and when. It rebuilds that from the record every time, "
                                     "rather than keeping a running summary that could drift "
                                     "out of step with what actually happened.")))
            continue

        if a in ("run_completed", "run_aborted"):
            done = sum(1 for x in proj["phases"] if x["status"] == "completed")
            gates = sum(1 for g in proj["gates"].values() if g.get("decision"))
            tok = p.get("progressive_tokens") or proj["tokens"].get("prompt_est")
            wall = _wall(m.get("t_ms", 0))
            sub = f"{done} phases · {gates} gates decided · ~{_k(tok)} tokens est · " \
                  f"wall clock {wall}"
            if a == "run_completed":
                nar = f"Every phase delivered, every gate decided: {done} phases, " \
                      f"{gates} human decisions, about {_k(tok)} tokens of context, in {wall}. " \
                      f"This film was compiled by the run itself."
                long_end = (
                    f"And that is the whole cycle. {done} stage"
                    f"{'s' if done != 1 else ''} delivered, {gates} checkpoint"
                    f"{'s' if gates != 1 else ''} approved by a person, and every step of it "
                    f"written down as it happened. In real time the run took {wall}. "
                    f"One last thing worth saying: the video you have just watched was put "
                    f"together by the run itself, from its own recording and its own record "
                    f"of what it did. Nobody edited this by hand.")
                ttl = "Run completed"
            else:
                nar = f"The run was aborted after {done} completed phases. The evidence up to " \
                      f"this point is preserved."
                long_end = (f"The run was stopped after {done} completed stage"
                            f"{'s' if done != 1 else ''}. Everything produced up to this "
                            f"point is kept, so nothing that was already done has to be "
                            f"done again.")
                ttl = "Run aborted"
            s = _scene(a, [m], proj, phase=None, title=ttl, subtitle=sub, narration=nar,
                       narration_long=long_end)
            s["overlay_until"] = 0.9
            scenes.append(s)
            continue

        scenes.append(_scene("note", [m], proj, title=str(m.get("title", ""))[:80],
                             subtitle="", narration=""))

    return scenes


def compress(scenes: list, target_seconds: float | None, narration_ms: dict | None = None) -> list:
    """Fit the visual durations to the target, then let narration lengthen what it must.

    `narration_ms` maps a scene index to the duration of its narration audio. A narrated
    scene is never shorter than its audio plus a beat; that is the one thing compression
    may not touch, because a sentence cut mid-word is worse than a long film.
    """
    narrated = {idx: int(ms) + 500 for idx, ms in (narration_ms or {}).items() if ms}
    for idx, hold in narrated.items():
        scenes[idx]["duration_ms"] = max(scenes[idx]["duration_ms"], hold)
    if target_seconds:
        # the narrated scenes are fixed; the silent ones share whatever room is left
        fixed = sum(s["duration_ms"] for i, s in enumerate(scenes) if i in narrated)
        free = [s for i, s in enumerate(scenes) if i not in narrated]
        free_total = sum(s["duration_ms"] for s in free)
        room = max(0, target_seconds * 1000 - fixed)
        if free_total > room and free_total:
            factor = room / free_total
            for s in free:
                s["duration_ms"] = max(MIN_SCENE_MS, int(s["duration_ms"] * factor))
    start = 0
    total = sum(s["duration_ms"] for s in scenes) or 1
    for s in scenes:
        s["start_ms"] = start
        s["progress"] = start / total
        start += s["duration_ms"]
    return scenes


def total_ms(scenes: list) -> int:
    return sum(s["duration_ms"] for s in scenes)
