namespace SpendSmart.Application.Abstractions;

/// <summary>The signed-in user, resolved from cookies (MVC) or a JWT (API).</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The user's id. Throws <see cref="UnauthorizedAccessException"/> when nobody is signed in.</summary>
    string UserId { get; }
}
