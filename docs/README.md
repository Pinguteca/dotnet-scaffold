# Scaffold design docs

Documentation about **the scaffold itself**, not the projects it generates.

This directory is excluded from `dotnet new` output (see `.template.config/template.json`), so nothing under `docs/` ships into instantiated repos.

## Contents

- `adr/` — Architecture Decision Records for scaffold-level choices. Each ADR records the trade-off behind a default that consumers inherit by using this template.

## When to add an ADR

Add one when a decision shapes how *every* generated repo behaves and the rationale is non-obvious from reading the code. Examples:

- Choice of validation library, error model, telemetry stack, release tooling.
- Why a tool was rejected (so a future contributor doesn't relitigate).
- Conditional features behind template parameters (why the parameter exists, what it gates).

Don't ADR routine implementation details, dependency bumps, or CI tweaks.
