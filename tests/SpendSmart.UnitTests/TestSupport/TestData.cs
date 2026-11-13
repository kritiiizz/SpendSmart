using System.Reflection;
using SpendSmart.Domain.Common;
using SpendSmart.Domain.Entities;

namespace SpendSmart.UnitTests.TestSupport;

internal static class TestData
{
    public const string UserId = "user-1";
    public const string OtherUserId = "user-2";

    /// <summary>Ids are normally assigned by the database; tests set them directly.</summary>
    public static T WithId<T>(this T entity, int id) where T : Entity
    {
        typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, id);
        return entity;
    }

    /// <summary>Sets a navigation property the way EF Core's Include would.</summary>
    public static T WithCategory<T>(this T entity, Category category) where T : Entity
    {
        typeof(T).GetProperty("Category", BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, category);
        return entity;
    }

    public static Category Category(int id = 1, string name = "Groceries", string userId = UserId) =>
        new Category(userId, name, "#198754").WithId(id);
}

/// <summary>A clock frozen at a given instant, so date-dependent rules are deterministic.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public FixedTimeProvider(int year, int month, int day)
        : this(new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero))
    {
    }

    public override DateTimeOffset GetUtcNow() => now;
}
