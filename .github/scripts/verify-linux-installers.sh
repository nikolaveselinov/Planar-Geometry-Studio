#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
rid="${1:?Pass runtime}"
version="$(tr -d '[:space:]' < "$root/VERSION")"
stem="$root/artifacts/PlanarGeometryStudio-v$version-$rid"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
destination="$work/Studio With Spaces"
"$stem.run" --yes --no-launch --no-tools --no-shortcuts --prefix="$destination"
[[ -x "$destination/PlanarGeometryStudio" && -f "$destination/setup/drawing-tools.sh" && -f "$destination/TERMS.md" ]]
sentinel="$work/results-that-must-survive.txt"
echo 'user work' > "$sentinel"
# Reinstall exercises atomic replacement without leaving old binaries around.
"$stem.run" --yes --no-launch --no-tools --no-shortcuts --prefix="$destination"
bash "$destination/uninstall.sh"
[[ ! -e "$destination" && -f "$sentinel" ]]
mkdir -p "$destination"
if "$stem.run" --yes --no-launch --no-tools --no-shortcuts --prefix="$destination"; then
    echo 'Installer replaced an unmanaged directory.' >&2; exit 1
fi
rmdir "$destination"
head -c -100 "$stem.run" > "$work/damaged.run"
if bash "$work/damaged.run" --yes --no-launch --no-tools --no-shortcuts --prefix="$destination"; then
    echo 'Installer accepted a damaged payload.' >&2; exit 1
fi
[[ ! -e "$destination" ]]
dpkg-deb --info "$stem.deb"
dpkg-deb --extract "$stem.deb" "$work/deb"
[[ -x "$work/deb/opt/planargeometrystudio/PlanarGeometryStudio" ]]
[[ -f "$work/deb/usr/share/applications/planargeometrystudio.desktop" ]]
rpm -qp --info "$stem.rpm"
rpm -qpl "$stem.rpm" | rg '^/opt/planargeometrystudio/PlanarGeometryStudio$'
