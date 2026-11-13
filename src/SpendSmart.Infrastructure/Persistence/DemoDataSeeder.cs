using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpendSmart.Application.Categories;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Entities;
using SpendSmart.Domain.Enums;
using SpendSmart.Infrastructure.Identity;

namespace SpendSmart.Infrastructure.Persistence;

/// <summary>
/// Creates a demo account with six months of realistic data so the live demo never looks empty.
/// Uses a fixed random seed, so every environment gets the same data shape.
/// </summary>
public sealed class DemoDataSeeder(
    SpendSmartDbContext db,
    UserManager<ApplicationUser> userManager,
    IOptions<SeedOptions> options,
    TimeProvider timeProvider,
    ILogger<DemoDataSeeder> logger)
{
    private static readonly Dictionary<string, (string[] Descriptions, int PerMonthMin, int PerMonthMax, decimal Min, decimal Max)> Patterns = new()
    {
        ["Groceries"] = (["Supermarket", "Farmers market", "Bakery", "Corner shop", "Weekly shop"], 4, 6, 18m, 120m),
        ["Dining Out"] = (["Lunch with team", "Pizza night", "Coffee & cake", "Sushi", "Brunch"], 3, 6, 9m, 55m),
        ["Transport"] = (["Metro card top-up", "Fuel", "Taxi", "Train ticket", "Parking"], 3, 5, 6m, 45m),
        ["Utilities"] = (["Electricity bill", "Water bill", "Internet"], 2, 3, 35m, 95m),
        ["Entertainment"] = (["Cinema", "Concert tickets", "Bowling", "Museum"], 1, 3, 12m, 45m),
        ["Shopping"] = (["Clothes", "Electronics accessory", "Books", "Home decor"], 1, 3, 15m, 90m),
        ["Health"] = (["Pharmacy", "Dentist", "Vitamins"], 0, 2, 10m, 80m),
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (await userManager.FindByEmailAsync(settings.DemoEmail) is not null)
        {
            return;
        }

        var user = new ApplicationUser { UserName = settings.DemoEmail, Email = settings.DemoEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, settings.DemoPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Could not create the demo user: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var categories = DefaultCategories.All
            .Select(c => new Category(user.Id, c.Name, c.Color))
            .ToDictionary(c => c.Name);
        db.Categories.AddRange(categories.Values);
        await db.SaveChangesAsync(cancellationToken);

        var today = timeProvider.Today();
        var firstMonth = DateHelpers.FirstDayOfMonth(today.Year, today.Month).AddMonths(-5);

        AddBudgets(user.Id, categories);
        await AddRecurringExpensesAsync(user.Id, categories, firstMonth, today, cancellationToken);
        AddVariableExpenses(user.Id, categories, firstMonth, today);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded demo account {Email}", settings.DemoEmail);
    }

    private void AddBudgets(string userId, Dictionary<string, Category> categories)
    {
        db.Budgets.AddRange(
            new Budget(userId, categories["Groceries"].Id, 450m),
            new Budget(userId, categories["Dining Out"].Id, 150m),
            new Budget(userId, categories["Entertainment"].Id, 80m),
            new Budget(userId, categories["Shopping"].Id, 200m),
            new Budget(userId, categories["Transport"].Id, 120m));
    }

    private async Task AddRecurringExpensesAsync(
        string userId, Dictionary<string, Category> categories, DateOnly firstMonth, DateOnly today, CancellationToken cancellationToken)
    {
        var templates = new[]
        {
            new RecurringExpense(userId, 1450m, "Rent", categories["Housing"].Id, RecurrenceFrequency.Monthly, firstMonth),
            new RecurringExpense(userId, 15.49m, "Netflix", categories["Subscriptions"].Id, RecurrenceFrequency.Monthly, firstMonth.AddDays(11)),
            new RecurringExpense(userId, 11.99m, "Spotify", categories["Subscriptions"].Id, RecurrenceFrequency.Monthly, firstMonth.AddDays(19)),
            new RecurringExpense(userId, 39.99m, "Gym membership", categories["Health"].Id, RecurrenceFrequency.Monthly, firstMonth.AddDays(4)),
        };
        db.RecurringExpenses.AddRange(templates);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var template in templates)
        {
            foreach (var date in template.CollectDueOccurrences(today))
            {
                db.Expenses.Add(new Expense(userId, template.Amount, template.Description, date, template.CategoryId, template.Id));
            }
        }
    }

    private void AddVariableExpenses(string userId, Dictionary<string, Category> categories, DateOnly firstMonth, DateOnly today)
    {
        var random = new Random(42);
        decimal currentMonthDining = 0;

        for (var month = firstMonth; month <= today; month = month.AddMonths(1))
        {
            var lastDay = month.Year == today.Year && month.Month == today.Month
                ? today.Day
                : DateTime.DaysInMonth(month.Year, month.Month);

            foreach (var (categoryName, pattern) in Patterns)
            {
                var count = random.Next(pattern.PerMonthMin, pattern.PerMonthMax + 1);
                for (var i = 0; i < count; i++)
                {
                    var amount = Math.Round(pattern.Min + ((decimal)random.NextDouble() * (pattern.Max - pattern.Min)), 2);
                    var date = month.AddDays(random.Next(0, lastDay));
                    var description = pattern.Descriptions[random.Next(pattern.Descriptions.Length)];
                    db.Expenses.Add(new Expense(userId, amount, description, date, categories[categoryName].Id));

                    if (categoryName == "Dining Out" && date.Month == today.Month && date.Year == today.Year)
                    {
                        currentMonthDining += amount;
                    }
                }
            }
        }

        // Guarantee the demo shows a budget alert: push "Dining Out" past 80% of its 150 limit.
        const decimal diningTarget = 130m;
        if (currentMonthDining < diningTarget)
        {
            db.Expenses.Add(new Expense(
                userId, diningTarget - currentMonthDining, "Birthday dinner", today, categories["Dining Out"].Id));
        }
    }
}
