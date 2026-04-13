# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in ScaffoldProjectName, please report it privately via one of these channels:

- [GitHub Security Advisories](https://github.com/ScaffoldOwner/ScaffoldTemplate/security/advisories/new) (preferred)
- Email the maintainers directly

Please do NOT open a public issue for security vulnerabilities.

## What to Include

- A clear description of the vulnerability
- Steps to reproduce
- Affected versions
- Potential impact
- Suggested fix (if any)

## Response Time

The maintainers aim to:

- Acknowledge receipt within 72 hours
- Provide an initial assessment within 7 days
- Coordinate disclosure once a fix is available

## Supply Chain Security

This project follows supply chain best practices:

- All GitHub Actions pinned to SHAs (enforced by Pinact)
- Container images signed via Cosign (keyless Sigstore)
- SBOMs published with every release (CycloneDX + SPDX)
- SLSA Level 3 provenance attestations
- NuGet packages with lockfile verification
- Pre-commit and CI secret scanning via Kingfisher
- Commits signed via Gitsign (Sigstore)
