# 1. Server-side validation uses FluentValidation, not `buf.validate`

Date: 2026-05-08
Status: Accepted

## Context

The scaffold ships with Protobuf as the schema source of truth and generates Connect/gRPC SDKs in 8 languages. The natural pairing for schema-driven validation is [`buf.validate`](https://github.com/bufbuild/protovalidate), which expresses constraints as proto annotations and ships matching runtimes per language.

Bufbuild officially maintains `protovalidate` runtimes for **Go, Java, Python, JavaScript/TypeScript, and C++**. As of 2026-05 there is no official .NET runtime ([bufbuild/protovalidate#70](https://github.com/bufbuild/protovalidate/issues/70) is open). The only existing port is `telus-oss/protovalidate-net` (single-maintainer, last commit 2025-10-10).

The primary SDK consumer for this scaffold is .NET (European teams). Adopting `buf.validate` would force the most-used SDK onto a stale, single-maintainer dependency on both server and client side.

## Decision

Validate requests server-side with [FluentValidation](https://fluentvalidation.net/). Map failures to `google.rpc.Status` with a `BadRequest` detail (one `FieldViolation` per error) so Connect/gRPC clients in any language receive structured per-field errors. Use the typed-error helpers in `Errors/RpcExceptions.cs` for non-validation domain errors.

The proto schema remains the **structural / wire** contract. FluentValidation is the **semantic** contract.

## Consequences

Pros:
- .NET-idiomatic, fully maintained, MS-aligned dependency stack.
- Validators sit next to the service code that consumes them; refactor-friendly.
- Connect/gRPC clients still receive Protobuf-typed error details; no language-side regression.

Cons:
- Two places hold semantic rules: `proto/*.proto` for the wire contract, `*ApiService/Validation/*Validator.cs` for runtime rules. PRs that change a field's semantics must touch both.
- No automatic rule propagation to clients. Client SDKs do not know the rules in advance and must round-trip to discover them. Acceptable: validation errors are cheap and the server is the trust boundary anyway.

## Revisit when

- Bufbuild ships an official `protovalidate-net` (track [bufbuild/protovalidate#70](https://github.com/bufbuild/protovalidate/issues/70)). At that point: migrate rules into `.proto` annotations, replace `ValidationInterceptor` with the official protovalidate interceptor, delete the FluentValidation packages, and have client SDKs validate locally before sending.
- Or: another schema-driven validator with a maintained .NET runtime emerges (CEL-on-Protobuf, JSON Schema bridge, etc.).
