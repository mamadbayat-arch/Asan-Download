"""Forced-command release receiver. Install root-owned; never run uploaded code."""
import base64
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

REPOSITORY = 'mamadbayat-arch/Asan-Download'
STABLE = ('downloads/AsanDownload-Windows.zip',
          'downloads/AsanDownload-BrowserExtension.zip',
          'downloads/AsanDownload-Update.exe', 'release.json')
FILES = set(STABLE) | {'deployment.json', 'AsanDownload-Source.zip'}
LIMIT = 32 * 1024 * 1024


def require(condition, message):
    if not condition:
        raise ValueError(message)


def version(text):
    require(isinstance(text, str) and re.fullmatch(r'\d{1,5}\.\d{1,5}\.\d{1,5}', text), 'Invalid release version')
    parts = tuple(map(int, text.split('.')))
    require(max(parts) < 65535, 'Version component too large')
    return parts


def receive(stream, destination):
    """Write only bounded regular files from an exact allowlist; never extract links."""
    seen, total, count = set(), 0, 0
    with tarfile.open(fileobj=stream, mode='r|gz') as archive:
        for member in archive:
            count += 1
            require(count <= 10, 'Too many archive entries')
            path = PurePosixPath(member.name)
            require(not path.is_absolute() and '..' not in path.parts and '\\' not in member.name, 'Unsafe archive path')
            name = str(path)
            if member.isdir():
                require(name in ('.', 'downloads'), 'Unexpected archive directory')
                continue
            require(member.isfile() and name in FILES and name not in seen, 'Unexpected or duplicate archive member')
            total += member.size
            require(0 < member.size <= LIMIT and total <= LIMIT, 'Release exceeds upload limit')
            target = destination / name
            target.parent.mkdir(parents=True, exist_ok=True)
            with archive.extractfile(member) as source, target.open('xb') as output:
                shutil.copyfileobj(source, output)
            seen.add(name)
    require(seen == FILES, 'Incomplete release archive')


def verify_signature(release, public_key):
    require(release.get('updateUrl') == '/downloads/AsanDownload-Update.exe', 'Untrusted update URL')
    require(re.fullmatch(r'[a-f0-9]{64}', release.get('sha256', '')), 'Invalid SHA-256')
    require(type(release.get('exeSize')) is int and 0 < release['exeSize'] <= LIMIT, 'Invalid executable size')
    version(release.get('version'))
    key = ET.fromstring(public_key)
    n = int.from_bytes(base64.b64decode(key.findtext('Modulus'), validate=True), 'big')
    e = int.from_bytes(base64.b64decode(key.findtext('Exponent'), validate=True), 'big')
    require(n.bit_length() >= 2048 and e == 65537, 'Invalid pinned publisher key')
    payload = '\n'.join((release['version'], release['sha256'], str(release['exeSize']), release['updateUrl'])).encode('utf-8')
    verify_rsa_sha256(base64.b64decode(release['signature'], validate=True), payload, n, e)


def verify_rsa_sha256(signature, payload, n, e):
    size = (n.bit_length() + 7) // 8
    require(n.bit_length() >= 2048 and e == 65537, 'Invalid RSA key')
    require(len(signature) == size and int.from_bytes(signature, 'big') < n, 'Invalid RSA signature size')
    digest = bytes.fromhex('3031300d060960864801650304020105000420') + hashlib.sha256(payload).digest()
    expected = b'\x00\x01' + b'\xff' * (size - len(digest) - 3) + b'\x00' + digest
    actual = pow(int.from_bytes(signature, 'big'), e, n).to_bytes(size, 'big')
    require(actual == expected, 'Publisher signature rejected')


def validate(artifacts, public_key, expected_commit):
    release = json.loads((artifacts / 'release.json').read_text(encoding='utf-8'))
    metadata = json.loads((artifacts / 'deployment.json').read_text(encoding='utf-8'))
    require(metadata.get('repository') == REPOSITORY, 'Unexpected source repository')
    require(re.fullmatch(r'[a-f0-9]{40}', metadata.get('sourceCommit', '')), 'Invalid source commit')
    require(metadata['sourceCommit'] == expected_commit, 'Stale release: main has moved')
    require(metadata.get('version') == release.get('version') and release.get('available') is True, 'Invalid release metadata')
    verify_signature(release, public_key)
    require(release.get('downloadUrl') == '/downloads/AsanDownload-Windows.zip', 'Unexpected installer URL')
    require(release.get('extensionDownloadUrl') == '/downloads/AsanDownload-BrowserExtension.zip', 'Unexpected extension URL')
    exe = (artifacts / 'downloads/AsanDownload-Update.exe').read_bytes()
    require(exe[:2] == b'MZ' and len(exe) == release['exeSize'] and hashlib.sha256(exe).hexdigest() == release['sha256'], 'Executable integrity failure')
    installer = artifacts / 'downloads/AsanDownload-Windows.zip'
    require(installer.stat().st_size == release['size'], 'Installer length mismatch')
    extension = (artifacts / 'downloads/AsanDownload-BrowserExtension.zip').read_bytes()
    require(len(extension) == release['extensionSize'] and hashlib.sha256(extension).hexdigest() == release['extensionSha256'], 'Extension integrity failure')
    with zipfile.ZipFile(installer) as z:
        require(set(z.namelist()) == {'AsanDownload.exe', 'Install.cmd', 'README.txt', 'AsanDownload-BrowserExtension.zip'} and len(z.infolist()) == 4, 'Unexpected installer contents')
        require(z.getinfo('AsanDownload.exe').file_size == len(exe) and z.getinfo('AsanDownload-BrowserExtension.zip').file_size == len(extension), 'Invalid nested artifact size')
        require(z.read('AsanDownload.exe') == exe and z.read('AsanDownload-BrowserExtension.zip') == extension, 'Installer and update differ')
    with zipfile.ZipFile(io.BytesIO(extension)) as z:
        entry = z.getinfo('BrowserExtension/manifest.json')
        require(entry.file_size <= 65536, 'Extension manifest too large')
        require(json.loads(z.read(entry))['version'] == release['extensionVersion'], 'Extension version mismatch')
    with zipfile.ZipFile(artifacts / 'AsanDownload-Source.zip') as z:
        require(len(z.infolist()) < 10000 and 'App.cs' in z.namelist(), 'Source archive missing')
        require(not any('PRIVATE KEY' in p or p.upper().endswith('UPDATE-SIGNING-KEY.XML') for p in z.namelist()), 'Secret file in source archive')
    return release, metadata


