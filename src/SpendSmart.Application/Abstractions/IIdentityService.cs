namespace SpendSmart.Application.Abstractions;

/// <summary>Wraps ASP.NET Core Identity so the application layer doesn't depend on it.</summary>
public interface IIdentityService
{
    Task<IdentityResultDto> CreateUserAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Returns the user when the credentials are valid and the account isn't locked out.</summary>
    Task<AuthenticatedUser?> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
}

public sealed record AuthenticatedUser(string UserId, string Email);

public sealed record IdentityError(string Code, string Description);

public sealed record IdentityResultDto(string? UserId, IReadOnlyList<IdentityError> Errors)
{
    public bool Succeeded => UserId is not null && Errors.Count == 0;
}
