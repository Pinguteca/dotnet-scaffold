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

builder.Build().Run();
