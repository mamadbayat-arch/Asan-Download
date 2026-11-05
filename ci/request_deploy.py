"""Request a short-lived GitHub identity token and submit the release over TLS."""
import json
import os
from pathlib import Path
import tarfile
import tempfile
import urllib.parse
import urllib.request

audience = 'https://downloader.hamtavar.ir'
url = os.environ['ACTIONS_ID_TOKEN_REQUEST_URL'] + '&audience=' + urllib.parse.quote(audience, safe='')
request = urllib.request.Request(url, headers={'Authorization': 'Bearer ' + os.environ['ACTIONS_ID_TOKEN_REQUEST_TOKEN']})
with urllib.request.urlopen(request, timeout=30) as response:
    token = json.load(response)['value']
print('::add-mask::' + token, flush=True)
root = Path(__file__).resolve().parent.parent / 'ci-output/release'
with tempfile.TemporaryFile() as upload:
    with tarfile.open(fileobj=upload, mode='w:gz') as archive:
        for name in ('release.json', 'deployment.json', 'AsanDownload-Source.zip', 'downloads/AsanDownload-Update.exe', 'downloads/AsanDownload-Windows.zip', 'downloads/AsanDownload-BrowserExtension.zip'):
            archive.add(root / name, arcname=name)
    upload.seek(0)
    request = urllib.request.Request(audience + '/api/ci/deploy', data=upload.read(), headers={'Authorization': 'Bearer ' + token, 'Content-Type': 'application/gzip'}, method='POST')
    with urllib.request.urlopen(request, timeout=240) as response:
        print(response.read().decode())
