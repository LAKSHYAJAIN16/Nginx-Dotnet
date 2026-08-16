using System.Collections.Concurrent;
using NginxDotNet.Core.Abstractions;
using NginxDotNet.Core.Models;

namespace NginxDotNet.Core.Adaptors.Cache;

public class MemoryCacheAdaptor : ICacheAdaptor
{
    private class CacheEntry
    {
        public required AuthResult Result { get; set; }
        public required DateTime ExpiresAt { get; set; }
    }

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public Task<AuthResult?> GetAsync(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.ExpiresAt)
            {
                return Task.FromResult<AuthResult?>(entry.Result);
            }
            _cache.TryRemove(key, out _); // Expired
        }
        return Task.FromResult<AuthResult?>(null);
    }

    public Task SetAsync(string key, AuthResult result, TimeSpan timeToLive)
    {
        _cache[key] = new CacheEntry
        {
            Result = result,
            ExpiresAt = DateTime.UtcNow.Add(timeToLive)
        };
        return Task.CompletedTask;
    }
}
