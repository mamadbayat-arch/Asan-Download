param([string]$OutputPath = 'MehrDownload.exe')
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$productResources=@(Get-ChildItem -LiteralPath Assets/Products -Filter *.png | ForEach-Object {"/resource:$($_.FullName),Products.$($_.Name)"})
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /optimize+ /out:NativeHost.exe /r:System.Web.Extensions.dll NativeHost.cs
if ($LASTEXITCODE -ne 0) { throw 'Browser bridge build failed' }
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /out:MakeIcon.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll MakeIcon.cs Glass.cs Lang.cs Icons.cs
if ($LASTEXITCODE -ne 0) { throw 'Icon build failed' }
& .\MakeIcon.exe
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /resource:NativeHost.exe,NativeHost.exe /resource:BrowserExtension/manifest.json,Browser.manifest.json /resource:BrowserExtension/background.js,Browser.background.js /resource:BrowserExtension/intent.js,Browser.intent.js /resource:BrowserExtension/popup.html,Browser.popup.html /resource:BrowserExtension/popup.js,Browser.popup.js /resource:BrowserExtension/icon128.png,Browser.icon128.png "/out:$OutputPath" /win32icon:Assets/Download.ico /resource:Assets/UpdatePublicKey.xml,UpdatePublicKey.xml /resource:Assets/Download.ico,Download.ico /resource:Assets/AsanDownload.png,AsanDownload.png @productResources /resource:Assets/Hamtavar.png,Hamtavar.png /resource:Assets/Vazir-Regular.ttf,Vazir-Regular.ttf /resource:Assets/Vazir-OFL.txt,Vazir-OFL.txt /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:Microsoft.CSharp.dll App.cs HistoryStore.cs Engine.cs Glass.cs FdmWidgets.cs Lang.cs Ads.cs Updates.cs Reports.cs Integration.cs DownloadPrompt.cs ProductAds.cs Icons.cs
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
