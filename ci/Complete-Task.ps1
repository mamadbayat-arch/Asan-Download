param([Parameter(Mandatory=$true)][string]$Message, [switch]$StagedOnly)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
if ((git branch --show-current).Trim() -ne 'main') { throw 'Delivery must run from main' }
& git fetch origin main
if ($LASTEXITCODE -ne 0) { throw 'Cannot fetch origin/main' }
& git merge-base --is-ancestor origin/main HEAD
if ($LASTEXITCODE -ne 0) { throw 'main is behind or diverged; reconcile changes before delivery' }
& ./Build.ps1
& ./Test.ps1
& ./TestUpdates.ps1
& node BrowserExtension/test-background.mjs
if ($LASTEXITCODE -ne 0) { throw 'Browser extension tests failed' }
& node server/test.mjs
if ($LASTEXITCODE -ne 0) { throw 'Server tests failed' }
& ./ci/Test-ReleaseVersion.ps1
if (Get-Command py -ErrorAction SilentlyContinue) { & py -3 -m unittest discover -s ci -p 'test_*.py' }
else { & python -m unittest discover -s ci -p 'test_*.py' }
if ($LASTEXITCODE -ne 0) { throw 'Deployment tests failed' }
if (-not $StagedOnly) { & git add -A; if ($LASTEXITCODE -ne 0) { throw 'Staging failed' } }
& git diff --cached --quiet
if ($LASTEXITCODE -eq 1) {
    $authorName = git config user.name
    $authorEmail = git config user.email
    if (-not $authorName) { $authorName = (git log -1 --format='%an').Trim() }
    if (-not $authorEmail) { $authorEmail = (git log -1 --format='%ae').Trim() }
    & git -c "user.name=$authorName" -c "user.email=$authorEmail" commit -m $Message
    if ($LASTEXITCODE -ne 0) { throw 'Commit failed' }
} elseif ($LASTEXITCODE -ne 0) { throw 'Cannot inspect staged changes' }
& git push origin main
if ($LASTEXITCODE -ne 0) { throw 'Push failed; production was not manually modified' }
Write-Output 'Pushed main. Follow the Signed Windows release workflow to confirm deployment.'
