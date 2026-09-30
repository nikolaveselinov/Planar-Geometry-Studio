#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
rid="${1:?Pass linux-x64 or linux-arm64}"
version="${2:-$(tr -d '[:space:]' < "$root/VERSION")}"
[[ "$rid" == linux-x64 || "$rid" == linux-arm64 ]] || exit 2
app="$root/artifacts/staging/$rid/PlanarGeometryStudio"
[[ -x "$app/PlanarGeometryStudio" ]] || { echo 'Run publish.sh first.' >&2; exit 1; }
stem="PlanarGeometryStudio-v$version-$rid"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
cp "$root/packaging/assets/studio.svg" "$app/studio.svg"
# Include the icon in the portable archive too, before embedding it in .run.
tar --owner=0 --group=0 -C "$(dirname "$app")" -czf "$root/artifacts/$stem.tar.gz" PlanarGeometryStudio
hash="$(sha256sum "$root/artifacts/$stem.tar.gz" | cut -d ' ' -f 1)"
sed -e "s/@ARCH@/${rid#linux-}/g" -e "s/@PAYLOAD_SHA256@/$hash/g" \
    "$root/packaging/linux/installer-header.sh" > "$root/artifacts/$stem.run"
cat "$root/artifacts/$stem.tar.gz" >> "$root/artifacts/$stem.run"
chmod +x "$root/artifacts/$stem.run"

payload="$work/payload"
mkdir -p "$payload/opt/planargeometrystudio" "$payload/usr/bin" \
    "$payload/usr/share/applications" "$payload/usr/share/icons/hicolor/scalable/apps" \
    "$payload/usr/share/doc/planargeometrystudio"
cp -a "$app/." "$payload/opt/planargeometrystudio/"
printf '#!/bin/sh\nexec /opt/planargeometrystudio/PlanarGeometryStudio "$@"\n' > "$payload/usr/bin/planargeometrystudio"
chmod 755 "$payload/usr/bin/planargeometrystudio"
cp "$root/packaging/linux/planargeometrystudio.desktop" "$payload/usr/share/applications/"
cp "$root/packaging/assets/studio.svg" "$payload/usr/share/icons/hicolor/scalable/apps/planargeometrystudio.svg"
cp "$root/LICENSE" "$payload/usr/share/doc/planargeometrystudio/copyright"
cp "$root/TERMS.md" "$payload/usr/share/doc/planargeometrystudio/TERMS.md"

deb_arch=amd64
rpm_arch=x86_64
if [[ "$rid" == linux-arm64 ]]; then deb_arch=arm64; rpm_arch=aarch64; fi
mkdir -p "$payload/DEBIAN"
size="$(du -sk "$payload" | cut -f1)"
cat > "$payload/DEBIAN/control" <<CONTROL
Package: planargeometrystudio
Version: $version
Architecture: $deb_arch
Maintainer: Nikola Veselinov
Section: education
Priority: optional
Homepage: https://github.com/nikolaveselinov/Planar-Geometry-Studio
Installed-Size: $size
Depends: libc6 (>= 2.35), libgcc-s1, libstdc++6, zlib1g, libssl3 | libssl3t64, libicu76 | libicu74 | libicu72 | libicu78 | libicu77 | libicu80, libfontconfig1, libx11-6, libxext6, libxrender1, libice6, libsm6
Recommends: texlive-metapost, texlive-base, texlive-latex-base, texlive-fonts-recommended, ghostscript
Description: Planar geometry theorem generation, proof search, and drawing
 A desktop studio with its geometry engine and .NET runtime included.
 Drawing tools can be installed automatically from Studio Setup.
CONTROL
dpkg-deb --root-owner-group --build "$payload" "$root/artifacts/$stem.deb"
rm -rf "$payload/DEBIAN"

mkdir -p "$work/rpm/"{BUILD,RPMS,SOURCES,SPECS,SRPMS,BUILDROOT}
cat > "$work/rpm/SPECS/studio.spec" <<SPEC
Name: planargeometrystudio
Version: $version
Release: 1
Summary: Planar geometry theorem generation, proof search, and drawing
License: AGPL-3.0-only
URL: https://github.com/nikolaveselinov/Planar-Geometry-Studio
BuildArch: $rpm_arch
AutoReqProv: no
Requires: glibc >= 2.35, libgcc, libstdc++, zlib, openssl-libs, libicu, fontconfig, libX11, libXext, libXrender, libICE, libSM
Recommends: texlive-metapost, texlive-plain, texlive-cm, texlive-epstopdf, ghostscript
%description
A desktop studio with its geometry engine and .NET runtime included.
Drawing tools can be installed automatically from Studio Setup.
%install
mkdir -p %{buildroot}
cp -a "$payload/." %{buildroot}/
%files
/opt/planargeometrystudio
/usr/bin/planargeometrystudio
/usr/share/applications/planargeometrystudio.desktop
/usr/share/icons/hicolor/scalable/apps/planargeometrystudio.svg
/usr/share/doc/planargeometrystudio
SPEC
rpmbuild --define "_topdir $work/rpm" --define '_build_id_links none' \
    --define 'debug_package %{nil}' --define '__os_install_post %{nil}' \
    --target "$rpm_arch" -bb "$work/rpm/SPECS/studio.spec"
cp "$work/rpm/RPMS/$rpm_arch/planargeometrystudio-$version-1.$rpm_arch.rpm" "$root/artifacts/$stem.rpm"
echo "$root/artifacts/$stem.run"
echo "$root/artifacts/$stem.deb"
echo "$root/artifacts/$stem.rpm"
