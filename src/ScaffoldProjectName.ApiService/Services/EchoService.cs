using Grpc.Core;
using ScaffoldProjectName.V1;

namespace ScaffoldProjectName.ApiService.Services;

/// <summary>
/// Echo service. Returns the request message verbatim. Replace with real service logic.
/// </summary>
public sealed class EchoService(ILogger<EchoService> logger) : ScaffoldProjectName.V1.EchoService.EchoServiceBase
{
    public override Task<EchoResponse> Echo(EchoRequest request, ServerCallContext context)
    {
        logger.LogInformation("Echo request received with message length {Length}", request.Message.Length);
        return Task.FromResult(new EchoResponse { Message = request.Message });
    }
}
