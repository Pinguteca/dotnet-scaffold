using FluentValidation;
using ScaffoldProjectName.V1;

namespace ScaffoldProjectName.ApiService.Validation;

/// <summary>
/// Reference validator. Field-shape rules (min length, regex, etc.) belong
/// in <c>echo.proto</c> as <c>buf.validate</c> annotations so every SDK
/// client mirrors them. Use this <see cref="AbstractValidator{T}"/> only
/// for semantic rules outside CEL: cross-aggregate invariants, database
/// lookups, anything stateful. One validator per request type; the
/// ValidationInterceptor resolves them from DI automatically.
/// </summary>
public sealed class EchoRequestValidator : AbstractValidator<EchoRequest>
{
    public EchoRequestValidator()
    {
        // No semantic rules yet. Field-shape constraints live in the
        // .proto annotation on EchoRequest.message.
    }
}
