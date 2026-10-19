"""Minimal obs-websocket v5 client -- standard library only.

OBS Studio 28 and later ship the websocket server in the box (Tools -> WebSocket Server
Settings). The protocol is JSON over a WebSocket, so a client needs the RFC 6455 handshake,
frame masking, and the v5 identify/auth exchange -- about two hundred lines, which is less
than the cost of adding `websocket-client` as a framework dependency for one optional feature.

    with OBSClient.connect() as obs:
        obs.request("StartRecord")
        ...
        path = obs.request("StopRecord")["outputPath"]

`OBSClient.connect()` reads the host's own obs-websocket config for the port and password, so
nothing is configured twice. Every failure raises `OBSError`; demo mode treats that as "use the
other capture backend" rather than as a run failure.
"""

from __future__ import annotations

import base64
import hashlib
import json
import os
import socket
import struct
import time
from pathlib import Path

CONFIG_REL = "obs-studio/plugin_config/obs-websocket/config.json"
DEFAULT_PORT = 4455
RPC_VERSION = 1

# op codes, protocol v5
HELLO, IDENTIFY, IDENTIFIED, REIDENTIFY, EVENT, REQUEST, REQUEST_RESPONSE = 0, 1, 2, 3, 5, 6, 7


class OBSError(RuntimeError):
    """Anything that stops the client talking to OBS."""


def config_path() -> Path | None:
    """The host's obs-websocket config, if OBS is installed for this user."""
    appdata = os.environ.get("APPDATA")
    if not appdata:
        return None
    p = Path(appdata) / CONFIG_REL
    return p if p.exists() else None


def read_config() -> dict:
    """{enabled, port, password, auth_required} from the host's OBS config."""
    p = config_path()
    if not p:
        return {"enabled": False, "port": DEFAULT_PORT, "password": None,
                "auth_required": True, "reason": "no obs-websocket config found"}
    try:
        data = json.loads(p.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError) as exc:
        return {"enabled": False, "port": DEFAULT_PORT, "password": None,
                "auth_required": True, "reason": f"unreadable obs-websocket config: {exc}"}
    return {
        "enabled": bool(data.get("server_enabled")),
        "port": int(data.get("server_port") or DEFAULT_PORT),
        "password": data.get("server_password") or None,
        "auth_required": bool(data.get("auth_required", True)),
        "config_path": str(p),
        "reason": None if data.get("server_enabled") else
                  "the OBS WebSocket server is disabled (OBS -> Tools -> WebSocket Server "
                  "Settings -> Enable WebSocket server)",
    }


def _mask(payload: bytes) -> bytes:
    key = os.urandom(4)
    return key + bytes(b ^ key[i % 4] for i, b in enumerate(payload))


