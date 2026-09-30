#!/usr/bin/env bash
set -euo pipefail
prefix="${HOME:?}/.local/opt/planargeometrystudio"
launch=true
tools=true
shortcuts=true
assume_yes=false
for argument in "$@"; do
    case "$argument" in
        --prefix=*) prefix="${argument#--prefix=}" ;;
        --no-launch) launch=false ;;
        --no-tools) tools=false ;;
        --no-shortcuts) shortcuts=false ;;
        --yes) assume_yes=true ;;
        --help) echo 'Usage: bash Studio.run [--prefix=/absolute/path] [--no-launch] [--no-tools] [--no-shortcuts] [--yes]'; exit 0 ;;
        *) echo "Unknown argument: $argument" >&2; exit 2 ;;
    esac
done
[[ "$prefix" == /* && "$prefix" != / && "$prefix" != "$HOME" && "$prefix" != *$'\n'* && "$prefix" != *'"'* && "$prefix" != *'\'* ]] || { echo 'Choose a dedicated absolute installation directory.' >&2; exit 2; }
prefix="${prefix%/}"
if [[ -e "$prefix" && ! -f "$prefix/.pgs-managed-install" ]]; then
    echo "Refusing to replace a directory not managed by Studio: $prefix" >&2
    exit 1
fi
case "$(uname -m)" in
    x86_64) machine=x64 ;;
    aarch64|arm64) machine=arm64 ;;
    *) echo 'This CPU architecture is not supported.' >&2; exit 1 ;;
esac
[[ "$machine" == '@ARCH@' ]] || { echo 'Download the installer matching your CPU architecture.' >&2; exit 1; }

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
line="$(awk '/^__PGS_ARCHIVE__$/ {print NR + 1; exit}' "$0")"
tail -n +"$line" "$0" > "$work/payload.tar.gz"
actual="$(sha256sum "$work/payload.tar.gz" | cut -d ' ' -f 1)"
[[ "$actual" == '@PAYLOAD_SHA256@' ]] || { echo 'Installer payload is damaged. Download it again.' >&2; exit 1; }
tar --no-same-owner -xzf "$work/payload.tar.gz" -C "$work"

if ! $assume_yes; then
    if command -v zenity >/dev/null && [[ -n "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]]; then
        zenity --text-info --title='Studio Setup · Terms and privacy' --width=720 --height=600 \
            --filename="$work/PlanarGeometryStudio/TERMS.md" --checkbox='I have reviewed the license and setup notes.'
        zenity --question --title='From conjecture to figure.' --width=500 \
            --text="Install Planar Geometry Studio in $prefix?\n\nThe engine and runtime are included. Your existing runs are preserved."
        if ! zenity --question --title='Drawing tools' --text='Set up missing TeX Live, MetaPost, and PDF drawing tools when Studio opens? This uses your package manager and may require a system permission prompt.'; then tools=false; fi
    else
        [[ -t 0 ]] || { echo 'Open this installer in a terminal, or pass --yes after reviewing TERMS.md.' >&2; exit 1; }
        cat "$work/PlanarGeometryStudio/TERMS.md"
        read -r -p "Install Studio in $prefix? [y/N] " response
        [[ "$response" == [Yy] || "$response" == [Yy][Ee][Ss] ]] || exit 0
        read -r -p 'Set up missing drawing tools when Studio opens? [Y/n] ' response
        [[ "$response" == [Nn] || "$response" == [Nn][Oo] ]] && tools=false
    fi
fi
mkdir -p "$(dirname "$prefix")"
new_dir="$(mktemp -d "$(dirname "$prefix")/.pgs-new-XXXXXXXX")"
cp -a "$work/PlanarGeometryStudio/." "$new_dir/"
touch "$new_dir/.pgs-managed-install"
chmod +x "$new_dir/PlanarGeometryStudio" "$new_dir/tools/engine/GeoGen" "$new_dir/tools/drawer/GeoGen.DrawingLauncher"

data_home="${XDG_DATA_HOME:-$HOME/.local/share}"
mkdir -p "$data_home/applications" "$data_home/icons/hicolor/scalable/apps"
desktop_path="$data_home/applications/planargeometrystudio-user.desktop"
icon_path="$data_home/icons/hicolor/scalable/apps/planargeometrystudio-user.svg"
# Build the uninstaller with shell-quoted paths, including paths with spaces.
{
    echo '#!/usr/bin/env bash'
    echo 'set -euo pipefail'
    printf 'prefix=%q\ndesktop_path=%q\nicon_path=%q\n' "$prefix" "$desktop_path" "$icon_path"
    echo '[[ -f "$prefix/.pgs-managed-install" ]] || { echo "This is not a managed Studio installation." >&2; exit 1; }'
    echo 'rm -f "$desktop_path" "$icon_path"'
    echo 'rm -rf -- "$prefix"'
    echo 'echo "Studio removed. Your runs and shared drawing tools have been preserved."'
} > "$new_dir/uninstall.sh"
chmod +x "$new_dir/uninstall.sh"
if [[ -d "$prefix" ]]; then
    previous="$(mktemp -d "$(dirname "$prefix")/.pgs-previous-XXXXXXXX")"
    rmdir "$previous"
    mv "$prefix" "$previous"
    if ! mv "$new_dir" "$prefix"; then mv "$previous" "$prefix"; exit 1; fi
    rm -rf -- "$previous"
else
    mv "$new_dir" "$prefix"
fi
if $shortcuts; then
    cp "$prefix/studio.svg" "$icon_path"
    cat > "$desktop_path" <<DESKTOP
[Desktop Entry]
Type=Application
Name=Planar Geometry Studio
Comment=Generate, prove, and draw planar geometry theorems
Exec="$prefix/PlanarGeometryStudio"
Icon=planargeometrystudio-user
Terminal=false
Categories=Education;Science;Math;
Actions=Setup;
[Desktop Action Setup]
Name=Studio Setup
Exec="$prefix/PlanarGeometryStudio" --setup
DESKTOP
    if command -v update-desktop-database >/dev/null; then update-desktop-database "$data_home/applications" || true; fi
fi
echo "Installed in $prefix"
echo "Uninstall with: bash \"$prefix/uninstall.sh\""
if $launch; then
    args=(--setup)
    if $tools; then args+=(--install-drawing-tools); fi
    "$prefix/PlanarGeometryStudio" "${args[@]}" >/dev/null 2>&1 &
fi
exit 0
__PGS_ARCHIVE__
