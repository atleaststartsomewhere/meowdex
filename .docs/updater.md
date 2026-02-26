# Updater (Implemented)

## What Is Implemented
- In-app update check in **Settings -> Updates**.
- Background check on app startup (best effort, non-blocking).
- Manifest-based version compare (`current` vs `latest`).
- Per-platform download URL selection (`win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`).
- "Open Download" launches the platform artifact URL in the default browser.

This is a safe phase-1 updater (check + directed install), not in-place binary patching.

## Configuration
Set your manifest URL in:
- `Meowdex.Desktop/UpdateConfig.cs`

```csharp
public const string ManifestUrl = "https://raw.githubusercontent.com/<owner>/<repo>/<branch>/update-manifest.json";
```

If empty, update checks will report "not configured".

## Manifest Format
Host JSON like this:

```json
{
  "version": "1.0.3",
  "releaseNotes": "Bug fixes and UX improvements.",
  "downloads": {
    "win-x64": "https://github.com/<owner>/<repo>/releases/download/v1.0.3/Meowdex.Desktop-win-x64-Release.zip",
    "linux-x64": "https://github.com/<owner>/<repo>/releases/download/v1.0.3/Meowdex.Desktop-linux-x64-Release.zip",
    "osx-x64": "https://github.com/<owner>/<repo>/releases/download/v1.0.3/Meowdex.Desktop-osx-x64-Release.zip",
    "osx-arm64": "https://github.com/<owner>/<repo>/releases/download/v1.0.3/Meowdex.Desktop-osx-arm64-Release.zip"
  }
}
```

## Manual Release Steps (After You Commit)
1. Bump your manual release base if needed:
   - `Meowdex.Desktop/Meowdex.Desktop.csproj` -> `<VersionPrefix>1.0.x</VersionPrefix>`
2. Create release artifacts:
   - `./.scripts/publish.ps1`
3. Create a GitHub release/tag (e.g. `v1.0.3`) and upload the generated zip artifacts.
4. Update the hosted `update-manifest.json`:
   - set `version` to the new release version
   - set each platform URL to the new release assets
   - update `releaseNotes`
5. Commit/publish the manifest change.
6. Client flow:
   - open app -> Settings -> Updates -> Check for Updates
   - if update exists, click Open Download and install manually.

## Notes
- Internal build identity shown in Settings (`Build: ...`) is independent of release semver.
- Version compare uses semver core (`major.minor.patch`) and ignores build metadata suffixes.
