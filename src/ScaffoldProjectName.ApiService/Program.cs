using FluentValidation;
using Microsoft.AspNetCore.StaticFiles;
using ProtoValidate;
using Scalar.AspNetCore;
using ScaffoldProjectName.ApiService.Interceptors;
using ScaffoldProjectName.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ProtoValidate evaluates buf.validate annotations on every incoming
// proto message (ADR 0002). DisableLazy=false so unknown message types
// fall back to descriptor discovery instead of throwing.
builder.Services.AddProtoValidate();

// FluentValidation covers semantic rules outside CEL's reach (see ADR 0002).
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

// Serve the buf-generated OpenAPI YAML out of wwwroot. .yaml is not
// a default static-file MIME type, so register it explicitly.
var yamlProvider = new FileExtensionContentTypeProvider();
yamlProvider.Mappings[".yaml"] = "application/yaml";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = yamlProvider });

// Scalar API reference at /docs, reading the buf-generated YAML from
// wwwroot/openapi/. Regenerate via `mise run proto:generate` after
// any .proto change.
app.MapScalarApiReference("/docs", options =>
{
    options.WithTitle("ScaffoldProjectName API")
           .WithOpenApiRoutePattern("/openapi/scaffoldprojectname/v1/echo.openapi.yaml");
});

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
