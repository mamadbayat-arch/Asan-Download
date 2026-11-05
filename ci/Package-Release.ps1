param([Parameter(Mandatory=$true)][string]$SourceCommit, [string]$RunUrl = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$release = Get-Content -LiteralPath (Join-Path $root 'server/public/release.json') -Raw | ConvertFrom-Json
if ($SourceCommit -cnotmatch '^[a-f0-9]{40}$') { throw 'Invalid source commit' }
$destination = Join-Path $root 'ci-output/release'
New-Item -ItemType Directory -Path (Join-Path $destination 'downloads') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'server/public/release.json') -Destination $destination -Force
foreach ($name in @('AsanDownload-Update.exe','AsanDownload-Windows.zip','AsanDownload-BrowserExtension.zip')) {
    Copy-Item -LiteralPath (Join-Path $root ('server/public/downloads/' + $name)) -Destination (Join-Path $destination 'downloads') -Force
}
$sourceArchive = Join-Path $destination 'AsanDownload-Source.zip'
& git -C $root archive --format=zip "--output=$sourceArchive" $SourceCommit
if ($LASTEXITCODE -ne 0) { throw 'Source archive failed' }
$metadata = [ordered]@{ version=$release.version; sourceCommit=$SourceCommit; repository='mamadbayat-arch/asan-download'; runUrl=$RunUrl }
[IO.File]::WriteAllText((Join-Path $destination 'deployment.json'), ($metadata | ConvertTo-Json -Compress), [Text.UTF8Encoding]::new($false))
Write-Output "Prepared signed release $($release.version) from $SourceCommit"
