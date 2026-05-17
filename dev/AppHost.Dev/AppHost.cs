// Scaffold-only dev orchestrator: FakeIdp + ApiService + SampleClient.
// Consumers run src/ScaffoldProjectName.AppHost which only knows about
// the real workloads; the IdP URL there is supplied via configuration.

var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.FakeIdp>("idp");

var apiService = builder.AddProject<Projects.ScaffoldProjectName_ApiService>("apiservice")
    .WithHttpHealthCheck("/health/live");

builder.AddProject<Projects.ScaffoldProjectName_SampleClient>("sampleclient")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(idp)
    .WaitFor(idp)
    .WithEnvironment("Sdk__ApiUrl", apiService.GetEndpoint("https"))
    .WithEnvironment("Sdk__IdpTokenUrl", ReferenceExpression.Create($"{idp.GetEndpoint("https")}/token"))
    .WithEnvironment("Sdk__IdpClientId", "sample-client")
    .WithEnvironment("Sdk__IdpClientSecret", "fake-secret");

// k6 against the dev apiservice. Scripts mounted from the repo's
// /k6 directory so edits round-trip without rebuilding the image.
var k6Scripts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "k6"));
var protoDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "proto"));

builder.AddK6("k6")
    .WithBindMount(k6Scripts, "/scripts", isReadOnly: true)
    .WithBindMount(protoDir, "/proto", isReadOnly: true)
    .WithScript("/scripts/echo.js")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("API_URL", apiService.GetEndpoint("https"));

builder.Build().Run();
