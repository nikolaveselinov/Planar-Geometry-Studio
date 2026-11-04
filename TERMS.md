# Planar Geometry Studio — License, use, and setup notes

Last updated: 4 November 2026

Planar Geometry Studio is a free desktop application based on GeoGen by Patrik Bak, maintained by Nikola Veselinov, and available on Windows, macOS, and Linux. No payment, registration, or user account is required.

## Free software license

Studio is licensed under the **GNU Affero General Public License, version 3 (AGPL-3.0-only)**. The complete license accompanies each application package as `LICENSE.txt` and appears in the source repository as `LICENSE`. The corresponding versioned source, build scripts, and original engine credits are available at https://github.com/nikolaveselinov/Planar-Geometry-Studio; the original GeoGen project is https://github.com/PatrikBak/GeoGen.

You may use, modify, share, and redistribute Studio under the terms of the AGPL. **These notes do not impose any restrictions in addition to that license**, or limit your rights to inspect the source, reverse-engineer, modify, or redistribute it. Applicable third-party software retains its own license terms.

## Your work

The application does not claim ownership of input configurations, generated theorems, proofs, figures, or other mathematical work created with Studio. Your results are saved locally, ordinarily in `Documents/Planar Geometry Studio/Runs/`. They are preserved separately from the application when it updates or is uninstalled; back up important work yourself.

## Privacy and automatic updates

See the detailed [privacy notice](PRIVACY.md).

Studio has no accounts, advertising, in-app analytics, or telemetry. By default it checks the GitHub Releases API for a newer stable version when it opens and may download a verified application archive in the background. GitHub receives ordinary connection information including your IP address, request time, and a User-Agent identifying the Studio version. Your geometry input, results, logs, and local file paths are not sent. The download is checked against GitHub's published size and SHA-256 digest, and prepared locally for a later launch or an optional restart.

Disable automatic checks or automatic downloads independently in **Help → Studio Setup**. Manual checks remain available. Offline use and failed update checks do not prevent geometry generation or proofs. Downloaded updates may be large. The old installed application remains available as a recovery copy.

## Optional third-party drawing tools

The geometry engine and .NET runtime are bundled. Creating figures requires MetaPost/TeX; PDF conversion also requires suitable conversion tools. Studio Setup may install missing tools if you select that option. Such downloads contact independent distributors, may require system permissions, and use those providers' licenses and privacy notices. Shared third-party tools are not necessarily uninstalled when Studio is removed.

- [MiKTeX license](https://miktex.org/copying) and [privacy](https://miktex.org/privacy)
- [TeX Live license information](https://tug.org/texlive/copying.html)
- [Ghostscript licensing](https://www.ghostscript.com/licensing/)
- [GitHub privacy statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement)

## Accuracy, warranty, and legal rights

Generated statements and proofs may be wrong and should be independently checked before being relied on. The AGPL addresses warranty and liability in sections 15–17. Nothing in these notes excludes consumer rights, data-protection rights, or any other protections that may not legally be waived.

For help, licensing or privacy matters, contact **nikola.veselinov.veselinov@gmail.com**. Report security vulnerabilities privately; see [SECURITY.md](SECURITY.md).

---

Adapted with reference to [General Legal's terms-of-use template](https://github.com/General-Legal/legal-templates/tree/main/templates/terms-of-use) (CC0 1.0). Its proprietary restrictions, account provisions, advertising clauses, and U.S. arbitration agreement do not apply to this AGPL desktop application.
