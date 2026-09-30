# Runs as the desktop user, never as the elevated Studio installer account.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Refresh-ToolPath {
    $paths = @(
        [Environment]::GetEnvironmentVariable('Path', 'User'),
        [Environment]::GetEnvironmentVariable('Path', 'Machine'),
        "$env:LOCALAPPDATA\Programs\MiKTeX\miktex\bin\x64",
        "$env:ProgramFiles\MiKTeX\miktex\bin\x64",
        $env:Path
    )
    $env:Path = ($paths | Where-Object { $_ }) -join ';'
    $gsRoot = Join-Path $env:ProgramFiles 'gs'
    if (Test-Path $gsRoot) {
        Get-ChildItem $gsRoot -Directory | Sort-Object Name -Descending | ForEach-Object {
            $env:Path += ';' + (Join-Path $_.FullName 'bin')
        }
    }
}

function Invoke-Checked([string]$Executable, [string[]]$Arguments) {
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Executable exited with code $LASTEXITCODE." }
}

try {
    Refresh-ToolPath
    $mpost = Get-Command mpost.exe -ErrorAction SilentlyContinue
    $tex = Get-Command tex.exe -ErrorAction SilentlyContinue
    if (-not $mpost -or -not $tex) {
        $miktex = Get-Command miktex.exe -ErrorAction SilentlyContinue
        if (-not $miktex) {
            Write-Output 'Installing MiKTeX for your user account. Existing shared installations are preserved.'
            $winget = Get-Command winget.exe -ErrorAction SilentlyContinue
            if ($winget) {
                & $winget.Source install --id MiKTeX.MiKTeX --exact --source winget --scope user --architecture x64 --silent --accept-source-agreements --accept-package-agreements --disable-interactivity
                # MiKTeX currently supplies x64 tools; Windows 11 ARM runs these via emulation.
                Refresh-ToolPath
                $miktex = Get-Command miktex.exe -ErrorAction SilentlyContinue
            }
            if (-not $miktex) {
                # Fallback for computers without App Installer/WinGet. The exact installer
                # is pinned to the SHA-256 published in Microsoft's winget-pkgs manifest.
                $url = 'https://miktex.org/download/ctan/systems/win32/miktex/setup/windows-x64/basic-miktex-25.12-x64.exe'
                $expected = '14B42DD9F4B4A7813A8BFD69C8F99316C2888CC4EE26F631F397E163D85D6C62'
                $temporary = Join-Path ([IO.Path]::GetTempPath()) ('pgs-miktex-' + [Guid]::NewGuid().ToString('N') + '.exe')
                try {
                    Write-Output 'Downloading the verified MiKTeX basic installer...'
                    Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $temporary
                    if ((Get-FileHash $temporary -Algorithm SHA256).Hash -ne $expected) {
                        throw 'MiKTeX checksum mismatch. No downloaded program was executed.'
                    }
                    $process = Start-Process -FilePath $temporary -ArgumentList '--unattended', '--private' -Wait -PassThru
                    if ($process.ExitCode -ne 0) { throw "MiKTeX installer exited with code $($process.ExitCode)." }
                } finally {
                    if (Test-Path $temporary) { Remove-Item -Force $temporary }
                }
                Refresh-ToolPath
            }
        }
    }

    $miktex = Get-Command miktex.exe -ErrorAction SilentlyContinue
    if ($miktex) {
        Write-Output 'Preparing MetaPost, plain TeX, and PDF export...'
        Invoke-Checked $miktex.Source @('packages', 'update-package-database')
        foreach ($package in @('metapost', 'miktex-epstopdf-bin-x64-2.9')) {
            # MiKTeX's install command reports an error for already-installed
            # packages. Check metadata first so repeated setup is safe.
            $installed = (& $miktex.Source packages info '--template={isInstalled}' $package | Select-Object -Last 1)
            if ($LASTEXITCODE -ne 0) { throw "Could not inspect MiKTeX package $package." }
            if ("$installed".Trim() -notmatch '^(true|yes|1)$') {
                Invoke-Checked $miktex.Source @('packages', 'install', $package)
            } else {
                Write-Output "$package is already installed; reusing it."
            }
        }
        $initexmf = Get-Command initexmf.exe -ErrorAction Stop
        # User explicitly opted into dependency downloads, including missing TeX packages.
        Invoke-Checked $initexmf.Source @('--set-config-value=[MPM]AutoInstall=1')
        Invoke-Checked $initexmf.Source @('--update-fndb')
        Invoke-Checked $initexmf.Source @('--mklinks', '--force')
        Invoke-Checked $initexmf.Source @('--dump=tex')
    }

    Refresh-ToolPath
    if (-not (Get-Command mpost.exe -ErrorAction SilentlyContinue) -or -not (Get-Command tex.exe -ErrorAction SilentlyContinue)) {
        throw 'MetaPost or TeX could not be found. Install MiKTeX from https://miktex.org/download, then select Check again.'
    }
    if (-not (Get-Command epstopdf.exe -ErrorAction SilentlyContinue) -and
        -not (Get-Command miktex-epstopdf.exe -ErrorAction SilentlyContinue) -and
        -not (Get-Command gswin64c.exe -ErrorAction SilentlyContinue)) {
        throw 'PDF export is still missing. Install epstopdf through your TeX distribution, or Ghostscript from https://ghostscript.com/releases/gsdnld.html.'
    }
    Write-Output 'Drawing tools are ready. No restart is needed.'
    exit 0
} catch {
    Write-Output ('Setup could not finish: ' + $_.Exception.Message)
    Write-Output 'You can continue using generation and proofs, then retry from Help > Studio Setup.'
    exit 1
}
