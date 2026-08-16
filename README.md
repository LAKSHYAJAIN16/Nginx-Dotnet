# ngx-dotnet-auth

High-performance **NGINX + C# (.NET 9)** authorization subrequest & reverse proxy architecture.

Similar to how OpenResty (`lua-nginx-module`) embeds Lua into NGINX, `ngx-dotnet-auth` uses NGINX's native `auth_request` subrequest pipeline paired with ultra-fast ASP.NET Core Minimal APIs for token validation, session management, and dynamic HTTP header injection.

---

## 🏗️ Architecture

```
[ Client ]
    │
    ▼ (GET /api/data with Authorization Header)
[ NGINX (Port 8080) ]
    │
    ├── 1. Subrequest GET /validate ────────► [ C# AuthService (Port 5001) ]
    │                                             │
    │   ◄── 2. 200 OK + (X-User-ID: user_9941) ───┘
    │
    └── 3. Proxy GET /api/data ──────────────► [ C# BackendService (Port 5002) ]
                                                  │
        ◄── 4. Response with User Data ───────────┘
```

---

## 🚀 Quick Start

### 1. Start C# AuthService (Port 5001)
```bash
dotnet run --project AuthService
```

### 2. Start C# BackendService (Port 5002)
```bash
dotnet run --project BackendService
```

### 3. Start NGINX (Port 8080)
```bash
nginx -c /path/to/ngx-dotnet-auth/nginx.conf
```

---

## 🧪 Testing

### Test 1: Request Without Token (Unauthorized)
```bash
curl -i http://localhost:8080/api/data
```
**Response:** `HTTP/1.1 401 Unauthorized`

---

### Test 2: Request With Valid Token (Authorized)
```bash
curl -i -H "Authorization: Bearer secret-token-123" http://localhost:8080/api/data
```
**Response:**
```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "status": "Success",
  "message": "Access Granted to Downstream API!",
  "authenticatedUser": "user_9941",
  "role": "Admin",
  "timestamp": "2026-08-16T01:12:11Z"
}
```

---

## 📁 Repository Structure

* `AuthService/` — High-performance C# authentication & header injection service (.NET 9 / Native AOT ready)
* `BackendService/` — Downstream protected microservice receiving injected headers
* `ProxyService/` — NGINX-simulated proxy handler for environments without native NGINX binary installed
* `nginx.conf` — Standard production NGINX configuration using `auth_request` and `auth_request_set`
