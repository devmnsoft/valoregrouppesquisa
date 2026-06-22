from __future__ import annotations

import json
import mimetypes
import os
import re
import smtplib
import socket
import ssl
import threading
import urllib.error
import urllib.request
import webbrowser
from datetime import datetime, timezone
from email.message import EmailMessage
from http import HTTPStatus
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlparse

ROOT = Path(__file__).resolve().parent
DATA_DIR = ROOT / "data"
OUTBOX_DIR = DATA_DIR / "outbox"
CONFIG_FILE = DATA_DIR / "email_config.json"
LOGO_FILE = ROOT / "assets" / "logo-full.jpeg"
DEFAULT_SENDER = "valoragroup@mnsoft.com.br"
MAX_BODY = 2 * 1024 * 1024
APP_VERSION = "8.6.6"
LOCAL_CSP = (
    "default-src 'self'; "
    "script-src 'self' https://www.gstatic.com; "
    "style-src 'self' 'unsafe-inline'; "
    "img-src 'self' data: blob:; "
    "font-src 'self' data:; "
    "connect-src 'self' https://www.gstatic.com https://viacep.com.br https://brasilapi.com.br "
    "https://*.googleapis.com https://*.firebaseio.com https://*.cloudfunctions.net "
    "https://identitytoolkit.googleapis.com https://securetoken.googleapis.com https://firestore.googleapis.com; "
    "object-src 'none'; base-uri 'self'; frame-ancestors 'self'"
)

DATA_DIR.mkdir(exist_ok=True)
OUTBOX_DIR.mkdir(exist_ok=True)


def default_email_config() -> dict:
    return {
        "mode": "outbox",
        "senderName": "Valora Group",
        "senderEmail": DEFAULT_SENDER,
        "smtpUsername": DEFAULT_SENDER,
        "smtpHost": "",
        "smtpPort": 587,
        "smtpSecurity": "starttls",
        "password": "",
    }


def load_email_config() -> dict:
    cfg = default_email_config()
    if CONFIG_FILE.exists():
        try:
            saved = json.loads(CONFIG_FILE.read_text(encoding="utf-8"))
            if isinstance(saved, dict):
                cfg.update(saved)
        except (OSError, json.JSONDecodeError):
            pass
    env_password = os.environ.get("VALORA_SMTP_PASSWORD")
    if env_password:
        cfg["password"] = env_password
    return cfg


def save_email_config(payload: dict) -> dict:
    current = load_email_config()
    allowed = {
        "mode",
        "senderName",
        "senderEmail",
        "smtpUsername",
        "smtpHost",
        "smtpPort",
        "smtpSecurity",
    }
    for key in allowed:
        if key in payload:
            current[key] = payload[key]
    password = str(payload.get("password", ""))
    if password:
        current["password"] = password
    current["mode"] = "smtp" if current.get("mode") == "smtp" else "outbox"
    current["smtpPort"] = int(current.get("smtpPort") or 587)
    current["smtpSecurity"] = current.get("smtpSecurity") if current.get("smtpSecurity") in {"starttls", "ssl", "none"} else "starttls"
    CONFIG_FILE.write_text(json.dumps(current, ensure_ascii=False, indent=2), encoding="utf-8")
    try:
        os.chmod(CONFIG_FILE, 0o600)
    except OSError:
        pass
    return current


def public_email_status(cfg: dict) -> dict:
    return {
        "mode": cfg.get("mode", "outbox"),
        "senderName": cfg.get("senderName", "Valora Group"),
        "senderEmail": cfg.get("senderEmail", DEFAULT_SENDER),
        "smtpHostConfigured": bool(cfg.get("smtpHost")),
        "passwordConfigured": bool(cfg.get("password")),
    }


def safe_filename(value: str) -> str:
    cleaned = re.sub(r"[^a-zA-Z0-9._-]+", "-", value).strip("-.")
    return cleaned[:120] or "email"


