# Changelog

All notable changes to Planar Geometry Studio are documented here. The project follows [Semantic Versioning](https://semver.org/).

## [1.1.0] - 2026-08-27

### Highlights

- Redesigned the desktop experience with a focused editor, live engine console, clear run status, keyboard shortcuts, and built-in input reference.
- Rebuilt generation around isolated, timestamped workspaces in the user's documents folder so installed application files stay read-only and previous results are never overwritten.
- Added dependable cancellation, unsaved-change protection, input validation, safe process arguments, bounded console output, and actionable error reporting.
- Repaired figure generation by copying drawing resources into a writable run workspace and preserving EPS output when no PDF converter is installed.

### Engine and correctness

- Integrated the latest GeoGen upstream performance, determinism, theorem-proving, analytic-geometry, and integration-test improvements.
- Added the `CircleWithRadius` construction and its inference rules.
- Strengthened validation for cyclic quadrilaterals and right triangles.
- Prevented redirected and desktop-launched engine processes from blocking on an interactive key press.
- Fixed subprocess output handling to avoid deadlocks and lost output.

### Distribution and maintenance

- Replaced the incomplete release job with reproducible packages that bundle the Studio, engine, drawer, rules, settings, license, and documentation.
- Added self-contained Windows, Linux, and macOS packages for x64 and Arm64, plus SHA-256 checksums.
- Added CI build, test, and package-content checks, focused desktop service tests, Dependabot, and contributor/security guidance.
- Removed generated MetaPost, PDF, log, and example-output artifacts from version control.

## [1.0.0] - 2026-04-01

- Initial Planar Geometry Studio desktop release based on GeoGen.

[1.1.0]: https://github.com/nikolaveselinov/Planar-Geometry-Studio/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/tag/v1.0.0
