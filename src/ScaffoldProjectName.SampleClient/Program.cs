using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pinguteca.Sdk.Core.Auth;
using Pinguteca.Sdk.Core.Presets;
using ScaffoldProjectName.V1;

var builder = Host.CreateApplicationBuilder(args);

// Pulls in OTel tracing / metrics, service discovery, and resilient HTTP
// defaults. The Pinguteca OtelInterceptor publishes spans through the
// "Pinguteca.Sdk.Core" ActivitySource which the OTel SDK picks up
// automatically once AddServiceDefaults has wired the listeners.
builder.AddServiceDefaults();

builder.Services
    .AddOptions<SampleClientOptions>()
    .Bind(builder.Configuration.GetSection("Sdk"))
    .ValidateDataAnnotations();

builder.Services.AddHostedService<EchoSmokeWorker>();

await builder.Build().RunAsync();

internal sealed class SampleClientOptions
{
    public required Uri ApiUrl { get; set; }
    public required Uri IdpTokenUrl { get; set; }
    public required string IdpClientId { get; set; }
    public required string IdpClientSecret { get; set; }
}

/// <summary>
/// One-shot smoke test: obtain a token, build the full Pinguteca SDK chain
/// via <see cref="Presets.Standalone"/>, call EchoService.Echo, and exit.
/// Aspire orchestrates this as a short-lived workload alongside the API.
/// </summary>
internal sealed class EchoSmokeWorker(
    IOptions<SampleClientOptions> options,
    IHostApplicationLifetime lifetime,
    ILogger<EchoSmokeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        using var tokenSource = new ClientCredentialsTokenSource(new ClientCredentialsOptions
        {
            TokenUrl = opts.IdpTokenUrl,
            ClientId = opts.IdpClientId,
            ClientSecret = opts.IdpClientSecret,
        });

        var presetOptions = new PresetOptions
        {
            Auth = new AuthOptions { Source = tokenSource },
        };
        var chain = Presets.Standalone(presetOptions);

        using var channel = GrpcChannel.ForAddress(opts.ApiUrl);
        var invoker = channel.CreateCallInvoker().Intercept(chain);
        var client = new EchoService.EchoServiceClient(invoker);

        // Loop instead of exit-on-first-call: Aspire treats project
        // resources as long-running services and flags short-lived
        // processes as "Failed to start". A polling demo also surfaces
        // OTel spans, breaker state, and retry behaviour over time.
        var interval = TimeSpan.FromSeconds(5);
        var iteration = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            iteration++;
            try
            {
                var reply = await client.EchoAsync(
                    new EchoRequest { Message = $"hello from sample client #{iteration}" },
                    cancellationToken: stoppingToken);
                logger.LogInformation("Echo replied: {Message}", reply.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Echo call failed on iteration {Iteration}", iteration);
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        _ = lifetime; // keep the dependency injection happy when the loop is the lifecycle
    }
}
