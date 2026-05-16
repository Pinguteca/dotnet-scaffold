using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// Minimal OAuth2 token endpoint that hands out an opaque access_token
// for any client_credentials request. Intended for local scaffold smoke
// tests only; never reaches the templated output (see template.json
// exclude list).

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/token", (
    HttpRequest req,
    [FromForm(Name = "grant_type")] string? grantType,
    [FromForm(Name = "client_id")] string? clientId,
    [FromForm(Name = "client_secret")] string? clientSecret,
    [FromForm(Name = "scope")] string? scope) =>
{
    if (!string.Equals(grantType, "client_credentials", StringComparison.Ordinal))
    {
        return Results.BadRequest(new { error = "unsupported_grant_type" });
    }

    // Pinguteca SDK defaults to HTTP Basic auth so the credentials
    // ride the Authorization header rather than the form body.
    if (string.IsNullOrEmpty(clientId) &&
        req.Headers.Authorization.ToString() is { Length: > 0 } header &&
        header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..]));
        var split = raw.IndexOf(':');
        if (split > 0)
        {
            clientId = raw[..split];
        }
    }

    var token = $"fake.{clientId ?? "anonymous"}.{Guid.NewGuid():N}";
    return Results.Ok(new
    {
        access_token = token,
        token_type = "Bearer",
        expires_in = 3600,
    });
}).DisableAntiforgery();

app.Run();
