using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SpendSmart.Api.Controllers;

/// <summary>
/// API endpoints accept only JWT bearer tokens (never the MVC cookie), which keeps them immune to CSRF.
/// Errors are returned as ProblemDetails by <see cref="ErrorHandling.GlobalExceptionHandler"/>.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public abstract class ApiControllerBase : ControllerBase;
