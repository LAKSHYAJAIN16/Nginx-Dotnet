using NginxDotNet.Core.Abstractions;
using NginxDotNet.Core.Adaptors.Auth;
using NginxDotNet.Core.Adaptors.Cache;
using NginxDotNet.Core.Adaptors.RateLimit;
using NginxDotNet.Core.Models;

var builder = WebApplication.CreateSlimBuilder(args);

// Register Core Adaptors
builder.Services.AddSingleton<ICacheAdaptor, MemoryCacheAdaptor>();
builder.Services.AddSingleton<IRateLimiterAdaptor, RateLimiterAdaptor>();
builder.Services.AddSingleton<JwtBearerAdaptor>();
builder.Services.AddSingleton<ApiKeyAdaptor>();

var app = builder.Build();

var cache = app.Services.GetRequiredService<ICacheAdaptor>();
var rateLimiter = app.Services.GetRequiredService<IRateLimiterAdaptor>();
var jwtAdaptor = app.Services.GetRequiredService<JwtBearerAdaptor>();
var apiKeyAdaptor = app.Services.GetRequiredService<ApiKeyAdaptor>();

// List of configured Auth Adaptors
var authAdaptors = new List<IAuthAdaptor> { jwtAdaptor, apiKeyAdaptor };

// Main NGINX auth_request subrequest endpoint
app.MapGet("/validate", async (HttpContext context) =>
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // 1. Rate Limiting Check (100 req / min per IP)
    var allowed = await rateLimiter.IsAllowedAsync(clientIp, maxRequests: 100, window: TimeSpan.FromMinutes(1));
    if (!allowed)
    {
        return Results.StatusCode(429); // 429 Too Many Requests
    }

    // Extract request headers
    var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    var path = context.Request.Path.Value ?? "/";

    // Create Cache Key based on Auth Headers
    var cacheKey = headers.TryGetValue("Authorization", out var authVal) ? authVal :
                   headers.TryGetValue("X-API-Key", out var apiVal) ? apiVal : null;

    // 2. Fast Path: Check Cache
    if (!string.IsNullOrEmpty(cacheKey))
    {
        var cachedResult = await cache.GetAsync(cacheKey);
        if (cachedResult != null)
        {
            ApplyResultHeaders(context, cachedResult);
            return cachedResult.IsSuccess ? Results.Ok() : Results.StatusCode(cachedResult.StatusCode);
        }
    }

    // 3. Fallback Legacy / Hardcoded Token Check ("Bearer secret-token-123")
    if (!string.IsNullOrEmpty(authVal) && authVal.Trim() == "Bearer secret-token-123")
    {
        var legacyResult = AuthResult.Success(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-User-ID"] = "user_9941",
            ["X-User-Role"] = "Admin",
            ["X-Auth-Status"] = "Verified"
        });

        if (!string.IsNullOrEmpty(cacheKey))
            await cache.SetAsync(cacheKey, legacyResult, TimeSpan.FromMinutes(5));

        ApplyResultHeaders(context, legacyResult);
        return Results.Ok();
    }

    // 4. Run Auth Adaptors Pipeline
    foreach (var adaptor in authAdaptors)
    {
        var result = await adaptor.AuthenticateAsync(headers, path);
        if (result.IsSuccess)
        {
            if (!string.IsNullOrEmpty(cacheKey))
            {
                await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            }

            ApplyResultHeaders(context, result);
            return Results.Ok();
        }
    }

    return Results.Unauthorized();
});

void ApplyResultHeaders(HttpContext context, AuthResult result)
{
    foreach (var (headerKey, headerVal) in result.Headers)
    {
        context.Response.Headers[headerKey] = headerVal;
    }
}

app.Run("http://localhost:5001");
