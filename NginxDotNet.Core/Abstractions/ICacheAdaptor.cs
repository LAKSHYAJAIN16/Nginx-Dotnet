using NginxDotNet.Core.Models;

namespace NginxDotNet.Core.Abstractions;

public interface ICacheAdaptor
{
    Task<AuthResult?> GetAsync(string key);
    Task SetAsync(string key, AuthResult result, TimeSpan timeToLive);
}
