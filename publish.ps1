# Planar Geometry Studio — publish script (Windows PowerShell)
# Publishes the desktop app + engine + drawer as self-contained win-x64 binaries,
# then packages everything into a zip.

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Src = Join-Path $ScriptDir "Source"
$Out = Join-Path $ScriptDir "publish\PlanarGeometryStudio"
$Rid = "win-x64"

Write-Host "=== Cleaning previous publish ==="
if (Test-Path (Join-Path $ScriptDir "publish")) {
    Remove-Item -Recurse -Force (Join-Path $ScriptDir "publish")
}

Write-Host "=== Publishing GeoGen.DesktopApp ==="
dotnet publish "$Src\Launchers\GeoGen.DesktopApp\GeoGen.DesktopApp.csproj" `
    -c Release -r $Rid --self-contained -p:PublishSingleFile=true `
    -o $Out

Write-Host "=== Publishing GeoGen.MainLauncher (engine) ==="
dotnet publish "$Src\Launchers\GeoGen.MainLauncher\GeoGen.MainLauncher.csproj" `
    -c Release -r $Rid --self-contained -p:PublishSingleFile=true `
    -o "$Out\tools\engine"

Write-Host "=== Publishing GeoGen.DrawingLauncher (drawer) ==="
dotnet publish "$Src\Launchers\GeoGen.DrawingLauncher\GeoGen.DrawingLauncher.csproj" `
    -c Release -r $Rid --self-contained -p:PublishSingleFile=true `
    -o "$Out\tools\drawer"

Write-Host "=== Copying MetaPost data files ==="
$DataSrc = "$Src\Launchers\GeoGen.DrawingLauncher\Data"
$DataDst = "$Out\tools\drawer\Data"
if (-not (Test-Path $DataDst)) { New-Item -ItemType Directory -Path $DataDst | Out-Null }
Copy-Item "$DataSrc\*.mp" $DataDst -ErrorAction SilentlyContinue
Copy-Item "$DataSrc\*.txt" $DataDst -ErrorAction SilentlyContinue

Write-Host "=== Cleaning leaked Output/Logs dirs from engine ==="
$Cleanup = @("$Out\tools\engine\Examples\Output", "$Out\tools\engine\Logs", "$Out\tools\drawer\Logs")
foreach ($dir in $Cleanup) {
    if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
}

Write-Host "=== Creating zip ==="
$ZipPath = Join-Path $ScriptDir "publish\PlanarGeometryStudio-$Rid.zip"
Compress-Archive -Path $Out -DestinationPath $ZipPath -Force
Write-Host "=== Done: $ZipPath ==="
