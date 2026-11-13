namespace SpendSmart.Application.Caching;

/// <summary>Per-user cache for expensive report queries. Any write for a user invalidates all of their entries.</summary>
public interface IDashboardCache
{
    Task<T> GetOrCreateAsync<T>(
        string userId, string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default);

    void Invalidate(string userId);
}
