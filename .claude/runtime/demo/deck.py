"""The HTML presentation deck -- the fallback that always works, and a companion otherwise.

One self-contained file: scenes, keyframes (PNG, base64) or text snapshots when Pillow was
absent, per-scene narration (WAV, base64) when a voice was available, the telemetry table of
every marker, and a small vanilla-JS player that auto-advances on the scene durations the
timeline decided. No network, no framework, no build step: open it in any browser, or
hand it to the person running the projector.
"""

from __future__ import annotations

import base64
import html
import json
from pathlib import Path

_CSS = """
:root{--bg:#0b0f19;--panel:#121a2b;--edge:#243048;--text:#e5e7eb;--muted:#94a3b8;--dim:#475569;
--accent:#38bdf8;--green:#22c55e;--amber:#f59e0b;--red:#ef4444;--purple:#a78bfa}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);
font:14px/1.45 "Segoe UI",system-ui,-apple-system,sans-serif}
header{display:flex;align-items:center;gap:16px;padding:14px 22px;border-bottom:1px solid var(--edge)}
header .logo{width:14px;height:14px;background:var(--accent);transform:rotate(45deg);margin:0 6px}
header h1{font-size:16px;margin:0;letter-spacing:.04em}header .meta{color:var(--muted);font-family:ui-monospace,Consolas,monospace;font-size:12px}
header .spacer{flex:1}header .rec{color:var(--red);font-weight:700;font-size:12px;letter-spacing:.1em}
header .rec::before{content:"";display:inline-block;width:8px;height:8px;border-radius:50%;background:var(--red);margin-right:6px;animation:pulse 1.2s infinite}
@keyframes pulse{0%,100%{opacity:1}50%{opacity:.35}}
main{display:grid;grid-template-columns:1fr 320px;gap:18px;padding:18px 22px}
.stage{position:relative;background:#000;border:1px solid var(--edge);border-radius:12px;overflow:hidden;aspect-ratio:16/9}
.stage img{width:100%;height:100%;display:block;object-fit:contain}
.stage pre{margin:0;padding:18px;height:100%;overflow:auto;color:var(--text);background:var(--bg);font:12px/1.35 ui-monospace,Consolas,"Cascadia Mono",monospace;white-space:pre}
.overlay{position:absolute;left:50%;top:26%;transform:translate(-50%,-50%);background:rgba(18,26,43,.92);border:2px solid var(--accent);border-radius:16px;padding:18px 34px;text-align:center;opacity:0;transition:opacity .25s;pointer-events:none;max-width:80%}
.overlay.show{opacity:1}.overlay h2{margin:0 0 6px;font-size:28px}.overlay p{margin:0;color:var(--muted);font-size:14px}
.think{position:absolute;left:0;right:0;bottom:0;display:flex;align-items:center;gap:12px;padding:10px 18px;background:rgba(11,15,25,.88);border-top:1px solid var(--edge);font-weight:600;color:var(--accent);opacity:0;transition:opacity .3s}
.think.on{opacity:1}.think .spin{width:16px;height:16px;border:3px solid rgba(56,189,248,.25);border-top-color:var(--accent);border-radius:50%;animation:spin .9s linear infinite}
@keyframes spin{to{transform:rotate(360deg)}}.think .dots::after{content:"";animation:dots 1.2s steps(3,end) infinite}
@keyframes dots{0%{content:"."}33%{content:".."}66%{content:"..."}}
.controls{display:flex;align-items:center;gap:10px;margin-top:12px}
button{background:var(--panel);color:var(--text);border:1px solid var(--edge);border-radius:8px;padding:7px 12px;cursor:pointer;font-weight:600}
button:hover{border-color:var(--accent)}button.primary{background:var(--accent);color:#04121c;border-color:var(--accent)}
.progress{flex:1;height:8px;background:var(--panel);border-radius:6px;overflow:hidden;cursor:pointer;position:relative}
.progress i{display:block;height:100%;width:0;background:var(--accent)}
.counter{font-family:ui-monospace,Consolas,monospace;color:var(--muted);font-size:12px;min-width:120px;text-align:right}
aside{background:var(--panel);border:1px solid var(--edge);border-radius:12px;overflow:hidden;display:flex;flex-direction:column;max-height:calc(100vh - 140px)}
aside h3{margin:0;padding:12px 14px;font-size:11px;letter-spacing:.14em;color:var(--muted);border-bottom:1px solid var(--edge)}
aside ol{list-style:none;margin:0;padding:6px;overflow:auto;flex:1}
aside li{padding:8px 10px;border-radius:8px;cursor:pointer;display:grid;grid-template-columns:34px 1fr;gap:8px;color:var(--muted)}
aside li:hover{background:rgba(56,189,248,.08)}aside li.active{background:rgba(56,189,248,.16);color:var(--text)}
aside li .n{font-family:ui-monospace,Consolas,monospace;font-size:11px;color:var(--dim)}
aside li .t{font-weight:600;font-size:13px}aside li .s{font-size:11px;color:var(--muted);display:block}
.nar{padding:12px 22px 0;color:var(--muted);font-style:italic;min-height:40px}
details{margin:6px 22px 30px;border:1px solid var(--edge);border-radius:12px;background:var(--panel)}
summary{cursor:pointer;padding:12px 16px;font-weight:600;color:var(--muted)}
table{width:100%;border-collapse:collapse;font:12px ui-monospace,Consolas,monospace}
td,th{padding:6px 10px;border-top:1px solid var(--edge);text-align:left;vertical-align:top}
th{color:var(--muted);font-weight:600;font-size:11px;letter-spacing:.08em}
td.k{color:var(--accent)}tr:hover td{background:rgba(255,255,255,.02)}
.badge{display:inline-block;padding:1px 8px;border-radius:10px;font-size:11px;border:1px solid var(--edge);color:var(--muted)}
.note{padding:0 22px 12px;color:var(--dim);font-size:12px}
@media (max-width:980px){main{grid-template-columns:1fr}aside{max-height:320px}}
"""

