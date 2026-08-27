# Changelog

## [1.1.0] - 2026-08-27

### Application

- Reworked the desktop interface.
- Each run is now stored in a separate folder.
- Added input validation, cancellation, and unsaved-change handling.
- Fixed figure generation from installed builds.
- EPS files are saved when PDF conversion is unavailable.

### GeoGen

- Merged 20 upstream GeoGen commits.
- Added `CircleWithRadius` and its inference rules.
- Tightened validation for right triangles and cyclic quadrilaterals.
- Fixed process output handling and non-interactive runs.

### Release

- Packages now include the desktop application, GeoGen engine, drawing tool, rules, settings, and .NET runtime.
- Added x64 and Arm64 packages for Windows, Linux, and macOS.
- Added checksums, CI, package checks, tests, and Dependabot.
- Removed generated output from the repository.

## [1.0.0] - 2026-04-01

- First desktop release.

[1.1.0]: https://github.com/nikolaveselinov/Planar-Geometry-Studio/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/tag/v1.0.0
