var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

app.MapGet("/api/data", (HttpContext context) =>
{
    var userId = context.Request.Headers["X-User-ID"].FirstOrDefault() ?? "Anonymous";
    var userRole = context.Request.Headers["X-User-Role"].FirstOrDefault() ?? "Guest";

    return Results.Ok(new
    {
        Status = "Success",
        Message = "Access Granted to Downstream API!",
        AuthenticatedUser = userId,
        Role = userRole,
        Timestamp = DateTime.UtcNow
    });
});

app.Run("http://localhost:5002");
