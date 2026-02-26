# Publish Instructions

## Packaged Artifacts
Use the publish script to build packaged outputs in one run:

```
./.scripts/publish.ps1
```

Artifacts are created in the repo root:
- `Meowdex.Desktop-win-x64-Release.zip`
- `Meowdex.Desktop-linux-x64-Release.zip`
- `Meowdex.Desktop-osx-x64-Release.zip`
- `Meowdex.Desktop-osx-arm64-Release.zip`

Notes:
- All outputs are zipped publish folders.

## Single-File Build (Manual)
These commands produce self-contained single-file builds.

### Windows (x64)
```
dotnet publish Meowdex.Desktop/Meowdex.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Linux (x64)
```
dotnet publish Meowdex.Desktop/Meowdex.Desktop.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

### macOS (Intel x64)
```
dotnet publish Meowdex.Desktop/Meowdex.Desktop.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

### macOS (Apple Silicon)
```
dotnet publish Meowdex.Desktop/Meowdex.Desktop.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

## Install/Run (Linux)
1. Unzip the Linux publish archive.
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

## cats.json location (macOS)
```
~/Library/Application Support/Meowdex/cats.json
```
