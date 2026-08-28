param(
    [string]$Runtime,
    [string]$Version
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $Version) {
    $Version = (Get-Content (Join-Path $ScriptDir "VERSION") -Raw).Trim()
}

if (-not $Runtime) {
    $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
    $arch = if ($architecture -eq "arm64") { "arm64" } else { "x64" }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $Runtime = "win-$arch"
    } elseif ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) {
        $Runtime = "osx-$arch"
    } else {
        $Runtime = "linux-$arch"
    }
}

if ($Runtime -notmatch '^(win|linux|osx)-(x64|arm64)$') {
    throw "Unsupported runtime '$Runtime'."
}

if ($Version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z.-]+)?$') {
    throw "Invalid version '$Version'."
}

$SourceDir = Join-Path $ScriptDir "Source"
$ArtifactDir = Join-Path $ScriptDir "artifacts"
$StagingDir = Join-Path $ArtifactDir "staging\$Runtime"
$AppDir = Join-Path $StagingDir "PlanarGeometryStudio"
$AssetStem = "PlanarGeometryStudio-v$Version-$Runtime"

if (Test-Path $StagingDir) {
    Remove-Item -Recurse -Force $StagingDir
}

New-Item -ItemType Directory -Force -Path $AppDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $AppDir "tools\engine") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $AppDir "tools\drawer") | Out-Null
New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null

$PublishArgs = @(
    "--configuration", "Release",
    "--runtime", $Runtime,
    "--self-contained", "true",
    "-p:Version=$Version",
    "-p:PublishSingleFile=true",
    "-p:PublishTrimmed=false",
    "-p:EnableCompressionInSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:DebugSymbols=false",
    "-p:DebugType=None"
)

Write-Host "Publishing Planar Geometry Studio $Version for $Runtime"

& dotnet publish (Join-Path $SourceDir "Launchers\GeoGen.DesktopApp\GeoGen.DesktopApp.csproj") @PublishArgs --output $AppDir
& dotnet publish (Join-Path $SourceDir "Launchers\GeoGen.MainLauncher\GeoGen.MainLauncher.csproj") @PublishArgs --output (Join-Path $AppDir "tools\engine")
& dotnet publish (Join-Path $SourceDir "Launchers\GeoGen.DrawingLauncher\GeoGen.DrawingLauncher.csproj") @PublishArgs --output (Join-Path $AppDir "tools\drawer")

$DrawerData = Join-Path $AppDir "tools\drawer\Data"
New-Item -ItemType Directory -Force -Path $DrawerData | Out-Null
Copy-Item -Recurse -Force (Join-Path $SourceDir "Launchers\GeoGen.DrawingLauncher\Data\*") $DrawerData

@(
    (Join-Path $AppDir "tools\engine\Examples\Output"),
    (Join-Path $AppDir "tools\engine\Logs"),
    (Join-Path $AppDir "tools\drawer\Logs")
) | ForEach-Object {
    if (Test-Path $_) { Remove-Item -Recurse -Force $_ }
}

@(
    "ReadableWithoutProofs",
    "ReadableWithProofs",
    "JsonOutput",
    "ReadableBestTheorems",
    "JsonBestTheorems"
) | ForEach-Object {
    New-Item -ItemType Directory -Force -Path (Join-Path $AppDir "tools\engine\Examples\Output\$_") | Out-Null
}

Copy-Item (Join-Path $ScriptDir "LICENSE") (Join-Path $AppDir "LICENSE.txt")
Copy-Item (Join-Path $ScriptDir "README.md") (Join-Path $AppDir "README.md")
Copy-Item (Join-Path $ScriptDir "CHANGELOG.md") (Join-Path $AppDir "CHANGELOG.md")

if ($Runtime.StartsWith("osx-")) {
    $BundleDir = Join-Path $StagingDir "Planar Geometry Studio.app"
    $MacOsDir = Join-Path $BundleDir "Contents\MacOS"
    New-Item -ItemType Directory -Force -Path $MacOsDir | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $BundleDir "Contents\Resources") | Out-Null
    Copy-Item -Recurse -Force (Join-Path $AppDir "*") $MacOsDir

    $InfoPlist = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleDisplayName</key><string>Planar Geometry Studio</string>
<key>CFBundleExecutable</key><string>PlanarGeometryStudio</string>
<key>CFBundleIdentifier</key><string>com.nikolaveselinov.planargeometrystudio</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>$Version</string>
<key>CFBundleVersion</key><string>$Version</string>
<key>LSMinimumSystemVersion</key><string>12.0</string>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
"@
    Set-Content -Path (Join-Path $BundleDir "Contents\Info.plist") -Value $InfoPlist -Encoding utf8
    $AssetPath = Join-Path $ArtifactDir "$AssetStem.zip"
    if (Test-Path $AssetPath) { Remove-Item -Force $AssetPath }
    Compress-Archive -Path $BundleDir -DestinationPath $AssetPath
} elseif ($Runtime.StartsWith("linux-")) {
    $AssetPath = Join-Path $ArtifactDir "$AssetStem.tar.gz"
    if (Test-Path $AssetPath) { Remove-Item -Force $AssetPath }
    & tar -C $StagingDir -czf $AssetPath PlanarGeometryStudio
} else {
    $AssetPath = Join-Path $ArtifactDir "$AssetStem.zip"
    if (Test-Path $AssetPath) { Remove-Item -Force $AssetPath }
    Compress-Archive -Path $AppDir -DestinationPath $AssetPath
}

Write-Output $AssetPath
