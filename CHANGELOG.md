# Changelog

## [1.1.2] - 2026-08-28

- Generation failures now return an error instead of reporting success.
- Run folders stay unique across concurrent app instances.
- Release builds now run the full test suite and an installed-engine smoke test.
- Release versions must match the app and changelog, and existing releases cannot be overwritten.
- Linux archives no longer preserve CI runner ownership.

## [1.1.1] - 2026-08-27

### Application

- Fixed cancellation and child-process cleanup.
- Prevented stale or empty PDFs from being reported as successful figure conversions.
- Made run and figure workspaces collision-safe.
- Fixed GeoGen tool discovery for RID-specific development builds.

### Development

- Desktop builds now treat warnings as errors and build with zero warnings.
- Added regression tests for process lifecycle, redirected input, workspaces, and tool discovery.
- Kept Fluent Assertions on the maintained open-source 7.x line.

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

[1.1.2]: https://github.com/nikolaveselinov/Planar-Geometry-Studio/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/nikolaveselinov/Planar-Geometry-Studio/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/nikolaveselinov/Planar-Geometry-Studio/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/tag/v1.0.0
