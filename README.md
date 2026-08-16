# ngx-dotnet-auth (`Nginx-Dotnet`)

A high-performance, modular **NGINX + C# (.NET 9)** authorization subrequest & reverse proxy gateway framework.

Similar to how OpenResty (`lua-nginx-module`) embeds Lua into NGINX, `ngx-dotnet-auth` pairs NGINX's native `auth_request` subrequest pipeline with pluggable **C# .NET 9 Adaptors** for JWT validation, API Key authentication, in-memory caching, rate limiting, and claim-to-header transformation.

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

## 🚀 Quick Start

### 1. Build & Run Unit Tests
```bash
dotnet test NginxDotNet.Tests/NginxDotNet.Tests.csproj
```

### 2. Start AuthService (Port 5001)
```bash
dotnet run --project AuthService
```

### 3. Start BackendService (Port 5002)
```bash
dotnet run --project BackendService
```

### 4. Start ProxyService / NGINX (Port 8080)
```bash
# If using NGINX:
nginx -c /path/to/nginx.conf

# Or if testing locally without NGINX binary:
dotnet run --project ProxyService
```

---

## 🧪 Testing with `curl`

### 1. JWT Bearer Token Request (Authorized)
```bash
curl -i -H "Authorization: Bearer <YOUR_JWT_TOKEN>" http://localhost:8080/api/data
```

### 2. API Key Request (Authorized)
```bash
curl -i -H "X-API-Key: secret-api-key-123" http://localhost:8080/api/data
```

### 3. Missing / Invalid Token (Unauthorized)
```bash
curl -i http://localhost:8080/api/data
```
**Response:** `HTTP/1.1 401 Unauthorized`

---

## 📂 Project Structure

```
├── NginxDotNet.Core/          # Core Adaptor Library & Interfaces
│   ├── Abstractions/          # IAuthAdaptor, ICacheAdaptor, IRateLimiterAdaptor
│   ├── Adaptors/              # JwtBearerAdaptor, ApiKeyAdaptor, MemoryCacheAdaptor, RateLimiterAdaptor
│   └── Models/                # AuthResult model
├── NginxDotNet.Tests/         # xUnit Automated Unit Test Suite
├── AuthService/               # High-Performance C# Gateway Subrequest API
├── BackendService/            # Sample Downstream Microservice
├── ProxyService/              # NGINX-simulated Proxy for environments without NGINX installed
├── nginx.conf                 # NGINX production configuration (auth_request)
├── envoy.yaml                 # Envoy production configuration (ext_authz filter)
└── docker-compose.yml         # Container orchestration setup
```