def atomic_copy(source, target, owner):
    fd, name = tempfile.mkstemp(prefix='.ci-publish-', dir=target.parent)
    temp = Path(name)
    try:
        with os.fdopen(fd, 'wb') as output, source.open('rb') as input_file:
            shutil.copyfileobj(input_file, output)
            os.fchmod(output.fileno(), 0o644) if hasattr(os, 'fchmod') else os.chmod(temp, 0o644)
            if hasattr(os, 'fchown'):
                os.fchown(output.fileno(), *owner)
        os.replace(temp, target)
    finally:
        temp.unlink(missing_ok=True)


def publish(artifacts, public, work, release, health_check):
    previous = json.loads((public / 'release.json').read_text(encoding='utf-8'))
    require(version(release['version']) > version(previous['version']), 'Refusing same-version replacement or downgrade')
    versioned = public / 'downloads' / release['version']
    require(not versioned.exists(), 'Version directory already exists')
    owner = (public.stat().st_uid, public.stat().st_gid)
    backup = work / 'backup'
    for name in STABLE:
        (backup / name).parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(public / name, backup / name)
    version_stage = work / 'versioned'
    version_stage.mkdir()
    for name in STABLE:
        shutil.copyfile(artifacts / name, version_stage / Path(name).name)
    for name in ('AsanDownload-Source.zip', 'deployment.json'):
        shutil.copyfile(artifacts / name, version_stage / name)
    for path in version_stage.iterdir():
        os.chmod(path, 0o644)
        if hasattr(os, 'chown'):
            os.chown(path, *owner)
    os.chmod(version_stage, 0o755)
    if hasattr(os, 'chown'):
        os.chown(version_stage, *owner)
    os.replace(version_stage, versioned)
    try:
        # Publish the manifest last so clients never see an unuploaded release.
        for name in STABLE:
            atomic_copy(artifacts / name, public / name, owner)
        health_check(release)
    except Exception:
        for name in STABLE:
            atomic_copy(backup / name, public / name, owner)
        os.replace(versioned, work / 'failed-versioned')
        raise
    return previous['version']


def health_check(release):
    with urllib.request.urlopen('http://127.0.0.1:4080/api/public/release', timeout=15) as response:
        require(json.load(response) == release, 'Live release API mismatch')
    with urllib.request.urlopen('http://127.0.0.1:4080/', timeout=15) as response:
        require(response.status == 200, 'Website health check failed')


def main():
    import fcntl
    import datetime
    config = json.loads(Path('/etc/asan-download-ci.json').read_text())
    root = Path(config['root'])
    public = root / 'server/public'
    releases = root / 'releases/cicd'
    releases.mkdir(parents=True, exist_ok=True)
    with Path('/run/lock/asan-download-ci.lock').open('w') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        work = Path(tempfile.mkdtemp(prefix='release-', dir=releases))
        artifacts = work / 'artifacts'
        artifacts.mkdir()
        compressed = sys.stdin.buffer.read(LIMIT + 1)
        require(len(compressed) <= LIMIT, 'Compressed upload exceeds limit')
        receive(io.BytesIO(compressed), artifacts)
        commit = subprocess.check_output(['git', 'ls-remote', 'https://github.com/' + REPOSITORY, 'refs/heads/main'], text=True, timeout=30).split()[0]
        release, metadata = validate(artifacts, Path('/etc/asan-download-publisher.xml').read_text(), commit)
        previous = publish(artifacts, public, work, release, health_check)
        report = dict(metadata, previousVersion=previous, publishedAtUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(), signatureVerified=True, healthVerified=True)
        (work / 'published.json').write_text(json.dumps(report, indent=2))
        print(json.dumps(report))


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print('DEPLOYMENT FAILED: ' + str(error), file=sys.stderr)
        sys.exit(1)
