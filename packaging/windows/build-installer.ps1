param(
    [ValidateSet('win-x64', 'win-arm64')][string]$Runtime = 'win-x64',
    [string]$Version,
    [switch]$SelfSign
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
$compilerArguments = @("/DAppVersion=$Version", "/DRuntime=$Runtime", "/DSourceDir=$sourceDir", "/DOutputDir=$(Join-Path $repoRoot 'artifacts')")
$certificate = $null
try {
    if ($SelfSign) {
        $signTool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if (-not $signTool) { throw 'Install the Windows SDK signing tools to create publisher signatures.' }
        # A build-local, non-exportable key labels this build; it is not a verified
        # publisher identity and is never uploaded, committed, or trusted by setup.
        $certificate = New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=Nikola Veselinov' `
            -FriendlyName 'Planar Geometry Studio self-signed publisher' -CertStoreLocation 'Cert:\CurrentUser\My' `
            -KeyAlgorithm RSA -KeyLength 3072 -HashAlgorithm SHA256 -KeyExportPolicy NonExportable `
            -NotAfter (Get-Date).AddYears(5)
        Export-Certificate -Cert $certificate -FilePath (Join-Path $sourceDir 'setup\publisher.cer') | Out-Null
        @'
Signer: Nikola Veselinov
This build uses a self-signed Authenticode certificate. The name appears in
Properties > Digital Signatures; it has not been verified by a certificate authority.
Windows may still display Unknown publisher or security warnings.
The public certificate is publisher.cer. Setup does not install it into trust stores.
Each build generates a separate certificate and destroys its private key afterwards.
Do not treat this signature as proof of a verified publisher identity.
'@ | Set-Content (Join-Path $sourceDir 'setup\publisher-signature.txt') -Encoding utf8
        foreach ($file in @('PlanarGeometryStudio.exe', 'tools\engine\GeoGen.exe', 'tools\drawer\GeoGen.DrawingLauncher.exe')) {
            & $signTool.FullName sign /fd SHA256 /sha1 $certificate.Thumbprint /s My /d 'Planar Geometry Studio' (Join-Path $sourceDir $file)
            if ($LASTEXITCODE -ne 0) { throw "Signing failed for $file." }
        }
        # Inno signs both setup and its generated uninstaller with the same key.
        $signCommand = '$q' + $signTool.FullName + '$q sign /fd SHA256 /sha1 ' + $certificate.Thumbprint + ' /s My /d $qPlanar Geometry Studio$q $f'
        $compilerArguments += '/DSelfSign=1', ('/Sstudio=' + $signCommand)
    }
    & $compilerPath @compilerArguments (Join-Path $PSScriptRoot 'studio.iss')
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup exited with code $LASTEXITCODE." }
    if ($SelfSign) {
        # publish.ps1 created the portable archive before signing; replace it with
        # the final signed executables and the public certificate, never the key.
        $archive = Join-Path $repoRoot "artifacts\PlanarGeometryStudio-v$Version-$Runtime.zip"
        if (Test-Path $archive) { Remove-Item -Force $archive }
        Compress-Archive -Path $sourceDir -DestinationPath $archive
    }
} finally {
    if ($certificate) { Remove-Item ("Cert:\CurrentUser\My\" + $certificate.Thumbprint) -DeleteKey -Force }
}
