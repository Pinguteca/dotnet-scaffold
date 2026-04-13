var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.GeneratedClassNamePrefix_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
