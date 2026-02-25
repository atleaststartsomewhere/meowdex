$ErrorActionPreference = "Stop"

$project = "Meowdex.Desktop/Meowdex.Desktop.csproj"
$config = "Release"
$runtimes = @("win-x64", "linux-x64", "osx-x64", "osx-arm64")

foreach ($runtime in $runtimes) {
    $publishDir = "Meowdex.Desktop/bin/$config/net8.0/$runtime/publish"
    $zipName = "Meowdex.Desktop-$runtime-$config.zip"
    $zipPath = Join-Path (Get-Location) $zipName

    Write-Host "Publishing $project ($config, $runtime, single-file)..."
    dotnet publish $project -c $config -r $runtime --self-contained true -p:PublishSingleFile=true

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Write-Host "Zipping publish output to $zipName..."
    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath
}

Write-Host "Done."