def build_message(payload: dict, cfg: dict) -> EmailMessage:
    to_address = str(payload.get("to", "")).strip()
    if not to_address or "@" not in to_address:
        raise ValueError("Destinatário de e-mail inválido.")
    subject = str(payload.get("subject", "Valora Pulse™"))[:250]
    text = str(payload.get("text", ""))
    html = str(payload.get("html", ""))
    msg = EmailMessage()
    sender_name = str(cfg.get("senderName", "Valora Group"))
    sender_email = str(cfg.get("senderEmail", DEFAULT_SENDER))
    msg["From"] = f"{sender_name} <{sender_email}>"
    msg["To"] = to_address
    msg["Subject"] = subject
    msg["X-Valora-Pulse"] = APP_VERSION
    msg.set_content(text or "Mensagem Valora Pulse™")
    if html:
        msg.add_alternative(html, subtype="html")
        if LOGO_FILE.exists():
            html_part = msg.get_payload()[-1]
            html_part.add_related(
                LOGO_FILE.read_bytes(),
                maintype="image",
                subtype="jpeg",
                cid="<valora-logo>",
                filename="valora-group.jpeg",
            )
    return msg


def deliver_message(msg: EmailMessage, cfg: dict) -> dict:
    mode = cfg.get("mode", "outbox")
    if mode != "smtp":
        timestamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S-%f")
        recipient = safe_filename(str(msg.get("To", "destinatario")))
        path = OUTBOX_DIR / f"{timestamp}-{recipient}.eml"
        path.write_bytes(msg.as_bytes())
        return {"ok": True, "mode": "outbox", "file": path.name}

    host = str(cfg.get("smtpHost", "")).strip()
    if not host:
        raise ValueError("Configure o servidor SMTP antes de usar o modo real.")
    port = int(cfg.get("smtpPort") or 587)
    username = str(cfg.get("smtpUsername", "")).strip()
    password = str(cfg.get("password", ""))
    security = cfg.get("smtpSecurity", "starttls")
    context = ssl.create_default_context()
    if security == "ssl":
        server = smtplib.SMTP_SSL(host, port, timeout=25, context=context)
    else:
        server = smtplib.SMTP(host, port, timeout=25)
    try:
        server.ehlo()
        if security == "starttls":
            server.starttls(context=context)
            server.ehlo()
        if username:
            if not password:
                raise ValueError("A senha SMTP ainda não foi configurada no servidor local.")
            server.login(username, password)
        server.send_message(msg)
    finally:
        try:
            server.quit()
        except Exception:
            server.close()
    return {"ok": True, "mode": "smtp"}


def fetch_json(url: str) -> dict:
    request = urllib.request.Request(
        url,
        headers={"User-Agent": f"ValoraPulse/{APP_VERSION} (+local diagnostic platform)"},
    )
    with urllib.request.urlopen(request, timeout=12) as response:
        return json.loads(response.read().decode("utf-8"))