_JS = r"""
(function(){
const D=JSON.parse(document.getElementById('deck-data').textContent);
const S=D.scenes, img=document.getElementById('frame'), pre=document.getElementById('text'),
ov=document.getElementById('overlay'), ovT=document.getElementById('ov-title'), ovS=document.getElementById('ov-sub'),
think=document.getElementById('think'), thinkTxt=document.getElementById('think-txt'),
bar=document.getElementById('bar'), cnt=document.getElementById('counter'), nar=document.getElementById('nar'),
list=document.getElementById('list'), play=document.getElementById('play'), mute=document.getElementById('mute');
let i=0, playing=false, muted=false, timer=null, ovTimer=null, audio=null, startedAt=0, raf=null;
S.forEach((s,k)=>{const li=document.createElement('li');li.innerHTML='<span class="n">'+String(k+1).padStart(2,'0')+'</span><span><span class="t"></span><span class="s"></span></span>';
li.querySelector('.t').textContent=s.title;li.querySelector('.s').textContent=s.subtitle||'';li.onclick=()=>go(k,true);list.appendChild(li);});
function stopAudio(){if(audio){audio.pause();audio=null;}}
function go(k,user){i=(k+S.length)%S.length;const s=S[i];
if(s.image){img.src='data:image/png;base64,'+D.images[s.image];img.hidden=false;pre.hidden=true;}else{pre.textContent=s.snapshot||'';pre.hidden=false;img.hidden=true;}
ovT.textContent=s.title;ovS.textContent=s.subtitle||'';ov.classList.add('show');clearTimeout(ovTimer);ovTimer=setTimeout(()=>ov.classList.remove('show'),Math.min(3200,s.duration_ms*(s.overlay_until||0.38)));
think.classList.toggle('on',!!s.thinking);thinkTxt.textContent=s.thinking?(s.kind==='tool_group'?'Agent using tools':'Agent Thinking'):'';
nar.textContent=s.narration||'';[...list.children].forEach((li,k2)=>li.classList.toggle('active',k2===i));list.children[i].scrollIntoView({block:'nearest'});
cnt.textContent=(i+1)+' / '+S.length+'  ·  '+(s.duration_ms/1000).toFixed(1)+'s';
stopAudio();if(!muted&&D.audio[i]){audio=new Audio('data:audio/wav;base64,'+D.audio[i]);audio.play().catch(()=>{});}
startedAt=performance.now();if(playing)schedule();if(user&&!playing)tick();}
function schedule(){clearTimeout(timer);timer=setTimeout(()=>{if(i<S.length-1)go(i+1);else setPlaying(false);},S[i].duration_ms);tick();}
function tick(){cancelAnimationFrame(raf);const f=()=>{const el=performance.now()-startedAt;const base=S[i].start_ms;bar.style.width=Math.min(100,100*(base+Math.min(el,S[i].duration_ms))/D.total_ms)+'%';if(playing)raf=requestAnimationFrame(f);};f();}
function setPlaying(p){playing=p;play.textContent=p?'Pause':'Play';play.classList.toggle('primary',!p);if(p){startedAt=performance.now();schedule();}else{clearTimeout(timer);cancelAnimationFrame(raf);stopAudio();}}
play.onclick=()=>setPlaying(!playing);mute.onclick=()=>{muted=!muted;mute.textContent=muted?'Unmute':'Mute';if(muted)stopAudio();};
document.getElementById('prev').onclick=()=>go(i-1,true);document.getElementById('next').onclick=()=>go(i+1,true);
document.getElementById('restart').onclick=()=>{go(0,true);setPlaying(true);};
document.getElementById('progress').onclick=e=>{const r=e.currentTarget.getBoundingClientRect();const ms=(e.clientX-r.left)/r.width*D.total_ms;let k=0;for(let j=0;j<S.length;j++){if(S[j].start_ms<=ms)k=j;}go(k,true);};
document.addEventListener('keydown',e=>{if(e.key===' '){e.preventDefault();setPlaying(!playing);}else if(e.key==='ArrowRight')go(i+1,true);else if(e.key==='ArrowLeft')go(i-1,true);});
go(0,false);setPlaying(true);
})();
"""


def _b64(data: bytes) -> str:
    return base64.b64encode(data).decode("ascii")


