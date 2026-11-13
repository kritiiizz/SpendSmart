using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SpendSmart.Api.Authentication;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Accounts;

namespace SpendSmart.Api.Controllers;

[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting(ApiServiceCollectionExtensions.AuthRateLimitPolicy)]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
[Tags("Auth")]
public sealed class AuthController(IAccountService accounts, IJwtTokenService tokens) : ApiControllerBase
{
    /// <summary>Creates an account (with default categories) and returns an access token.</summary>
    [HttpPost("register")]
    [EndpointSummary("Register a new account")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TokenResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var userId = await accounts.RegisterAsync(request, cancellationToken);
        var token = tokens.CreateToken(new AuthenticatedUser(userId, request.Email));
        return StatusCode(StatusCodes.Status201Created, token);
    }

    [HttpPost("login")]
    [EndpointSummary("Exchange email and password for a JWT")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await accounts.AuthenticateAsync(request, cancellationToken);
        if (user is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The email or password is incorrect, or the account is temporarily locked.");
        }

        return tokens.CreateToken(user);
    }
}