class ValoraHandler(SimpleHTTPRequestHandler):
    server_version = f"ValoraPulseLocal/{APP_VERSION}"

    def end_headers(self) -> None:
        self.send_header("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0")
        self.send_header("Pragma", "no-cache")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("X-Frame-Options", "SAMEORIGIN")
        self.send_header("Referrer-Policy", "strict-origin-when-cross-origin")
        self.send_header("Permissions-Policy", "camera=(), microphone=(), geolocation=()")
        self.send_header("Content-Security-Policy", LOCAL_CSP)
        super().end_headers()

    def log_message(self, fmt: str, *args) -> None:
        print(f"[{self.log_date_time_string()}] {fmt % args}")

    def translate_path(self, path: str) -> str:
        parsed = urlparse(path)
        relative = Path(unquote(parsed.path).lstrip("/"))
        candidate = (ROOT / relative).resolve()
        try:
            candidate.relative_to(ROOT)
        except ValueError:
            return str(ROOT / "index.html")
        if candidate.is_dir():
            candidate = candidate / "index.html"
        return str(candidate)

    def _read_json(self) -> dict:
        length = int(self.headers.get("Content-Length", "0") or 0)
        if length <= 0 or length > MAX_BODY:
            raise ValueError("Corpo da requisição inválido.")
        raw = self.rfile.read(length)
        value = json.loads(raw.decode("utf-8"))
        if not isinstance(value, dict):
            raise ValueError("JSON inválido.")
        return value

    def _json(self, status: int, payload: dict) -> None:
        body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self) -> None:
        path = urlparse(self.path).path
        try:
            if path == "/api/health":
                return self._json(200, {"ok": True, "version": APP_VERSION})
            if path == "/api/email/status":
                return self._json(200, public_email_status(load_email_config()))
            if path == "/api/outbox":
                items = []
                for file in sorted(OUTBOX_DIR.glob("*.eml"), key=lambda p: p.stat().st_mtime, reverse=True)[:100]:
                    items.append(
                        {
                            "name": file.name,
                            "createdAt": datetime.fromtimestamp(file.stat().st_mtime).isoformat(timespec="seconds"),
                            "url": f"/api/outbox/{file.name}",
                        }
                    )
                return self._json(200, {"items": items})
            if path.startswith("/api/outbox/"):
                name = Path(unquote(path.split("/api/outbox/", 1)[1])).name
                file = OUTBOX_DIR / name
                if not file.exists() or file.suffix.lower() != ".eml":
                    return self._json(404, {"error": "Arquivo não encontrado."})
                body = file.read_bytes()
                self.send_response(200)
                self.send_header("Content-Type", "message/rfc822")
                self.send_header("Content-Disposition", f'attachment; filename="{safe_filename(file.name)}"')
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                self.wfile.write(body)
                return
            if path.startswith("/api/cep/"):
                cep = re.sub(r"\D", "", path.rsplit("/", 1)[-1])
                if len(cep) != 8:
                    return self._json(400, {"error": "CEP inválido."})
                try:
                    data = fetch_json(f"https://viacep.com.br/ws/{cep}/json/")
                    if data.get("erro"):
                        raise ValueError("CEP não encontrado.")
                except Exception:
                    data = fetch_json(f"https://brasilapi.com.br/api/cep/v2/{cep}")
                    data = {
                        "cep": data.get("cep"),
                        "logradouro": data.get("street"),
                        "bairro": data.get("neighborhood"),
                        "localidade": data.get("city"),
                        "uf": data.get("state"),
                    }
                return self._json(200, data)
            if path.startswith("/api/cnpj/"):
                cnpj = re.sub(r"\D", "", path.rsplit("/", 1)[-1])
                if len(cnpj) != 14:
                    return self._json(400, {"error": "CNPJ inválido."})
                data = fetch_json(f"https://brasilapi.com.br/api/cnpj/v1/{cnpj}")
                return self._json(200, data)
        except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError, ValueError, json.JSONDecodeError) as exc:
            return self._json(502, {"error": f"Serviço externo indisponível: {exc}"})
        return super().do_GET()

    def do_POST(self) -> None:
        path = urlparse(self.path).path
        try:
            payload = self._read_json()
            if path == "/api/email/config":
                cfg = save_email_config(payload)
                return self._json(200, {"ok": True, **public_email_status(cfg)})
            if path == "/api/email/send":
                cfg = load_email_config()
                msg = build_message(payload, cfg)
                result = deliver_message(msg, cfg)
                return self._json(200, result)
            return self._json(404, {"error": "Endpoint não encontrado."})
        except (ValueError, json.JSONDecodeError) as exc:
            return self._json(400, {"error": str(exc)})
        except (smtplib.SMTPException, OSError) as exc:
            return self._json(502, {"error": f"Falha no envio: {exc}"})
        except Exception as exc:  # noqa: BLE001 - servidor local deve responder em JSON
            return self._json(500, {"error": f"Erro interno: {exc}"})


class ValoraServer(ThreadingHTTPServer):
    allow_reuse_address = True


def candidate_ports() -> list[int]:
    requested = int(os.environ.get("VALORA_PORT", "0") or 0)
    ordered = [requested, 8095, 8096, 8097, 8081, 8082, 8000, 5500, 5173, 3000, 5000, 9090]
    result: list[int] = []
    for port in ordered:
        if port and port not in result:
            result.append(port)
    return result


def create_server() -> tuple[ValoraServer, int]:
    errors: list[str] = []
    for port in candidate_ports():
        try:
            return ValoraServer(("127.0.0.1", port), ValoraHandler), port
        except OSError as exc:
            errors.append(f"{port}: {exc}")
    detail = "; ".join(errors[-4:])
    raise SystemExit(f"Nenhuma porta local disponível foi encontrada. {detail}")


def main() -> None:
    os.chdir(ROOT)
    server, port = create_server()
    url = f"http://127.0.0.1:{port}/index.html?v={APP_VERSION}"
    print("===============================================")
    print(f" VALORA PULSE {APP_VERSION} - SERVIDOR LOCAL")
    print("===============================================")
    print(f"Sistema disponível em {url}")
    print("Se o navegador não abrir automaticamente, copie o endereço acima.")
    print("Pressione Ctrl+C para encerrar.")
    threading.Timer(0.8, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nServidor encerrado.")
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
