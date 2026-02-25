# Publish Instructions

## Packaged Zip (Friend-Friendly Handoff)
Use the Windows script to build and zip both Windows and Linux outputs in one go.

```
./.scripts/publish.ps1
```

The zips are created in the repo root, e.g.:
`Meowdex.Desktop-win-x64-Release.zip`
`Meowdex.Desktop-linux-x64-Release.zip`

Each zip is a **self‑contained single‑file** build inside the publish folder.

## Single-File Build (Manual)
These commands produce a single-file executable (self-contained) if you need to run them directly.

### Windows (x64)
```
dotnet publish Meowdex.Desktop/Meowdex.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Linux (x64)
```
dotnet publish Meowdex.Desktop/Meowdex.Desktop.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

## Install/Run (Linux)
1. Unzip the publish folder or the zip file.
2. Make the binary executable:
```
chmod +x Meowdex.Desktop
```
3. Run:
```
./Meowdex.Desktop
```

## cats.json location (Linux)
```
~/.config/Meowdex/cats.json
```
