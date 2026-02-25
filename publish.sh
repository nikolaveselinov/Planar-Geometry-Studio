#!/usr/bin/env bash
set -euo pipefail

# Planar Geometry Studio — publish script (Linux/macOS)
# Publishes the desktop app + engine + drawer as self-contained win-x64 binaries,
# then packages everything into a zip.

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC="$SCRIPT_DIR/Source"
OUT="$SCRIPT_DIR/publish/PlanarGeometryStudio"
RID="win-x64"

echo "=== Cleaning previous publish ==="
rm -rf "$SCRIPT_DIR/publish"

echo "=== Publishing GeoGen.DesktopApp ==="
dotnet publish "$SRC/Launchers/GeoGen.DesktopApp/GeoGen.DesktopApp.csproj" \
    -c Release -r "$RID" --self-contained -p:PublishSingleFile=true \
    -o "$OUT"

echo "=== Publishing GeoGen.MainLauncher (engine) ==="
dotnet publish "$SRC/Launchers/GeoGen.MainLauncher/GeoGen.MainLauncher.csproj" \
    -c Release -r "$RID" --self-contained -p:PublishSingleFile=true \
    -o "$OUT/tools/engine"

echo "=== Publishing GeoGen.DrawingLauncher (drawer) ==="
dotnet publish "$SRC/Launchers/GeoGen.DrawingLauncher/GeoGen.DrawingLauncher.csproj" \
    -c Release -r "$RID" --self-contained -p:PublishSingleFile=true \
    -o "$OUT/tools/drawer"

echo "=== Copying MetaPost data files ==="
cp "$SRC/Launchers/GeoGen.DrawingLauncher/Data/"*.mp "$OUT/tools/drawer/Data/" 2>/dev/null || true
cp "$SRC/Launchers/GeoGen.DrawingLauncher/Data/"*.txt "$OUT/tools/drawer/Data/" 2>/dev/null || true

echo "=== Cleaning leaked Output/Logs dirs from engine ==="
rm -rf "$OUT/tools/engine/Examples/Output"
rm -rf "$OUT/tools/engine/Logs"
rm -rf "$OUT/tools/drawer/Logs"

echo "=== Creating zip ==="
cd "$SCRIPT_DIR/publish"
zip -r "PlanarGeometryStudio-${RID}.zip" PlanarGeometryStudio/
echo "=== Done: publish/PlanarGeometryStudio-${RID}.zip ==="
