param([switch]$PackageOnly, [string]$SigningKeyPath = '')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
if([string]::IsNullOrWhiteSpace($SigningKeyPath)){$SigningKeyPath=Join-Path $PSScriptRoot 'UPDATE-SIGNING-KEY.xml'}
if(-not $PackageOnly -and [string]::IsNullOrWhiteSpace($env:UPDATE_SIGNING_KEY_XML) -and -not (Test-Path -LiteralPath $SigningKeyPath -PathType Leaf)){throw 'The original publisher signing key is required. Set UPDATE_SIGNING_KEY_XML, provide SigningKeyPath, or use -PackageOnly.'}
& ./Build.ps1
if($LASTEXITCODE -ne 0){throw 'App build failed'}
$taskVersion=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $PSScriptRoot 'MehrDownload.exe')).Version.ToString(3)
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'server/public/downloads') -Force | Out-Null
$taskExtensionStage=Join-Path $PSScriptRoot 'Release/ExtensionPackage/BrowserExtension'
New-Item -ItemType Directory -Path $taskExtensionStage -Force | Out-Null
Copy-Item -LiteralPath BrowserExtension/manifest.json,BrowserExtension/background.js,BrowserExtension/intent.js,BrowserExtension/popup.html,BrowserExtension/popup.js,BrowserExtension/icon128.png,BrowserExtension/README-fa.txt,BrowserExtension/guide.html -Destination $taskExtensionStage -Force
Copy-Item -LiteralPath server/public/assets/KalamehWeb-Regular.woff2 -Destination $taskExtensionStage -Force
Compress-Archive -LiteralPath $taskExtensionStage -DestinationPath Release/AsanDownload-BrowserExtension.zip -Force
Copy-Item -LiteralPath MehrDownload.exe -Destination Release/AsanDownload.exe -Force
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'Release/README.txt'),"آسان دانلود — نسخه $taskVersion`r`nفایل ZIP را استخراج و Install.cmd را اجرا کنید.`r`nفایل AsanDownload-BrowserExtension.zip شامل افزونهٔ Chrome و Edge و راهنمای فعال‌سازی است.`r`nهنگام بازشدن برنامه نسخهٔ جدید خودکار بررسی و نصب می‌شود.`r`nتنظیمات و سابقهٔ دانلودها حفظ می‌شوند. اتصال ناموفق مانع اجرای برنامه نیست.`r`n")
$taskDownloadDir=Join-Path $PSScriptRoot 'server/public/downloads'
if($PackageOnly){$taskDownloadDir=Join-Path $taskDownloadDir $taskVersion;New-Item -ItemType Directory -Path $taskDownloadDir -Force | Out-Null}
Copy-Item -LiteralPath Release/AsanDownload-BrowserExtension.zip -Destination (Join-Path $taskDownloadDir 'AsanDownload-BrowserExtension.zip') -Force
Compress-Archive -LiteralPath Release/AsanDownload.exe,Release/Install.cmd,Release/README.txt,Release/AsanDownload-BrowserExtension.zip -DestinationPath (Join-Path $taskDownloadDir 'AsanDownload-Windows.zip') -Force
if($PackageOnly){Write-Output "Standalone package $taskVersion ready in $taskDownloadDir. The signed automatic-update manifest and executable were not modified.";return}
Copy-Item -LiteralPath MehrDownload.exe -Destination server/public/downloads/AsanDownload-Update.exe -Force
$taskExe=Get-Item server/public/downloads/AsanDownload-Update.exe
$taskHash=(Get-FileHash -LiteralPath $taskExe.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
$taskUrl='/downloads/AsanDownload-Update.exe'
$taskPayload=$taskVersion+"`n"+$taskHash+"`n"+$taskExe.Length.ToString([Globalization.CultureInfo]::InvariantCulture)+"`n"+$taskUrl
$taskRsa=New-Object Security.Cryptography.RSACryptoServiceProvider
$taskRsa.PersistKeyInCsp=$false
try {
 $taskKeyXml=$env:UPDATE_SIGNING_KEY_XML
 if([string]::IsNullOrWhiteSpace($taskKeyXml)){$taskKeyXml=[IO.File]::ReadAllText($SigningKeyPath)}
 $taskRsa.FromXmlString($taskKeyXml)
 $taskPublic=New-Object Security.Cryptography.RSACryptoServiceProvider
 $taskPublic.PersistKeyInCsp=$false
 try {
  $taskPublic.FromXmlString([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Assets/UpdatePublicKey.xml')))
  $taskSignatureBytes=$taskRsa.SignData([Text.Encoding]::UTF8.GetBytes($taskPayload),[Security.Cryptography.CryptoConfig]::MapNameToOID('SHA256'))
  if(-not $taskPublic.VerifyData([Text.Encoding]::UTF8.GetBytes($taskPayload),[Security.Cryptography.CryptoConfig]::MapNameToOID('SHA256'),$taskSignatureBytes)){throw 'Signing key does not match the pinned publisher identity'}
  $taskSignature=[Convert]::ToBase64String($taskSignatureBytes)
 } finally {$taskPublic.Dispose()}
} finally {$taskRsa.Dispose();$taskKeyXml=$null;$env:UPDATE_SIGNING_KEY_XML=$null}
$taskExtensionManifest=Get-Content -LiteralPath BrowserExtension/manifest.json -Raw|ConvertFrom-Json
$taskRelease=[ordered]@{version=$taskVersion;available=$true;downloadUrl='/downloads/AsanDownload-Windows.zip';size=(Get-Item server/public/downloads/AsanDownload-Windows.zip).Length;updateUrl=$taskUrl;exeSize=$taskExe.Length;sha256=$taskHash;signature=$taskSignature;extensionDownloadUrl='/downloads/AsanDownload-BrowserExtension.zip';extensionVersion=$taskExtensionManifest.version;extensionSize=(Get-Item Release/AsanDownload-BrowserExtension.zip).Length;extensionSha256=(Get-FileHash Release/AsanDownload-BrowserExtension.zip -Algorithm SHA256).Hash.ToLowerInvariant()}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'server/public/release.json'),($taskRelease|ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))
'Signed public release ready. Private signing key excluded.'
