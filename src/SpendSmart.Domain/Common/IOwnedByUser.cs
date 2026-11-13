namespace SpendSmart.Domain.Common;

/// <summary>Marks an entity that belongs to exactly one user and must never be shown to anyone else.</summary>
public interface IOwnedByUser
{
    string UserId { get; }
}
