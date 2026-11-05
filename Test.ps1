$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$productResources=@(Get-ChildItem -LiteralPath Assets/Products -Filter *.png | ForEach-Object {"/resource:$($_.FullName),Products.$($_.Name)"})
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /out:Tests.exe /r:System.Web.Extensions.dll Tests.cs Engine.cs
if ($LASTEXITCODE -ne 0) { throw 'Test build failed' }
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /main:InterfaceTests /out:InterfaceTests.exe /resource:Assets/UpdatePublicKey.xml,UpdatePublicKey.xml /resource:Assets/Download.ico,Download.ico /resource:Assets/AsanDownload.png,AsanDownload.png @productResources /resource:Assets/Hamtavar.png,Hamtavar.png /resource:Assets/Vazir-Regular.ttf,Vazir-Regular.ttf /resource:Assets/Vazir-OFL.txt,Vazir-OFL.txt /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:Microsoft.CSharp.dll InterfaceTests.cs App.cs HistoryStore.cs Engine.cs Glass.cs FdmWidgets.cs Lang.cs Ads.cs Updates.cs Reports.cs Integration.cs DownloadPrompt.cs ProductAds.cs Icons.cs
if ($LASTEXITCODE -ne 0) { throw 'Interface test build failed' }
$qualityArgs = @('/nologo','/target:exe','/main:UIQualityTests','/out:UIQualityTests.exe','/resource:Assets/Download.ico,Download.ico','/resource:Assets/Hamtavar.png,Hamtavar.png','/resource:Assets/Vazir-Regular.ttf,Vazir-Regular.ttf','/resource:Assets/Vazir-OFL.txt,Vazir-OFL.txt','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:Microsoft.CSharp.dll','UIQualityTests.cs','App.cs','HistoryStore.cs','Engine.cs','Glass.cs','FdmWidgets.cs','Lang.cs','Ads.cs','Updates.cs','Reports.cs','Integration.cs','DownloadPrompt.cs','ProductAds.cs','Icons.cs')
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" @productResources @qualityArgs
if ($LASTEXITCODE -ne 0) { throw 'UI quality test build failed' }
$testServer = Start-Process node -ArgumentList 'TestServer.js' -WindowStyle Hidden -PassThru
try {
    Start-Sleep -Milliseconds 800
    & .\Tests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
    & .\InterfaceTests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Interface tests failed' }
    & .\UIQualityTests.exe
    if ($LASTEXITCODE -ne 0) { throw 'UI quality tests failed' }
} finally { Stop-Process -Id $testServer.Id -ErrorAction SilentlyContinue }
