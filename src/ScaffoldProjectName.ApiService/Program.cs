using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using ScaffoldProjectName.ApiService.Interceptors;
using ScaffoldProjectName.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ValidationInterceptor>();
});
// ServiceDefaults.AddDefaultHealthChecks already registers a "self"
// liveness check and a readiness check against the shared
// IHealthChecksBuilder. AddGrpcHealthChecks exposes those over the
// grpc.health.v1.Health service; piling another "self" check on
// here would collide on the registry name.
builder.Services.AddGrpcHealthChecks();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddGrpcReflection();
}

var app = builder.Build();

// Connect-Web bridge so browser and Connect clients can speak HTTP/1.1 + JSON to the gRPC server.
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.MapGrpcService<EchoService>();
app.MapGrpcHealthChecksService();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

// Kubernetes liveness vs readiness probe endpoints (split is configured in ServiceDefaults).
app.MapDefaultEndpoints();

app.Run();
