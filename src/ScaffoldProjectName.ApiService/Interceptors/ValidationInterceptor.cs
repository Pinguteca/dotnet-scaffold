using FluentValidation;
using Google.Protobuf;
using Google.Rpc;
using Grpc.Core;
using Grpc.Core.Interceptors;
using ScaffoldProjectName.ApiService.Errors;

namespace ScaffoldProjectName.ApiService.Interceptors;

/// <summary>
/// gRPC server interceptor that validates every incoming request in two
/// passes (ADR 0002):
///
/// <list type="number">
///   <item><description>
///     ProtoValidate evaluates the <c>buf.validate</c> annotations on the
///     <c>.proto</c> message. These are the cross-language field-shape
///     rules that every SDK client should mirror locally.
///   </description></item>
///   <item><description>
///     A FluentValidation <see cref="IValidator{T}"/> registered in DI
///     runs after for semantic rules outside CEL's reach (cross-aggregate
///     invariants, stateful preconditions). Methods without a registered
///     validator skip this pass.
///   </description></item>
/// </list>
///
/// Both passes translate failures to <c>INVALID_ARGUMENT</c> with a
/// <see cref="BadRequest"/> detail so Connect/gRPC clients in any
/// language receive structured per-field errors.
/// </summary>
public sealed class ValidationInterceptor(IServiceProvider services, ProtoValidate.IValidator protoValidator) : Interceptor
// FluentValidation and ProtoValidate both ship an IValidator type. The
// constructor parameter is qualified with the namespace; the local
// IValidator below resolves to FluentValidation via the using directive.
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        await ValidateAsync(request, context.CancellationToken).ConfigureAwait(false);
        return await continuation(request, context).ConfigureAwait(false);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var validating = new ValidatingStreamReader<TRequest>(requestStream, services, protoValidator, context.CancellationToken);
        return await continuation(validating, context).ConfigureAwait(false);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        await ValidateAsync(request, context.CancellationToken).ConfigureAwait(false);
        await continuation(request, responseStream, context).ConfigureAwait(false);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var validating = new ValidatingStreamReader<TRequest>(requestStream, services, protoValidator, context.CancellationToken);
        await continuation(validating, responseStream, context).ConfigureAwait(false);
    }

    private async Task ValidateAsync<T>(T request, CancellationToken ct) where T : class
    {
        RunProtoValidate(request, protoValidator);

        var fluent = services.GetService(typeof(IValidator<T>)) as IValidator<T>;
        if (fluent is null)
        {
            return;
        }

        var result = await fluent.ValidateAsync(request, ct).ConfigureAwait(false);
        if (result.IsValid)
        {
            return;
        }

        var violations = result.Errors.Select(e => new BadRequest.Types.FieldViolation
        {
            Field = e.PropertyName,
            Description = e.ErrorMessage,
        });

        throw RpcExceptions.InvalidArgument(violations);
    }

    private static void RunProtoValidate(object request, ProtoValidate.IValidator protoValidator)
    {
        if (request is not IMessage message)
        {
            return;
        }

        var violations = protoValidator.Validate(message, failFast: false);
        if (violations.Violations.Count == 0)
        {
            return;
        }

        var fieldViolations = violations.Violations.Select(v => new BadRequest.Types.FieldViolation
        {
            Field = v.Field is { } path
                ? string.Join(".", path.Elements.Select(e => e.FieldName))
                : string.Empty,
            Description = v.Message,
        });

        throw RpcExceptions.InvalidArgument(fieldViolations);
    }

    private sealed class ValidatingStreamReader<T>(IAsyncStreamReader<T> inner, IServiceProvider services, ProtoValidate.IValidator protoValidator, CancellationToken ct)
        : IAsyncStreamReader<T> where T : class
    {
        public T Current => inner.Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            var moved = await inner.MoveNext(cancellationToken).ConfigureAwait(false);
            if (!moved)
            {
                return false;
            }

            RunProtoValidate(inner.Current, protoValidator);

            if (services.GetService(typeof(IValidator<T>)) is IValidator<T> fluent)
            {
                var result = await fluent.ValidateAsync(inner.Current, ct).ConfigureAwait(false);
                if (!result.IsValid)
                {
                    var violations = result.Errors.Select(e => new BadRequest.Types.FieldViolation
                    {
                        Field = e.PropertyName,
                        Description = e.ErrorMessage,
                    });
                    throw RpcExceptions.InvalidArgument(violations);
                }
            }

            return true;
        }
    }
}
