using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace SpendSmart.Application.Caching;

/// <summary>
/// <see cref="IMemoryCache"/>-backed implementation. Each user's entries share a cancellation token,
/// so invalidating a user evicts every cached month for them in one call.
/// Register as a singleton; swap for a Redis-backed version when running more than one instance.
/// </summary>
public sealed class DashboardCache(IMemoryCache cache) : IDashboardCache, IDisposable
{
    private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _userTokens = new();

    public async Task<T> GetOrCreateAsync<T>(
        string userId, string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"dashboard:{userId}:{key}";
        if (cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);

        var userToken = _userTokens.GetOrAdd(userId, _ => new CancellationTokenSource());
        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeToLive }
            .AddExpirationToken(new CancellationChangeToken(userToken.Token));
        cache.Set(cacheKey, value, options);

        return value;
    }

    public void Invalidate(string userId)
    {
        if (_userTokens.TryRemove(userId, out var tokenSource))
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var tokenSource in _userTokens.Values)
        {
            tokenSource.Dispose();
        }

        _userTokens.Clear();
    }
}
