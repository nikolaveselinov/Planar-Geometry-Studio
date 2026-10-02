# Planar Geometry Studio — terms and setup notes

Effective 2 October 2026.

## Your license

Planar Geometry Studio is free software under the GNU Affero General Public
License, version 3. The complete license is supplied in LICENSE.txt and at
https://github.com/nikolaveselinov/Planar-Geometry-Studio/blob/master/LICENSE.
These notes explain installation and operation; they do not add restrictions
to, replace, or reduce the rights granted by that license.

The GeoGen engine was created by Patrik Bak. Studio is maintained by Nikola
Veselinov. Source code, including the source for each released version, is at
https://github.com/nikolaveselinov/Planar-Geometry-Studio.

## Your work

Configurations, generated theorems, proofs, and figures stay on your computer.
These terms make no claim to ownership of your work. Runs are saved separately
from the installed application, in Documents/Planar Geometry Studio/Runs.
Updating or uninstalling Studio does not delete them.

## Network access and privacy

Studio has no analytics, advertising, account requirement, or telemetry.
By default, it checks the public GitHub Releases API once on each launch for
a newer stable version. GitHub receives the usual request information, including
your IP address and the Studio version in its User-Agent. Your configurations,
results, and local file paths are not included. You can disable automatic checks
in Studio Setup, or check manually from Help. By default, newer stable versions
also download from the official GitHub release in the background. Studio verifies
the published SHA-256 checksum and prepares a separate copy in your user folder.
The new copy opens on your next launch, or when you choose Restart to update;
Studio does not interrupt a running session. Unsaved work gets the normal save
prompt before restarting. You can disable automatic downloads in Studio Setup.
Download requests go to GitHub and its download servers; no work files are sent.
Updates include the app, engine, drawing launcher, and runtime; optional TeX tools
are reused. Downloads can be substantial. Offline or unsuccessful checks and
downloads do not prevent you from working. The installed copy remains available
as a recovery launcher, and operating-system trust warnings may still appear.

## Optional drawing tools

The application and its .NET runtime are included. Creating figures also needs
MetaPost and TeX; PDF conversion needs epstopdf or Ghostscript. Setup detects
existing tools before offering installation. Generation and proof search work
without these optional tools.

If you choose to install drawing tools, setup contacts their distributors and
package repositories. Windows uses MiKTeX, macOS uses BasicTeX/TeX Live, and
Linux uses your distribution's packages. These are independent software with
their own licenses and privacy policies; review them before selecting install.
Operating-system permission prompts may appear. Downloads can be substantial.
An unsuccessful dependency installation can be retried from Studio Setup.
Uninstalling Studio leaves shared drawing tools available to your other programs.

MiKTeX: https://miktex.org/copying and https://miktex.org/privacy
TeX Live: https://tug.org/texlive/copying.html
Ghostscript: https://www.ghostscript.com/licensing/
GitHub: https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement

## Warranty and mathematical output

The software is provided without warranty, as described in sections 15–17 of
the AGPL. Review generated statements and proofs before relying on them.
These notes do not exclude rights that cannot lawfully be excluded.

Report problems at
https://github.com/nikolaveselinov/Planar-Geometry-Studio/issues.
