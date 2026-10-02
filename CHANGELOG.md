# Changelog

## [1.2.6] - 2026-10-02

- Fix a startup crash that prevented packaged installations and portable copies from generating figures.
- Include the drawing tool’s error details when figure generation fails.

## [1.2.5] - 2026-10-02

- Download verified updates automatically in the background and open them on the next launch.
- Restart from inside Studio, with the normal save prompt for unsaved input.
- Update installed and portable copies without administrator rights, reusing existing drawing tools and user settings.
- Keep the installed copy available for recovery if a prepared update cannot start.
- Control automatic downloads separately from update checks in Studio Setup.

## [1.2.4] - 2026-10-02

- Enable or disable multiple generation tools with compact checkboxes while the construction browser stays open.
- See your current construction choices immediately and keep them while searching or filtering.
- Update the input as you select tools, preserving initial definitions and the rest of your configuration.

## [1.2.3] - 2026-09-30

- Use a restrained dark interface with standard controls and simpler setup screens.

- Search the full construction catalog in the app, inspect argument types, copy calls, and enable individual generation tools.
- Start with a focused triangle-and-medians configuration instead of a long list of constructions.
- Find the right download from a platform table, with optional verification files collected in one archive.
- Keep macOS installation in Applications when a portable copy exists elsewhere.

## [1.2.2] - 2026-09-30

- Add download signatures and verification instructions for macOS and Linux installers and portable archives.

## [1.2.1] - 2026-09-30

- Add signatures and publisher information to Windows installers and portable executables.

## [1.2.0] - 2026-09-30

- Native Windows setup executables, macOS packages/disk images, and Linux deb/rpm/per-user installers for x64 and ARM64.
- Guided Studio Setup with automatic MiKTeX/TeX Live dependency installation, tool detection, privacy preferences, and setup notes.
- Background update checks on every launch, native installer links, and manual checking from Help.
- Start Menu/application menu integration, upgrade and uninstall support, and original installer artwork.
- Discover newly installed TeX tools immediately, including GUI launches with an old PATH.
- Preserve the existing AGPL license and all user runs during installation, upgrading, and uninstalling.
- Shared cross-platform packaging CI with installer and update-service tests.

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
