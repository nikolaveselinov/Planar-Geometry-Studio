#!/usr/bin/env bash
set -euo pipefail
app="${1:?Pass desktop executable}"
output="${2:?Pass preview directory}"
mkdir -p "$output"
preferences="$(mktemp -d)"
mkdir -p "$preferences/PlanarGeometryStudio"
printf '%s\n' '{"SetupCompleted":true,"CheckForUpdatesOnLaunch":false}' > "$preferences/PlanarGeometryStudio/settings.json"
XDG_DATA_HOME="$preferences" "$app" --no-update-checks > "$output/constructions.log" 2>&1 &
pid=$!
trap 'kill "$pid" 2>/dev/null || true; rm -rf "$preferences"' EXIT
wait_window() {
    local pattern="$1" window=''
    for attempt in {1..160}; do
        window="$(xdotool search --onlyvisible --name "$pattern" 2>/dev/null | head -1 || true)"
        [[ -n "$window" ]] && { printf '%s\n' "$window"; return; }
        kill -0 "$pid" || { cat "$output/constructions.log" >&2; return 1; }
        sleep 0.25
    done
    cat "$output/constructions.log" >&2
    echo "Window did not open: $pattern" >&2
    return 1
}
main="$(wait_window 'Planar Geometry Studio$')"
sleep 1
import -window "$main" "$output/studio-workspace.png"
xdotool windowfocus --sync "$main"
xdotool key ctrl+k
browser="$(wait_window '^Constructions$')"
sleep 1
import -window "$browser" "$output/studio-constructions.png"
xdotool windowfocus --sync "$browser"
xdotool type --clearmodifiers 'circumcenter'
sleep 1
import -window "$browser" "$output/studio-constructions-search.png"
test -s "$output/studio-constructions-search.png"
