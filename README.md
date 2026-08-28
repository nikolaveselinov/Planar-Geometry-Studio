# Planar Geometry Studio

[![CI](https://github.com/nikolaveselinov/Planar-Geometry-Studio/actions/workflows/ci.yaml/badge.svg)](https://github.com/nikolaveselinov/Planar-Geometry-Studio/actions/workflows/ci.yaml)
[![Latest release](https://img.shields.io/github/v/release/nikolaveselinov/Planar-Geometry-Studio)](https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/latest)
[![License: AGPL-3.0](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](LICENSE)

A desktop application for generating, proving, ranking, and drawing planar geometry theorems. It is based on [GeoGen](https://github.com/PatrikBak/GeoGen) by Patrik Bak.

## Download

Download the [latest release](https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/latest).

| Platform | x64 | Arm64 |
|---|---|---|
| Windows | `win-x64.zip` | `win-arm64.zip` |
| Linux | `linux-x64.tar.gz` | `linux-arm64.tar.gz` |
| macOS | `osx-x64.zip` | `osx-arm64.zip` |

Extract the archive. Run `PlanarGeometryStudio.exe` on Windows, `PlanarGeometryStudio` on Linux, or open `Planar Geometry Studio.app` on macOS. The packages include the application, GeoGen, and the .NET runtime.

The macOS builds are unsigned. On first launch, right-click the app and select **Open**.

Drawing figures requires MetaPost from [TeX Live](https://tug.org/texlive/) or [MiKTeX](https://miktex.org/). If no PDF converter is installed, figures are saved as EPS files.

## Use

1. Write an input configuration.
2. Press <kbd>F5</kbd> or select **Generate**.
3. Select **Open Results** to view the output.
4. Select **Figures** to draw the latest result.

Runs are stored in `Documents/Planar Geometry Studio/Runs/`. Existing runs are not overwritten.

### Example

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

See the [input and output reference](InputOutputFormat.md) for the full format.

## Output

| Directory | Contents |
|---|---|
| `ReadableWithoutProofs/` | Theorem statements |
| `ReadableWithProofs/` | Theorems and proofs |
| `ReadableBestTheorems/` | Highest-ranked theorems |
| `JsonOutput/` | JSON output |
| `JsonBestTheorems/` | Highest-ranked theorems in JSON |
| `Logs/` | Run logs |
| `Figures/` | EPS and PDF figures |

## Build

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run:

```bash
dotnet restore Source/GeoGen.sln
dotnet build Source/GeoGen.sln --configuration Release
dotnet test Source/GeoGen.sln --configuration Release
dotnet run --project Source/Launchers/GeoGen.DesktopApp/GeoGen.DesktopApp.csproj
```

To build a release package:

```bash
./publish.sh linux-x64
```

On Windows, use `./publish.ps1 -Runtime win-x64`.

## License

[GNU AGPL v3.0](LICENSE). The GeoGen engine was created by [Patrik Bak](https://github.com/PatrikBak).
