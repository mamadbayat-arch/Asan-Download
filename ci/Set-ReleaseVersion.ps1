param(
    [string]$PublishedVersion,
    [string]$SourcePath = (Join-Path $PSScriptRoot '../App.cs')
)
$ErrorActionPreference = 'Stop'
$source = [IO.File]::ReadAllText($SourcePath)
$match = [regex]::Match($source, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.0"\)')
if (-not $match.Success) { throw 'Cannot find the source assembly version' }
$committed = [version]$match.Groups[1].Value
$published = [version]$PublishedVersion
if ($committed -gt $published) {
    $next = $committed
} else {
    if ($published.Build -ge 65534) { throw 'Release patch component exhausted; bump the minor version in App.cs' }
    $next = [version]::new($published.Major, $published.Minor, ($published.Build + 1))
}
$version = $next.ToString(3)
$updated = [regex]::Replace($source, '(Assembly(?:File)?Version\(")[\d.]+("\))', ('${1}' + $version + '.0${2}'))
if ([regex]::Matches($updated, [regex]::Escape($version + '.0')).Count -lt 2) { throw 'Both assembly versions must be updated' }
[IO.File]::WriteAllText([IO.Path]::GetFullPath($SourcePath), $updated, [Text.UTF8Encoding]::new($false))
Write-Output $version
