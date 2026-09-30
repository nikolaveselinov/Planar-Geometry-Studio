# Native installers

Release assets are built for Windows, Linux, and macOS on x64 and ARM64. Each
contains Studio, both GeoGen launchers, data/rules, the .NET runtime, the AGPL
license, and the setup/privacy notes. Matching source is available in the
GitHub release's source archives and tag.

## Windows

Run `publish.ps1 -Runtime win-x64`, then
`packaging/windows/build-installer.ps1 -Runtime win-x64 -SelfSign`.
Use `win-arm64` for ARM. Install Inno Setup 6.3 or later first.

`*-setup.exe` supports per-machine installation in Program Files or per-user
installation without elevation, a chosen Start Menu folder, an optional desktop
shortcut, setup shortcut, Add/Remove Programs registration, and an uninstaller.
The application identity is stable across versions and architectures.

Drawing tools are selected by default. When Studio opens after installation,
it runs dependency setup as the original desktop user, even when the installer
was elevated. MiKTeX is installed for that user via WinGet; computers without
WinGet use the official MiKTeX 25.12 installer, checked against Microsoft's
published SHA-256 before execution. On Windows 11 ARM, MiKTeX's x64 tools run
under Windows emulation while Studio itself is native ARM64.
Existing TeX installations are reused. MiKTeX setup prepares MetaPost and
epstopdf and enables MiKTeX's installation of missing packages.

For unattended core installation:

```powershell
.\PlanarGeometryStudio-v1.2.1-win-x64-setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CURRENTUSER
```

Silent installation deliberately does not launch Studio or download drawing
tools. Open Studio to review setup notes and complete optional dependencies.
`/ALLUSERS` selects machine installation. Run `Studio Setup` from Start or
Help at any time to repair dependencies or change update preferences.

## macOS

On a Mac, run `./publish.sh osx-arm64`, then
`./packaging/macos/build-installer.sh osx-arm64`.
Use `osx-x64` for Intel. macOS's `pkgbuild`, `productbuild`, and `hdiutil` are
required; Python 3 is used only while building installer resources.

The `.pkg` installs in `/Applications`; the `.dmg` contains the same installer.
macOS 14 (Sonoma) or later is required by the bundled .NET 10 runtime.
The wizard includes welcome, setup/privacy notes, AGPL license, and completion
pages. First launch opens Studio Setup. It downloads checksum-verified BasicTeX
when needed and adds MetaPost, plain TeX, and epstopdf to the existing TeX Live
installation. Ghostscript is installed through existing Homebrew or the small,
signed MacTeX Ghostscript package with its official SHA-512 checked. Homebrew
is not required or installed by Studio. System authorization uses native prompts.

Move the app to Trash to uninstall. Studio preferences, runs, and shared TeX
tools are preserved.

## Linux

Run `./publish.sh linux-x64`, then
`./packaging/linux/build-installers.sh linux-x64`.
Use `linux-arm64` for ARM. Install `dpkg-deb` and `rpmbuild` on the build machine.

- `.deb`: Debian/Ubuntu package, installed under `/opt/planargeometrystudio`,
  with an application menu entry and `planargeometrystudio` command.
  Use `sudo apt install ./PlanarGeometryStudio-v1.2.1-linux-x64.deb`; apt resolves
  native runtime requirements and recommended drawing tools.
- `.rpm`: Fedora-compatible package, same layout. Use
  `sudo dnf install ./PlanarGeometryStudio-v1.2.1-linux-x64.rpm`.
- `.run`: distribution-independent, per-user installer. Run `bash filename.run`.
  Uses a graphical wizard if Zenity is installed, otherwise terminal prompts.
  Defaults to `~/.local/opt/planargeometrystudio`; integrates with the application
  menu; verifies its embedded archive; replaces managed installations atomically;
  refuses to replace unrelated directories; includes `uninstall.sh`.

The `.run` core needs the system libraries required by Avalonia and .NET 10.
For a machine with missing native libraries, prefer the `.deb` or `.rpm`, whose
package manager resolves them. Supported automatic drawing setup covers apt,
dnf, pacman, and zypper. `pkexec` provides a desktop permission prompt. If no
graphical privilege helper is available, setup displays the terminal command.
Generation and proof search work without TeX.

## Update checks

The app requests the GitHub latest stable release endpoint once on each launch,
in the background, with an eight-second timeout and a one-megabyte response
limit. No application files, input, output, or local paths are sent. Drafts and
prereleases are ignored. Semantic versions are compared numerically.
The notice links to the exact platform/architecture installer, with a verified
repository download URL. If a release has no matching installer it links to its
release page. Downloads are handled by the browser, and the user runs the native
installer. There is no background replacement of the running application.

Disable checks in **Help → Studio Setup → Workspace**, or run a manual check
from **Help → Check for Updates**. Preferences are written atomically to the
user's local application data directory, never to Program Files or the app bundle.

## Signing

Windows release builds use SHA-256 Authenticode signatures with the certificate
subject **Nikola Veselinov**, including Studio, both GeoGen executables, the
installer, and its uninstaller. The portable Windows archives contain the same
signed application files. The desktop executable also records Nikola Veselinov
as its company in File Properties.

These certificates are **self-signed**, not verified publisher certificates.
The name appears in **Properties → Digital Signatures**; Windows may continue to
show **Unknown publisher** in UAC or SmartScreen. This does not remove security
warnings or establish a publicly trusted identity. Setup never changes certificate
trust stores. Each Windows packaging job creates a non-exportable build-local key,
publishes only its public certificate inside `setup/publisher.cer`, and deletes
the private key after compilation. Each build therefore has a different public key.
No private key or PFX is committed or uploaded. Local unsigned builds remain
available by omitting `-SelfSign`; self-signing requires the Windows SDK SignTool.

macOS apps are ad-hoc signed for Apple Silicon compatibility; macOS packages are
unsigned and not notarized. OS reputation/Gatekeeper prompts may appear.
Verified publisher signing requires a trusted Windows signing identity and Apple
Developer ID/notarization credentials. These cannot be replaced by self-signatures.

## Validation

CI and release builds share `.github/workflows/package-build.yaml`. Unit tests
cover version ordering, API failures, caller cancellation, malformed and oversized
responses, platform selection, rejected foreign download URLs, persistent settings,
and tool paths with spaces. Packages must pass installed-engine smoke tests.
Windows x64 setup is installed, reinstalled, and uninstalled on Windows. Both
Linux architectures exercise `.run` install/upgrade/uninstall, directory protection,
and corrupted-payload rejection, and validate `.deb`/`.rpm` metadata. Both macOS
architectures install the native `.pkg`, verify bundle signing, and run the engine.
Windows ARM64 packages are cross-built on the Windows x64 runner; their native
execution and the internet-dependent drawing setup still merit testing on end-user
machines. Shared drawing tools are never removed by the Studio uninstaller.
