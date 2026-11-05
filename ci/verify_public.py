"""Verify the deployed public manifest and binary without running an installer."""
import hashlib
import json
from pathlib import Path
import sys
import urllib.request
from deploy import require, verify_signature

root = Path(__file__).resolve().parent.parent
expected = json.loads((root / 'ci-output/release/release.json').read_text())
origin = 'https://downloader.hamtavar.ir'
with urllib.request.urlopen(origin + '/api/public/release', timeout=30) as response:
    actual = json.load(response)
require(actual == expected, 'Public release API mismatch')
verify_signature(actual, (root / 'Assets/UpdatePublicKey.xml').read_text())
with urllib.request.urlopen(origin + actual['updateUrl'], timeout=60) as response:
    binary = response.read(actual['exeSize'] + 1)
require(len(binary) == actual['exeSize'] and binary[:2] == b'MZ' and hashlib.sha256(binary).hexdigest() == actual['sha256'], 'Public binary integrity failed')
print('VERIFIED public signed automatic update ' + actual['version'])
