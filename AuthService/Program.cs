var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

// NGINX auth_request subrequest endpoint
app.MapGet("/validate", (HttpContext context) =>
{
    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

    // Validate token (Example: "Bearer secret-token-123")
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Unauthorized();
    }

    var token = authHeader["Bearer ".Length..].Trim();

    if (token == "secret-token-123")
    {
        // Headers returned here will be captured by NGINX via auth_request_set
        context.Response.Headers["X-User-ID"] = "user_9941";
        context.Response.Headers["X-User-Role"] = "Admin";
        context.Response.Headers["X-Auth-Status"] = "Verified";
        return Results.Ok();
    }

    return Results.Unauthorized();
});

app.Run("http://localhost:5001");
