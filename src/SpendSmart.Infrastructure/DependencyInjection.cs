using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpendSmart.Application.Abstractions;
using SpendSmart.Infrastructure.BackgroundJobs;
using SpendSmart.Infrastructure.Identity;
using SpendSmart.Infrastructure.Persistence;
using SpendSmart.Infrastructure.Persistence.Repositories;

namespace SpendSmart.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Resolved lazily so tests (WebApplicationFactory) can override the connection string.
        services.AddDbContext<SpendSmartDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5));
        });

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SpendSmartDbContext>());
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IRecurringExpenseRepository, RecurringExpenseRepository>();

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<SpendSmartDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddOptions<SeedOptions>().BindConfiguration(SeedOptions.SectionName);
        services.AddScoped<DemoDataSeeder>();

        services.AddOptions<RecurringExpenseOptions>().BindConfiguration(RecurringExpenseOptions.SectionName);
        services.AddHostedService<RecurringExpenseWorker>();

        services.AddHealthChecks().AddDbContextCheck<SpendSmartDbContext>("database");

        return services;
    }
}
