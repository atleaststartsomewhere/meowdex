# Meowdex Desktop Auto-Updater Spec (Draft)

## Goal
Enable the desktop client to self-update from release artifacts (GitHub Releases or equivalent feed) with a safe, low-friction user experience.

## Scope
- Target app: `Meowdex.Desktop` (Avalonia, `net8.0`)
- Update style: in-app check, download, stage, then restart to apply
- Deployment source: GitHub Releases first; keep provider abstraction for later

## Recommended Stack
- Updater framework: Velopack (cross-platform .NET desktop)
- Hosting: GitHub Releases (tagged versions)
- Signing: platform-appropriate code signing for production trust

## Functional Requirements
1. On startup, perform a non-blocking update check.
2. Support a manual "Check for updates" action in Settings.
3. If update is available:
   - Show version and release notes summary.
   - Allow user to download/install now or defer.
4. Download updates with progress reporting.
5. Stage update and prompt restart when ready.
6. Apply update on restart and relaunch app.
7. Handle offline/error paths gracefully without blocking normal app use.

## Non-Functional Requirements
- Startup impact: update check must not block initial UI.
- Reliability: failed downloads must not corrupt current install.
- Observability: log check/download/apply outcomes for diagnostics.
- Security: verify source integrity and use signed artifacts in production.

## Proposed Architecture
### Services
- `IUpdateService`
  - `Task<UpdateCheckResult> CheckForUpdatesAsync(bool silent, CancellationToken ct)`
  - `Task DownloadAndStageAsync(UpdateInfo update, IProgress<double> progress, CancellationToken ct)`
  - `Task ApplyAndRestartAsync(CancellationToken ct)`
- `UpdateService` (Velopack adapter)

### Models
- `UpdateCheckResult`
  - `IsUpdateAvailable`
  - `CurrentVersion`
  - `AvailableVersion`
  - `ReleaseNotes`
  - `Error`
- `UpdateState`
  - `Idle | Checking | Available | Downloading | ReadyToRestart | Failed`

### UI Integration
- Startup hook in `App.axaml.cs` after main window creation.
- Optional manual entry in Settings overlay:
  - `Check for updates`
  - status text/progress
  - `Restart to update` action when staged

## Runtime Flow
1. App launches and initializes UI.
2. Background update check starts.
3. If no update: stay silent (unless manual check).
4. If update exists: show unobtrusive prompt.
5. User accepts update -> download + stage.
6. When staged, prompt restart.
7. App restarts, updater applies patch, app relaunches on new version.

## Release Pipeline (GitHub)
1. Build release artifacts per RID (e.g., `win-x64`, `linux-x64`, optional macOS targets).
2. Package updater-compatible release artifacts.
3. Publish to GitHub Release tag (`vX.Y.Z`).
4. App update feed points to repository release source.

## Versioning
- Semantic Versioning (`MAJOR.MINOR.PATCH`)
- App checks for strictly newer versions than installed.
- Optional channels later: `alpha`, `beta`, `stable`.

## Failure Handling
- Network timeout/unreachable: mark as transient and continue app normally.
- Invalid package/signature: fail update, keep current install, log error.
- Partial download: resume/retry where supported.
- Apply failure: rollback/retain previous working app state.

## Data Compatibility
- Treat app data schema changes as explicit migrations.
- New app versions should tolerate old data where feasible.
- Migration steps must be idempotent and logged.

## Open Questions
1. Initial platform target for auto-update: Windows only, or Windows+Linux now?
2. Silent auto-download or user-confirmed download?
3. Any corporate/offline environments requiring custom feed URLs?
4. Should alpha builds update from separate channel/feed?

## Implementation Plan (Later)
1. Add updater package and `UpdateService` abstraction.
2. Wire startup check in `App.axaml.cs`.
3. Add Settings UI + commands for manual check/apply.
4. Add logging and user-facing error messaging.
5. Build CI release workflow and publish signed artifacts.
6. End-to-end test upgrade from N -> N+1 on clean and populated profiles.