class OBSClient:
    """One WebSocket connection to OBS, speaking protocol v5."""

    def __init__(self, sock: socket.socket, timeout: float = 15.0):
        self.sock = sock
        self.timeout = timeout
        self._buf = b""
        self._req = 0

    # ------------------------------------------------------------------ lifecycle

    @classmethod
    def connect(cls, host: str = "127.0.0.1", port: int | None = None,
                password: str | None = None, timeout: float = 8.0) -> "OBSClient":
        cfg = read_config()
        port = port or cfg["port"]
        password = password if password is not None else cfg["password"]
        try:
            sock = socket.create_connection((host, port), timeout=timeout)
        except OSError as exc:
            raise OBSError(f"cannot reach obs-websocket at {host}:{port}: {exc}. "
                           f"{cfg.get('reason') or 'Is OBS running?'}") from exc
        sock.settimeout(timeout)
        client = cls(sock, timeout)
        try:
            client._handshake(host, port)
            client._identify(password)
        except Exception:
            client.close()
            raise
        return client

    def close(self):
        try:
            self.sock.close()
        except OSError:
            pass

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        self.close()

    # ------------------------------------------------------------------ websocket

    def _handshake(self, host: str, port: int):
        key = base64.b64encode(os.urandom(16)).decode()
        req = (f"GET / HTTP/1.1\r\nHost: {host}:{port}\r\nUpgrade: websocket\r\n"
               f"Connection: Upgrade\r\nSec-WebSocket-Key: {key}\r\n"
               f"Sec-WebSocket-Version: 13\r\n\r\n")
        self.sock.sendall(req.encode())
        head = b""
        while b"\r\n\r\n" not in head:
            chunk = self.sock.recv(4096)
            if not chunk:
                raise OBSError("obs-websocket closed during the handshake")
            head += chunk
        header, _, rest = head.partition(b"\r\n\r\n")
        if b" 101 " not in header.split(b"\r\n")[0]:
            raise OBSError(f"obs-websocket refused the upgrade: "
                           f"{header.splitlines()[0].decode(errors='replace')}")
        self._buf = rest

    def _send(self, payload: dict):
        body = json.dumps(payload).encode("utf-8")
        n = len(body)
        if n < 126:
            head = struct.pack("!BB", 0x81, 0x80 | n)
        elif n < (1 << 16):
            head = struct.pack("!BBH", 0x81, 0x80 | 126, n)
        else:
            head = struct.pack("!BBQ", 0x81, 0x80 | 127, n)
        self.sock.sendall(head + _mask(body))

    def _read(self, n: int) -> bytes:
        while len(self._buf) < n:
            chunk = self.sock.recv(65536)
            if not chunk:
                raise OBSError("obs-websocket closed the connection")
            self._buf += chunk
        out, self._buf = self._buf[:n], self._buf[n:]
        return out

    def _recv(self) -> dict:
        """The next data frame, decoded. Control frames are handled transparently."""
        while True:
            b1, b2 = struct.unpack("!BB", self._read(2))
            opcode, length = b1 & 0x0F, b2 & 0x7F
            if length == 126:
                length = struct.unpack("!H", self._read(2))[0]
            elif length == 127:
                length = struct.unpack("!Q", self._read(8))[0]
            if b2 & 0x80:                       # server frames are never masked, but be safe
                key = self._read(4)
                body = bytes(b ^ key[i % 4] for i, b in enumerate(self._read(length)))
            else:
                body = self._read(length)
            if opcode == 0x8:
                raise OBSError("obs-websocket sent a close frame")
            if opcode == 0x9:                   # ping -> pong
                self.sock.sendall(struct.pack("!BB", 0x8A, 0x80 | len(body)) + _mask(body))
                continue
            if opcode in (0x1, 0x2):
                return json.loads(body.decode("utf-8"))

    # ------------------------------------------------------------------ protocol v5

    def _identify(self, password: str | None):
        hello = self._recv()
        if hello.get("op") != HELLO:
            raise OBSError(f"expected Hello, got op {hello.get('op')}")
        d = hello.get("d") or {}
        payload = {"rpcVersion": RPC_VERSION}
        auth = d.get("authentication")
        if auth:
            if not password:
                raise OBSError("OBS requires a websocket password and none was found in its "
                               "config")
            secret = base64.b64encode(
                hashlib.sha256((password + auth["salt"]).encode()).digest()).decode()
            payload["authentication"] = base64.b64encode(
                hashlib.sha256((secret + auth["challenge"]).encode()).digest()).decode()
        self._send({"op": IDENTIFY, "d": payload})
        while True:
            msg = self._recv()
            if msg.get("op") == IDENTIFIED:
                self.obs_version = d.get("obsWebSocketVersion")
                return
            if msg.get("op") != EVENT:
                raise OBSError(f"identification failed: {json.dumps(msg)[:200]}")

    def request(self, request_type: str, data: dict | None = None,
                timeout: float | None = None) -> dict:
        """One request/response round trip. Raises OBSError on a non-success status."""
        self._req += 1
        rid = f"req-{self._req}"
        self._send({"op": REQUEST, "d": {"requestType": request_type, "requestId": rid,
                                         "requestData": data or {}}})
        deadline = time.monotonic() + (timeout or self.timeout)
        while time.monotonic() < deadline:
            msg = self._recv()
            if msg.get("op") != REQUEST_RESPONSE:
                continue
            d = msg.get("d") or {}
            if d.get("requestId") != rid:
                continue
            status = d.get("requestStatus") or {}
            if not status.get("result"):
                raise OBSError(f"{request_type} failed: {status.get('code')} "
                               f"{status.get('comment') or ''}".strip())
            return d.get("responseData") or {}
        raise OBSError(f"{request_type} timed out after {timeout or self.timeout}s")

    # ------------------------------------------------------------------ convenience

    def try_request(self, request_type: str, data: dict | None = None) -> dict | None:
        """As `request`, but None instead of an exception -- for idempotent provisioning
        where "it already exists" and "it worked" are the same outcome."""
        try:
            return self.request(request_type, data)
        except OBSError:
            return None

    def recording(self) -> dict:
        return self.request("GetRecordStatus")


def probe() -> dict:
    """Is OBS controllable right now? Never raises; used to pick a capture backend."""
    cfg = read_config()
    info = {"config": {k: v for k, v in cfg.items() if k != "password"},
            "reachable": False, "version": None, "reason": cfg.get("reason")}
    if not cfg["enabled"]:
        return info
    try:
        with OBSClient.connect(timeout=3.0) as obs:
            v = obs.request("GetVersion")
            info.update(reachable=True, reason=None,
                        version=f"OBS {v.get('obsVersion')} / websocket "
                                f"{v.get('obsWebSocketVersion')}")
    except OBSError as exc:
        info["reason"] = str(exc)
    return info
