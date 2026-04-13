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

### Secrets Management

Secrets are managed via [fnox](https://fnox.jdx.dev/) integrated with mise. The setup follows a 2-tier architecture aligned with 12-factor:

1. **Remote source of truth**: Azure Key Vault, 1Password, Bitwarden Secrets Manager, AWS Secrets Manager, or HashiCorp Vault. Configured in `fnox.toml` under `[providers]`. Required for CI and production: all reads go directly against the remote, no local cache or encrypted fallback.
2. **Local cache (OS Keychain, dev only)**: macOS Keychain, Windows Credential Manager, or Linux Secret Service. Populated via `fnox sync` to eliminate remote calls on every shell activation during development.

**First-time setup (local dev):**

```bash
fnox init                        # initialize config
mise run secrets:sync            # pull remote secrets into OS Keychain
mise activate                    # subsequent activations read from keychain (offline, instant)
```

**Add or update secrets:**

```bash
fnox set DATABASE_URL <value>              # stored in default provider
fnox set API_KEY <value> --profile production
mise run secrets:refresh                   # re-sync keychain from remote
```

**Run commands with secrets loaded:**

```bash
fnox exec -- dotnet run
fnox exec --profile production -- dotnet run
```

**CI/production:** No keychain (headless runners). Secrets read directly from the remote provider via workload identity or OIDC, configured in the `ci` and `production` profiles.

### Common Tasks

| Task | Alias | Description |
|------|-------|-------------|
| `mise run build` | `b` | Build all projects |
| `mise run dev` | `d` | Start Aspire AppHost (local development) |
| `mise run clean` | `cl` | Clean build artifacts |
| `mise run restore` | `r` | Restore NuGet packages |
| `mise run format` | `fmt` | Format code via `.editorconfig` rules |
| `mise run format:check` | `fmtc` | Verify formatting without changes |
| `mise run secret:scan` | `ss` | Scan codebase for secrets with Kingfisher |
| `mise run secret:validate` | `sv` | Validate & revoke live secrets found |
| `mise run secrets:sync` | `ss-sync` | Sync remote secrets to OS Keychain |
| `mise run secrets:sync-dry` | `ss-dry` | Preview secrets that would sync |
| `mise run secrets:refresh` | `ss-refresh` | Force refresh OS Keychain from remote |
| `mise run secrets:list` | `ss-ls` | List all resolved secrets for current profile |
| `mise run openapi:generate` | `oag` | Generate OpenAPI spec from ApiService |
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
