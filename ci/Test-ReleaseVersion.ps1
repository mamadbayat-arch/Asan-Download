$ErrorActionPreference = 'Stop'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('asan-version-test-' + [Guid]::NewGuid().ToString('N') + '.cs')
$cases = @(
    @('1.8.3','1.8.3','1.8.4'),
    @('1.8.3','1.8.9','1.8.10'),
    @('1.9.0','1.8.9','1.9.0'),
    @('1.8.3','2.0.0','2.0.1')
)
try {
    foreach ($case in $cases) {
        $source = '[assembly: System.Reflection.AssemblyVersion("' + $case[0] + '.0")]' + "`n" + '[assembly: System.Reflection.AssemblyFileVersion("' + $case[0] + '.0")]'
        [IO.File]::WriteAllText($temp, $source)
        $actual = & (Join-Path $PSScriptRoot 'Set-ReleaseVersion.ps1') -PublishedVersion $case[1] -SourcePath $temp
        if ($actual -ne $case[2] -or [regex]::Matches([IO.File]::ReadAllText($temp), [regex]::Escape($case[2] + '.0')).Count -ne 2) { throw 'Release version regression' }
    }
    Write-Output 'PASS: release versions increase after publishing and on retries; explicit newer source versions are preserved'
} finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Force }
}
