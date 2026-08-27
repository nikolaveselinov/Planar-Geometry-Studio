# Planar Geometry Studio

[![CI](https://github.com/nikolaveselinov/Planar-Geometry-Studio/actions/workflows/ci.yaml/badge.svg)](https://github.com/nikolaveselinov/Planar-Geometry-Studio/actions/workflows/ci.yaml)
[![Latest release](https://img.shields.io/github/v/release/nikolaveselinov/Planar-Geometry-Studio)](https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/latest)
[![License: AGPL-3.0](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](LICENSE)

Planar Geometry Studio is a cross-platform desktop environment for automatically discovering, proving, ranking, and drawing planar-geometry theorems. It pairs a focused Avalonia editor with the [GeoGen](https://github.com/PatrikBak/GeoGen) engine by Patrik Bak.

## Why use it?

- Write and validate generation configurations in one focused workspace.
- Run GeoGen with live progress, reliable cancellation, and useful failure details.
- Keep every run in its own timestamped folder—inputs and results are never silently overwritten.
- Review readable theorems, full proofs, ranked results, and machine-readable JSON.
- Render generated configurations with MetaPost and convert figures to PDF when a supported converter is available.
- Install nothing else for theorem generation: release packages include the desktop app, .NET runtime, engine, drawer, rules, and settings.

## Download

Download the archive for your system from the [latest release](https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/latest):

| Platform | Intel / AMD 64-bit | Arm 64-bit |
|---|---|---|
| Windows | `win-x64.zip` | `win-arm64.zip` |
| Linux | `linux-x64.tar.gz` | `linux-arm64.tar.gz` |
| macOS | `osx-x64.zip` | `osx-arm64.zip` |

Extract the archive and launch `PlanarGeometryStudio` (`PlanarGeometryStudio.exe` on Windows). On macOS, open **Planar Geometry Studio.app**. Release checksums are provided in `SHA256SUMS`.

> Figure rendering additionally requires MetaPost from [TeX Live](https://tug.org/texlive/) or [MiKTeX](https://miktex.org/). The Studio keeps generated EPS figures if a PDF converter is unavailable.

## Quick start

1. Enter a configuration in the editor. The bundled example is ready to run.
2. Select **Generate** or press <kbd>Ctrl</kbd>+<kbd>Enter</kbd>.
3. Follow progress in the live console; use <kbd>Esc</kbd> to cancel safely.
4. Select **Open results** to inspect theorem and JSON output.
5. Select **Generate figures** to draw configurations from the latest run.

Each run is stored under `Documents/Planar Geometry Studio/Runs/` in a unique directory.

### Minimal input

```text
Constructions:

 Midpoint
 Median

Initial configuration:

 Triangle: A, B, C

Iterations: 1
MaximalPoints: 1
MaximalLines: 1
MaximalCircles: 0
SymmetryGenerationMode: GenerateBothSymmetricAndAsymmetric
```

The in-app **Input reference** documents available configuration types, parameters, symmetry modes, and constructions including `CircleWithRadius`.

## Results

| Directory | Contents |
|---|---|
| `ReadableWithoutProofs/` | Human-readable theorem statements |
| `ReadableWithProofs/` | Theorems with complete generated proofs |
| `ReadableBestTheorems/` | Top-ranked theorems |
| `JsonOutput/` | Machine-readable generation results |
| `JsonBestTheorems/` | Machine-readable ranked results |
| `Logs/` | Engine diagnostics for that run |
| `Figures/` | EPS and, when conversion is available, PDF figures |

## Build from source

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run:

```bash
dotnet restore Source/GeoGen.sln
dotnet build Source/GeoGen.sln --configuration Release
dotnet test Source/GeoGen.sln --configuration Release
dotnet run --project Source/Launchers/GeoGen.DesktopApp/GeoGen.DesktopApp.csproj
```

Create a self-contained package with:

```bash
./publish.sh linux-x64
```

Windows users can run `./publish.ps1 -Runtime win-x64`. Supported runtime identifiers are `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`.

See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidance and [CHANGELOG.md](CHANGELOG.md) for release details.

## Project lineage and license

The original GeoGen engine—configuration generation, theorem discovery, proof, simplification, sorting, and ranking—was created by [Patrik Bak](https://github.com/PatrikBak). Planar Geometry Studio adds the desktop workflow and distribution while continuing to integrate upstream improvements.

Licensed under [GNU AGPL v3.0](LICENSE).
