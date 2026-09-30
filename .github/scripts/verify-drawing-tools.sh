#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
rid="${1:?Pass runtime}"
app="$root/artifacts/staging/$rid/PlanarGeometryStudio"
PGS_SETUP_NONINTERACTIVE=1 bash "$app/setup/drawing-tools.sh"
export PATH="/Library/TeX/texbin:/opt/homebrew/bin:/usr/local/bin:/usr/bin:/bin:${PATH:-}"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
cp "$app/tools/drawer/Data/"*.mp "$work/"
cat > "$work/smoke.mp" <<'METAPOST'
input macros;
beginfig(1);
draw (0,0)--(100,0)--(50,80)--cycle;
label(btex $A$ etex,(0,0));
endfig;
end.
METAPOST
cd "$work"
mpost -interaction=nonstopmode -halt-on-error -s prologues=3 smoke.mp
test -s smoke.1
gs -sDEVICE=pdfwrite -dNOPAUSE -dBATCH -dEPSCrop -sOutputFile=smoke.pdf smoke.1
test -s smoke.pdf
