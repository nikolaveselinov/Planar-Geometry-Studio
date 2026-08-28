#!/usr/bin/env bash
set -euo pipefail

fail() {
    echo "$1" >&2
    exit 1
}

ARCHIVE="${1:-}"
if [[ -z "$ARCHIVE" || ! -s "$ARCHIVE" ]]; then
    echo "Pass a non-empty Linux package." >&2
    exit 2
fi

AUDIT_DIR="$(mktemp -d)"
trap 'rm -rf "$AUDIT_DIR"' EXIT

tar --no-same-owner -xzf "$ARCHIVE" -C "$AUDIT_DIR"
APP_DIR="$AUDIT_DIR/PlanarGeometryStudio"
ENGINE_DIR="$APP_DIR/tools/engine"

[[ -x "$APP_DIR/PlanarGeometryStudio" ]] || fail "The desktop executable is missing."
[[ -x "$ENGINE_DIR/GeoGen" ]] || fail "The engine executable is missing."
[[ -x "$APP_DIR/tools/drawer/GeoGen.DrawingLauncher" ]] || fail "The drawing executable is missing."
[[ -f "$APP_DIR/tools/drawer/Data/drawing_rules.txt" ]] || fail "The drawing rules are missing."
[[ -f "$APP_DIR/LICENSE.txt" ]] || fail "The license is missing."

(
    cd "$ENGINE_DIR"
    GEOGEN_NO_PAUSE=1 timeout 60s ./GeoGen > "$AUDIT_DIR/engine-success.log" 2>&1
)

[[ -s "$ENGINE_DIR/Examples/Output/JsonOutput/output.json" ]] || fail "The installed engine produced no JSON output."
if grep -Fq "[ERR]" "$AUDIT_DIR/engine-success.log"; then
    cat "$AUDIT_DIR/engine-success.log" >&2
    exit 1
fi

rm -rf "$ENGINE_DIR/Examples/Output"
if (cd "$ENGINE_DIR" && GEOGEN_NO_PAUSE=1 ./GeoGen > "$AUDIT_DIR/engine-failure.log" 2>&1); then
    cat "$AUDIT_DIR/engine-failure.log" >&2
    echo "The engine returned success after every input failed." >&2
    exit 1
fi

grep -Fq "Generation failed for 3 input file(s)." "$AUDIT_DIR/engine-failure.log" ||
    fail "The installed engine did not report the failed inputs."
