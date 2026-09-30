#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
rid="${1:?Pass osx-x64 or osx-arm64}"
version="${2:-$(tr -d '[:space:]' < "$root/VERSION")}"
[[ "$rid" == osx-x64 || "$rid" == osx-arm64 ]] || exit 2
bundle="$root/artifacts/staging/$rid/Planar Geometry Studio.app"
[[ -d "$bundle" ]] || { echo 'Run publish.sh first.' >&2; exit 1; }
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
stem="PlanarGeometryStudio-v$version-$rid"
mkdir -p "$work/root/Applications" "$work/resources" "$work/disk"
cp -R "$bundle" "$work/root/Applications/"
python3 - "$root" "$work/resources" <<'PY'
import html, pathlib, sys
root, output = map(pathlib.Path, sys.argv[1:])
style = '<style>body{font:14px -apple-system,Helvetica;color:#17213a;padding:24px}h1{font-size:28px;color:#6558d8}p{line-height:1.6}pre{white-space:pre-wrap;font:12px -apple-system,Helvetica}</style>'
pages = {
    'welcome.html': '<h1>From conjecture to figure.</h1><p>Welcome to Planar Geometry Studio.</p><p>This installer places Studio and its bundled geometry engine and .NET runtime in Applications. Launch Studio once installation finishes to set up optional drawing tools.</p><p>Your configurations and runs remain in Documents/Planar Geometry Studio, separately from the application.</p>',
    'conclusion.html': '<h1>Your studio is ready.</h1><p>Open <b>Applications → Planar Geometry Studio</b>. Studio Setup checks your drawing tools and offers automatic installation of BasicTeX, MetaPost, and Ghostscript.</p><p>Updates are checked in the background on each launch. Manage this preference in Help → Studio Setup.</p><p>To uninstall, move Planar Geometry Studio.app to the Trash. Your runs and shared TeX installation are preserved.</p>',
    'terms.html': '<h1>Terms and setup notes</h1><pre>' + html.escape((root/'TERMS.md').read_text()) + '</pre>',
    'license.html': '<h1>GNU AGPL version 3</h1><pre>' + html.escape((root/'LICENSE').read_text()) + '</pre>'
}
for name, body in pages.items():
    (output/name).write_text('<!doctype html><html><meta charset="utf-8">'+style+'<body>'+body+'</body></html>')
PY

pkgbuild --root "$work/root" --identifier com.nikolaveselinov.planargeometrystudio \
    --version "$version" --install-location / --ownership recommended "$work/Studio.pkg"
cat > "$work/Distribution.xml" <<XML
<?xml version="1.0" encoding="UTF-8"?>
<installer-gui-script minSpecVersion="2">
  <title>Planar Geometry Studio</title>
  <welcome file="welcome.html"/>
  <readme file="terms.html"/>
  <license file="license.html"/>
  <conclusion file="conclusion.html"/>
  <options customize="never" require-scripts="false" hostArchitectures="${rid/osx-x64/x86_64}"/>
  <domains enable_localSystem="true" enable_currentUserHome="false" enable_anywhere="false"/>
  <volume-check><allowed-os-versions><os-version min="12.0"/></allowed-os-versions></volume-check>
  <choices-outline><line choice="studio"/></choices-outline>
  <choice id="studio" visible="false"><pkg-ref id="com.nikolaveselinov.planargeometrystudio"/></choice>
  <pkg-ref id="com.nikolaveselinov.planargeometrystudio" version="$version">Studio.pkg</pkg-ref>
</installer-gui-script>
XML
# Apple's architecture identifier for native ARM packages is arm64.
sed -i '' 's/hostArchitectures="osx-arm64"/hostArchitectures="arm64"/' "$work/Distribution.xml"
productbuild --distribution "$work/Distribution.xml" --resources "$work/resources" \
    --package-path "$work" "$root/artifacts/$stem.pkg"
cp "$root/artifacts/$stem.pkg" "$work/disk/Install Planar Geometry Studio.pkg"
cp "$root/TERMS.md" "$work/disk/Terms and setup notes.txt"
hdiutil create -volname 'Planar Geometry Studio Setup' -srcfolder "$work/disk" \
    -format UDZO -ov "$root/artifacts/$stem.dmg"
echo "$root/artifacts/$stem.pkg"
echo "$root/artifacts/$stem.dmg"
