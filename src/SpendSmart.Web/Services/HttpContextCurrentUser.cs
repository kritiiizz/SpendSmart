using System.Security.Claims;
using SpendSmart.Application.Abstractions;

namespace SpendSmart.Web.Services;

/// <summary>Reads the user id from the cookie principal (MVC) or the JWT "sub" claim (API).</summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string UserId
    {
        get
        {
            var principal = Principal;
            var userId = principal?.Identity?.IsAuthenticated == true
                ? principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub")
                : null;
            return userId ?? throw new UnauthorizedAccessException("No user is signed in.");
        }
    }
}
