using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SpendSmart.Application.Abstractions;

namespace SpendSmart.Api.Authentication;

public sealed record TokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    TokenResponse CreateToken(AuthenticatedUser user);
}

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public TokenResponse CreateToken(AuthenticatedUser user)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(settings.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.UserId),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            }),
            SigningCredentials = new SigningCredentials(CreateKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
        };

        return new TokenResponse(_handler.CreateToken(descriptor), "Bearer", expiresAt);
    }

    internal static SymmetricSecurityKey CreateKey(string signingKey) => new(Encoding.UTF8.GetBytes(signingKey));
}
