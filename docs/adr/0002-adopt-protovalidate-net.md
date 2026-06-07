# 2. Adopt telus-labs/protovalidate-net for field-shape validation

Date: 2026-06-08
Status: Accepted
Supersedes: [0001-validation-strategy.md](0001-validation-strategy.md)

## Context

ADR 0001 rejected `buf.validate` because no official `protovalidate-net`
runtime existed and the only port was a single-maintainer community
project. Eight months on, two facts changed the calculus:

- The .NET stack is confirmed as the primary SDK consumer for this
  scaffold. The cost of maintaining two parallel rule sets (`.proto`
  field annotations for cross-language clients vs. C# `IValidator`
  classes for the server) is paid on every PR that touches a field.
- `telus-labs/protovalidate-net` is live (Apache-2.0, latest push
  2025-10-10, not archived). Small (~14 stars) but maintained. The
  failure mode if it stops being maintained is bounded: revert to
  FluentValidation; the `.proto` annotations stay valid for the
  other SDKs.

## Decision

Field-shape rules live in `.proto` as `buf.validate` annotations.
`telus-labs/protovalidate-net` evaluates them server-side inside a
`ValidationInterceptor` in the gRPC pipeline. The same CEL rules
that the Go and TypeScript SDKs enforce client-side execute on
the .NET server, giving a single source of truth across every
language client this scaffold produces.

FluentValidation is kept ONLY for semantic rules that cannot be
expressed in CEL: cross-aggregate invariants, database-touching
preconditions, anything stateful. Pure shape rules (`min_len`,
`gte`, `email`, `uuid`, regex) MUST live in `.proto`.

Validation failures still map to `google.rpc.Status` with a
`BadRequest` detail (one `FieldViolation` per error) so the wire
shape stays compatible with what ADR 0001 established. Connect
and gRPC clients in any language receive the same structured
errors regardless of which validator produced them.

## Consequences

Pros:

- One source of truth for field shapes across .NET, Go, TS, Dart,
  Kotlin, Java, Python, Rust.
- Clients enforce the same rules locally before sending, reducing
  invalid round-trips.
- Rule changes happen in `.proto`; codegen propagates to every
  client without manual sync.

Cons:

- Dependency on a community-maintained .NET runtime instead of
  an official bufbuild package.
- Two-tier validation pipeline (protovalidate first, then
  FluentValidation) is more moving parts than one.

## Revisit when

- bufbuild ships an official `protovalidate-net`
  ([bufbuild/protovalidate#70](https://github.com/bufbuild/protovalidate/issues/70)).
  Migration is a package-id swap plus a re-pin in
  `Directory.Packages.props`; `.proto` annotations stay unchanged.
- Or `telus-labs/protovalidate-net` goes silent for an extended
  window: fall back to ADR 0001's FluentValidation-only strategy
  for shape rules until the official runtime ships.
