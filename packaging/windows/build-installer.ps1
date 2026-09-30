param(
    [ValidateSet('win-x64', 'win-arm64')][string]$Runtime = 'win-x64',
    [string]$Version
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $Version) { $Version = (Get-Content (Join-Path $repoRoot 'VERSION') -Raw).Trim() }
$sourceDir = Join-Path $repoRoot "artifacts\staging\$Runtime\PlanarGeometryStudio"
if (-not (Test-Path (Join-Path $sourceDir 'PlanarGeometryStudio.exe'))) { throw 'Run publish.ps1 for this runtime first.' }
$compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($compiler) { $compilerPath = $compiler.Source }
else { $compilerPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" }
if (-not (Test-Path $compilerPath)) { throw 'Install Inno Setup 6.3 or later from https://jrsoftware.org/isinfo.php.' }
& $compilerPath "/DAppVersion=$Version" "/DRuntime=$Runtime" "/DSourceDir=$sourceDir" "/DOutputDir=$(Join-Path $repoRoot 'artifacts')" (Join-Path $PSScriptRoot 'studio.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup exited with code $LASTEXITCODE." }
