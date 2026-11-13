using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.Common;
using SpendSmart.Application.Expenses;

namespace SpendSmart.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ExpensesApiTests(SpendSmartApiFactory factory)
{
    [Fact]
    public async Task Get_WithoutToken_Returns401ProblemDetails()
    {
        var response = await factory.CreateClient().GetAsync("/api/expenses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_ThenGet_RoundTrips()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var groceries = await client.GetCategoryAsync("Groceries");

        var response = await client.PostAsJsonAsync(
            "/api/expenses", new SaveExpenseRequest(42.10m, "Weekly shop", ApiTestExtensions.Today, groceries.Id), SpendSmartApiFactory.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var created = await response.ReadAsync<ExpenseDto>();

        var fetched = await client.GetFromJsonAsync<ExpenseDto>(response.Headers.Location, SpendSmartApiFactory.Json);
        Assert.Equal(created, fetched);
        Assert.Equal("Groceries", fetched!.CategoryName);
    }

    [Fact]
    public async Task Create_WithInvalidBody_Returns400WithFieldErrors()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var groceries = await client.GetCategoryAsync("Groceries");

        var response = await client.PostAsJsonAsync(
            "/api/expenses", new SaveExpenseRequest(0m, "", ApiTestExtensions.Today, groceries.Id), SpendSmartApiFactory.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains("amount", problem.Errors.Keys);
        Assert.Contains("description", problem.Errors.Keys);
    }

    [Fact]
    public async Task OtherUsersExpense_IsNotVisible_AndCannotBeDeleted()
    {
        var alice = await factory.CreateAuthenticatedClientAsync();
        var bob = await factory.CreateAuthenticatedClientAsync();
        var alicesExpense = await alice.CreateExpenseAsync(9.99m, "Private", (await alice.GetCategoryAsync("Other")).Id);

        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/expenses/{alicesExpense.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/expenses/{alicesExpense.Id}")).StatusCode);
        var bobsList = await bob.GetFromJsonAsync<PagedResult<ExpenseDto>>("/api/expenses", SpendSmartApiFactory.Json);
        Assert.Equal(0, bobsList!.TotalCount);
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync($"/api/expenses/{alicesExpense.Id}")).StatusCode);
    }

    [Fact]
    public async Task Bob_CannotUseAlicesCategory()
    {
        var alice = await factory.CreateAuthenticatedClientAsync();
        var bob = await factory.CreateAuthenticatedClientAsync();
        var alicesCategory = await alice.GetCategoryAsync("Groceries");

        var response = await bob.PostAsJsonAsync(
            "/api/expenses", new SaveExpenseRequest(5m, "Sneaky", ApiTestExtensions.Today, alicesCategory.Id), SpendSmartApiFactory.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_SupportsCategoryFilterSortingAndPaging()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var groceries = (await client.GetCategoryAsync("Groceries")).Id;
        var dining = (await client.GetCategoryAsync("Dining Out")).Id;
        foreach (var amount in new[] { 10m, 20m, 30m, 40m, 50m })
        {
            await client.CreateExpenseAsync(amount, $"Shop {amount}", groceries);
        }

        await client.CreateExpenseAsync(99m, "Dinner", dining);

        var page = await client.GetFromJsonAsync<PagedResult<ExpenseDto>>(
            "/api/expenses?category=groceries&sortBy=amount&sortDirection=asc&page=2&pageSize=2", SpendSmartApiFactory.Json);

        Assert.Equal(5, page!.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(new[] { 30m, 40m }, page.Items.Select(e => e.Amount));
    }

    [Fact]
    public async Task List_WithInvalidQuery_Returns400()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/expenses?pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_And_Delete_Work()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var category = (await client.GetCategoryAsync("Transport")).Id;
        var expense = await client.CreateExpenseAsync(12m, "Taxi", category);

        var update = await client.PutAsJsonAsync(
            $"/api/expenses/{expense.Id}", new SaveExpenseRequest(15m, "Taxi home", expense.Date, category), SpendSmartApiFactory.Json);
        Assert.Equal(15m, (await update.ReadAsync<ExpenseDto>()).Amount);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/expenses/{expense.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/expenses/{expense.Id}")).StatusCode);
    }
}
