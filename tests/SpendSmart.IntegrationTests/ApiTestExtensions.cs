using System.Net.Http.Json;
using SpendSmart.Application.Categories;
using SpendSmart.Application.Expenses;

namespace SpendSmart.IntegrationTests;

internal static class ApiTestExtensions
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(SpendSmartApiFactory.Json))!;

    public static async Task<CategoryDto> GetCategoryAsync(this HttpClient client, string name)
    {
        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories", SpendSmartApiFactory.Json);
        return categories!.Single(c => c.Name == name);
    }

    public static async Task<ExpenseDto> CreateExpenseAsync(
        this HttpClient client, decimal amount, string description, int categoryId, DateOnly? date = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/expenses", new SaveExpenseRequest(amount, description, date ?? Today, categoryId), SpendSmartApiFactory.Json);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<ExpenseDto>();
    }
}
