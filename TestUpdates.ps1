$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
$productResources=@(Get-ChildItem -LiteralPath Assets/Products -Filter *.png | ForEach-Object {"/resource:$($_.FullName),Products.$($_.Name)"})
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /main:UpdateTests /out:UpdateTests.exe /resource:Assets/UpdatePublicKey.xml,UpdatePublicKey.xml /resource:Assets/Download.ico,Download.ico /resource:Assets/AsanDownload.png,AsanDownload.png @productResources /resource:Assets/Hamtavar.png,Hamtavar.png /resource:Assets/Vazir-Regular.ttf,Vazir-Regular.ttf /resource:Assets/Vazir-OFL.txt,Vazir-OFL.txt /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:Microsoft.CSharp.dll UpdateTests.cs App.cs HistoryStore.cs Engine.cs Glass.cs FdmWidgets.cs Lang.cs Ads.cs Updates.cs Reports.cs Integration.cs DownloadPrompt.cs ProductAds.cs Icons.cs
if($LASTEXITCODE -ne 0){throw 'Update test build failed'}
& ./UpdateTests.exe
if($LASTEXITCODE -ne 0){throw 'Update tests failed'}
