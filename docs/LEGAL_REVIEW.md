# Legal and distribution review — maintainer checklist

Technical review performed 4 November 2026 against master at `044e1f1cc45c371d996d0b7495ffef6d4b5fc4d6` and release v1.2.6. **Not legal advice, a compliance certification, or a guarantee.**

## Verified from code and packaging

- GeoGen is by Patrik Bak; the parent and this fork use AGPL-3.0.
- Studio saves input, proofs, output, logs and preferences locally. No user accounts, analytics or crash telemetry were found in the reviewed desktop application.
- Launch-time update checks go to GitHub's release API with the app version in User-Agent. Optional downloads use GitHub release assets. Users can disable either update check or background download in Studio Setup.
- Optional drawing-tool installation uses MiKTeX, TeX Live and/or package repositories; these are independent third parties.
- A first-launch Studio Setup screen appears before automatic update checks.
- Generic Terms of Use from General Legal include proprietary limitations and U.S. arbitration clauses that would be unsuitable for an AGPL desktop app.
- No app cookies or advertising SDK were found, so a generic cookie banner would misstate the desktop product's practices.

## Release checklist

- [x] Preserve the AGPL and credit the original GeoGen author.
- [x] Supply app-specific privacy notice, separate from a short setup/license guide.
- [x] Explain default update network requests and expose choices in first-run setup.
- [x] Embed privacy notice in the first-run UI and bundle it with future portable packages/installers.
- [x] Provide a working contact rather than a disabled GitHub Issues link.
- [ ] **Audit all third-party licenses and notices in release packages.** Inspect complete direct and transitive runtime NuGet dependencies, the bundled .NET runtime, fonts including Inter, and assets. Supply all required license text, notices and attributions in each shipped package. The project's AGPL is not a substitute for these.
- [ ] **Verify release/source correspondence.** Verify every tagged source archive, included build scripts, dependency sources as required, and any installation information against the actual Windows, macOS and Linux artifacts.
- [ ] **Confirm publisher disclosure obligations.** The public contact email and city are provided, but a complete establishment address, trader information and registration details may be required under Bulgarian/EU law if distribution constitutes an information-society service by a trader. Do not publish a home address without deciding the appropriate public business address.
- [ ] **Confirm support-email retention and access** match the privacy notice.
- [ ] **Confirm external-installer choice,** especially the Windows drawing-tools component selected by default, is transparent and suitable for schools and other users.
- [ ] **Inspect newly rebuilt assets.** Already published v1.2.6 archives do not contain the new privacy notice. Confirm the notice exists in all next-version installer/portable outputs and the first-run UI.
- [ ] **Qualified legal review:** for a claim of full compliance, obtain Bulgarian/EU advice on controller roles, information-society-provider requirements, consumer status and international distribution.

Reassess these documents before adding accounts, crash reports, telemetry, cloud computation, marketing, payments, or a separate website. A website would require its own actual cookie/hosting audit.

## References

- [General Legal templates (CC0)](https://github.com/General-Legal/legal-templates)
- [European Commission — GDPR information duties](https://commission.europa.eu/law/law-topic/data-protection/information-individuals_en)
- [EU e-Commerce Directive, Article 5](https://eur-lex.europa.eu/legal-content/EN/TXT/?uri=CELEX:32000L0031)
- [EU digital content contract rules](https://commission.europa.eu/topics/business-and-industry/contract-rules/digital-contracts/digital-contract-rules_en)
- [Bulgarian CPDP](https://cpdp.bg/en/)
- [GNU license FAQ](https://www.gnu.org/licenses/gpl-faq.html)
- [GitHub Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement)
