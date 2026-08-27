#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RID="${1:-}"
VERSION="${2:-$(tr -d '[:space:]' < "$SCRIPT_DIR/VERSION")}"

if [[ -z "$RID" ]]; then
    case "$(uname -s)-$(uname -m)" in
        Linux-x86_64) RID="linux-x64" ;;
        Linux-aarch64|Linux-arm64) RID="linux-arm64" ;;
        Darwin-x86_64) RID="osx-x64" ;;
        Darwin-arm64) RID="osx-arm64" ;;
        MINGW*|MSYS*|CYGWIN*) RID="win-x64" ;;
        *) echo "Unable to infer a runtime identifier; pass one explicitly." >&2; exit 2 ;;
    esac
fi

if [[ ! "$RID" =~ ^(win|linux|osx)-(x64|arm64)$ ]]; then
    echo "Unsupported runtime '$RID'. Expected win-, linux-, or osx- with x64 or arm64." >&2
    exit 2
fi

if [[ ! "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z.-]+)?$ ]]; then
    echo "Invalid version '$VERSION'." >&2
    exit 2
fi

SOURCE_DIR="$SCRIPT_DIR/Source"
ARTIFACT_DIR="$SCRIPT_DIR/artifacts"
STAGING_DIR="$ARTIFACT_DIR/staging/$RID"
APP_DIR="$STAGING_DIR/PlanarGeometryStudio"
ASSET_STEM="PlanarGeometryStudio-v$VERSION-$RID"

rm -rf "$STAGING_DIR"
mkdir -p "$APP_DIR/tools/engine" "$APP_DIR/tools/drawer" "$ARTIFACT_DIR"

PUBLISH_ARGS=(
    --configuration Release
    --runtime "$RID"
    --self-contained true
    -p:Version="$VERSION"
    -p:PublishSingleFile=true
    -p:PublishTrimmed=false
    -p:EnableCompressionInSingleFile=true
    -p:IncludeNativeLibrariesForSelfExtract=true
    -p:DebugSymbols=false
    -p:DebugType=None
)

echo "Publishing Planar Geometry Studio $VERSION for $RID"

dotnet publish "$SOURCE_DIR/Launchers/GeoGen.DesktopApp/GeoGen.DesktopApp.csproj" \
    "${PUBLISH_ARGS[@]}" --output "$APP_DIR"

dotnet publish "$SOURCE_DIR/Launchers/GeoGen.MainLauncher/GeoGen.MainLauncher.csproj" \
    "${PUBLISH_ARGS[@]}" --output "$APP_DIR/tools/engine"

dotnet publish "$SOURCE_DIR/Launchers/GeoGen.DrawingLauncher/GeoGen.DrawingLauncher.csproj" \
    "${PUBLISH_ARGS[@]}" --output "$APP_DIR/tools/drawer"

mkdir -p "$APP_DIR/tools/drawer/Data"
cp -R "$SOURCE_DIR/Launchers/GeoGen.DrawingLauncher/Data/." "$APP_DIR/tools/drawer/Data/"

rm -rf \
    "$APP_DIR/tools/engine/Examples/Output" \
    "$APP_DIR/tools/engine/Logs" \
    "$APP_DIR/tools/drawer/Logs"

cp "$SCRIPT_DIR/LICENSE" "$APP_DIR/LICENSE.txt"
cp "$SCRIPT_DIR/README.md" "$APP_DIR/README.md"
cp "$SCRIPT_DIR/CHANGELOG.md" "$APP_DIR/CHANGELOG.md"

if [[ "$RID" == osx-* ]]; then
    BUNDLE_DIR="$STAGING_DIR/Planar Geometry Studio.app"
    MACOS_DIR="$BUNDLE_DIR/Contents/MacOS"
    mkdir -p "$MACOS_DIR" "$BUNDLE_DIR/Contents/Resources"
    cp -R "$APP_DIR/." "$MACOS_DIR/"
    chmod +x "$MACOS_DIR/PlanarGeometryStudio"

    cat > "$BUNDLE_DIR/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDisplayName</key><string>Planar Geometry Studio</string>
  <key>CFBundleExecutable</key><string>PlanarGeometryStudio</string>
  <key>CFBundleIdentifier</key><string>com.nikolaveselinov.planargeometrystudio</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>CFBundleName</key><string>Planar Geometry Studio</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST

    ASSET_PATH="$ARTIFACT_DIR/$ASSET_STEM.zip"
    rm -f "$ASSET_PATH"
    (cd "$STAGING_DIR" && zip -qry "$ASSET_PATH" "Planar Geometry Studio.app")
elif [[ "$RID" == linux-* ]]; then
    ASSET_PATH="$ARTIFACT_DIR/$ASSET_STEM.tar.gz"
    rm -f "$ASSET_PATH"
    tar -C "$STAGING_DIR" -czf "$ASSET_PATH" PlanarGeometryStudio
else
    ASSET_PATH="$ARTIFACT_DIR/$ASSET_STEM.zip"
    rm -f "$ASSET_PATH"
    (cd "$STAGING_DIR" && zip -qry "$ASSET_PATH" PlanarGeometryStudio)
fi

echo "$ASSET_PATH"
