using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SpendSmart.Application.Accounts;
using SpendSmart.Application.Budgets;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Categories;
using SpendSmart.Application.Expenses;
using SpendSmart.Application.RecurringExpenses;
using SpendSmart.Application.Reports;

namespace SpendSmart.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IDashboardCache, DashboardCache>();

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IRecurringExpenseService, RecurringExpenseService>();
        services.AddScoped<IRecurringExpenseProcessor, RecurringExpenseProcessor>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
