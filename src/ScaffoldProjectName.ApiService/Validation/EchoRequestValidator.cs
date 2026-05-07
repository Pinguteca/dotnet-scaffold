using FluentValidation;
using ScaffoldProjectName.V1;

namespace ScaffoldProjectName.ApiService.Validation;

/// <summary>
/// Reference validator. Replace with real rules as the schema grows. One <see cref="AbstractValidator{T}"/>
/// per request type; the ValidationInterceptor resolves them from DI automatically.
/// </summary>
public sealed class EchoRequestValidator : AbstractValidator<EchoRequest>
{
    public EchoRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("message must not be empty.")
            .MaximumLength(1024).WithMessage("message must be at most 1024 characters.");
    }
}
