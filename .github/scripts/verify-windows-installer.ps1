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
if (-not (Test-Path (Join-Path $startMenu 'Planar Geometry Studio.lnk'))) { throw 'Start Menu shortcut is missing.' }
if (-not (Test-Path (Join-Path $startMenu 'Studio Setup.lnk'))) { throw 'Setup shortcut is missing.' }

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
