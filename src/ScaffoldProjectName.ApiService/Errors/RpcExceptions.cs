using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;

namespace ScaffoldProjectName.ApiService.Errors;

/// <summary>
/// Builds <see cref="RpcException"/>s carrying a <c>google.rpc.Status</c> payload with standard
/// detail types (BadRequest, ErrorInfo, ResourceInfo, etc.). Connect-Web and gRPC clients in every
/// supported SDK language decode these into typed errors.
/// <para>
/// The <see cref="Code"/> values map 1:1 to Connect error codes, so a TypeScript or Go Connect
/// client receives <c>ConnectError</c> with the matching code and structured details.
/// </para>
/// </summary>
public static class RpcExceptions
{
    /// <summary>
    /// INVALID_ARGUMENT with a <see cref="BadRequest"/> detail listing every field violation.
    /// Use for request validation failures.
    /// </summary>
    public static RpcException InvalidArgument(IEnumerable<BadRequest.Types.FieldViolation> violations, string? message = null)
    {
        var bad = new BadRequest();
        bad.FieldViolations.AddRange(violations);
        return Status(Code.InvalidArgument, message ?? "Request failed validation.", bad);
    }

    /// <summary>NOT_FOUND with <see cref="ResourceInfo"/>.</summary>
    public static RpcException NotFound(string resourceType, string resourceName, string? description = null) =>
        Status(Code.NotFound, description ?? $"{resourceType} '{resourceName}' was not found.", new ResourceInfo
        {
            ResourceType = resourceType,
            ResourceName = resourceName,
            Description = description ?? string.Empty,
        });

    /// <summary>ALREADY_EXISTS with <see cref="ResourceInfo"/>.</summary>
    public static RpcException AlreadyExists(string resourceType, string resourceName, string? description = null) =>
        Status(Code.AlreadyExists, description ?? $"{resourceType} '{resourceName}' already exists.", new ResourceInfo
        {
            ResourceType = resourceType,
            ResourceName = resourceName,
            Description = description ?? string.Empty,
        });

    /// <summary>PERMISSION_DENIED with <see cref="ErrorInfo"/> (reason + domain for client routing).</summary>
    public static RpcException PermissionDenied(string reason, string domain, IDictionary<string, string>? metadata = null) =>
        ErrorInfoStatus(Code.PermissionDenied, "Permission denied.", reason, domain, metadata);

    /// <summary>UNAUTHENTICATED with <see cref="ErrorInfo"/>.</summary>
    public static RpcException Unauthenticated(string reason, string domain, IDictionary<string, string>? metadata = null) =>
        ErrorInfoStatus(Code.Unauthenticated, "Authentication required.", reason, domain, metadata);

    /// <summary>FAILED_PRECONDITION with <see cref="PreconditionFailure"/>.</summary>
    public static RpcException FailedPrecondition(IEnumerable<PreconditionFailure.Types.Violation> violations, string? message = null)
    {
        var fail = new PreconditionFailure();
        fail.Violations.AddRange(violations);
        return Status(Code.FailedPrecondition, message ?? "Precondition failed.", fail);
    }

    /// <summary>RESOURCE_EXHAUSTED with <see cref="QuotaFailure"/> and optional <see cref="RetryInfo"/>.</summary>
    public static RpcException ResourceExhausted(IEnumerable<QuotaFailure.Types.Violation> violations, TimeSpan? retryAfter = null, string? message = null)
    {
        var quota = new QuotaFailure();
        quota.Violations.AddRange(violations);

        var details = new List<IMessage> { quota };
        if (retryAfter is { } delay)
        {
            details.Add(new RetryInfo { RetryDelay = Duration.FromTimeSpan(delay) });
        }
        return Status(Code.ResourceExhausted, message ?? "Quota exceeded.", details);
    }

    /// <summary>UNAVAILABLE with <see cref="RetryInfo"/>; clients should retry after the given delay.</summary>
    public static RpcException Unavailable(TimeSpan retryAfter, string? message = null) =>
        Status(Code.Unavailable, message ?? "Service temporarily unavailable.", new RetryInfo { RetryDelay = Duration.FromTimeSpan(retryAfter) });

    /// <summary>INTERNAL with no details. Use for unexpected server-side errors; never leak internals.</summary>
    public static RpcException Internal(string? message = null) =>
        new(new global::Grpc.Core.Status(StatusCode.Internal, message ?? "Internal server error."));

    private static RpcException ErrorInfoStatus(Code code, string message, string reason, string domain, IDictionary<string, string>? metadata)
    {
        var info = new ErrorInfo { Reason = reason, Domain = domain };
        if (metadata is not null)
        {
            foreach (var (k, v) in metadata)
            {
                info.Metadata.Add(k, v);
            }
        }
        return Status(code, message, info);
    }

    private static RpcException Status(Code code, string message, IMessage detail) =>
        Status(code, message, [detail]);

    private static RpcException Status(Code code, string message, IEnumerable<IMessage> details)
    {
        var status = new global::Google.Rpc.Status { Code = (int)code, Message = message };
        foreach (var detail in details)
        {
            status.Details.Add(Any.Pack(detail));
        }
        return status.ToRpcException();
    }
}