def build_deck(out: Path, *, title: str, scenes: list, keyframes: dict, narration: dict,
               markers: list, manifest: dict) -> Path:
    images: dict = {}
    deck_scenes = []
    for i, s in enumerate(scenes):
        key = None
        if i in keyframes and keyframes[i]:
            key = f"k{i}"
            images[key] = _b64(keyframes[i])
        deck_scenes.append({
            "title": s.get("title") or "", "subtitle": s.get("subtitle") or "",
            "kind": s.get("kind"), "thinking": bool(s.get("thinking")),
            "duration_ms": int(s.get("duration_ms", 1500)), "start_ms": int(s.get("start_ms", 0)),
            "overlay_until": s.get("overlay_until", 0.38),
            "narration": s.get("spoken", s.get("narration")) or "",
            "snapshot": (s.get("marker") or {}).get("visual_snapshot_text") or "",
            "image": key, "phase": s.get("phase"), "agent": s.get("agent"),
            "marker_ids": s.get("marker_ids") or [],
        })
    total = sum(s["duration_ms"] for s in deck_scenes) or 1
    # JSON object keys are strings; the player's `D.audio[i]` coerces its number the same way.
    data = {"title": title, "scenes": deck_scenes, "images": images,
            "audio": {str(i): _b64(b) for i, b in narration.items()}, "total_ms": total}
    data_json = json.dumps(data, ensure_ascii=False).replace("</", "<\\/")

    run_id = markers[0]["run_id"] if markers else ""
    nar_engine = (manifest.get("narration") or {}).get("engine")
    rows = []
    for m in markers:
        p = m.get("payload") or {}
        tok = p.get("prompt_tokens_est") or p.get("response_tokens_est") or ""
        rows.append(
            f"<tr><td class='k'>{html.escape(m['marker_id'])}</td>"
            f"<td>{_clock(m.get('t_ms', 0))}</td><td>{html.escape(str(m.get('agent_name', '')))}</td>"
            f"<td><span class='badge'>{html.escape(m['action_type'])}</span></td>"
            f"<td>{html.escape(str(m.get('phase') or ''))}</td>"
            f"<td>{html.escape(str(m.get('title', '')))}</td>"
            f"<td>{html.escape(str(tok))}</td><td>{html.escape(m.get('source', ''))}</td></tr>")
    fallbacks = "".join(f"<li>{html.escape(f['stage'])}: {html.escape(f['reason'])}</li>"
                        for f in manifest.get("fallbacks") or [])
    doc = f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>{html.escape(title)} — demo</title><style>{_CSS}</style></head>
<body>
<header><div class="logo"></div><h1>AI ENGINEERING FRAMEWORK</h1>
<span class="meta">/{html.escape(str((manifest.get('options') or {}).get('command', 'implement')))} --demo · {html.escape(run_id)}</span>
<span class="spacer"></span><span class="rec">REC · {len(scenes)} scenes · {total / 1000:.0f}s</span></header>
<main>
<section>
  <div class="stage">
    <img id="frame" alt="dashboard frame"><pre id="text" hidden></pre>
    <div class="overlay" id="overlay"><h2 id="ov-title"></h2><p id="ov-sub"></p></div>
    <div class="think" id="think"><span class="spin"></span><span id="think-txt">Agent Thinking</span><span class="dots"></span></div>
  </div>
  <div class="controls">
    <button id="restart" title="Restart">⟲</button><button id="prev" title="Previous (←)">‹</button>
    <button id="play" class="primary">Play</button><button id="next" title="Next (→)">›</button>
    <div class="progress" id="progress"><i id="bar"></i></div><span class="counter" id="counter"></span>
    <button id="mute">{'Mute' if narration else 'No voice'}</button>
  </div>
</section>
<aside><h3>SCENES</h3><ol id="list"></ol></aside>
</main>
<p class="nar" id="nar"></p>
<p class="note">{html.escape(title)} · narration: {html.escape(nar_engine or 'none available on the build host')} · compiled {html.escape(manifest.get('built_at', ''))} by the run itself (framework demo mode v{html.escape(manifest.get('version', ''))}). Space plays and pauses; arrows move between scenes.</p>
<details><summary>Execution telemetry — {len(markers)} markers</summary>
<div style="overflow:auto;max-height:420px"><table><thead><tr><th>marker</th><th>t+</th><th>agent</th><th>action</th><th>phase</th><th>title</th><th>tokens est</th><th>source</th></tr></thead>
<tbody>{''.join(rows)}</tbody></table></div></details>
{f'<details><summary>Build fallbacks</summary><ul>{fallbacks}</ul></details>' if fallbacks else ''}
<script id="deck-data" type="application/json">{data_json}</script>
<script>{_JS}</script>
</body></html>"""
    out.write_text(doc, encoding="utf-8")
    return out


def _clock(ms: int) -> str:
    s = max(0, int(ms)) // 1000
    h, rem = divmod(s, 3600)
    m, sec = divmod(rem, 60)
    return f"{h:d}:{m:02d}:{sec:02d}" if h else f"{m:02d}:{sec:02d}"
