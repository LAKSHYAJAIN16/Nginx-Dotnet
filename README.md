# NginxDotNet.Core

[![Publish NuGet Package](https://github.com/LAKSHYAJAIN16/Nginx-Dotnet/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/LAKSHYAJAIN16/Nginx-Dotnet/actions/workflows/nuget-publish.yml)
[![NuGet Package](https://img.shields.io/badge/nuget-v1.0.0-blue)](https://github.com/LAKSHYAJAIN16/Nginx-Dotnet)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

High-performance **NGINX & Envoy** authentication subrequest, rate-limiting, caching, and header-transformation gateway framework for **C# .NET 9**.

Similar to how OpenResty (`lua-nginx-module`) embeds Lua into NGINX, `NginxDotNet.Core` pairs NGINX's native `auth_request` subrequest pipeline and Envoy's `ext_authz` filter with pluggable **C# .NET 9 Adaptors** for JWT validation, API Key authentication, in-memory caching, rate limiting, and claim-to-header transformation.

---

## 📦 Installation via NuGet

### .NET CLI
```bash
dotnet add package NginxDotNet.Core --version 1.0.0
```

### Package Manager Console
```powershell
Install-Package NginxDotNet.Core -Version 1.0.0
```

---

## 🏗️ Architecture

```
                               ┌───────────────────────────────┐
                               │  Client Request (/api/data)   │
                               └───────────────┬───────────────┘
                                               │
                                               ▼
                                  ┌─────────────────────────┐
                                  │ NGINX Proxy (Port 8080) │
                                  └────────────┬────────────┘
                                               │
                       1. Auth Subrequest      │
                       GET /validate           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                              NginxDotNet AuthService (Port 5001)                       │
│                                                                                        │
│ ┌──────────────────────┐  ┌──────────────────────┐  ┌────────────────────────────────┐ │
│ │  RateLimiterAdaptor  │  │  MemoryCacheAdaptor  │  │         Auth Adaptors          │ │
│ ├──────────────────────┤  ├──────────────────────┤  ├────────────────────────────────┤ │
│ │ 100 req/min per IP   │  │ Sub-ms TTL caching   │  │ • JwtBearerAdaptor (HMAC/RSA)  │ │
│ └──────────────────────┘  └──────────────────────┘  │ • ApiKeyAdaptor (X-API-Key)    │ │
│                                                     └────────────────────────────────┘ │
└──────────────────────────────────────────────┬─────────────────────────────────────────┘
                                               │
                                               │ 2. Returns 200 OK + Headers:
                                               │    X-User-ID: user_12345
                                               │    X-User-Role: Admin
                                               │    X-Tenant-ID: Tenant_A
                                               ▼
                                  ┌─────────────────────────┐
                                  │ NGINX Proxy (Port 8080) │
                                  └────────────┬────────────┘
                                               │
                       3. Forwarded Request    │
                       with User Headers       ▼
                                  ┌─────────────────────────┐
                                  │ BackendService (5002)   │
                                  └─────────────────────────┘
```

---

## 🔌 Included Adaptors (`NginxDotNet.Core`)

| Adaptor | Namespace | Description |
| :--- | :--- | :--- |
| **`JwtBearerAdaptor`** | `NginxDotNet.Core.Adaptors.Auth` | Validates signed JWT tokens (HMAC-SHA256/RSA), checks lifetime/issuer/audience, and extracts claims into headers (`X-User-ID`, `X-User-Role`, `X-Tenant-ID`). |
| **`ApiKeyAdaptor`** | `NginxDotNet.Core.Adaptors.Auth` | Fast hash lookup for `X-API-Key` headers. |
| **`MemoryCacheAdaptor`** | `NginxDotNet.Core.Adaptors.Cache` | Thread-safe LRU in-memory TTL cache for auth decision tokens (delivering < 0.5ms subrequest latencies). |
| **`RateLimiterAdaptor`** | `NginxDotNet.Core.Adaptors.RateLimit` | Thread-safe sliding window rate limiter responding with `429 Too Many Requests`. |
| **`ClaimToHeaderMapper`** | `NginxDotNet.Core.Adaptors.Transforms` | Dynamically maps JWT claims into downstream HTTP headers. |

---

## 🚀 Usage Example in C#

```csharp
using NginxDotNet.Core.Adaptors.Auth;
using NginxDotNet.Core.Adaptors.Cache;
using NginxDotNet.Core.Adaptors.RateLimit;

var builder = WebApplication.CreateSlimBuilder(args);

// Register NginxDotNet Core Adaptors
builder.Services.AddSingleton<ICacheAdaptor, MemoryCacheAdaptor>();
builder.Services.AddSingleton<IRateLimiterAdaptor, RateLimiterAdaptor>();
builder.Services.AddSingleton<JwtBearerAdaptor>();
builder.Services.AddSingleton<ApiKeyAdaptor>();

var app = builder.Build();

app.MapGet("/validate", async (HttpContext context, 
    JwtBearerAdaptor jwtAdaptor, 
    ApiKeyAdaptor apiKeyAdaptor,
    ICacheAdaptor cache) =>
{
    var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

    // Run Adaptors
    var result = await jwtAdaptor.AuthenticateAsync(headers, context.Request.Path);
    if (result.IsSuccess)
    {
        foreach (var (k, v) in result.Headers) context.Response.Headers[k] = v;
        return Results.Ok();
    }

    return Results.Unauthorized();
});

app.Run();
```

---

## 🛠️ Building & Packaging Locally

```bash
# Build & Test
dotnet test

# Create NuGet Package (.nupkg)
dotnet pack NginxDotNet.Core/NginxDotNet.Core.csproj -c Release -o ./nupkg
```

---

## 📄 License

Distributed under the [MIT License](LICENSE).
