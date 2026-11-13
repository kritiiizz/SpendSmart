using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SpendSmart.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class HostTests(SpendSmartApiFactory factory)
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger/index.html")]
    [InlineData("/")]
    [InlineData("/account/login")]
    public async Task PublicEndpoints_Return200(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedPage_RedirectsAnonymousUserToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/expenses");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenApiDocument_ListsExpenseEndpoints_ButNotMvcPages()
    {
        var json = await factory.CreateClient().GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/expenses", json);
        Assert.DoesNotContain("/account/login", json, StringComparison.OrdinalIgnoreCase);
    }
}
