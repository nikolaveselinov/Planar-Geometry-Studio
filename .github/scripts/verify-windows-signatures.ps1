param([Parameter(Mandatory)][string[]]$Files)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'This verification runs only on disposable GitHub Actions runners.' }
$first = Get-AuthenticodeSignature $Files[0]
if (-not $first.SignerCertificate -or $first.SignerCertificate.Subject -ne 'CN=Nikola Veselinov') {
    throw 'Expected Nikola Veselinov publisher signature.'
}
$certificate = $first.SignerCertificate
$rootPath = 'Cert:\LocalMachine\Root\' + $certificate.Thumbprint
if (Test-Path $rootPath) { throw 'The test certificate should not already be trusted.' }
$store = [Security.Cryptography.X509Certificates.X509Store]::new('Root', 'LocalMachine')
$tampered = Join-Path $env:RUNNER_TEMP ('publisher-tamper-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    # Trust only inside this disposable test runner to distinguish an intact
    # self-signature from damaged bytes. The machine store avoids the current-user
    # protected-root confirmation dialog on unattended, elevated CI runners.
    # No installer performs this operation.
    $store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $store.Add($certificate)
    foreach ($file in $Files) {
        $signature = Get-AuthenticodeSignature $file
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne $certificate.Thumbprint) {
            throw "Publisher signature verification failed for $file`: $($signature.Status)."
        }
        Write-Output "Verified Nikola Veselinov signature: $file"
    }
    Copy-Item $Files[0] $tampered
    $stream = [IO.File]::Open($tampered, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite)
    try {
        # The DOS stub is covered by Authenticode; changing it leaves a readable
        # PE file whose signature must fail integrity verification.
        $stream.Position = 64
        $value = $stream.ReadByte()
        $stream.Position = 64
        $stream.WriteByte($value -bxor 1)
    } finally { $stream.Dispose() }
    if ((Get-AuthenticodeSignature $tampered).Status -ne 'HashMismatch') {
        throw 'A modified executable incorrectly passed the signature integrity check.'
    }
} finally {
    if (Test-Path $tampered) { Remove-Item -Force $tampered }
    $store.Remove($certificate)
    $store.Close()
    $store.Dispose()
}
