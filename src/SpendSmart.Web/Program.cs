using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using SpendSmart.Api;
using SpendSmart.Application;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Common;
using SpendSmart.Infrastructure;
using SpendSmart.Infrastructure.Persistence;
using SpendSmart.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Structured logging. Sinks and levels come from the "Serilog" section of appsettings.
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Layers: Application (use cases) → Infrastructure (EF Core, Identity, jobs) → Api (REST + JWT).
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddSpendSmartApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddControllersWithViews().AddSpendSmartApiControllers();
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Azure App Service, Render, etc. terminate TLS at a proxy; trust its X-Forwarded-* headers.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();

// API requests get ProblemDetails (GlobalExceptionHandler); pages get the friendly error view.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandlingPath = "/home/error",
    StatusCodeSelector = exception => exception is NotFoundException
        ? StatusCodes.Status404NotFound
        : StatusCodes.Status500InternalServerError,
});
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    api => api.UseStatusCodePages());
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    pages => pages.UseStatusCodePagesWithReExecute("/home/status/{0}"));

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapSpendSmartApiDocs();
app.MapHealthChecks("/health");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await app.Services.InitialiseDatabaseAsync();
await app.RunAsync();

/// <summary>Exposed for WebApplicationFactory in the integration tests.</summary>
public partial class Program;
