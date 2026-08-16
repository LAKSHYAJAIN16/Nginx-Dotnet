using System.Collections.Concurrent;
using NginxDotNet.Core.Abstractions;

namespace NginxDotNet.Core.Adaptors.RateLimit;

public class RateLimiterAdaptor : IRateLimiterAdaptor
{
    private class RequestTracker
    {
        public int Count;
        public DateTime WindowStart;
    }

    private readonly ConcurrentDictionary<string, RequestTracker> _trackers = new();

    public Task<bool> IsAllowedAsync(string clientIdentifier, int maxRequests, TimeSpan window)
    {
        var now = DateTime.UtcNow;

        var tracker = _trackers.AddOrUpdate(
            clientIdentifier,
            _ => new RequestTracker { Count = 1, WindowStart = now },
            (_, existing) =>
            {
                lock (existing)
                {
                    if (now - existing.WindowStart > window)
                    {
                        existing.Count = 1;
                        existing.WindowStart = now;
                    }
                    else
                    {
                        existing.Count++;
                    }
                    return existing;
                }
            });

        return Task.FromResult(tracker.Count <= maxRequests);
    }
}
