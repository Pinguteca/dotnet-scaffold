using FluentValidation;
using Google.Rpc;
using Grpc.Core;
using Grpc.Core.Interceptors;
using ScaffoldProjectName.ApiService.Errors;

namespace ScaffoldProjectName.ApiService.Interceptors;

/// <summary>
/// gRPC server interceptor that runs the FluentValidation <see cref="IValidator{T}"/> registered for
/// each request type before invoking the service method. Throws <c>INVALID_ARGUMENT</c> with a
/// <see cref="BadRequest"/> detail when validation fails so Connect/gRPC clients receive structured
/// per-field errors. Methods without a registered validator pass through unchanged.
/// </summary>
public sealed class ValidationInterceptor(IServiceProvider services) : Interceptor
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
        var validating = new ValidatingStreamReader<TRequest>(requestStream, services, context.CancellationToken);
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
        var validating = new ValidatingStreamReader<TRequest>(requestStream, services, context.CancellationToken);
        await continuation(validating, responseStream, context).ConfigureAwait(false);
    }

    private async Task ValidateAsync<T>(T request, CancellationToken ct) where T : class
    {
        var validator = services.GetService(typeof(IValidator<T>)) as IValidator<T>;
        if (validator is null)
        {
            return;
        }

        var result = await validator.ValidateAsync(request, ct).ConfigureAwait(false);
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

    private sealed class ValidatingStreamReader<T>(IAsyncStreamReader<T> inner, IServiceProvider services, CancellationToken ct)
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

            if (services.GetService(typeof(IValidator<T>)) is IValidator<T> validator)
            {
                var result = await validator.ValidateAsync(inner.Current, ct).ConfigureAwait(false);
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
