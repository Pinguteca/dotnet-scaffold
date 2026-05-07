# ScaffoldProjectName

ScaffoldDescription

## Stack

- **.NET 10** with Aspire for distributed application orchestration
- **gRPC** server (`Grpc.AspNetCore`) with **Connect-Web** bridge (HTTP/JSON for browsers)
- **Protobuf schema** in `proto/` driven by [Buf](https://buf.build/) (lint, breaking change detection, code generation)
- **mise** for tool version management and task orchestration
- **fnox** for 12-factor secrets (OS Keychain locally, remote provider in CI/prod)
- **JReleaser** for multi-platform release automation (SBOM, Cosign, SLSA)
- **Cocogitto** for conventional commits and semantic versioning
- **prek** for pre-commit hooks (secret scanning via Kingfisher, lockfile validation, formatting)
- **Renovate** for automated dependency updates

## Project Structure

```
.
├── .github/
│   ├── chainguard/                # Octo STS identities (per-workflow OIDC tokens)
│   └── workflows/                 # CI/CD workflows
├── .mise-tasks/
│   ├── ci/                        # CI-only file-tasks (test runner, etc.)
│   └── release/                   # Release scripts (publish-all, jreleaser, container)
├── proto/
│   └── scaffoldprojectname/v1/    # Protobuf schema (versioned package)
├── src/
│   ├── ScaffoldProjectName.AppHost/           # Aspire orchestrator
│   ├── ScaffoldProjectName.ServiceDefaults/   # Telemetry, resilience, health, drain
│   └── ScaffoldProjectName.ApiService/        # gRPC server (with Connect-Web bridge)
├── Directory.Build.props          # Shared MSBuild properties
├── Directory.Build.targets        # Shared MSBuild targets
├── Directory.Packages.props       # Central NuGet version management
├── ScaffoldProjectName.slnx       # Solution file (XML format)
├── aspire.config.json             # Aspire AppHost pointer
├── buf.yaml                       # Buf workspace, lint, and breaking-change config
├── buf.gen.yaml                   # Multi-language SDK code generation config
└── fnox.toml                      # Secret references and provider config
```

## Getting Started

### Prerequisites

- [mise](https://mise.jdx.dev/) - installs every other tool automatically
- .NET 10 SDK (pinned in `global.json`, also installed by mise)
- A container runtime (Docker, Podman) for the Aspire dashboard

### Setup

```bash
# Install all tools defined in mise.toml (including buf, dotnet, fnox, etc.)
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

```bash
fnox init                        # initialize config
mise run secrets:sync            # pull remote secrets into OS Keychain
mise activate                    # subsequent activations read from keychain (offline, instant)
```

CI and production never use the keychain cache (no GUI session in headless runners). Secrets resolve from the remote provider via workload identity or OIDC.

### Common Tasks

| Task | Alias | Description |
|------|-------|-------------|
| `mise run build` | `b` | Build all projects |
| `mise run dev` | `d` | Start Aspire AppHost (local development) |
| `mise run clean` | `cl` | Clean build artifacts |
| `mise run restore` | `r` | Restore NuGet packages |
| `mise run format` | `fmt` | Format code via `.editorconfig` rules |
| `mise run format:check` | `fmtc` | Verify formatting without changes |
| `mise run proto:lint` | `pl` | Lint proto files (buf lint) |
| `mise run proto:format` | `pf` | Format proto files in place |
| `mise run proto:format-check` | `pfc` | Verify proto formatting |
| `mise run proto:breaking` | `pb` | Detect breaking proto changes vs main |
| `mise run proto:generate` | `pg` | Generate code from proto via buf.gen.yaml |
| `mise run secret:scan` | `ss` | Scan codebase for secrets with Kingfisher |
| `mise run secret:validate` | `sv` | Validate and revoke live secrets found |
| `mise run secrets:sync` | `ss-sync` | Sync remote secrets to OS Keychain |
| `mise run secrets:list` | `ss-ls` | List all resolved secrets for current profile |
| `mise run aspire:deploy` | `apd` | Deploy via Aspire |
| `mise run release:sbom` | `rsbom` | Generate CycloneDX SBOM |

## Schema and Client SDKs

The API contract lives in `proto/` as Protobuf files. Buf handles linting, breaking change detection, and multi-language code generation.

### Supported SDK languages

- **Connect-native** (HTTP/JSON, browser-friendly, idiomatic): Go, TypeScript/JavaScript, Kotlin
- **gRPC standard** (protoc-gen plugins): .NET (C#), Java, Python, Rust, Dart

### Local generation

```bash
mise run proto:generate          # generates all 8 languages into out/sdk/<lang>/
```

Output structure:

```
out/sdk/
├── go/{proto,connect}/
├── typescript/
├── kotlin/
├── csharp/{proto,grpc}/
├── java/{proto,grpc}/
├── python/{proto,grpc}/
├── rust/{proto,grpc}/
└── dart/
```

The set of plugins is defined in `buf.gen.yaml`. Add a `name:` to `buf.yaml` to publish the schema to the [Buf Schema Registry (BSR)](https://buf.build/) for hosted plugin caching and module sharing. Public BSR modules are free, private modules require a paid plan.

### CI generation and publishing

`.github/workflows/sdk-generate.yml` runs on release tags (`v*`) and uploads the generated SDK set as a workflow artifact. Per-language registry publishing (npm, PyPI, NuGet, crates.io, Maven Central, pub.dev) is left as a follow-up: each ecosystem needs its own trust boundary and signing flow.

## Development

Start the Aspire dashboard:

```bash
mise run dev
```

The dashboard runs at `https://localhost:17000`. The ApiService is exposed through service discovery.

### Calling the gRPC server

In Development, the server has gRPC reflection enabled, so any reflection-aware tool works without checked-out protos:

```bash
# List services
grpcurl -plaintext localhost:5301 list

# Describe a method
grpcurl -plaintext localhost:5301 describe scaffoldprojectname.v1.EchoService

# Call Echo
grpcurl -plaintext -d '{"message":"hi"}' localhost:5301 scaffoldprojectname.v1.EchoService/Echo

# Or via Connect protocol over plain HTTP/JSON
curl -X POST http://localhost:5301/scaffoldprojectname.v1.EchoService/Echo \
  -H 'Content-Type: application/json' \
  -d '{"message":"hi"}'
```

Reflection is disabled in non-Development environments to reduce attack surface and avoid runtime cost.

### Request validation and typed errors

Server-side validation runs through a gRPC interceptor (`Interceptors/ValidationInterceptor.cs`) that resolves a [FluentValidation](https://fluentvalidation.net/) `IValidator<TRequest>` from DI for every incoming request type. Failures are mapped to `INVALID_ARGUMENT` with a `google.rpc.BadRequest` detail listing every field violation, so Connect/gRPC clients in any language receive structured per-field errors.

Add a validator per request type under `Validation/`:

```csharp
public sealed class CreateThingRequestValidator : AbstractValidator<CreateThingRequest>
{
    public CreateThingRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
```

For non-validation errors, use the `RpcExceptions` helpers (`Errors/RpcExceptions.cs`). They emit `google.rpc.Status` carrying the standard detail type for each error class (`ResourceInfo`, `ErrorInfo`, `PreconditionFailure`, `QuotaFailure`, `RetryInfo`):

```csharp
throw RpcExceptions.NotFound("thing", id);
throw RpcExceptions.PermissionDenied(reason: "missing_scope", domain: "scaffoldprojectname");
throw RpcExceptions.ResourceExhausted(violations, retryAfter: TimeSpan.FromSeconds(30));
```

## Health and Graceful Shutdown

The service exposes Kubernetes-friendly probe endpoints:

- `GET /health/live` - liveness (process alive, only fails on deadlock)
- `GET /health/ready` - readiness (accepting new traffic; flips to draining on SIGTERM)

On `SIGTERM` the readiness probe fails immediately so K8s removes the pod from the Service endpoints, then in-flight RPCs are allowed to finish within `HostOptions.ShutdownTimeout` (25 seconds, kept under typical `terminationGracePeriodSeconds`).

## Release

Releases are driven by git tags matching `v*` and orchestrated via JReleaser:

```bash
RELEASE_VERSION=1.0.0 mise run release:publish-all
RELEASE_VERSION=1.0.0 mise run release:jreleaser
```

Release artifacts include:

- Multi-platform archives (linux-x64/arm64, osx-x64/arm64, win-x64/arm64)
- CycloneDX and SPDX SBOMs
- Cosign keyless signatures (via Sigstore)
- SLSA provenance attestations
- SWID tags (ISO/IEC 19770-2:2015)
- SHA-256 and SHA-512 checksums

## CI/CD

All workflows use [Octo STS](https://github.com/octo-sts) for short-lived OIDC tokens instead of long-lived `GITHUB_TOKEN`. Identity files live in `.github/chainguard/` - one per workflow.

| Workflow | Purpose |
|----------|---------|
| `build.yml` | Restore (locked mode), format check, build, test |
| `proto.yml` | buf lint, format check, breaking change detection on PR |
| `sdk-generate.yml` | Generate multi-language SDKs on release tag |
| `lint-gha.yml` | Lint GitHub Actions (Zizmor, Actionlint, Pinact) |
| `secret-scan.yml` | Kingfisher secret detection and live revocation |
| `verify-gha-integrity.yml` | Verify pinned Action SHAs against lockfile |
| `gitsign-verify.yml` | Verify commit signatures (Sigstore) for every commit in PR |
| `img-optimization.yml` | Auto-optimize images in PRs |
| `svg-optimization.yml` | Auto-optimize SVGs with OIDC-signed commits |
| `scorecard.yml` | OpenSSF Scorecard (public repos only) |

## Security

- **Central package management** - all versions pinned in `Directory.Packages.props`
- **NuGet lockfiles** - committed, verified in CI with `--locked-mode`
- **Buf breaking change detection** - schema diffs against main on every PR
- **Secret scanning** - Kingfisher on pre-commit and in CI (with live revocation)
- **GHA pinning** - all Actions pinned to SHAs, enforced by Pinact
- **StepSecurity harden-runner** - egress auditing (configurable, free for public repos)
- **OIDC-based auth** - no long-lived tokens in CI; per-workflow Octo STS identities
- **gRPC reflection** - dev-only, never exposed in production

## Contributing

1. Fork and branch from `main`
2. Follow [Conventional Commits](https://www.conventionalcommits.org/) (enforced by Cocogitto)
3. Ensure `mise run format`, `mise run build`, `mise run proto:lint` pass locally
4. Submit PR; CI will verify secrets, lockfiles, commit messages, schema, and build

## License

See [LICENSE](./LICENSE). ScaffoldVendor holds copyright.
