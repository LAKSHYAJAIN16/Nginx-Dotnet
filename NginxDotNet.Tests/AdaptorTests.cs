using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NginxDotNet.Core.Adaptors.Auth;
using NginxDotNet.Core.Adaptors.Cache;
using NginxDotNet.Core.Adaptors.RateLimit;
using NginxDotNet.Core.Adaptors.Transforms;
using NginxDotNet.Core.Models;
using Xunit;

namespace NginxDotNet.Tests;

public class AdaptorTests
{
    private const string SecretKey = "SuperSecretKeyForJwtSigning1234567890!";

    [Fact]
    public async Task JwtBearerAdaptor_ValidToken_ReturnsSuccessResultWithHeaders()
    {
        // Arrange
        var adaptor = new JwtBearerAdaptor(new JwtBearerAdaptorOptions
        {
            SecretKey = SecretKey,
            Issuer = "NginxDotNet",
            Audience = "NginxDotNetClients"
        });

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "user_12345"),
                new Claim("role", "Admin"),
                new Claim("tenant", "Tenant_A")
            }),
            Expires = DateTime.UtcNow.AddMinutes(30),
            Issuer = "NginxDotNet",
            Audience = "NginxDotNetClients",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var jwtString = tokenHandler.CreateEncodedJwt(tokenDescriptor);

        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {jwtString}"
        };

        // Act
        var result = await adaptor.AuthenticateAsync(headers, "/api/resource");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("user_12345", result.Headers["X-User-ID"]);
        Assert.Equal("Admin", result.Headers["X-User-Role"]);
        Assert.Equal("Tenant_A", result.Headers["X-Tenant-ID"]);
    }

    [Fact]
    public async Task ApiKeyAdaptor_ValidKey_ReturnsSuccess()
    {
        // Arrange
        var adaptor = new ApiKeyAdaptor(new ApiKeyAdaptorOptions
        {
            HeaderName = "X-API-Key",
            ValidKeys = new Dictionary<string, string> { ["key-999"] = "Client_App" }
        });

        var headers = new Dictionary<string, string> { ["X-API-Key"] = "key-999" };

        // Act
        var result = await adaptor.AuthenticateAsync(headers, "/api/data");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Client_App", result.Headers["X-Client-ID"]);
    }

    [Fact]
    public async Task MemoryCacheAdaptor_SetAndGet_ReturnsCachedResult()
    {
        // Arrange
        var cache = new MemoryCacheAdaptor();
        var result = AuthResult.Success(new Dictionary<string, string> { ["X-User-ID"] = "user_777" });

        // Act
        await cache.SetAsync("auth_token_key", result, TimeSpan.FromSeconds(10));
        var cached = await cache.GetAsync("auth_token_key");

        // Assert
        Assert.NotNull(cached);
        Assert.True(cached!.IsSuccess);
        Assert.Equal("user_777", cached.Headers["X-User-ID"]);
    }

    [Fact]
    public async Task RateLimiterAdaptor_ExceedsLimit_ReturnsFalse()
    {
        // Arrange
        var limiter = new RateLimiterAdaptor();
        var clientId = "192.168.1.100";

        // Act
        var req1 = await limiter.IsAllowedAsync(clientId, maxRequests: 2, window: TimeSpan.FromSeconds(10));
        var req2 = await limiter.IsAllowedAsync(clientId, maxRequests: 2, window: TimeSpan.FromSeconds(10));
        var req3 = await limiter.IsAllowedAsync(clientId, maxRequests: 2, window: TimeSpan.FromSeconds(10));

        // Assert
        Assert.True(req1);
        Assert.True(req2);
        Assert.False(req3); // Blocked
    }

    [Fact]
    public void ClaimToHeaderMapper_MapsClaimsCorrectly()
    {
        // Arrange
        var mapper = new ClaimToHeaderMapper();
        var claims = new Dictionary<string, string>
        {
            ["sub"] = "user_404",
            ["role"] = "Manager",
            ["tenant"] = "Tenant_B"
        };

        // Act
        var headers = mapper.MapClaimsToHeaders(claims);

        // Assert
        Assert.Equal("user_404", headers["X-User-ID"]);
        Assert.Equal("Manager", headers["X-User-Role"]);
        Assert.Equal("Tenant_B", headers["X-Tenant-ID"]);
    }
}
