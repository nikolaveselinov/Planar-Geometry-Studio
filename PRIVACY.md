# Planar Geometry Studio — Privacy notice

Last updated: 4 November 2026

This notice describes how Planar Geometry Studio, a free open-source desktop application, handles information when you use it, download updates, install optional drawing software, or contact its maintainer.

## Publisher and contact

Planar Geometry Studio is maintained by **Nikola Veselinov**, Sofia, Bulgaria. For privacy questions and requests, email **nikola.veselinov.veselinov@gmail.com**. Project source and releases are at https://github.com/nikolaveselinov/Planar-Geometry-Studio.

## Information stored on your device

Studio has no accounts, advertising, in-app analytics, tracking, or telemetry. Your configurations, generated theorems, proofs, figures, and run logs are processed locally and are not uploaded to the maintainer. Runs are ordinarily saved in `Documents/Planar Geometry Studio/Runs/`. Preferences, including update choices, are stored locally in the user-data folder under `PlanarGeometryStudio`; downloaded application updates are stored separately in that user-data folder. You control those files using your operating system. Uninstalling Studio may leave these files in place.

## Update checks and downloads

**By default**, Studio contacts the public GitHub Releases API on each launch to check for a newer stable version. It may download an available release archive from GitHub's download servers in the background. The request identifies itself using the `PlanarGeometryStudio/<version>` User-Agent. GitHub and its delivery providers necessarily receive your IP address, request time, and ordinary connection information. The request does **not** contain project files, local paths, generated results, or proofs. The maintainer does not receive individual-user usage reports or an analytics database from these requests.

Turn off **Check for a new stable version every time Studio opens** or **Download verified updates automatically** under **Help → Studio Setup**. These are separate settings. The first prevents automatic launch-time checks; the second prevents background downloads. You can also check manually and can use the core application offline. GitHub's processing of data it receives is governed by its [privacy statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

## Optional drawing software

If you choose to install drawing tools, Studio runs a setup script or package manager that may contact MiKTeX, TeX Live, package repositories, and related services. These providers receive network requests directly from your device and operate under their own notices. These downloads are optional; the core geometry engine works without them. Consult [the setup notes](TERMS.md) before installing third-party software.

## Messages and correspondence

If you email the maintainer, your email address, message, and attachments will be processed to respond to the request. Information that you post to GitHub is subject to GitHub's own privacy practices. Avoid including private project files or sensitive personal information in public reports.

## Purposes, legal grounds, and recipients

Where the GDPR applies to processing for which the maintainer is responsible, purposes include responding to correspondence, maintaining and securing free software, and complying with legal requirements. The relevant grounds are generally legitimate interests (GDPR Article 6(1)(f)) and, where applicable, compliance with a legal obligation (Article 6(1)(c)). You may object to processing based on legitimate interests by contacting the maintainer. Update requests go directly to GitHub, not to a server controlled by the maintainer. The maintainer does not sell or share personal data for targeted advertising.

GitHub, email services, and optional drawing-tool distributors may process information outside Bulgaria or the European Economic Area. Their privacy policies explain their own roles, retention rules, and international-transfer arrangements. No database of Studio users is transmitted to those providers.

## Retention, security, and rights

The files created by Studio remain on your device until you delete them. The maintainer does not set their retention period or remotely control them. Correspondence is retained as reasonably necessary to respond and handle related legal obligations, then removed when no longer needed. Third-party providers determine their own retention as described in their notices.

GitHub update requests use HTTPS; downloaded archives are checked against the size and SHA-256 digest reported by GitHub. This provides an integrity check but is not a guarantee against all security problems.

Depending on applicable law and circumstances, you may request access, correction, deletion, restriction, or portability of your personal data and may object to processing. Contact the maintainer by email. You may complain to the Bulgarian [Commission for Personal Data Protection](https://cpdp.bg/en/) or another competent supervisory authority. Studio does not profile users or make automated decisions about individuals.

## Children, cookies, and changes

Studio is an educational mathematics application. It does not require disclosure of age or track children. The desktop app does not use web cookies. Websites you choose to visit, including GitHub, have their own cookie policies.

Changes to this notice will be published in the source repository and in subsequent releases. Changes adding personal-data collection or new network services should be disclosed before those features are used.

---

Drafted for the actual desktop application with reference to the [General Legal GDPR-enhanced privacy-policy template](https://github.com/General-Legal/legal-templates/tree/main/templates/privacy-policy-gdpr), CC0 1.0. Use of the template is not proof of compliance.
