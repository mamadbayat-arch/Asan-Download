"""One-time root bootstrap; run from a reviewed staging directory on the host."""
import json
import os
from pathlib import Path
import pwd
import shutil
import subprocess
import urllib.error
import urllib.request

source = Path(__file__).resolve().parent
try:
    pwd.getpwnam('asan-ci')
except KeyError:
    subprocess.run(['useradd', '--system', '--create-home', '--home-dir', '/var/lib/asan-ci', '--shell', '/usr/sbin/nologin', 'asan-ci'], check=True)
target = Path('/usr/local/libexec/asan-download-ci')
target.mkdir(parents=True, exist_ok=True)
for name in ('deploy.py', 'oidc_gateway.py'):
    shutil.copyfile(source / name, target / name)
    os.chown(target / name, 0, 0)
    os.chmod(target / name, 0o644)
shutil.copyfile(source / 'deploy.py', '/usr/local/libexec/asan-download-deploy.py')
os.chmod('/usr/local/libexec/asan-download-deploy.py', 0o644)
shutil.copyfile(source / 'UpdatePublicKey.xml', '/etc/asan-download-publisher.xml')
Path('/etc/asan-download-ci.json').write_text(json.dumps({'root': '/srv/downloader/asan-download'}))
os.chmod('/etc/asan-download-ci.json', 0o600)
rule = Path('/etc/sudoers.d/asan-download-ci')
rule.write_text('asan-ci ALL=(root) NOPASSWD: /usr/bin/python3 /usr/local/libexec/asan-download-deploy.py\n')
os.chmod(rule, 0o440)
subprocess.run(['visudo', '-cf', str(rule)], check=True)
unit = Path('/etc/systemd/system/asan-download-ci.service')
unit.write_text('''[Unit]
Description=Asan Download GitHub OIDC deployment gateway
After=network-online.target
Wants=network-online.target
[Service]
Type=simple
User=asan-ci
Group=asan-ci
WorkingDirectory=/usr/local/libexec/asan-download-ci
ExecStart=/usr/bin/python3 /usr/local/libexec/asan-download-ci/oidc_gateway.py
Restart=on-failure
PrivateTmp=true
ProtectHome=true
ProtectSystem=full
UMask=0077
[Install]
WantedBy=multi-user.target
''')
subprocess.run(['systemctl', 'daemon-reload'], check=True)
subprocess.run(['systemctl', 'enable', '--now', 'asan-download-ci.service'], check=True)
vhost = Path('/etc/nginx/sites-enabled/downloader.hamtavar.ir').resolve()
original = vhost.read_text()
if 'location = /api/ci/deploy' not in original:
    shutil.copy2(vhost, source / 'nginx-before.conf')
    route = '''    location = /api/ci/deploy {
        client_max_body_size 32m;
        proxy_pass http://127.0.0.1:4081/deploy;
        proxy_read_timeout 240s;
        proxy_send_timeout 120s;
        proxy_set_header Authorization $http_authorization;
    }

'''
    assert original.count('    location / {') == 1, 'Unexpected vhost layout'
    vhost.write_text(original.replace('    location / {', route + '    location / {', 1))
    try:
        subprocess.run(['nginx', '-t'], check=True)
        subprocess.run(['systemctl', 'reload', 'nginx'], check=True)
    except Exception:
        vhost.write_text(original)
        raise
print('Gateway installed; publisher key is public only; runtime server and data preserved.')
