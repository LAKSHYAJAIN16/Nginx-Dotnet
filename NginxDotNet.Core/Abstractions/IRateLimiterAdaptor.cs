namespace NginxDotNet.Core.Abstractions;

public interface IRateLimiterAdaptor
{
    Task<bool> IsAllowedAsync(string clientIdentifier, int maxRequests, TimeSpan window);
}
