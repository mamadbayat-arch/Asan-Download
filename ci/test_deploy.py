import base64
import copy
import hashlib
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import tarfile
import tempfile
import unittest
import zipfile

import deploy
import oidc_gateway
import xml.etree.ElementTree as ET


class DeploymentTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.keys = tempfile.TemporaryDirectory(prefix='asan-test-key-')
        cls.key = Path(cls.keys.name) / 'test.pem'
        cls.openssl = shutil.which('openssl') or 'C:/Program Files/Git/usr/bin/openssl.exe'
        subprocess.run([cls.openssl, 'genpkey', '-algorithm', 'RSA', '-pkeyopt', 'rsa_keygen_bits:2048', '-out', str(cls.key)], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        modulus = subprocess.check_output([cls.openssl, 'rsa', '-in', str(cls.key), '-modulus', '-noout'], text=True).strip().split('=', 1)[1]
        cls.public = '<RSAKeyValue><Modulus>' + base64.b64encode(bytes.fromhex(modulus)).decode() + '</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>'

    @classmethod
    def tearDownClass(cls):
        cls.keys.cleanup()

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='asan-deploy-test-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.artifacts = self.root / 'artifacts'
        (self.artifacts / 'downloads').mkdir(parents=True)
        exe = b'MZ deterministic test executable'
        ext = io.BytesIO()
        with zipfile.ZipFile(ext, 'w') as z:
            z.writestr('BrowserExtension/manifest.json', json.dumps({'version': '1.7.1'}))
        extension = ext.getvalue()
        (self.artifacts / 'downloads/AsanDownload-Update.exe').write_bytes(exe)
        (self.artifacts / 'downloads/AsanDownload-BrowserExtension.zip').write_bytes(extension)
        with zipfile.ZipFile(self.artifacts / 'downloads/AsanDownload-Windows.zip', 'w') as z:
            z.writestr('AsanDownload.exe', exe)
            z.writestr('AsanDownload-BrowserExtension.zip', extension)
            z.writestr('Install.cmd', 'installer fixture')
            z.writestr('README.txt', 'fixture')
        with zipfile.ZipFile(self.artifacts / 'AsanDownload-Source.zip', 'w') as z:
            z.writestr('App.cs', '// source fixture')
        self.commit = 'a' * 40
        self.release = dict(version='1.8.4', available=True, updateUrl='/downloads/AsanDownload-Update.exe', sha256=hashlib.sha256(exe).hexdigest(), exeSize=len(exe), downloadUrl='/downloads/AsanDownload-Windows.zip', size=(self.artifacts / 'downloads/AsanDownload-Windows.zip').stat().st_size, extensionDownloadUrl='/downloads/AsanDownload-BrowserExtension.zip', extensionVersion='1.7.1', extensionSize=len(extension), extensionSha256=hashlib.sha256(extension).hexdigest())
        payload = '\n'.join([self.release['version'], self.release['sha256'], str(len(exe)), self.release['updateUrl']]).encode()
        signature = subprocess.run([self.openssl, 'dgst', '-sha256', '-sign', str(self.key)], input=payload, capture_output=True, check=True).stdout
        self.release['signature'] = base64.b64encode(signature).decode()
        (self.artifacts / 'release.json').write_text(json.dumps(self.release))
        (self.artifacts / 'deployment.json').write_text(json.dumps(dict(version='1.8.4', sourceCommit=self.commit, repository=deploy.REPOSITORY)))

    def test_accepts_signed_release_and_exact_installer(self):
        release, metadata = deploy.validate(self.artifacts, self.public, self.commit)
        self.assertEqual(release, self.release)
        self.assertEqual(metadata['sourceCommit'], self.commit)

    def test_rejects_tampered_manifest_and_foreign_urls(self):
        for field, value in [('version', '1.8.5'), ('sha256', '0' * 64), ('exeSize', 1), ('updateUrl', 'https://evil.example/update.exe')]:
            with self.subTest(field=field):
                altered = dict(self.release, **{field: value})
                with self.assertRaises(ValueError):
                    deploy.verify_signature(altered, self.public)

    def test_rejects_tampered_binary(self):
        (self.artifacts / 'downloads/AsanDownload-Update.exe').write_bytes(b'MZ corrupt')
        with self.assertRaisesRegex(ValueError, 'integrity'):
            deploy.validate(self.artifacts, self.public, self.commit)

    def test_rejects_stale_commit(self):
        with self.assertRaisesRegex(ValueError, 'Stale release'):
            deploy.validate(self.artifacts, self.public, 'b' * 40)

    def test_rejects_installer_with_different_executable(self):
        path = self.artifacts / 'downloads/AsanDownload-Windows.zip'
        with zipfile.ZipFile(path, 'w') as z:
            z.writestr('AsanDownload.exe', 'untrusted')
        release = dict(self.release, size=path.stat().st_size)
        (self.artifacts / 'release.json').write_text(json.dumps(release))
        with self.assertRaisesRegex(ValueError, 'installer contents'):
            deploy.validate(self.artifacts, self.public, self.commit)

    def archive(self, extra=None):
        stream = io.BytesIO()
        with tarfile.open(fileobj=stream, mode='w:gz') as tar:
            for name in sorted(deploy.FILES):
                tar.add(self.artifacts / name, arcname=name)
            if extra:
                tar.addfile(extra, io.BytesIO(b'x') if extra.isreg() else None)
        stream.seek(0)
        return stream

    def test_receives_complete_allowlisted_archive(self):
        destination = self.root / 'received'
        destination.mkdir()
        deploy.receive(self.archive(), destination)
        self.assertEqual(set(str(p.relative_to(destination)).replace(os.sep, '/') for p in destination.rglob('*') if p.is_file()), deploy.FILES)

    def test_rejects_traversal_links_extra_and_duplicate_files(self):
        for name, kind in [('../outside', tarfile.REGTYPE), ('unexpected', tarfile.REGTYPE), ('release.json', tarfile.REGTYPE), ('downloads/link', tarfile.SYMTYPE)]:
            with self.subTest(name=name):
                directory = self.root / ('received-' + str(len(list(self.root.iterdir()))))
                directory.mkdir()
                entry = tarfile.TarInfo(name)
                entry.type = kind
                entry.size = 1 if kind == tarfile.REGTYPE else 0
                entry.linkname = '/etc/passwd' if kind == tarfile.SYMTYPE else ''
                with self.assertRaises(ValueError):
                    deploy.receive(self.archive(entry), directory)

    def prepare_public(self):
        public = self.root / 'public'
        (public / 'downloads').mkdir(parents=True)
        for name in deploy.STABLE:
            (public / name).write_text(json.dumps({'version': '1.8.3'}) if name == 'release.json' else 'previous bytes')
        work = self.root / 'work'
        work.mkdir()
        return public, work

    def test_publishes_manifest_last_and_preserves_runtime_data(self):
        public, work = self.prepare_public()
        runtime = public / 'unrelated-data.json'
        runtime.write_text('preserve')
        checked = []
        previous = deploy.publish(self.artifacts, public, work, self.release, lambda r: checked.append(json.loads((public / 'release.json').read_text()) == r))
        self.assertEqual(previous, '1.8.3')
        self.assertEqual(checked, [True])
        self.assertEqual(runtime.read_text(), 'preserve')
        self.assertTrue((public / 'downloads/1.8.4/deployment.json').is_file())

    def test_rolls_back_all_stable_files_after_health_failure(self):
        public, work = self.prepare_public()
        before = {name: (public / name).read_bytes() for name in deploy.STABLE}
        def fail(_):
            raise RuntimeError('health failure')
        with self.assertRaisesRegex(RuntimeError, 'health failure'):
            deploy.publish(self.artifacts, public, work, self.release, fail)
        self.assertEqual({name: (public / name).read_bytes() for name in deploy.STABLE}, before)
        self.assertFalse((public / 'downloads/1.8.4').exists())
        self.assertTrue((work / 'failed-versioned').is_dir())

    def test_rejects_downgrade_before_writing(self):
        public, work = self.prepare_public()
        for candidate in ['1.8.3', '1.8.2']:
            with self.subTest(version=candidate), self.assertRaisesRegex(ValueError, 'downgrade'):
                deploy.publish(self.artifacts, public, work, dict(self.release, version=candidate), lambda _: None)

    def test_oidc_restricts_repository_branch_environment_and_expiry(self):
        def encode(data):
            return base64.urlsafe_b64encode(data).rstrip(b'=').decode()
        xml = ET.fromstring(self.public)
        keyset = {'keys': [dict(kid='test', kty='RSA', use='sig', n=xml.findtext('Modulus').rstrip('=').replace('+', '-').replace('/', '_'), e='AQAB')]}
        now = 1700000000
        claims = dict(iss=oidc_gateway.ISSUER, aud=oidc_gateway.AUDIENCE, repository=deploy.REPOSITORY, repository_id=oidc_gateway.REPOSITORY_ID, repository_owner_id=oidc_gateway.OWNER_ID, ref='refs/heads/main', environment='production', workflow_ref=deploy.REPOSITORY+'/.github/workflows/release.yml@refs/heads/main', event_name='push', exp=now+300, nbf=now-5, iat=now-5)
        def token(values):
            payload = encode(json.dumps({'alg':'RS256','kid':'test'}).encode()) + '.' + encode(json.dumps(values).encode())
            signature = subprocess.run([self.openssl, 'dgst', '-sha256', '-sign', str(self.key)], input=payload.encode(), capture_output=True, check=True).stdout
            return payload + '.' + encode(signature)
        self.assertEqual(oidc_gateway.authenticate(token(claims), keyset, now)['repository'], deploy.REPOSITORY)
        for field, value in [('ref','refs/heads/evil'),('repository_id','1'),('environment','preview'),('event_name','pull_request'),('aud','other'),('exp',now-60)]:
            with self.subTest(claim=field), self.assertRaises(ValueError):
                oidc_gateway.authenticate(token(dict(claims, **{field:value})), keyset, now)
        signed = token(claims)
        with self.assertRaises(ValueError):
            oidc_gateway.authenticate(signed[:-10]+'AAAAAAAAAA', keyset, now)


if __name__ == '__main__':
    unittest.main()
