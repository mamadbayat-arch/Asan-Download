# آسان دانلود · Asan Download

Persian Windows download manager with a browser extension and a small self-hosted website/admin service.

- Desktop source base: **1.8.3**, C# / Windows Forms / .NET Framework. CI assigns each published build a version newer than the live automatic-update channel.
- Chrome and Microsoft Edge extension: **1.7.1**, Manifest V3.
- Website: https://downloader.hamtavar.ir/

## Features

- Up to 16 download connections, adaptive chunk scheduling, retries, pause and resume.
- Batch processing: paste many links at once, they are queued, shown in the download table and fetched one after another while the current file keeps downloading.
- Pausing or failing the current download keeps later links queued and preserves the current URL and destination for retry.
- One default download folder, automatic filenames and collision-safe destinations.
- Persistent completed-download history with independent records, atomic writes and recovery backups.
- Persian completion dates, active download duration and average speed; clickable MB/s ↔ Mb/s display.
- Confirmation before browser downloads; automatic interception requires a recent real click or keyboard action. Browser-startup downloads do not generate unsolicited prompts.
- Free Download Manager inspired interface: dark navy title bar, filterable sidebar, FDM styled download table with per-row progress, speed and remaining time, drop zone, status bar and a sponsored strip that stays pinned to the bottom of the window.
- Restrained motion: one shared ticker drives hover/focus feedback, progress bars, statistics and toasts. Resizing, language changes and theme changes do not replay entrance animations or cover the window with transition layers.
- Download failures remain visible in the status strip without blocking the interface; the retry URL and remaining queue are preserved.
- Bilingual interface: Persian and English can be switched at any time from the side bar. The reading direction, the table columns, the tool bars, the window buttons, the digits and the calendar dates all follow the selected language, and Persian runs with a slightly smaller type size.
- White and green interface, light and dark variants, embedded Kalameh fonts and Hamtavar branding.
- Windows startup option, browser native messaging, signed automatic updates, centrally managed advertisements and issue reporting.

## Build and run

Completed work is automatically committed/pushed on `main` after validation.
GitHub Actions then tests, signs with the existing publisher identity, deploys
through GitHub OIDC and verifies the public automatic-update package. See
[DEPLOYMENT.md](DEPLOYMENT.md) for the pipeline and recovery procedure.

Use Windows 10/11 with .NET Framework 4.8, PowerShell and Node.js 22 or later. The build uses the C# compiler included with Windows and does not require Visual Studio.

```powershell
./Build.ps1
./MehrDownload.exe
```

To install for the current Windows user, run `Install.cmd` after building. The native messaging host and extension files are installed with the app.

In Chrome/Edge, enable Developer mode on the extensions page and choose **Load unpacked** for the installed `BrowserExtension` folder. After updating extension files, click **Reload** and refresh the download webpage. The extension folder also includes a Persian illustrated guide.

Downloads requiring browser cookies or login, streaming media and webpage URLs are not supported as direct downloads. Download speed remains subject to the network and source server. Resume requires compatible server range/validator support.

## Tests

```powershell
./Test.ps1
./TestUpdates.ps1
node BrowserExtension/test-background.mjs
node server/test.mjs
```

These cover download integrity, pause/resume, history persistence and recovery, Persian dates, UI behavior, signed-update validation and rollback, browser consent/fallback and unwanted startup requests.

Source-only checkouts run all update protocol and rollback tests. The final check of the actual publisher-signed release package runs when generated release artifacts are present; an incomplete or invalid artifact pair fails validation.

`UIQualityTests.cs`, included in `Test.ps1`, also exercises language/theme/sidebar persistence, keyboard controls, confirmation and batch dialogs, rapid toggles, table sorting and virtualization, and animation cleanup. It captures Persian/English and light/dark previews at 1280×940, 1060×760 and 960×640 under `qa-artifacts/after/`. These checks run against isolated settings and do not touch the current user's download history.

The main interface adapts its action bars to smaller windows and keeps the sidebar toggle visible while the navigation scrolls vertically. Completed files open with double click or Enter; right click and the row menu expose file actions. Theme and sidebar preferences persist alongside language.

To build a separate executable while another copy is running:

```powershell
New-Item -ItemType Directory -Force qa-artifacts | Out-Null
./Build.ps1 -OutputPath qa-artifacts/MehrDownload.exe
```

`AdaptiveTests.cs` and `AdaptiveServer.mjs` provide the controlled slow-connection scheduling benchmark. `NativeSmoke.mjs ping` checks the installed native host without starting a download.

## Website and administration

```powershell
cd server
# Set ADMIN_PASSWORD to a unique password of at least 12 characters.
# Optional: ADMIN_USERNAME, DATA_DIR, PUBLIC_ORIGIN, PORT, NODE_ENV.
node setup-admin.mjs
npm start
```

The default port is 4080. Set `PUBLIC_ORIGIN` to the deployed HTTPS origin. Admin pages are `/admin` and `/admin/reports`; runtime data is stored in `server/data` unless `DATA_DIR` is configured. Supply secrets through the environment; do not commit them.

## Release builds

`BuildRelease.ps1` creates the Windows bundle, extension ZIP, update executable and signed release manifest. It requires the existing private publisher key `UPDATE-SIGNING-KEY.xml`, which is intentionally excluded from Git. `Assets/UpdatePublicKey.xml` is the public verification key used by installed clients. Keep the matching private key in secure backup; a new key will not sign updates accepted by existing clients.

```powershell
./BuildRelease.ps1
```

Use `./BuildRelease.ps1 -SigningKeyPath <private-key-path>` when the original publisher key is stored outside the checkout. To build a standalone manual installer without publishing or changing the signed automatic-update channel, use `./BuildRelease.ps1 -PackageOnly`. These standalone packages are written under `server/public/downloads/<version>/` and do not include a new update manifest.

Release files are generated in `server/public/downloads` and the manifest in `server/public/release.json`. Generated executables, archives, signing secrets, credentials, local histories and test output are excluded from the repository.

## Source layout

| Path | Purpose |
| --- | --- |
| `App.cs`, `Glass.cs`, `FdmWidgets.cs`, `Lang.cs` | Windows interface, FDM design system, animation engine, Persian/English strings and installer |
| `Engine.cs` | Download engine and resume state |
| `HistoryStore.cs` | Durable completed-download records |
| `Updates.cs` | Signed updates and rollback |
| `Integration.cs`, `NativeHost.cs` | Windows/browser integration |
| `BrowserExtension/` | Extension, guide and regression tests |
| `Ads.cs`, `Reports.cs` | Central ads and issue reporting |
| `server/` | Node.js website, admin and public APIs |
| `Assets/` | Embedded fonts, branding and public verification key |

Brand assets and bundled fonts retain their existing rights; this repository does not grant a new license for them.
