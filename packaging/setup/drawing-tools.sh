#!/usr/bin/env bash
set -euo pipefail
export PATH="/Library/TeX/texbin:/opt/homebrew/bin:/usr/local/bin:/usr/bin:/bin:${PATH:-}"

if [[ "$(uname -s)" == Darwin ]]; then
    work_dir="$(mktemp -d)"
    trap 'rm -rf "$work_dir"' EXIT
    # Use the official Homebrew cask metadata for a versioned BasicTeX URL and
    # SHA-256; Homebrew itself is not required and is not installed by this script.
    if ! command -v mpost >/dev/null || ! command -v tex >/dev/null; then
        if ! command -v tlmgr >/dev/null; then
            echo 'Downloading BasicTeX (a compact TeX Live distribution)...'
            curl --fail --location --proto '=https' --proto-redir '=https' --connect-timeout 20 \
                https://formulae.brew.sh/api/cask/basictex.json -o "$work_dir/basictex.json"
            url="$(plutil -extract url raw -o - "$work_dir/basictex.json")"
            expected="$(plutil -extract sha256 raw -o - "$work_dir/basictex.json")"
            [[ "$url" == https://* && "$expected" =~ ^[a-fA-F0-9]{64}$ ]] || { echo 'Invalid BasicTeX metadata.' >&2; exit 1; }
            curl --fail --location --proto '=https' --proto-redir '=https' --connect-timeout 20 "$url" -o "$work_dir/BasicTeX.pkg"
            actual="$(shasum -a 256 "$work_dir/BasicTeX.pkg" | cut -d ' ' -f 1)"
            [[ "$actual" == "$expected" ]] || { echo 'BasicTeX checksum mismatch; nothing was installed.' >&2; exit 1; }
            # A native system permission prompt avoids asking for a password in Studio.
            osascript - "$work_dir/BasicTeX.pkg" <<'APPLESCRIPT'
on run argv
    do shell script "/usr/sbin/installer -pkg " & quoted form of (item 1 of argv) & " -target /" with administrator privileges
end run
APPLESCRIPT
        fi
    fi
    echo 'Preparing MetaPost, plain TeX, and PDF export...'
    # Add packages to the existing distribution rather than installing a second TeX.
    tlmgr_path="$(command -v tlmgr)"
    osascript - "$tlmgr_path" <<'APPLESCRIPT'
on run argv
    set manager to quoted form of (item 1 of argv)
    do shell script manager & " update --self && " & manager & " install metapost cm plain epstopdf" with administrator privileges
end run
APPLESCRIPT
    # Reuse Homebrew when available; otherwise use MacTeX's small,
    # signed universal Ghostscript package rather than requiring Homebrew.
    if ! command -v gs >/dev/null; then
        if command -v brew >/dev/null; then
            brew install ghostscript
        else
            echo 'Downloading the MacTeX Ghostscript package...'
            gs_url='https://tug.ctan.org/systems/mac/mactex/mactex-ghostscript-10.07.0-20260324.pkg'
            curl --fail --location --proto '=https' --proto-redir '=https' --connect-timeout 20 "$gs_url.sha512" -o "$work_dir/ghostscript.sha512"
            expected_gs="$(awk 'NR==1 {print $1}' "$work_dir/ghostscript.sha512")"
            [[ "$expected_gs" =~ ^[a-fA-F0-9]{128}$ ]] || { echo 'Invalid Ghostscript checksum metadata.' >&2; exit 1; }
            curl --fail --location --proto '=https' --proto-redir '=https' --connect-timeout 20 "$gs_url" -o "$work_dir/Ghostscript.pkg"
            actual_gs="$(shasum -a 512 "$work_dir/Ghostscript.pkg" | cut -d ' ' -f 1)"
            [[ "$actual_gs" == "$expected_gs" ]] || { echo 'Ghostscript checksum mismatch; nothing was installed.' >&2; exit 1; }
            pkgutil --check-signature "$work_dir/Ghostscript.pkg"
            osascript - "$work_dir/Ghostscript.pkg" <<'APPLESCRIPT'
on run argv
    do shell script "/usr/sbin/installer -pkg " & quoted form of (item 1 of argv) & " -target /" with administrator privileges
end run
APPLESCRIPT
        fi
    fi
else
    echo 'Installing TeX Live, MetaPost, and Ghostscript from your distribution...'
    # Only these fixed package-manager commands are elevated. Never execute a
    # downloaded script as root. pkexec supplies the desktop's permission dialog.
    if command -v apt-get >/dev/null; then
        command_text='apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y texlive-metapost texlive-base texlive-latex-base texlive-fonts-recommended ghostscript'
    elif command -v dnf >/dev/null; then
        command_text='dnf install -y texlive-metapost texlive-plain texlive-cm texlive-epstopdf ghostscript'
    elif command -v pacman >/dev/null; then
        command_text='pacman -S --needed --noconfirm texlive-metapost texlive-basic texlive-fontsrecommended ghostscript'
    elif command -v zypper >/dev/null; then
        command_text='zypper --non-interactive install texlive-metapost texlive-plain texlive-cm texlive-epstopdf ghostscript'
    else
        echo 'Install MetaPost, plain TeX, and Ghostscript with your distribution package manager, then select Check again.' >&2
        exit 1
    fi
    if [[ "$EUID" == 0 ]]; then
        /bin/sh -c "$command_text"
    elif command -v pkexec >/dev/null; then
        pkexec /bin/sh -c "$command_text"
    elif sudo -n true 2>/dev/null; then
        sudo /bin/sh -c "$command_text"
    else
        echo "No graphical permission helper is available. Run in a terminal: sudo sh -c '$command_text'" >&2
        exit 1
    fi
fi
command -v mpost
command -v tex
command -v gs
echo 'Drawing tools are ready. No restart is needed.'
