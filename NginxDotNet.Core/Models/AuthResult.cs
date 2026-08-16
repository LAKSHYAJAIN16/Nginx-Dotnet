namespace NginxDotNet.Core.Models;

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; } = 200;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static AuthResult Success(Dictionary<string, string>? headers = null) => new()
    {
        IsSuccess = true,
        StatusCode = 200,
        Headers = headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    };

    public static AuthResult Fail(int statusCode = 401, string? message = null) => new()
    {
        IsSuccess = false,
        StatusCode = statusCode,
        ErrorMessage = message
    };
}
