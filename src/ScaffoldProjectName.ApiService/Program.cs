using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("ScaffoldProjectName API");
        options.WithTheme(ScalarTheme.BluePlanet);
    });
}

app.MapGet("/", () => "API service is running.");

app.MapDefaultEndpoints();

app.Run();
