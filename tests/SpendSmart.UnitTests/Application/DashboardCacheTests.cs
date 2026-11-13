using Microsoft.Extensions.Caching.Memory;
using SpendSmart.Application.Caching;

namespace SpendSmart.UnitTests.Application;

public class DashboardCacheTests
{
    private readonly DashboardCache _cache = new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task GetOrCreateAsync_ReusesValueUntilInvalidated()
    {
        var calls = 0;
        Task<string> Factory(CancellationToken _) => Task.FromResult($"value-{++calls}");

        var first = await _cache.GetOrCreateAsync("alice", "2026-03", Factory);
        var second = await _cache.GetOrCreateAsync("alice", "2026-03", Factory);
        _cache.Invalidate("alice");
        var third = await _cache.GetOrCreateAsync("alice", "2026-03", Factory);

        Assert.Equal("value-1", first);
        Assert.Equal("value-1", second);
        Assert.Equal("value-2", third);
    }

    [Fact]
    public async Task Invalidate_OnlyAffectsThatUser()
    {
        await _cache.GetOrCreateAsync("alice", "k", _ => Task.FromResult("alice-1"));
        await _cache.GetOrCreateAsync("bob", "k", _ => Task.FromResult("bob-1"));

        _cache.Invalidate("alice");

        Assert.Equal("alice-2", await _cache.GetOrCreateAsync("alice", "k", _ => Task.FromResult("alice-2")));
        Assert.Equal("bob-1", await _cache.GetOrCreateAsync("bob", "k", _ => Task.FromResult("bob-2")));
    }
}
