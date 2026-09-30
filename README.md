# Planar Geometry Studio

[![CI](https://github.com/nikolaveselinov/Planar-Geometry-Studio/actions/workflows/ci.yaml/badge.svg)](https://github.com/nikolaveselinov/Planar-Geometry-Studio/actions/workflows/ci.yaml)
[![Latest release](https://img.shields.io/github/v/release/nikolaveselinov/Planar-Geometry-Studio)](https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/latest)
[![License: AGPL-3.0](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](LICENSE)

A desktop application for generating, proving, ranking, and drawing planar geometry theorems. It is based on [GeoGen](https://github.com/PatrikBak/GeoGen) by Patrik Bak.

## Download

Download the [latest release](https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/latest).

| Platform | x64 | Arm64 |
|---|---|---|
| Windows | `win-x64-setup.exe` | `win-arm64-setup.exe` |
| Linux | `linux-x64.deb`, `.rpm`, or `.run` | `linux-arm64.deb`, `.rpm`, or `.run` |
| macOS | `osx-x64.pkg` or `.dmg` | `osx-arm64.pkg` or `.dmg` |

Asset names begin with `PlanarGeometryStudio-v<version>-`. Download the installer for your platform and processor. Windows setup offers Program Files or per-user installation, Start Menu and desktop shortcuts, optional drawing tools, and an uninstaller. macOS installs in Applications. On Linux, install `.deb` with apt, `.rpm` with dnf, or run `bash <filename>.run` for a per-user setup wizard.

The packages include the application, GeoGen, and the .NET runtime. Portable `.zip` and `.tar.gz` archives remain available. Installer details and commands are in the [installation guide](packaging/README.md).

On first launch, **Studio Setup** detects existing drawing tools and offers automatic installation of MiKTeX on Windows, BasicTeX on macOS, or TeX Live on Linux, including MetaPost and PDF export. It is also available from **Help → Studio Setup**. Generation and proofs work without these optional downloads. See the [terms and setup notes](TERMS.md).

Studio checks for new stable releases in the background on every launch. A notice links to the installer for your platform. Manage automatic checks in Studio Setup, or select **Help → Check for Updates**. Your configurations and results stay local.

Windows installers are self-signed as **Nikola Veselinov**. You can see this name in **Properties → Digital Signatures**. Windows may still display “Unknown publisher” or security warnings because the certificate is not publicly trusted. macOS apps are ad-hoc signed, with unsigned packages.

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
