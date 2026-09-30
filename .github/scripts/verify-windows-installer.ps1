param([string]$Installer)
$ErrorActionPreference = 'Stop'
$installDirectory = Join-Path $env:RUNNER_TEMP 'Studio Install With Spaces'
$group = 'Planar Geometry Studio Installer Test'
$installLog = Join-Path $env:RUNNER_TEMP 'studio-install.log'
$process = Start-Process -FilePath $Installer -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/CURRENTUSER', "/DIR=`"$installDirectory`"", "/GROUP=`"$group`"", '/TASKS=autoupdates', "/LOG=`"$installLog`"") -Wait -PassThru
if ($process.ExitCode -ne 0) { Get-Content $installLog; throw "Installer failed: $($process.ExitCode)" }
foreach ($file in @('PlanarGeometryStudio.exe', 'tools\engine\GeoGen.exe', 'tools\drawer\GeoGen.DrawingLauncher.exe', 'setup\drawing-tools.ps1', 'LICENSE.txt', 'TERMS.md', 'unins000.exe')) {
    if (-not (Test-Path (Join-Path $installDirectory $file))) { throw "Missing installed file: $file" }
}
$startMenu = Join-Path ([Environment]::GetFolderPath('ApplicationData')) "Microsoft\Windows\Start Menu\Programs\$group"
& (Join-Path $PSScriptRoot 'verify-windows-signatures.ps1') -Files @(
    $Installer, (Join-Path $installDirectory 'PlanarGeometryStudio.exe'),
    (Join-Path $installDirectory 'unins000.exe'))
if (-not (Test-Path (Join-Path $startMenu 'Planar Geometry Studio.lnk'))) { throw 'Start Menu shortcut is missing.' }
if (-not (Test-Path (Join-Path $startMenu 'Studio Setup.lnk'))) { throw 'Setup shortcut is missing.' }

# Exercise the packaged MiKTeX bootstrap on a real Windows runner. This script
# refreshes the current process PATH just as Studio refreshes each child PATH.
& (Join-Path $installDirectory 'setup\drawing-tools.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Automatic MiKTeX setup failed.' }
$renderDirectory = Join-Path $env:RUNNER_TEMP 'Studio Drawing Test'
New-Item -ItemType Directory -Force -Path $renderDirectory | Out-Null
Copy-Item (Join-Path $installDirectory 'tools\drawer\Data\*.mp') $renderDirectory
@'
input macros;
beginfig(1);
draw (0,0)--(100,0)--(50,80)--cycle;
label(btex $A$ etex,(0,0));
endfig;
end.
'@ | Set-Content (Join-Path $renderDirectory 'smoke.mp') -Encoding ascii
Push-Location $renderDirectory
try {
    & mpost.exe -interaction=nonstopmode -halt-on-error -s prologues=3 smoke.mp
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path 'smoke.1')) { throw 'MetaPost rendering failed.' }
    $converter = Get-Command epstopdf.exe -ErrorAction SilentlyContinue
    if (-not $converter) { $converter = Get-Command miktex-epstopdf.exe -ErrorAction Stop }
    & $converter.Source --outfile=smoke.pdf smoke.1
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path 'smoke.pdf')) { throw 'PDF conversion failed.' }
} finally { Pop-Location }

Push-Location (Join-Path $installDirectory 'tools\engine')
try {
    $env:GEOGEN_NO_PAUSE = '1'
    & .\GeoGen.exe
    if ($LASTEXITCODE -ne 0) { throw 'The installed engine failed.' }
    if (-not (Test-Path 'Examples\Output\JsonOutput\output.json')) { throw 'The installed engine did not produce output.' }
} finally { Pop-Location }

# Re-running setup exercises the stable application identity and upgrade path.
$process = Start-Process -FilePath $Installer -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/CURRENTUSER', "/DIR=`"$installDirectory`"", "/GROUP=`"$group`"", '/TASKS=autoupdates') -Wait -PassThru
if ($process.ExitCode -ne 0) { throw 'Reinstall/upgrade failed.' }
$process = Start-Process -FilePath (Join-Path $installDirectory 'unins000.exe') -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait -PassThru
if ($process.ExitCode -ne 0) { throw 'Uninstall failed.' }
if (Test-Path (Join-Path $installDirectory 'PlanarGeometryStudio.exe')) { throw 'Uninstall left the application executable.' }
if (Test-Path (Join-Path $startMenu 'Planar Geometry Studio.lnk')) { throw 'Uninstall left the Start Menu shortcut.' }
