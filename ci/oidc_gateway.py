"""Local-only HTTP gateway: GitHub OIDC authentication, then bounded release upload."""
import base64
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
from pathlib import Path
import subprocess
import threading
import time
import urllib.request

from deploy import LIMIT, REPOSITORY, require, verify_rsa_sha256

ISSUER = 'https://token.actions.githubusercontent.com'
AUDIENCE = 'https://downloader.hamtavar.ir'
REPOSITORY_ID = '1421205535'
OWNER_ID = '301430384'
_cache, _cache_until = None, 0
_cache_lock = threading.Lock()
_deploy_slots = threading.BoundedSemaphore(2)


def decode64(value):
    return base64.b64decode(value + '=' * (-len(value) % 4), altchars=b'-_', validate=True)


def keys():
    global _cache, _cache_until
    with _cache_lock:
        if _cache_until <= time.time():
            with urllib.request.urlopen(ISSUER + '/.well-known/jwks', timeout=15) as response:
                _cache = json.load(response)
            _cache_until = time.time() + 60
        return _cache


def authenticate(token, keyset=None, now=None):
    require(isinstance(token, str) and 0 < len(token) <= 20000, 'Invalid identity token')
    parts = token.split('.')
    require(len(parts) == 3, 'Invalid identity token')
    header, claims = (json.loads(decode64(item)) for item in parts[:2])
    require(header.get('alg') == 'RS256' and isinstance(header.get('kid'), str), 'Invalid token algorithm')
    available = (keyset if keyset is not None else keys())['keys']
    matches = [key for key in available if key.get('kid') == header['kid'] and key.get('kty') == 'RSA' and key.get('use') == 'sig']
    require(len(matches) == 1, 'Unknown GitHub signing key')
    key = matches[0]
    verify_rsa_sha256(decode64(parts[2]), (parts[0] + '.' + parts[1]).encode('ascii'), int.from_bytes(decode64(key['n']), 'big'), int.from_bytes(decode64(key['e']), 'big'))
    clock = time.time() if now is None else now
    require(all(type(claims.get(field)) is int for field in ('exp', 'nbf', 'iat')), 'Invalid token times')
    require(claims['nbf'] <= clock + 30 and claims['iat'] <= clock + 30 and claims['exp'] > clock - 30 and clock - claims['iat'] < 900, 'Expired or future identity token')
    expected = dict(iss=ISSUER, aud=AUDIENCE, repository=REPOSITORY, repository_id=REPOSITORY_ID, repository_owner_id=OWNER_ID, ref='refs/heads/main', environment='production', workflow_ref=REPOSITORY + '/.github/workflows/release.yml@refs/heads/main')
    require(all(str(claims.get(key)) == value for key, value in expected.items()), 'Workflow identity is not authorized')
    require(claims.get('event_name') in ('push', 'workflow_dispatch'), 'Untrusted workflow event')
    return claims


class Gateway(BaseHTTPRequestHandler):
    def reply(self, status, body):
        data = json.dumps(body).encode()
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(data)))
        self.send_header('Connection', 'close')
        self.end_headers()
        self.wfile.write(data)
        self.close_connection = True

    def do_POST(self):
        if self.path != '/deploy':
            return self.reply(404, {'error': 'Unknown endpoint'})
        try:
            authorization = self.headers.get('Authorization', '')
            require(authorization.startswith('Bearer '), 'Identity token required')
            authenticate(authorization[len('Bearer '):])
        except Exception:
            return self.reply(401, {'error': 'GitHub production workflow identity required'})
        if not _deploy_slots.acquire(blocking=False):
            return self.reply(503, {'error': 'Deployment busy; retry later'})
        try:
            length = int(self.headers.get('Content-Length', '0'))
            require(0 < length <= LIMIT, 'Invalid upload size')
            self.connection.settimeout(60)
            body = self.rfile.read(length)
            require(len(body) == length, 'Incomplete upload')
            result = subprocess.run(['/usr/bin/sudo', '-n', '/usr/bin/python3', '/usr/local/libexec/asan-download-deploy.py'], input=body, capture_output=True, timeout=180)
            if result.returncode:
                return self.reply(422, {'error': 'Release refused', 'details': result.stderr.decode(errors='replace')[-1500:]})
            return self.reply(200, json.loads(result.stdout))
        except Exception as error:
            return self.reply(422, {'error': str(error)[:300]})
        finally:
            _deploy_slots.release()

    def log_message(self, format_string, *args):
        # Never log Authorization, JWT claims or request bodies.
        print('CI gateway status: ' + str(args[1] if len(args) > 1 else 'request'), flush=True)


if __name__ == '__main__':
    ThreadingHTTPServer(('127.0.0.1', 4081), Gateway).serve_forever()
