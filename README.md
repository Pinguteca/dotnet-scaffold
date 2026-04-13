# ScaffoldProjectName

ScaffoldDescription

## Stack

- **.NET 10** with Aspire for distributed application orchestration
- **ASP.NET Core Minimal API** with OpenAPI support
- **mise** for tool version management and task orchestration
- **JReleaser** for multi-platform release automation (SBOM, Cosign, SLSA)
- **Cocogitto** for conventional commits and semantic versioning
- **prek** for pre-commit hooks (secret scanning via Kingfisher, lockfile validation, formatting)
- **Renovate** for automated dependency updates

## Project Structure

```
├── .github/
│   ├── chainguard/           # Octo STS identities (per-workflow OIDC tokens)
│   └── workflows/            # CI/CD workflows
├── .mise-tasks/
│   └── release/              # C# release scripts (publish-all, jreleaser)
├── src/
│   ├── ScaffoldProjectName.AppHost/           # Aspire orchestrator
│   ├── ScaffoldProjectName.ServiceDefaults/   # Shared telemetry, resilience, health checks
│   └── ScaffoldProjectName.ApiService/        # ASP.NET Core Minimal API
├── Directory.Build.props         # Shared MSBuild properties
├── Directory.Build.targets       # Shared MSBuild targets
├── Directory.Packages.props      # Central NuGet version management
├── ScaffoldProjectName.slnx      # Solution file (XML format)
└── aspire.config.json            # Aspire AppHost pointer
```

## Getting Started

### Prerequisites

- [mise](https://mise.jdx.dev/) - installs all other tools automatically
- .NET 10 SDK (pinned in `global.json`)
- A container runtime (Docker, Podman) for Aspire dashboard

### Setup

```bash
# Install all tools defined in mise.toml
mise install

# Install pre-commit hooks
prek install

# Restore packages (generates packages.lock.json)
mise run restore
```

### Common Tasks

| Task | Alias | Description |
|------|-------|-------------|
| `mise run build` | `b` | Build all projects |
| `mise run dev` | `d` | Start Aspire AppHost (local development) |
| `mise run clean` | `cl` | Clean build artifacts |
| `mise run restore` | `r` | Restore NuGet packages |
| `mise run format` | `fmt` | Format code via `.editorconfig` rules |
| `mise run format:check` | `fmtc` | Verify formatting without changes |
| `mise run secret:scan` | `ss` | Scan for secrets with Kingfisher |
| `mise run secret:validate` | `sv` | Validate & revoke live secrets |
| `mise run aspire:deploy` | `apd` | Deploy via Aspire |
| `mise run release:sbom` | `rsbom` | Generate CycloneDX SBOM |

## Development

Start the Aspire dashboard:

```bash
mise run dev
```

The dashboard runs at `https://localhost:17000` with the ApiService exposed through service discovery.

## Release

Releases are driven by git tags matching `v*` and orchestrated via JReleaser:

```bash
# Multi-platform binary publish
RELEASE_VERSION=1.0.0 mise run release:publish-all

# Full release (publish + SBOM + sign + GitHub release)
RELEASE_VERSION=1.0.0 mise run release:jreleaser
```

Release artifacts include:
- Multi-platform archives (linux-x64/arm64, osx-x64/arm64, win-x64/arm64)
- CycloneDX + SPDX SBOMs
- Cosign keyless signatures (via Sigstore)
- SLSA provenance attestations
- SWID tags (ISO/IEC 19770-2:2015)
- SHA-256 + SHA-512 checksums

## CI/CD

All workflows use [Octo STS](https://github.com/octo-sts) for short-lived OIDC tokens instead of long-lived `GITHUB_TOKEN`. Identity files are in `.github/chainguard/` - one per workflow.

| Workflow | Purpose |
|----------|---------|
| `lint-gha.yml` | Lint GitHub Actions (Zizmor, Actionlint, Pinact) |
| `secret-scan.yml` | Kingfisher secret detection & live revocation |
| `verify-gha-integrity.yml` | Verify pinned Action SHAs against lockfile |
| `gitsign-verify.yml` | Verify commit signatures (Sigstore) |
| `img-optimization.yml` | Auto-optimize images in PRs |
| `svg-optimization.yml` | Auto-optimize SVGs with OIDC-signed commits |
| `scorecard.yml` | OpenSSF Scorecard (public repos only) |

## Security

- **Central package management** - all versions pinned in `Directory.Packages.props`
- **NuGet lockfiles** - committed, verified in CI with `--locked-mode`
- **Secret scanning** - Kingfisher runs on pre-commit and in CI (with live validation)
- **GHA pinning** - all Actions pinned to SHAs, enforced by Pinact
- **StepSecurity harden-runner** - egress auditing (configurable, free for public repos)
- **OIDC-based auth** - no long-lived tokens in CI

## Contributing

1. Fork and branch from `main`
2. Follow [Conventional Commits](https://www.conventionalcommits.org/) (enforced by Cocogitto)
3. Ensure `mise run format` and `mise run build` pass locally
4. Submit PR - CI will verify secrets, lockfiles, commit messages, and build

## License

See [LICENSE](./LICENSE) - ScaffoldVendor holds copyright.
