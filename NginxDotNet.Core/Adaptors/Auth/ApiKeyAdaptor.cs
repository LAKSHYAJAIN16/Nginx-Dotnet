using NginxDotNet.Core.Abstractions;
using NginxDotNet.Core.Models;

namespace NginxDotNet.Core.Adaptors.Auth;

public class ApiKeyAdaptorOptions
{
    public string HeaderName { get; set; } = "X-API-Key";
    public Dictionary<string, string> ValidKeys { get; set; } = new()
    {
        ["secret-api-key-123"] = "Client_App_1",
        ["admin-api-key-999"] = "System_Admin"
    };
}

public class ApiKeyAdaptor : IAuthAdaptor
{
    private readonly ApiKeyAdaptorOptions _options;

    public string Name => "ApiKeyAdaptor";

    public ApiKeyAdaptor(ApiKeyAdaptorOptions? options = null)
    {
        _options = options ?? new ApiKeyAdaptorOptions();
    }

    public Task<AuthResult> AuthenticateAsync(IDictionary<string, string> requestHeaders, string path)
    {
        if (!requestHeaders.TryGetValue(_options.HeaderName, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(AuthResult.Fail(401, $"Missing {_options.HeaderName} header"));
        }

        if (_options.ValidKeys.TryGetValue(apiKey, out var clientName))
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Client-ID"] = clientName,
                ["X-Auth-Scheme"] = "ApiKey"
            };

            return Task.FromResult(AuthResult.Success(headers));
        }

        return Task.FromResult(AuthResult.Fail(401, "Invalid API Key"));
    }
}
