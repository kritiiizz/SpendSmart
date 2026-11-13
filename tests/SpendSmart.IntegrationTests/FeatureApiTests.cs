using System.Net;
using System.Net.Http.Json;
using SpendSmart.Api.Authentication;
using SpendSmart.Application.Budgets;
using SpendSmart.Application.Common;
using SpendSmart.Application.Expenses;
using SpendSmart.Application.RecurringExpenses;
using SpendSmart.Application.Reports;
using SpendSmart.Domain.Enums;

namespace SpendSmart.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class FeatureApiTests(SpendSmartApiFactory factory)
{
    [Fact]
    public async Task Register_CreatesDefaultCategories()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var categories = await client.GetFromJsonAsync<List<object>>("/api/categories");

        Assert.Equal(10, categories!.Count);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();
        var email = $"login-{Guid.NewGuid():N}@tests.dev";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password123" });

        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Nope12345" });
        var right = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.False(string.IsNullOrEmpty((await right.ReadAsync<TokenResponse>()).AccessToken));
    }

    [Fact]
    public async Task Budget_ShowsWarningAt80Percent()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var groceries = (await client.GetCategoryAsync("Groceries")).Id;
        var create = await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(groceries, 100m), SpendSmartApiFactory.Json);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        await client.CreateExpenseAsync(85m, "Big shop", groceries);
        var budget = Assert.Single((await client.GetFromJsonAsync<List<BudgetDto>>("/api/budgets", SpendSmartApiFactory.Json))!);

        Assert.Equal(BudgetStatus.Warning, budget.Status);
        Assert.Equal(85m, budget.PercentUsed);
    }

    [Fact]
    public async Task Budget_DuplicateCategory_Returns409()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var groceries = (await client.GetCategoryAsync("Groceries")).Id;
        await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(groceries, 100m), SpendSmartApiFactory.Json);

        var duplicate = await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(groceries, 200m), SpendSmartApiFactory.Json);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Dashboard_ReflectsNewExpenses_AfterCacheInvalidation()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var category = (await client.GetCategoryAsync("Dining Out")).Id;
        await client.CreateExpenseAsync(20m, "Lunch", category);
        var before = await client.GetFromJsonAsync<DashboardDto>("/api/reports/dashboard", SpendSmartApiFactory.Json);

        await client.CreateExpenseAsync(30m, "Dinner", category);
        var after = await client.GetFromJsonAsync<DashboardDto>("/api/reports/dashboard", SpendSmartApiFactory.Json);

        Assert.Equal(20m, before!.TotalThisMonth);
        Assert.Equal(50m, after!.TotalThisMonth);
        Assert.Equal("Dining Out", Assert.Single(after.SpendingByCategory).CategoryName);
    }

    [Fact]
    public async Task RecurringExpense_StartingToday_IsBookedImmediately()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var housing = (await client.GetCategoryAsync("Housing")).Id;

        var response = await client.PostAsJsonAsync("/api/recurring-expenses",
            new CreateRecurringExpenseRequest(1200m, "Rent", housing, RecurrenceFrequency.Monthly, ApiTestExtensions.Today),
            SpendSmartApiFactory.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var recurring = await response.ReadAsync<RecurringExpenseDto>();
        Assert.True(recurring.NextOccurrence > ApiTestExtensions.Today);
        var expenses = await client.GetFromJsonAsync<PagedResult<ExpenseDto>>("/api/expenses", SpendSmartApiFactory.Json);
        Assert.True(Assert.Single(expenses!.Items).IsRecurring);
    }

    [Fact]
    public async Task DeletingCategoryInUse_Returns409()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var other = await client.GetCategoryAsync("Other");
        await client.CreateExpenseAsync(5m, "Something", other.Id);

        var response = await client.DeleteAsync($"/api/categories/{other.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
