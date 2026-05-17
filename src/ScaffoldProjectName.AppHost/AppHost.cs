var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.GeneratedClassNamePrefix_ApiService>("apiservice")
    .WithHttpHealthCheck("/health/live");

// SampleClient demonstrates how a consumer wires the Pinguteca SDK
// against this service. IdP URL and client credentials are supplied
// via configuration (env, user secrets, secret manager); plug your
// real IdP in by setting Sdk__IdpTokenUrl, Sdk__IdpClientId, and
// Sdk__IdpClientSecret at deploy time.
builder.AddProject<Projects.GeneratedClassNamePrefix_SampleClient>("sampleclient")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("Sdk__ApiUrl", apiService.GetEndpoint("https"));

// k6 load test runner. Opt-in via the EnableK6 config key (set on
// appsettings, env, or user secrets). It bind-mounts the host's
// /k6 and /proto trees into the container, which means the
// container runtime needs file-sharing access to wherever the
// repo lives. Docker Desktop: Settings -> Resources -> File
// Sharing. Podman: re-init the machine with `--volume <root>:<root>`.
//
// Off by default so a fresh consumer who has not configured their
// container runtime can still run the AppHost. Run loadtests
// standalone via `mise run loadtest:load` either way.
if (builder.Configuration.GetValue<bool>("EnableK6"))
{
    var k6Scripts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "k6"));
    var protoDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "proto"));

    builder.AddK6("k6")
        .WithBindMount(k6Scripts, "/scripts", isReadOnly: true)
        .WithBindMount(protoDir, "/proto", isReadOnly: true)
        .WithScript("/scripts/echo.js")
        .WithReference(apiService)
        .WaitFor(apiService)
        .WithEnvironment("API_URL", apiService.GetEndpoint("https"));
}

builder.Build().Run();
