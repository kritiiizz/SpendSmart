using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Exceptions;

namespace SpendSmart.Api.ErrorHandling;

/// <summary>
/// Turns exceptions thrown by API requests into RFC 9457 <see cref="ProblemDetails"/> responses,
/// so every error has the same shape. Non-API (MVC page) requests fall through to the error page.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // UseExceptionHandler has already rewritten Request.Path to the error page by the time handlers run,
        // so the original request path has to come from the feature.
        var originalPath = httpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? httpContext.Request.Path;
        if (!originalPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            && !originalPath.Equals("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var problem = exception switch
        {
            ValidationException validation => new ValidationProblemDetails(GroupErrors(validation))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            },
            DomainException domain => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request is invalid.",
                Detail = domain.Message,
            },
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Detail = notFound.Message,
            },
            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The request conflicts with the current state.",
                Detail = conflict.Message,
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication is required.",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
            },
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, originalPath);
        }
        else
        {
            logger.LogInformation("Request failed with {StatusCode}: {Message}", problem.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static Dictionary<string, string[]> GroupErrors(ValidationException exception) =>
        exception.Errors
            .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
}
