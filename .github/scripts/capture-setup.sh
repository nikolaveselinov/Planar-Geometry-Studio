#!/usr/bin/env bash
set -euo pipefail
app="${1:?Pass desktop executable}"
output="${2:?Pass image destination}"
"$app" --setup --no-update-checks > "$output.log" 2>&1 &
pid=$!
trap 'kill "$pid" 2>/dev/null || true' EXIT
window=''
for attempt in {1..40}; do
    window="$(xdotool search --onlyvisible --name '^Studio Setup$' 2>/dev/null | head -1 || true)"
    [[ -n "$window" ]] && break
    kill -0 "$pid" || { cat "$output.log"; exit 1; }
    sleep 0.25
done
[[ -n "$window" ]] || { cat "$output.log"; echo 'Setup window did not open.' >&2; exit 1; }
sleep 1
import -window "$window" "$output"
test -s "$output"
