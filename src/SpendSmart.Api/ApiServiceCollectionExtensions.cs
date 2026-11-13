using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SpendSmart.Api.Authentication;
using SpendSmart.Api.Controllers;
using SpendSmart.Api.ErrorHandling;
using SpendSmart.Api.OpenApi;

namespace SpendSmart.Api;

public static class ApiServiceCollectionExtensions
{
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>Registers JWT authentication, OpenAPI, ProblemDetails and rate limiting for the API.</summary>
    public static IServiceCollection AddSpendSmartApi(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(o => o.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters.")
            .ValidateOnStart();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddAuthentication().AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                var settings = jwt.Value;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTokenService.CreateKey(settings.SigningKey),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        // Slows down password guessing against the token endpoint. Limit is configurable (tests raise it).
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            var permitLimit = configuration.GetValue("RateLimiting:AuthPermitsPerMinute", 10);
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }

    /// <summary>Adds the API controllers to an existing MVC registration and serializes enums as strings.</summary>
    public static IMvcBuilder AddSpendSmartApiControllers(this IMvcBuilder mvc)
    {
        mvc.ConfigureApplicationPartManager(manager =>
        {
            var assembly = typeof(ApiControllerBase).Assembly;
            if (!manager.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == assembly))
            {
                manager.ApplicationParts.Add(new AssemblyPart(assembly));
            }
        });

        // FluentValidation (in the application layer) is the single source of validation rules. Without this,
        // MVC would treat every non-nullable string as [Required] and reject requests before FluentValidation
        // runs, producing a second, differently shaped set of errors.
        mvc.AddMvcOptions(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

        mvc.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        return mvc;
    }

    /// <summary>Serves the OpenAPI document at /openapi/v1.json and Swagger UI at /swagger.</summary>
    public static WebApplication MapSpendSmartApiDocs(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "SpendSmart API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "SpendSmart API";
            options.EnablePersistAuthorization();
        });
        return app;
    }
}
