var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();

// Simulates NGINX auth_request proxy pipeline on Port 8080
app.Map("/{**catchAll}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();

    // 1. Trigger internal auth_request subrequest to C# AuthService (Port 5001)
    var authReq = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5001/validate");
    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        authReq.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
    }

    var authRes = await client.SendAsync(authReq);

    if (!authRes.IsSuccessStatusCode)
    {
        context.Response.StatusCode = (int)authRes.StatusCode;
        return;
    }

    // 2. Extract headers returned by AuthService
    var userId = authRes.Headers.TryGetValues("X-User-ID", out var uId) ? uId.FirstOrDefault() : null;
    var userRole = authRes.Headers.TryGetValues("X-User-Role", out var uRole) ? uRole.FirstOrDefault() : null;

    // 3. Proxy request downstream to BackendService (Port 5002)
    var backendReq = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5002/api/data");
    if (!string.IsNullOrEmpty(userId)) backendReq.Headers.Add("X-User-ID", userId);
    if (!string.IsNullOrEmpty(userRole)) backendReq.Headers.Add("X-User-Role", userRole);

    var backendRes = await client.SendAsync(backendReq);

    context.Response.StatusCode = (int)backendRes.StatusCode;
    context.Response.ContentType = backendRes.Content.Headers.ContentType?.ToString() ?? "application/json";

    var content = await backendRes.Content.ReadAsStringAsync();
    await context.Response.WriteAsync(content);
});

app.Run("http://localhost:8080");
