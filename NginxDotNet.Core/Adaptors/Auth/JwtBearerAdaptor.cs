using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NginxDotNet.Core.Abstractions;
using NginxDotNet.Core.Models;

namespace NginxDotNet.Core.Adaptors.Auth;

public class JwtBearerAdaptorOptions
{
    public string SecretKey { get; set; } = "SuperSecretKeyForJwtSigning1234567890!";
    public string Issuer { get; set; } = "NginxDotNet";
    public string Audience { get; set; } = "NginxDotNetClients";
    public string HeaderName { get; set; } = "Authorization";
}

public class JwtBearerAdaptor : IAuthAdaptor
{
    private readonly JwtBearerAdaptorOptions _options;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public string Name => "JwtBearerAdaptor";

    public JwtBearerAdaptor(JwtBearerAdaptorOptions? options = null)
    {
        _options = options ?? new JwtBearerAdaptorOptions();
    }

    public Task<AuthResult> AuthenticateAsync(IDictionary<string, string> requestHeaders, string path)
    {
        if (!requestHeaders.TryGetValue(_options.HeaderName, out var authHeader) || string.IsNullOrWhiteSpace(authHeader))
        {
            return Task.FromResult(AuthResult.Fail(401, "Missing Authorization header"));
        }

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthResult.Fail(401, "Invalid Authorization scheme"));
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var key = Encoding.UTF8.GetBytes(_options.SecretKey);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = !string.IsNullOrEmpty(_options.Issuer),
                ValidIssuer = _options.Issuer,
                ValidateAudience = !string.IsNullOrEmpty(_options.Audience),
                ValidAudience = _options.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            var claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value, StringComparer.OrdinalIgnoreCase);

            var userId = claims.GetValueOrDefault("sub") 
                ?? claims.GetValueOrDefault("userId") 
                ?? claims.GetValueOrDefault(ClaimTypes.NameIdentifier) 
                ?? "unknown";

            var userRole = claims.GetValueOrDefault("role") 
                ?? claims.GetValueOrDefault(ClaimTypes.Role) 
                ?? "User";

            var tenantId = claims.GetValueOrDefault("tenant") 
                ?? "default";

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-User-ID"] = userId,
                ["X-User-Role"] = userRole,
                ["X-Tenant-ID"] = tenantId
            };

            return Task.FromResult(AuthResult.Success(headers));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthResult.Fail(401, $"Token validation failed: {ex.Message}"));
        }
    }
}
