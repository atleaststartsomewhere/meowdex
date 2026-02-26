$ErrorActionPreference = "Stop"

$project = "Meowdex.Desktop/Meowdex.Desktop.csproj"
$config = "Release"
$runtimes = @("win-x64", "linux-x64", "osx-x64", "osx-arm64")

function Publish-ZipRuntime {
    param(
        [string]$Runtime
    )

    $publishDir = "Meowdex.Desktop/bin/$config/net8.0/$Runtime/publish"
    $zipName = "Meowdex.Desktop-$Runtime-$config.zip"
    $zipPath = Join-Path (Get-Location) $zipName

    Write-Host "Publishing $project ($config, $Runtime, single-file)..."
    dotnet publish $project -c $config -r $Runtime --self-contained true -p:PublishSingleFile=true

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Write-Host "Zipping publish output to $zipName..."
    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath
}

foreach ($runtime in $runtimes) {
    Publish-ZipRuntime -Runtime $runtime
}

Write-Host "Done."
